using PasswordManager.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PasswordManager.Views
{
    /// <summary>
    /// Логика взаимодействия для VaultPage.xaml
    /// </summary>
    public partial class VaultPage : UserControl
    {
        public VaultPage()
        {
            InitializeComponent();
            
            // Подписываемся на изменение пароля
            this.Loaded += VaultPage_Loaded;
        }

        private void VaultPage_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Получаем ViewModel
                var viewModel = this.DataContext as VaultViewModel;
                
                if (viewModel != null)
                {
                    // Привязываем PasswordBox к свойству ViewModel
                    // (так как PasswordBox не поддерживает привязку из коробки)
                    NewPasswordBox.Password = viewModel.NewPassword;
                    
                    NewPasswordBox.PasswordChanged += (s, args) =>
                    {
                        viewModel.NewPassword = NewPasswordBox.Password;
                    };
                    
                    // Подписываемся на изменение свойства в ViewModel
                    viewModel.PropertyChanged += (s, args) =>
                    {
                        if (args.PropertyName == nameof(viewModel.NewPassword))
                        {
                            if (NewPasswordBox.Password != viewModel.NewPassword)
                            {
                                NewPasswordBox.Password = viewModel.NewPassword;
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
    }

    /// <summary>
    /// Конвертер, который возвращает Visibility.Visible, если значение равно 0
    /// </summary>
    public class ZeroToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is int count)
            {
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            if (value is System.Collections.ICollection collection)
            {
                return collection.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            
            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}