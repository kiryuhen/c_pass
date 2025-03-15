namespace PasswordManager.Models
{
    /// <summary>
    /// Модель для настройки параметров генерации пароля
    /// </summary>
    public class PasswordOptions
    {
        /// <summary>
        /// Длина пароля
        /// </summary>
        public int Length { get; set; } = 12;

        /// <summary>
        /// Использовать заглавные буквы (A-Z)
        /// </summary>
        public bool IncludeUppercase { get; set; } = true;

        /// <summary>
        /// Использовать строчные буквы (a-z)
        /// </summary>
        public bool IncludeLowercase { get; set; } = true;

        /// <summary>
        /// Использовать цифры (0-9)
        /// </summary>
        public bool IncludeNumbers { get; set; } = true;

        /// <summary>
        /// Использовать специальные символы (!@#$%^&*()_+-=[]{}|;:,.<>?/)
        /// </summary>
        public bool IncludeSpecialChars { get; set; } = true;

        /// <summary>
        /// Исключать похожие символы (il1|oO0)
        /// </summary>
        public bool ExcludeSimilarChars { get; set; } = false;

        /// <summary>
        /// Исключать неоднозначные символы (`~;:'"\)
        /// </summary>
        public bool ExcludeAmbiguousChars { get; set; } = false;

        /// <summary>
        /// Создает настройки для шаблона "Высокая безопасность"
        /// </summary>
        public static PasswordOptions HighSecurity()
        {
            return new PasswordOptions
            {
                Length = 20,
                IncludeUppercase = true,
                IncludeLowercase = true,
                IncludeNumbers = true,
                IncludeSpecialChars = true,
                ExcludeSimilarChars = false,
                ExcludeAmbiguousChars = false
            };
        }

        /// <summary>
        /// Создает настройки для шаблона "Средняя безопасность"
        /// </summary>
        public static PasswordOptions MediumSecurity()
        {
            return new PasswordOptions
            {
                Length = 12,
                IncludeUppercase = true,
                IncludeLowercase = true,
                IncludeNumbers = true,
                IncludeSpecialChars = true,
                ExcludeSimilarChars = true,
                ExcludeAmbiguousChars = true
            };
        }
    }
}