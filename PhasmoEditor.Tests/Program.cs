using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using PhasmoEditor.Core;

internal static class Program
{
    static void Assert(bool result, string message)
    {
        if (!result) throw new Exception(message);
    }
    static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new Exception("Expected " + typeof(T).Name);
    }

    // Independent fixture writer matches the original .NET Framework implementation,
    // with a UTF-8 BOM to cover saves written with that encoding.
    static byte[] LegacyFixture(string json)
    {
        byte[] iv = Enumerable.Range(0, 16).Select(i => (byte)i).ToArray();
        using var aes = Aes.Create();
#pragma warning disable SYSLIB0041, SYSLIB0060
        using var derivation = new Rfc2898DeriveBytes(Encoding.ASCII.GetBytes("t36gref9u84y7f43g"), iv, 100);
#pragma warning restore SYSLIB0041, SYSLIB0060
        aes.Key = derivation.GetBytes(16); aes.IV = iv;
        using var output = new MemoryStream();
        output.Write(iv);
        using (var crypto = new CryptoStream(output, aes.CreateEncryptor(), CryptoStreamMode.Write, leaveOpen: true))
        using (var writer = new StreamWriter(crypto, new UTF8Encoding(true))) writer.Write(json);
        return output.ToArray();
    }

    [STAThread]
    static int Main(string[] args)
    {
        string directory = Path.Combine(Path.GetTempPath(), "PhasmoEditor-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try
        {
            var service = new SaveService();
            string path = Path.Combine(directory, "SaveFile.txt");
            const string json = """{"PlayersMoney":{"value":100,"type":"int"},"NewLevel":{"value":12},"Prestige":{"value":2},"Other":{"value":"O'Brien True False Привет"},"Flags":[true,false],"Nested":{"untouched":1.25}}""";
            byte[] original = LegacyFixture(json);
            File.WriteAllBytes(path, original);
            var loaded = service.Load(path);
            Assert(loaded.Money == 100 && loaded.Level == 12 && loaded.Prestige == 2, "Legacy fixture read");
            string backup1 = service.Save(loaded, 500, 12, 2);
            Assert(File.ReadAllBytes(backup1).SequenceEqual(original), "Exact original backup");
            var changed = service.Load(path);
            Assert(changed.Money == 500 && changed.Level == 12 && changed.Prestige == 2, "Selective field update");
            foreach (var key in new[] { "Other", "Flags", "Nested" })
                Assert(JsonNode.DeepEquals(loaded.Document[key], changed.Document[key]), "Unknown field preserved: " + key);
            byte[] firstEdit = File.ReadAllBytes(path);
            string backup2 = service.Save(changed, 600, 15, 3);
            Assert(backup1 != backup2 && File.Exists(backup1), "Backup history retained");
            Assert(File.ReadAllBytes(backup2).SequenceEqual(firstEdit), "Second backup exact");
            service.Restore(service.Load(path), backup1);
            Assert(File.ReadAllBytes(path).SequenceEqual(original), "Exact restore");
            Assert(SaveService.Backups(path).Length == 3, "Restore backs up current file");
            Throws<ArgumentException>(() => service.Save(service.Load(path), -1, 1, 1));
            Assert(File.ReadAllBytes(path).SequenceEqual(original), "Invalid value leaves original intact");

            loaded = service.Load(path);
            File.WriteAllBytes(path, firstEdit);
            Throws<IOException>(() => service.Save(loaded, 700, 20, 4));
            Assert(File.ReadAllBytes(path).SequenceEqual(firstEdit), "Stale snapshot rejected");

            loaded = service.Load(path);
            using (var locked = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read))
                Throws<IOException>(() => service.Save(loaded, 700, 20, 4));
            Assert(File.ReadAllBytes(path).SequenceEqual(firstEdit), "Locked destination preserved");
            Assert(Directory.GetFiles(directory, "*.tmp").Length == 0, "Temporary files cleaned");

            File.WriteAllBytes(path, [1, 2, 3]);
            Throws<InvalidDataException>(() => service.Load(path));
            service.Restore(path, backup1);
            Assert(File.ReadAllBytes(path).SequenceEqual(original), "Corrupted destination can be restored");
            Throws<InvalidDataException>(() => SaveService.Decode(LegacyFixture("{}")));
            Throws<InvalidDataException>(() => SaveService.Decode(LegacyFixture("{broken")));
            Throws<InvalidDataException>(() => SaveService.Decode(LegacyFixture(json.Replace("\"value\":100", "\"value\":\"100\""))));
            Throws<InvalidDataException>(() => SaveService.Decode(new byte[32]));
            string nested = Path.Combine(directory, "profile");
            Directory.CreateDirectory(nested);
            File.WriteAllBytes(Path.Combine(nested, "SaveFile.txt"), original);
            Assert(SaveService.Discover(directory).Length == 2, "Nested profile discovery");
            Assert(SaveService.Discover(Path.Combine(directory, "missing")).Length == 0, "Missing directory");
            string legacyBackup = Path.Combine(directory, "SaveFileBackup.txt");
            File.WriteAllBytes(legacyBackup, original);
            Assert(SaveService.Backups(path).Contains(legacyBackup), "Legacy backup discoverable");
            var window = new PhasmoEditor.MainWindow();
            var candidates = (System.Windows.Controls.ComboBox)window.FindName("Candidates");
            candidates.ItemsSource = new[] { path };
            candidates.SelectedIndex = 0;
            Assert(((System.Windows.Controls.TextBox)window.FindName("MoneyInput")).Text == "100", "UI loads current values");
            Assert(((System.Windows.Controls.Button)window.FindName("SaveButton")).IsEnabled, "UI enables valid save");
            File.WriteAllBytes(path, [1, 2, 3]);
            candidates.SelectedIndex = -1;
            candidates.SelectedIndex = 0;
            Assert(!((System.Windows.Controls.Button)window.FindName("SaveButton")).IsEnabled, "UI rejects corrupt save");
            Assert(((System.Windows.Controls.ComboBox)window.FindName("BackupList")).Items.Count > 0, "UI retains restore access");
            service.Restore(path, backup1);
            candidates.SelectedIndex = -1;
            candidates.SelectedIndex = 0;
            var visual = (FrameworkElement)window.Content;
            visual.Measure(new Size(920, 720));
            visual.Arrange(new Rect(0, 0, 920, 720));
            visual.UpdateLayout();
            if (args.Length > 0)
            {
                var image = new RenderTargetBitmap(920, 720, 96, 96, PixelFormats.Pbgra32);
                image.Render(visual);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using var stream = File.Create(args[0]);
                encoder.Save(stream);
            }
            window.Close();
            Console.WriteLine("PASS: legacy format, field preservation, backup history, restore, corrupt files, stale snapshots, file locks, discovery, WPF initialization.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(directory, true); }
    }
}
