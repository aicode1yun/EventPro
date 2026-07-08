using Ticket.ViewModels;
using ZXing.Net.Maui;
using Microsoft.Maui.ApplicationModel;

namespace Ticket.Views
{
    public partial class ScannerPage : ContentPage
    {
        private readonly ScannerViewModel _viewModel;
        private bool _animating;

        public ScannerPage(ScannerViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        private async void OnBarcodesDetected(object? sender, BarcodeDetectionEventArgs e)
        {
            if (e.Results?.FirstOrDefault()?.Value is string value)
            {
                await _viewModel.OnQrDetected(value);
            }
        }

        private void OnTorchClicked(object? sender, EventArgs e)
        {
            BarcodeReader.IsTorchOn = !BarcodeReader.IsTorchOn;
            _viewModel.IsTorchOn = BarcodeReader.IsTorchOn;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            if (!ZXing.Net.Maui.BarcodeScanning.IsSupported)
            {
                await DisplayAlertAsync("Camera Unavailable", "This device does not have a camera available for scanning.", "OK");
                TorchButton.IsVisible = false;
                return;
            }

            try
            {
                var status = await Permissions.CheckStatusAsync<Permissions.Camera>();
                if (status != PermissionStatus.Granted)
                {
                    status = await Permissions.RequestAsync<Permissions.Camera>();
                }

                if (status == PermissionStatus.Granted)
                {
                    await Dispatcher.DispatchAsync(() => _viewModel.IsScanning = true);
                }
                else
                {
                    TorchButton.IsVisible = false;
                    await DisplayAlertAsync("Permission Required", "Camera permission is required to scan tickets. Please enable it in Settings.", "OK");
                }
            }
            catch (Exception ex)
            {
                _viewModel.IsScanning = false;
                TorchButton.IsVisible = false;
                await DisplayAlertAsync("Camera Error", $"Unable to start camera: {ex.Message}", "OK");
            }

            AnimateScanLine();
        }

        protected override async void OnDisappearing()
        {
            base.OnDisappearing();
            _animating = false;
            await Dispatcher.DispatchAsync(() => _viewModel.IsScanning = false);
            if (BarcodeReader.IsTorchOn)
            {
                BarcodeReader.IsTorchOn = false;
                _viewModel.IsTorchOn = false;
            }
        }

        private async void AnimateScanLine()
        {
            _animating = true;
            var frameHeight = 260;
            while (_animating)
            {
                await ScanLineContainer.TranslateToAsync(0, frameHeight, 1200, Easing.SinInOut);
                await ScanLineContainer.TranslateToAsync(0, 0, 1200, Easing.SinInOut);
            }
        }
    }
}
