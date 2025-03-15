using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace PasswordManager.Views
{
    /// <summary>
    /// Логика взаимодействия для GeneratorPage.xaml
    /// </summary>
    public partial class GeneratorPage : UserControl
    {
        public GeneratorPage()
        {
            InitializeComponent();
        }
    }

    /// <summary>
    /// Мультиконвертер для вычисления ширины индикатора прогресс-бара
    /// </summary>
    public class ProductMultiConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // values[0] = Value, values[1] = ActualWidth, values[2] = Maximum
            if (values.Length == 3 && 
                values[0] is double value &&
                values[1] is double width &&
                values[2] is double maximum && 
                maximum > 0)
            {
                return width * (value / maximum);
            }
            return 0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}