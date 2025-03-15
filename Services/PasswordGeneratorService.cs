using PasswordManager.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace PasswordManager.Services
{
    /// <summary>
    /// Сервис для генерации паролей
    /// </summary>
    public class PasswordGeneratorService
    {
        // Наборы символов
        private const string LowercaseChars = "abcdefghijklmnopqrstuvwxyz";
        private const string UppercaseChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
        private const string NumberChars = "0123456789";
        private const string SpecialChars = "!@#$%^&*()_+-=[]{}|;:,./<>?";
        private const string SimilarChars = "il1|oO0";
        private const string AmbiguousChars = "`~;:'\"\\";

        /// <summary>
        /// Генерирует пароль согласно заданным параметрам
        /// </summary>
        /// <param name="options">Параметры генерации пароля</param>
        /// <returns>Сгенерированный пароль</returns>
        public string GeneratePassword(PasswordOptions options)
        {
            // Проверка параметров
            if (options.Length <= 0)
                throw new ArgumentException("Длина пароля должна быть положительным числом", nameof(options.Length));

            // Сбор набора символов
            var charSet = new StringBuilder();
            
            if (options.IncludeLowercase)
                charSet.Append(LowercaseChars);
            
            if (options.IncludeUppercase)
                charSet.Append(UppercaseChars);
            
            if (options.IncludeNumbers)
                charSet.Append(NumberChars);
            
            if (options.IncludeSpecialChars)
                charSet.Append(SpecialChars);

            // Исключение символов при необходимости
            var finalCharSet = charSet.ToString();
            
            if (options.ExcludeSimilarChars)
                finalCharSet = RemoveChars(finalCharSet, SimilarChars);
            
            if (options.ExcludeAmbiguousChars)
                finalCharSet = RemoveChars(finalCharSet, AmbiguousChars);

            if (string.IsNullOrEmpty(finalCharSet))
                throw new InvalidOperationException("Нет символов для генерации пароля. Измените параметры.");

            // Генерация пароля
            var password = new StringBuilder(options.Length);
            var requiredCharTypes = new Dictionary<Func<char, bool>, bool>
            {
                { c => LowercaseChars.Contains(c), options.IncludeLowercase },
                { c => UppercaseChars.Contains(c), options.IncludeUppercase },
                { c => NumberChars.Contains(c), options.IncludeNumbers },
                { c => SpecialChars.Contains(c), options.IncludeSpecialChars }
            };

            // Генерируем начальный пароль
            for (int i = 0; i < options.Length; i++)
            {
                password.Append(GetRandomChar(finalCharSet));
            }

            // Проверяем, что пароль содержит хотя бы один символ каждого требуемого типа
            foreach (var charType in requiredCharTypes)
            {
                if (charType.Value && !password.ToString().Any(charType.Key))
                {
                    // Заменяем случайный символ на символ требуемого типа
                    int pos = GetRandomNumber(0, password.Length);
                    char newChar;
                    
                    do
                    {
                        newChar = GetRandomChar(finalCharSet);
                    } 
                    while (!charType.Key(newChar));
                    
                    password[pos] = newChar;
                }
            }

            return password.ToString();
        }

        /// <summary>
        /// Оценивает сложность пароля по шкале от 0 до 100
        /// </summary>
        /// <param name="password">Пароль для оценки</param>
        /// <returns>Оценка сложности пароля (0-100)</returns>
        public int EvaluatePasswordStrength(string password)
        {
            if (string.IsNullOrEmpty(password))
                return 0;

            int score = 0;
            
            // Длина пароля (максимум 30 баллов)
            score += Math.Min(30, password.Length * 2);

            // Разнообразие символов
            bool hasLower = password.Any(c => LowercaseChars.Contains(c));
            bool hasUpper = password.Any(c => UppercaseChars.Contains(c));
            bool hasNumber = password.Any(c => NumberChars.Contains(c));
            bool hasSpecial = password.Any(c => SpecialChars.Contains(c));

            int varietyScore = 0;
            if (hasLower) varietyScore += 10;
            if (hasUpper) varietyScore += 15;
            if (hasNumber) varietyScore += 15;
            if (hasSpecial) varietyScore += 20;
            
            score += varietyScore;

            // Сложная последовательность (для избежания повторений)
            int duplicateCount = password.GroupBy(c => c).Count(g => g.Count() > 1);
            score -= duplicateCount * 2;

            // Не допускаем отрицательное значение
            score = Math.Max(0, score);
            
            // Ограничиваем максимальное значение
            score = Math.Min(100, score);

            return score;
        }

        /// <summary>
        /// Получает случайный символ из набора
        /// </summary>
        /// <param name="charSet">Набор символов</param>
        /// <returns>Случайный символ</returns>
        private char GetRandomChar(string charSet)
        {
            int index = GetRandomNumber(0, charSet.Length);
            return charSet[index];
        }

        /// <summary>
        /// Генерирует криптографически безопасное случайное число в заданном диапазоне
        /// </summary>
        /// <param name="minValue">Минимальное значение (включительно)</param>
        /// <param name="maxValue">Максимальное значение (исключительно)</param>
        /// <returns>Случайное число</returns>
        private int GetRandomNumber(int minValue, int maxValue)
        {
            if (minValue >= maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue), "Минимальное значение должно быть меньше максимального");

            // Вычисляем количество байт, необходимых для представления (maxValue - minValue)
            int range = maxValue - minValue;
            int bytesNeeded = sizeof(uint);
            byte[] buffer = new byte[bytesNeeded];

            // Используем современный способ получения случайных чисел
            RandomNumberGenerator.Fill(buffer);
            uint scale = uint.MaxValue;
            uint result;

            result = BitConverter.ToUInt32(buffer, 0);
            return minValue + (int)(result % (uint)range);
        }

        /// <summary>
        /// Удаляет указанные символы из набора
        /// </summary>
        /// <param name="source">Исходный набор символов</param>
        /// <param name="charsToRemove">Символы для удаления</param>
        /// <returns>Новый набор символов без указанных символов</returns>
        private string RemoveChars(string source, string charsToRemove)
        {
            return new string(source.Where(c => !charsToRemove.Contains(c)).ToArray());
        }
    }
}