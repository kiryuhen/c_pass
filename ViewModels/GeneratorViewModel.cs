using PasswordManager.Models;
using PasswordManager.Services;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Input;

namespace PasswordManager.ViewModels
{
    /// <summary>
    /// ViewModel для генератора паролей
    /// </summary>
    public class GeneratorViewModel : BaseViewModel
    {
        private readonly PasswordGeneratorService _passwordGeneratorService;
        private readonly ClipboardService _clipboardService;

        // Параметры генерации пароля
        private PasswordOptions _passwordOptions;
        
        // Сгенерированный пароль
        private string _generatedPassword = string.Empty;
        
        // Оценка сложности пароля
        private int _passwordStrength;
        
        // Текстовое описание сложности пароля
        private string _passwordStrengthText = string.Empty;
        
        // Цвет индикатора сложности пароля
        private string _passwordStrengthColor = string.Empty;
        
        // Сообщение для пользователя
        private string _statusMessage = string.Empty;

        /// <summary>
        /// Параметры генерации пароля
        /// </summary>
        public PasswordOptions PasswordOptions
        {
            get => _passwordOptions;
            set
            {
                if (SetProperty(ref _passwordOptions, value))
                {
                    // При изменении параметров обновляем сгенерированный пароль
                    GeneratePassword();
                }
            }
        }

        /// <summary>
        /// Сгенерированный пароль
        /// </summary>
        public string GeneratedPassword
        {
            get => _generatedPassword;
            set
            {
                if (SetProperty(ref _generatedPassword, value))
                {
                    // При изменении пароля обновляем его оценку
                    UpdatePasswordStrength();
                }
            }
        }

        /// <summary>
        /// Оценка сложности пароля (0-100)
        /// </summary>
        public int PasswordStrength
        {
            get => _passwordStrength;
            set => SetProperty(ref _passwordStrength, value);
        }

        /// <summary>
        /// Текстовое описание сложности пароля
        /// </summary>
        public string PasswordStrengthText
        {
            get => _passwordStrengthText;
            set => SetProperty(ref _passwordStrengthText, value);
        }

        /// <summary>
        /// Цвет индикатора сложности пароля
        /// </summary>
        public string PasswordStrengthColor
        {
            get => _passwordStrengthColor;
            set => SetProperty(ref _passwordStrengthColor, value);
        }

        /// <summary>
        /// Сообщение для пользователя
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Команда генерации пароля
        /// </summary>
        public ICommand GeneratePasswordCommand { get; }

        /// <summary>
        /// Команда копирования пароля в буфер обмена
        /// </summary>
        public ICommand CopyPasswordCommand { get; }

        /// <summary>
        /// Команда применения шаблона высокой безопасности
        /// </summary>
        public ICommand ApplyHighSecurityTemplateCommand { get; }

        /// <summary>
        /// Команда применения шаблона средней безопасности
        /// </summary>
        public ICommand ApplyMediumSecurityTemplateCommand { get; }

        /// <summary>
        /// Инициализирует новый экземпляр GeneratorViewModel
        /// </summary>
        /// <param name="passwordGeneratorService">Сервис генерации паролей</param>
        /// <param name="clipboardService">Сервис буфера обмена</param>
        public GeneratorViewModel(
            PasswordGeneratorService passwordGeneratorService,
            ClipboardService clipboardService)
        {
            _passwordGeneratorService = passwordGeneratorService;
            _clipboardService = clipboardService;

            // Инициализируем параметры по умолчанию
            _passwordOptions = new PasswordOptions();

            // Инициализируем команды
            GeneratePasswordCommand = new RelayCommand(_ => GeneratePassword());
            CopyPasswordCommand = new RelayCommand(CopyPassword, _ => !string.IsNullOrEmpty(GeneratedPassword));
            ApplyHighSecurityTemplateCommand = new RelayCommand(_ => ApplyTemplate(PasswordOptions.HighSecurity()));
            ApplyMediumSecurityTemplateCommand = new RelayCommand(_ => ApplyTemplate(PasswordOptions.MediumSecurity()));

            // Генерируем начальный пароль
            GeneratePassword();

            // Подписываемся на изменения свойств PasswordOptions
            if (_passwordOptions is INotifyPropertyChanged notifyPropertyChanged)
            {
                notifyPropertyChanged.PropertyChanged += (sender, e) => GeneratePassword();
            }
        }

        /// <summary>
        /// Генерирует пароль с текущими параметрами
        /// </summary>
        private void GeneratePassword()
        {
            try
            {
                GeneratedPassword = _passwordGeneratorService.GeneratePassword(PasswordOptions);
                StatusMessage = "Пароль успешно сгенерирован";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при генерации пароля: {ex.Message}";
                MessageBox.Show($"Ошибка при генерации пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Копирует сгенерированный пароль в буфер обмена
        /// </summary>
        private void CopyPassword(object parameter)
        {
            try
            {
                _clipboardService.CopyToClipboard(GeneratedPassword);
                StatusMessage = "Пароль скопирован в буфер обмена (будет автоматически очищен через 30 секунд)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при копировании пароля: {ex.Message}";
                MessageBox.Show($"Ошибка при копировании пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Применяет шаблон параметров генерации пароля
        /// </summary>
        /// <param name="template">Шаблон параметров</param>
        private void ApplyTemplate(PasswordOptions template)
        {
            try
            {
                PasswordOptions = template;
                StatusMessage = "Применен шаблон параметров";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при применении шаблона: {ex.Message}";
                MessageBox.Show($"Ошибка при применении шаблона: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обновляет оценку сложности пароля
        /// </summary>
        private void UpdatePasswordStrength()
        {
            try
            {
                if (string.IsNullOrEmpty(GeneratedPassword))
                {
                    PasswordStrength = 0;
                    PasswordStrengthText = "Нет пароля";
                    PasswordStrengthColor = "#CCCCCC"; // Серый
                    return;
                }

                PasswordStrength = _passwordGeneratorService.EvaluatePasswordStrength(GeneratedPassword);

                if (PasswordStrength < 30)
                {
                    PasswordStrengthText = "Слабый";
                    PasswordStrengthColor = "#FF4040"; // Красный
                }
                else if (PasswordStrength < 60)
                {
                    PasswordStrengthText = "Средний";
                    PasswordStrengthColor = "#FFA500"; // Оранжевый
                }
                else if (PasswordStrength < 80)
                {
                    PasswordStrengthText = "Хороший";
                    PasswordStrengthColor = "#3CB371"; // Зеленый
                }
                else
                {
                    PasswordStrengthText = "Отличный";
                    PasswordStrengthColor = "#008891"; // Бирюзовый (из цветовой палитры)
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при оценке сложности пароля: {ex.Message}";
            }
        }
    }
}