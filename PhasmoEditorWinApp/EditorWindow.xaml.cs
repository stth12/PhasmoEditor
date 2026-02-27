using System.Windows;

namespace PhasmoEditor
{
    public partial class EditorWindow : Window
    {
        public EditorWindow()
        {
            InitializeComponent();
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            string playersMoney = PlayersMoneyTextBox.Text;
            string newLevel = NewLevelTextBox.Text;
            string prestige = PrestigeTextBox.Text;

            // Pass the input values to the result window
            ResultWindow resultWindow = new ResultWindow(playersMoney, newLevel, prestige);
            resultWindow.Show();
            this.Close();
        }
    }
}
