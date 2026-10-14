using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Text.Json;

namespace PhasmoEditor;

public sealed class Localization : INotifyPropertyChanged
{
    private static readonly Dictionary<string, (string En, string Ru)> Texts = new()
    {
        ["Minimize"] = ("Minimize", "Свернуть"),
        ["Maximize"] = ("Maximize / restore", "Развернуть / восстановить"),
        ["Close"] = ("Close", "Закрыть"),
        ["Tagline"] = ("Your progress.\nYour control.", "Ваш прогресс.\nПод вашим контролем."),
        ["Workspace"] = ("WORKSPACE", "РАБОЧАЯ ОБЛАСТЬ"),
        ["NavSave"] = ("01   Save file", "01   Сохранение"),
        ["NavProgress"] = ("02   Progress", "02   Параметры"),
        ["NavBackups"] = ("03   Backups", "03   Архив копий"),
        ["FieldNotes"] = ("FIELD NOTES / 001", "ПОЛЕВЫЕ ЗАМЕТКИ / 001"),
        ["BackupNote"] = ("Every change leaves\na backup behind.", "Каждая запись оставляет\nрезервную копию."),
        ["Subtitle"] = ("PHASMOPHOBIA / SAVE EDITOR", "PHASMOPHOBIA / РЕДАКТОР СОХРАНЕНИЙ"),
        ["Heading"] = ("Make progress your own", "Настройте свой прогресс"),
        ["Description"] = ("Load a save. Adjust your progress. Keep a backup.", "Загрузите сохранение. Измените нужное. Сохраните с копией."),
        ["LocalFile"] = ("LOCAL FILE", "ЛОКАЛЬНЫЙ ФАЙЛ"),
        ["SaveFile"] = ("Save file", "Сохранение"),
        ["Reload"] = ("↻  Reload", "↻  Обновить"),
        ["FoundSaves"] = ("Discovered save files", "Найденные сохранения"),
        ["Discover"] = ("Find", "Найти"),
        ["Browse"] = ("Open…", "Открыть…"),
        ["NoSelection"] = ("No save file selected", "Сохранение не выбрано"),
        ["Profile"] = ("Profile settings", "Параметры профиля"),
        ["Money"] = ("Money", "Деньги"),
        ["Level"] = ("Level", "Уровень"),
        ["Prestige"] = ("Prestige", "Престиж"),
        ["MoneyHint"] = ("Player money", "Количество денег"),
        ["CurrentEmpty"] = ("Current value —", "Текущее значение —"),
        ["CurrentValue"] = ("Current: {0}", "Сейчас: {0}"),
        ["CloudNote"] = ("Close the game before saving.\nCheck your Steam Cloud settings.", "Закройте игру перед записью.\nПроверьте настройки Steam Cloud."),
        ["Save"] = ("Review and save  →", "Проверить и сохранить  →"),
        ["Backups"] = ("Backup archive", "Архив копий"),
        ["BackupsHint"] = ("You can always go back", "Можно вернуться назад"),
        ["SelectBackup"] = ("Select a backup", "Выберите резервную копию"),
        ["Restore"] = ("Restore backup", "Восстановить копию"),
        ["OpenFolder"] = ("Open folder  ↗", "Открыть папку  ↗"),
        ["Welcome"] = ("Choose a save file. Compatibility with the current game version still needs verification.", "Выберите сохранение. Совместимость с текущей версией игры требует проверки."),
        ["ChooseFile"] = ("Choose a file…", "Выберите файл…"),
        ["LegacyBackup"] = ("Backup from a previous version", "Копия из предыдущей версии"),
        ["Loaded"] = ("Save loaded. Unchanged fields will keep their current values.", "Сохранение прочитано. Неизменённые поля сохранят текущие значения."),
        ["NoSaves"] = ("No saves found. Open a file manually.", "Сохранения не найдены. Выберите файл вручную."),
        ["FoundCount"] = ("Found {0} save file(s). Choose the one to edit.", "Найдено: {0}. Выберите нужное сохранение."),
        ["FileFilter"] = ("Save files (*.txt)|*.txt|All files (*.*)|*.*", "Сохранения (*.txt)|*.txt|Все файлы (*.*)|*.*"),
        ["InvalidInput"] = ("{0}: enter a whole number between 0 and {1}.", "{0}: введите целое число от 0 до {1}."),
        ["GameRunning"] = ("Close Phasmophobia before saving or restoring.", "Закройте Phasmophobia перед записью или восстановлением."),
        ["NoChanges"] = ("There are no changes to save.", "Нет изменений для записи."),
        ["Preview"] = ("Money: {0} → {1}\nLevel: {2} → {3}\nPrestige: {4} → {5}\n\nA backup will be created. Save these changes?", "Деньги: {0} → {1}\nУровень: {2} → {3}\nПрестиж: {4} → {5}\n\nБудет создана резервная копия. Сохранить?"),
        ["ReviewTitle"] = ("Review changes", "Проверка изменений"),
        ["Confirm"] = ("Confirm", "Подтвердить"),
        ["Cancel"] = ("Cancel", "Отмена"),
        ["Saved"] = ("Changes saved and verified. Backup: {0}", "Изменения записаны и проверены. Резервная копия: {0}"),
        ["ChooseBackup"] = ("Choose a save file and a backup.", "Выберите сохранение и резервную копию."),
        ["RestorePreview"] = ("Restore this backup?\n{0}\n\nThe current file will also be backed up.", "Восстановить файл из копии?\n{0}\n\nТекущее сохранение тоже будет скопировано."),
        ["RestoreTitle"] = ("Restore backup", "Восстановление"),
        ["Restored"] = ("Save restored. The previous file is kept in the backup archive.", "Сохранение восстановлено. Предыдущая версия сохранена в резервных копиях."),
        ["NoBackups"] = ("Backups will appear after the first save.", "Резервные копии появятся после первой записи."),
        ["NegativeValues"] = ("Values cannot be negative.", "Значения не могут быть отрицательными."),
        ["VerificationFailed"] = ("Verification failed. The save was not changed.", "Проверка результата не пройдена. Сохранение не изменено."),
        ["InvalidField"] = ("Unsupported structure in field {0}. No changes were written.", "Неподдерживаемая структура: поле {0}. Изменения не записаны."),
        ["InvalidFormat"] = ("Unsupported or damaged save file.", "Неподдерживаемый или повреждённый файл сохранения."),
        ["ExpectedObject"] = ("The save must contain a JSON object.", "Ожидался объект JSON."),
        ["DecodeFailed"] = ("Could not read the save. The format may be unsupported or the file damaged.", "Не удалось прочитать сохранение. Формат может быть неподдерживаемым или файл повреждён."),
        ["FileChanged"] = ("The file changed after loading. Reload it before saving.", "Файл изменился после загрузки. Загрузите его заново перед записью."),
        ["MissingFile"] = ("The file was not found. Select it again.", "Файл не найден. Выберите его заново."),
        ["AccessDenied"] = ("Access denied. Check file permissions.", "Нет доступа к файлу. Проверьте права доступа."),
        ["IoError"] = ("Could not access the file. It may be locked, missing, or the disk may be full.", "Не удалось обратиться к файлу. Он может быть занят, отсутствовать или на диске нет места."),
        ["UnexpectedError"] = ("The operation failed. Reload the file and try again.", "Операция не выполнена. Перезагрузите файл и повторите попытку."),
        ["SettingsError"] = ("The language changed, but the preference could not be saved.", "Язык изменён, но сохранить настройку не удалось."),
    };
    private string language = "en";
    private string? settingsPath;
    public string Language => language;
    public string this[string key] => Texts.TryGetValue(key, out var pair)
        ? language == "ru" ? pair.Ru : pair.En : throw new KeyNotFoundException(key);
    public event PropertyChangedEventHandler? PropertyChanged;
    public string Format(string key, params object[] values) => string.Format(CultureInfo.InvariantCulture, this[key], values);
    public static IEnumerable<string> Keys => Texts.Keys;

