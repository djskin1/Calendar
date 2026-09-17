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
        private const int CalendarDaysToShow = 14;

        private int? _selectedCompanyEventId;
        private bool _isCreatingCompanyEvent;

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

            if (!_isAuthenticated)
            {
                LoggedInUserText.Text = LocalizationService.Get("NotLoggedIn");

                LoginTypeText.Text = LocalizationService.Get("localCalendar");

                UserProfileImage.Visibility = Visibility.Collapsed;

                UserProfileIcon.Visibility = Visibility.Visible;

                return;
            }

            LoggedInUserText.Text = _currentUserName ?? "User";

            if (_isLocalAdministrator)
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
                    branding.LogoData.Length > 0)
                {
                    BrandingLogoPreview.Source =
                        CreateLogoImageSource(
                            branding.LogoData,
                            branding.LogoContentType);
                }
                else
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

            if (string.IsNullOrWhiteSpace(name))
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

            // CreateTestUsers();

            SearchResultsList.ItemsSource =
                _searchResults;

            _searchTimer.Interval =
                TimeSpan.FromMilliseconds(350);

            _searchTimer.Tick +=
                SearchTimer_Tick;

            AppearanceComboBox.SelectedIndex = 0;

            // LoadCalendar();

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

            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    LocalizationService.Get("LogoLoadError") + "\n\n" + ex.Message,
                    "Central calendar",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
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

        private async void AdminGroupsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            try
            {
                AdminPage.Visibility =
                    Visibility.Collapsed;

                AdminGroupsPage.Visibility =
                    Visibility.Visible;

                await LoadAdminGroupsAsync();
            }
            catch (Exception ex)
            {
                AdminGroupsPage.Visibility =
                    Visibility.Collapsed;

                AdminPage.Visibility =
                    Visibility.Visible;

                string message =
                    $"{ex.GetType().Name}\n\n" +
                    $"{ex.Message}";

                if (ex.InnerException != null)
                {
                    message +=
                        $"\n\nInner exception:\n" +
                        $"{ex.InnerException.Message}";
                }

                MessageBox.Show(
                    message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void AdminGroupsBackButton_Click(object sender, RoutedEventArgs e)
        {
            AdminGroupsPage.Visibility = Visibility.Collapsed;
            AdminPage.Visibility = Visibility.Visible;
        }

        private async Task LoadAdminGroupsAsync()
        {
            using CentralCalendarDbContext database = new();

            List<CalendarGroup> groups =
                await database.calendarGroups
                    .AsNoTracking()
                    .OrderByDescending(group => group.IsActive)
                    .ThenBy(group => group.DisplayName)
                    .ToListAsync();

            AdminGroupsDataGrid.ItemsSource =
                groups;


            EntraConfiguration? configuration =
                await database.entraConfigurations
                    .AsNoTracking()
                    .FirstOrDefaultAsync(
                        item => item.Id == 1);


            if (configuration?.LastGroupSyncAt == null)
            {
                GroupsLastSyncText.Text =
                    LocalizationService.Get("NeverSynced");
            }
            else
            {
                GroupsLastSyncText.Text =
                    $"{LocalizationService.Get("LastSync")}: " +
                    $"{configuration.LastGroupSyncAt.Value.ToLocalTime():g}";
            }
        }

        private async void SaveGroupsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (AdminGroupsDataGrid.ItemsSource
                is not IEnumerable<CalendarGroup> groups)
            {
                return;
            }


            using CentralCalendarDbContext database = new();


            foreach (CalendarGroup row in groups)
            {
                CalendarGroup? databaseGroup =
                    await database.calendarGroups
                        .FirstOrDefaultAsync(
                            group => group.Id == row.Id);


                if (databaseGroup == null)
                {
                    continue;
                }


                databaseGroup.IsVisibleInCalendar =
                    row.IsVisibleInCalendar;

                databaseGroup.ModifiedAt =
                    DateTime.UtcNow;
            }


            await database.SaveChangesAsync();


            MessageBox.Show(
                LocalizationService.Get("GroupsSaved"),
                LocalizationService.Get("AppName"),
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        private async void AdminStatusesButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            try
            {
                await LoadCalendarStatusesAsync();
                AdminPage.Visibility = Visibility.Collapsed;
                AdminStatusesPage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                );
            }
        }

        private void AdminStatusesBackButton_Click(
            object sender, RoutedEventArgs e)
        {
            AdminStatusesPage.Visibility = Visibility.Collapsed;
            AdminPage.Visibility = Visibility.Visible;
        }

        private async Task LoadCalendarStatusesAsync()
        {
            using CentralCalendarDbContext database = new();

            CalendarStatusesDataGrid.ItemsSource =
                await database.CalendarStatuses
                    .AsNoTracking()
                    .OrderBy(status => status.SortOrder)
                    .ThenBy(status => status.Code)
                    .ToListAsync();
        }

        private void CalendarStatusesDataGrid_SelectionChanged(
    object sender,
    SelectionChangedEventArgs e)
        {
            if (CalendarStatusesDataGrid.SelectedItem
                is not CalendarStatus status)
            {
                return;
            }

            StatusCodeTextBox.Text =
                status.Code;

            StatusDisplayNameTextBox.Text =
                status.DisplayName;

            StatusDescriptionTextBox.Text =
                status.Description ?? "";

            StatusBackgroundColorTextBox.Text =
                status.BackgroundColor;

            StatusForegroundColorTextBox.Text =
                status.ForegroundColor;

            StatusActiveCheckBox.IsChecked =
                status.IsActive;

            StatusSelectableCheckBox.IsChecked =
                status.IsSelectable;

            StatusSortOrderTextBox.Text =
                status.SortOrder.ToString();

            // Systeemstatuscode beschermen.
            StatusCodeTextBox.IsReadOnly =
                status.IsSystemStatus;

            UpdateStatusPreview();
        }

        private void StatusBackgroundColorButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ColorPickerWindow picker =
                new(
                    StatusBackgroundColorTextBox.Text)
                {
                    Owner = this
                };

            if (picker.ShowDialog() == true)
            {
                StatusBackgroundColorTextBox.Text =
                    picker.SelectedColorHex;

                UpdateStatusPreview();
            }
        }

        private void StatusForegroundColorButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            ColorPickerWindow picker =
                new(
                    StatusForegroundColorTextBox.Text)
                {
                    Owner = this
                };

            if (picker.ShowDialog() == true)
            {
                StatusForegroundColorTextBox.Text =
                    picker.SelectedColorHex;

                UpdateStatusPreview();
            }
        }

        private void StatusColorTextBox_TextChanged(
    object sender,
    TextChangedEventArgs e)
        {
            UpdateStatusPreview();
        }

        private void UpdateStatusPreview()
        {
            if (StatusPreview == null ||
                StatusPreviewText == null)
            {
                return;
            }

            if (TryParseColor(
                StatusBackgroundColorTextBox.Text,
                out Color backgroundColor))
            {
                StatusPreview.Background =
                    new SolidColorBrush(backgroundColor);
            }

            if (TryParseColor(
                StatusForegroundColorTextBox.Text,
                out Color foregroundColor))
            {
                StatusPreviewText.Foreground =
                    new SolidColorBrush(foregroundColor);
            }

            StatusPreviewText.Text =
                string.IsNullOrWhiteSpace(
                    StatusCodeTextBox.Text)
                    ? "STATUS"
                    : StatusCodeTextBox.Text.Trim();
        }

        private async void SaveStatusButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            if (CalendarStatusesDataGrid.SelectedItem
                is not CalendarStatus selectedStatus)
            {
                return;
            }

            string code =
                StatusCodeTextBox.Text.Trim();

            string name =
                StatusDisplayNameTextBox.Text.Trim();

            string background =
                StatusBackgroundColorTextBox.Text.Trim();

            string foreground =
                StatusForegroundColorTextBox.Text.Trim();


            if (string.IsNullOrWhiteSpace(code) ||
                string.IsNullOrWhiteSpace(name))
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "StatusCodeAndNameRequired"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (!TryParseColor(background, out _) ||
                !TryParseColor(foreground, out _))
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "InvalidStatusColor"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (!int.TryParse(
                StatusSortOrderTextBox.Text,
                out int sortOrder))
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "InvalidSortOrder"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            using CentralCalendarDbContext database = new();

            CalendarStatus? status =
                await database.CalendarStatuses
                    .FirstOrDefaultAsync(
                        item => item.Id ==
                            selectedStatus.Id);

            if (status == null)
            {
                return;
            }


            if (!status.IsSystemStatus)
            {
                status.Code = code;
            }

            status.DisplayName = name;
            status.Description =
                StatusDescriptionTextBox.Text.Trim();

            status.BackgroundColor = background;
            status.ForegroundColor = foreground;

            status.IsActive =
                StatusActiveCheckBox.IsChecked == true;

            status.IsSelectable =
                StatusSelectableCheckBox.IsChecked == true;

            status.SortOrder = sortOrder;
            status.ModifiedAt = DateTime.UtcNow;


            await database.SaveChangesAsync();

            await LoadCalendarStatusesAsync();
        }

        private async void AddStatusButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            using CentralCalendarDbContext database = new();

            int nextSortOrder =
                await database.CalendarStatuses
                    .AnyAsync()
                    ? await database.CalendarStatuses
                        .MaxAsync(status => status.SortOrder) + 10
                    : 10;

            CalendarStatus status =
                new()
                {
                    Code = $"NEW{nextSortOrder}",
                    DisplayName =
                        LocalizationService.Get("NewStatus"),
                    BackgroundColor = "#E5E7EB",
                    ForegroundColor = "#111827",
                    IsActive = true,
                    IsSelectable = true,
                    IsSystemStatus = false,
                    SortOrder = nextSortOrder,
                    CreatedAt = DateTime.UtcNow
                };

            database.CalendarStatuses.Add(status);

            await database.SaveChangesAsync();

            await LoadCalendarStatusesAsync();
        }

        // Public Holiday

        private async void AdminHolidaysButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            try
            {
                await LoadPublicHolidayAdminPageAsync();
                AdminPage.Visibility = Visibility.Collapsed;
                AdminPublicHolidayPage.Visibility = Visibility.Visible;
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private async Task LoadPublicHolidayAdminPageAsync()
        {
            await PublicHolidaySyncService
                .EnsureCountriesAsync();

            using CentralCalendarDbContext database = new();

            List<PublicHolidayCountry> countries =
                await database.PublicHolidayCountries
                .AsNoTracking()
                .OrderBy(country => country.CountryName)
                .ToListAsync();

            HolidayCountriesDataGrid.ItemsSource = countries;
            BlockingCountryComboBox.ItemsSource = countries;

            PublicHolidayCountry? blockingCountry =
                countries.FirstOrDefault(country => country.IsBlockingCountry);

            if (blockingCountry != null)
            {
                BlockingCountryComboBox.SelectedValue =
                    blockingCountry.CountryCode;
            }

            int year =
                DateTime.Today.Year;

            PublicHolidaysDataGrid.ItemsSource =
                await database.PublicHolidays
                .AsNoTracking()
                .Where(holiday =>
                    holiday.Date.Year == year)
                .OrderBy(holiday => holiday.Date)
                .ThenBy(holiday => holiday.CountryCode)
                .ThenBy(holiday => holiday.Name)
                .ToListAsync();
        }

        private void AdminPublicHolidaysBackButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            AdminPublicHolidayPage.Visibility = Visibility.Collapsed;
            AdminPage.Visibility = Visibility.Visible;
        }

        private async void SaveHolidayCountriesButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            HolidayCountriesDataGrid.CommitEdit(DataGridEditingUnit.Cell,
                true);

            HolidayCountriesDataGrid.CommitEdit(DataGridEditingUnit.Row,
                true);

            if (HolidayCountriesDataGrid.ItemsSource
                is not IEnumerable<PublicHolidayCountry> rows)
            {
                return;
            }

            string? blockingCountryCode =
                BlockingCountryComboBox.SelectedValue
                ?.ToString();

            List<PublicHolidayCountry> rowList = rows.ToList();

            if (!string.IsNullOrWhiteSpace(blockingCountryCode))
            {
                PublicHolidayCountry? blocking =
                    rowList.FirstOrDefault(country =>
                        country.CountryCode ==
                        blockingCountryCode);


                if (blocking == null ||
                    !blocking.IsSelected)
                {
                    MessageBox.Show(
                        LocalizationService.Get(
                            "BlockingCountryMustBeSelected"),
                        LocalizationService.Get("AppName"),
                        MessageBoxButton.OK,
                        MessageBoxImage.Warning);

                    return;
                }
            }


            try
            {
                using CentralCalendarDbContext database = new();

                List<PublicHolidayCountry> databaseCountries =
                    await database.PublicHolidayCountries
                        .ToListAsync();


                // Eerst overal blocking uitzetten.
                foreach (PublicHolidayCountry country
                         in databaseCountries)
                {
                    country.IsBlockingCountry = false;
                }

                await database.SaveChangesAsync();


                // Daarna selectie + één blocking country opslaan.
                foreach (PublicHolidayCountry country
                         in databaseCountries)
                {
                    PublicHolidayCountry? row =
                        rowList.FirstOrDefault(item =>
                            item.Id == country.Id);

                    if (row == null)
                    {
                        continue;
                    }


                    country.IsSelected =
                        row.IsSelected;

                    country.IsBlockingCountry =
                        row.IsSelected &&
                        country.CountryCode ==
                        blockingCountryCode;
                }


                await database.SaveChangesAsync();


                await LoadPublicHolidayAdminPageAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

            private async void SyncPublicHolidaysButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor =
                    Cursors.Wait;


                // Eerst de huidige checkbox-selecties opslaan.
                HolidayCountriesDataGrid.CommitEdit(
                    DataGridEditingUnit.Cell,
                    true);

                HolidayCountriesDataGrid.CommitEdit(
                    DataGridEditingUnit.Row,
                    true);


                int count =
                    await PublicHolidaySyncService
                        .SyncSelectedCountriesAsync(
                            DateTime.Today.Year);


                await LoadPublicHolidayAdminPageAsync();


                MessageBox.Show(
                    $"{LocalizationService.Get("PublicHolidaySyncComplete")} {count}",
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationService.Get("PublicHolidaySyncError")}\n\n" +
                    ex.Message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
            finally
            {
                Mouse.OverrideCursor = null;
            }

        }


        // Events 
        private async void AdminEventsButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            try
            {
                await LoadCompanyEventsAsync();

                AdminPage.Visibility = Visibility.Collapsed;
                AdminCompanyEventsPage.Visibility = Visibility.Visible;
            }
            catch(Exception ex)
            {
                MessageBox.Show(
                    ex.Message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error
                    );
            }
        }

        private void AdminCompanyEventsBackButton_Click(Object sender, RoutedEventArgs e)
        {
            AdminCompanyEventsPage.Visibility = Visibility.Collapsed;

            AdminPage.Visibility = Visibility.Visible;
        }

        private async Task LoadCompanyEventsAsync()
        {
            using CentralCalendarDbContext database = new();

            CompanyEventsDataGrid.ItemsSource =
                await database.CompanyEvents
                .AsNoTracking()
                .OrderByDescending(item => item.IsActive)
                .ThenBy(item => item.Date)
                .ThenBy(item => item.Title)
                .ToListAsync();
        }

        private void CompanyEventsDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CompanyEventsDataGrid.SelectedItem is not CompanyEvent companyEvent)
            {
                return;
            }

            _selectedCompanyEventId = companyEvent.Id;

            _isCreatingCompanyEvent = false;

            CompanyEventTitleTextBox.Text = companyEvent.Title;

            CompanyEventDatePicker.SelectedDate = companyEvent.Date;

            CompanyEventDescriptionTextBox.Text = companyEvent.Description ?? "";

            CompanyEventActiveCheckBox.IsChecked = companyEvent.IsActive;

        }

        private void AddCompanyEventButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            CompanyEventsDataGrid.SelectedItem =
                null;

            _selectedCompanyEventId =
                null;

            _isCreatingCompanyEvent =
                true;

            CompanyEventTitleTextBox.Clear();

            CompanyEventDescriptionTextBox.Clear();

            CompanyEventDatePicker.SelectedDate =
                DateTime.Today;

            CompanyEventActiveCheckBox.IsChecked =
                true;

            CompanyEventTitleTextBox.Focus();
        }

        private async void SaveCompanyEventButton_Click(
    object sender,
    RoutedEventArgs e)
        {
            string title =
                CompanyEventTitleTextBox.Text.Trim();

            if (string.IsNullOrWhiteSpace(title))
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "CompanyEventTitleRequired"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            if (CompanyEventDatePicker.SelectedDate
                is not DateTime eventDate)
            {
                MessageBox.Show(
                    LocalizationService.Get(
                        "CompanyEventDateRequired"),
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);

                return;
            }


            try
            {
                using CentralCalendarDbContext database = new();


                if (_isCreatingCompanyEvent ||
                    _selectedCompanyEventId == null)
                {
                    CompanyEvent companyEvent =
                        new()
                        {
                            Title =
                                title,

                            Description =
                                string.IsNullOrWhiteSpace(
                                    CompanyEventDescriptionTextBox.Text)
                                    ? null
                                    : CompanyEventDescriptionTextBox.Text.Trim(),

                            Date =
                                eventDate.Date,

                            IsActive =
                                CompanyEventActiveCheckBox.IsChecked == true
                        };


                    database.CompanyEvents.Add(
                        companyEvent);
                }
                else
                {
                    CompanyEvent? companyEvent =
                        await database.CompanyEvents
                            .FirstOrDefaultAsync(item =>
                                item.Id ==
                                _selectedCompanyEventId.Value);


                    if (companyEvent == null)
                    {
                        return;
                    }


                    companyEvent.Title =
                        title;

                    companyEvent.Description =
                        string.IsNullOrWhiteSpace(
                            CompanyEventDescriptionTextBox.Text)
                            ? null
                            : CompanyEventDescriptionTextBox.Text.Trim();

                    companyEvent.Date =
                        eventDate.Date;

                    companyEvent.IsActive =
                        CompanyEventActiveCheckBox.IsChecked == true;
                }


                await database.SaveChangesAsync();


                _selectedCompanyEventId =
                    null;

                _isCreatingCompanyEvent =
                    false;


                await LoadCompanyEventsAsync();


                ClearCompanyEventEditor();
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    $"{LocalizationService.Get("CompanyEventSaveError")}\n\n" +
                    ex.Message,
                    LocalizationService.Get("AppName"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        private void ClearCompanyEventEditor()
        {
            CompanyEventTitleTextBox.Clear();

            CompanyEventDescriptionTextBox.Clear();

            CompanyEventDatePicker.SelectedDate =
                null;

            CompanyEventActiveCheckBox.IsChecked =
                true;

            CompanyEventsDataGrid.SelectedItem =
                null;
        }

        //entra

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

        private async void PreviousButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _startDate = _startDate.AddDays(-CalendarDaysToShow);

            await LoadCalendarFromDatabaseAsync();
        }

        private async void NextButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            _startDate = _startDate.AddDays(CalendarDaysToShow);

            await LoadCalendarFromDatabaseAsync();
        }

        private async void TodayButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DateTime today = DateTime.Today;
            _startDate = StartOfWeek(today);
            _updatingDatePicker = true;
            GoToDatePicker.SelectedDate = today;
            _updatingDatePicker = false;

            await LoadCalendarFromDatabaseAsync();
        }

        private async void RefreshButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await LoadCalendarFromDatabaseAsync();
        }

        private static DateTime StartOfWeek(DateTime date)
        {
            int difference =
                (7 +
                 (date.DayOfWeek - DayOfWeek.Monday))
                % 7;

            return date.AddDays(-difference).Date;
        }

        private async void GoToDatePicker_SelectedDateChanged(
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
                await LoadCalendarFromDatabaseAsync();
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
                await LoadCalendarFromDatabaseAsync();
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



        private async Task LoadCalendarFromDatabaseAsync()
        {
            DateTime endDate =
                _startDate.AddDays(CalendarDaysToShow);


            using CentralCalendarDbContext database = new();


            // =====================================================
            // USERS
            // =====================================================

            List<User> users =
                await database.Users
                    .AsNoTracking()
                    .Where(user => user.IsActive)
                    .OrderBy(user => user.DisplayName)
                    .ToListAsync();


            // =====================================================
            // CALENDAR ENTRIES
            // =====================================================

            List<CalendarEntry> entries =
                await database.CalendarEntries
                    .AsNoTracking()
                    .Where(entry =>
                        entry.Date >= _startDate &&
                        entry.Date < endDate)
                    .ToListAsync();


            // =====================================================
            // STATUSES
            //
            // Ook inactive statussen laden.
            // Oude entries moeten namelijk zichtbaar blijven.
            // =====================================================

            List<CalendarStatus> statuses =
                await database.CalendarStatuses
                    .AsNoTracking()
                    .ToListAsync();


            Dictionary<string, CalendarStatus> statusByCode =
                statuses.ToDictionary(
                    status => status.Code,
                    StringComparer.OrdinalIgnoreCase);


            // =====================================================
            // PUBLIC HOLIDAY COUNTRIES
            //
            // Meerdere landen mogen zichtbaar zijn.
            // Slechts één land mag blocking zijn.
            // =====================================================

            List<PublicHolidayCountry> selectedHolidayCountries =
                await database.PublicHolidayCountries
                    .AsNoTracking()
                    .Where(country =>
                        country.IsSelected)
                    .OrderBy(country =>
                        country.CountryName)
                    .ToListAsync();


            List<string> selectedCountryCodes =
                selectedHolidayCountries
                    .Select(country =>
                        country.CountryCode)
                    .ToList();


            string? blockingCountryCode =
                selectedHolidayCountries
                    .Where(country =>
                        country.IsBlockingCountry)
                    .Select(country =>
                        country.CountryCode)
                    .FirstOrDefault();


            // =====================================================
            // PUBLIC HOLIDAYS
            //
            // Alleen geselecteerde landen worden weergegeven.
            // =====================================================

            List<PublicHoliday> publicHolidays =
                new();


            if (selectedCountryCodes.Count > 0)
            {
                publicHolidays =
                    await database.PublicHolidays
                        .AsNoTracking()
                        .Where(holiday =>
                            holiday.IsActive &&
                            holiday.Date >= _startDate &&
                            holiday.Date < endDate &&
                            selectedCountryCodes.Contains(
                                holiday.CountryCode))
                        .OrderBy(holiday =>
                            holiday.Date)
                        .ThenBy(holiday =>
                            holiday.CountryCode)
                        .ThenBy(holiday =>
                            holiday.Name)
                        .ToListAsync();
            }


            // =====================================================
            // ALL HOLIDAYS BY DATE
            //
            // Deze worden gebruikt voor de Public Holidays-rij.
            // =====================================================

            Dictionary<DateTime, List<PublicHoliday>> holidaysByDate =
                publicHolidays
                    .GroupBy(holiday =>
                        holiday.Date.Date)
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToList());


            // =====================================================
            // BLOCKING HOLIDAYS BY DATE
            //
            // Alleen officiële feestdagen van het blocking country
            // worden aan employee-cellen als PUB doorgegeven.
            // =====================================================

            Dictionary<DateTime, PublicHoliday> holidayByDate =
                new();


            if (!string.IsNullOrWhiteSpace(
                blockingCountryCode))
            {
                holidayByDate =
                    publicHolidays
                        .Where(holiday =>
                            holiday.IsOfficialPublicHoliday &&
                            holiday.CountryCode.Equals(
                                blockingCountryCode,
                                StringComparison.OrdinalIgnoreCase))
                        .GroupBy(holiday =>
                            holiday.Date.Date)
                        .ToDictionary(
                            group => group.Key,
                            group => group.First());
            }


            // =====================================================
            // COMPANY EVENTS
            //
            // Deze staan los van personeel.
            // =====================================================

            List<CompanyEvent> companyEvents =
                await database.CompanyEvents
                    .AsNoTracking()
                    .Where(companyEvent =>
                        companyEvent.IsActive &&
                        companyEvent.Date >= _startDate &&
                        companyEvent.Date < endDate)
                    .OrderBy(companyEvent =>
                        companyEvent.Date)
                    .ThenBy(companyEvent =>
                        companyEvent.Title)
                    .ToListAsync();


            Dictionary<DateTime, List<CompanyEvent>> eventsByDate =
                companyEvents
                    .GroupBy(companyEvent =>
                        companyEvent.Date.Date)
                    .ToDictionary(
                        group => group.Key,
                        group => group.ToList());


            // =====================================================
            // ENTRY LOOKUP
            // =====================================================

            Dictionary<(int UserId, DateTime Date), CalendarEntry>
                entryByUserAndDate =
                    entries
                        .GroupBy(entry =>
                            (
                                entry.UserId,
                                entry.Date.Date
                            ))
                        .ToDictionary(
                            group => group.Key,
                            group => group
                                .OrderByDescending(
                                    entry =>
                                        entry.ModifiedAt ??
                                        entry.CreatedAt)
                                .First());


            // =====================================================
            // BUILD ROWS
            // =====================================================

            List<CalendarEmployeeRowViewModel> rows =
                new();


            // =====================================================
            // PUBLIC HOLIDAYS ROW
            //
            // Deze rij bestaat altijd, ook als er geen employees zijn.
            // =====================================================

            CalendarEmployeeRowViewModel publicHolidayRow =
                new()
                {
                    UserID = 0,

                    DisplayName =
                        LocalizationService.Get(
                            "PublicHolidays"),

                    GroupName =
                        LocalizationService.Get(
                            "CalendarInformation")
                };


            for (int dayIndex = 0;
                 dayIndex < CalendarDaysToShow;
                 dayIndex++)
            {
                DateTime date =
                    _startDate
                        .AddDays(dayIndex)
                        .Date;


                CalendarDayCellViewModel cell =
                    new()
                    {
                        Date = date
                    };


                if (holidaysByDate.TryGetValue(
                    date,
                    out List<PublicHoliday>? holidays))
                {
                    List<string> holidayNames =
                        new();

                    List<string> holidayToolTips =
                        new();


                    foreach (PublicHoliday holiday in holidays)
                    {
                        holidayNames.Add(
                            $"{holiday.Name} ({holiday.CountryCode})");


                        holidayToolTips.Add(
                            $"{holiday.CountryCode} - {holiday.Name}");
                    }


                    cell.Text =
                        string.Join(
                            Environment.NewLine,
                            holidayNames);


                    cell.ToolTip =
                        string.Join(
                            Environment.NewLine,
                            holidayToolTips);


                    // PUB statuskleur gebruiken als die bestaat.
                    if (statusByCode.TryGetValue(
                        "PUB",
                        out CalendarStatus? pubStatus))
                    {
                        if (TryParseColor(
                            pubStatus.BackgroundColor,
                            out System.Windows.Media.Color backgroundColor))
                        {
                            cell.Background =
                                new System.Windows.Media.SolidColorBrush(
                                    backgroundColor);
                        }


                        if (TryParseColor(
                            pubStatus.ForegroundColor,
                            out System.Windows.Media.Color foregroundColor))
                        {
                            cell.Foreground =
                                new System.Windows.Media.SolidColorBrush(
                                    foregroundColor);
                        }
                    }
                }


                publicHolidayRow.Days.Add(
                    cell);
            }


            rows.Add(
                publicHolidayRow);


            // =====================================================
            // COMPANY EVENTS ROW
            //
            // Ook deze rij bestaat altijd.
            // =====================================================

            CalendarEmployeeRowViewModel companyEventRow =
                new()
                {
                    UserID = 0,

                    DisplayName =
                        LocalizationService.Get(
                            "CompanyEvents"),

                    GroupName =
                        LocalizationService.Get(
                            "CalendarInformation")
                };


            for (int dayIndex = 0;
                 dayIndex < CalendarDaysToShow;
                 dayIndex++)
            {
                DateTime date =
                    _startDate
                        .AddDays(dayIndex)
                        .Date;


                CalendarDayCellViewModel cell =
                    new()
                    {
                        Date = date
                    };


                if (eventsByDate.TryGetValue(
                    date,
                    out List<CompanyEvent>? events))
                {
                    cell.Text =
                        string.Join(
                            Environment.NewLine,
                            events.Select(
                                companyEvent =>
                                    companyEvent.Title));


                    cell.ToolTip =
                        string.Join(
                            Environment.NewLine +
                            Environment.NewLine,
                            events.Select(
                                companyEvent =>
                                    string.IsNullOrWhiteSpace(
                                        companyEvent.Description)

                                        ? companyEvent.Title

                                        : companyEvent.Title +
                                          Environment.NewLine +
                                          companyEvent.Description));
                }


                companyEventRow.Days.Add(
                    cell);
            }


            rows.Add(
                companyEventRow);


            // =====================================================
            // EMPLOYEE ROWS
            // =====================================================

            foreach (User user in users)
            {
                CalendarEmployeeRowViewModel row =
                    new()
                    {
                        UserID =
                            user.Id,

                        DisplayName =
                            user.DisplayName,

                        // Totdat Entra group-membership aan Users
                        // is gekoppeld staat iedereen onder All.
                        GroupName =
                            LocalizationService.Get("All")
                    };


                for (int dayIndex = 0;
                     dayIndex < CalendarDaysToShow;
                     dayIndex++)
                {
                    DateTime date =
                        _startDate
                            .AddDays(dayIndex)
                            .Date;


                    CalendarDayCellViewModel cell =
                        CreateCalendarCell(
                            user.Id,
                            date,
                            entryByUserAndDate,
                            statusByCode,

                            // Alleen holidays van het blocking
                            // country komen hier binnen.
                            holidayByDate);


                    row.Days.Add(
                        cell);
                }


                rows.Add(
                    row);
            }


            // =====================================================
            // DYNAMIC DATE COLUMNS
            //
            // Jouw bestaande methode blijft intact.
            // =====================================================

            BuildCalendarDateColumns();


            // =====================================================
            // GROUPING
            //
            // Resultaat:
            //
            // Calendar information
            //   Public holidays
            //   Company events
            //
            // All
            //   Employees...
            // =====================================================

            ICollectionView view =
                CollectionViewSource.GetDefaultView(
                    rows);


            view.GroupDescriptions.Clear();


            view.GroupDescriptions.Add(
                new PropertyGroupDescription(
                    nameof(
                        CalendarEmployeeRowViewModel.GroupName)));


            CalendarGrid.ItemsSource =
                view;


            // =====================================================
            // PERIOD
            // =====================================================

            DateTime lastDate =
                _startDate
                    .AddDays(CalendarDaysToShow - 1);


            PeriodText.Text =
                $"{_startDate.ToString(
                    "dd MMM yyyy",
                    CultureInfo.CurrentCulture)} - " +
                $"{lastDate.ToString(
                    "dd MMM yyyy",
                    CultureInfo.CurrentCulture)}";
        }

        private void ApplyCalendarDateHeaders(
    Dictionary<DateTime, List<PublicHoliday>> holidaysByDate,
    Dictionary<DateTime, List<CompanyEvent>> eventsByDate,
    string? blockingCountryCode)
        {
            for (int dayIndex = 0;
                 dayIndex < CalendarDaysToShow;
                 dayIndex++)
            {
                // Column 0 is Employee.
                // Daarom begint de eerste datum op column 1.
                int columnIndex =
                    dayIndex + 1;


                if (columnIndex >=
                    CalendarGrid.Columns.Count)
                {
                    break;
                }


                DateTime date =
                    _startDate
                        .AddDays(dayIndex)
                        .Date;


                CalendarGrid.Columns[columnIndex].Header =
                    CreateCalendarDateHeader(
                        date,
                        holidaysByDate,
                        eventsByDate,
                        blockingCountryCode);
            }
        }

        private object CreateCalendarDateHeader(
    DateTime date,
    Dictionary<DateTime, List<PublicHoliday>> holidaysByDate,
    Dictionary<DateTime, List<CompanyEvent>> eventsByDate,
    string? blockingCountryCode)
        {
            StackPanel panel =
                new()
                {
                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    Margin =
                        new Thickness(4)
                };


            // =====================================================
            // DAY
            // =====================================================

            TextBlock dayText =
                new()
                {
                    Text =
                        date.ToString(
                            "ddd",
                            CultureInfo.CurrentCulture),

                    FontWeight =
                        FontWeights.SemiBold,

                    HorizontalAlignment =
                        HorizontalAlignment.Center
                };


            panel.Children.Add(
                dayText);


            // =====================================================
            // DATE
            // =====================================================

            TextBlock dateText =
                new()
                {
                    Text =
                        date.ToString(
                            "dd MMM",
                            CultureInfo.CurrentCulture),

                    HorizontalAlignment =
                        HorizontalAlignment.Center
                };


            panel.Children.Add(
                dateText);


            // =====================================================
            // PUBLIC HOLIDAYS
            // =====================================================

            if (holidaysByDate.TryGetValue(
                date.Date,
                out List<PublicHoliday>? holidays))
            {
                foreach (PublicHoliday holiday in holidays)
                {
                    bool isBlocking =
                        !string.IsNullOrWhiteSpace(
                            blockingCountryCode) &&
                        holiday.CountryCode.Equals(
                            blockingCountryCode,
                            StringComparison.OrdinalIgnoreCase);


                    TextBlock holidayText =
                        new()
                        {
                            Text =
                                $"PUB {holiday.CountryCode}",

                            FontSize =
                                10,

                            FontWeight =
                                isBlocking
                                    ? FontWeights.Bold
                                    : FontWeights.Normal,

                            HorizontalAlignment =
                                HorizontalAlignment.Center,

                            ToolTip =
                                $"{holiday.CountryCode} - {holiday.Name}"
                        };


                    panel.Children.Add(
                        holidayText);
                }
            }


            // =====================================================
            // COMPANY EVENTS
            // =====================================================

            if (eventsByDate.TryGetValue(
                date.Date,
                out List<CompanyEvent>? companyEvents))
            {
                foreach (CompanyEvent companyEvent
                         in companyEvents)
                {
                    string tooltip =
                        companyEvent.Title;


                    if (!string.IsNullOrWhiteSpace(
                        companyEvent.Description))
                    {
                        tooltip +=
                            Environment.NewLine +
                            companyEvent.Description;
                    }


                    TextBlock eventText =
                        new()
                        {
                            Text =
                                companyEvent.Title,

                            FontSize =
                                10,

                            FontWeight =
                                FontWeights.SemiBold,

                            HorizontalAlignment =
                                HorizontalAlignment.Center,

                            TextAlignment =
                                TextAlignment.Center,

                            TextTrimming =
                                TextTrimming.CharacterEllipsis,

                            MaxWidth =
                                120,

                            ToolTip =
                                tooltip
                        };


                    panel.Children.Add(
                        eventText);
                }
            }


            return panel;
        }

        private CalendarDayCellViewModel CreateCalendarCell(
    int userId,
    DateTime date,
    Dictionary<(int UserId, DateTime Date), CalendarEntry>
        entries,
    Dictionary<string, CalendarStatus> statuses,
    Dictionary<DateTime, PublicHoliday> publicHolidays)
        {
            CalendarDayCellViewModel cell =
                new()
                {
                    Date = date
                };


            // =====================================================
            // PUBLIC HOLIDAY
            // =====================================================

            if (publicHolidays.TryGetValue(
                date,
                out PublicHoliday? holiday))
            {
                cell.IsPublicHoliday = true;

                cell.Text = "PUB";
                cell.ToolTip = holiday.Name;

                ApplyCalendarStatusToCell(
                    cell,
                    "PUB",
                    statuses);

                return cell;
            }


            // =====================================================
            // WEEKEND
            // =====================================================

            if (date.DayOfWeek == DayOfWeek.Saturday ||
                date.DayOfWeek == DayOfWeek.Sunday)
            {
                cell.IsWeekend = true;

                cell.Text = "Weekend";

                ApplyCalendarStatusToCell(
                    cell,
                    "Weekend",
                    statuses);

                return cell;
            }


            // =====================================================
            // NORMAL CALENDAR ENTRY
            // =====================================================

            if (!entries.TryGetValue(
                (userId, date),
                out CalendarEntry? entry))
            {
                return cell;
            }


            cell.CalendarEntryId =
                entry.Id;

            cell.Text =
                entry.StatusCode;

            cell.ToolTip =
                string.IsNullOrWhiteSpace(entry.Notes)
                    ? entry.StatusCode
                    : $"{entry.StatusCode}\n{entry.Notes}";


            ApplyCalendarStatusToCell(
                cell,
                entry.StatusCode,
                statuses);


            return cell;
        }

        private void ApplyCalendarStatusToCell(
    CalendarDayCellViewModel cell,
    string statusCode,
    Dictionary<string, CalendarStatus> statuses)
        {
            if (!statuses.TryGetValue(
                statusCode,
                out CalendarStatus? status))
            {
                return;
            }


            if (TryParseColor(
                status.BackgroundColor,
                out Color backgroundColor))
            {
                cell.Background =
                    new SolidColorBrush(
                        backgroundColor);
            }


            if (TryParseColor(
                status.ForegroundColor,
                out Color foregroundColor))
            {
                cell.Foreground =
                    new SolidColorBrush(
                        foregroundColor);
            }


            if (string.IsNullOrWhiteSpace(cell.ToolTip))
            {
                cell.ToolTip =
                    status.DisplayName;
            }
        }

        private void BuildCalendarDateColumns()
        {
            while (CalendarGrid.Columns.Count > 1)
            {
                CalendarGrid.Columns.RemoveAt(
                    CalendarGrid.Columns.Count - 1);
            }


            for (int index = 0;
                 index < CalendarDaysToShow;
                 index++)
            {
                DateTime date =
                    _startDate.AddDays(index);

                CalendarGrid.Columns.Add(
                    CreateCalendarDateColumn(
                        date,
                        index));
            }
        }

        private DataGridTemplateColumn CreateCalendarDateColumn(
    DateTime date,
    int dayIndex)
        {
            DataGridTemplateColumn column =
                new()
                {
                    Width = new DataGridLength(85),

                    Header =
                        CreateCalendarDateHeader(date)
                };


            DataTemplate template =
                new();


            FrameworkElementFactory border =
                new(typeof(Border));


            border.SetBinding(
                Border.BackgroundProperty,
                new Binding(
                    $"Days[{dayIndex}].Background"));


            border.SetValue(
                Border.PaddingProperty,
                new Thickness(4));


            FrameworkElementFactory text =
                new(typeof(TextBlock));


            text.SetBinding(
                TextBlock.TextProperty,
                new Binding(
                    $"Days[{dayIndex}].Text"));


            text.SetBinding(
                TextBlock.ForegroundProperty,
                new Binding(
                    $"Days[{dayIndex}].Foreground"));


            text.SetBinding(
                TextBlock.ToolTipProperty,
                new Binding(
                    $"Days[{dayIndex}].ToolTip"));


            text.SetValue(
                TextBlock.HorizontalAlignmentProperty,
                HorizontalAlignment.Center);


            text.SetValue(
                TextBlock.VerticalAlignmentProperty,
                VerticalAlignment.Center);


            text.SetValue(
                TextBlock.FontWeightProperty,
                FontWeights.SemiBold);


            border.AppendChild(text);

            template.VisualTree =
                border;

            column.CellTemplate =
                template;


            return column;
        }

        private object CreateCalendarDateHeader(
    DateTime date)
        {
            StackPanel panel =
                new()
                {
                    HorizontalAlignment =
                        HorizontalAlignment.Center
                };


            panel.Children.Add(
                new TextBlock
                {
                    Text =
                        date.ToString(
                            "ddd",
                            CultureInfo.CurrentCulture),

                    HorizontalAlignment =
                        HorizontalAlignment.Center,

                    FontWeight =
                        FontWeights.SemiBold
                });


            panel.Children.Add(
                new TextBlock
                {
                    Text =
                        date.ToString(
                            "dd MMM",
                            CultureInfo.CurrentCulture),

                    HorizontalAlignment =
                        HorizontalAlignment.Center
                });


            return panel;
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
}