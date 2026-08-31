using Calendar;
using Calendar.Data;
using Calendar.Localization;
using Calendar.Models;
using Calendar.Services;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Threading;

namespace CompanyCalendar
{
    public partial class MainWindow : Window
    {
        private DateTime _startDate;
        private bool _updatingDatePicker;

        private const int DaysVisible = 17;

        private readonly ObservableCollection<EmployeeCalendarRow> _employees = new();

        private readonly ObservableCollection<SearchResultItem>
    _searchResults = new();

        private readonly DispatcherTimer _searchTimer =
    new DispatcherTimer();

        private bool IsLocalAdministrator()
        {
            using WindowsIdentity identity = WindowsIdentity.GetCurrent();

            SecurityIdentifier administratorsSid =
                new SecurityIdentifier(
                    WellKnownSidType.BuiltinAdministratorsSid,
                    null);

            return identity.Groups?
                .Any(group => group.Equals(administratorsSid))
                ?? false;
        }

        private void UpdateAdminAccess()
        {
            bool isLocalAdministrator = IsLocalAdministrator();

            // Entra Global Administrator check will be added
            // when Microsoft authentication is implemented.
            bool isEntraGlobalAdministrator = false;

            bool canAccessAdmin =
                isLocalAdministrator ||
                isEntraGlobalAdministrator;

            AdminButton.Visibility =
                canAccessAdmin
                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }

