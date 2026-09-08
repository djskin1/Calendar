using Calendar;
using Calendar.Data;
using Calendar.Localization;
using Calendar.Models;
using Calendar.ViewModels;
using Calendar.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using SharpVectors.Converters;
using SharpVectors.Renderers.Wpf;
using System;
using System.IO;
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
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Windows.Input;
using System.Linq.Expressions;

namespace CompanyCalendar
{
    public partial class MainWindow : Window
    {
        private bool _isAuthenticated = false;
        private bool _isLocalAdministrator = false;
        private bool _isEntraGlobalAdministrator = false;
        private byte[]? _brandingLogoData;
        private string? _brandingLogoFileName;
        private string? _brandingLogoContentType;
        private string? _currentUserName;
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

        private void UpdateAccountAccess()
        {
            bool isLocalAdministrator = IsLocalAdministrator();

            // Entra Global Administrator check will be added
            // when Microsoft authentication is implemented.
            bool isAdministrator =
                _isLocalAdministrator ||
                _isEntraGlobalAdministrator;

            AdminButton.Visibility =
                isAdministrator
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            SettingsButton.Visibility =
                _isAuthenticated
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if(!_isAuthenticated)
            {
                LoggedInUserText.Text = LocalizationService.Get("NotLoggedIn");

                LoginTypeText.Text = LocalizationService.Get("localCalendar");

                UserProfileImage.Visibility = Visibility.Collapsed;

                UserProfileIcon.Visibility = Visibility.Visible;

                return;
            }

            LoggedInUserText.Text = _currentUserName ?? "User";

            if(_isLocalAdministrator)
            {
                LoginTypeText.Text = LocalizationService.Get("localAdmin");
            }
            else if (_isEntraGlobalAdministrator)
            {
                LoginTypeText.Text = LocalizationService.Get("entraAdmin");
            }
            else
            {
                LoginTypeText.Text = LocalizationService.Get("regularUser");
            }
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
                        LocalizationService.Get("Connected"),
                        "Calendar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
                else
                {
                    MessageBox.Show(
                        LocalizationService.Get("NotConnected"),
                        "Calendar",
                        MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocalizationService.Get("DBError1"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }

            VersionButton.Content =
                $"Version {VersionService.CurrentVersion}";
        }

        private async Task LoadBrandingEditorAsync()
        {
            try
            {
                ApplicationBranding branding =
                    await BrandingService.GetAsync();

                BrandingCompanyNameTextBox.Text =
                    branding.CompanyName;

                BrandingPrimaryColorTextBox.Text =
                    branding.PrimaryColor;

                BrandingAccentColorTextBox.Text =
                    branding.AccentColor;

                _brandingLogoData =
                    branding.LogoData;

                _brandingLogoFileName =
                    branding.LogoFileName;

                _brandingLogoContentType =
                    branding.LogoContentType;

                BrandingLogoFileNameText.Text =
                    _brandingLogoFileName ?? "Default Logo";

                if (branding.LogoData != null &&
                    branding.LogoData.Length >0)
                {
                    BrandingLogoPreview.Source =
                        CreateLogoImageSource(
                            branding.LogoData,
                            branding.LogoContentType);
                } else
                {
                    BrandingLogoPreview.Source =
                        CreateDefaultLogoImageSource();
                }

                UpdateBrandingColorPreviews();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocalizationService.Get("BrandingLoadError"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadUsersAndAdministratorsAsync()
        {
            try
            {
                AdminUsersDataGrid.ItemsSource =
                    await RoleService.GetUsersAsync();

                LocalAdministratorsDataGrid.ItemsSource =
                    await RoleService.GetLocalAdministratorsAsync();


                List<Role> roles =
                    await RoleService.GetRolesAsync();


                RolesListBox.ItemsSource = roles;

                UserRoleComboBox.ItemsSource = roles;
                AdministratorRoleComboBox.ItemsSource = roles;


                if (roles.Count > 0)
                {
                    RolesListBox.SelectedIndex = 0;

                    UserRoleComboBox.SelectedIndex = 0;
                    AdministratorRoleComboBox.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationService.Get("UsersLoadError")}\n\n{ex.Message}",
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async void RolesListBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (RolesListBox.SelectedItem is not Role role)
            {
                return;
            }

            SelectedRoleNameText.Text =
                role.Name;
            List<Permission> permissions =
                await RoleService.GetPermissionsAsync();
            HashSet<int> selectedPermissionIds =
                await RoleService.GetRolePermissionIdsAsync(
                    role.Id);
            RolePermissionsPanel.Children.Clear();

            foreach (Permission permission in permissions)
            {
                CheckBox checkBox =
                    new()
                    {
                        Content =
                            LocalizationService.Get(
                                permission.NameResourceKey),
                        Tag = permission.Id,
                        IsChecked =
                            selectedPermissionIds.Contains(
                                permission.Id),
                        Margin =
                            new Thickness(0, 4, 0, 4),
                        IsEnabled =
                            !role.IsSystemRole
                    };

                RolePermissionsPanel.Children.Add(checkBox);
            }

            SaveRolePermissionsButton.IsEnabled =
                !role.IsSystemRole;
        }

        private async void AddRoleButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            string name =
                NewRoleNameTextBox.Text.Trim();

            if(string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "RoleNameRequired"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }

            try
            {
                Role newRole =
                    await RoleService.CreateRoleAsync(
                        name,
                        null);

                NewRoleNameTextBox.Clear();

                await LoadUsersAndAdministratorsAsync();

                RolesListBox.SelectedItem =
                    ((IEnumerable<Role>)RolesListBox.ItemsSource)
                        .FirstOrDefault(
                            role => role.Id == newRole.Id);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void SaveRolePermissionsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (RolesListBox.SelectedItem
                is not Role role)
            {
                return;
            }


            List<int> selectedPermissionIds =
                RolePermissionsPanel.Children
                    .OfType<CheckBox>()
                    .Where(checkBox =>
                        checkBox.IsChecked == true)
                    .Select(checkBox =>
                        (int)checkBox.Tag)
                    .ToList();


            try
            {
                await RoleService.SaveRolePermissionsAsync(
                    role.Id,
                    selectedPermissionIds);


                MessageBox.Show(
                    LocalizationService.Get(
                        "PermissionsSaved"),

                    LocalizationService.Get("AppName"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }

        private async void AssignUserRoleButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (AdminUsersDataGrid.SelectedItem
                is not AdminUserRow user)
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "SelectUserFirst"),

                    LocalizationService.Get("AppName"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (UserRoleComboBox.SelectedItem
                is not Role role)
            {
                return;
            }


            await RoleService.AssignRoleToUserAsync(
                user.Id,
                role.Id);


            await LoadUsersAndAdministratorsAsync();
        }

        private void AdminUsersSectionButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ShowUsersAdminSection("Users");
        }


        private void AdminAdministratorsSectionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowUsersAdminSection("Administrators");
        }


        private void AdminRolesSectionButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowUsersAdminSection("Roles");
        }


        private void ShowUsersAdminSection(string section)
        {
            AdminUsersSection.Visibility =
                Visibility.Collapsed;

            AdminAdministratorsSection.Visibility =
                Visibility.Collapsed;

            AdminRolesSection.Visibility =
                Visibility.Collapsed;


            switch (section)
            {
                case "Users":
                    AdminUsersSection.Visibility =
                        Visibility.Visible;
                    break;

                case "Administrators":
                    AdminAdministratorsSection.Visibility =
                        Visibility.Visible;
                    break;

                case "Roles":
                    AdminRolesSection.Visibility =
                        Visibility.Visible;
                    break;
            }
        }

        private async void AssignAdministratorRoleButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (LocalAdministratorsDataGrid.SelectedItem
                is not AdminLocalAdministratorRow administrator)
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "SelectAdministratorFirst"),

                    LocalizationService.Get("AppName"),

                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (AdministratorRoleComboBox.SelectedItem
                is not Role role)
            {
                return;
            }


            await RoleService.AssignRoleToAdministratorAsync(
                administrator.Id,
                role.Id);


            await LoadUsersAndAdministratorsAsync();
        }

        public MainWindow()
        {
            InitializeComponent();

            Loaded += Mainwindow_Loaded;

            Title = $"{LocalizationService.Get("AppName")} " +
                    $"{VersionService.CurrentVersion}";

            SettingsVersionText.Text = VersionService.CurrentVersion;

            LanguageComboBox.ItemsSource = LocalizationService.Languages;

            LanguageComboBox.SelectedValue = LocalizationService.CurrentLanguagesCode;

            // Start at the Monday of the current week.
            _startDate = StartOfWeek(DateTime.Today);

            CreateTestUsers();

            SearchResultsList.ItemsSource =
                _searchResults;

            _searchTimer.Interval =
                TimeSpan.FromMilliseconds(350);

            _searchTimer.Tick +=
                SearchTimer_Tick;

            AppearanceComboBox.SelectedIndex = 0;

            LoadCalendar();

        }

        //language //

        private void LanguageComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (LanguageComboBox.SelectedValue is not string languageCode)
            {
                return;
            }

            LocalizationService.SetLanguage(languageCode);
        }

        private void AppearanceComboBox_SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            if (AppearanceComboBox.SelectedItem
                is not ComboBoxItem selectedItem)
            {
                return;
            }

            if (selectedItem.Tag
                is not string theme)
            {
                return;
            }

            ThemeService.SetTheme(theme);
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

        // ============================================================
        // Admin Panel
        // ============================================================

        private void ShowAdminHome()
        {
            AdminDetailPanel.Visibility =
                 Visibility.Collapsed;

            AdminHomePanel.Visibility =
                Visibility.Visible;
        }

        private void ShowAdminDetail(
                string titleKey,
                string descriptionKey,
                string icon)
        {
            AdminHomePanel.Visibility =
                Visibility.Collapsed;

            AdminDetailPanel.Visibility =
                Visibility.Visible;

            AdminDetailTitle.Text =
                LocalizationService.Get(titleKey);

            AdminDetailDescription.Text =
                LocalizationService.Get(descriptionKey);

            AdminDetailHeading.Text =
                LocalizationService.Get(titleKey);

            AdminDetailIcon.Text =
                icon;

            AdminDetailMessage.Text =
                LocalizationService.Get(
                    "AdminComingSoon");
        }

        // admin buttons //

        private void AdminBackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ShowAdminHome();
        }

        private async void AdminBrandingButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            await LoadBrandingEditorAsync();

            AdminPage.Visibility = Visibility.Collapsed;
            AdminBrandingPage.Visibility = Visibility.Visible;
        }

