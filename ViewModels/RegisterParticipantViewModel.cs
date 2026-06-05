using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Ticket.Helpers;
using Ticket.Models;
using Ticket.Services;

namespace Ticket.ViewModels
{
    [QueryProperty(nameof(ScannedTicketCode), "ticketCode")]
    public partial class RegisterParticipantViewModel : BaseViewModel
    {
        private readonly ISupabaseClient _supabase;
        private readonly IQrCodeService _qrService;

        [ObservableProperty]
        private string _fullName = string.Empty;

        [ObservableProperty]
        private string _phoneNumber = string.Empty;

        [ObservableProperty]
        private string _ticketType = "General";

        [ObservableProperty]
        private string _paymentStatus = "Pending";

        [ObservableProperty]
        private string _notes = string.Empty;

        [ObservableProperty]
        private string _scannedTicketCode = string.Empty;

        [ObservableProperty]
        private ImageSource? _photoPreview;

        private string? _photoPath;

        public List<string> TicketTypes { get; } = new() { "General", "VIP", "Premium", "Early Bird", "Student" };
        public List<string> PaymentStatuses { get; } = new() { "Pending", "Paid", "Free" };

        public RegisterParticipantViewModel(ISupabaseClient supabase, IQrCodeService qrService)
        {
            _supabase = supabase;
            _qrService = qrService;
            Title = "Register";
        }

        [RelayCommand]
        private void ResetForm()
        {
            FullName = string.Empty;
            PhoneNumber = string.Empty;
            TicketType = "General";
            PaymentStatus = "Pending";
            Notes = string.Empty;
            ScannedTicketCode = string.Empty;
            PhotoPreview = null;
            _photoPath = null;
        }

        [RelayCommand]
        private async Task TakePhotoAsync()
        {
            try
            {
                if (!MediaPicker.IsCaptureSupported)
                {
                    await PopupHelper.ShowWarningToastAsync("Camera not available on this device.");
                    return;
                }

                var cameraStatus = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (cameraStatus != PermissionStatus.Granted)
                {
                    cameraStatus = await Permissions.RequestAsync<Permissions.Camera>();
                    if (cameraStatus != PermissionStatus.Granted)
                    {
                        await PopupHelper.ShowWarningToastAsync("Camera permission is required to take photos.");
                        return;
                    }
                }

                var photo = await MediaPicker.CapturePhotoAsync();
                if (photo is null) return;

                _photoPath = photo.FullPath;
                PhotoPreview = ImageSource.FromFile(photo.FullPath);
            }
            catch (Exception ex)
            {
                await PopupHelper.ShowErrorToastAsync(ex.Message);
            }
        }

        [RelayCommand]
        private async Task PickPhotoAsync()
        {
            try
            {
#pragma warning disable CS0618
                var photo = await MediaPicker.PickPhotoAsync();
#pragma warning restore CS0618
                if (photo is null) return;

                _photoPath = photo.FullPath;
                PhotoPreview = ImageSource.FromFile(photo.FullPath);
            }
            catch (Exception ex)
            {
                await PopupHelper.ShowErrorToastAsync(ex.Message);
            }
        }

        [RelayCommand]
        private void RemovePhoto()
        {
            PhotoPreview = null;
            _photoPath = null;
        }

        [RelayCommand]
        private async Task SaveAsync()
        {
            if (string.IsNullOrWhiteSpace(FullName))
            {
                await PopupHelper.ShowWarningToastAsync("Full name is required.");
                return;
            }

            if (string.IsNullOrWhiteSpace(PhoneNumber))
            {
                await PopupHelper.ShowWarningToastAsync("Phone number is required.");
                return;
            }

            IsBusy = true;
            try
            {
                var existing = await _supabase.GetAttendeeByPhoneAsync(PhoneNumber.Trim());
                if (existing is not null)
                {
                    await PopupHelper.ShowWarningToastAsync("An attendee with this phone number already exists.");
                    return;
                }

                var attendee = new Attendee
                {
                    FullName = FullName.Trim(),
                    PhoneNumber = PhoneNumber.Trim(),
                    TicketType = TicketType,
                    PaymentStatus = PaymentStatus,
                    Notes = Notes?.Trim(),
                    TicketCode = TicketCodeGenerator.GenerateCode(),
                    QrToken = TicketCodeGenerator.GenerateToken(),
                    RegisteredAt = DateTime.UtcNow
                };

                if (!string.IsNullOrEmpty(_photoPath))
                {
                    var fileName = $"{attendee.TicketCode}_{DateTime.UtcNow:yyyyMMddHHmmss}.jpg";
                    using var stream = File.OpenRead(_photoPath);
                    var photoUrl = await _supabase.UploadPhotoAsync(stream, fileName);
                    attendee.PhotoUrl = photoUrl;
                }

                await _supabase.SaveAttendeeAsync(attendee);

                await Shell.Current.GoToAsync($"ticketPreview?attendeeId={attendee.Id}");

                FullName = string.Empty;
                PhoneNumber = string.Empty;
                TicketType = "General";
                PaymentStatus = "Pending";
                Notes = string.Empty;
                PhotoPreview = null;
                _photoPath = null;
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
    }
}
