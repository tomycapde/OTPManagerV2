# Índice de Documentación Técnica

Bienvenido al centro de documentación técnica de **OTPManager**. Aquí encontrarás guías detalladas sobre el diseño, funcionamiento, almacenamiento y desarrollo de la aplicación.

---

## Guías Disponibles

1. **[Almacenamiento, Seguridad y Persistencia de Datos](almacenamiento-y-persistencia.md)**
   * Dónde se guardan los datos en Windows (`%LOCALAPPDATA%\OTPManager\`).
   * Mecanismo de doble cifrado (SQLCipher + Windows DPAPI).
   * Persistencia de cuentas al compilar o actualizar a versiones más nuevas.
   * Exportación e importación segura de copias de seguridad (`.otpm`).

2. **[Arquitectura y Proyectos de la Solución](arquitectura-y-proyectos.md)**
   * Estructura modular (`OTPManager.Desktop`, `OTPManager.Shared`, `OTPManager.UWP`).
   * Modelos reactivos (`OTPDisplayItem`) y ciclo de refresco cada 100ms.
   * Sistema de clasificación por etiquetas (`#tags`).

3. **[Captura y Escaneo de Códigos QR](escaneo-y-captura-qr.md)**
   * Escaneo en vivo con cámara web (`FlashCap` + `ZXing.Net`).
   * Herramienta de recorte de pantalla multimonitor (`ScreenSnippingWindow`).
   * Autocompletado directo desde el portapapeles.
   * Procesamiento del protocolo `otpauth://totp/...`.

4. **[Guía de Desarrollo y Compilación](guia-desarrollo-y-compilacion.md)**
   * Requisitos (.NET 8 SDK, Windows 10/11).
   * Comandos para compilar y ejecutar por consola (`dotnet`).
   * Ejecución de pruebas unitarias.
   * Publicación de un ejecutable autocontenido (.exe único).
