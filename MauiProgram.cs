using CommunityToolkit.Maui;
using MauiDevFlow.Agent;
using Microsoft.Extensions.Logging;
using SkiaSharp.Views.Maui.Controls.Hosting;
using Ticket.Data;
using Ticket.Services;
using Ticket.ViewModels;
using Ticket.Views;
using UXDivers.Popups.Maui;
using ZXing.Net.Maui.Controls;

namespace Ticket
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .UseBarcodeReader()
                .UseSkiaSharp()
                .UseUXDiversPopups()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                    fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
                    fonts.AddFont("fa-solid-900.ttf", "FAS");
                    fonts.AddFont("fa-regular-400.ttf", "FAR");
                });

            // Data (local SQLite cache)
            builder.Services.AddSingleton<AppDbContext>();

            // Services
            builder.Services.AddSingleton<ISupabaseClient, SupabaseClient>();
            builder.Services.AddSingleton<IAuthService, AuthService>();
            builder.Services.AddSingleton<IRoleService, RoleService>();
            builder.Services.AddSingleton<IQrCodeService, QrCodeService>();
            builder.Services.AddSingleton<ITicketValidationService, TicketValidationService>();
            builder.Services.AddSingleton<ITicketImageService, TicketImageService>();
            builder.Services.AddSingleton<IMediaService, MediaService>();
            builder.Services.AddSingleton<IPhotoUploadService, PhotoUploadService>();
            builder.Services.AddSingleton<IPhotoCompressionService, PhotoCompressionService>();
            builder.Services.AddSingleton<ITelemetryService, TelemetryService>();

            // ViewModels (transient to avoid stale state)
            builder.Services.AddTransient<LoginViewModel>();
            builder.Services.AddTransient<DashboardViewModel>();
            builder.Services.AddTransient<RegisterParticipantViewModel>();
            builder.Services.AddTransient<TicketPreviewViewModel>();
            builder.Services.AddTransient<ParticipantsViewModel>();
            builder.Services.AddTransient<ParticipantDetailViewModel>();
            builder.Services.AddTransient<ScannerViewModel>();
            builder.Services.AddTransient<SettingsViewModel>();

            // Pages (transient)
            builder.Services.AddTransient<LoginPage>();
            builder.Services.AddTransient<DashboardPage>();
            builder.Services.AddTransient<RegisterParticipantPage>();
            builder.Services.AddTransient<TicketPreviewPage>();
            builder.Services.AddTransient<ParticipantsPage>();
            builder.Services.AddTransient<ParticipantDetailPage>();
            builder.Services.AddTransient<ScannerPage>();
            builder.Services.AddTransient<SettingsPage>();

#if DEBUG
            builder.Logging.AddDebug();
            builder.AddMauiDevFlowAgent();
#endif

            return builder.Build();
        }
    }
}
