using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using PhasmoEditor.Core;

namespace PhasmoEditor;

public partial class MainWindow : Window
{
    private readonly SaveService service = new();
    private SaveSnapshot? loaded;
    public MainWindow() { InitializeComponent(); }

    private void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) { Status.Text = ex.Message; }
    }

    private void Load(string path)
    {
        loaded = null;
        SaveButton.IsEnabled = false;
        BackupList.ItemsSource = null;
        PathLabel.Text = path;
        MoneyInput.Clear(); LevelInput.Clear(); PrestigeInput.Clear();
        MoneyCurrent.Text = LevelCurrent.Text = PrestigeCurrent.Text = "";
        BackupList.ItemsSource = SaveService.Backups(path);
        if (BackupList.Items.Count > 0) BackupList.SelectedIndex = 0;
        loaded = service.Load(path);
        MoneyInput.Text = loaded.Money.ToString(CultureInfo.InvariantCulture);
        LevelInput.Text = loaded.Level.ToString(CultureInfo.InvariantCulture);
        PrestigeInput.Text = loaded.Prestige.ToString(CultureInfo.InvariantCulture);
        MoneyCurrent.Text = "Сейчас: " + MoneyInput.Text;
        LevelCurrent.Text = "Сейчас: " + LevelInput.Text;
        PrestigeCurrent.Text = "Сейчас: " + PrestigeInput.Text;
        SaveButton.IsEnabled = true;
        BackupList.ItemsSource = SaveService.Backups(path);
        if (BackupList.Items.Count > 0) BackupList.SelectedIndex = 0;
        Status.Text = "Сохранение прочитано. Неизменённые поля сохранят текущие значения.";
    }

    private void Discover_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "Kinetic Games", "Phasmophobia");
        var paths = SaveService.Discover(directory);
        Candidates.ItemsSource = paths;
        Status.Text = paths.Length == 0 ? "Сохранения не найдены. Выберите файл вручную." : $"Найдено: {paths.Length}. Выберите нужное сохранение.";
        if (paths.Length == 1) Candidates.SelectedIndex = 0;
    });

    private void Candidates_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Candidates.SelectedItem is string path) Run(() => Load(path));
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Сохранения (*.txt)|*.txt|Все файлы (*.*)|*.*", CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) Run(() => Load(dialog.FileName));
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        if (File.Exists(PathLabel.Text)) Run(() => Load(PathLabel.Text));
    }

    private static int Parse(TextBox input, string name)
    {
        if (!int.TryParse(input.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            throw new ArgumentException($"{name}: введите целое число от 0 до {int.MaxValue}.");
        return value;
    }

    private static void CheckGame()
    {
        var processes = Process.GetProcessesByName("Phasmophobia");
        try
        {
            if (processes.Length > 0) throw new IOException("Закройте Phasmophobia перед записью или восстановлением.");
        }
        finally { foreach (var process in processes) process.Dispose(); }
    }

    private void Save_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (loaded is null) return;
        int money = Parse(MoneyInput, "Деньги"), level = Parse(LevelInput, "Уровень"), prestige = Parse(PrestigeInput, "Престиж");
        if (money == loaded.Money && level == loaded.Level && prestige == loaded.Prestige)
        { Status.Text = "Нет изменений для записи."; return; }
        CheckGame();
        string preview = $"Деньги: {loaded.Money} → {money}\nУровень: {loaded.Level} → {level}\nПрестиж: {loaded.Prestige} → {prestige}\n\nБудет создана резервная копия. Сохранить?";
        if (MessageBox.Show(this, preview, "Проверка изменений", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        CheckGame();
        string backup = service.Save(loaded, money, level, prestige);
        Load(loaded.Path);
        Status.Text = "Изменения записаны и проверены. Резервная копия: " + backup;
    });

    private void Restore_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (!File.Exists(PathLabel.Text) || BackupList.SelectedItem is not string backup) { Status.Text = "Выберите сохранение и резервную копию."; return; }
        CheckGame();
        if (MessageBox.Show(this, $"Восстановить файл из копии?\n{backup}\n\nТекущее сохранение тоже будет скопировано.",
            "Восстановление", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        CheckGame();
        string path = PathLabel.Text;
        service.Restore(path, backup);
        Load(path);
        Status.Text = "Сохранение восстановлено. Предыдущая версия сохранена в резервных копиях.";
    });

    private void OpenBackups_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (loaded is null) return;
        string directory = SaveService.BackupDirectory(loaded.Path);
        if (!Directory.Exists(directory)) { Status.Text = "Резервные копии появятся после первой записи."; return; }
        Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
    });
}
