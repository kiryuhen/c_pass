using Newtonsoft.Json;
using PasswordManager.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace PasswordManager.Services
{
    /// <summary>
    /// Сервис для хранения данных приложения
    /// </summary>
    public class StorageService
    {
        private readonly EncryptionService _encryptionService;
        private readonly string _dataDirectoryPath;
        private readonly string _vaultFileName = "vault.dat";
        private readonly string _masterPasswordFileName = "master.key";

        /// <summary>
        /// Инициализирует новый экземпляр сервиса хранения
        /// </summary>
        /// <param name="encryptionService">Сервис шифрования</param>
        public StorageService(EncryptionService encryptionService)
        {
            _encryptionService = encryptionService;

            try
            {
                // Создаем директорию для данных в более доступном месте
                _dataDirectoryPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "PasswordManager");

                // Создаем директорию, если она не существует
                if (!Directory.Exists(_dataDirectoryPath))
                {
                    try
                    {
                        Directory.CreateDirectory(_dataDirectoryPath);
                    }
                    catch (Exception ex)
                    {
                        string message = $"Не удалось создать директорию для хранения данных: {ex.Message}";
                        MessageBox.Show(message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                        
                        // Как запасной вариант, используем папку рядом с приложением
                        _dataDirectoryPath = Path.Combine(
                            Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory) ?? ".",
                            "PasswordManagerData");
                        
                        // Пробуем создать и эту директорию
                        if (!Directory.Exists(_dataDirectoryPath))
                        {
                            Directory.CreateDirectory(_dataDirectoryPath);
                        }
                    }
                }
                
                // Проверяем права доступа к директории
                TestDirectoryAccess();
            }
            catch (Exception ex)
            {
                string message = $"Критическая ошибка при инициализации хранилища: {ex.Message}";
                MessageBox.Show(message, "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Пробрасываем исключение дальше, так как без хранилища работа невозможна
            }
        }

        /// <summary>
        /// Проверяет права доступа к директории хранения данных
        /// </summary>
        private void TestDirectoryAccess()
        {
            try
            {
                string testPath = Path.Combine(_dataDirectoryPath, "test_access.tmp");
                File.WriteAllText(testPath, "test");
                File.Delete(testPath);
            }
            catch (Exception ex)
            {
                string message = $"Нет прав на запись в директорию хранения данных: {ex.Message}\n" +
                                 "Пожалуйста, переместите приложение в другую папку или запустите с правами администратора.";
                MessageBox.Show(message, "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Пробрасываем исключение дальше
            }
        }

        /// <summary>
        /// Проверяет, установлен ли мастер-пароль
        /// </summary>
        /// <returns>true, если мастер-пароль установлен, иначе false</returns>
        public bool IsMasterPasswordSet()
        {
            string masterPasswordPath = Path.Combine(_dataDirectoryPath, _masterPasswordFileName);
            return File.Exists(masterPasswordPath);
        }

        /// <summary>
        /// Проверяет, правильный ли мастер-пароль
        /// </summary>
        /// <param name="masterPassword">Мастер-пароль для проверки</param>
        /// <returns>true, если пароль верный, иначе false</returns>
        public bool VerifyMasterPassword(string masterPassword)
        {
            string masterPasswordPath = Path.Combine(_dataDirectoryPath, _masterPasswordFileName);
            
            if (!File.Exists(masterPasswordPath))
                return false;

            try
            {
                string hashedPassword = File.ReadAllText(masterPasswordPath);
                return _encryptionService.VerifyMasterPassword(hashedPassword, masterPassword);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при проверке мастер-пароля: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }
        }

        /// <summary>
        /// Устанавливает новый мастер-пароль
        /// </summary>
        /// <param name="masterPassword">Новый мастер-пароль</param>
        public void SetMasterPassword(string masterPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(masterPassword))
                    throw new ArgumentException("Мастер-пароль не может быть пустым", nameof(masterPassword));

                string hashedPassword = _encryptionService.HashMasterPassword(masterPassword);
                string masterPasswordPath = Path.Combine(_dataDirectoryPath, _masterPasswordFileName);
                
                File.WriteAllText(masterPasswordPath, hashedPassword);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при установке мастер-пароля: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Пробрасываем исключение дальше
            }
        }

        /// <summary>
        /// Сохраняет список паролей в хранилище
        /// </summary>
        /// <param name="entries">Список записей паролей</param>
        /// <param name="masterPassword">Мастер-пароль для шифрования</param>
        public async Task SavePasswordEntries(List<PasswordEntry> entries, string masterPassword)
        {
            try
            {
                string vaultPath = Path.Combine(_dataDirectoryPath, _vaultFileName);
                
                // Сериализуем список паролей в JSON
                string jsonData = JsonConvert.SerializeObject(entries);
                
                // Шифруем JSON-данные
                string encryptedData = _encryptionService.Encrypt(jsonData, masterPassword);
                
                // Сохраняем зашифрованные данные
                await File.WriteAllTextAsync(vaultPath, encryptedData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении паролей: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Пробрасываем исключение дальше
            }
        }

        /// <summary>
        /// Загружает список паролей из хранилища
        /// </summary>
        /// <param name="masterPassword">Мастер-пароль для дешифрования</param>
        /// <returns>Список записей паролей</returns>
        public async Task<List<PasswordEntry>> LoadPasswordEntries(string masterPassword)
        {
            string vaultPath = Path.Combine(_dataDirectoryPath, _vaultFileName);
            
            if (!File.Exists(vaultPath))
                return new List<PasswordEntry>();

            try
            {
                // Читаем зашифрованные данные
                string encryptedData = await File.ReadAllTextAsync(vaultPath);
                
                // Дешифруем данные
                string jsonData = _encryptionService.Decrypt(encryptedData, masterPassword);
                
                // Десериализуем JSON в список паролей
                return JsonConvert.DeserializeObject<List<PasswordEntry>>(jsonData) ?? new List<PasswordEntry>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при загрузке паролей: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return new List<PasswordEntry>(); // Возвращаем пустой список в случае ошибки
            }
        }

        /// <summary>
        /// Добавляет запись пароля в хранилище
        /// </summary>
        /// <param name="entry">Запись пароля</param>
        /// <param name="password">Пароль в открытом виде</param>
        /// <param name="masterPassword">Мастер-пароль</param>
        /// <returns>Обновленный список записей паролей</returns>
        public async Task<List<PasswordEntry>> AddPasswordEntry(PasswordEntry entry, string password, string masterPassword)
        {
            try
            {
                // Загружаем текущие записи
                var entries = await LoadPasswordEntries(masterPassword);
                
                // Шифруем пароль
                entry.EncryptedPassword = _encryptionService.Encrypt(password, masterPassword);
                
                // Добавляем новую запись
                entries.Add(entry);
                
                // Сохраняем обновленный список
                await SavePasswordEntries(entries, masterPassword);
                
                return entries;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при добавлении пароля: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Пробрасываем исключение дальше
            }
        }

        /// <summary>
        /// Удаляет запись пароля из хранилища
        /// </summary>
        /// <param name="entryId">Идентификатор записи для удаления</param>
        /// <param name="masterPassword">Мастер-пароль</param>
        /// <returns>Обновленный список записей паролей</returns>
        public async Task<List<PasswordEntry>> DeletePasswordEntry(Guid entryId, string masterPassword)
        {
            try
            {
                // Загружаем текущие записи
                var entries = await LoadPasswordEntries(masterPassword);
                
                // Удаляем запись по идентификатору
                var entryToRemove = entries.FirstOrDefault(e => e.Id == entryId);
                
                if (entryToRemove != null)
                {
                    entries.Remove(entryToRemove);
                    
                    // Сохраняем обновленный список
                    await SavePasswordEntries(entries, masterPassword);
                }
                
                return entries;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при удалении пароля: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return await LoadPasswordEntries(masterPassword); // Возвращаем текущий список в случае ошибки
            }
        }

        /// <summary>
        /// Обновляет запись пароля в хранилище
        /// </summary>
        /// <param name="updatedEntry">Обновленная запись</param>
        /// <param name="newPassword">Новый пароль в открытом виде (null, если пароль не меняется)</param>
        /// <param name="masterPassword">Мастер-пароль</param>
        /// <returns>Обновленный список записей паролей</returns>
        public async Task<List<PasswordEntry>> UpdatePasswordEntry(PasswordEntry updatedEntry, string? newPassword, string masterPassword)
        {
            try
            {
                // Загружаем текущие записи
                var entries = await LoadPasswordEntries(masterPassword);
                
                // Находим запись для обновления
                var existingEntry = entries.FirstOrDefault(e => e.Id == updatedEntry.Id);
                
                if (existingEntry != null)
                {
                    // Обновляем свойства
                    existingEntry.ServiceName = updatedEntry.ServiceName;
                    existingEntry.Notes = updatedEntry.Notes;
                    existingEntry.ModifiedAt = DateTime.Now;
                    
                    // Обновляем пароль, если он был изменен
                    if (newPassword != null)
                    {
                        existingEntry.EncryptedPassword = _encryptionService.Encrypt(newPassword, masterPassword);
                    }
                    
                    // Сохраняем обновленный список
                    await SavePasswordEntries(entries, masterPassword);
                }
                
                return entries;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обновлении пароля: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return await LoadPasswordEntries(masterPassword); // Возвращаем текущий список в случае ошибки
            }
        }

        /// <summary>
        /// Получает пароль в открытом виде для указанной записи
        /// </summary>
        /// <param name="entryId">Идентификатор записи</param>
        /// <param name="masterPassword">Мастер-пароль</param>
        /// <returns>Пароль в открытом виде</returns>
        public async Task<string> GetDecryptedPassword(Guid entryId, string masterPassword)
        {
            try
            {
                // Загружаем записи
                var entries = await LoadPasswordEntries(masterPassword);
                
                // Находим нужную запись
                var entry = entries.FirstOrDefault(e => e.Id == entryId);
                
                if (entry == null)
                    throw new Exception("Запись пароля не найдена");

                // Дешифруем пароль
                return _encryptionService.Decrypt(entry.EncryptedPassword, masterPassword);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при получении пароля: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return string.Empty; // Возвращаем пустую строку в случае ошибки
            }
        }
    }
}