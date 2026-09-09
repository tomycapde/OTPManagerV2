# Arquitectura y Proyectos de la Solución

Este documento describe la arquitectura global del repositorio **OTPManager**, la responsabilidad de cada proyecto de la solución y cómo interactúan sus componentes.

---

## 1. Estructura de la Solución

La solución `OTPManager.sln` se compone de cuatro proyectos principales:

```
OTPManager/
├── OTPManager.Desktop/         # Aplicación de escritorio moderna (WPF, .NET 8)
├── OTPManager.Shared/          # Biblioteca de lógica de negocio y modelos (.NET Standard 2.0)
├── OTPManager.UWP/             # Versión original Universal Windows Platform (UWP)
├── OTPManager.Shared.Test/     # Pruebas unitarias
└── docs/                       # Documentación técnica
```

---

## 2. Proyectos y Responsabilidades

### A. `OTPManager.Desktop` (.NET 8 Windows - WPF)
Es la versión principal y moderna para Windows 10/11:
* **Framework:** `.NET 8.0 (net8.0-windows)`
* **Tecnología UI:** WPF (XAML) con estilos modernos, tema fluido y soporte DPI consciente.
* **Componentes clave:**
  * [`MainWindow.xaml / .cs`](../OTPManager.Desktop/MainWindow.xaml.cs): Ventana principal con listado en vivo de códigos, barra de búsqueda, chips de filtrado por etiquetas (`#tags`), contador visual de 30 segundos y botones de acción rápida.
  * [`DesktopStorageService.cs`](../OTPManager.Desktop/Services/DesktopStorageService.cs): Capa de persistencia local en `%LOCALAPPDATA%\OTPManager\data.db` cifrada con SQLCipher y clave protegida por Windows DPAPI.
  * [`QRCodeScannerService.cs`](../OTPManager.Desktop/Services/QRCodeScannerService.cs): Motor de decodificación de códigos QR mediante `ZXing.Net`.
  * **Vistas secundarias (`Views/`):**
    * [`AddAccountDialog`](../OTPManager.Desktop/Views/AddAccountDialog.xaml.cs): Diálogo para añadir o editar cuentas, con selector de algoritmos (SHA1, SHA256, SHA512), dígitos (6 u 8), protección contra edición accidental de secretos y botones de captura QR.
    * [`CameraScannerWindow`](../OTPManager.Desktop/Views/CameraScannerWindow.xaml.cs): Escaneo en vivo con webcam mediante `FlashCap`.
    * [`ScreenSnippingWindow`](../OTPManager.Desktop/Views/ScreenSnippingWindow.xaml.cs): Herramienta de recorte de pantalla que congela el escritorio multimonitor y detecta códigos QR en la selección.
    * [`PasswordDialog`](../OTPManager.Desktop/Views/PasswordDialog.xaml.cs): Solicitud de contraseña para copias de seguridad.

---

### B. `OTPManager.Shared` (.NET Standard 2.0)
Contiene la lógica agnóstica compartida entre las diferentes interfaces de usuario:
* **Modelos:**
  * [`OTPGenerator.cs`](../OTPManager.Shared/Models/OTPGenerator.cs): Entidad que representa una cuenta TOTP. Contiene propiedades como `Uid`, `Label`, `Issuer`, `Tags`, `Secret` (bytes y Base32), `AlgorithmName` y `NumDigits`.
* **Componentes:**
  * [`OTPUriConverter.cs`](../OTPManager.Shared/Components/OTPUriConverter.cs): Parser y serializador del protocolo estándar `otpauth://totp/...`.
  * [`Encryptor.cs`](../OTPManager.Shared/Components/Encryptor.cs): Utilidades de cifrado simétrico AES con derivación de claves mediante PBKDF2.
* **Librerías externas:**
  * `Otp.NET`: Generación matemática del algoritmo TOTP ([RFC 6238](https://tools.ietf.org/html/rfc6238)) y HOTP ([RFC 4226](https://tools.ietf.org/html/rfc4226)).
  * `sqlite-net-sqlcipher`: ORM liviano SQLite con soporte de cifrado de base de datos.

---

### C. `OTPManager.UWP` (Universal Windows Platform)
* Implementación histórica para la Microsoft Store basada en UWP y MvvmCross.
* Mantiene compatibilidad con el entorno sandbox de Windows 10/11 y `Windows.Storage.ApplicationData`.

---

### D. `OTPManager.Shared.Test`
* Pruebas unitarias que validan la generación precisa de códigos TOTP contra vectores de prueba RFC, parsing de URIs y serialización.

---

## 3. Flujo de Datos y Generación de Códigos TOTP

```
[ Base de Datos: data.db (Cifrada) ]
                │
                ▼ (DesktopStorageService)
      List<OTPGenerator>
                │
                ▼ (Mapeo a ViewModel/DisplayItem)
      List<OTPDisplayItem>
                │
                ├──> DispatcherTimer (Tick cada 100ms)
                │         └──> OTPDisplayItem.UpdateOTP(DateTime.UtcNow)
                │                   └──> OtpNet.Totp.ComputeTotp()
                │
                ▼
      [ Interfaz de Usuario WPF ]
   (Visualización de código formateado: "123 456" + Barra de cuenta regresiva)
```

1. **Al iniciar la app**, `DesktopStorageService` lee las cuentas almacenadas en la base de datos SQLite cifrada.
2. Cada entidad `OTPGenerator` se envuelve en un objeto reactivo `OTPDisplayItem` (`INotifyPropertyChanged`).
3. Un `DispatcherTimer` en `MainWindow` se ejecuta cada 100 milisegundos:
   * Calcula el segundo actual dentro del intervalo de 30 segundos (`30 - (DateTime.UtcNow.Second % 30)`).
   * Actualiza el arco / barra de progreso visual.
   * Si cambia el paso de tiempo, recalcula el código TOTP llamando a `UpdateOTP()`.
   * Formatea la cadena para facilitar la lectura (separando en grupos de 3 o 4 dígitos).

---

## 4. Gestión de Etiquetas (#tags)

La aplicación soporta clasificación por etiquetas:
* Las etiquetas se almacenan como texto normalizado en la propiedad `Tags` de `OTPGenerator` (ej. `#trabajo #bancos #correo`).
* El método `NormalizeTags` limpia espacios, comas y puntos y comas, anteponiendo `#` a cada término.
* En `MainWindow`, se analizan dinámicamente todas las etiquetas activas y se renderiza una barra de chips superiores para filtrar el listado con un solo clic.
