# Guía de Desarrollo y Compilación

Este documento contiene los requisitos técnicos, pasos para compilar, depurar y publicar la aplicación **OTPManager**, así como pautas para realizar futuras modificaciones.

---

## 1. Requisitos del Entorno

* **Sistema Operativo:** Windows 10 (versión 1809 o superior) o Windows 11.
* **SDK:** [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) (o posterior) con la carga de trabajo de escritorio para Windows (`windowsdesktop`).
* **Entornos de desarrollo recomendados:**
  * **Visual Studio 2022** (versión 17.8 o superior) con la carga de trabajo *.NET Desktop Development* (Desarrollo de escritorio de .NET).
  * **Visual Studio Code** con la extensión de C# Dev Kit.
  * **JetBrains Rider**.

---

## 2. Compilación y Ejecución desde la Consola

Puedes compilar y ejecutar el proyecto principal directamente con la CLI de .NET:

### Compilar la solución completa:
```powershell
dotnet build OTPManager.sln
```

### Ejecutar la aplicación de escritorio (`OTPManager.Desktop`):
```powershell
dotnet run --project OTPManager.Desktop/OTPManager.Desktop.csproj
```

### Ejecutar las pruebas unitarias:
```powershell
dotnet test OTPManager.Shared.Test/OTPManager.Shared.Test.csproj
```

---

## 3. Publicación y Empaquetado

Para generar un ejecutable listo para distribución (por ejemplo, para compartirlo o usarlo en otra máquina sin requerir la instalación previa del SDK de .NET):

### Generar ejecutable autocontenido de un solo archivo (Single-File):
```powershell
dotnet publish OTPManager.Desktop/OTPManager.Desktop.csproj `
  -c Release `
  -r win-x64 `
  --self-contained true `
  -p:PublishSingleFile=true `
  -p:IncludeNativeLibrariesForSelfExtract=true
```

El archivo `.exe` compilado se generará en:
`OTPManager.Desktop/bin/Release/net8.0-windows/win-x64/publish/`

---

## 4. Pautas para Desarrolladores

### Modificación de Modelos y Base de Datos
* Las entidades almacenadas en SQLite residen en `OTPManager.Shared/Models/`.
* La versión de escritorio utiliza `sqlite-net` con conexión asíncrona (`SQLiteAsyncConnection`).
* Si agregas nuevas propiedades a [`OTPGenerator.cs`](../OTPManager.Shared/Models/OTPGenerator.cs):
  * Mantén tipos compatibles con SQLite (`string`, `int`, `byte[]`, etc.).
  * Si la propiedad es calculada o no debe guardarse en la base de datos, agrégale el atributo `[Ignore]`.
  * Si no debe serializarse en exportaciones JSON, agrégale `[JsonIgnore]`.
  * Recuerda consultar [`docs/almacenamiento-y-persistencia.md`](almacenamiento-y-persistencia.md) antes de renombrar columnas existentes.

### Hilo de UI y Concurrencia
* Las operaciones en `DesktopStorageService` están protegidas por un `SemaphoreSlim(1, 1)` para evitar accesos concurrentes a la base de datos SQLite cifrada.
* Toda actualización de controles visuales en WPF debe realizarse en el hilo de la interfaz de usuario (`Dispatcher.Invoke` o eventos asociados al `DispatcherTimer`).
