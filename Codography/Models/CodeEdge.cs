using System.Collections.Generic;

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
    }
}
