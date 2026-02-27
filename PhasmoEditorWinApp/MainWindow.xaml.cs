using System.Windows;

namespace PhasmoEditor
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            AlertWindow editorWindow = new AlertWindow();
            editorWindow.Show();
            this.Close();
        }
    }
}
