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

## 🚀 Descarga e Instalación

No necesitas compilar el proyecto para empezar a usarlo. Puedes descargar la versión portable precompilada y autocontenida directamente desde la sección de lanzamientos:

👉 **[Descargar la última versión en GitHub Releases](https://github.com/tomycapde/OTPManagerV2/releases/latest)**

### Pasos rápidos:
1. Descarga el archivo `.zip` de la última versión disponible (ej. `OTPManagerV2-v1.2.0-win-x64.zip`).
2. Descomprímelo en cualquier carpeta de tu equipo.
3. Ejecuta **`OTPManagerV2.exe`**.

> 💡 **Nota:** La aplicación es **100% portable y autocontenida**. No requiere instalación previa ni tener instalado el SDK o Runtime de .NET en Windows.

---

## 🛠️ Desarrollo y Compilación

Si deseas compilar el código fuente por tu cuenta, depurar o contribuir al proyecto:

* Consulta la **[Guía de Desarrollo y Compilación](docs/guia-desarrollo-y-compilacion.md)** para conocer los requisitos previos, instrucciones para clonar y compilar con la CLI de .NET o Visual Studio, y los comandos para empaquetar ejecutables.

---

## 📄 Licencia

Este proyecto está disponible bajo la licencia **MIT**. Consulta el archivo [LICENSE](LICENSE) para más información.
Basado en el trabajo original de [Aftnet](https://github.com/Aftnet).