using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticket.Models;
using Ticket.Services;

namespace Ticket.ViewModels
{
    public partial class DashboardViewModel : BaseViewModel
    {
        private readonly ISupabaseClient _supabase;
        private readonly IAuthService _authService;
        private readonly IRoleService _roleService;

        [ObservableProperty]
        private int _totalParticipants;

        [ObservableProperty]
        private int _checkedInCount;

        [ObservableProperty]
        private int _remainingCount;

        [ObservableProperty]
        private bool _isAdmin;

        public DashboardViewModel(ISupabaseClient supabase, IAuthService authService, IRoleService roleService)
        {
            _supabase = supabase;
            _authService = authService;
            _roleService = roleService;
            Title = "Dashboard";
        }

        [RelayCommand]
        private async Task LoadStatsAsync()
        {
            IsBusy = true;
            try
            {
                // Check user role and set visibility flag
                var userRole = await _authService.GetCurrentUserRoleAsync();
                IsAdmin = userRole.HasValue && _roleService.IsAdmin(userRole.Value);

                // Batch API call for dashboard stats (reduces latency vs separate calls)
                var (total, checkedIn) = await _supabase.GetDashboardStatsAsync();
                TotalParticipants = total;
                CheckedInCount = checkedIn;
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

        [RelayCommand]
        private async Task GoToSettingsAsync()
        {
            await Shell.Current.GoToAsync("//settings");
        }
    }
}
