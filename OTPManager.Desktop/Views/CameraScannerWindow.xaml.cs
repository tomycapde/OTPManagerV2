using FlashCap;
using OTPManager.Desktop.Services;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace OTPManager.Desktop.Views
{
    public partial class CameraScannerWindow : Window
    {
        private CaptureDevice? currentDevice;
        private List<CaptureDeviceDescriptor> descriptors = new List<CaptureDeviceDescriptor>();
        private int isUpdatingPreview = 0;
        private bool isDetected = false;

        public string? ScannedQrCode { get; private set; }

        public CameraScannerWindow()
        {
            InitializeComponent();
            Loaded += CameraScannerWindow_Loaded;
            Closing += CameraScannerWindow_Closing;
        }

        private async void CameraScannerWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await InitializeCameraAsync();
        }

        private async Task InitializeCameraAsync()
        {
            try
            {
                var devices = new CaptureDevices();
                descriptors = devices.EnumerateDescriptors().ToList();

                if (descriptors.Count == 0)
                {
                    NoCameraOverlay.Visibility = Visibility.Visible;
                    StatusText.Text = "No se encontró ninguna cámara web conectada.";
                    StatusText.Foreground = Brushes.Tomato;
                    return;
                }

                if (descriptors.Count > 1)
                {
                    CameraSelector.ItemsSource = descriptors.Select(d => d.Name).ToList();
                    CameraSelector.SelectedIndex = 0;
                    CameraSelector.Visibility = Visibility.Visible;
                }

                await StartCameraAsync(descriptors[0]);
            }
            catch (Exception ex)
            {
                NoCameraOverlay.Visibility = Visibility.Visible;
                NoCameraMessage.Text = $"Error al acceder a la cámara: {ex.Message}";
                StatusText.Text = "No fue posible iniciar el dispositivo de video.";
                StatusText.Foreground = Brushes.Tomato;
            }
        }

        private async Task StartCameraAsync(CaptureDeviceDescriptor descriptor)
        {
            await StopCurrentDeviceAsync();

            try
            {
                StatusText.Text = "Buscando código QR...";
                StatusText.Foreground = new SolidColorBrush(Color.FromRgb(160, 168, 184));

                var characteristic = descriptor.Characteristics.FirstOrDefault() 
                    ?? throw new InvalidOperationException("No se encontraron características de captura para la cámara.");

                currentDevice = await descriptor.OpenAsync(characteristic, OnPixelBufferArrived);
                await currentDevice.StartAsync();
            }
            catch (Exception ex)
            {
                StatusText.Text = $"Error al iniciar captura: {ex.Message}";
                StatusText.Foreground = Brushes.Tomato;
            }
        }

        private void OnPixelBufferArrived(PixelBufferScope bufferScope)
        {
            if (isDetected) return;

            byte[] imageBytes;
            try
            {
                imageBytes = bufferScope.Buffer.ExtractImage();
            }
            catch
            {
                return;
            }

            // Decode QR Code
            var qrText = QRCodeScannerService.DecodeQrCode(imageBytes);
            if (!string.IsNullOrEmpty(qrText) && !isDetected)
            {
                isDetected = true;
                ScannedQrCode = qrText;

                Dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        System.Media.SystemSounds.Asterisk.Play();
                    }
                    catch { }

                    StatusText.Text = "✓ ¡Código QR detectado!";
                    StatusText.Foreground = Brushes.LightGreen;

                    await StopCurrentDeviceAsync();
                    await Task.Delay(350);

                    DialogResult = true;
                    Close();
                });

                return;
            }

            // Update Preview Frame (throttled)
            if (Interlocked.CompareExchange(ref isUpdatingPreview, 1, 0) == 0)
            {
                Dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        using var ms = new MemoryStream(imageBytes);
                        var bmp = new BitmapImage();
                        bmp.BeginInit();
                        bmp.CacheOption = BitmapCacheOption.OnLoad;
                        bmp.StreamSource = ms;
                        bmp.EndInit();
                        bmp.Freeze();
                        CameraPreview.Source = bmp;
                    }
                    catch
                    {
                        // Ignore frame rendering glitches
                    }
                    finally
                    {
                        Interlocked.Exchange(ref isUpdatingPreview, 0);
                    }
                });
            }
        }

        private async void CameraSelector_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CameraSelector.SelectedIndex >= 0 && CameraSelector.SelectedIndex < descriptors.Count)
            {
                await StartCameraAsync(descriptors[CameraSelector.SelectedIndex]);
            }
        }

        private async Task StopCurrentDeviceAsync()
        {
            if (currentDevice != null)
            {
                try
                {
                    await currentDevice.StopAsync();
                    currentDevice.Dispose();
                }
                catch
                {
                    // Ignore shutdown errors
                }
                finally
                {
                    currentDevice = null;
                }
            }
        }

        private async void CameraScannerWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            await StopCurrentDeviceAsync();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
