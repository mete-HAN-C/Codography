using System.Globalization;
using System.Windows.Data;

// Bu dönüştürücü, bir bool değeri alır ve tam tersini döndürür. Yani true gelirse false, false gelirse true yapar.
// Projenin ViewModel kısmında IsBusy adındaki değişken Analiz başladığında IsBusy = true olur (Sistem meşgul). Analiz bittiğinde IsBusy = false olur (Sistem hazır).
// Sorun şurada: Butonun tıklanabilir olmasını sağlayan IsEnabled özelliği, çalışmak için true değeri bekler. Eğer biz butonu doğrudan IsBusy değişkenine bağlarsak; sistem meşgulken (true) buton aktif olur, sistem boşken (false) buton kilitlenir. Yani her şey ters çalışır.
// Çözüm: InverseBooleanConverter araya girer. IsBusy değerini alır, tersine çevirir ve butona öyle verir. Böylece: Sistem Meşgul (true) Converter bunu false yapar. Buton Kilitlenir. Sistem Boş (false) Converter bunu true yapar. Buton Aktifleşir.
namespace Codography.Converters
{
    public class InverseBooleanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value is bool b ? !b : false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}