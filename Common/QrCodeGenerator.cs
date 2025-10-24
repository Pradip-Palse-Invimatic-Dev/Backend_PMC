namespace MyWebApp.Api.Common
{
    using System;
    using System.IO;
    using QRCoder;

    public static class QrCodeGenerator
    {
        public static byte[] GenerateQrCode(string data, int pixelsPerModule = 10)
        {
            using (QRCodeGenerator qrGenerator = new QRCodeGenerator())
            {
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(data, QRCodeGenerator.ECCLevel.Q);

                // Use PngByteQRCode for cross-platform compatibility (Linux/Windows)
                using (var pngQrCode = new PngByteQRCode(qrCodeData))
                {
                    return pngQrCode.GetGraphic(pixelsPerModule);
                }
            }
        }
    }
}