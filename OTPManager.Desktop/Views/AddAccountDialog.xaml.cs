using OTPManager.Desktop.Services;
using OTPManager.Shared.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace OTPManager.Desktop.Views
{
    public partial class AddAccountDialog : Window
    {
        public OTPGenerator ResultGenerator { get; private set; }
        private readonly bool isEditing;
        private string originalSecret = "";
        private bool isSecretUnlocked = false;

        public AddAccountDialog(OTPGenerator? existing = null)
        {
            InitializeComponent();
            ResultGenerator = existing ?? new OTPGenerator();
            isEditing = existing != null;

            if (existing != null)
            {
                Title = "Editar Cuenta OTP";
                LabelInput.Text = !string.IsNullOrWhiteSpace(existing.Label) ? existing.Label : existing.Issuer;
                
                if (!string.IsNullOrWhiteSpace(existing.Tags))
                {
                    TagsInput.Text = existing.Tags;
                }
                else if (!string.IsNullOrWhiteSpace(existing.Issuer))
                {
                    var fallbackTag = existing.Issuer.Trim().Replace(" ", "");
                    TagsInput.Text = fallbackTag.StartsWith("#") ? fallbackTag : "#" + fallbackTag;
                }

                originalSecret = existing.SecretBase32 ?? "";
                SecretInput.Text = originalSecret;

                // Lock secret key by default in edit mode to prevent accidental modification
                SecretInput.IsReadOnly = true;
                SecretInput.Background = new SolidColorBrush(Color.FromRgb(243, 244, 246));
                SecretInput.Foreground = new SolidColorBrush(Color.FromRgb(107, 114, 128));
                UnlockSecretBtn.Visibility = Visibility.Visible;

                DigitsSelect.SelectedIndex = existing.NumDigits == 8 ? 1 : 0;
                if (existing.AlgorithmName == "SHA256") AlgorithmSelect.SelectedIndex = 1;
                else if (existing.AlgorithmName == "SHA512") AlgorithmSelect.SelectedIndex = 2;
                else AlgorithmSelect.SelectedIndex = 0;
            }

            Loaded += (s, e) => LabelInput.Focus();
        }

        private void UnlockSecret_Click(object sender, RoutedEventArgs e)
        {
            if (!isSecretUnlocked)
            {
                var result = MessageBox.Show(
                    this,
                    "⚠️ ADVERTENCIA:\n\nModificar la clave secreta (semilla) cambiará los códigos OTP generados y podrías perder el acceso a la cuenta si la clave no es la correcta.\n\n¿Estás seguro de que deseas desbloquearla para modificarla?",
                    "Confirmar modificación de semilla",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (result == MessageBoxResult.Yes)
                {
                    isSecretUnlocked = true;
                    SecretInput.IsReadOnly = false;
                    SecretInput.Background = Brushes.White;
                    SecretInput.Foreground = Brushes.Black;
                    UnlockSecretBtn.Content = "🔓 Semilla desbloqueada";
                    UnlockSecretBtn.IsEnabled = false;
                    SecretInput.Focus();
                    SecretInput.SelectAll();
                }
            }
        }

        private void ScanScreen_Click(object sender, RoutedEventArgs e)
        {
            var previousVisibility = Visibility;
            Visibility = Visibility.Collapsed;

            try
            {
                var snipper = new ScreenSnippingWindow();
                if (snipper.ShowDialog() == true)
                {
                    if (!string.IsNullOrEmpty(snipper.ScannedQrCode))
                    {
                        ApplyScannedQr(snipper.ScannedQrCode);
                    }
                    else
                    {
                        MessageBox.Show("No se detectó ningún código QR en la zona seleccionada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            finally
            {
                Visibility = previousVisibility;
                Activate();
            }
        }

        private void ScanCamera_Click(object sender, RoutedEventArgs e)
        {
            var scanner = new CameraScannerWindow { Owner = this };
            if (scanner.ShowDialog() == true && !string.IsNullOrEmpty(scanner.ScannedQrCode))
            {
                ApplyScannedQr(scanner.ScannedQrCode);
            }
        }

        private void ScanClipboard_Click(object sender, RoutedEventArgs e)
        {
            var qr = QRCodeScannerService.DecodeQrCodeFromClipboard();
            if (!string.IsNullOrEmpty(qr))
            {
                ApplyScannedQr(qr);
            }
            else
            {
                MessageBox.Show(this, "No se encontró un código QR en la imagen del portapapeles.\n\nPuedes capturar una parte de la pantalla con 'Win + Shift + S' y volver a pulsar este botón.", "Portapapeles", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        public void ApplyScannedQr(string qrContent)
        {
            if (string.IsNullOrWhiteSpace(qrContent)) return;

            if (isEditing && !isSecretUnlocked)
            {
                var confirm = MessageBox.Show(
                    this,
                    "⚠️ La cuenta actual tiene la clave secreta (semilla) protegida.\n\n¿Deseas reemplazarla con los datos del código QR escaneado?",
                    "Confirmar reemplazo de semilla",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning);

                if (confirm != MessageBoxResult.Yes)
                {
                    return;
                }

                isSecretUnlocked = true;
                SecretInput.IsReadOnly = false;
                SecretInput.Background = Brushes.White;
                SecretInput.Foreground = Brushes.Black;
                UnlockSecretBtn.Content = "🔓 Semilla desbloqueada";
                UnlockSecretBtn.IsEnabled = false;
            }

            var trimmed = qrContent.Trim();
            if (trimmed.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
            {
                UriInput.Text = trimmed;
            }
            else
            {
                SecretInput.Text = trimmed.Replace(" ", "").Replace("-", "");
                ErrorMessage.Visibility = Visibility.Collapsed;
            }
        }

        private void UriInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            var text = UriInput.Text?.Trim();
            if (!string.IsNullOrEmpty(text) && text.StartsWith("otpauth://", StringComparison.OrdinalIgnoreCase))
            {
                var parsed = OTPGenerator.FromString(text);
                if (parsed != null)
                {
                    LabelInput.Text = !string.IsNullOrWhiteSpace(parsed.Label) ? parsed.Label : parsed.Issuer;
                    
                    if (!string.IsNullOrWhiteSpace(parsed.Issuer) && string.IsNullOrWhiteSpace(TagsInput.Text))
                    {
                        var cleanTag = parsed.Issuer.Trim().Replace(" ", "");
                        TagsInput.Text = cleanTag.StartsWith("#") ? cleanTag : "#" + cleanTag;
                    }

                    if (!isEditing || isSecretUnlocked)
                    {
                        SecretInput.Text = parsed.SecretBase32 ?? "";
                    }

                    DigitsSelect.SelectedIndex = parsed.NumDigits == 8 ? 1 : 0;
                    if (parsed.AlgorithmName == "SHA256") AlgorithmSelect.SelectedIndex = 1;
                    else if (parsed.AlgorithmName == "SHA512") AlgorithmSelect.SelectedIndex = 2;
                    else AlgorithmSelect.SelectedIndex = 0;
                    ErrorMessage.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            var accountName = LabelInput.Text?.Trim();
            if (string.IsNullOrEmpty(accountName))
            {
                ShowError("Debes ingresar el nombre de la cuenta.");
                LabelInput.Focus();
                return;
            }

            var secret = SecretInput.Text?.Replace(" ", "").Replace("-", "").Trim();
            if (string.IsNullOrEmpty(secret))
            {
                ShowError("Debes ingresar la clave secreta en formato Base32.");
                SecretInput.Focus();
                return;
            }

            try
            {
                ResultGenerator.SecretBase32 = secret;
                _ = ResultGenerator.GenerateOTP(DateTime.UtcNow);
            }
            catch
            {
                ShowError("La clave secreta no es válida en formato Base32.");
                SecretInput.Focus();
                return;
            }

            if (isEditing && isSecretUnlocked && !string.Equals(secret, originalSecret, StringComparison.OrdinalIgnoreCase))
            {
                var confirmSave = MessageBox.Show(
                    this,
                    "⚠️ Has modificado la clave secreta (semilla) original.\n\nLos códigos de verificación que genere esta cuenta cambiarán por completo a partir de ahora.\n\n¿Deseas guardar definitivamente este cambio?",
                    "Confirmación de cambio de semilla",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Exclamation);

                if (confirmSave != MessageBoxResult.Yes)
                {
                    return;
                }
            }

            ResultGenerator.Label = accountName;
            ResultGenerator.Tags = OTPGenerator.NormalizeTags(TagsInput.Text ?? "");
            ResultGenerator.NumDigits = DigitsSelect.SelectedIndex == 1 ? 8 : 6;
            ResultGenerator.AlgorithmName = AlgorithmSelect.SelectedIndex switch
            {
                1 => "SHA256",
                2 => "SHA512",
                _ => "SHA1"
            };

            DialogResult = true;
            Close();
        }

        private void ShowError(string msg)
        {
            ErrorMessage.Text = msg;
            ErrorMessage.Visibility = Visibility.Visible;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
