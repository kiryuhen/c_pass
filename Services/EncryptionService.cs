using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services
{
    /// <summary>
    /// Сервис для шифрования и дешифрования данных с использованием AES
    /// </summary>
    public class EncryptionService
    {
        // Количество итераций при генерации ключа
        private const int Iterations = 10000;
        
        // Размер соли в байтах
        private const int SaltSize = 16;
        
        // Размер вектора инициализации в байтах
        private const int IvSize = 16;
        
        // Размер ключа шифрования в битах (256 бит = 32 байта)
        private const int KeySize = 256;

        /// <summary>
        /// Шифрует строку с использованием мастер-пароля
        /// </summary>
        /// <param name="plainText">Строка для шифрования</param>
        /// <param name="masterPassword">Мастер-пароль</param>
        /// <returns>Зашифрованная строка в Base64</returns>
        public string Encrypt(string plainText, string masterPassword)
        {
            if (string.IsNullOrEmpty(plainText))
                return string.Empty;

            if (string.IsNullOrEmpty(masterPassword))
                throw new ArgumentException("Мастер-пароль не может быть пустым", nameof(masterPassword));

            try
            {
                // Генерируем соль
                byte[] salt = GenerateRandomBytes(SaltSize);
                
                // Генерируем вектор инициализации
                byte[] iv = GenerateRandomBytes(IvSize);
                
                // Получаем ключ на основе пароля и соли
                byte[] key = GenerateKey(masterPassword, salt);

                // Шифруем данные
                byte[] encryptedData;
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var encryptor = aes.CreateEncryptor())
                    using (var memoryStream = new MemoryStream())
                    {
                        using (var cryptoStream = new CryptoStream(memoryStream, encryptor, CryptoStreamMode.Write))
                        using (var writer = new StreamWriter(cryptoStream))
                        {
                            writer.Write(plainText);
                        }
                        encryptedData = memoryStream.ToArray();
                    }
                }

                // Формируем результат: соль + IV + зашифрованные данные
                byte[] resultBytes = new byte[SaltSize + IvSize + encryptedData.Length];
                Buffer.BlockCopy(salt, 0, resultBytes, 0, SaltSize);
                Buffer.BlockCopy(iv, 0, resultBytes, SaltSize, IvSize);
                Buffer.BlockCopy(encryptedData, 0, resultBytes, SaltSize + IvSize, encryptedData.Length);

                // Возвращаем закодированный в Base64 результат
                return Convert.ToBase64String(resultBytes);
            }
            catch (Exception ex)
            {
                throw new CryptographicException($"Ошибка при шифровании данных: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// Дешифрует строку с использованием мастер-пароля
        /// </summary>
        /// <param name="encryptedText">Зашифрованная строка в Base64</param>
        /// <param name="masterPassword">Мастер-пароль</param>
        /// <returns>Расшифрованная строка</returns>
        public string Decrypt(string encryptedText, string masterPassword)
        {
            if (string.IsNullOrEmpty(encryptedText))
                return string.Empty;

            if (string.IsNullOrEmpty(masterPassword))
                throw new ArgumentException("Мастер-пароль не может быть пустым", nameof(masterPassword));

            try
            {
                // Декодируем Base64
                byte[] encryptedBytes = Convert.FromBase64String(encryptedText);

                // Проверяем минимальную длину
                if (encryptedBytes.Length < SaltSize + IvSize + 1)
                    throw new CryptographicException("Неверный формат зашифрованных данных");

                // Извлекаем соль
                byte[] salt = new byte[SaltSize];
                Buffer.BlockCopy(encryptedBytes, 0, salt, 0, SaltSize);

                // Извлекаем вектор инициализации
                byte[] iv = new byte[IvSize];
                Buffer.BlockCopy(encryptedBytes, SaltSize, iv, 0, IvSize);

                // Извлекаем зашифрованные данные
                int encryptedDataLength = encryptedBytes.Length - SaltSize - IvSize;
                byte[] encryptedData = new byte[encryptedDataLength];
                Buffer.BlockCopy(encryptedBytes, SaltSize + IvSize, encryptedData, 0, encryptedDataLength);

                // Получаем ключ на основе пароля и соли
                byte[] key = GenerateKey(masterPassword, salt);

                // Расшифровываем данные
                using (var aes = Aes.Create())
                {
                    aes.Key = key;
                    aes.IV = iv;
                    aes.Mode = CipherMode.CBC;
                    aes.Padding = PaddingMode.PKCS7;

                    using (var decryptor = aes.CreateDecryptor())
                    using (var memoryStream = new MemoryStream(encryptedData))
                    using (var cryptoStream = new CryptoStream(memoryStream, decryptor, CryptoStreamMode.Read))
                    using (var reader = new StreamReader(cryptoStream))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
            catch (CryptographicException ex)
            {
                throw new CryptographicException("Ошибка дешифрования. Возможно, неверный мастер-пароль.", ex);
            }
            catch (Exception ex)
            {
                throw new Exception("Ошибка дешифрования данных", ex);
            }
        }

        /// <summary>
        /// Проверяет, правильный ли мастер-пароль
        /// </summary>
        /// <param name="hashedPassword">Хешированный пароль для проверки</param>
        /// <param name="masterPassword">Введенный мастер-пароль</param>
        /// <returns>true, если пароль верный, иначе false</returns>
        public bool VerifyMasterPassword(string hashedPassword, string masterPassword)
        {
            try
            {
                // Декодируем Base64
                byte[] hashBytes = Convert.FromBase64String(hashedPassword);

                // Проверяем минимальную длину
                if (hashBytes.Length < SaltSize + 32) // 32 байта для SHA-256
                    return false;

                // Извлекаем соль
                byte[] salt = new byte[SaltSize];
                Buffer.BlockCopy(hashBytes, 0, salt, 0, SaltSize);

                // Извлекаем хеш
                byte[] hash = new byte[32];
                Buffer.BlockCopy(hashBytes, SaltSize, hash, 0, 32);

                // Генерируем хеш для введенного мастер-пароля
                byte[] computedHash = GenerateSHA256(masterPassword, salt);

                // Сравниваем хеши
                for (int i = 0; i < 32; i++)
                {
                    if (hash[i] != computedHash[i])
                        return false;
                }

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Хеширует мастер-пароль для последующей проверки
        /// </summary>
        /// <param name="masterPassword">Мастер-пароль</param>
        /// <returns>Хешированный пароль в Base64</returns>
        public string HashMasterPassword(string masterPassword)
        {
            if (string.IsNullOrEmpty(masterPassword))
                throw new ArgumentException("Мастер-пароль не может быть пустым", nameof(masterPassword));

            // Генерируем соль
            byte[] salt = GenerateRandomBytes(SaltSize);

            // Генерируем хеш
            byte[] hash = GenerateSHA256(masterPassword, salt);

            // Формируем результат: соль + хеш
            byte[] resultBytes = new byte[SaltSize + hash.Length];
            Buffer.BlockCopy(salt, 0, resultBytes, 0, SaltSize);
            Buffer.BlockCopy(hash, 0, resultBytes, SaltSize, hash.Length);

            // Возвращаем закодированный в Base64 результат
            return Convert.ToBase64String(resultBytes);
        }

        /// <summary>
        /// Генерирует случайные байты
        /// </summary>
        /// <param name="length">Длина в байтах</param>
        /// <returns>Массив случайных байтов</returns>
        private byte[] GenerateRandomBytes(int length)
        {
            byte[] bytes = new byte[length];
            RandomNumberGenerator.Fill(bytes);
            return bytes;
        }

        /// <summary>
        /// Генерирует ключ шифрования из пароля и соли
        /// </summary>
        /// <param name="password">Пароль</param>
        /// <param name="salt">Соль</param>
        /// <returns>Ключ шифрования</returns>
        private byte[] GenerateKey(string password, byte[] salt)
        {
            using (var deriveBytes = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256))
            {
                return deriveBytes.GetBytes(KeySize / 8);
            }
        }

        /// <summary>
        /// Генерирует хеш SHA-256 из пароля и соли
        /// </summary>
        /// <param name="password">Пароль</param>
        /// <param name="salt">Соль</param>
        /// <returns>Хеш SHA-256</returns>
        private byte[] GenerateSHA256(string password, byte[] salt)
        {
            byte[] passwordBytes = Encoding.UTF8.GetBytes(password);
            byte[] passwordWithSalt = new byte[passwordBytes.Length + salt.Length];
            
            Buffer.BlockCopy(passwordBytes, 0, passwordWithSalt, 0, passwordBytes.Length);
            Buffer.BlockCopy(salt, 0, passwordWithSalt, passwordBytes.Length, salt.Length);

            using (var sha256 = SHA256.Create())
            {
                return sha256.ComputeHash(passwordWithSalt);
            }
        }
    }
}