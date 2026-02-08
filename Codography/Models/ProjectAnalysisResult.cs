using System.Collections.Generic;

// Eskiden analiz sonuçları (GlobalNodes ve GlobalEdges), AnalysisService sınıfı içerisinde iki ayrı liste olarak tutuyorduk. Artık tüm sonuçları ProjectAnalysisResult isimli tek bir "konteynır" sınıfında topladık.
// Bu yeni yapı sayesinde AnalysisService artık doğrudan bir ProjectAnalysisResult nesnesi döndürecek. Tüm analiz verisini tek bir paket olarak taşıyabiliriz.
namespace Codography.Models
{
    /// <summary>
    /// Tek bir analiz işleminin sonucunda üretilen tüm verileri (Düğümler ve İlişkiler) barındıran paket sınıfı.
    /// </summary>
    public class ProjectAnalysisResult
    {
        // Analiz edilen projenin adını tutar. Eğer kullanıcı özel bir isim vermezse varsayılan olarak "Adsız Proje" atanır
        public string ProjectName { get; set; } = "Adsız Proje";

        // Analizin yapıldığı tarih ve saati tutar. Nesne oluşturulduğu anda otomatik olarak o anki tarih/saat atanır. Daha sonra analiz ne zaman yapıldı diye göstermek için kullanılır
        public DateTime AnalysisDate { get; set; } = DateTime.Now;

        // Analiz edilen tüm sınıflar ve metotlar (Hiyerarşik yapıda)
        public List<CodeNode> Nodes { get; set; } = new List<CodeNode>();

        // Analiz edilen tüm bağlantılar (Çağrılar ve Kalıtımlar)
        public List<CodeEdge> Edges { get; set; } = new List<CodeEdge>();

        // TERS İNDEKS (Reverse Lookup):
        // Bir düğümün (TargetId) hangi düğümler tarafından kullanıldığını hızlıca bulmak için kullanılır
        // Key   : Hedef düğümün Id'si (örneğin bir sınıf, metot veya değişken)
        // Value : Bu hedef düğümü kullanan kaynak düğümlerin Id listesi
        // Amaç  : "Bu metodu / değişkeni kimler kullanıyor?" sorusuna O(1) erişimle cevap verebilmek
        public Dictionary<string, List<string>> ReverseLookup { get; set; } = new Dictionary<string, List<string>>();
    }
}