        private async Task TestDatabaseConnectionAsync()
        {
            try
            {
                using CentralCalendarDbContext database =
                    new CentralCalendarDbContext();

                bool canConnect =
                    await database.Database.CanConnectAsync();

                if (canConnect)
                {
                    MessageBox.Show(
                        "Connection to CentralCalendar database successful.",
                        "Database",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        "Could not connect to the CentralCalendar database.",
                        "Database",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    "Database error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            VersionButton.Content =
                $"Version {VersionService.CurrentVersion}";
        }

        public MainWindow()
        {
            InitializeComponent();

            Title = $"{LocalizationService.Get("AppName")} " +
                    $"{VersionService.CurrentVersion}";

            // Start at the Monday of the current week.
            _startDate = StartOfWeek(DateTime.Today);

            CreateTestUsers();

            SearchResultsList.ItemsSource =
                _searchResults;

            _searchTimer.Interval =
                TimeSpan.FromMilliseconds(350);

            _searchTimer.Tick +=
                SearchTimer_Tick;

            LoadCalendar();

        }

        // Search //
        private void SearchTextBox_TextChanged(
    object sender,
    TextChangedEventArgs e)
        {
            SearchPlaceholder.Visibility =
                string.IsNullOrWhiteSpace(
                    SearchTextBox.Text)

                    ? Visibility.Visible
                    : Visibility.Collapsed;


            _searchTimer.Stop();


            if (string.IsNullOrWhiteSpace(
                SearchTextBox.Text))
            {
                _searchResults.Clear();

                NoSearchResultsPanel.Visibility =
                    Visibility.Collapsed;

                return;
            }


            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(
            object? sender,
            EventArgs e)
        {
            _searchTimer.Stop();

            await SearchDatabaseAsync(
                SearchTextBox.Text.Trim());
        }

        private async Task SearchDatabaseAsync(
    string searchText)
        {
            _searchResults.Clear();

            if (string.IsNullOrWhiteSpace(searchText))
            {
                NoSearchResultsPanel.Visibility =
                    Visibility.Collapsed;

                return;
            }


            DateTime today =
                DateTime.Today;


            try
            {
                using CentralCalendarDbContext database =
                    new CentralCalendarDbContext();


                // =====================================================
                // EMPLOYEES
                // =====================================================

                var employees =
                    await database.Users
                        .Where(user =>
                            user.IsActive &&
                            user.DisplayName.Contains(searchText))
                        .OrderBy(user =>
                            user.DisplayName)
                        .Take(20)
                        .ToListAsync();


                foreach (User employee in employees)
                {
                    var upcomingEntries =
                        await database.CalendarEntries
                            .Where(entry =>
                                entry.UserId == employee.Id &&
                                entry.Date >= today)
                            .OrderBy(entry =>
                                entry.Date)
                            .Take(5)
                            .ToListAsync();


                    string description;

                    if (upcomingEntries.Count == 0)
                    {
                        description =
                            "No upcoming calendar entries.";
                    }
                    else
                    {
                        description =
                            string.Join(
                                "  •  ",
                                upcomingEntries.Select(
                                    entry =>
                                        $"{entry.Date:dd-MM-yyyy}: {entry.StatusCode}"));
                    }


                    _searchResults.Add(
                        new SearchResultItem
                        {
                            Type = "Employee",
                            Title = employee.DisplayName,
                            Description = description,

                            Icon = "\uE77B",

                            UserId = employee.Id
                        });
                }


                // =====================================================
                // PUBLIC HOLIDAYS
                // ONLY TODAY / FUTURE
                // =====================================================

                var holidays =
                    await database.PublicHolidays
                        .Where(holiday =>
                            holiday.IsActive &&
                            holiday.Date >= today &&
                            holiday.Name.Contains(searchText))
                        .OrderBy(holiday =>
                            holiday.Date)
                        .Take(20)
                        .ToListAsync();


                foreach (PublicHoliday holiday in holidays)
                {
                    _searchResults.Add(
                        new SearchResultItem
                        {
                            Type = "Public Holiday",
                            Title = holiday.Name,

                            Description =
                                "Public holiday",

                            Date = holiday.Date,

                            DateText =
                                holiday.Date.ToString(
                                    "dd MMMM yyyy"),

                            Icon = "\uE787"
                        });
                }


                // =====================================================
                // COMPANY EVENTS
                // ONLY TODAY / FUTURE
                // =====================================================

                var events =
                    await database.CompanyEvents
                        .Where(companyEvent =>
                            companyEvent.IsActive &&
                            companyEvent.Date >= today &&
                            (
                                companyEvent.Title.Contains(searchText) ||
                                (
                                    companyEvent.Description != null &&
                                    companyEvent.Description.Contains(searchText)
                                )
                            ))
                        .OrderBy(companyEvent =>
                            companyEvent.Date)
                        .Take(20)
                        .ToListAsync();


                foreach (CompanyEvent companyEvent in events)
                {
                    _searchResults.Add(
                        new SearchResultItem
                        {
                            Type = "Company Event",

                            Title =
                                companyEvent.Title,

                            Description =
                                companyEvent.Description
                                ?? "Company event",

                            Date =
                                companyEvent.Date,

                            DateText =
                                companyEvent.Date.ToString(
                                    "dd MMMM yyyy"),

                            Icon = "\uECA5"
                        });
                }


                // =====================================================
                // NOTHING FOUND
                // =====================================================

                NoSearchResultsPanel.Visibility =
                    _searchResults.Count == 0

                        ? Visibility.Visible
                        : Visibility.Collapsed;
            }
            catch (Exception)
            {
                _searchResults.Clear();

                NoSearchResultsPanel.Visibility =
                    Visibility.Visible;
            }
        }

        // ============================================================
        // CALENDAR
        // ============================================================

        private void LoadCalendar()
        {
            PeriodText.Text =
                $"{_startDate:dd MMM yyyy} - {_startDate.AddDays(DaysVisible - 1):dd MMM yyyy}";

            CreateDateColumns();
            CreateCalendarDays();

            ICollectionView view =
                CollectionViewSource.GetDefaultView(_employees);

            view.GroupDescriptions.Clear();

            // Later these groups will come from the Admin Panel.
            view.GroupDescriptions.Add(
                new PropertyGroupDescription(nameof(EmployeeCalendarRow.Department)));

            CalendarGrid.ItemsSource = view;
        }

        private void CreateDateColumns()
        {
            // Keep Employee column.
            while (CalendarGrid.Columns.Count > 1)
            {
                CalendarGrid.Columns.RemoveAt(1);
            }

            for (int i = 0; i < DaysVisible; i++)
            {
                DateTime date = _startDate.AddDays(i);

                var column = new DataGridTemplateColumn
                {
                    Header = CreateDateHeader(date),
                    Width = 78
                };

                var template = new DataTemplate();

                var borderFactory =
                    new FrameworkElementFactory(typeof(Border));

                borderFactory.SetValue(
                    Border.PaddingProperty,
                    new Thickness(3));

                borderFactory.SetBinding(
                    Border.BackgroundProperty,
                    new Binding($"Days[{i}].Background"));

                var textFactory =
                    new FrameworkElementFactory(typeof(TextBlock));

                textFactory.SetValue(
                    TextBlock.HorizontalAlignmentProperty,
                    HorizontalAlignment.Center);

                textFactory.SetValue(
                    TextBlock.VerticalAlignmentProperty,
                    VerticalAlignment.Center);

                textFactory.SetValue(
                    TextBlock.FontSizeProperty,
                    11.0);

                textFactory.SetBinding(
                    TextBlock.TextProperty,
                    new Binding($"Days[{i}].Status"));

                borderFactory.AppendChild(textFactory);

                template.VisualTree = borderFactory;

                column.CellTemplate = template;

                CalendarGrid.Columns.Add(column);
            }
        }

        private object CreateDateHeader(DateTime date)
        {
            var stack = new StackPanel
            {
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stack.Children.Add(
                new TextBlock
                {
                    Text = date.ToString(
                        "dd-MMM-yy",
                        CultureInfo.InvariantCulture),

                    FontSize = 11,
                    HorizontalAlignment = HorizontalAlignment.Center
                });

            stack.Children.Add(
                new TextBlock
                {
                    Text = date.ToString(
                        "ddd",
                        CultureInfo.InvariantCulture),

                    FontSize = 10,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    Foreground = Brushes.DimGray
                });

            return stack;
        }

        private void CreateCalendarDays()
        {
            foreach (EmployeeCalendarRow employee in _employees)
            {
                employee.Days.Clear();

                for (int i = 0; i < DaysVisible; i++)
                {
                    DateTime date = _startDate.AddDays(i);

                    bool weekend =
                        date.DayOfWeek == DayOfWeek.Saturday ||
                        date.DayOfWeek == DayOfWeek.Sunday;

                    employee.Days.Add(
                        new CalendarDay
                        {
                            Date = date,
                            IsWeekend = weekend,
                            Status = ""
                        });
                }
            }

            // Temporary demo data.
            // Later this comes from the database.

            if (_employees.Count > 0)
            {
                SetDemoStatus(_employees[0], 0, "OFFICE");
                SetDemoStatus(_employees[0], 3, "HOME");
                SetDemoStatus(_employees[0], 4, "ABSENT");
            }

            if (_employees.Count > 1)
            {
                SetDemoStatus(_employees[1], 0, "HOME");
                SetDemoStatus(_employees[1], 3, "OFFICE");
                SetDemoStatus(_employees[1], 4, "HOLIDAY");
            }
        }

        private void SetDemoStatus(
            EmployeeCalendarRow employee,
            int dayIndex,
            string status)
        {
            if (dayIndex < 0 ||
                dayIndex >= employee.Days.Count)
            {
                return;
            }

            if (employee.Days[dayIndex].IsWeekend)
            {
                return;
            }

            employee.Days[dayIndex].Status = status;
        }

        // ============================================================
        // NAVIGATION
        // ============================================================

        private void PreviousButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _startDate = _startDate.AddDays(-7);

            LoadCalendar();
        }

        private void NextButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _startDate = _startDate.AddDays(7);

            LoadCalendar();
        }

        private void TodayButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DateTime today = DateTime.Today;
            _startDate = StartOfWeek(today);
            _updatingDatePicker = true;
            GoToDatePicker.SelectedDate = today;
            _updatingDatePicker = false;

            LoadCalendar();
        }

        private void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            LoadCalendar();
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            int difference =
                (7 +
                 (date.DayOfWeek - DayOfWeek.Monday))
                % 7;

            return date.AddDays(-difference).Date;
        }

        private void GoToDatePicker_SelectedDateChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (_updatingDatePicker)
            {
                return;
            }
            if (GoToDatePicker.SelectedDate.HasValue)
            {
                DateTime selectedDate = GoToDatePicker.SelectedDate.Value;
                _startDate = StartOfWeek(selectedDate);
                LoadCalendar();
            }
        }

