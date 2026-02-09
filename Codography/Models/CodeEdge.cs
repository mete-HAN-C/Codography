// CodeEdge sınıfı, eskiden MainWindow.xaml.cs dosyasında arayüz kodlarının hemen üstünde tanımlı iken artık kendi fiziksel dosyası var.
namespace Codography.Models
{
    /// <summary>
    /// İki kod objesi (Node) arasındaki ilişkiyi (bağlantıyı) temsil eder.
    /// </summary>
    public class CodeEdge
    {
        // Çizginin başladığı düğümün benzersiz kimliği (Örn: Çağıran metot Id'si)
        public string SourceId { get; set; }

        // Çizginin bittiği düğümün benzersiz kimliği (Örn: Çağrılan metot Id'si)
        public string TargetId { get; set; }

        // İlişkinin türü (Metot Çağrısı mı yoksa Kalıtım mı?)
        public EdgeType Type { get; set; }

        // İlişkinin yazma (write) erişimi içerip içermediğini belirtir. Eğer true ise, kaynak düğüm hedef düğüm üzerinde veri değiştiriyor demektir
        // Örn: bir field'a değer atama, property set etme gibi durumlar
        // Eğer false ise, sadece okuma yapılıyor demektir
        public bool IsWriteAccess { get; set; }
    }
}