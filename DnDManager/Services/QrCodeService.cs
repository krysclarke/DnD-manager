using Avalonia.Media.Imaging;
using QRCoder;

namespace DnDManager.Services;

public class QrCodeService : IQrCodeService {
    public Bitmap GenerateQrCode(string url, int pixelsPerModule = 4) {
        using var qrGenerator = new QRCodeGenerator();
        using var qrCodeData = qrGenerator.CreateQrCode(url, QRCodeGenerator.ECCLevel.M);
        using var qrCode = new PngByteQRCode(qrCodeData);
        var pngBytes = qrCode.GetGraphic(pixelsPerModule);

        using var stream = new MemoryStream(pngBytes);
        return new Bitmap(stream);
    }
}
