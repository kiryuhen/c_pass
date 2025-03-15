using PasswordManager.Helpers;
using PasswordManager.Services;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace PasswordManager.ViewModels
{
    /// <summary>
    /// ViewModel для главного окна приложения
    /// </summary>
    public class MainViewModel : BaseViewModel
    {
        // Сервисы
        private readonly StorageService _storageService;
        private readonly PasswordGeneratorService _passwordGeneratorService;
        private readonly ClipboardService _clipboardService;
        private readonly EncryptionService _encryptionService;
        private readonly InactivityDetector _inactivityDetector;

        // Текущий отображаемый ViewModel
        private BaseViewModel _currentViewModel = null!;

        // Флаг, указывающий, открыто ли хранилище
        private bool _isVaultUnlocked;

        // Мастер-пароль для доступа к хранилищу
        private string _masterPassword = string.Empty;

        /// <summary>
        /// Текущий отображаемый ViewModel
        /// </summary>
        public BaseViewModel CurrentViewModel
        {
            get => _currentViewModel;
            set => SetProperty(ref _currentViewModel, value);
        }

        /// <summary>
        /// Флаг, указывающий, открыто ли хранилище
        /// </summary>
        public bool IsVaultUnlocked
        {
            get => _isVaultUnlocked;
            set => SetProperty(ref _isVaultUnlocked, value);
        }

        /// <summary>
        /// Мастер-пароль для доступа к хранилищу
        /// </summary>
        public string MasterPassword
        {
            get => _masterPassword;
            set
            {
                if (SetProperty(ref _masterPassword, value))
                {
                    // Обновляем состояние команд при изменении мастер-пароля
                    (UnlockVaultCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Команда открытия хранилища
        /// </summary>
        public ICommand UnlockVaultCommand { get; private set; }

        /// <summary>
        /// Команда закрытия хранилища
        /// </summary>
        public ICommand LockVaultCommand { get; private set; }

        /// <summary>
        /// Команда создания мастер-пароля
        /// </summary>
        public ICommand CreateMasterPasswordCommand { get; private set; }

        /// <summary>
        /// Команда перехода к генератору паролей
        /// </summary>
        public ICommand ShowPasswordGeneratorCommand { get; private set; }

        /// <summary>
        /// Команда перехода к хранилищу паролей
        /// </summary>
        public ICommand ShowVaultCommand { get; private set; }

        /// <summary>
        /// Инициализирует новый экземпляр MainViewModel
        /// </summary>
        /// <param name="storageService">Сервис хранения</param>
        /// <param name="passwordGeneratorService">Сервис генерации паролей</param>
        /// <param name="encryptionService">Сервис шифрования</param>
        /// <param name="clipboardService">Сервис буфера обмена</param>
        public MainViewModel(
            StorageService storageService,
            PasswordGeneratorService passwordGeneratorService,
            EncryptionService encryptionService,
            ClipboardService clipboardService)
        {
            try
            {
                _storageService = storageService;
                _passwordGeneratorService = passwordGeneratorService;
                _encryptionService = encryptionService;
                _clipboardService = clipboardService;

                // Инициализируем детектор неактивности
                _inactivityDetector = new InactivityDetector(5); // 5 минут
                _inactivityDetector.InactivityDetected += OnInactivityDetected;

                // Инициализируем команды
                UnlockVaultCommand = new RelayCommand(UnlockVault, CanUnlockVault);
                LockVaultCommand = new RelayCommand(LockVault);
                CreateMasterPasswordCommand = new RelayCommand(CreateMasterPassword);
                ShowPasswordGeneratorCommand = new RelayCommand(ShowPasswordGenerator);
                ShowVaultCommand = new RelayCommand(ShowVault, _ => IsVaultUnlocked);

                // По умолчанию показываем генератор паролей
                var generatorViewModel = new GeneratorViewModel(passwordGeneratorService, clipboardService);
                CurrentViewModel = generatorViewModel;

                // Проверяем, установлен ли мастер-пароль
                if (!_storageService.IsMasterPasswordSet())
                {
                    try
                    {
                        // Показываем диалог создания мастер-пароля
                        ShowCreateMasterPasswordDialog();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при создании мастер-пароля: {ex.Message}",
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Критическая ошибка при инициализации приложения: {ex.Message}\n{ex.StackTrace}",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проверяет, можно ли открыть хранилище
        /// </summary>
        private bool CanUnlockVault(object parameter)
        {
            return !string.IsNullOrEmpty(MasterPassword) && _storageService.IsMasterPasswordSet();
        }

        /// <summary>
        /// Открывает хранилище с использованием мастер-пароля
        /// </summary>
        private void UnlockVault(object parameter)
        {
            try
            {
                if (_storageService.VerifyMasterPassword(MasterPassword))
                {
                    IsVaultUnlocked = true;
                    
                    // Запускаем отслеживание неактивности
                    _inactivityDetector.Start();
                    
                    // Переходим к хранилищу паролей
                    ShowVault(null);
                    
                    // Очищаем поле ввода мастер-пароля
                    MasterPassword = string.Empty;
                }
                else
                {
                    MessageBox.Show("Неверный мастер-пароль", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при разблокировке хранилища: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Закрывает хранилище
        /// </summary>
        private void LockVault(object parameter)
        {
            try
            {
                IsVaultUnlocked = false;
                
                // Останавливаем отслеживание неактивности
                _inactivityDetector.Stop();
                
                // Переходим к генератору паролей
                ShowPasswordGenerator(null);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при блокировке хранилища: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Показывает диалог создания мастер-пароля
        /// </summary>
        private void ShowCreateMasterPasswordDialog()
        {
            try
            {
                // Создаем и настраиваем диалоговое окно
                Window inputDialog = new Window
                {
                    Title = "Создание мастер-пароля",
                    Width = 400,
                    Height = 200,
                    WindowStartupLocation = WindowStartupLocation.CenterScreen,
                    ResizeMode = ResizeMode.NoResize,
                    Background = new SolidColorBrush(Color.FromRgb(0x1E, 0x1E, 0x1E)),
                    BorderThickness = new Thickness(1),
                    BorderBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33))
                };

                // Создаем элементы управления
                Grid grid = new Grid { Margin = new Thickness(20) };
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
                grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

                TextBlock headerText = new TextBlock
                {
                    Text = "Создайте мастер-пароль для доступа к хранилищу:",
                    Foreground = new SolidColorBrush(Color.FromRgb(0xF5, 0xF5, 0xF5)),
                    Margin = new Thickness(0, 0, 0, 15),
                    TextWrapping = TextWrapping.Wrap
                };

                PasswordBox passwordBox = new PasswordBox
                {
                    Margin = new Thickness(0, 0, 0, 20)
                };

                StackPanel buttonPanel = new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right
                };

                Button okButton = new Button
                {
                    Content = "OK",
                    Width = 80,
                    Height = 30,
                    Margin = new Thickness(5, 0, 0, 0),
                    IsDefault = true
                };

                Button cancelButton = new Button
                {
                    Content = "Отмена",
                    Width = 80,
                    Height = 30,
                    IsCancel = true
                };

                buttonPanel.Children.Add(cancelButton);
                buttonPanel.Children.Add(okButton);

                grid.Children.Add(headerText);
                Grid.SetRow(headerText, 0);

                grid.Children.Add(passwordBox);
                Grid.SetRow(passwordBox, 1);

                grid.Children.Add(buttonPanel);
                Grid.SetRow(buttonPanel, 2);

                inputDialog.Content = grid;

                // Обработчики событий для кнопок
                bool dialogResult = false;
                okButton.Click += (s, e) =>
                {
                    dialogResult = true;
                    inputDialog.Close();
                };

                // Показываем диалог
                inputDialog.ShowDialog();

                // Обрабатываем результат
                if (dialogResult && !string.IsNullOrEmpty(passwordBox.Password))
                {
                    _storageService.SetMasterPassword(passwordBox.Password);
                    MessageBox.Show("Мастер-пароль создан успешно!",
                        "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else if (dialogResult) // Пользователь нажал OK, но пароль пустой
                {
                    MessageBox.Show("Мастер-пароль не может быть пустым",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    ShowCreateMasterPasswordDialog(); // Повторный запрос
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании мастер-пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Создает новый мастер-пароль
        /// </summary>
        private void CreateMasterPassword(object parameter)
        {
            try
            {
                // Если хранилище открыто, сначала закрываем его
                if (IsVaultUnlocked)
                {
                    LockVault(null);
                }
                
                ShowCreateMasterPasswordDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при создании мастер-пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Переходит к генератору паролей
        /// </summary>
        private void ShowPasswordGenerator(object parameter)
        {
            try
            {
                // Создаем новый экземпляр ViewModel
                var generatorViewModel = new GeneratorViewModel(_passwordGeneratorService, _clipboardService);
                CurrentViewModel = generatorViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переходе к генератору паролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Переходит к хранилищу паролей
        /// </summary>
        private void ShowVault(object parameter)
        {
            try
            {
                if (!IsVaultUnlocked)
                {
                    MessageBox.Show("Сначала необходимо разблокировать хранилище",
                        "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                // Создаем новый экземпляр VaultViewModel
                var vaultViewModel = new VaultViewModel(
                    _storageService,
                    _clipboardService,
                    _passwordGeneratorService,
                    MasterPassword);
                
                CurrentViewModel = vaultViewModel;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при переходе к хранилищу паролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик события обнаружения неактивности
        /// </summary>
        private void OnInactivityDetected(object? sender, EventArgs e)
        {
            try
            {
                // Закрываем хранилище при обнаружении неактивности
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (IsVaultUnlocked)
                    {
                        MessageBox.Show("Хранилище автоматически заблокировано из-за неактивности",
                            "Информация", MessageBoxButton.OK, MessageBoxImage.Information);
                        LockVault(null);
                    }
                });
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке неактивности: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}