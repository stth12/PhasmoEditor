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
            string settingsPath = Path.Combine(directory, "settings.json");
            var window = new PhasmoEditor.MainWindow(settingsPath);
            Assert(window.Strings.Language == "en", "English is the first-launch default");
            Assert(window.Icon is not null, "Application icon loaded");
            var candidates = (System.Windows.Controls.ComboBox)window.FindName("Candidates");
            candidates.ItemsSource = new[] { path };
            candidates.SelectedIndex = 0;
            Assert(((System.Windows.Controls.TextBox)window.FindName("MoneyInput")).Text == "100", "UI loads current values");
            Assert(((System.Windows.Controls.Button)window.FindName("SaveButton")).IsEnabled, "UI enables valid save");
            var moneyInput = (System.Windows.Controls.TextBox)window.FindName("MoneyInput");
            moneyInput.Text = "777";
            var englishButton = (System.Windows.Controls.Button)window.FindName("EnglishButton");
            var russianButton = (System.Windows.Controls.Button)window.FindName("RussianButton");
            russianButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            window.Dispatcher.Invoke(() => { }, System.Windows.Threading.DispatcherPriority.DataBind);
            Assert(window.Strings.Language == "ru", "Language switch to Russian");
            Assert(moneyInput.Text == "777", "Language switch preserves unsaved values");
            Assert(((System.Windows.Controls.Button)window.FindName("SaveButton")).Content.ToString() == "Проверить и сохранить  →", "Live button translation");
            var savedPreference = new PhasmoEditor.Localization();
            savedPreference.Initialize(settingsPath);
            Assert(savedPreference.Language == "ru", "Language preference persists");
            englishButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert(File.ReadAllBytes(path).SequenceEqual(original), "Language switching never modifies the save");
            moneyInput.Text = "invalid";
            ((System.Windows.Controls.Button)window.FindName("SaveButton")).RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert(((System.Windows.Controls.TextBlock)window.FindName("Status")).Text.StartsWith("Money:"), "English validation message");
            russianButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert(((System.Windows.Controls.TextBlock)window.FindName("Status")).Text.StartsWith("Деньги:"), "Existing validation message translates");
            englishButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            File.WriteAllBytes(path, [1, 2, 3]);
            candidates.SelectedIndex = -1;
            candidates.SelectedIndex = 0;
            Assert(!((System.Windows.Controls.Button)window.FindName("SaveButton")).IsEnabled, "UI rejects corrupt save");
            Assert(((System.Windows.Controls.ComboBox)window.FindName("BackupList")).Items.Count > 0, "UI retains restore access");
            Assert(((System.Windows.Controls.TextBlock)window.FindName("Status")).Text == window.Strings["InvalidFormat"], "Localized format error");
            russianButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            Assert(((System.Windows.Controls.TextBlock)window.FindName("Status")).Text == "Неподдерживаемый или повреждённый файл сохранения.", "Error translates without reloading");
            englishButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            service.Restore(path, backup1);
            candidates.SelectedIndex = -1;
            candidates.SelectedIndex = 0;
            foreach (string key in PhasmoEditor.Localization.Keys)
            {
                Assert(!string.IsNullOrWhiteSpace(window.Strings[key]), "English translation: " + key);
                savedPreference.SetLanguage("ru", false);
                Assert(!string.IsNullOrWhiteSpace(savedPreference[key]), "Russian translation: " + key);
            }
            File.WriteAllText(settingsPath, "[]");
            var corruptPreference = new PhasmoEditor.Localization();
            corruptPreference.Initialize(settingsPath);
            Assert(corruptPreference.Language == "en", "Invalid settings fall back to English");
            if (args.Length > 3 && args[3] == "ru")
                russianButton.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Button.ClickEvent));
            var visual = (FrameworkElement)window.Content;
            int width = args.Length > 1 ? int.Parse(args[1]) : 1120;
            int height = args.Length > 2 ? int.Parse(args[2]) : 880;
            visual.Measure(new Size(width, height));
            visual.Arrange(new Rect(0, 0, width, height));
            visual.UpdateLayout();
            if (args.Length > 0)
            {
                var image = new RenderTargetBitmap(width, height, 96, 96, PixelFormats.Pbgra32);
                image.Render(visual);
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(image));
                using var stream = File.Create(args[0]);
                encoder.Save(stream);
            }
            window.Close();
            Console.WriteLine("PASS: save handling, recovery, file errors, English default, live EN/RU switching, input preservation, translated errors, preferences and icon.");
            return 0;
        }
        catch (Exception ex) { Console.Error.WriteLine(ex); return 1; }
        finally { Directory.Delete(directory, true); }
    }
}