    public void Initialize(string? path = null)
    {
        settingsPath = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "PhasmoEditor", "settings.json");
        try
        {
            if (File.Exists(settingsPath))
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(settingsPath));
                if (doc.RootElement.ValueKind == JsonValueKind.Object && doc.RootElement.TryGetProperty("language", out var value) && value.ValueKind == JsonValueKind.String)
                    SetLanguage(value.GetString() == "ru" ? "ru" : "en", false);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or JsonException) { }
    }

    public void SetLanguage(string value, bool persist = true)
    {
        if (value is not ("en" or "ru")) throw new ArgumentException("Unsupported language.");
        language = value;
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(string.Empty));
        if (!persist || settingsPath is null) return;
        string temporary = settingsPath + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(settingsPath))!);
            File.WriteAllText(temporary, JsonSerializer.Serialize(new { language }));
            File.Move(temporary, settingsPath, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    public string Describe(Exception exception)
    {
        if (exception.Data["MessageKey"] is string key)
        {
            var arguments = (exception.Data["MessageArguments"] as object[] ?? []).ToArray();
            if (key == "InvalidInput" && arguments.Length > 0 && arguments[0] is string field)
                arguments[0] = this[field];
            return Format(key, arguments);
        }
        return this[exception switch
        {
            FileNotFoundException or DirectoryNotFoundException => "MissingFile",
            UnauthorizedAccessException => "AccessDenied",
            IOException => "IoError",
            _ => "UnexpectedError"
        }];
    }
}
