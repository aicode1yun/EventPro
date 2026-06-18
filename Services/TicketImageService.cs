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
            var eventDate = evt?.EventDate ?? DateTime.Today;
            var venue = evt?.Venue ?? string.Empty;
            var location = evt?.Description ?? string.Empty;

            const float s = 1.8f;
            int w = (int)(800 * s), h = (int)(1400 * s);
            var info = new SKImageInfo(w, h);
            using var surface = SKSurface.Create(info);
            var canvas = surface.Canvas;

            var navy = new SKColor(0x0F, 0x17, 0x2A);
            var purple = new SKColor(0x7C, 0x3A, 0xED);
            var blue = new SKColor(0x25, 0x63, 0xEB);
            var teal = new SKColor(0x06, 0xB6, 0xD4);
            var green = new SKColor(0x10, 0xB9, 0x81);
            var white = SKColors.White;
            var w90 = new SKColor(0xFF, 0xFF, 0xFF, 0xE6);
            var w80 = new SKColor(0xFF, 0xFF, 0xFF, 0xCC);
            var w70 = new SKColor(0xFF, 0xFF, 0xFF, 0xB3);
            var w50 = new SKColor(0xFF, 0xFF, 0xFF, 0x80);
            var w30 = new SKColor(0xFF, 0xFF, 0xFF, 0x4D);
            var w15 = new SKColor(0xFF, 0xFF, 0xFF, 0x26);
            var w08 = new SKColor(0xFF, 0xFF, 0xFF, 0x14);
            var w04 = new SKColor(0xFF, 0xFF, 0xFF, 0x0A);
            var cardBg = new SKColor(0xFF, 0xFF, 0xFF, 0x06);

            // Pre-fetch photo bytes so we can use them for both the large photo above title and the avatar
            var largePhotoBytes = await GetPhotoBytesAsync(attendee);

            canvas.DrawRect(0, 0, w, h, new SKPaint { Color = navy });

            var topGlow = new SKPaint
            {
                Color = new SKColor(0x7C, 0x3A, 0xED, 0x15),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 100 * s)
            };
            canvas.DrawCircle(w * 0.5f, 0, 300 * s, topGlow);

            var bottomGlow = new SKPaint
            {
                Color = new SKColor(0x06, 0xB6, 0xD4, 0x08),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 120 * s)
            };
            canvas.DrawCircle(w * 0.5f, h * 0.92f, 250 * s, bottomGlow);

            DrawDotGrid(canvas, w, h, navy, s);

            float cx = w / 2f;
            float pad = 44 * s;
            float ml = pad;
            float mr = w - pad;
            float colW = (mr - ml) / 2f;

            using var titleFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 54 * s);
            using var bodyFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 16 * s);
            using var labelFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 10 * s);
            using var smallFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 12 * s);
            using var badgeFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 18 * s);
            using var subFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 14 * s);

            // ============================================================
            // TICKET BORDER
            // ============================================================
            var borderPaint = new SKPaint
            {
                Color = new SKColor(0x7C, 0x3A, 0xED, 0x20),
                IsAntialias = true,
                Style = SKPaintStyle.Stroke,
                StrokeWidth = 1 * s
            };
            float bp = 12 * s;
            canvas.DrawRoundRect(bp, bp, w - bp * 2, h - bp * 2, 16 * s, 16 * s, borderPaint);

            DrawCornerOrnament(canvas, bp + 10 * s, bp + 10 * s, 20 * s, purple);
            DrawCornerOrnament(canvas, mr - 10 * s, bp + 10 * s, 20 * s, purple);
            DrawCornerOrnament(canvas, bp + 10 * s, h - bp - 30 * s, 20 * s, purple);
            DrawCornerOrnament(canvas, mr - 10 * s, h - bp - 30 * s, 20 * s, purple);

            float y = 140 * s;

            // ============================================================
            // LARGE PHOTO (above title)
            // ============================================================
            if (largePhotoBytes is not null)
            {
                using var largeBitmap = SKBitmap.Decode(largePhotoBytes);
                if (largeBitmap is not null)
                {
                    float photoSize = 160 * s;
                    float photoY = y;

                    var photoGlow = new SKPaint
                    {
                        Color = new SKColor(0x7C, 0x3A, 0xED, 0x18),
                        IsAntialias = true,
                        MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 20 * s)
                    };
                    canvas.DrawRoundRect(cx - photoSize / 2f - 8 * s, photoY - 8 * s, photoSize + 16 * s, photoSize + 16 * s, 20 * s, 20 * s, photoGlow);

                    canvas.DrawRoundRect(cx - photoSize / 2f - 3 * s, photoY - 3 * s, photoSize + 6 * s, photoSize + 6 * s, 18 * s, 18 * s,
                        new SKPaint { Color = white, IsAntialias = true });

                    using var croppedPhoto = new SKBitmap((int)photoSize, (int)photoSize);
                    using var cropCanvas = new SKCanvas(croppedPhoto);
                    cropCanvas.Clear(SKColors.Transparent);

                    var clipPath = new SKPath();
                    clipPath.AddRoundRect(new SKRect(0, 0, photoSize, photoSize), 14 * s, 14 * s);
                    cropCanvas.ClipPath(clipPath, SKClipOperation.Intersect, true);

                    float srcSize = Math.Min(largeBitmap.Width, largeBitmap.Height);
                    float srcX = (largeBitmap.Width - srcSize) / 2f;
                    float srcY = (largeBitmap.Height - srcSize) / 2f;
                    cropCanvas.DrawBitmap(largeBitmap, new SKRect(
                        -srcX * photoSize / srcSize,
                        -srcY * photoSize / srcSize,
                        (largeBitmap.Width - srcX) * photoSize / srcSize,
                        (largeBitmap.Height - srcY) * photoSize / srcSize));

                    canvas.DrawBitmap(croppedPhoto, cx - photoSize / 2f, photoY);

                    y += photoSize + 80 * s;
                }
            }
            else
            {
                y = 174 * s;
            }

            // ============================================================
            // EVENT TITLE
            // ============================================================
            var titleStr = eventName.ToUpper();
            var tW = titleFont.MeasureText(titleStr, new SKPaint());
            var titleGlow = new SKPaint
            {
                Color = new SKColor(0x7C, 0x3A, 0xED, 0x20),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 16 * s)
            };
            canvas.DrawText(titleStr, cx - tW / 2f, y + 3 * s, titleFont, titleGlow);
            canvas.DrawText(titleStr, cx - tW / 2f, y, titleFont, new SKPaint { Color = white, IsAntialias = true });
            y += 48 * s;

            if (!string.IsNullOrEmpty(location))
            {
                var locW = subFont.MeasureText(location, new SKPaint());
                canvas.DrawText(location, cx - locW / 2f, y, subFont, new SKPaint { Color = w50, IsAntialias = true });
                y += 34 * s;
            }

            var sepGrad = SKShader.CreateLinearGradient(
                new SKPoint(ml, 0), new SKPoint(mr, 0),
                new[] { w04, w30, w04 }, new[] { 0f, 0.5f, 1f }, SKShaderTileMode.Clamp);
            canvas.DrawLine(ml, y, mr, y, new SKPaint { Shader = sepGrad, StrokeWidth = 1 * s, IsAntialias = true });
            y += 28 * s;

            // ============================================================
            // DATE & VENUE (centered)
            // ============================================================
            var dateStr = eventDate.ToString("ddd, MMM dd yyyy");
            var dateW = bodyFont.MeasureText(dateStr, new SKPaint());
            canvas.DrawText("DATE", cx - labelFont.MeasureText("DATE", new SKPaint()) / 2f, y, labelFont, new SKPaint { Color = w50, IsAntialias = true });
            canvas.DrawText(dateStr, cx - dateW / 2f, y + 20 * s, bodyFont, new SKPaint { Color = w80, IsAntialias = true });
            y += 50 * s;

            if (!string.IsNullOrEmpty(venue))
            {
                var venueStr = venue;
                var venueW = bodyFont.MeasureText(venueStr, new SKPaint());
                canvas.DrawText("VENUE", cx - labelFont.MeasureText("VENUE", new SKPaint()) / 2f, y, labelFont, new SKPaint { Color = w50, IsAntialias = true });
                canvas.DrawText(venueStr, cx - venueW / 2f, y + 20 * s, bodyFont, new SKPaint { Color = w80, IsAntialias = true });
                y += 50 * s;
            }

            // ============================================================
            // PERFORATED TEAR LINE
            // ============================================================
            float notchY = y;
            for (float nx_ = 0; nx_ <= w; nx_ += 30 * s)
            {
                canvas.DrawCircle(nx_, notchY, 6 * s, new SKPaint { Color = navy, IsAntialias = true });
            }
            canvas.DrawLine(ml, notchY, mr, notchY,
                new SKPaint { Color = w15, StrokeWidth = 1 * s, PathEffect = SKPathEffect.CreateDash(new[] { 8 * s, 8 * s }, 0), IsAntialias = true });

            var notchLabel = "━━ TICKET ━━";
            var nlW = smallFont.MeasureText(notchLabel, new SKPaint());
            canvas.DrawRoundRect(cx - nlW / 2f - 10 * s, notchY - 10 * s, nlW + 20 * s, 20 * s, 10 * s, 10 * s,
                new SKPaint { Color = navy, IsAntialias = true });
            canvas.DrawText(notchLabel, cx - nlW / 2f, notchY + 4 * s, smallFont, new SKPaint { Color = w30, IsAntialias = true });

            y += 32 * s;

            // ============================================================
            // ATTENDEE CARD
            // ============================================================
            float attCardY = y;
            float attCardH = 76 * s;
            canvas.DrawRoundRect(ml, attCardY, mr - ml, attCardH, 10 * s, 10 * s, new SKPaint { Color = cardBg, IsAntialias = true });

            float aY = attCardY + 14 * s;
            float avSize = 48 * s;
            float avCenterX = ml + 24 * s + avSize / 2f;
            float avCenterY = aY + avSize / 2f;

            var avatarGlow = new SKPaint
            {
                Color = new SKColor(0x7C, 0x3A, 0xED, 0x15),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 10 * s)
            };
            canvas.DrawCircle(avCenterX, avCenterY, avSize / 2f + 4 * s, avatarGlow);

            if (largePhotoBytes is not null)
            {
                using var photoBitmap = SKBitmap.Decode(largePhotoBytes);
                if (photoBitmap is not null)
                {
                    using var circularPhoto = new SKBitmap((int)avSize, (int)avSize);
                    using var photoCanvas = new SKCanvas(circularPhoto);
                    photoCanvas.Clear(SKColors.Transparent);

                    float srcSize = Math.Min(photoBitmap.Width, photoBitmap.Height);
                    float srcX = (photoBitmap.Width - srcSize) / 2f;
                    float srcY = (photoBitmap.Height - srcSize) / 2f;

                    var clipPath = new SKPath();
                    clipPath.AddCircle(avSize / 2f, avSize / 2f, avSize / 2f);
                    photoCanvas.ClipPath(clipPath, SKClipOperation.Intersect, true);
                    photoCanvas.DrawBitmap(photoBitmap, new SKRect(
                        -srcX * avSize / srcSize,
                        -srcY * avSize / srcSize,
                        (photoBitmap.Width - srcX) * avSize / srcSize,
                        (photoBitmap.Height - srcY) * avSize / srcSize));

                    canvas.DrawBitmap(circularPhoto, avCenterX - avSize / 2f, avCenterY - avSize / 2f);
                }
            }
            else
            {
                var avatarGrad = SKShader.CreateLinearGradient(
                    new SKPoint(0, 0), new SKPoint(avSize, avSize),
                    new[] { purple, teal }, new[] { 0f, 1f }, SKShaderTileMode.Clamp);
                canvas.DrawCircle(avCenterX, avCenterY, avSize / 2f, new SKPaint { Shader = avatarGrad, IsAntialias = true });

                var initial = !string.IsNullOrEmpty(attendee.FullName) ? attendee.FullName[..1].ToUpper() : "?";
                using var initFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 22 * s);
                var initW = initFont.MeasureText(initial, new SKPaint());
                canvas.DrawText(initial, avCenterX - initW / 2f, avCenterY + 8 * s, initFont, new SKPaint { Color = white, IsAntialias = true });
            }

            float nx = ml + 24 * s + avSize + 20 * s;
            canvas.DrawText("ATTENDEE", nx, aY + 10 * s, labelFont, new SKPaint { Color = w50, IsAntialias = true });

            using var nameFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 22 * s);
            canvas.DrawText(attendee.FullName, nx, aY + 36 * s, nameFont, new SKPaint { Color = w90, IsAntialias = true });

            if (!string.IsNullOrEmpty(attendee.PhoneNumber))
            {
                using var phoneFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 14 * s);
                canvas.DrawText(attendee.PhoneNumber, nx, aY + 54 * s, phoneFont, new SKPaint { Color = w70, IsAntialias = true });
            }

            y = attCardY + attCardH + 20 * s;

            // ============================================================
            // TICKET TYPE + ID — side by side
            // ============================================================
            float halfW = (mr - ml - 16 * s) / 2f;
            float tcY = y;

            var ticketTypeCard = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(1, 0),
                new[] { new SKColor(0x7C, 0x3A, 0xED, 0x25), new SKColor(0x25, 0x63, 0xEB, 0x12) },
                new[] { 0f, 1f }, SKShaderTileMode.Clamp);
            canvas.DrawRoundRect(ml, tcY, halfW, 58 * s, 10 * s, 10 * s, new SKPaint { Shader = ticketTypeCard, IsAntialias = true });
            canvas.DrawRoundRect(ml + 2 * s, tcY + 8 * s, 3 * s, 58 * s - 16 * s, 1.5f * s, 1.5f * s, new SKPaint { Color = purple, IsAntialias = true });

            canvas.DrawText("TICKET TYPE", ml + 18 * s, tcY + 16 * s, labelFont, new SKPaint { Color = w50, IsAntialias = true });
            canvas.DrawText(attendee.TicketType.ToUpper(), ml + 18 * s, tcY + 46 * s, badgeFont, new SKPaint { Color = white, IsAntialias = true });

            float idX = ml + halfW + 16 * s;
            var ticketIdCard = SKShader.CreateLinearGradient(
                new SKPoint(0, 0), new SKPoint(1, 0),
                new[] { new SKColor(0x06, 0xB6, 0xD4, 0x15), new SKColor(0x7C, 0x3A, 0xED, 0x12) },
                new[] { 0f, 1f }, SKShaderTileMode.Clamp);
            canvas.DrawRoundRect(idX, tcY, halfW, 58 * s, 10 * s, 10 * s, new SKPaint { Shader = ticketIdCard, IsAntialias = true });
            canvas.DrawRoundRect(idX + halfW - 5 * s, tcY + 8 * s, 3 * s, 58 * s - 16 * s, 1.5f * s, 1.5f * s, new SKPaint { Color = teal, IsAntialias = true });

            canvas.DrawText("TICKET ID", idX + 12 * s, tcY + 16 * s, labelFont, new SKPaint { Color = w50, IsAntialias = true });
            using var tidFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 16 * s);
            canvas.DrawText($"TKT-{attendee.TicketCode}", idX + 12 * s, tcY + 46 * s, tidFont, new SKPaint { Color = w90, IsAntialias = true });

            y = tcY + 58 * s + 28 * s;

            // ============================================================
            // SOUNDWAVE PATTERN
            // ============================================================
            float waveY = y + 100 * s;
            for (int i = 0; i < 10; i++)
            {
                float rad = 16 * s + i * 18 * s;
                byte alpha = (byte)((0.02f + i * 0.012f) * 255);
                var wavePaint = new SKPaint
                {
                    Color = new SKColor(0x7C, 0x3A, 0xED, alpha),
                    IsAntialias = true,
                    Style = SKPaintStyle.Stroke,
                    StrokeWidth = (1.5f - i * 0.08f) * s
                };
                if (i % 2 == 0) wavePaint.PathEffect = SKPathEffect.CreateDash(new[] { 6 * s, 4 * s }, 0);
                canvas.DrawCircle(cx, waveY, rad, wavePaint);
            }
            y += 20 * s;

            // ============================================================
            // QR CODE
            // ============================================================
            var qrBytes = _qrService.GenerateQrCode(attendee);
            using var qrBitmap = SKBitmap.Decode(qrBytes);
            int qrSize = (int)(200 * s);
            float qrX = cx - qrSize / 2f;
            float qrY_ = y + 28 * s;

            var qrOuterGlow = new SKPaint
            {
                Color = new SKColor(0x7C, 0x3A, 0xED, 0x25),
                IsAntialias = true,
                MaskFilter = SKMaskFilter.CreateBlur(SKBlurStyle.Normal, 24 * s)
            };
            canvas.DrawRoundRect(qrX - 14 * s, qrY_ - 14 * s, qrSize + 28 * s, qrSize + 28 * s, 16 * s, 16 * s, qrOuterGlow);

            var qrFramePaint = new SKPaint
            {
                Color = new SKColor(0x7C, 0x3A, 0xED, 0x08),
                IsAntialias = true,
            };
            canvas.DrawRoundRect(qrX - 12 * s, qrY_ - 12 * s, qrSize + 24 * s, qrSize + 24 * s, 14 * s, 14 * s, qrFramePaint);

            canvas.DrawRoundRect(qrX - 10 * s, qrY_ - 10 * s, qrSize + 20 * s, qrSize + 20 * s, 12 * s, 12 * s,
                new SKPaint { Color = white, IsAntialias = true });

            canvas.DrawRoundRect(qrX - 10 * s, qrY_ - 10 * s, qrSize + 20 * s, qrSize + 20 * s, 12 * s, 12 * s,
                new SKPaint { Color = new SKColor(0x7C, 0x3A, 0xED, 0x35), IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 2 * s });

            canvas.DrawBitmap(qrBitmap, new SKRect(qrX, qrY_, qrX + qrSize, qrY_ + qrSize));
            y += qrSize + 70 * s;

            using var scanFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 13 * s);
            var scanText = "Present this QR code at the entrance";
            var scanW = scanFont.MeasureText(scanText, new SKPaint());
            canvas.DrawText(scanText, cx - scanW / 2f, y, scanFont, new SKPaint { Color = w50, IsAntialias = true });

            y += 26 * s;

            var arrowText = "▼  SCAN FOR ENTRY  ▼";
            using var arrowFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Normal, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 10 * s);
            var arrowW = arrowFont.MeasureText(arrowText, new SKPaint());
            canvas.DrawText(arrowText, cx - arrowW / 2f, y, arrowFont, new SKPaint { Color = w30, IsAntialias = true });
            y += 32 * s;

            // ============================================================
            // BARCODE STRIP
            // ============================================================
            DrawBarcode(canvas, ml + 20 * s, y, mr - ml - 40 * s, 32 * s, attendee.TicketCode, s);
            y += 44 * s;

            // ============================================================
            // FOOTER
            // ============================================================
            canvas.DrawLine(ml, y, mr, y, new SKPaint { Shader = sepGrad, StrokeWidth = 1 * s, IsAntialias = true });
            y += 20 * s;

            var brandText = "EventPro by Eddy Graphix";
            using var brandFont = new SKFont(SKTypeface.FromFamilyName("Arial", SKFontStyleWeight.Bold, SKFontStyleWidth.Normal, SKFontStyleSlant.Upright), 15 * s);
            var brandW = brandFont.MeasureText(brandText, new SKPaint());
            canvas.DrawText(brandText, cx - brandW / 2f, y + 16 * s, brandFont, new SKPaint { Color = w50, IsAntialias = true });

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            var bytes = data.ToArray();
            return new MemoryStream(bytes);
        }

        // ============================================================
        // DECORATIVE HELPERS
        // ============================================================
        private static void DrawDotGrid(SKCanvas c, int w, int h, SKColor bg, float s)
        {
            var dot = new SKPaint
            {
                Color = new SKColor(0xFF, 0xFF, 0xFF, 0x04),
                IsAntialias = true
            };
            float spacing = 40 * s;
            for (float x = 0; x < w; x += spacing)
                for (float y = 0; y < h; y += spacing)
                    c.DrawCircle(x, y, 1.2f * s, dot);
        }

        private static void DrawCornerOrnament(SKCanvas c, float x, float y, float size, SKColor color)
        {
            var p = new SKPaint { Color = new SKColor(color.Red, color.Green, color.Blue, 0x30), IsAntialias = true, StrokeWidth = 1.5f, Style = SKPaintStyle.Stroke };
            float s2 = size / 2f;
            c.DrawLine(x, y + s2, x + s2, y + s2, p);
            c.DrawLine(x + s2, y, x + s2, y + s2, p);
        }

        private static void DrawBarcode(SKCanvas c, float x, float y, float width, float height, string code, float s)
        {
            var rand = new Random(code.GetHashCode());
            float gap = 1.2f * s;
            float avail = width - 39 * gap;
            float barW = avail / 40f;
            float xx = x;
            for (int i = 0; i < 40; i++)
            {
                float bw = barW + (float)(rand.NextDouble() - 0.5) * barW * 0.6f;
                if (xx + bw > x + width) bw = x + width - xx;
                if (bw < 0.5f * s) bw = 0.5f * s;
                var bar = new SKPaint { Color = new SKColor(0xFF, 0xFF, 0xFF, (byte)(0x40 + rand.Next(0x60))), IsAntialias = true };
                c.DrawRect(xx, y, bw, height - (float)rand.NextDouble() * height * 0.3f, bar);
                xx += bw + gap;
            }
        }

        // ============================================================
        // ICON HELPERS
        // ============================================================
        private static void DrawCalendarIcon(SKCanvas c, float x, float y, float size, SKColor color)
        {
            var p = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.8f };
            var f = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
            c.DrawRoundRect(x, y + size * 0.28f, size, size * 0.72f, 2, 2, p);
            c.DrawLine(x + size * 0.18f, y, x + size * 0.18f, y + size * 0.35f, p);
            c.DrawLine(x + size * 0.55f, y, x + size * 0.55f, y + size * 0.35f, p);
            c.DrawRect(x + size * 0.26f, y + size * 0.52f, size * 0.16f, size * 0.16f, f);
            c.DrawRect(x + size * 0.52f, y + size * 0.52f, size * 0.16f, size * 0.16f, f);
        }

        private static void DrawClockIcon(SKCanvas c, float x, float y, float size, SKColor color)
        {
            var p = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.8f };
            c.DrawCircle(x + size / 2f, y + size / 2f, size / 2f - 1, p);
            float cx_ = x + size / 2f;
            float cy_ = y + size / 2f;
            c.DrawLine(cx_, cy_, cx_, cy_ - size * 0.32f, p);
            c.DrawLine(cx_, cy_, cx_ + size * 0.22f, cy_, p);
            var f = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
            c.DrawCircle(cx_, cy_, 1.4f, f);
        }

        private static void DrawPinIcon(SKCanvas c, float x, float y, float size, SKColor color)
        {
            var p = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.8f };
            var f = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Fill };
            float cx_ = x + size / 2f;
            float topY = y + size * 0.12f;
            float pinR = size * 0.28f;
            c.DrawCircle(cx_, topY + pinR, pinR, f);
            var triPath = new SKPath();
            triPath.MoveTo(cx_ - pinR * 0.55f, topY + pinR * 0.75f);
            triPath.LineTo(cx_, topY + size * 0.88f);
            triPath.LineTo(cx_ + pinR * 0.55f, topY + pinR * 0.75f);
            triPath.Close();
            c.DrawPath(triPath, f);
        }

        private static void DrawMapIcon(SKCanvas c, float x, float y, float size, SKColor color)
        {
            var p = new SKPaint { Color = color, IsAntialias = true, Style = SKPaintStyle.Stroke, StrokeWidth = 1.8f };
            c.DrawRoundRect(x + size * 0.1f, y + size * 0.15f, size * 0.8f, size * 0.65f, 3, 3, p);
            c.DrawLine(x + size / 2f, y, x + size / 2f, y + size * 0.15f, p);
        }

        private static void DrawIconLabel(SKCanvas c, float x, float y, SKFont labelFont, SKFont valueFont,
            string label, string value, SKColor labelColor, SKColor valueColor, float s)
        {
            c.DrawText(label, x, y + 8 * s, labelFont, new SKPaint { Color = labelColor, IsAntialias = true });
            c.DrawText(value, x, y + 28 * s, valueFont, new SKPaint { Color = valueColor, IsAntialias = true });
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
