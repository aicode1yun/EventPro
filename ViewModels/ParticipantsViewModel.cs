using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticket.Models;
using Ticket.Services;

namespace Ticket.ViewModels
{
    public partial class ParticipantsViewModel : BaseViewModel
    {
        private readonly ISupabaseClient _supabase;

        [ObservableProperty]
        private string _searchText = string.Empty;

        [ObservableProperty]
        private string _selectedFilter = "All";

        public ObservableCollection<Attendee> Attendees { get; } = new();
        public ObservableCollection<FilterChip> FilterChips { get; } = new();

        private List<Attendee> _allAttendees = new();

        public ParticipantsViewModel(ISupabaseClient supabase)
        {
            _supabase = supabase;
            Title = "Participants";
            FilterChips.Add(new FilterChip("All", true));
            FilterChips.Add(new FilterChip("Checked In"));
            FilterChips.Add(new FilterChip("Pending"));
        }

        [RelayCommand]
        private async Task LoadAttendees()
        {
            IsBusy = true;
            try
            {
                _allAttendees = await _supabase.GetAttendeesAsync();
                ApplyFilters();
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void SelectFilter(string filter)
        {
            SelectedFilter = filter;
            foreach (var chip in FilterChips)
                chip.IsSelected = chip.Text == filter;
        }

        partial void OnSelectedFilterChanged(string value)
        {
            ApplyFilters();
        }

        partial void OnSearchTextChanged(string value)
        {
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var filtered = _allAttendees.AsEnumerable();

            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                var lower = SearchText.ToLower();
                filtered = filtered.Where(a =>
                    a.FullName.ToLower().Contains(lower) ||
                    a.TicketCode.ToLower().Contains(lower) ||
                    a.PhoneNumber.Contains(lower));
            }

            filtered = SelectedFilter switch
            {
                "Checked In" => filtered.Where(a => a.IsCheckedIn),
                "Pending" => filtered.Where(a => !a.IsCheckedIn),
                _ => filtered
            };

            Attendees.Clear();
            foreach (var a in filtered)
                Attendees.Add(a);
        }

        [RelayCommand]
        private async Task ViewDetail(Attendee attendee)
        {
            if (attendee == null)
            {
                // Defensive: command may be invoked with a null parameter from the view.
                return;
            }

            await Shell.Current.GoToAsync($"participantDetail?attendeeId={attendee.Id}");
        }

        [RelayCommand]
        private async Task DeleteAttendee(Attendee attendee)
        {
            var confirm = await PopupHelper.ShowConfirmAsync("Delete", $"Delete {attendee.FullName}?", "Yes", "No");
            if (!confirm) return;

            await _supabase.DeleteAttendeeAsync(attendee);
            _allAttendees.Remove(attendee);
            Attendees.Remove(attendee);
        }

        [RelayCommand]
        private async Task RegenerateTicket(Attendee attendee)
        {
            attendee.QrToken = Helpers.TicketCodeGenerator.GenerateToken();
            await _supabase.SaveAttendeeAsync(attendee);
            await PopupHelper.ShowSuccessToastAsync("Ticket QR code has been regenerated.");
        }
    }
}
