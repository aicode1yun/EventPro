using SkiaSharp;
using Ticket.Models;

namespace Ticket.Services
{
    public interface ITicketImageService
    {
        Task<Stream> GenerateTicketImageAsync(Attendee attendee);
        Task<string> SaveTicketImageAsync(Attendee attendee);
    }

    public class TicketImageService : ITicketImageService
    {
        private readonly IQrCodeService _qrService;
        private readonly ISupabaseClient _supabase;
        private static readonly HttpClient _photoClient = new() { Timeout = TimeSpan.FromSeconds(10) };

        public TicketImageService(IQrCodeService qrService, ISupabaseClient supabase)
        {
            _qrService = qrService;
            _supabase = supabase;
        }

        public async Task<Stream> GenerateTicketImageAsync(Attendee attendee)
        {
            var evt = await _supabase.GetEventAsync();
            var eventName = evt?.EventName ?? "EventPro";
            var location = evt?.Description ?? string.Empty;

            const float s = 1.8f;
            int w = (int)(800 * s), h = (int)(1400 * s);
            var info = new SKImageInfo(w, h);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            var white = SKColors.White;
            var w80 = new SKColor(0xFF, 0xFF, 0xFF, 0xCC);
            var w50 = new SKColor(0xFF, 0xFF, 0xFF, 0x80);

            using var bgShader = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(0, h),
                new SKColor[] { new SKColor(0x0F, 0x17, 0x2A), new SKColor(0x16, 0x19, 0x38) },
                SKShaderTileMode.Clamp);
            canvas.DrawRect(0, 0, w, h, new SKPaint { Shader = bgShader });

            float cx = w / 2f;
            float pad = 52 * s;
            float ml = pad;
            float mr = w - pad;

