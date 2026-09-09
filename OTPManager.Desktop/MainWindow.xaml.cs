using Microsoft.Win32;
using OTPManager.Desktop.Models;
using OTPManager.Desktop.Services;
using OTPManager.Desktop.Views;
using OTPManager.Shared.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace OTPManager.Desktop
{
    public partial class MainWindow : Window
    {
        private readonly DesktopStorageService storage = new DesktopStorageService();
        private readonly List<OTPDisplayItem> allItems = new List<OTPDisplayItem>();
        private readonly DispatcherTimer timer = new DispatcherTimer();
        private DateTime lastStepTime = DateTime.MinValue;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;

            timer.Interval = TimeSpan.FromMilliseconds(100);
            timer.Tick += Timer_Tick;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadAccountsAsync();
            timer.Start();
        }

        private async Task LoadAccountsAsync()
        {
            try
            {
                var generators = await storage.GetAllAsync();
                allItems.Clear();
                foreach (var gen in generators)
                {
                    allItems.Add(new OTPDisplayItem(gen));
                }
                UpdateTagFilterBar();
                ApplySearchFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error al cargar las cuentas: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private List<string> GetAllKnownTags()
        {
            return allItems
                .SelectMany(x => x.Tags)
                .Where(t => !string.IsNullOrWhiteSpace(t))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(t => t, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private void UpdateTagFilterBar()
        {
            var tags = GetAllKnownTags();
            if (tags.Count == 0)
            {
                TagFilterScrollViewer.Visibility = Visibility.Collapsed;
                return;
            }

            TagFilterScrollViewer.Visibility = Visibility.Visible;
            TagFilterPanel.Children.Clear();

            var chipStyle = TryFindResource("TagFilterChipStyle") as Style;
            var currentQuery = SearchBox.Text?.Trim() ?? "";

            // "Todos" chip
            var allBtn = new Button
            {
                Content = "Todos",
                Tag = "",
                Style = chipStyle
            };
            allBtn.Click += TagFilterChip_Click;
            TagFilterPanel.Children.Add(allBtn);

            foreach (var tag in tags)
            {
                var btn = new Button
                {
                    Content = tag,
                    Tag = tag,
                    Style = chipStyle
                };
                btn.Click += TagFilterChip_Click;
                TagFilterPanel.Children.Add(btn);
            }

            UpdateTagFilterBarActiveState();
        }

        private void TagFilterChip_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string tag)
            {
                if (string.IsNullOrEmpty(tag))
                {
                    SearchBox.Text = string.Empty;
                }
                else
                {
                    if (string.Equals(SearchBox.Text?.Trim(), tag, StringComparison.OrdinalIgnoreCase))
                    {
                        SearchBox.Text = string.Empty;
                    }
                    else
                    {
                        SearchBox.Text = tag;
                    }
                }
            }
        }

        private void UpdateTagFilterBarActiveState()
        {
            var currentQuery = SearchBox.Text?.Trim() ?? "";
            foreach (var child in TagFilterPanel.Children)
            {
                if (child is Button btn && btn.Tag is string tag)
                {
                    bool isSelected = string.IsNullOrEmpty(tag) 
                        ? string.IsNullOrEmpty(currentQuery)
                        : string.Equals(currentQuery, tag, StringComparison.OrdinalIgnoreCase);

                    if (isSelected)
                    {
                        btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 103, 192));
                        btn.Foreground = System.Windows.Media.Brushes.White;
                        btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 90, 158));
                    }
                    else
                    {
                        btn.Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(238, 244, 252));
                        btn.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(0, 90, 158));
                        btn.BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(204, 227, 248));
                    }
                }
            }
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            int elapsedSeconds = now.Second % OTPGenerator.TimeStepSeconds;
            double remainingSeconds = OTPGenerator.TimeStepSeconds - (elapsedSeconds + (now.Millisecond / 1000.0));
            TotpProgressBar.Value = remainingSeconds;

            if (remainingSeconds < 5)
            {
                TotpProgressBar.Foreground = System.Windows.Media.Brushes.DarkOrange;
            }
            else
            {
                TotpProgressBar.Foreground = System.Windows.Media.Brushes.DodgerBlue;
            }

            if (now.Subtract(lastStepTime).TotalSeconds >= 1)
            {
                lastStepTime = now;
                foreach (var item in allItems)
                {
                    item.UpdateOTP(now);
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var query = SearchBox.Text;
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(query) ? Visibility.Visible : Visibility.Collapsed;
            ClearSearchBtn.Visibility = string.IsNullOrEmpty(query) ? Visibility.Collapsed : Visibility.Visible;
            ApplySearchFilter();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = string.Empty;
            SearchBox.Focus();
        }

        private void ApplySearchFilter()
        {
            var query = SearchBox.Text?.Trim();
            UpdateTagFilterBarActiveState();

            if (allItems.Count == 0)
            {
                AccountsScrollViewer.Visibility = Visibility.Collapsed;
                EmptyVaultBanner.Visibility = Visibility.Visible;
                NoSearchResultsBanner.Visibility = Visibility.Collapsed;
                return;
            }

            EmptyVaultBanner.Visibility = Visibility.Collapsed;

            List<OTPDisplayItem> filtered;
            if (string.IsNullOrWhiteSpace(query))
            {
                filtered = allItems.ToList();
            }
            else
            {
                filtered = allItems
                    .Where(x => x.AccountName.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                             || x.TagsString.Contains(query, StringComparison.CurrentCultureIgnoreCase)
                             || (!string.IsNullOrEmpty(x.Issuer) && x.Issuer.Contains(query, StringComparison.CurrentCultureIgnoreCase)))
                    .ToList();
            }

            if (filtered.Count == 0)
            {
                AccountsScrollViewer.Visibility = Visibility.Collapsed;
                NoSearchResultsText.Text = $"No se encontraron cuentas para '{query}'";
                NoSearchResultsBanner.Visibility = Visibility.Visible;
            }
            else
            {
                AccountsScrollViewer.Visibility = Visibility.Visible;
                NoSearchResultsBanner.Visibility = Visibility.Collapsed;
                AccountsList.ItemsSource = filtered;
            }
        }

        private async void CopyCode_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OTPDisplayItem item)
            {
                try
                {
                    Clipboard.SetText(item.RawOTP);
                    var originalContent = btn.Content;
                    btn.Content = "✓ ¡Copiado!";
                    await Task.Delay(1400);
                    btn.Content = originalContent;
                }
                catch
                {
                    // Ignore transient clipboard access
                }
            }
        }

        private void ItemMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void BackupMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void AddAccountMenu_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.ContextMenu != null)
            {
                btn.ContextMenu.PlacementTarget = btn;
                btn.ContextMenu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
                btn.ContextMenu.IsOpen = true;
            }
        }

        private void AddAccountManual_Click(object sender, RoutedEventArgs e)
        {
            AddAccount_Click(sender, e);
        }

        private async void AddAccount_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddAccountDialog { Owner = this };
            if (dialog.ShowDialog() == true)
            {
                await storage.InsertOrReplaceAsync(dialog.ResultGenerator);
                await LoadAccountsAsync();
            }
        }

        private void MainScanScreen_Click(object sender, RoutedEventArgs e)
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
                        OpenAddAccountWithQr(snipper.ScannedQrCode);
                    }
                    else
                    {
                        MessageBox.Show(this, "No se detectó ningún código QR en la zona seleccionada.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
            }
            finally
            {
                Visibility = previousVisibility;
                Activate();
            }
        }

        private void MainScanCamera_Click(object sender, RoutedEventArgs e)
        {
            var scanner = new CameraScannerWindow { Owner = this };
            if (scanner.ShowDialog() == true && !string.IsNullOrEmpty(scanner.ScannedQrCode))
            {
                OpenAddAccountWithQr(scanner.ScannedQrCode);
            }
        }

        private void MainScanClipboard_Click(object sender, RoutedEventArgs e)
        {
            var qr = QRCodeScannerService.DecodeQrCodeFromClipboard();
            if (!string.IsNullOrEmpty(qr))
            {
                OpenAddAccountWithQr(qr);
            }
            else
            {
                MessageBox.Show(this, "No se detectó un código QR en la imagen del portapapeles.\n\nPuedes capturar la pantalla con 'Win + Shift + S' y volver a intentar.", "Portapapeles", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void OpenAddAccountWithQr(string qrContent)
        {
            var dialog = new AddAccountDialog { Owner = this };
            dialog.ApplyScannedQr(qrContent);
            if (dialog.ShowDialog() == true)
            {
                await storage.InsertOrReplaceAsync(dialog.ResultGenerator);
                await LoadAccountsAsync();
            }
        }

        private async void EditAccount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is Button btn && btn.Tag is OTPDisplayItem item)
            {
                var dialog = new AddAccountDialog(item.Generator) { Owner = this };
                if (dialog.ShowDialog() == true)
                {
                    await storage.InsertOrReplaceAsync(dialog.ResultGenerator);
                    await LoadAccountsAsync();
                }
            }
        }

        private async void DeleteAccount_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem menuItem && menuItem.Parent is ContextMenu contextMenu && contextMenu.PlacementTarget is Button btn && btn.Tag is OTPDisplayItem item)
            {
                var result = MessageBox.Show(
                    $"¿Estás seguro de que deseas eliminar la cuenta '{item.AccountName}'?",
                    "Confirmar eliminación",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    await storage.DeleteAsync(item.Generator);
                    await LoadAccountsAsync();
                }
            }
        }

        private async void ImportBackup_Click(object sender, RoutedEventArgs e)
        {
            var openDlg = new OpenFileDialog
            {
                Title = "Seleccionar archivo de copia de seguridad",
                Filter = "Copia de seguridad OTPManager (*.otpm)|*.otpm|Todos los archivos (*.*)|*.*",
                DefaultExt = ".otpm"
            };

            if (openDlg.ShowDialog(this) == true)
            {
                var pwDlg = new PasswordDialog("Ingresa la contraseña del archivo de respaldo:") { Owner = this };
                if (pwDlg.ShowDialog() == true)
                {
                    try
                    {
                        using var fileStream = File.OpenRead(openDlg.FileName);
                        var success = await storage.RestoreAsync(fileStream, pwDlg.Password);
                        if (success)
                        {
                            await LoadAccountsAsync();
                            MessageBox.Show("¡Copia de seguridad importada con éxito!", "Importación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Contraseña incorrecta o archivo de respaldo no válido.", "Error al importar", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al leer el archivo: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void ExportBackup_Click(object sender, RoutedEventArgs e)
        {
            if (allItems.Count == 0)
            {
                MessageBox.Show("No hay cuentas guardadas para exportar.", "Aviso", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var pwDlg = new PasswordDialog("Ingresa una contraseña para cifrar y proteger el archivo:") { Owner = this };
            if (pwDlg.ShowDialog() == true)
            {
                var saveDlg = new SaveFileDialog
                {
                    Title = "Guardar copia de seguridad",
                    Filter = "Copia de seguridad OTPManager (*.otpm)|*.otpm",
                    DefaultExt = ".otpm",
                    FileName = $"OTPManager_Backup_{DateTime.Now:yyyyMMdd}.otpm"
                };

                if (saveDlg.ShowDialog(this) == true)
                {
                    try
                    {
                        using var dumpStream = await storage.DumpAsync(pwDlg.Password);
                        using var fileStream = File.Create(saveDlg.FileName);
                        dumpStream.Position = 0;
                        await dumpStream.CopyToAsync(fileStream);
                        MessageBox.Show($"Copia de seguridad guardada correctamente en:\n{saveDlg.FileName}", "Exportación exitosa", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error al exportar: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }
    }
}
