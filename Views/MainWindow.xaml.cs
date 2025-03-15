using PasswordManager.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace PasswordManager.Views
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            try
            {
                InitializeComponent();
                
                // Подписываемся на изменение пароля в PasswordBox
                this.Loaded += MainWindow_Loaded;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при инициализации главного окна: {ex.Message}\n{ex.StackTrace}",
                    "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем ViewModel
                var viewModel = this.DataContext as MainViewModel;
                
                if (viewModel != null)
                {
                    // Привязываем PasswordBox к свойству ViewModel
                    // (так как PasswordBox не поддерживает привязку из коробки)
                    MasterPasswordBox.Password = viewModel.MasterPassword;
                    
                    MasterPasswordBox.PasswordChanged += (s, args) =>
                    {
                        viewModel.MasterPassword = MasterPasswordBox.Password;
                    };
                    
                    // Подписываемся на событие нажатия клавиши Enter
                    MasterPasswordBox.KeyDown += (s, args) =>
                    {
                        if (args.Key == System.Windows.Input.Key.Enter)
                        {
                            if (viewModel.UnlockVaultCommand.CanExecute(null))
                            {
                                viewModel.UnlockVaultCommand.Execute(null);
                            }
                        }
                    };
                    
                    // Подписываемся на изменение свойства в ViewModel
                    viewModel.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(viewModel.MasterPassword))
                        {
                            if (MasterPasswordBox.Password != viewModel.MasterPassword)
                            {
                                MasterPasswordBox.Password = viewModel.MasterPassword;
                            }
                        }
                    };
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при настройке привязок: {ex.Message}",
                    "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Обработчики событий для кнопок управления окном
        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            this.WindowState = WindowState.Minimized;
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.WindowState == WindowState.Maximized)
            {
                this.WindowState = WindowState.Normal;
                MaximizeButton.Content = "🗖";
            }
            else
            {
                this.WindowState = WindowState.Maximized;
                MaximizeButton.Content = "🗗";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void WindowTitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                MaximizeButton_Click(null, null);
            }
            else
            {
                this.DragMove();
            }
        }
    }
}