using Codography.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.ConstrainedExecution;
using System.Security.Claims;
using System.Windows.Controls;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Text.Json;

namespace Codography.Services
{
    public class AnalysisService
    {
        // Yapılan en son analiz sonucunu tutar. Dışarıdan sadece okunabilir, sınıf dışından değiştirilemez. Böylece analiz sonucu kontrolsüz şekilde ezilmez
        public ProjectAnalysisResult LastResult { get; private set; }
        // Eskiden AnaliziBaslat metodu void tipindeydi.Analiz sonuçları, içindeki GlobalNodes ve GlobalEdges isimli genel listelere ekleniyordu.
        // Metot artık doğrudan ProjectAnalysisResult tipinde bir nesne döndürüyor. Artık veriyi saklamak yerine, analizi yapıp sonucu teslim ediyor.
        public ProjectAnalysisResult AnaliziBaslat(string secilenKlasorYolu)
        {
            var result = new ProjectAnalysisResult();

            if (!Directory.Exists(secilenKlasorYolu)) return result;

            // 1. Dosyaları bul
            // Klasör içindeki tüm .cs dosyalarını bulur; ancak bin ve obj gibi gereksiz klasörleri pas geçer.
            var dosyaYollari = Directory.GetFiles(secilenKlasorYolu, "*.cs", SearchOption.AllDirectories)
                .Where(dosya => !dosya.Contains("\\bin\\") && !dosya.Contains("\\obj\\"))
                .ToArray();

            List<SyntaxTree> tumAgaclar = new List<SyntaxTree>();

            // 2. Syntax Ağaçlarını oluştur
            // Her bir kod dosyası okunur ve bunlar birer SyntaxTree (kod ağacı) haline getirilir.
            foreach (var dosya in dosyaYollari)
            {
                string kodIcerigi = File.ReadAllText(dosya);
                tumAgaclar.Add(CSharpSyntaxTree.ParseText(kodIcerigi));
            }

            // 3. Roslyn Derleme
            // Roslyn analizinde kullanılacak temel .NET derleme referansları tanımlanır
            // Bu referanslar olmadan System, Console, Collections gibi tipler çözülemez
            var referanslar = new List<MetadataReference>
            {
                // mscorlib / System.Private.CoreLib. object, string, int gibi temel .NET tiplerini çözebilmek için eklenir
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),

                // System.Runtime. DateTime, Task gibi runtime tiplerinin çözülebilmesi için gereklidir
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Runtime.dll")),

                // System.Collections. List<T>, Dictionary<TKey, TValue> gibi koleksiyon tipleri için eklenir
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Collections.dll")),

                // System.Console. Console.WriteLine gibi console API'lerinin çözülebilmesi için eklenir
                MetadataReference.CreateFromFile(Path.Combine(Path.GetDirectoryName(typeof(object).Assembly.Location), "System.Console.dll"))
            };

            // Ardından bir Compilation (derleme) nesnesi oluşturularak kodun anlamlandırılması (semantik model) sağlanır.
            var derleme = CSharpCompilation.Create("CodographyAnalysis")
                .AddReferences(referanslar)
                .AddSyntaxTrees(tumAgaclar);

            // 4. Gezgin ile TÜM verileri topla
            // Her kod dosyası için bir KodGezgini(Gezgin) oluşturulur. Gezgin, kodun içine girer:
            foreach (var agac in tumAgaclar)
            {
                // BEYİN: Roslyn o anki dosyanın anlam haritasını çıkarıyor
                SemanticModel model = derleme.GetSemanticModel(agac);

                // KodGezgini.cs sınıfına gidip gezgin oluşturulur.
                var gezgin = new KodGezgini(model);

                // Gezgin gezmeye başlar.
                gezgin.Visit(agac.GetRoot());

                // Gezgin işini bitirip tüm verilerini ProjectAnalysisResult.cs sınıfına tek tek taşır.
                result.Nodes.AddRange(gezgin.Nodes);
                result.Edges.AddRange(gezgin.Edges);
            }
            // Tüm dosyalar taranıp her şey result içine düz bir liste olarak aktarıldı ama veriler karışık durumda.
            // Bu yüzden result içindeki veriler OrganizeHierarchy metodu ile sıralanır.
            OrganizeHierarchy(result);

            // Oluşturulan ilişkilerden ters yönlü erişim (reverse lookup) yapısı hazırlanır. Böylece bir düğüme kimlerin eriştiği bilgisi hızlıca bulunabilir
            BuildReverseLookup(result);

            // Oluşturulan analiz sonucu, servis içinde "son analiz" olarak saklanır. Daha sonra tekrar erişilebilmesi için LastResult alanına atanır
            LastResult = result;
            return result;
        }

