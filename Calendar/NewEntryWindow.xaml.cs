using Calendar.Data;
using Calendar.Localization;
using Calendar.Models;
using Calendar.Services;
using Calendar.ViewModels;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace CompanyCalendar
{
    public partial class NewEntryWindow : Window
    {
        public NewEntryWindow()
        {
            InitializeComponent();

            EntryDatePicker.SelectedDate = DateTime.Today;

            RecurrenceComboBox.SelectedIndex = 0;

            Loaded += NewEntryWindow_Loaded;
        }


        private async void NewEntryWindow_Loaded(
            object sender,
            RoutedEventArgs e)
        {
            try
            {
                using CentralCalendarDbContext database = new();

                List<CalendarStatus> statuses =
     await database.CalendarStatuses
         .AsNoTracking()
         .Where(status =>
             status.IsActive &&
             status.IsSelectable)
         .OrderBy(status => status.SortOrder)
         .ToListAsync();


                List<CalendarStatusOption> statusOptions =
                    statuses
                        .Select(status => new CalendarStatusOption
                        {
                            Id = status.Id,

                            Code = status.Code,

                            DisplayName =
                                GetLocalizedStatusName(status),

                            Status = status
                        })
                        .ToList();


                StatusComboBox.ItemsSource =
                    statusOptions;


                if (statusOptions.Count > 0)
                {
                    StatusComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationService.Get("NewEntryLoadError")}\n\n{ex.Message}",
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }


        private static string GetLocalizedStatusName(
    CalendarStatus status)
        {
            if (string.IsNullOrWhiteSpace(
                status.DisplayNameResourceKey))
            {
                return status.DisplayName;
            }


            string translated =
                LocalizationService.Get(
                    status.DisplayNameResourceKey);


            // Als de key niet gevonden wordt:
            // tekst uit database als fallback.
            if (string.IsNullOrWhiteSpace(translated) ||
                translated == status.DisplayNameResourceKey)
            {
                return status.DisplayName;
            }


            return translated;
        }

        private string GetSelectedRecurrence()
        {
            if (RecurrenceComboBox.SelectedItem
                is not ComboBoxItem item)
            {
                return "Once";
            }

            return item.Content?.ToString() switch
            {
                "Daily" => "Daily",
                "Every week" => "Weekly",
                "Every 2 weeks" => "Every2Weeks",
                "Every 3 weeks" => "Every3Weeks",
                "Every 4 weeks" => "Every4Weeks",
                _ => "Once"
            };
        }


        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }


        private async void SaveButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (EmployeeComboBox.SelectedItem
                is not User selectedUser)
            {
                MessageBox.Show(
                    LocalizationService.Get("SelectEmployee"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (EntryDatePicker.SelectedDate
                is not DateTime selectedDate)
            {
                MessageBox.Show(
                    LocalizationService.Get("SelectDate"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (StatusComboBox.SelectedItem
                is not CalendarStatusOption selectedStatusOption)
            {
                MessageBox.Show(
                    LocalizationService.Get("SelectStatus"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            CalendarStatus selectedStatus = selectedStatusOption.Status;

            selectedDate = selectedDate.Date;


            // Een handmatige entry mag niet op zaterdag/zondag beginnen.
            if (selectedDate.DayOfWeek == DayOfWeek.Saturday ||
                selectedDate.DayOfWeek == DayOfWeek.Sunday)
            {
                MessageBox.Show(
                    LocalizationService.Get("CannotCreateOnWeekend"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            try
            {
                using CentralCalendarDbContext database = new();


                bool isPublicHoliday =
                    await database.PublicHolidays
                        .AnyAsync(holiday =>
                            holiday.IsActive &&
                            holiday.Date == selectedDate);


                if (isPublicHoliday)
                {
                    MessageBox.Show(
                        LocalizationService.Get("CannotCreateOnHoliday"),
                        LocalizationService.Get("AppName"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }


                string recurrence =
                    GetSelectedRecurrence();


                CalendarEntry? existing =
                    await database.CalendarEntries
                        .FirstOrDefaultAsync(entry =>
                            entry.UserId == selectedUser.Id &&
                            entry.Date == selectedDate);


                if (existing == null)
                {
                    database.CalendarEntries.Add(
                        new CalendarEntry
                        {
                            UserId = selectedUser.Id,
                            Date = selectedDate,
                            StatusCode = selectedStatus.Code,

                            Recurrence = recurrence,

                            Notes =
                                string.IsNullOrWhiteSpace(NotesTextBox.Text)
                                    ? null
                                    : NotesTextBox.Text.Trim(),

                            CreatedAt = DateTime.UtcNow
                        });
                }
                else
                {
                    existing.StatusCode =
                        selectedStatus.Code;

                    existing.Recurrence =
                        recurrence;

                    existing.Notes =
                        string.IsNullOrWhiteSpace(NotesTextBox.Text)
                            ? null
                            : NotesTextBox.Text.Trim();

                    existing.ModifiedAt =
                        DateTime.UtcNow;
                }


                await database.SaveChangesAsync();

                DialogResult = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationService.Get("NewEntrySaveError")}\n\n{ex.Message}",
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }
}