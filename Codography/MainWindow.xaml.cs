using System;
using System.Windows; // WPF için gerekli (Window, MessageBox). Konsol değil, pencere uygulaması olduğu için var.
using Microsoft.CodeAnalysis; // Roslyn’in çekirdeği (Semantic, Symbol, Compilation vb.).
using Microsoft.CodeAnalysis.CSharp; // “Bu kod C# kodu” demek (Parse işlemi).
using Microsoft.CodeAnalysis.CSharp.Syntax; // ClassDeclarationSyntax, MethodDeclarationSyntax, InvocationExpressionSyntax (kod parçalarının türleri için).

namespace Codography
{
    // Enum = seçenek listesi yani “Bu şey sadece şunlardan biri olabilir”.
    // Bir düğüm (Node) ya (Class) sınıftır ya da (Method) metottur.
    // Edge = İki düğüm arasındaki ilişkinin türüdür.
    // Bir kenar (Edge) ya Metot → Metot çağrısı (Call) ya da Class → Class (Inheritance) tır.
    public enum NodeType { Class, Method }
    public enum EdgeType { Call, Inheritance }

    // Her bir görsel objeyi temsil eder
    public class CodeNode
    {
        public string Id { get; set; } // Genellikle "Namespace.Class.Method" şeklinde benzersiz olmalı. Aynı isimli yapıları ayırmak için Id kullanılır.
        public string Name { get; set; } // Görselde yazılacak ve kullanıcıya gösterilecek isim “Calistir”, “MotoruKontrolEt”.
        public NodeType Type { get; set; } // Bu düğüm "Class" mı yoksa Method mu? ayrımı için.
    }

    // İki obje arasındaki çizgiyi temsil eder
    public class CodeEdge
    {
        public string SourceId { get; set; } // Çizginin başladığı yer ve Çağıran metot.
        public string TargetId { get; set; } // Çizginin bittiği yer ve Çağrılan metot
        public EdgeType Type { get; set; } // Çizginin anlamı "Call" mu "İnheritance" mı ?
    }
    public partial class MainWindow : Window  // Pencere sınıfından türer çünkü bu ana ekran bir pencere. Diğer yarısı ise XAML’de
    {
        public MainWindow()
        {
            InitializeComponent(); // XAML’de çizdiğimiz her şey yüklenir (Butonlar, grid’ler).

            // Program başlar başlamaz analizi yapması için buraya çağırıyoruz.
            AnaliziBaslat();
        }

        public void AnaliziBaslat()
        {
            // 1. Adım: Analiz edilecek metin(string).
            // Pc henüz bunun C# kodu olduğunu bilmiyor metin olarak görüyor.
            string kodMetni = @"
                class Araba {
                    void Calistir() {
                        MotoruKontrolEt(); // Buradaki çağrıyı yakalayacağız!
                    }
                    void MotoruKontrolEt() { }
                }";

            // 2. Adım: Ağaca dönüştürme
            // ParseText komutu, düz metni alıp parçalar, her bir kelimeyi (class, void, {, }) analiz eder ve en son bunlar bir Sözdizimi Ağacı (Syntax Tree) yapısı haline getirilir.
            SyntaxTree agac = CSharpSyntaxTree.ParseText(kodMetni);
            // Şuan metin olan kod ilk kez Class → Method → Invocation şeklinde ağaç yapısına dönüştü.

            // --- SEMANTİK HAZIRLIK ---
            // Eğer referans yazmazsak semantic model çalışmaz.
            // Roslyn'e "Temel C# bilgilerini (object, string vb.) kullan diyoruz.
            var referanslar = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };

            // Sanal bir derleme (proje) oluşturuyoruz. Çünkü Semantic Model ancak bir “proje” varsa oluşur.
            // Hayali projeyi oluşturup ona temel c# bilgi referansı ve analiz edilecek kod yapısı (ağaç şeklinde) verilir
            var derleme = CSharpCompilation.Create("AnalizProjem")
            .AddReferences(referanslar)
            .AddSyntaxTrees(agac);

            // Artık sahte proje ile kod yapısına uygun anlam haritasını (Semantic Model) çıkartabiliriz. Semantic model = Syntax Tree + Referans --> Sahte proje
            SemanticModel model = derleme.GetSemanticModel(agac);

            // 3. Adım: Kökü alma
            // Her ağacın bir kökü vardır. kok değişkeni, kodun en dış katmanını (dosyanın kendisini) temsil eder. Tüm sınıflar ve metotlar bu kökün altındadır.
            CompilationUnitSyntax kok = agac.GetCompilationUnitRoot();

