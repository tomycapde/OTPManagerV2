# Almacenamiento, Seguridad y Persistencia de Datos

Este documento describe cómo gestiona el almacenamiento y cifrado la aplicación **OTPManager.Desktop**, dónde se ubican los archivos, cómo se preservan los datos al actualizar la app y cómo transferirlos entre computadoras.

---

## 1. Ubicación de la Base de Datos

Los datos de la versión de escritorio se guardan en el directorio de datos locales de la cuenta de usuario de Windows:

* **Variable de entorno:** `%LOCALAPPDATA%\OTPManager\`
* **Ruta física habitual:** `C:\Users\<TuUsuario>\AppData\Local\OTPManager\`

Dentro de este directorio se encuentran dos archivos fundamentales:

| Archivo | Descripción |
| :--- | :--- |
| `data.db` | Base de datos SQLite cifrada que contiene todas las cuentas, etiquetas y secretos OTP. |
| `vault.key` | Clave de cifrado simétrica aleatoria (256 bits) generada en el primer inicio de la app. |

---

## 2. Mecanismo de Seguridad y Cifrado

El servicio [`DesktopStorageService`](../OTPManager.Desktop/Services/DesktopStorageService.cs) implementa un modelo de seguridad de dos niveles:

1. **Cifrado de la base de datos:**
   * La base de datos SQLite se abre utilizando la clave binaria de `vault.key`.
   * Toda la información (secretos TOTP, etiquetas, emisores) se almacena cifrada en disco.

2. **Protección de la clave con Windows DPAPI:**
   * `vault.key` no se almacena en texto plano. Se cifra utilizando la API de Windows **Data Protection API (DPAPI)**:
     ```csharp
     ProtectedData.Protect(newKey, null, DataProtectionScope.CurrentUser);
     ```
   * **¿Qué significa esto?** La clave solo puede ser descifrada por el mismo usuario de Windows en la misma máquina física. Si otro usuario del mismo equipo o un atacante copia el archivo `vault.key`, no podrá descifrarlo.

---

## 3. Persistencia entre Modificaciones y Nuevas Versiones

### ¿Se mantiene la base de datos si actualizo o recompilo la app?
**Sí, se mantiene de forma 100% transparente.**

* **Ubicación independiente del ejecutable:** La base de datos no está en la carpeta de compilación (`bin\Debug`, `bin\Release`) ni junto al archivo `.exe`. Por ende, recompilar, limpiar la solución o sustituir el `.exe` por una versión nueva no toca la carpeta `%LOCALAPPDATA%\OTPManager\`.
* **Persistencia de la clave:** Al ejecutarse con la misma cuenta de Windows, DPAPI desbloquea `vault.key` automáticamente en cualquier versión del ejecutable.
* **Compatibilidad de esquemas (`sqlite-net`):** En el inicio, la app ejecuta:
  ```csharp
  await connection.CreateTableAsync<OTPGenerator>();
  ```
  `sqlite-net` es seguro para adición de campos: preserva los registros existentes y solo añade las columnas nuevas que se agreguen a la clase [`OTPGenerator`](../OTPManager.Shared/Models/OTPGenerator.cs).

### Precauciones para futuras modificaciones en el código
Si realizas cambios en el código fuente, ten en cuenta lo siguiente para no perder compatibilidad:
* **No cambiar los nombres de archivo:** Modificar `DbFileName = "data.db"` o `KeyFileName = "vault.key"` creará un nuevo almacén vacío a menos que implementes una migración.
* **No alterar `DataProtectionScope`:** Cambiar de `CurrentUser` a otro ámbito impedirá leer las claves existentes.
* **Renombrar columnas existentes:** Si renombras una propiedad existente en `OTPGenerator` (por ejemplo, cambiar `Label` por `Title`), `sqlite-net` creará una columna nueva con valores nulos y no migrará automáticamente los datos antiguos. Para estos casos, se debe realizar un script de migración SQL.

---

## 4. Migración a Otra Computadora (Copia de Seguridad)

> [!WARNING]
> **No copies directamente los archivos `data.db` y `vault.key` a otra máquina.**
> Debido al cifrado DPAPI, la clave `vault.key` está ligada a la instalación y usuario actual de Windows. En otra máquina o bajo otro usuario, DPAPI no podrá descifrar la clave y la base de datos no podrá abrirse.

### Procedimiento correcto para migrar datos:
1. **En el equipo actual:**
   * Abre la aplicación.
   * Haz clic en el botón **"Exportar copia de seguridad"** en la barra superior.
   * Ingresa una contraseña para proteger el archivo.
   * Guarda el archivo generado (`.otpm`).
2. **En el equipo nuevo:**
   * Instala/ejecuta la aplicación.
   * Haz clic en **"Importar copia de seguridad"**.
   * Selecciona el archivo `.otpm` e introduce la misma contraseña.
   * Las cuentas se restaurarán e integrarán en la base de datos del nuevo equipo.

---

## 5. Histórico: Versión UWP vs Desktop

* **Versión UWP histórica (`OTPManager.UWP`):** Almacenaba los datos en la carpeta aislada de la app UWP (`%LOCALAPPDATA%\Packages\<PackageId>\LocalState\Data_v2.db3`).
* **Versión Desktop actual (`OTPManager.Desktop`):** Utiliza `%LOCALAPPDATA%\OTPManager\data.db` con soporte para Windows Forms / WPF en .NET 8, lo que permite un despliegue libre de sandbox y mayor facilidad de respaldos.
