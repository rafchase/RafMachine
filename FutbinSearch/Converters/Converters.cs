using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace FutbinSearch.Converters
{
    /// <summary>
    /// Converte um valor booleano em Visibility (True → Visible, False → Collapsed).
    /// Uso XAML: Visibility="{Binding IsLoading, Converter={StaticResource BoolToVisibility}}"
    /// </summary>
    [ValueConversion(typeof(bool), typeof(Visibility))]
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool boolValue = value is bool b && b;
            // Se o parâmetro "Invert" for passado, inverte a lógica
            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            return (boolValue ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => value is Visibility v && v == Visibility.Visible;
    }

    /// <summary>
    /// Converte um rating numérico (0-99) em uma cor de texto para exibição.
    /// Ratings altos (85+) aparecem em ouro, médios em branco, baixos em cinza.
    /// </summary>
    [ValueConversion(typeof(int), typeof(Brush))]
    public class RatingToColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int rating)
            {
                if (rating >= 90) return new SolidColorBrush(Color.FromRgb(0xFF, 0xD7, 0x00)); // Ouro
                if (rating >= 80) return new SolidColorBrush(Color.FromRgb(0xFF, 0xE0, 0x80)); // Ouro claro
                if (rating >= 70) return new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)); // Branco
                return new SolidColorBrush(Color.FromRgb(0xCC, 0xCC, 0xCC)); // Cinza
            }
            return Brushes.White;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converte uma string hexadecimal de cor (#RRGGBB) em um SolidColorBrush.
    /// Usado para aplicar a cor dinâmica da carta baseada na versão.
    /// </summary>
    [ValueConversion(typeof(string), typeof(Brush))]
    public class HexColorToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string hex && !string.IsNullOrEmpty(hex))
            {
                try
                {
                    var color = (Color)ColorConverter.ConvertFromString(hex);
                    return new SolidColorBrush(color);
                }
                catch { }
            }
            return new SolidColorBrush(Color.FromRgb(0xC8, 0xA8, 0x50)); // Padrão ouro
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converte um valor inteiro em estrelas Unicode (★).
    /// Ex: 5 → "★★★★★", 3 → "★★★☆☆"
    /// </summary>
    [ValueConversion(typeof(int), typeof(string))]
    public class IntToStarsConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count && count >= 0)
            {
                int max = parameter is string p && int.TryParse(p, out var m) ? m : 5;
                return new string('★', Math.Min(count, max)) + new string('☆', Math.Max(0, max - count));
            }
            return "☆☆☆☆☆";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converte um valor longo de preço em string formatada (ex: 1500000 → "1.5M").
    /// </summary>
    [ValueConversion(typeof(long), typeof(string))]
    public class PriceFormatter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is long price && price > 0)
                return Models.PlayerCard.FormatPrice(price);
            return "N/D";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converte um int: maior que zero → Visible, zero → Collapsed.
    /// Uso: Visibility="{Binding SomeCollection.Count, Converter={StaticResource CountToVisible}}"
    /// </summary>
    [ValueConversion(typeof(int), typeof(Visibility))]
    public class CountToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert   = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool hasItems = value is int count && count > 0;
            return (hasItems ^ invert) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converte uma string não-nula/vazia em Visible e string vazia em Collapsed.
    /// </summary>
    [ValueConversion(typeof(string), typeof(Visibility))]
    public class StringToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool hasValue = value is string s && !string.IsNullOrEmpty(s);
            return hasValue ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }

    /// <summary>
    /// Converte null em Collapsed e não-null em Visible.
    /// Útil para mostrar imagens apenas quando carregadas.
    /// </summary>
    [ValueConversion(typeof(object), typeof(Visibility))]
    public class NullToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            bool isNull = value == null;
            return (isNull ^ invert) ? Visibility.Collapsed : Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => DependencyProperty.UnsetValue;
    }
}