        // ============================================================
        // BUTTONS
        // ============================================================

        private void CalendarButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HelpPage.Visibility = Visibility.Collapsed;
            CalendarPage.Visibility = Visibility.Visible;
            SearchPage.Visibility = Visibility.Collapsed;
            LoadCalendar();
        }

        private void AdminButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            MessageBox.Show(
                "The Admin Panel will be added next.",
                "Admin",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private void SearchButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CalendarPage.Visibility =
                Visibility.Collapsed;

            HelpPage.Visibility =
                Visibility.Collapsed;

            SearchPage.Visibility =
                Visibility.Visible;

            SearchTextBox.Focus();
        }

        private void HelpButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            CalendarPage.Visibility = Visibility.Collapsed;
            HelpPage.Visibility = Visibility.Visible;
            SearchPage.Visibility = Visibility.Collapsed;
        }

        private void VersionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AboutWindow aboutWindow = new()
            {
                Owner = this
            };
            aboutWindow.ShowDialog();
        }

        private void NewEntryButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            NewEntryWindow window = new()
            {
                Owner = this
            };

            window.ShowDialog();
        }

        private void AccountArea_MouseLeftButtonUp(
            object sender,
            System.Windows.Input.MouseButtonEventArgs e)
        {
            ShowAccountMenu();
        }

        private void ShowAccountMenu()
        {
            bool entraConfigured = false; // This will be determined by the actual authentication implementation.
            if(entraConfigured)
            {
                MessageBox.Show(
           "Microsoft sign-in will be available here.",
           "Account",
           MessageBoxButton.OK,
           MessageBoxImage.Information);

            } else
            {
                LocalAdminLoginWindow loginWindow = new()
                {
                    Owner = this
                };

                bool? result = loginWindow.ShowDialog();

                if(result != true)
                {
                    MessageBox.Show(
                        "Login failed or canceled.",
                        "Account",
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);
                }

                string username = loginWindow.Username;
                string password = loginWindow.Password;
            }
        }

        // ============================================================
        // TEMPORARY TEST USERS
        // ============================================================

        private void CreateTestUsers()
        {
            _employees.Add(
                new EmployeeCalendarRow
                {
                    DisplayName = "All",
                    Department = "All"
                });

            _employees.Add(
                new EmployeeCalendarRow
                {
                    DisplayName = "Kevin Verweij",
                    Department = "IT"
                });

            _employees.Add(
                new EmployeeCalendarRow
                {
                    DisplayName = "John Smith",
                    Department = "IT"
                });

            _employees.Add(
                new EmployeeCalendarRow
                {
                    DisplayName = "Lisa Johnson",
                    Department = "HR"
                });

            _employees.Add(
                new EmployeeCalendarRow
                {
                    DisplayName = "Michael Brown",
                    Department = "HR"
                });

            _employees.Add(
                new EmployeeCalendarRow
                {
                    DisplayName = "Emma Wilson",
                    Department = "Finance"
                });
        }
    }

    // ================================================================
    // CALENDAR ROW
    // ================================================================

    public class EmployeeCalendarRow
    {
        public string DisplayName { get; set; } = "";

        public string Department { get; set; } = "";

        public ObservableCollection<CalendarDay> Days { get; }
            = new();
    }

    // ================================================================
    // CALENDAR DAY
    // ================================================================

    public class CalendarDay : INotifyPropertyChanged
    {
        private string _status = "";

        public DateTime Date { get; set; }

        public bool IsWeekend { get; set; }

        public string Status
        {
            get => _status;

            set
            {
                _status = value;

                OnPropertyChanged(nameof(Status));
                OnPropertyChanged(nameof(Background));
            }
        }

        public Brush Background
        {
            get
            {
                if (IsWeekend)
                {
                    return new SolidColorBrush(
                        Color.FromRgb(210, 210, 210));
                }

                return Status switch
                {
                    "OFFICE" =>
                        new SolidColorBrush(
                            Color.FromRgb(219, 234, 254)),

                    "HOME" =>
                        new SolidColorBrush(
                            Color.FromRgb(187, 247, 208)),

                    "ABSENT" =>
                        new SolidColorBrush(
                            Color.FromRgb(254, 202, 202)),

                    "HOLIDAY" =>
                        new SolidColorBrush(
                            Color.FromRgb(254, 240, 138)),

                    _ => Brushes.White
                };
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(
                this,
                new PropertyChangedEventArgs(propertyName));
        }
    }
}