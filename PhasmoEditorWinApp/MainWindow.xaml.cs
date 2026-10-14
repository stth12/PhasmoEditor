using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using Microsoft.Win32;
using PhasmoEditor.Core;

namespace PhasmoEditor;

public partial class MainWindow : Window
{
    private readonly SaveService service = new();
    private SaveSnapshot? loaded;
    private string? selectedPath;
    private string statusKey = "Welcome";
    private object[] statusArguments = [];
    private Exception? statusError;
    public Localization Strings => (Localization)Resources["Strings"];
    public MainWindow() : this(null) { }
    public MainWindow(string? settingsPath)
    {
        InitializeComponent();
        Strings.Initialize(settingsPath);
        RefreshLanguage();
    }

    private void English_Click(object sender, RoutedEventArgs e) => ChangeLanguage("en");
    private void Russian_Click(object sender, RoutedEventArgs e) => ChangeLanguage("ru");
    private void ChangeLanguage(string language)
    {
        try { Strings.SetLanguage(language); }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { SetStatus("SettingsError"); }
        RefreshLanguage();
    }
    private void RefreshLanguage()
    {
        if (selectedPath is null) PathLabel.Text = Strings["NoSelection"];
        MoneyCurrent.Text = loaded is null ? Strings["CurrentEmpty"] : Strings.Format("CurrentValue", loaded.Money);
        LevelCurrent.Text = loaded is null ? Strings["CurrentEmpty"] : Strings.Format("CurrentValue", loaded.Level);
        PrestigeCurrent.Text = loaded is null ? Strings["CurrentEmpty"] : Strings.Format("CurrentValue", loaded.Prestige);
        Status.Text = statusError is null ? Strings.Format(statusKey, statusArguments) : Strings.Describe(statusError);
    }
    private void SetStatus(string key, params object[] arguments)
    {
        statusKey = key; statusArguments = arguments; statusError = null;
        Status.Text = Strings.Format(key, arguments);
    }
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
    private void NavigateSave_Click(object sender, RoutedEventArgs e) => SaveSection.BringIntoView();
    private void NavigateProgress_Click(object sender, RoutedEventArgs e) => ProgressSection.BringIntoView();
    private void NavigateBackups_Click(object sender, RoutedEventArgs e) => BackupsSection.BringIntoView();

    private void Run(Action action)
    {
        try { action(); }
        catch (Exception ex) { statusError = ex; Status.Text = Strings.Describe(ex); }
    }

    private void Load(string path)
    {
        loaded = null;
        selectedPath = path;
        SaveButton.IsEnabled = false;
        BackupList.ItemsSource = null;
        PathLabel.Text = path;
        MoneyInput.Clear(); LevelInput.Clear(); PrestigeInput.Clear();
        RefreshLanguage();
        BackupList.ItemsSource = SaveService.Backups(path);
        if (BackupList.Items.Count > 0) BackupList.SelectedIndex = 0;
        loaded = service.Load(path);
        MoneyInput.Text = loaded.Money.ToString(CultureInfo.InvariantCulture);
        LevelInput.Text = loaded.Level.ToString(CultureInfo.InvariantCulture);
        PrestigeInput.Text = loaded.Prestige.ToString(CultureInfo.InvariantCulture);
        SaveButton.IsEnabled = true;
        SetStatus("Loaded");
        RefreshLanguage();
    }

