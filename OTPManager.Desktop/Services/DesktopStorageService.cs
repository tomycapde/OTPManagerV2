using OTPManager.Shared.Components;
using OTPManager.Shared.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace OTPManager.Desktop.Services
{
    public class DesktopStorageService
    {
        private const string PasswordSalt = "bz77KNXdP,Bc4Acg";
        private const string DbFileName = "data.db";
        private const string KeyFileName = "vault.key";

        private readonly string dataDir;
        private readonly string dbPath;
        private readonly string keyPath;
        private readonly SemaphoreSlim mutex = new SemaphoreSlim(1, 1);
        private SQLiteAsyncConnection? connection;

        public DesktopStorageService()
        {
            dataDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OTPManager");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            dbPath = Path.Combine(dataDir, DbFileName);
            keyPath = Path.Combine(dataDir, KeyFileName);
        }

        private byte[] GetOrCreateKey()
        {
            if (File.Exists(keyPath))
            {
                try
                {
                    var encryptedKey = File.ReadAllBytes(keyPath);
                    return ProtectedData.Unprotect(encryptedKey, null, DataProtectionScope.CurrentUser);
                }
                catch
                {
                    // If decryption fails, generate a new key
                }
            }

            var newKey = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(newKey);
            }

            var protectedBytes = ProtectedData.Protect(newKey, null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(keyPath, protectedBytes);
            return newKey;
        }

        private async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (connection == null)
            {
                var key = GetOrCreateKey();
                var connString = new SQLiteConnectionString(dbPath, true, key: key);
                connection = new SQLiteAsyncConnection(connString);
                await connection.CreateTableAsync<OTPGenerator>();
            }

            return connection;
        }

        public async Task<List<OTPGenerator>> GetAllAsync()
        {
            await mutex.WaitAsync();
            try
            {
                var conn = await GetConnectionAsync();
                var list = await conn.Table<OTPGenerator>().ToListAsync();
                return list.OrderBy(d => !string.IsNullOrWhiteSpace(d.Label) ? d.Label : d.Issuer, StringComparer.CurrentCultureIgnoreCase).ToList();
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task<int> InsertOrReplaceAsync(OTPGenerator item)
        {
            await mutex.WaitAsync();
            try
            {
                var conn = await GetConnectionAsync();
                return await conn.InsertOrReplaceAsync(item);
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task<int> DeleteAsync(OTPGenerator item)
        {
            await mutex.WaitAsync();
            try
            {
                var conn = await GetConnectionAsync();
                return await conn.DeleteAsync(item);
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task ClearAsync()
        {
            await mutex.WaitAsync();
            try
            {
                var conn = await GetConnectionAsync();
                await conn.DropTableAsync<OTPGenerator>();
                await conn.CreateTableAsync<OTPGenerator>();
            }
            finally
            {
                mutex.Release();
            }
        }

        public async Task<MemoryStream> DumpAsync(string password)
        {
            password = password ?? string.Empty;
            var data = await GetAllAsync();
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(data);
            var jsonBytes = Encoding.UTF8.GetBytes(json);
            var payload = Encryptor.SimpleEncryptWithPassword(jsonBytes, password + PasswordSalt);
            return new MemoryStream(payload);
        }

        public async Task<bool> RestoreAsync(Stream data, string password)
        {
            password = password ?? string.Empty;
            if (data.CanSeek && data.Position != 0)
            {
                data.Position = 0;
            }

            byte[] payload;
            using (var ms = new MemoryStream())
            {
                await data.CopyToAsync(ms);
                payload = ms.ToArray();
            }

            if (payload == null || payload.Length == 0) return false;

            byte[] jsonBytes;
            try
            {
                jsonBytes = Encryptor.SimpleDecryptWithPassword(payload, password + PasswordSalt);
            }
            catch
            {
                return false;
            }

            if (jsonBytes == null) return false;

            OTPGenerator[]? items;
            try
            {
                var json = Encoding.UTF8.GetString(jsonBytes);
                items = Newtonsoft.Json.JsonConvert.DeserializeObject<OTPGenerator[]>(json);
            }
            catch
            {
                return false;
            }

            if (items == null || !items.Any()) return false;

            await ClearAsync();
            foreach (var item in items)
            {
                await InsertOrReplaceAsync(item);
            }

            return true;
        }
    }
}
