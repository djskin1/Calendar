using System;
using System.Windows;
using System.Windows.Controls;

namespace CompanyCalendar
{
    public partial class MainWindow : Window
    {
        private DateTime currentMonth = DateTime.Today;

        public MainWindow()
        {
            InitializeComponent();

            StatusComboBox.SelectedIndex = 0;

            UpdateMonthTitle();
        }

        private void UpdateMonthTitle()
        {
            MonthText.Text = currentMonth.ToString("MMMM yyyy");
            MainCalendar.DisplayDate = currentMonth;
        }

        private void PreviousMonthButton_Click(object sender, RoutedEventArgs e)
        {
            currentMonth = currentMonth.AddMonths(-1);

            UpdateMonthTitle();
        }

        private void NextMonthButton_Click(object sender, RoutedEventArgs e)
        {
            currentMonth = currentMonth.AddMonths(1);

            UpdateMonthTitle();
        }

        private void MainCalendar_SelectedDatesChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (MainCalendar.SelectedDate.HasValue)
            {
                SelectedDateText.Text =
                    MainCalendar.SelectedDate.Value.ToString("dddd, dd MMMM yyyy");
            }
        }

        private void SaveEntryButton_Click(object sender, RoutedEventArgs e)
        {
            if (!MainCalendar.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Please select a date.",
                    "Calendar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            if (StatusComboBox.SelectedItem is not ComboBoxItem selectedStatus)
            {
                return;
            }

            string status = selectedStatus.Content.ToString() ?? "";
            string notes = NotesTextBox.Text;

            MessageBox.Show(
                $"Date: {MainCalendar.SelectedDate.Value:dd-MM-yyyy}\n" +
                $"Status: {status}\n" +
                $"Notes: {notes}",
                "Entry saved",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void NewEntryButton_Click(object sender, RoutedEventArgs e)
        {
            MainCalendar.SelectedDate = DateTime.Today;
            MainCalendar.DisplayDate = DateTime.Today;

            StatusComboBox.SelectedIndex = 0;
            NotesTextBox.Clear();
        }

        private void CalendarButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Calendar");
        }

        private void UsersButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("User management will be added here.");
        }

        private void AdminButton_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Admin Panel will be added here.");
        }
    }
}