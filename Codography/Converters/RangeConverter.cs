using System.Globalization;
using System.Windows.Data;

namespace Codography.Converters
{
    // XAML binding işlemlerinde bir değerin belirli bir sayı aralığı içinde olup olmadığını kontrol eden converter
    public class RangeConverter : IValueConverter
    {
        // Kaynaktan (ViewModel) gelen değeri, hedefte (UI) kullanılacak bir boolean değere dönüştürür
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Eğer gelen değer int ise ve XAML'den gelen parameter string türündeyse (örn: "3-7")
            if (value is int intValue && parameter is string rangeStr)
            {
                // Parameter olarak gelen aralık bilgisini '-' karakterine göre böl
                var parts = rangeStr.Split('-');

                // Aralık iki parçadan oluşuyorsa (min-max) ve her iki parça da başarıyla sayıya dönüştürülebiliyorsa
                if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                {
                    // Değer, belirtilen minimum ve maksimum aralıkta ise true döner. Aksi durumda false döner
                    return intValue >= min && intValue <= max;
                }
            }
            // Geçersiz değer veya hatalı parameter durumlarında varsayılan olarak false döndürülür
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