        // Analiz sonucundaki ilişkilerden ters yönlü bir arama (reverse lookup) yapısı oluşturur
        // Bu yapı sayesinde: "Bu düğüme kimler erişiyor?" sorusu hızlıca cevaplanabilir
        private void BuildReverseLookup(ProjectAnalysisResult result)
        {
            // Daha önce oluşturulmuş ters arama verileri varsa temizlenir. Böylece eski analizden kalan bilgiler karışmaz
            result.ReverseLookup.Clear();

            // Analizdeki tüm ilişkiler (edge) tek tek dolaşılır
            foreach (var edge in result.Edges)
            {
                // Eğer hedef ID daha önce sözlüğe eklenmemişse bu hedef için yeni bir kaynak listesi oluşturulur
                if (!result.ReverseLookup.ContainsKey(edge.TargetId))
                {
                    result.ReverseLookup[edge.TargetId] = new List<string>();
                }

                // Aynı kaynak ID'nin, aynı hedefin listesine daha önce eklenip eklenmediği kontrol edilir
                // Böylece mükerrer (tekrarlı) kayıtların oluşması engellenir
                if (!result.ReverseLookup[edge.TargetId].Contains(edge.SourceId))
                {
                    // Kaynağı, hedefe erişenler listesine ekle
                    result.ReverseLookup[edge.TargetId].Add(edge.SourceId);
                }
            }
        }

        // Eskiden Hiyerarşi kurma (yani hangi metodun hangi sınıfa ait olduğu) işlemi analiz sırasında değil, MainWindow.xaml.cs içinde PopulateTreeView metodu çalışırken arayüz tarafında yapılıyordu.
        // Artık OrganizeHierarchy metodu eklendi. Bu metot, CodeNode içindeki Children listesini kullanarak sınıfları ve metotları servis katmanında birbirine bağlıyor. Artık arayüzün (UI) bu teknik detaylarla uğraşmasına gerek kalmıyor.
        private void OrganizeHierarchy(ProjectAnalysisResult result)
        {
            // Tekilleştirme (Aynı sınıf farklı dosyalarda "partial" olarak bulunabilir)
            // Tüm düğümleri ID'lerine (kimliklerine) göre grupluyor ve her gruptan sadece "birinciyi" alıyoruz. Böylece kopyaları çöpe atmış oluyoruz.
            // Sonuç: allNodes artık içinde "tekil" (benzersiz) düğümlerin olduğu tertemiz bir listedir.
            var allNodes = result.Nodes.GroupBy(n => n.Id).Select(g => g.First()).ToList();

            // Elimizdeki karışık veri listesini sadece Sınıf(Class) olanları seçip classes isimli bir "üst liste" oluşturuyoruz. Bu bizim ana klasörlerimiz olacak.
            var classes = allNodes.Where(n => n.Type == NodeType.Class).ToList();

            // Her bir sınıfı sırayla eline alır (Örneğin şu an elimizde Araba sınıfı var).
            foreach (var cls in classes)
            {
                // allNodes içindeki metotlara bakar. Eğer metodun ID'si "Araba." ile başlıyorsa (Örn: Araba.Calistir()), bu metodu o sınıfın metot listesine (methodsOfClass) dahil eder.
                var methodsOfClass = allNodes.Where(n =>
                    n.Type == NodeType.Method &&
                    n.Id.StartsWith(cls.Id + ".")).ToList();

                // Çift eklemeyi önlemek için temizleyip ekliyoruz
                // Önce sınıfın içini temizliyoruz.
                // Bulduğu o metotları, o an elinde tuttuğu cls (Araba) nesnesinin içindeki Children listesine kopyalar.
                cls.Children.Clear();
                cls.Children.AddRange(methodsOfClass);
            }
            // Önce result.Nodes içinde karışık (sınıf+metotlar) vardı. Biz metotları sınıfların içine taşıdık ama onlar hala ana listede de duruyorlar.
            // Ana listeyi (result.Nodes) tamamen siliyoruz ve yerine sadece Sınıfları koyuyoruz.
            // Ana listede sadece sınıflar kalsın
            result.Nodes = classes.Cast<CodeNode>().ToList();
        }

        // Verilen analiz sonucunu JSON formatına çevirip belirtilen dosya yoluna kaydeder
        public void SaveResultToJson(ProjectAnalysisResult result, string filePath)
        {
            // JSON yazım ayarları belirlenir. WriteIndented = true sayesinde JSON daha okunabilir (satır satır, girintili) olur
            var options = new JsonSerializerOptions { WriteIndented = true };

            // Analiz sonucu nesnesi JSON metnine dönüştürülür
            string jsonString = JsonSerializer.Serialize(result, options);

            // Oluşturulan JSON metni belirtilen dosya yolundaki dosyaya yazılır. Dosya yoksa oluşturulur, varsa üzerine yazılır
            File.WriteAllText(filePath, jsonString);
        }

        // Daha önce kaydedilmiş olan JSON dosyasını okuyup analiz sonucunu geri yükler
        public ProjectAnalysisResult LoadResultFromJson(string filePath)
        {
            // Dosya gerçekten var mı kontrol edilir. Yoksa null döndürülerek yükleme işlemi sonlandırılır
            if (!File.Exists(filePath)) return null;

            // JSON dosyasının içeriği metin olarak okunur
            string jsonString = File.ReadAllText(filePath);

            // Okunan JSON metni tekrar ProjectAnalysisResult nesnesine dönüştürülür
            return JsonSerializer.Deserialize<ProjectAnalysisResult>(jsonString);
        }
    }
}