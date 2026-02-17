// CodeNodge sınıfı, eskiden MainWindow.xaml.cs dosyasında arayüz kodlarının hemen üstünde tanımlı iken artık kendi fiziksel dosyası var.
// Eski CodeNode sınıfı sadece Id, Name ve Type alanlarına sahipti. Artık yeni sınıf Children { get; set; } özelliğine sahip. Bu, CodeNode sınıfını Recursive (Özyinelemeli) bir yapıya dönüştürüyor. Artık bir düğüm kendi içinde alt düğümleri taşıyabiliyor.
using System.Text.Json.Serialization; // Özellikle [JsonIgnore], [JsonPropertyName] gibi JSON serileştirme ayarlarını kullanmak için eklenir.

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

        // Bu nesnenin bağlı olduğu üst (parent) öğenin kimliğini tutar. Örneğin bir metodun ParentId değeri, ait olduğu sınıfın Id'si olabilir 
        // Bu sayede hiyerarşik ilişki (Class → Method gibi) kurulabilir.
        public string ParentId { get; set; }

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

        // Metodun toplam satır sayısını tutar (Lines Of Code - LOC). Kod + yorum + boş satırlar dahil olacak şekilde hesaplanabilir
        public int TotalLines { get; set; }

        // Metodun içerisindeki yorum satırlarının sayısını tutar. (// tek satır yorumlar veya /* */ blok yorumlar)
        public int CommentLines { get; set; }

        // Toplam boş satır sayısını tutan propertydir.
        public int EmptyLines { get; set; }

        // Toplam satır sayısından (TotalLines) yorum satırları (CommentLines) ve boş satırlar (EmptyLines) çıkartılarak gerçek kod satır sayısı hesaplanır.
        // Math.Max(1, ...) kullanımı log(0) hatasını önlemek içindir.
        // Eğer metod tamamen yorumdan oluşuyorsa bile minimum 1 kabul edilir.
        public int PureCodeLines => Math.Max(1, TotalLines - CommentLines - EmptyLines);

        // Metodun dokümantasyon oranını yüzde (%) olarak hesaplayan yardımcı özellik
        // Eğer toplam satır sayısı 0'dan büyükse: yorum satırı / toplam satır * 100 formülü ile oran hesaplanır
        // Eğer toplam satır 0 ise: bölme hatasını önlemek için 0 döndürülür   
        public double CommentRatio => TotalLines > 0 ? (double)CommentLines / TotalLines * 100 : 0;

        // Metodun veya sınıfın bakım yapılabilirlik skorunu tutar. Maintainability Index, 0–100 arası hesaplanır.
        // 0  → Çok kötü, bakımı zor
        // 100 → Çok sağlıklı, bakımı kolay
        public double MaintainabilityIndex { get; set; }

        // MaintainabilityIndex değerine göre otomatik renk döndüren yardımcı özellik
        // UI tarafında ekstra if yazmadan doğrudan renk bağlanabilir
        [JsonIgnore] // Bu property JSON formatına dönüştürüldüğünde bu alan çıktıya dahil edilmez.
        public string HealthColor => MaintainabilityIndex switch
        {
            >= 80 => "#28A745", // Eğer indeks 80 ve üzerindeyse Yeşil (Sağlıklı)
            >= 50 => "#FFC107", // Eğer indeks 50 ile 79 arasındaysa Sarı (Dikkat)
            _ => "#DC3545"      // 50'nin altındaki tüm değerler Kırmızı (Kritik)
        };

        // Bir metoda ait tespit edilen tüm kod kokularını (uyarı mesajlarını) tutan liste.
        // Her metot için ayrı ayrı oluşturulur. Başlangıçta boş bir liste olarak initialize edilir.
        public List<string> CodeSmells { get; set; } = new List<string>();

        // Bu property, metoda ait herhangi bir uyarı olup olmadığını kontrol eder.
        // Eğer CodeSmells listesinde en az 1 eleman varsa true döner.
        // UI tarafında uyarı ikonu göstermek için kullanılır.
        [JsonIgnore] // Bu property JSON formatına dönüştürüldüğünde bu alan çıktıya dahil edilmez.
        public bool HasWarning => CodeSmells.Count > 0;

        // Bu property, listedeki tüm uyarıları tek bir metin haline getirir.
        // Amaç: Tooltip veya detay panelinde alt alta göstermek.
        // CodeSmells.Select(w => "• " + w) : Her uyarının başına madde işareti (•) ekler.
        // string.Join("\n", ...) : Uyarıları satır satır birleştirir.
        // Örnek çıktı: • Çok Uzun Metot, • Yüksek Karmaşıklık, • Düşük Belgeleme
        [JsonIgnore] // Bu property JSON formatına dönüştürüldüğünde bu alan çıktıya dahil edilmez.
        public string WarningSummary => string.Join("\n", CodeSmells.Select(w => "• " + w));
    }
}