using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using TimeTracker.Wpf.ViewModels;

namespace TimeTracker.Wpf.Views;

/// Un ecart positif en vert, negatif en rouge ; neutre garde la couleur du theme.
public sealed class TonBrushConverter : IValueConverter
{
    private static readonly Brush Positif = Figer(new SolidColorBrush(Color.FromRgb(0x2E, 0xA0, 0x43)));
    private static readonly Brush Negatif = Figer(new SolidColorBrush(Color.FromRgb(0xD1, 0x3B, 0x30)));

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) => value switch
    {
        Ton.Positif => Positif,
        Ton.Negatif => Negatif,
        _ => DependencyProperty.UnsetValue,
    };

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();

    private static Brush Figer(Brush brush)
    {
        brush.Freeze();
        return brush;
    }
}

/// Visible quand le texte n'est pas vide.
public sealed class TextVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) =>
        string.IsNullOrEmpty(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
