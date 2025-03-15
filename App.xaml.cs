using Microsoft.Extensions.DependencyInjection;
using PasswordManager.Services;
using PasswordManager.ViewModels;
using PasswordManager.Views;
using System;
using System.Windows;

namespace PasswordManager
{
    /// <summary>
    /// Логика взаимодействия для App.xaml
    /// </summary>
    public partial class App : Application
    {
        /// <summary>
        /// Сервисы приложения
        /// </summary>
        public IServiceProvider Services { get; private set; }

        public App()
        {
            // Настраиваем контейнер зависимостей
            ServiceCollection services = new ServiceCollection();
            ConfigureServices(services);
            Services = services.BuildServiceProvider();
            
            // Глобальная обработка исключений
            AppDomain.CurrentDomain.UnhandledException += (s, e) => {
                if (e.ExceptionObject is Exception ex) {
                    MessageBox.Show($"Критическая ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            };
        }

        private void ConfigureServices(ServiceCollection services)
        {
            // Регистрируем сервисы
            services.AddSingleton<PasswordGeneratorService>();
            services.AddSingleton<EncryptionService>();
            services.AddSingleton<StorageService>();
            services.AddSingleton<ClipboardService>();

            // Регистрируем ViewModels
            services.AddSingleton<MainViewModel>();
            
            // Регистрируем окна и страницы
            services.AddTransient<MainWindow>();
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            try
            {
                // Создаем и отображаем главное окно
                var mainWindow = Services.GetService<MainWindow>();
                mainWindow.DataContext = Services.GetService<MainViewModel>();
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при запуске приложения: {ex.Message}", 
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}