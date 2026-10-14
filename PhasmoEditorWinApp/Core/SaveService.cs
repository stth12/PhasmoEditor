using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;

namespace PhasmoEditor.Core;

public sealed record SaveSnapshot(string Path, byte[] Original, JsonObject Document,
    int Money, int Level, int Prestige);

public sealed class SaveService
{
    private static readonly string[] Fields = ["PlayersMoney", "NewLevel", "Prestige"];
    private const string Password = "t36gref9u84y7f43g";

    public SaveSnapshot Load(string path)
    {
        path = System.IO.Path.GetFullPath(path);
        var bytes = File.ReadAllBytes(path);
        var doc = Decode(bytes);
        return new(path, bytes, doc, ReadValue(doc, Fields[0]), ReadValue(doc, Fields[1]), ReadValue(doc, Fields[2]));
    }

    public string Save(SaveSnapshot snapshot, int money, int level, int prestige)
    {
        if (money < 0 || level < 0 || prestige < 0)
            throw new ArgumentException("Значения не могут быть отрицательными.");
        var updated = (JsonObject)snapshot.Document.DeepClone();
        var values = new[] { money, level, prestige };
        for (int i = 0; i < Fields.Length; i++)
            updated[Fields[i]]!["value"] = values[i];
        var encrypted = Encode(updated);
        if (!JsonNode.DeepEquals(updated, Decode(encrypted)))
            throw new InvalidDataException("Проверка результата не пройдена. Сохранение не изменено.");
        return Replace(snapshot, encrypted);
    }

    public string Restore(SaveSnapshot current, string backupPath)
    {
        var backup = Load(backupPath);
        return Replace(current, backup.Original);
    }

    public string Restore(string currentPath, string backupPath)
    {
        var backup = Load(backupPath);
        var path = System.IO.Path.GetFullPath(currentPath);
        var current = new SaveSnapshot(path, File.ReadAllBytes(path), backup.Document,
            backup.Money, backup.Level, backup.Prestige);
        return Replace(current, backup.Original);
    }

    public static string BackupDirectory(string path) =>
        System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!, "PhasmoEditorBackups");

    public static string[] Backups(string path)
    {
        var directory = BackupDirectory(path);
        var prefix = System.IO.Path.GetFileName(path) + ".";
        var backups = Directory.Exists(directory)
            ? Directory.GetFiles(directory).Where(p => System.IO.Path.GetFileName(p).StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
                && p.EndsWith(".bak", StringComparison.OrdinalIgnoreCase)).OrderDescending().ToArray()
            : [];
        var legacy = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(path)!,
            System.IO.Path.GetFileNameWithoutExtension(path) + "Backup" + System.IO.Path.GetExtension(path));
        return File.Exists(legacy) ? [.. backups, legacy] : backups;
    }

    public static string[] Discover(string directory)
    {
        if (!Directory.Exists(directory)) return [];
        var options = new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true };
        return Directory.EnumerateFiles(directory, "SaveFile.txt", options)
            .Where(p => !p.Split(System.IO.Path.DirectorySeparatorChar).Contains("PhasmoEditorBackups", StringComparer.OrdinalIgnoreCase))
            .OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    private static int ReadValue(JsonObject document, string field)
    {
        if (document[field] is not JsonObject entry || entry["value"] is not JsonValue value
            || !value.TryGetValue<int>(out var number))
            throw new InvalidDataException($"Неподдерживаемая структура: поле {field}. Изменения не записаны.");
        return number;
    }

    public static JsonObject Decode(byte[] bytes)
    {
        if (bytes.Length < 32 || (bytes.Length - 16) % 16 != 0)
            throw new InvalidDataException("Неподдерживаемый или повреждённый файл сохранения.");
        try
        {
            using var aes = Aes.Create();
            aes.IV = bytes[..16];
            aes.Key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(Password), aes.IV, 100, HashAlgorithmName.SHA1, 16);
            var plain = aes.DecryptCbc(bytes[16..], aes.IV, PaddingMode.PKCS7);
            var text = new UTF8Encoding(false, true).GetString(plain).TrimStart('\uFEFF').TrimEnd('\0');
            var document = JsonNode.Parse(text) as JsonObject
                ?? throw new InvalidDataException("Ожидался объект JSON.");
            foreach (var field in Fields) ReadValue(document, field);
            return document;
        }
        catch (Exception ex) when (ex is CryptographicException or System.Text.Json.JsonException or DecoderFallbackException)
        {
            throw new InvalidDataException("Не удалось прочитать сохранение. Формат может быть неподдерживаемым или файл повреждён.", ex);
        }
    }

    public static byte[] Encode(JsonObject document)
    {
        using var aes = Aes.Create();
        aes.GenerateIV();
        aes.Key = Rfc2898DeriveBytes.Pbkdf2(Encoding.UTF8.GetBytes(Password), aes.IV, 100, HashAlgorithmName.SHA1, 16);
        var encrypted = aes.EncryptCbc(Encoding.UTF8.GetBytes(document.ToJsonString()), aes.IV, PaddingMode.PKCS7);
        return [.. aes.IV, .. encrypted];
    }

    private static string Replace(SaveSnapshot snapshot, byte[] replacement)
    {
        var directory = System.IO.Path.GetDirectoryName(snapshot.Path)!;
        var temporary = System.IO.Path.Combine(directory, ".phasmoeditor-" + Guid.NewGuid().ToString("N") + ".tmp");
        var backupDir = BackupDirectory(snapshot.Path);
        Directory.CreateDirectory(backupDir);
        var backup = System.IO.Path.Combine(backupDir, System.IO.Path.GetFileName(snapshot.Path) + "."
            + DateTime.UtcNow.ToString("yyyyMMdd-HHmmss-fffffff") + "." + Guid.NewGuid().ToString("N") + ".bak");
        try
        {
            using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            {
                stream.Write(replacement);
                stream.Flush(true);
            }
            if (!File.ReadAllBytes(snapshot.Path).AsSpan().SequenceEqual(snapshot.Original))
                throw new IOException("Файл изменился после загрузки. Загрузите его заново перед записью.");
            // Atomic replacement also preserves the previous destination as a backup.
            File.Replace(temporary, snapshot.Path, backup);
            return backup;
        }
        finally
        {
            if (File.Exists(temporary)) File.Delete(temporary);
        }
    }
}
