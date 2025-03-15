using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Threading;

namespace PasswordManager.Helpers
{
    /// <summary>
    /// Класс для отслеживания неактивности пользователя
    /// </summary>
    public class InactivityDetector
    {
        // Таймер для отслеживания неактивности
        private readonly DispatcherTimer _inactivityTimer;
        
        // Время неактивности в минутах, после которого сработает событие
        private readonly int _inactivityTimeoutMinutes;
        
        // Флаг, указывающий, активно ли сейчас отслеживание неактивности
        private bool _isActive;

        /// <summary>
        /// Событие, которое возникает при обнаружении неактивности пользователя
        /// </summary>
        public event EventHandler? InactivityDetected;

        /// <summary>
        /// Инициализирует новый экземпляр детектора неактивности
        /// </summary>
        /// <param name="inactivityTimeoutMinutes">Время неактивности в минутах</param>
        public InactivityDetector(int inactivityTimeoutMinutes = 5)
        {
            _inactivityTimeoutMinutes = inactivityTimeoutMinutes;
            _inactivityTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMinutes(inactivityTimeoutMinutes)
            };
            _inactivityTimer.Tick += InactivityTimer_Tick;
        }

        /// <summary>
        /// Запускает отслеживание неактивности
        /// </summary>
        public void Start()
        {
            if (_isActive)
                return;

            try
            {
                // Проверяем, что главное окно существует
                if (Application.Current.MainWindow == null)
                {
                    return;
                }

                // Добавляем обработчики событий для сброса таймера
                Application.Current.MainWindow.PreviewMouseMove += ResetTimer;
                Application.Current.MainWindow.PreviewKeyDown += ResetTimer;
                Application.Current.MainWindow.PreviewMouseDown += ResetTimer;
                Application.Current.MainWindow.Deactivated += WindowDeactivated;
                Application.Current.MainWindow.Activated += WindowActivated;

                // Запускаем таймер
                _inactivityTimer.Start();
                _isActive = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске отслеживания неактивности: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Останавливает отслеживание неактивности
        /// </summary>
        public void Stop()
        {
            if (!_isActive)
                return;

            try
            {
                // Проверяем, что главное окно существует
                if (Application.Current.MainWindow == null)
                {
                    return;
                }

                // Удаляем обработчики событий
                Application.Current.MainWindow.PreviewMouseMove -= ResetTimer;
                Application.Current.MainWindow.PreviewKeyDown -= ResetTimer;
                Application.Current.MainWindow.PreviewMouseDown -= ResetTimer;
                Application.Current.MainWindow.Deactivated -= WindowDeactivated;
                Application.Current.MainWindow.Activated -= WindowActivated;

                // Останавливаем таймер
                _inactivityTimer.Stop();
                _isActive = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при остановке отслеживания неактивности: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Обработчик события таймера
        /// </summary>
        private void InactivityTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                // Вызываем событие обнаружения неактивности
                InactivityDetected?.Invoke(this, EventArgs.Empty);
                
                // Останавливаем таймер
                _inactivityTimer.Stop();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при обработке неактивности: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Сбрасывает таймер неактивности
        /// </summary>
        private void ResetTimer(object? sender, EventArgs e)
        {
            try
            {
                _inactivityTimer.Stop();
                _inactivityTimer.Start();
            }
            catch
            {
                // Игнорируем исключения при сбросе таймера
            }
        }

        /// <summary>
        /// Обработчик деактивации окна
        /// </summary>
        private void WindowDeactivated(object? sender, EventArgs e)
        {
            try
            {
                // Если окно теряет фокус, запускаем таймер с уменьшенным интервалом
                _inactivityTimer.Stop();
                _inactivityTimer.Interval = TimeSpan.FromMinutes(1); // Уменьшаем время до 1 минуты
                _inactivityTimer.Start();
            }
            catch
            {
                // Игнорируем исключения при деактивации окна
            }
        }

        /// <summary>
        /// Обработчик активации окна
        /// </summary>
        private void WindowActivated(object? sender, EventArgs e)
        {
            try
            {
                // Восстанавливаем исходный интервал
                _inactivityTimer.Stop();
                _inactivityTimer.Interval = TimeSpan.FromMinutes(_inactivityTimeoutMinutes);
                _inactivityTimer.Start();
            }
            catch
            {
                // Игнорируем исключения при активации окна
            }
        }
    }
}