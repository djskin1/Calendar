using System;
using System.Windows;

namespace CompanyCalendar
{
    public partial class NewEntryWindow : Window
    {
        public NewEntryWindow()
        {
            InitializeComponent();

            EntryDatePicker.SelectedDate = DateTime.Today;

            StatusComboBox.SelectedIndex = 0;
            RecurrenceComboBox.SelectedIndex = 0;

            // Temporary users.
            // Later these come from the database / Entra ID.

            EmployeeComboBox.Items.Add("Kevin Verweij");
            EmployeeComboBox.Items.Add("John Smith");
            EmployeeComboBox.Items.Add("Lisa Johnson");

            EmployeeComboBox.SelectedIndex = 0;
        }

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            Close();
        }

        private void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (EmployeeComboBox.SelectedItem == null)
            {
                MessageBox.Show(
                    "Please select an employee.");

                return;
            }

            if (!EntryDatePicker.SelectedDate.HasValue)
            {
                MessageBox.Show(
                    "Please select a date.");

                return;
            }

            // Database save will be added later.

            DialogResult = true;

            Close();
        }
    }
}