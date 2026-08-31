using Calendar.Data;
using Calendar.Localization;
using Calendar.Models;
using Calendar.Services;
using Microsoft.EntityFrameworkCore;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Calendar
{
    public partial class LocalAdminLoginWindow : Window
    {
        private bool _isPasswordVisible;

        public string Username =>
            UsernameTextBox.Text.Trim();

        public string Password =>
            _isPasswordVisible
                ? VisiblePasswordTextBox.Text
                : PasswordTextBox.Password;

        public string? AuthenticatedAdminDisplayName
        {
            get;
            private set;
        }


        public LocalAdminLoginWindow()
        {
            InitializeComponent();

            PasswordTextBox.PasswordChanged +=
                PasswordTextBox_PasswordChanged;

            Loaded += (_, _) =>
            {
                UsernameTextBox.Focus();
                UpdatePlaceholders();
            };
        }


        // =========================================================
        // USERNAME
        // =========================================================

        private void UsernameTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            UpdatePlaceholders();
        }


        // =========================================================
        // PASSWORD
        // =========================================================

        private void PasswordTextBox_PasswordChanged(
            object sender,
            RoutedEventArgs e)
        {
            UpdatePlaceholders();
        }


        private void VisiblePasswordTextBox_TextChanged(
            object sender,
            TextChangedEventArgs e)
        {
            UpdatePlaceholders();
        }


        private void ShowPasswordButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            if (_isPasswordVisible)
            {
                PasswordTextBox.Password =
                    VisiblePasswordTextBox.Text;

                VisiblePasswordTextBox.Visibility =
                    Visibility.Collapsed;

                PasswordTextBox.Visibility =
                    Visibility.Visible;

                ShowPasswordButton.Content = LocalizationService.Get("Show");

                _isPasswordVisible = false;

                PasswordTextBox.Focus();
            }
            else
            {
                VisiblePasswordTextBox.Text =
                    PasswordTextBox.Password;

                PasswordTextBox.Visibility =
                    Visibility.Collapsed;

                VisiblePasswordTextBox.Visibility =
                    Visibility.Visible;

                ShowPasswordButton.Content = LocalizationService.Get("Hide");

                _isPasswordVisible = true;

                VisiblePasswordTextBox.Focus();

                VisiblePasswordTextBox.CaretIndex =
                    VisiblePasswordTextBox.Text.Length;
            }

            UpdatePlaceholders();
        }


        // =========================================================
        // PLACEHOLDERS
        // =========================================================

        private void UpdatePlaceholders()
        {
            UsernamePlaceholderText.Visibility =
                string.IsNullOrWhiteSpace(
                    UsernameTextBox.Text)

                    ? Visibility.Visible
                    : Visibility.Collapsed;


            string currentPassword =
                _isPasswordVisible
                    ? VisiblePasswordTextBox.Text
                    : PasswordTextBox.Password;


            PasswordPlaceholderText.Visibility =
                string.IsNullOrWhiteSpace(
                    currentPassword)

                    ? Visibility.Visible
                    : Visibility.Collapsed;
        }


        // =========================================================
        // ENTER
        // =========================================================

        private async void PasswordTextBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await TrySubmitAsync();
            }
        }


        private async void VisiblePasswordTextBox_KeyDown(
            object sender,
            KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await TrySubmitAsync();
            }
        }


        // =========================================================
        // SIGN IN
        // =========================================================

        private async void SignInButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            await TrySubmitAsync();
        }


        private async Task TrySubmitAsync()
        {
            ErrorText.Visibility =
                Visibility.Collapsed;


            bool usernameMissing =
                string.IsNullOrWhiteSpace(Username);

            bool passwordMissing =
                string.IsNullOrWhiteSpace(Password);


            // BOTH EMPTY

            if (usernameMissing &&
                passwordMissing)
            {
                ShowError(LocalizationService.Get("Erroruserpass"));

                UsernameTextBox.Focus();

                return;
            }


            // USERNAME EMPTY

            if (usernameMissing)
            {
                ShowError(LocalizationService.Get("Erroruser"));

                UsernameTextBox.Focus();

                return;
            }


            // PASSWORD EMPTY

            if (passwordMissing)
            {
                ShowError(LocalizationService.Get("Errorpass"));

                FocusPassword();

                return;
            }


            SignInButton.IsEnabled = false;


            try
            {
                using CentralCalendarDbContext database =
                    new CentralCalendarDbContext();


                LocalAdministrator? administrator =
                    await database.LocalAdministrators
                        .SingleOrDefaultAsync(
                            admin =>
                                admin.Username == Username &&
                                admin.IsEnabled);


                // Unknown username

                if (administrator == null)
                {
                    ShowInvalidLogin();

                    return;
                }


                bool passwordCorrect =
                    PasswordSecurity.VerifyPassword(
                        Password,
                        administrator.PasswordHash,
                        administrator.PasswordSalt,
                        administrator.PasswordIterations);


                // Incorrect password

                if (!passwordCorrect)
                {
                    ShowInvalidLogin();

                    return;
                }


                // Successful login

                administrator.LastLoginAt =
                    DateTime.UtcNow;

                await database.SaveChangesAsync();


                AuthenticatedAdminDisplayName =
                    string.IsNullOrWhiteSpace(
                        administrator.DisplayName)

                        ? administrator.Username
                        : administrator.DisplayName;


                DialogResult = true;
            }
            catch (Exception)
            {
                ShowError(LocalizationService.Get("DBerror"));
            }
            finally
            {
                SignInButton.IsEnabled = true;
            }
        }


        // =========================================================
        // INVALID LOGIN
        // =========================================================

        private void ShowInvalidLogin()
        {
            /*
             * We deliberately do not tell someone whether
             * the username exists or whether only the password
             * was incorrect.
             */

            ShowError(LocalizationService.Get("Incoruserpass"));

            PasswordTextBox.Clear();

            VisiblePasswordTextBox.Clear();

            _isPasswordVisible = false;

            VisiblePasswordTextBox.Visibility =
                Visibility.Collapsed;

            PasswordTextBox.Visibility =
                Visibility.Visible;

            ShowPasswordButton.Content = LocalizationService.Get("Show");

            UpdatePlaceholders();

            FocusPassword();
        }


        private void FocusPassword()
        {
            if (_isPasswordVisible)
            {
                VisiblePasswordTextBox.Focus();
            }
            else
            {
                PasswordTextBox.Focus();
            }
        }


        // =========================================================
        // ERROR
        // =========================================================

        private void ShowError(
            string message)
        {
            ErrorText.Text = message;

            ErrorText.Visibility =
                Visibility.Visible;
        }


        // =========================================================
        // CANCEL
        // =========================================================

        private void CancelButton_Click(
            object sender,
            RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}