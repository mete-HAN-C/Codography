using Codography.Models; // Model katmanındaki sınıfları (CodeNode, CodeEdge vb.) kullanabilmek için eklenir.

namespace Codography.ViewModels
{
    // GraphNodeViewModel sınıfı, ekranda çizilecek bir KUTUYU temsil eder.
    // Yani MSAGL’den gelen veriyi WPF’in anlayacağı hale getirir.
    public class GraphNodeViewModel
    {
        // Asıl veri modelidir. İçinde Id, Name, Type gibi gerçek kod bilgileri bulunur.
        // UI tarafında yazı, renk vb. gösterimler için kullanılır.
        public CodeNode Data { get; set; }

        // WPF CANVAS KOORDİNATLARI

        // Canvas üzerindeki sol konum (yatay eksen)
        public double X { get; set; }

        // Canvas üzerindeki üst konum (dikey eksen)
        public double Y { get; set; }

        // Kutunun genişliği
        public double Width { get; set; }

        // Kutunun yüksekliği
        public double Height { get; set; }
    }

    // GraphEdgeViewModel sınıfı, ekranda çizilecek bir ÇİZGİYİ (bağlantıyı) temsil eder.
    public class GraphEdgeViewModel
    {
        // Bağlantının asıl veri modelidir. SourceId, TargetId ve ilişki tipi burada tutulur.
        // (Çizgi kimden kime gidiyor? vb.) bilgilerini tutar
        public CodeEdge Data { get; set; }

        // Çizginin geçeceği noktalardır. (X, Y) koordinat çiftleri içerir.
        // Özellikle kıvrımlı (routing) çizimler için kullanılır.
        public List<(double X, double Y)> RoutingPoints { get; set; } = new List<(double X, double Y)>();
    }
}
