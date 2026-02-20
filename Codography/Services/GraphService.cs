using Codography.Models;
using Microsoft.Msagl.Core.Geometry.Curves; // MSAGL'nin geometrik şekillerini (dikdörtgen, eğri vs.) oluşturmak için eklenir. CurveFactory burada bulunur.
using Microsoft.Msagl.Core.Layout; // MSAGL'nin temel grafik yapısını (GeometryGraph, Node vb.) kullanmak için eklenir.
using Microsoft.Msagl.Layout.Layered; // Katmanlı (hiyerarşik) yerleşim algoritması olan Sugiyama burada bulunur. Sınıfları alt alta veya katmanlı dizmek için kullanılır.

namespace Codography.Services
{
    // GraphService sınıfı, IGraphService interface'ini implemente eder. Yani CalculateLayout metodunu yazmak zorundadır.
    public class GraphService : IGraphService
    {
        // Analiz sonucu gelen Node'lara göre otomatik yerleşim hesaplayan metot.
        public GeometryGraph CalculateLayout(ProjectAnalysisResult analysisResult)
        {
            // 1. MSAGL'nin Geometri Grafiğini oluşturuyoruz. Bu boş bir tuval gibi düşünülebilir.
            var geometryGraph = new GeometryGraph();

            // string tipinde bir anahtar (key) ve Node tipinde bir değer (value) tutan yeni bir Dictionary (sözlük) oluşturuyoruz.
            // Dictionary → Anahtar-Değer (Key-Value) mantığıyla çalışır.
            // Burada: Key = string (örneğin Node'un Id'si) 
            // Value = Node (örneğin MSAGL Node nesnesi)
            var nodeMap = new Dictionary<string, Node>();

            // 2. Kendi CodeNode nesnelerimizi, MSAGL'nin anlayacağı Node nesnelerine çeviriyoruz.
            // MSAGL, düğümleri üst üste bindirmemek için boyutlarını (Genişlik/Yükseklik) bilmek ister.
            // Şimdilik varsayılan 120x60 boyutunda kutular varsayıyoruz.
            foreach (var codeNode in analysisResult.Nodes)
            {
                // 120 genişlik, 60 yükseklik olacak şekilde bir dikdörtgen oluşturuyoruz.
                // (0,0) başlangıç noktasıdır, yerleşim algoritması bunu daha sonra değiştirecek.
                var boundaryCurve = CurveFactory.CreateRectangle(120, 60, new Microsoft.Msagl.Core.Geometry.Point(0, 0));

                // Oluşturduğumuz dikdörtgeni kullanarak bir MSAGL Node nesnesi oluşturuyoruz.
                var msaglNode = new Node(boundaryCurve);

                // Kendi gerçek veri modelimizi (CodeNode), MSAGL Node'unun içine UserData olarak saklıyoruz.
                // Böylece layout sonrası hangi grafik düğümünün hangi CodeNode'a ait olduğunu bileceğiz.
                msaglNode.UserData = codeNode;

                // Oluşturduğumuz MSAGL Node'u geometri grafiğine ekliyoruz.
                geometryGraph.Nodes.Add(msaglNode);

                // Eğer codeNode.Id boş (null) değilse VE boş string ("") değilse VE bu Id daha önce nodeMap içinde yoksa
                if (!string.IsNullOrEmpty(codeNode.Id) && !nodeMap.ContainsKey(codeNode.Id))
                {
                    // nodeMap sözlüğüne yeni bir kayıt eklenir.
                    // Key  → codeNode.Id  (string)
                    // Value → msaglNode   (Node nesnesi)
                    nodeMap.Add(codeNode.Id, msaglNode);
                }
            }

            // 3. Düğümler arasındaki bağlantıları (Edge) oluşturuyoruz.
            // Eğer analiz sonucunda Edge listesi null değilse (yani gerçekten bağlantılar varsa)
            if (analysisResult.Edges != null)
            {
                // Tüm bağlantıları (CodeEdge) tek tek geziyoruz
                foreach (var codeEdge in analysisResult.Edges)
                {
                    // nodeMap sözlüğünde:
                    // SourceId'ye karşılık gelen Node'u bulmaya çalışıyoruz.
                    // TargetId'ye karşılık gelen Node'u bulmaya çalışıyoruz.
                    // TryGetValue → Eğer bulursa true döner ve ilgili Node'u out değişkene atar.
                    // Eğer iki kutu da sözlükte varsa, aralarına çizgi çekeceğiz.
                    if (nodeMap.TryGetValue(codeEdge.SourceId, out Node sourceNode) &&
                        nodeMap.TryGetValue(codeEdge.TargetId, out Node targetNode))
                    {

                        // Eğer hem kaynak hem hedef düğüm bulunduysa, aralarına bir MSAGL Edge (kenar) oluşturuyoruz.
                        var msaglEdge = new Edge(sourceNode, targetNode);

                        // Orijinal CodeEdge verisini, MSAGL Edge'in içine UserData olarak saklıyoruz.
                        // Böylece ileride renk, stil vb. değişiklik yapabiliriz.
                        msaglEdge.UserData = codeEdge;

                        // Oluşturulan kenarı geometri grafiğine ekliyoruz.
                        geometryGraph.Edges.Add(msaglEdge);
                    }
                }
            }

            // 4. Yerleşim Ayarlarını belirliyoruz.
            // Sugiyama algoritması hiyerarşik (katmanlı) bir düzen oluşturur.
            var settings = new SugiyamaLayoutSettings
            {
                NodeSeparation = 30,  // Aynı katmandaki kutular arasındaki yatay boşluk
                LayerSeparation = 50, // Katmanlar arası dikey boşluk
                Transformation = PlaneTransformation.Rotation(Math.PI / 2) // Grafiği döndürmek için dönüşüm uygular. Math.PI / 2 = 90 derece. Grafik yataydan dikeye (veya tam tersi) çevrilebilir.
            };

            // 5. Yerleşim Hesaplamasını Başlatıyoruz.
            // LayeredLayout sınıfı verilen graph ve ayarlara göre tüm Node'ların X,Y koordinatlarını hesaplar.
            var layout = new LayeredLayout(geometryGraph, settings);

            // Algoritmayı çalıştırıyoruz.
            layout.Run();

            // Bu noktadan sonra: geometryGraph içindeki her Node'un Center (X,Y) değeri dolmuştur.
            // Yani artık her kutunun ekranda nereye çizileceği bellidir.

            // Hesaplanmış grafiği geri döndürüyoruz.
            return geometryGraph;
        }
    }
}