            // 4. Adım: Gezgini çalıştırma
            // Gezgini oluştururken içine "model"i (semantic modeli) de beyin nakli gibi gönderiyoruz.
            var gezgin = new KodGezgini(model);
            // Gezgini en tepeden başlattık ve her yeri dolaşacak. Eğer sadece belirli bir metodun içini merak etseydik, gezgin.Visit(metotNode) da diyebilirdik. Yani gezgin sadece verdiğimiz düğümden aşağısını tarar.
            gezgin.Visit(kok);

            // TEST: Kaç tane düğüm ve bağlantı bulduk?
            MessageBox.Show($"Analiz Tamamlandı!\nToplam Düğüm: {gezgin.Nodes.Count}\nToplam Bağlantı: {gezgin.Edges.Count}");
        }
    }

    // GEZGİN SINIFI
    // Bu sınıf, kodun içinde dolaşan bir gezgindir. CSharpSyntaxWalker sınıfından miras aldığı için C# kodunun içindeki her bir parçayı (token) tek tek ziyaret etme yeteneğine sahiptir.
    public class KodGezgini : CSharpSyntaxWalker
    {
        // Gezgin her yerde senantic model ile anlam sorabilsin diye bu değişkeni tanımladık.
        private readonly SemanticModel _model;

        // Veri havuzlarımız
        public List<CodeNode> Nodes { get; } = new List<CodeNode>(); // Nodes listesi Bulunan sınıf + metotları tutar.
        public List<CodeEdge> Edges { get; } = new List<CodeEdge>(); // Metot çağrılarını tutar.

        // Gezgin Class’a girince → _currentClassId set edilir.
        // Gezgin Method’a girince → _currentMethodId set edilir.
        // Gezgin Invocation görünce → “Bu çağrı, şu metodun içinden geldi”
        private string _currentClassId;
        private string _currentMethodId;

        // Kurucu Metot: Gezgin oluşturulurken modeli (senantic modeli) yani beynini ona teslim ediyoruz
        public KodGezgini(SemanticModel model)
        {
            _model = model;
        }

        // Gezgin kodun içinde bir Sınıf (Class) gördüğü anda bu metot tetiklenir.
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // “Şu an bu sınıftayız”
            _currentClassId = node.Identifier.Text;

            // Sınıfı bir düğüm olarak ekle
            Nodes.Add(new CodeNode
            {
                Id = _currentClassId,
                Name = node.Identifier.Text,
                Type = NodeType.Class
            });

            // Eğer bu satır yazılmazsa, gezgin sınıfın kapısından içeri girmez. İçerideki metotları da görmesi için "yoluna devam et" komutu vermen gerekir.
            base.VisitClassDeclaration(node);
        }

        // Gezgin kodun içinde bir Metot (Method) gördüğü anda bu metot tetiklenir.
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // “Şu an bu metottayız”
            _currentMethodId = $"{_currentClassId}.{node.Identifier.Text}";

            // Metodu bir düğüm olarak ekle
            Nodes.Add(new CodeNode
            {
                Id = _currentMethodId,
                Name = node.Identifier.Text,
                Type = NodeType.Method
            });

            base.VisitMethodDeclaration(node);
        }

        // --- METOT ÇAĞRILARI YAKALAMA ---
        // Kod içinde bir metot çağrıldığında (Örn: MotoruKontrolEt();) burası çalışır.
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // Semantik modelden bu çağrının kimliğini soruyoruz. _model.GetSymbolInfo(node).Symbol : “Bu çağrı hangi metodu çağırıyor?” as IMethodSymbol : "Bu gerçekten bir metot mu?"
            var sembol = _model.GetSymbolInfo(node).Symbol as IMethodSymbol;

            // Eğer gerçekten bir metotsa ve biz şuan bir metot içindeysek
            if (sembol != null && _currentMethodId != null)
            {
                // Çağrılan metodun ID’si
                string targetId = $"{sembol.ContainingSymbol.Name}.{sembol.Name}";

                // ÇİZGİYİ (EDGE) EKLE: Kaynak metodumdan hedef metoda bir çağrı var
                // “Bu metot, şu metodu çağırıyor” bilgisi.
                Edges.Add(new CodeEdge
                {
                    SourceId = _currentMethodId,
                    TargetId = targetId,
                    Type = EdgeType.Call
                });
            }
            base.VisitInvocationExpression(node);
        }
    }
}