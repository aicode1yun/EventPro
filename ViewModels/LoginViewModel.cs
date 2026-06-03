using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticket.Helpers;
using Ticket.Services;

namespace Ticket.ViewModels
{
    public partial class LoginViewModel : BaseViewModel
    {
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _email = string.Empty;

        [ObservableProperty]
        private string _password = string.Empty;

        [ObservableProperty]
        private bool _rememberMe;

        [ObservableProperty]
        private string _errorMessage = string.Empty;

        [ObservableProperty]
        private bool _hasError;

        public LoginViewModel(IAuthService authService)
        {
            _authService = authService;
            Title = "Login";
        }

        public async Task InitializeAsync()
        {
            RememberMe = await SecureStorageHelper.GetRememberMeAsync();

            if (await _authService.IsLoggedInAsync())
            {
                await Shell.Current.GoToAsync("//dashboard");
            }
        }

        [RelayCommand]
        private async Task LoginAsync()
        {
            HasError = false;
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Email))
            {
                ShowError("Email is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(Password))
            {
                ShowError("Password is required.");
                return;
            }

            IsBusy = true;
            try
            {
                var success = await _authService.LoginAsync(Email.Trim(), Password, RememberMe);
                if (success)
                {
                    await Shell.Current.GoToAsync("//dashboard");
                }
                else
                {
                    ShowError("Invalid email or password.");
                }
            }
            catch (Exception ex)
            {
                ShowError(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private void ShowError(string message)
        {
            ErrorMessage = message;
            HasError = true;
        }
    }
}
