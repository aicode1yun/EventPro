using System.Text.Json;
using QRCoder;
using Ticket.Models;

namespace Ticket.Services
{
    public interface IQrCodeService
    {
        byte[] GenerateQrCode(Attendee attendee);
        QrPayload? ParseQrPayload(string rawData);
    }

    public class QrPayload
    {
        public string TicketId { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty;
    }

    public class QrCodeService : IQrCodeService
    {
        public byte[] GenerateQrCode(Attendee attendee)
        {
            var payload = new QrPayload
            {
                TicketId = attendee.TicketCode,
                Token = attendee.QrToken
            };

            var json = JsonSerializer.Serialize(payload);
            using var qrGenerator = new QRCodeGenerator();
            using var qrData = qrGenerator.CreateQrCode(json, QRCodeGenerator.ECCLevel.Q);
            using var qrCode = new PngByteQRCode(qrData);
            return qrCode.GetGraphic(20);
        }

        public QrPayload? ParseQrPayload(string rawData)
        {
            try
            {
                return JsonSerializer.Deserialize<QrPayload>(rawData);
            }
            catch
            {
                return null;
            }
        }
    }
}
