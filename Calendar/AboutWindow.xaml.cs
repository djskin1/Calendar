using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;

namespace CompanyCalendar
{
    public partial class AboutWindow : Window
    {
        public AboutWindow()
        {
            InitializeComponent();
        }

        private void CloseButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void EmailLink_RequestNavigate(
            object sender,
            RequestNavigateEventArgs e)
        {
            Process.Start(
                new ProcessStartInfo
                {
                    FileName = e.Uri.AbsoluteUri,
                    UseShellExecute = true
                });

            e.Handled = true;
        }
    }
}