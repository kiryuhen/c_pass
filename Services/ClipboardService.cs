using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace PasswordManager.Services
{
    /// <summary>
    /// Сервис для работы с буфером обмена
    /// </summary>
    public class ClipboardService
    {
        // Время в секундах, через которое буфер обмена будет очищен
        private const int DefaultClearDelay = 30;
        
        // Токен отмены для задачи очистки буфера обмена
        private CancellationTokenSource? _clearClipboardCts;

        /// <summary>
        /// Копирует данные в буфер обмена и устанавливает таймер на очистку
        /// </summary>
        /// <param name="text">Текст для копирования</param>
        /// <param name="clearDelaySeconds">Задержка очистки в секундах (по умолчанию 30)</param>
        public void CopyToClipboard(string text, int clearDelaySeconds = DefaultClearDelay)
        {
            if (string.IsNullOrEmpty(text))
                return;

            try
            {
                // Отменяем предыдущую задачу очистки буфера, если она существует
                CancelPendingClipboardClear();

                // Копируем текст в буфер обмена
                Clipboard.SetText(text);
                
                // Устанавливаем таймер на очистку буфера обмена
                SetupClipboardClearTimer(text, clearDelaySeconds);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось скопировать в буфер обмена: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Немедленно очищает буфер обмена, если он содержит указанный текст
        /// </summary>
        /// <param name="textToCheck">Текст для проверки</param>
        public void ClearClipboard(string? textToCheck = null)
        {
            try
            {
                if (Clipboard.ContainsText())
                {
                    // Если текст не указан или буфер обмена содержит указанный текст
                    if (textToCheck == null || Clipboard.GetText() == textToCheck)
                    {
                        Clipboard.Clear();
                    }
                }
                
                // Отменяем ожидающую задачу очистки буфера
                CancelPendingClipboardClear();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Не удалось очистить буфер обмена: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// Устанавливает таймер на очистку буфера обмена
        /// </summary>
        /// <param name="textToClear">Текст, который нужно очистить</param>
        /// <param name="delaySeconds">Задержка в секундах</param>
        private void SetupClipboardClearTimer(string textToClear, int delaySeconds)
        {
            // Создаем новый токен отмены
            _clearClipboardCts = new CancellationTokenSource();
            var token = _clearClipboardCts.Token;

            // Запускаем асинхронную задачу для очистки буфера обмена
            Task.Run(async () =>
            {
                try
                {
                    // Ждем указанное время
                    await Task.Delay(TimeSpan.FromSeconds(delaySeconds), token);
                    
                    // Если задача не была отменена, очищаем буфер обмена
                    if (!token.IsCancellationRequested)
                    {
                        // Поскольку мы находимся в фоновом потоке, а Clipboard требует
                        // доступа к UI-потоку, нам нужно перейти в UI-поток
                        await Application.Current.Dispatcher.InvokeAsync(() =>
                        {
                            ClearClipboard(textToClear);
                        });
                    }
                }
                catch (TaskCanceledException)
                {
                    // Задача была отменена, ничего не делаем
                }
                catch (Exception ex)
                {
                    // Обрабатываем другие исключения
                    await Application.Current.Dispatcher.InvokeAsync(() =>
                    {
                        MessageBox.Show($"Ошибка при очистке буфера обмена: {ex.Message}", 
                            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    });
                }
            }, token);
        }

        /// <summary>
        /// Отменяет ожидающую задачу очистки буфера обмена
        /// </summary>
        private void CancelPendingClipboardClear()
        {
            if (_clearClipboardCts != null && !_clearClipboardCts.IsCancellationRequested)
            {
                _clearClipboardCts.Cancel();
                _clearClipboardCts.Dispose();
                _clearClipboardCts = null;
            }
        }
    }
}