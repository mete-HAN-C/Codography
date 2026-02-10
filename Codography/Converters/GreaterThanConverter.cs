using System.Globalization;
using System.Windows.Data;

namespace Codography.Converters
{
    // XAML binding işlemlerinde bir değerin belirli bir sayıdan büyük olup olmadığını kontrol eden converter.
    public class GreaterThanConverter : IValueConverter
    {
        // Kaynaktan (ViewModel) gelen değeri, hedefte (UI) kullanılacak formata dönüştüren metot
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Eğer gelen değer int ise ve XAML'den gönderilen parameter sayıya dönüştürülebiliyorsa
            if (value is int intValue && int.TryParse(parameter?.ToString(), out int threshold))
            {
                // Değer, belirtilen eşik değerden büyükse true döner. Aksi durumda false döner
                return intValue > threshold;
            }
            // Geçersiz veya dönüştürülemeyen değerler için varsayılan olarak false döndürülür
            return false;
        }
        // UI'dan gelen değeri tekrar ViewModel'e dönüştürmek için kullanılır. Bu converter tek yönlü olduğu için geri dönüş desteklenmez
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Geri dönüş işlemi bilinçli olarak uygulanmaz.
            throw new NotImplementedException();
        }
    }
}