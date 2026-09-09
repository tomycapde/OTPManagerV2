# Captura y Escaneo de Códigos QR

Este documento explica en detalle cómo la aplicación **OTPManager.Desktop** detecta y procesa códigos QR para la adición rápida de cuentas de doble factor (2FA).

---

## 1. Métodos de Captura Disponibles

La aplicación cuenta con tres alternativas para capturar un código QR sin necesidad de escribir la clave secreta manualmente:

```
                          ┌────────────────────────┐
                          │  AddAccountDialog.xaml │
                          └───────────┬────────────┘
                                      │
            ┌─────────────────────────┼─────────────────────────┐
            ▼                         ▼                         ▼
   [ 1. Cámara Web ]        [ 2. Recorte Pantalla ]   [ 3. Portapapeles ]
  CameraScannerWindow       ScreenSnippingWindow       DecodeQrCodeFromClipboard
   (FlashCap + ZXing)     (VirtualScreen + ZXing)             (ZXing)
            │                         │                         │
            └─────────────────────────┼─────────────────────────┘
                                      ▼
                        Texto decodificado (otpauth://)
                                      ▼
                             OTPUriConverter
                                      ▼
                     Campos de cuenta autocompletados
```

---

## 2. Detalle de Implementación

### A. Escaneo con Cámara Web (`CameraScannerWindow`)
* **Librería de captura:** `FlashCap` (interfaz de alto rendimiento para captura de video UVC/DirectShow sin dependencias externas pesadas).
* **Librería de decodificación:** `ZXing.Net` (`BarcodeReader` con `AutoRotate = true`).
* **Funcionamiento:**
  1. Se enumeran los dispositivos de video conectados (`CaptureDevices.EnumerateDescriptors()`).
  2. Si hay más de una cámara conectada, se muestra un selector desplegable.
  3. Se inicia un stream de video en segundo plano a 30 FPS.
  4. Los cuadros de video se envían al decodificador de `ZXing`.
  5. En cuanto se detecta un código QR válido, la ventana emite una señal sonora / visual y se cierra automáticamente retornando el resultado.
  6. Se liberan adecuadamente los descriptores de hardware en el evento `Closing`.

---

### B. Recorte de Pantalla / Screen Snipping (`ScreenSnippingWindow`)
Permite capturar un código QR que se encuentre visible en cualquier parte de la pantalla (por ejemplo, en un navegador web o documento):
* **Soporte multimonitor:**
  * Utiliza la API nativa de Windows `GetSystemMetrics` con los índices:
    * `SM_XVIRTUALSCREEN` (76)
    * `SM_YVIRTUALSCREEN` (77)
    * `SM_CXVIRTUALSCREEN` (78)
    * `SM_CYVIRTUALSCREEN` (79)
  * Esto permite cubrir la totalidad del escritorio extendido en configuraciones de múltiples monitores sin cortes ni distorsiones de DPI.
* **Flujo interactivo:**
  1. Al activarse, toma una captura instantánea congelada del escritorio (`Graphics.CopyFromScreen`).
  2. Despliega una ventana translúcida en pantalla completa con el fondo congelado.
  3. El usuario dibuja un rectángulo con el cursor del ratón sobre el código QR.
  4. La región seleccionada se recorta como un `Bitmap` en memoria y se envía a `QRCodeScannerService.DecodeQrCode`.
  5. Si se detecta el QR, se extrae la información y se regresa a la pantalla de añadir cuenta.
  6. Se puede cancelar en cualquier momento presionando la tecla **Escape**.

---

### C. Detección desde el Portapapeles (`QRCodeScannerService.DecodeQrCodeFromClipboard`)
* Si el usuario copió una captura de pantalla al portapapeles (por ejemplo, con `Win + Shift + S` de Windows o copiando una imagen web):
  1. La aplicación verifica si el portapapeles contiene una imagen (`Clipboard.ContainsImage()`).
  2. Convierte el `BitmapSource` a formato mapa de bits en memoria mediante un `BmpBitmapEncoder`.
  3. `ZXing` procesa la imagen e intenta decodificar el código QR.
  4. Si se encuentra un código válido, los datos se autocompletan instantáneamente.

---

## 3. Procesamiento del Formato Estándar `otpauth://`

Una vez extraído el texto del código QR, el servicio [`OTPUriConverter`](../OTPManager.Shared/Components/OTPUriConverter.cs) procesa la URI según la especificación de Google Authenticator:

```text
otpauth://totp/Emisor:usuario@dominio.com?secret=JBSWY3DPEHPK3PXP&issuer=Emisor&algorithm=SHA1&digits=6&period=30
```

El conversor extrae automáticamente:
* **Secreto Base32 (`secret`):** Decodificado a bytes para la generación criptográfica.
* **Emisor (`issuer`):** Nombre del servicio (ej. *GitHub*, *Google*, *AWS*).
* **Etiqueta / Cuenta (`label`):** Correo electrónico o identificador de usuario.
* **Algoritmo (`algorithm`):** Por defecto `SHA1`, pero con soporte para `SHA256` y `SHA512`.
* **Número de dígitos (`digits`):** Soporta 6 u 8 dígitos.
* **Período (`period`):** Intervalo de rotación temporal (normalmente 30 segundos).
