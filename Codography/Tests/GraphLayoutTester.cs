using Codography.Models; // Projede tanımlı model sınıflarını (ProjectAnalysisResult, CodeNode, CodeEdge vb.) kullanabilmek için
using Codography.Services; // GraphService ve IGraphService'i kullanabilmek için servis katmanını projeye dahil ediyoruz.
using System.Diagnostics; // Debug.WriteLine ile çıktıları Visual Studio Output penceresine yazmak için gerekli.

namespace Codography.Tests
{
    /// <summary>
    /// Bu sınıf gerçek bir test frameworkü (xUnit, NUnit vb.) kullanmadan,
    /// manuel olarak GraphService'in doğru çalışıp çalışmadığını kontrol etmek için yazılmıştır.
    /// Ama mantık olarak Unit Test (Arrange-Act-Assert) yapısını taklit eder.
    /// </summary>

    // static : Bu sınıftan nesne üretmeye gerek yoktur. Direkt GraphLayoutTester.RunTest() şeklinde çağrılabilir.
    public static class GraphLayoutTester
    {
        // Testi başlatan ana metot
        public static void RunTest()
        {
            // Test başlangıç bilgisini Output ekranına yazıyoruz.
            Debug.WriteLine("=== GRAPH LAYOUT TESTİ BAŞLIYOR ===");

            // 1) ARRANGE (Hazırlık Aşaması)
            // Test için sahte (mock) bir analiz sonucu oluşturuyoruz. Normalde bu veri gerçek kod analizinden gelecek.
            var mockResult = new ProjectAnalysisResult();

            // 3 adet sahte düğüm (Node) oluşturuyoruz. Bunlar örnek olarak 3 sınıfı temsil ediyor.
            mockResult.Nodes.Add(new CodeNode { Id = "Node_A", Name = "ClassA", Type = NodeType.Class });
            mockResult.Nodes.Add(new CodeNode { Id = "Node_B", Name = "ClassB", Type = NodeType.Class });
            mockResult.Nodes.Add(new CodeNode { Id = "Node_C", Name = "ClassC", Type = NodeType.Class });

            // Düğümler arasına sahte bağlantılar (Edge) ekliyoruz.
            // A → B arasında Call ilişkisi
            // A → C arasında Inheritance ilişkisi
            mockResult.Edges.Add(new CodeEdge { SourceId = "Node_A", TargetId = "Node_B", Type = EdgeType.Call });
            mockResult.Edges.Add(new CodeEdge { SourceId = "Node_A", TargetId = "Node_C", Type = EdgeType.Inheritance });

            // Test edeceğimiz servisi oluşturuyoruz. Gerçek uygulamada bu Dependency Injection ile gelecek.
            IGraphService graphService = new GraphService();

            // 2) ACT (Çalıştırma Aşaması)
            // Layout algoritmasını çağırıyoruz. Varsayılan olarak Sugiyama algoritması çalışır.
            var geometryGraph = graphService.CalculateLayout(mockResult);

            // 3) ASSERT (Doğrulama Aşaması)

            // Testin başarılı olup olmadığını takip edeceğimiz değişken.
            bool isSuccess = true;

            // Eğer sonuç null geldiyse test direkt başarısızdır.
            if (geometryGraph == null)
            {
                Debug.WriteLine("HATA: GeometryGraph null döndü!");
                return;
            }

            // Oluşan düğüm ve bağlantı sayılarını yazdırıyoruz.
            Debug.WriteLine($"Test Edilen Düğüm Sayısı: {geometryGraph.Nodes.Count}");
            Debug.WriteLine($"Test Edilen Bağlantı Sayısı: {geometryGraph.Edges.Count}");

            // Her bir düğüm için koordinat kontrolü yapıyoruz.
            foreach (var node in geometryGraph.Nodes)
            {
                // MSAGL tarafından hesaplanan merkez koordinatları
                double x = node.Center.X;
                double y = node.Center.Y;

                // Düğümün boyut bilgileri (GraphService içinde 120x60 verilmişti)
                double width = node.BoundingBox.Width;
                double height = node.BoundingBox.Height;

                // UserData içinde bizim orijinal CodeNode saklanmıştı.
                var originalNode = (CodeNode)node.UserData;

                // Hesaplanan değerleri ekrana yazdırıyoruz.
                Debug.WriteLine($"Düğüm ID: {originalNode.Id} | Merkez Koordinatı: (X: {x:F2}, Y: {y:F2}) | Boyut: {width}x{height}");

                // Temel matematiksel doğrulama: Koordinatlar NaN veya Infinity olmamalı.
                // Eğer öyleyse algoritma düzgün çalışmamış demektir.
                if (double.IsNaN(x) || double.IsNaN(y) || double.IsInfinity(x) || double.IsInfinity(y))
                {
                    Debug.WriteLine($"HATA: Düğüm {originalNode.Id} için geçersiz koordinat hesaplandı!");
                    isSuccess = false;
                }
            }

            // Sonuç değerlendirmesi
            if (isSuccess)
            {
                Debug.WriteLine(">>> TEST BAŞARILI: Tüm düğümlere geçerli koordinatlar atandı.");
            }
            else
            {
                Debug.WriteLine(">>> TEST BAŞARISIZ: Bazı düğümlerin koordinatları hesaplanamadı.");
            }

            // Testin bittiğini belirtiyoruz.
            Debug.WriteLine("=== GRAPH LAYOUT TESTİ BİTTİ ===");
        }
    }
}
