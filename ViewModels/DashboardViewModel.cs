using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticket.Services;

namespace Ticket.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly ISupabaseClient _supabase;

        [ObservableProperty]
        private int _totalParticipants;

        [ObservableProperty]
        private int _checkedInCount;

        [ObservableProperty]
        private int _remainingCount;

        public DashboardViewModel(ISupabaseClient supabase)
        {
            _supabase = supabase;
            Title = "Dashboard";
        }

        [RelayCommand]
        private async Task LoadStatsAsync()
        {
            IsBusy = true;
            try
            {
                TotalParticipants = await _supabase.GetTotalAttendeesAsync();
                CheckedInCount = await _supabase.GetCheckedInCountAsync();
                RemainingCount = TotalParticipants - CheckedInCount;
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task GoToRegisterAsync()
        {
            await Shell.Current.GoToAsync("//register");
        }

        [RelayCommand]
        private async Task GoToScanAsync()
        {
            await Shell.Current.GoToAsync("//scan");
        }

        [RelayCommand]
        private async Task GoToParticipantsAsync()
        {
            await Shell.Current.GoToAsync("//participants");
        }
    }
}
