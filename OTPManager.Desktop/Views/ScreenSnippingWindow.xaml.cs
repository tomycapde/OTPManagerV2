using OTPManager.Desktop.Services;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace OTPManager.Desktop.Views
{
    public partial class ScreenSnippingWindow : Window
    {
        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int nIndex);

        private const int SM_XVIRTUALSCREEN = 76;
        private const int SM_YVIRTUALSCREEN = 77;
        private const int SM_CXVIRTUALSCREEN = 78;
        private const int SM_CYVIRTUALSCREEN = 79;

        private Bitmap? fullScreenshot;
        private System.Windows.Point startPoint;
        private bool isDragging = false;
        private int screenLeft;
        private int screenTop;
        private int screenWidth;
        private int screenHeight;

        public string? ScannedQrCode { get; private set; }

        public ScreenSnippingWindow()
        {
            InitializeComponent();

            // Query true physical metrics of all monitors combined
            screenLeft = GetSystemMetrics(SM_XVIRTUALSCREEN);
            screenTop = GetSystemMetrics(SM_YVIRTUALSCREEN);
            screenWidth = GetSystemMetrics(SM_CXVIRTUALSCREEN);
            screenHeight = GetSystemMetrics(SM_CYVIRTUALSCREEN);

            // Cover the virtual desktop in WPF coordinate space
            Left = SystemParameters.VirtualScreenLeft;
            Top = SystemParameters.VirtualScreenTop;
            Width = SystemParameters.VirtualScreenWidth;
            Height = SystemParameters.VirtualScreenHeight;

            CaptureFrozenScreenshot();
            Closed += (s, e) => fullScreenshot?.Dispose();
        }

        private void CaptureFrozenScreenshot()
        {
            try
            {
                fullScreenshot = new Bitmap(screenWidth, screenHeight);
                using (var g = Graphics.FromImage(fullScreenshot))
                {
                    g.CopyFromScreen(screenLeft, screenTop, 0, 0, new System.Drawing.Size(screenWidth, screenHeight), CopyPixelOperation.SourceCopy);
                }

                using var ms = new MemoryStream();
                fullScreenshot.Save(ms, ImageFormat.Bmp);
                ms.Position = 0;

                var bmpImage = new BitmapImage();
                bmpImage.BeginInit();
                bmpImage.CacheOption = BitmapCacheOption.OnLoad;
                bmpImage.StreamSource = ms;
                bmpImage.EndInit();
                bmpImage.Freeze();

                BackgroundImage.Source = bmpImage;
            }
            catch
            {
                // Fallback to solid dimmed background if capture fails
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(120, 0, 0, 0));
            }
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                DialogResult = false;
                Close();
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                startPoint = e.GetPosition(SelectionCanvas);
                isDragging = true;

                Canvas.SetLeft(CutoutBox, startPoint.X);
                Canvas.SetTop(CutoutBox, startPoint.Y);
                CutoutBox.Width = 0;
                CutoutBox.Height = 0;
                CutoutBox.Visibility = Visibility.Visible;
                CaptureMouse();
            }
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if (isDragging)
            {
                var currentPoint = e.GetPosition(SelectionCanvas);

                var x = Math.Min(currentPoint.X, startPoint.X);
                var y = Math.Min(currentPoint.Y, startPoint.Y);
                var w = Math.Abs(currentPoint.X - startPoint.X);
                var h = Math.Abs(currentPoint.Y - startPoint.Y);

                Canvas.SetLeft(CutoutBox, x);
                Canvas.SetTop(CutoutBox, y);
                CutoutBox.Width = w;
                CutoutBox.Height = h;
            }
        }

        private void Window_MouseUp(object sender, MouseButtonEventArgs e)
        {
            if (isDragging)
            {
                isDragging = false;
                ReleaseMouseCapture();

                var currentPoint = e.GetPosition(SelectionCanvas);
                var minX = Math.Min(currentPoint.X, startPoint.X);
                var minY = Math.Min(currentPoint.Y, startPoint.Y);
                var w = Math.Abs(currentPoint.X - startPoint.X);
                var h = Math.Abs(currentPoint.Y - startPoint.Y);

                if (w > 12 && h > 12 && fullScreenshot != null)
                {
                    double scaleX = (double)fullScreenshot.Width / Math.Max(ActualWidth, 1);
                    double scaleY = (double)fullScreenshot.Height / Math.Max(ActualHeight, 1);

                    int cropX = Math.Max(0, (int)(minX * scaleX));
                    int cropY = Math.Max(0, (int)(minY * scaleY));
                    int cropW = Math.Min(fullScreenshot.Width - cropX, (int)(w * scaleX));
                    int cropH = Math.Min(fullScreenshot.Height - cropY, (int)(h * scaleY));

                    if (cropW > 10 && cropH > 10)
                    {
                        using var cropped = fullScreenshot.Clone(new Rectangle(cropX, cropY, cropW, cropH), fullScreenshot.PixelFormat);
                        ScannedQrCode = QRCodeScannerService.DecodeQrCode(cropped);

                        // If not detected with exact crop, try with 15% padding
                        if (string.IsNullOrEmpty(ScannedQrCode))
                        {
                            int padX = (int)(cropW * 0.15);
                            int padY = (int)(cropH * 0.15);
                            int expX = Math.Max(0, cropX - padX);
                            int expY = Math.Max(0, cropY - padY);
                            int expW = Math.Min(fullScreenshot.Width - expX, cropW + (padX * 2));
                            int expH = Math.Min(fullScreenshot.Height - expY, cropH + (padY * 2));

                            using var expanded = fullScreenshot.Clone(new Rectangle(expX, expY, expW, expH), fullScreenshot.PixelFormat);
                            ScannedQrCode = QRCodeScannerService.DecodeQrCode(expanded);
                        }
                    }

                    DialogResult = true;
                    Close();
                }
                else
                {
                    CutoutBox.Visibility = Visibility.Collapsed;
                }
            }
        }
    }
}
