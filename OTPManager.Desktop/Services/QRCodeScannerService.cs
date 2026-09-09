using System;
using System.Drawing;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;

namespace OTPManager.Desktop.Services
{
    public static class QRCodeScannerService
    {
        public static string? DecodeQrCode(Bitmap bitmap)
        {
            if (bitmap == null) return null;

            try
            {
                var reader = new ZXing.Windows.Compatibility.BarcodeReader
                {
                    AutoRotate = true
                };

                var result = reader.Decode(bitmap);
                return result?.Text;
            }
            catch
            {
                return null;
            }
        }

        public static string? DecodeQrCode(byte[] imageBytes)
        {
            if (imageBytes == null || imageBytes.Length == 0) return null;

            try
            {
                using var ms = new MemoryStream(imageBytes);
                using var bitmap = new Bitmap(ms);
                return DecodeQrCode(bitmap);
            }
            catch
            {
                return null;
            }
        }

        public static string? DecodeQrCodeFromClipboard()
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    var source = Clipboard.GetImage();
                    if (source != null)
                    {
                        using var ms = new MemoryStream();
                        var encoder = new BmpBitmapEncoder();
                        encoder.Frames.Add(BitmapFrame.Create(source));
                        encoder.Save(ms);
                        ms.Position = 0;
                        using var bitmap = new Bitmap(ms);
                        return DecodeQrCode(bitmap);
                    }
                }
            }
            catch
            {
                // Ignore clipboard access errors
            }

            return null;
        }

        public static Bitmap CaptureScreenRegion(int x, int y, int width, int height)
        {
            width = Math.Max(width, 1);
            height = Math.Max(height, 1);

            var bitmap = new Bitmap(width, height);
            using (var g = Graphics.FromImage(bitmap))
            {
                g.CopyFromScreen(x, y, 0, 0, new System.Drawing.Size(width, height), CopyPixelOperation.SourceCopy);
            }

            return bitmap;
        }
    }
}
