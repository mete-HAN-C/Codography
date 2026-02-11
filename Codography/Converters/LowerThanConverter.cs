using System.Globalization;
using System.Windows.Data;

namespace Codography.Converters
{
    // IValueConverter arayüzünü implement ediyoruz. Bu sayede Binding sırasında gelen veriyi dönüştürebiliriz.
    public class LowerThanConverter : IValueConverter
    {
        // XAML binding işlemlerinde bir sayının belirli bir eşik değerden küçük olup olmadığını kontrol eden converter
        // value = ViewModel'den gelen değer, targetType = hedef tip (örneğin bool, string vs.)
        // parameter = XAML'den gönderilen ek parametre (örneğin eşik değeri)
        // culture = kültür bilgisi (TR, EN gibi)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Eğer gelen değer double türündeyse ve XAML'den gelen parameter sayıya dönüştürülebiliyorsa
            if (value is double doubleValue && double.TryParse(parameter?.ToString(), out double threshold))
            {
                // Eğer değer eşik değerinden küçükse true döndür. Örn: 12 < 20 → true. Aksi durumda false döner
                return doubleValue < threshold;
            }
            // Geçersiz veri veya dönüştürülemeyen parameter durumunda varsayılan olarak false döndürülür
            return false;
        }

        // UI'dan gelen değeri tekrar ViewModel'e dönüştürmek için kullanılır. Bu converter tek yönlü tasarlandığı için geri dönüş desteklenmez
        // 
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // Bu converter'da geri dönüşüm desteklenmiyor. Bilinçli olarak implement edilmedi
            throw new NotImplementedException();
        }
    }
}