        private void AdminBrandingBackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AdminBrandingPage.Visibility = Visibility.Collapsed;
            AdminPage.Visibility = Visibility.Visible;
        }

        private void BrandingColorTextBox_TextChanged(
    object sender,
    System.Windows.Controls.TextChangedEventArgs e)
        {
            if (BrandingPrimaryColorTextBox == null ||
                BrandingAccentColorTextBox == null)
            {
                return;
            }

            UpdateBrandingColorPreviews();
        }

        private void BrandingChooseLogoButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            OpenFileDialog dialog = new()
            {
                Title = LocalizationService.Get("ChooseLogo"),

                Filter =
                "Supported Image Files (*.jpg;*.jpeg;*.png;*.svg)|*.jpg;*.jpeg;*.png;*.svg|" +
                "PNG files |*.png" +
                "JPEG files |*.jpg;*.jpeg|" +
                "SVG files |*.svg|",

                CheckFileExists = true,
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != true)
            {
                return;
            }

            try
            {
                byte[] data =
                    File.ReadAllBytes(dialog.FileName);

                string fileName =
                    Path.GetFileName(dialog.FileName);

                _brandingLogoData = data;
                _brandingLogoFileName = fileName;
                _brandingLogoContentType =
                    GetLogoContentType(fileName);

                BrandingLogoPreview.Source =
                    CreateLogoImageSource(
                        _brandingLogoData,
                        _brandingLogoContentType);

                BrandingLogoFileNameText.Text =
                    fileName;

            } catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Get("LogoLoadError") + "\n\n" + ex.Message,
                    "Central calendar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error                    );
            }
        }

        private void BrandingDefaultLogoButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _brandingLogoData = null;
            _brandingLogoFileName = null;
            _brandingLogoContentType = null;
            BrandingLogoPreview.Source =
                CreateDefaultLogoImageSource();
            BrandingLogoFileNameText.Text =
                LocalizationService.Get("DefaultLogo");
        }

        private void BrandingPrimaryColorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ColorPickerWindow colorPicker =
                new(
                    BrandingPrimaryColorTextBox.Text)
                {
                    Owner = this
                };

            if (colorPicker.ShowDialog() == true)
            {
                BrandingPrimaryColorTextBox.Text =
                    colorPicker.SelectedColorHex;
            }
        }

        private void BrandingAccentColorButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            ColorPickerWindow colorPicker =
                new(
                    BrandingAccentColorTextBox.Text)
                {
                    Owner = this
                };
            if (colorPicker.ShowDialog() == true)
            {
                BrandingAccentColorTextBox.Text =
                    colorPicker.SelectedColorHex;
            }
        }

        private async void AdminUsersButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadUsersAndAdministratorsAsync();

            AdminPage.Visibility = Visibility.Collapsed;
            AdminUserPage.Visibility = Visibility.Visible;
        }

        private void AdminUsersBackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AdminUserPage.Visibility = Visibility.Collapsed;
            AdminPage.Visibility = Visibility.Visible;
        }

        private void AdminGroupsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ShowAdminDetail(
                "AdminGroups",
                "AdminGroupsDescription",
                "\uE902");
        }

        private void AdminStatusesButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ShowAdminDetail(
                "AdminStatuses",
                "AdminStatusesDescription",
                "\uE8FB");
        }

        private void AdminHolidaysButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ShowAdminDetail(
                "AdminHolidays",
                "AdminHolidaysDescription",
                "\uE787");
        }

        private void AdminEventsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ShowAdminDetail(
                "AdminEvents",
                "AdminEventsDescription",
                "\uECA5");
        }

        private void AdminEntraButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ShowAdminDetail(
                "AdminEntra",
                "AdminEntraDescription",
                "\uE77B");
        }

        private void AdminBackupButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ShowAdminDetail(
                "AdminBackup",
                "AdminBackupDescription",
                "\uE74E");
        }

        private void AdminUpdatesButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ShowAdminDetail(
                "AdminUpdates",
                "AdminUpdatesDescription",
                "\uE895");
        }

        // ============================================================
        // CALENDAR DAYS
        // ===========================================================

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

        private void HideAllPages()
        {
            CalendarPage.Visibility = Visibility.Collapsed;
            HelpPage.Visibility = Visibility.Collapsed;
            SearchPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Collapsed;
            AdminPage.Visibility = Visibility.Collapsed;
        }

        private void CalendarButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideAllPages();
            CalendarPage.Visibility = Visibility.Visible;
            LoadCalendar();
        }

        private void AdminButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideAllPages();
            AdminPage.Visibility = Visibility.Visible;
            ShowAdminHome();
        }

        private void SettingsButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideAllPages();
            SettingsPage.Visibility = Visibility.Visible;
        }

        private void SearchButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideAllPages();
            SearchPage.Visibility = Visibility.Visible;
        }

        private void HelpButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HideAllPages();
            HelpPage.Visibility = Visibility.Visible;
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
            bool entraConfigured = false;

            if (entraConfigured)
            {
                MessageBox.Show(
                    "Microsoft sign-in will be available here.",
                    "Account",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);

                return;
            }

            LocalAdminLoginWindow loginWindow = new()
            {
                Owner = this
            };

            bool? result = loginWindow.ShowDialog();

            // Niet succesvol ingelogd / Cancel
            if (result != true)
            {
                return;
            }

            // SUCCESVOL INGelogd
            _isAuthenticated = true;
            _isLocalAdministrator = true;
            _isEntraGlobalAdministrator = false;

            _currentUserName =
                loginWindow.AuthenticatedAdminDisplayName
                ?? loginWindow.Username;

            // Jouw bestaande methode gebruiken
            UpdateAdminAccess();
        }

        private void UpdateAdminAccess()
        {
            bool isAdministrator =
                _isLocalAdministrator ||
                _isEntraGlobalAdministrator;

            // Admin alleen zichtbaar voor administrators
            AdminButton.Visibility =
                isAdministrator
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            // Voor onze huidige test:
            // Settings pas zichtbaar nadat iemand is ingelogd
            SettingsButton.Visibility =
                _isAuthenticated
                    ? Visibility.Visible
                    : Visibility.Collapsed;

            if (!_isAuthenticated)
            {
                LoggedInUserText.Text =
                    LocalizationService.Get("NotSignedIn");

                LoginTypeText.Text =
                    LocalizationService.Get("Localcalendar");

                return;
            }

            LoggedInUserText.Text =
                _currentUserName ?? "Local Administrator";

            if (_isLocalAdministrator)
            {
                LoginTypeText.Text = "Local Administrator";
            }
            else if (_isEntraGlobalAdministrator)
            {
                LoginTypeText.Text = "Microsoft Entra ID";
            }
        }

        private static ImageSource CreateLogoImageSource(
                byte[] data,
                string? fileName)
        {
            string extension =
                Path.GetExtension(fileName ?? "")
                    .ToLowerInvariant();


            if (extension == ".svg")
            {
                return CreateSvgImageSource(data);
            }


            using MemoryStream stream =
                new(data);

            BitmapImage bitmap =
                new();

            bitmap.BeginInit();

            bitmap.CacheOption =
                BitmapCacheOption.OnLoad;

            bitmap.StreamSource =
                stream;

            bitmap.EndInit();

            bitmap.Freeze();

            return bitmap;
        }

        private static ImageSource CreateSvgImageSource(
    byte[] data)
        {
            string temporaryFile =
                Path.Combine(
                    Path.GetTempPath(),
                    $"CentralCalendar_{Guid.NewGuid():N}.svg");


            try
            {
                File.WriteAllBytes(
                    temporaryFile,
                    data);


                WpfDrawingSettings settings =
                    new();


                FileSvgReader reader =
                    new(settings);


                DrawingGroup drawing =
                    reader.Read(temporaryFile);


                DrawingImage image =
                    new(drawing);

                image.Freeze();

                return image;
            }
            finally
            {
                if (File.Exists(temporaryFile))
                {
                    try
                    {
                        File.Delete(temporaryFile);
                    }
                    catch
                    {
                        // Temporary file cleanup is non-critical.
                    }
                }
            }
        }

        private static ImageSource CreateDefaultLogoImageSource()
        {
            BitmapImage bitmap =
                new(
                    new Uri(
                        "pack://application:,,,/Images/Logo.png",
                        UriKind.Absolute));

            bitmap.Freeze();

            return bitmap;
        }

        private static string GetLogoContentType(
    string fileName)
        {
            return Path.GetExtension(fileName)
                .ToLowerInvariant() switch
            {
                ".png" => "image/png",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                ".svg" => "image/svg+xml",

                _ => "application/octet-stream"
            };
        }

        private void UpdateBrandingColorPreviews()
        {
            if (TryParseColor(
                BrandingPrimaryColorTextBox.Text,
                out Color primaryColor))
            {
                BrandingPrimaryColorPreview.Background =
                    new SolidColorBrush(primaryColor);
            }


            if (TryParseColor(
                BrandingAccentColorTextBox.Text,
                out Color accentColor))
            {
                BrandingAccentColorPreview.Background =
                    new SolidColorBrush(accentColor);
            }
        }

        private static bool TryParseColor(
    string value,
    out Color color)
        {
            color = Colors.Transparent;

            try
            {
                object? converted =
                    ColorConverter.ConvertFromString(
                        value.Trim());

                if (converted is Color result)
                {
                    color = result;
                    return true;
                }
            }
            catch
            {
            }

            return false;
        }

        private void BrandingRestoreDefaultsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            BrandingCompanyNameTextBox.Text = "Central calendar";

            BrandingPrimaryColorTextBox.Text = "#0B856D";
            BrandingAccentColorTextBox.Text = "#0097A7";

            // Gebruik dezelfde functionaliteit als de bestaande
            // "Use default logo"-knop.
            BrandingDefaultLogoButton_Click(sender, e);

            UpdateBrandingColorPreviews();
        }

        private async void BrandingSaveButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            string companyName =
                BrandingCompanyNameTextBox.Text.Trim();

            string primaryColor =
                BrandingPrimaryColorTextBox.Text.Trim();

            string accentColor =
                BrandingAccentColorTextBox.Text.Trim();


            if (string.IsNullOrWhiteSpace(companyName))
            {
                MessageBox.Show(
                    LocalizationService.Get("CompanyNameRequired"),
                    LocalizationService.Get("CentralCalendar"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (!TryParseColor(
                primaryColor,
                out _))
            {
                MessageBox.Show(
                    LocalizationService.Get("PrimaryColorInvalid"),
                    LocalizationService.Get("CentralCalendar"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (!TryParseColor(
                accentColor,
                out _))
            {
                MessageBox.Show(
                    LocalizationService.Get("AccentColorInvalid"),
                    LocalizationService.Get("CentralCalendar"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            try
            {
                ApplicationBranding branding =
                    new()
                    {
                        Id = 1,

                        CompanyName = companyName,

                        LogoData = _brandingLogoData,
                        LogoFileName = _brandingLogoFileName,
                        LogoContentType = _brandingLogoContentType,

                        PrimaryColor = primaryColor,
                        AccentColor = accentColor
                    };


                await BrandingService.SaveAsync(
                    branding);


                ApplyBranding(
                    branding);


                MessageBox.Show(
                    LocalizationService.Get("BrandingSaved"),
                    LocalizationService.Get("CentralCalendar"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show(
                    LocalizationService.Get("BrandingSaveFailed"),
                    LocalizationService.Get("CentralCalendar"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ApplyBranding(
    ApplicationBranding branding)
        {
            if (TryParseColor(
                branding.PrimaryColor,
                out Color primaryColor))
            {
                Application.Current.Resources["SidebarBrush"] =
                    new SolidColorBrush(primaryColor);
            }


            if (TryParseColor(
                branding.AccentColor,
                out Color accentColor))
            {
                Application.Current.Resources["AccentBrush"] =
                    new SolidColorBrush(accentColor);
            }


            if (branding.LogoData != null &&
                branding.LogoData.Length > 0)
            {
                CompanyLogoImage.Source =
                    CreateLogoImageSource(
                        branding.LogoData,
                        branding.LogoFileName);
            }
            else
            {
                CompanyLogoImage.Source =
                    CreateDefaultLogoImageSource();
            }


            Title =
                $"{branding.CompanyName} {VersionService.CurrentVersion}";
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

        #if DEBUG
            private async void Mainwindow_Loaded(
                object sender,
                RoutedEventArgs e)
            {
                try
                {
                    await DevelopmentAdminSeeder.EnsureTestAdminAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        $"Unable to create the development administrator.\n\n{ex.Message}",
                        "Central calendar",
                         MessageBoxButton.OK,
                        MessageBoxImage.Error);
                }
            }
        #endif
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