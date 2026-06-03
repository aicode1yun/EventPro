using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Ticket.Models;
using Ticket.Services;

namespace Ticket.ViewModels
{
    [QueryProperty(nameof(AttendeeId), "attendeeId")]
    public partial class TicketPreviewViewModel : BaseViewModel
    {
        private readonly ISupabaseClient _supabase;
        private readonly ITicketImageService _ticketImageService;
        private readonly IMediaService _mediaService;

        [ObservableProperty]
        private int _attendeeId;

        [ObservableProperty]
        private Attendee? _attendee;

        [ObservableProperty]
        private ImageSource? _ticketImage;

        [ObservableProperty]
        private bool _saveDone;

        public TicketPreviewViewModel(ISupabaseClient supabase, ITicketImageService ticketImageService, IMediaService mediaService)
        {
            _supabase = supabase;
            _ticketImageService = ticketImageService;
            _mediaService = mediaService;
            Title = "Ticket";
        }

        partial void OnAttendeeIdChanged(int value)
        {
            _ = LoadAttendeeAsync(value);
        }

        private async Task LoadAttendeeAsync(int id)
        {
            IsBusy = true;
            try
            {
                Attendee = await _supabase.GetAttendeeByIdAsync(id);
                if (Attendee is not null)
                {
                    var stream = await _ticketImageService.GenerateTicketImageAsync(Attendee);
                    TicketImage = ImageSource.FromStream(() => stream);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task ShareTicketAsync()
        {
            if (Attendee is null) return;

            IsBusy = true;
            try
            {
                var filePath = await _ticketImageService.SaveTicketImageAsync(Attendee);
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = $"Ticket - {Attendee.FullName}",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await PopupHelper.ShowErrorToastAsync(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private async Task SaveToGalleryAsync()
        {
            if (Attendee is null) return;

            if (!await CheckGalleryPermissionAsync()) return;

            IsBusy = true;
            try
            {
                var filePath = await _ticketImageService.SaveTicketImageAsync(Attendee);
                var saved = await _mediaService.SaveToGalleryAsync(filePath, $"ticket_{Attendee.FullName}.png");
                if (saved)
                {
                    SaveDone = true;
                    await Task.Delay(2000);
                    SaveDone = false;
                }
            }
            catch (Exception ex)
            {
                await PopupHelper.ShowErrorToastAsync(ex.Message);
            }
            finally
            {
                IsBusy = false;
            }
        }

        private static async Task<bool> CheckGalleryPermissionAsync()
        {
#if ANDROID
            if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            {
                var status = await Permissions.CheckStatusAsync<Permissions.StorageWrite>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.StorageWrite>();
                    if (status != PermissionStatus.Granted)
                    {
                        await PopupHelper.ShowWarningToastAsync("Storage permission is required to save images to the gallery.");
                        return false;
                    }
                }
            }
#endif
            return true;
        }

        [RelayCommand]
        private async Task DoneAsync()
        {
            await Shell.Current.GoToAsync("..");
        }
    }
}
