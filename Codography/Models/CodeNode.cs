// CodeNodge sınıfı, eskiden MainWindow.xaml.cs dosyasında arayüz kodlarının hemen üstünde tanımlı iken artık kendi fiziksel dosyası var.
// Eski CodeNode sınıfı sadece Id, Name ve Type alanlarına sahipti. Artık yeni sınıf Children { get; set; } özelliğine sahip. Bu, CodeNode sınıfını Recursive (Özyinelemeli) bir yapıya dönüştürüyor. Artık bir düğüm kendi içinde alt düğümleri taşıyabiliyor.
namespace Codography.Models
{
    /// <summary>
    /// Kod yapısındaki her bir öğeyi (Sınıf, Metot vb.) temsil eden düğüm.
    /// </summary>
    public class CodeNode
    {
        // Benzersiz kimlik: "Namespace.SınıfAdı.MetotAdı" formatında.
        public string Id { get; set; }

        // Ekranda görünecek olan kısa isim.
        public string Name { get; set; }

        // Öğenin türü (Class veya Method).
        public NodeType Type { get; set; }

        // Artık bir CodeNode (örneğin bir Class) oluşturduğunda, o sınıfa ait tüm metotları bu Children listesinin içine ekleyebiliriz. Örnek: Araba sınıfı bir düğümse, Calistir() ve Durdur() metotları o düğümün "çocukları" (Children) olur.
        // Children listesi sayesinde, TreeView'a sadece en üstteki sınıfları vermek yeterli olur. WPF, listenin içindeki bu Children özelliğine bakarak alt dalları (metotları) otomatik olarak ekranda oluşturabilir.
        // Özetle: Bu satır, verilerini karmaşık bir tablodan ziyade, gerçek bir dosya-klasör yapısı gibi düzenli tutmamızı sağlıyor.
        public List<CodeNode> Children { get; set; } = new List<CodeNode>();

        // Metodun dönüş tipini tutar. (Örn: void, int, string, Task, List<int> gibi). Eğer özel olarak set edilmezse varsayılan olarak "void" kabul edilir
        public string ReturnType { get; set; } = "void";

        // Metodun aldığı parametreleri tutar. Her parametre string olarak saklanır. (Örn: "string name", "int age", "Motor motor")
        // Liste olarak tutulmasının sebebi, bir metodun birden fazla parametresi olabilmesidir
        public List<string> Parameters { get; set; } = new List<string>();

        // Metodun karmaşıklık puanını tutar. Her metot, içinde hiçbir karar yapısı olmasa bile, tanımı gereği en az 1 karmaşıklık puanıyla başlar.
        public int ComplexityScore { get; set; } = 1;
    }
}