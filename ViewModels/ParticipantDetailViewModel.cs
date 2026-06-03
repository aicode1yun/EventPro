using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.ApplicationModel;
using Ticket.Models;
using Ticket.Services;

namespace Ticket.ViewModels
{
    [QueryProperty(nameof(AttendeeId), "attendeeId")]
    public partial class ParticipantDetailViewModel : BaseViewModel
    {
        private readonly ISupabaseClient _supabase;
        private readonly IQrCodeService _qrService;
        private readonly ITicketImageService _ticketImageService;
        private readonly IMediaService _mediaService;

        [ObservableProperty]
        private int _attendeeId;

        [ObservableProperty]
        private Attendee? _attendee;

        [ObservableProperty]
        private ImageSource? _qrCodeImage;

        [ObservableProperty]
        private ImageSource? _attendeePhoto;

        [ObservableProperty]
        private bool _isEditing;

        [ObservableProperty]
        private string _editFullName = string.Empty;

        [ObservableProperty]
        private string _editPhoneNumber = string.Empty;

        [ObservableProperty]
        private string _editTicketType = string.Empty;

        [ObservableProperty]
        private string _editNotes = string.Empty;

        public ParticipantDetailViewModel(ISupabaseClient supabase, IQrCodeService qrService, ITicketImageService ticketImageService, IMediaService mediaService)
        {
            _supabase = supabase;
            _qrService = qrService;
            _ticketImageService = ticketImageService;
            _mediaService = mediaService;
            Title = "Participant";
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
                    var qrBytes = _qrService.GenerateQrCode(Attendee);
                    QrCodeImage = ImageSource.FromStream(() => new MemoryStream(qrBytes));

                    if (!string.IsNullOrEmpty(Attendee.PhotoUrl))
                        AttendeePhoto = ImageSource.FromUri(new Uri(Attendee.PhotoUrl));

                    EditFullName = Attendee.FullName;
                    EditPhoneNumber = Attendee.PhoneNumber;
                    EditTicketType = Attendee.TicketType;
                    EditNotes = Attendee.Notes ?? string.Empty;
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        [RelayCommand]
        private void ToggleEdit()
        {
            IsEditing = !IsEditing;
            if (!IsEditing && Attendee is not null)
            {
                EditFullName = Attendee.FullName;
                EditPhoneNumber = Attendee.PhoneNumber;
                EditTicketType = Attendee.TicketType;
                EditNotes = Attendee.Notes ?? string.Empty;
            }
        }

        [RelayCommand]
        private async Task SaveEditAsync()
        {
            if (Attendee is null) return;

            Attendee.FullName = EditFullName.Trim();
            Attendee.PhoneNumber = EditPhoneNumber.Trim();
            Attendee.TicketType = EditTicketType;
            Attendee.Notes = EditNotes?.Trim();

            await _supabase.SaveAttendeeAsync(Attendee);
            IsEditing = false;
            await PopupHelper.ShowSuccessToastAsync("Participant info updated.");
        }

        [RelayCommand]
        private async Task RegenerateTicketAsync()
        {
            if (Attendee is null) return;

            Attendee.QrToken = Helpers.TicketCodeGenerator.GenerateToken();
            await _supabase.SaveAttendeeAsync(Attendee);

            var qrBytes = _qrService.GenerateQrCode(Attendee);
            QrCodeImage = ImageSource.FromStream(() => new MemoryStream(qrBytes));

            await PopupHelper.ShowSuccessToastAsync("Ticket QR code has been regenerated.");
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
                    await PopupHelper.ShowSuccessToastAsync("Ticket saved to gallery.");
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

        [RelayCommand]
        private async Task DeleteAttendeeAsync()
        {
            if (Attendee is null) return;

            var confirm = await PopupHelper.ShowConfirmAsync("Delete", $"Delete {Attendee.FullName}? This cannot be undone.", "Delete", "Cancel");
            if (!confirm) return;

            IsBusy = true;
            try
            {
                await _supabase.DeleteAttendeeAsync(Attendee);
                await Shell.Current.GoToAsync("..");
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
    }
}