    private void Discover_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        string directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "AppData", "LocalLow", "Kinetic Games", "Phasmophobia");
        var paths = SaveService.Discover(directory);
        Candidates.ItemsSource = paths;
        if (paths.Length == 0) SetStatus("NoSaves"); else SetStatus("FoundCount", paths.Length);
        if (paths.Length == 1) Candidates.SelectedIndex = 0;
    });

    private void Candidates_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (Candidates.SelectedItem is string path) Run(() => Load(path));
    }

    private void Browse_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = Strings["FileFilter"], Title = Strings["Browse"], CheckFileExists = true };
        if (dialog.ShowDialog(this) == true) Run(() => Load(dialog.FileName));
    }

    private void Reload_Click(object sender, RoutedEventArgs e)
    {
        if (selectedPath is not null) Run(() => Load(selectedPath));
    }

    private int Parse(TextBox input, string nameKey)
    {
        if (!int.TryParse(input.Text, NumberStyles.None, CultureInfo.InvariantCulture, out int value))
            throw SaveErrors.Tag(new ArgumentException("Invalid number."), "InvalidInput", nameKey, int.MaxValue);
        return value;
    }

    private static void CheckGame()
    {
        var processes = Process.GetProcessesByName("Phasmophobia");
        try
        {
            if (processes.Length > 0) throw SaveErrors.Tag(new IOException("Close Phasmophobia before writing."), "GameRunning");
        }
        finally { foreach (var process in processes) process.Dispose(); }
    }

    private bool Confirm(string titleKey, string message)
    {
        var dialog = new Window
        {
            Owner = this, Title = Strings[titleKey], Width = 470,
            SizeToContent = SizeToContent.Height, WindowStartupLocation = WindowStartupLocation.CenterOwner,
            WindowStyle = WindowStyle.None, ResizeMode = ResizeMode.NoResize, ShowInTaskbar = false,
            Background = new SolidColorBrush(Color.FromRgb(23, 32, 29)), FontFamily = FontFamily
        };
        var panel = new StackPanel { Margin = new Thickness(24) };
        var title = new TextBlock { Text = Strings[titleKey], FontSize = 21, FontWeight = FontWeights.SemiBold, Foreground = Brushes.White };
        title.MouseLeftButtonDown += (_, _) => dialog.DragMove();
        panel.Children.Add(title);
        var body = new TextBlock { Text = message, TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.FromRgb(183, 202, 189)), FontSize = 13, Margin = new Thickness(0, 18, 0, 22) };
        panel.Children.Add(new ScrollViewer { Content = body, MaxHeight = 420, VerticalScrollBarVisibility = ScrollBarVisibility.Auto });
        var actions = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = Strings["Cancel"], Style = (Style)FindResource("QuietButton"), IsCancel = true, Margin = new Thickness(0, 0, 10, 0) };
        var accept = new Button { Content = Strings["Confirm"], Style = (Style)FindResource(typeof(Button)), IsDefault = true };
        accept.Click += (_, _) => dialog.DialogResult = true;
        actions.Children.Add(cancel); actions.Children.Add(accept); panel.Children.Add(actions);
        dialog.Content = new Border { CornerRadius = new CornerRadius(16), BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(73, 98, 79)), Child = panel };
        return dialog.ShowDialog() == true;
    }

    private void Save_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (loaded is null) return;
        int money = Parse(MoneyInput, "Money"), level = Parse(LevelInput, "Level"), prestige = Parse(PrestigeInput, "Prestige");
        if (money == loaded.Money && level == loaded.Level && prestige == loaded.Prestige)
        { SetStatus("NoChanges"); return; }
        CheckGame();
        string preview = Strings.Format("Preview", loaded.Money, money, loaded.Level, level, loaded.Prestige, prestige);
        if (!Confirm("ReviewTitle", preview)) return;
        CheckGame();
        string backup = service.Save(loaded, money, level, prestige);
        Load(loaded.Path);
        SetStatus("Saved", backup);
    });

    private void Restore_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (selectedPath is null || !File.Exists(selectedPath) || BackupList.SelectedItem is not string backup)
        { SetStatus("ChooseBackup"); return; }
        CheckGame();
        if (!Confirm("RestoreTitle", Strings.Format("RestorePreview", backup))) return;
        CheckGame();
        service.Restore(selectedPath, backup);
        Load(selectedPath);
        SetStatus("Restored");
    });

    private void OpenBackups_Click(object sender, RoutedEventArgs e) => Run(() =>
    {
        if (selectedPath is null) return;
        string directory = SaveService.BackupDirectory(selectedPath);
        if (!Directory.Exists(directory)) { SetStatus("NoBackups"); return; }
        Process.Start(new ProcessStartInfo(directory) { UseShellExecute = true });
    });
}

public sealed class SaveDisplayConverter : IMultiValueConverter
{
    public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
    {
        bool ru = values.Length > 1 && values[1] is string language && language == "ru";
        if (values[0] is not string path) return ru ? "Выберите файл…" : "Choose a file…";
        string name = Path.GetFileName(path);
        if (name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase))
        {
            var match = System.Text.RegularExpressions.Regex.Match(name, @"\.(\d{8}-\d{6}-\d{7})\.");
            if (match.Success && DateTime.TryParseExact(match.Groups[1].Value, "yyyyMMdd-HHmmss-fffffff",
                CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out var date))
                return date.ToLocalTime().ToString(ru ? "dd.MM.yyyy  ·  HH:mm:ss" : "MMM dd, yyyy  ·  HH:mm:ss", ru ? CultureInfo.GetCultureInfo("ru-RU") : CultureInfo.GetCultureInfo("en-US"));
        }
        if (name.Equals("SaveFileBackup.txt", StringComparison.OrdinalIgnoreCase))
            return ru ? "Копия из предыдущей версии" : "Backup from a previous version";
        return name + "  ·  " + Path.GetFileName(Path.GetDirectoryName(path));
    }

    public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
