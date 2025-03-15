using PasswordManager.Models;
using PasswordManager.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace PasswordManager.ViewModels
{
    /// <summary>
    /// ViewModel для хранилища паролей
    /// </summary>
    public class VaultViewModel : BaseViewModel
    {
        private readonly StorageService _storageService;
        private readonly ClipboardService _clipboardService;
        private readonly PasswordGeneratorService _passwordGeneratorService;
        private readonly string _masterPassword;

        // Коллекция записей паролей
        private ObservableCollection<PasswordEntry> _passwordEntries = new ObservableCollection<PasswordEntry>();
        
        // Выбранная запись пароля
        private PasswordEntry? _selectedPasswordEntry;
        
        // Текст для поиска
        private string _searchText = string.Empty;
        
        // Сообщение для пользователя
        private string _statusMessage = string.Empty;
        
        // Новая запись пароля
        private PasswordEntry _newPasswordEntry = new PasswordEntry();
        
        // Новый пароль для добавления
        private string _newPassword = string.Empty;
        
        // Флаг видимости формы добавления пароля
        private bool _isAddPasswordFormVisible;

        /// <summary>
        /// Коллекция записей паролей
        /// </summary>
        public ObservableCollection<PasswordEntry> PasswordEntries
        {
            get => _passwordEntries;
            set => SetProperty(ref _passwordEntries, value);
        }

        /// <summary>
        /// Выбранная запись пароля
        /// </summary>
        public PasswordEntry? SelectedPasswordEntry
        {
            get => _selectedPasswordEntry;
            set
            {
                if (SetProperty(ref _selectedPasswordEntry, value))
                {
                    // Обновляем состояние команд при изменении выбранной записи
                    (CopyPasswordCommand as RelayCommand)?.RaiseCanExecuteChanged();
                    (DeletePasswordCommand as RelayCommand)?.RaiseCanExecuteChanged();
                }
            }
        }

        /// <summary>
        /// Текст для поиска
        /// </summary>
        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    // При изменении текста поиска фильтруем записи
                    FilterPasswordEntries();
                }
            }
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
        /// Новая запись пароля
        /// </summary>
        public PasswordEntry NewPasswordEntry
        {
            get => _newPasswordEntry;
            set => SetProperty(ref _newPasswordEntry, value);
        }

        /// <summary>
        /// Новый пароль для добавления
        /// </summary>
        public string NewPassword
        {
            get => _newPassword;
            set => SetProperty(ref _newPassword, value);
        }

        /// <summary>
        /// Флаг видимости формы добавления пароля
        /// </summary>
        public bool IsAddPasswordFormVisible
        {
            get => _isAddPasswordFormVisible;
            set => SetProperty(ref _isAddPasswordFormVisible, value);
        }

        /// <summary>
        /// Команда добавления новой записи пароля
        /// </summary>
        public ICommand AddPasswordCommand { get; }

        /// <summary>
        /// Команда сохранения новой записи пароля
        /// </summary>
        public ICommand SaveNewPasswordCommand { get; }

        /// <summary>
        /// Команда отмены добавления новой записи
        /// </summary>
        public ICommand CancelAddPasswordCommand { get; }

        /// <summary>
        /// Команда копирования пароля в буфер обмена
        /// </summary>
        public ICommand CopyPasswordCommand { get; }

        /// <summary>
        /// Команда удаления записи пароля
        /// </summary>
        public ICommand DeletePasswordCommand { get; }

        /// <summary>
        /// Команда генерации нового пароля для добавления
        /// </summary>
        public ICommand GenerateNewPasswordCommand { get; }

        /// <summary>
        /// Инициализирует новый экземпляр VaultViewModel
        /// </summary>
        /// <param name="storageService">Сервис хранения</param>
        /// <param name="clipboardService">Сервис буфера обмена</param>
        /// <param name="passwordGeneratorService">Сервис генерации паролей</param>
        /// <param name="masterPassword">Мастер-пароль</param>
        public VaultViewModel(
            StorageService storageService,
            ClipboardService clipboardService,
            PasswordGeneratorService passwordGeneratorService,
            string masterPassword)
        {
            _storageService = storageService;
            _clipboardService = clipboardService;
            _passwordGeneratorService = passwordGeneratorService;
            _masterPassword = masterPassword;

            // Инициализируем коллекцию паролей
            _passwordEntries = new ObservableCollection<PasswordEntry>();

            // Инициализируем новую запись пароля
            ResetNewPasswordEntry();

            // Инициализируем команды
            AddPasswordCommand = new RelayCommand(_ => ShowAddPasswordForm());
            SaveNewPasswordCommand = new RelayCommand(async _ => await SaveNewPassword(), CanSaveNewPassword);
            CancelAddPasswordCommand = new RelayCommand(_ => CancelAddPassword());
            CopyPasswordCommand = new RelayCommand(async p => await CopyPassword(), _ => SelectedPasswordEntry != null);
            DeletePasswordCommand = new RelayCommand(async _ => await DeletePassword(), _ => SelectedPasswordEntry != null);
            GenerateNewPasswordCommand = new RelayCommand(_ => GenerateNewPassword());

            // Загружаем пароли из хранилища
            LoadPasswordEntries();
        }

        /// <summary>
        /// Загружает записи паролей из хранилища
        /// </summary>
        private async void LoadPasswordEntries()
        {
            try
            {
                var entries = await _storageService.LoadPasswordEntries(_masterPassword);
                PasswordEntries = new ObservableCollection<PasswordEntry>(entries);
                StatusMessage = $"Загружено {entries.Count} записей";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при загрузке паролей: {ex.Message}";
                MessageBox.Show($"Не удалось загрузить пароли: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Фильтрует записи паролей в соответствии с текстом поиска
        /// </summary>
        private async void FilterPasswordEntries()
        {
            try
            {
                var allEntries = await _storageService.LoadPasswordEntries(_masterPassword);

                if (string.IsNullOrWhiteSpace(SearchText))
                {
                    PasswordEntries = new ObservableCollection<PasswordEntry>(allEntries);
                }
                else
                {
                    var filteredEntries = allEntries
                        .Where(e => e.ServiceName.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                    
                    PasswordEntries = new ObservableCollection<PasswordEntry>(filteredEntries);
                    StatusMessage = $"Найдено {filteredEntries.Count} записей";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при поиске паролей: {ex.Message}";
                MessageBox.Show($"Ошибка при поиске паролей: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Показывает форму добавления пароля
        /// </summary>
        private void ShowAddPasswordForm()
        {
            try
            {
                ResetNewPasswordEntry();
                IsAddPasswordFormVisible = true;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при отображении формы: {ex.Message}";
                MessageBox.Show($"Ошибка при отображении формы: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Проверяет, можно ли сохранить новую запись пароля
        /// </summary>
        private bool CanSaveNewPassword(object parameter)
        {
            return !string.IsNullOrWhiteSpace(NewPasswordEntry?.ServiceName) && 
                   !string.IsNullOrWhiteSpace(NewPassword);
        }

        /// <summary>
        /// Сохраняет новую запись пароля
        /// </summary>
        private async Task SaveNewPassword()
        {
            try
            {
                var entries = await _storageService.AddPasswordEntry(
                    NewPasswordEntry, 
                    NewPassword, 
                    _masterPassword);
                
                PasswordEntries = new ObservableCollection<PasswordEntry>(entries);
                StatusMessage = "Пароль успешно добавлен";
                IsAddPasswordFormVisible = false;
                ResetNewPasswordEntry();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при сохранении пароля: {ex.Message}";
                MessageBox.Show($"Не удалось сохранить пароль: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Отменяет добавление новой записи пароля
        /// </summary>
        private void CancelAddPassword()
        {
            try
            {
                IsAddPasswordFormVisible = false;
                ResetNewPasswordEntry();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при отмене добавления: {ex.Message}";
                MessageBox.Show($"Ошибка при отмене добавления: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Копирует пароль выбранной записи в буфер обмена
        /// </summary>
        private async Task CopyPassword()
        {
            if (SelectedPasswordEntry == null)
                return;

            try
            {
                string password = await _storageService.GetDecryptedPassword(
                    SelectedPasswordEntry.Id, 
                    _masterPassword);
                
                _clipboardService.CopyToClipboard(password);
                StatusMessage = "Пароль скопирован в буфер обмена (будет автоматически очищен через 30 секунд)";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при копировании пароля: {ex.Message}";
                MessageBox.Show($"Не удалось скопировать пароль: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Удаляет выбранную запись пароля
        /// </summary>
        private async Task DeletePassword()
        {
            if (SelectedPasswordEntry == null)
                return;

            try
            {
                var result = MessageBox.Show(
                    $"Вы уверены, что хотите удалить пароль для '{SelectedPasswordEntry.ServiceName}'?",
                    "Подтверждение удаления",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    var entries = await _storageService.DeletePasswordEntry(
                        SelectedPasswordEntry.Id, 
                        _masterPassword);
                    
                    PasswordEntries = new ObservableCollection<PasswordEntry>(entries);
                    StatusMessage = "Пароль успешно удален";
                }
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при удалении пароля: {ex.Message}";
                MessageBox.Show($"Не удалось удалить пароль: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Генерирует новый пароль для добавления
        /// </summary>
        private void GenerateNewPassword()
        {
            try
            {
                NewPassword = _passwordGeneratorService.GeneratePassword(PasswordOptions.HighSecurity());
                StatusMessage = "Новый пароль успешно сгенерирован";
                
                // Обновляем состояние команды сохранения
                (SaveNewPasswordCommand as RelayCommand)?.RaiseCanExecuteChanged();
            }
            catch (Exception ex)
            {
                StatusMessage = $"Ошибка при генерации пароля: {ex.Message}";
                MessageBox.Show($"Ошибка при генерации пароля: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сбрасывает новую запись пароля
        /// </summary>
        private void ResetNewPasswordEntry()
        {
            NewPasswordEntry = new PasswordEntry();
            NewPassword = string.Empty;
        }
    }
}