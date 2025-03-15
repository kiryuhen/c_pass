using System;

namespace PasswordManager.Models
{
    /// <summary>
    /// Модель для хранения информации о пароле
    /// </summary>
    public class PasswordEntry
    {
        /// <summary>
        /// Уникальный идентификатор записи
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Название сервиса или сайта, для которого создан пароль
        /// </summary>
        public string ServiceName { get; set; } = string.Empty;

        /// <summary>
        /// Зашифрованный пароль
        /// </summary>
        public string EncryptedPassword { get; set; } = string.Empty;

        /// <summary>
        /// Дополнительные заметки
        /// </summary>
        public string Notes { get; set; } = string.Empty;

        /// <summary>
        /// Дата создания записи
        /// </summary>
        public DateTime CreatedAt { get; set; }

        /// <summary>
        /// Дата последнего изменения записи
        /// </summary>
        public DateTime ModifiedAt { get; set; }

        /// <summary>
        /// Создает новую запись пароля
        /// </summary>
        public PasswordEntry()
        {
            Id = Guid.NewGuid();
            CreatedAt = DateTime.Now;
            ModifiedAt = DateTime.Now;
        }
    }
}