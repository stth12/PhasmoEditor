using System.Windows;

namespace PhasmoEditor
{
    public partial class AlertWindow : Window
    {
        public AlertWindow()
        {
            InitializeComponent();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            EditorWindow editorWindow = new EditorWindow();
            editorWindow.Show();
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) 
        {
            this.Close();
        }
    }
}
