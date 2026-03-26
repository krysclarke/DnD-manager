using Avalonia.Media.Imaging;

namespace DnDManager.Services;

public interface IQrCodeService {
    Bitmap GenerateQrCode(string url, int pixelsPerModule = 4);
}
