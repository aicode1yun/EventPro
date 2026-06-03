using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticket.Models;
using Ticket.Services;

namespace Ticket.ViewModels
{
    public partial class SettingsViewModel : BaseViewModel
    {
        private readonly ISupabaseClient _supabase;
        private readonly IAuthService _authService;

        [ObservableProperty]
        private string _eventName = string.Empty;

        [ObservableProperty]
        private DateTime _eventDate = DateTime.Today;

        [ObservableProperty]
        private string _venue = string.Empty;

        [ObservableProperty]
        private string _description = string.Empty;

        [ObservableProperty]
        private string _appVersion = "1.0.0";

        public SettingsViewModel(ISupabaseClient supabase, IAuthService authService)
        {
            _supabase = supabase;
            _authService = authService;
            Title = "Settings";
        }

        [RelayCommand]
        private async Task LoadSettingsAsync()
        {
            IsBusy = true;
            try
            {
                var evt = await _supabase.GetEventAsync();
                if (evt is not null)
                {
                    EventName = evt.EventName;
                    EventDate = evt.EventDate;
                    Venue = evt.Venue;
                    Description = evt.Description ?? string.Empty;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveEventAsync()
        {
            if (string.IsNullOrWhiteSpace(EventName))
            {
                await PopupHelper.ShowWarningToastAsync("Event name is required.");
                return;
            }

            IsBusy = true;
            try
            {
                var evt = await _supabase.GetEventAsync();
                if (evt is null)
                {
                    evt = new Event();
                }

                evt.EventName = EventName.Trim();
                evt.EventDate = EventDate;
                evt.Venue = Venue.Trim();
                evt.Description = Description?.Trim();

                await _supabase.SaveEventAsync(evt);
                await PopupHelper.ShowSuccessToastAsync("Event settings updated.");
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task LogoutAsync()
        {
            var confirm = await PopupHelper.ShowConfirmAsync("Logout", "Are you sure you want to logout?", "Yes", "No");
            if (!confirm) return;

            await _authService.LogoutAsync();
            await Shell.Current.GoToAsync("//login");
        }
    }
}
