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
        // Analiz edilen tüm sınıflar ve metotlar (Hiyerarşik yapıda)
        public List<CodeNode> Nodes { get; set; } = new List<CodeNode>();

        // Analiz edilen tüm bağlantılar (Çağrılar ve Kalıtımlar)
        public List<CodeEdge> Edges { get; set; } = new List<CodeEdge>();
    }
}