            var eventFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                52 * s);
            var nameFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                44 * s);
            using var locFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                18 * s);
            using var pillFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                18 * s);
            using var scanFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                18 * s);
            using var tidLabelFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                11 * s);
            using var tidValFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                18 * s);
            using var footerFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                14 * s);
            using var initFont = new SKFont(
                SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                100 * s);

            var stroke = new SKPaint
            {
                Color = white,
                IsAntialias = false,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 3 * s
            };

            var fillWhite = new SKPaint { Color = white, IsAntialias = false };

            float borderInset = 14 * s;
            canvas.DrawRect(borderInset, borderInset, w - borderInset * 2, h - borderInset * 2, stroke);

            float y = 140 * s;
            float maxW = mr - ml;

            // ================================================================
            // EVENT NAME
            // ================================================================
            var evtStr = eventName.ToUpper();
            float evtW = eventFont.MeasureText(evtStr, new SKPaint());
            if (evtW > maxW)
            {
                float fs = 52 * s * (maxW / evtW) * 0.95f;
                eventFont.Dispose();
                eventFont = new SKFont(
                    SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                    fs);
                evtW = eventFont.MeasureText(evtStr, new SKPaint());
            }
            canvas.DrawText(evtStr, cx - evtW / 2f, y + eventFont.Size * 0.85f, eventFont, fillWhite);
            y += eventFont.Size * 0.85f + 20 * s;

            // ================================================================
            // LOCATION
            // ================================================================
            if (!string.IsNullOrEmpty(location))
            {
                var locW = locFont.MeasureText(location, new SKPaint());
                canvas.DrawText(location, cx - locW / 2f, y + locFont.Size * 0.75f, locFont, new SKPaint { Color = w80, IsAntialias = false });
                y += locFont.Size * 0.75f + 32 * s;
            }

            // ================================================================
            // DIVIDER 1
            // ================================================================
            canvas.DrawLine(ml, y, mr, y, stroke);
            y += 36 * s;

            // ================================================================
            // ATTENDEE PHOTO — circular, larger
            // ================================================================
            var photoBytes = await GetPhotoBytesAsync(attendee);
            float avSize = 300 * s;
            float avCY = y + avSize / 2f;

            if (photoBytes is not null)
            {
                using var src = SKBitmap.Decode(photoBytes);
                if (src is not null)
                {
                    using var circ = new SKBitmap((int)avSize, (int)avSize);
                    using var cc = new SKCanvas(circ);
                    cc.Clear(SKColors.Transparent);
                    float ss = Math.Min(src.Width, src.Height);
                    float sx = (src.Width - ss) / 2f;
                    float sy = (src.Height - ss) / 2f;
                    var cp = new SKPath();
                    cp.AddCircle(avSize / 2f, avSize / 2f, avSize / 2f);
                    cc.ClipPath(cp, SKClipOperation.Intersect, true);
                    cc.DrawBitmap(src, new SKRect(-sx * avSize / ss, -sy * avSize / ss,
                        (src.Width - sx) * avSize / ss, (src.Height - sy) * avSize / ss));
                    canvas.DrawBitmap(circ, cx - avSize / 2f, avCY - avSize / 2f);
                }
            }
            else
            {
                canvas.DrawCircle(cx, avCY, avSize / 2f, new SKPaint { Color = new SKColor(0xFF, 0xFF, 0xFF, 0x20), IsAntialias = false });
                canvas.DrawCircle(cx, avCY, avSize / 2f, stroke);
                var init = !string.IsNullOrEmpty(attendee.FullName) ? attendee.FullName[..1].ToUpper() : "?";
                var iw = initFont.MeasureText(init, new SKPaint());
                canvas.DrawText(init, cx - iw / 2f, avCY + 14 * s, initFont, fillWhite);
            }

            y += avSize + 30 * s;

            // ================================================================
            // ATTENDEE NAME — large bold uppercase
            // ================================================================
            var an = attendee.FullName.ToUpper();
            float anW = nameFont.MeasureText(an, new SKPaint());
            if (anW > maxW)
            {
                float fs = 44 * s * (maxW / anW) * 0.95f;
                nameFont.Dispose();
                nameFont = new SKFont(
                    SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright),
                    fs);
                anW = nameFont.MeasureText(an, new SKPaint());
            }
            canvas.DrawText(an, cx - anW / 2f, y + nameFont.Size * 0.85f, nameFont, fillWhite);
            y += nameFont.Size * 0.85f + 24 * s;

            // ================================================================
            // TICKET TYPE PILL
            // ================================================================
            var tt = attendee.TicketType.ToUpper();
            if (string.IsNullOrEmpty(tt)) tt = "GENERAL";
            float tW = pillFont.MeasureText(tt, new SKPaint());
            float pillH = 34 * s;
            float pillPadX = 20 * s;
            float pillW = tW + pillPadX * 2;
            float pY = y;
            var pillRect = new SKRect(cx - pillW / 2f, pY, cx + pillW / 2f, pY + pillH);
            canvas.DrawRect(pillRect, fillWhite);
            canvas.DrawRect(pillRect, stroke);
            canvas.DrawText(tt, cx - tW / 2f, pY + pillH / 2f + pillFont.Size * 0.35f, pillFont, new SKPaint { Color = new SKColor(0x0F, 0x17, 0x2A), IsAntialias = false });
            y += pillH + 48 * s;

            // ================================================================
            // QR CODE — large, thick black border
            // ================================================================
            var qrBytes = _qrService.GenerateQrCode(attendee);
            using var qrBmp = SKBitmap.Decode(qrBytes);
            int qrS = (int)(280 * s);
            float qPad = 18 * s;
            float qCW = qrS + qPad * 2;
            float qCardH = qrS + qPad * 2 + 32 * s;
            float qCX = cx - qCW / 2f;
            float qCY = y;

            var qrCardRect = new SKRect(qCX, qCY, qCX + qCW, qCY + qCardH);
            canvas.DrawRect(qrCardRect, fillWhite);
            canvas.DrawRect(qrCardRect, stroke);

            float qX = cx - qrS / 2f;
            float qY = qCY + qPad;
            canvas.DrawBitmap(qrBmp, new SKRect(qX, qY, qX + qrS, qY + qrS));

            var scanTxt = "SCAN FOR ENTRY";
            float sW = scanFont.MeasureText(scanTxt, new SKPaint());
            canvas.DrawText(scanTxt, cx - sW / 2f, qY + qrS + 28 * s, scanFont, fillWhite);

            y = qCY + qCardH + 48 * s;

            // ================================================================
            // DIVIDER 2
            // ================================================================
            canvas.DrawLine(ml, y, mr, y, stroke);
            y += 32 * s;

            // ================================================================
            // TICKET ID
            // ================================================================
            canvas.DrawText("TICKET ID", cx - tidLabelFont.MeasureText("TICKET ID", new SKPaint()) / 2f,
                y + tidLabelFont.Size * 0.7f, tidLabelFont, new SKPaint { Color = w50, IsAntialias = false });
            y += 24 * s;

            var tid = $"TKT-{attendee.TicketCode}";
            float tidW = tidValFont.MeasureText(tid, new SKPaint());
            canvas.DrawText(tid, cx - tidW / 2f, y + tidValFont.Size * 0.75f, tidValFont,
                new SKPaint { Color = w80, IsAntialias = false });
            y += 40 * s;

            // ================================================================
            // DIVIDER 3
            // ================================================================
            canvas.DrawLine(ml, y, mr, y, stroke);
            y += 28 * s;

            // ================================================================
            // FOOTER
            // ================================================================
            var brand = "EVENTPRO BY EDDY GRAPHIX";
            float bW = footerFont.MeasureText(brand, new SKPaint());
            canvas.DrawText(brand, cx - bW / 2f, y + footerFont.Size * 0.7f, footerFont,
                new SKPaint { Color = w50, IsAntialias = false });

            eventFont.Dispose();
            nameFont.Dispose();

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            return new MemoryStream(data.ToArray());
        }

        private static async Task<byte[]?> GetPhotoBytesAsync(Attendee attendee)
        {
            if (string.IsNullOrEmpty(attendee.PhotoUrl))
                return null;

            var cacheDir = Path.Combine(FileSystem.CacheDirectory, "photos");
            Directory.CreateDirectory(cacheDir);
            var cachePath = Path.Combine(cacheDir, $"attendee_{attendee.Id}.jpg");

            if (File.Exists(cachePath))
                return await File.ReadAllBytesAsync(cachePath);

            try
            {
                var bytes = await _photoClient.GetByteArrayAsync(attendee.PhotoUrl);
                await File.WriteAllBytesAsync(cachePath, bytes);
                return bytes;
            }
            catch
            {
                return null;
            }
        }

        public async Task<string> SaveTicketImageAsync(Attendee attendee)
        {
            var stream = await GenerateTicketImageAsync(attendee);
            var fileName = $"ticket_{attendee.FullName}.png";
            var filePath = Path.Combine(FileSystem.CacheDirectory, fileName);

            using var fileStream = File.OpenWrite(filePath);
            await stream.CopyToAsync(fileStream);

            return filePath;
        }
    }
}
