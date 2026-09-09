# OTPManagerV2

<p align="center">
  <img src="OTPManager.Desktop/Assets/icon.png" alt="OTPManagerV2 Logo" width="128" height="128" />
</p>

<p align="center">
  <strong>Generador y gestor de códigos de verificación en dos pasos (2FA / TOTP) moderno, rápido y seguro para Windows.</strong>
</p>

---

## 📌 Acerca de este proyecto

**OTPManagerV2** es la evolución y modernización del conocido gestor de tokens 2FA para Windows. Diseñado sobre **.NET 8 y WPF**, ofrece una experiencia de escritorio nativa, fluida y sin dependencias de servicios externos ni conexión a Internet.

> ### 🤝 Créditos y Atribución
> Este proyecto nace como una continuación y modernización del trabajo original **[OTPManager (V1)](https://github.com/Aftnet/OTPManager)** desarrollado por **[Aftnet](https://github.com/Aftnet)** para Windows 10 UWP. 
> 
> **OTPManagerV2** rescata la esencia de privacidad y sencillez del original, reimplementándolo como una aplicación de escritorio moderna con soporte ampliado de algoritmos, sistema de etiquetas, nuevas opciones de escaneo y cifrado robusto.

---

## ✨ Características Principales

* 🔒 **100% Sin conexión y privado:** Tus secretos nunca salen de tu equipo ni requieren servidores externos.
* 🛡️ **Seguridad y Cifrado:** Base de datos local SQLite protegida con SQLCipher y clave simétrica cifrada mediante Windows DPAPI (`CurrentUser`).
* 📷 **3 Métodos de Captura de Códigos QR:**
  * **Cámara Web:** Escaneo en vivo con detección instantánea.
  * **Recorte de Pantalla (Screen Snipping):** Congela la pantalla en entornos multimonitor para escanear cualquier QR visible.
  * **Portapapeles:** Autocompletado inmediato al copiar imágenes con códigos QR.
* 🏷️ **Clasificación por Etiquetas (`#tags`):** Organiza tus cuentas (ej. `#trabajo`, `#finanzas`, `#correo`) y fíltra con un solo clic mediante chips visuales.
* ⏱️ **Temporizador fluido en tiempo real:** Visualización del tiempo restante del código TOTP con cuenta regresiva visual y copiado al portapapeles con un solo clic.
* 💾 **Copia de seguridad cifrada:** Exporta e importa tus cuentas en archivos protegidos por contraseña (`.otpm`).
* ⚙️ **Soporte ampliado de algoritmos:** TOTP con algoritmos SHA1, SHA256 y SHA512, en variantes de 6 u 8 dígitos.

---

## 📚 Documentación Técnica

Para conocer los detalles de diseño, arquitectura y almacenamiento:

* 📖 **[Índice General de Documentación](docs/README.md)**
* 💾 **[Almacenamiento, Seguridad y Persistencia](docs/almacenamiento-y-persistencia.md)**
* 🏗️ **[Arquitectura y Proyectos de la Solución](docs/arquitectura-y-proyectos.md)**
* 📷 **[Captura y Escaneo de Códigos QR](docs/escaneo-y-captura-qr.md)**
* 🛠️ **[Guía de Desarrollo y Compilación](docs/guia-desarrollo-y-compilacion.md)**

---

## 🚀 Inicio Rápido (Compilación y Ejecución)

### Requisitos
* Windows 10 (1809+) o Windows 11.
* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

### Compilar y Ejecutar
```powershell
# Clonar el repositorio
git clone https://github.com/tomycapde/OTPManagerV2.git
cd OTPManagerV2

# Ejecutar la aplicación
dotnet run --project OTPManager.Desktop/OTPManager.Desktop.csproj
```

---

## 📄 Licencia

Este proyecto está disponible bajo la licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más información.
Basado en el trabajo original de [Aftnet](https://github.com/Aftnet).