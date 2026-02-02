using System;
using System.Windows; // WPF için gerekli (Window, MessageBox). Konsol değil, pencere uygulaması olduğu için var.
using Microsoft.CodeAnalysis; // Roslyn’in çekirdeği (Semantic, Symbol, Compilation vb.).
using Microsoft.CodeAnalysis.CSharp; // “Bu kod C# kodu” demek (Parse işlemi).
using Microsoft.CodeAnalysis.CSharp.Syntax; // ClassDeclarationSyntax, MethodDeclarationSyntax, InvocationExpressionSyntax (kod parçalarının türleri için).
using System.IO; // Artık kodu string olarak yazmıyoruz, DISK’ten okuyacağız.

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
        // Tüm projeden toplanan verileri burada biriktiriyoruz.
        // Her gezgin kendi küçük listesini üretir. Sonra hepsi Global listede birleşir
        public List<CodeNode> GlobalNodes = new List<CodeNode>();
        public List<CodeEdge> GlobalEdges = new List<CodeEdge>();
        public MainWindow()
        {
            InitializeComponent(); // XAML’de çizdiğimiz her şey yüklenir (Butonlar, grid’ler).

            // Artık analiz otomatik başlamıyor, kullanıcıdan klasör bekliyor.
        }

        // Butona tıklandığında çalışacak olan metot
        private void btnAnaliz_Click(object sender, RoutedEventArgs e)
        {
            // Klasör seçme penceresini açıyoruz
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "C# Projenizin Klasörünü Seçin"
            };

            if (dialog.ShowDialog() == true)
            {
                txtDurum.Text = "Analiz ediliyor, lütfen bekleyin...";

                // Seçilen klasörün tam yolu Analiz metoduna gönderilir. Analiz burada başlar.
                AnaliziBaslat(dialog.FolderName);

                txtDurum.Text = "Analiz tamamlandı!";
            }
        }

        // Sabit metin yerine klasör yolu alan yeni ana metodumuz.
        public void AnaliziBaslat(string secilenKlasorYolu)
        {
            // Önceki bilgiler ile karışmaması için yeni analize başlamadan bellek temizlenir.
            GlobalNodes.Clear();
            GlobalEdges.Clear();

            // .cs dosyalarını bulurken hata almamak için klasör var mı kontrolü.
            if (!Directory.Exists(secilenKlasorYolu)) return;

            // 1. ADIM: Klasördeki tüm .cs dosyalarını bul (Alt klasörler dahil).
            // bin --> derlenmiş çıktı    obj --> geçici dosyalar. Gerçek kaynak kodlar olmadığından çıkarılır.
            var dosyaYollari = Directory.GetFiles(secilenKlasorYolu, "*.cs", SearchOption.AllDirectories)
            .Where(dosya => !dosya.Contains("\\bin\\") && !dosya.Contains("\\obj\\"))
            .ToArray();

            // Önceden tek dosya tek ağaç. Şimdi n dosya n ağaç
            List<SyntaxTree> tumAgaclar = new List<SyntaxTree>();

            // 2. ADIM: Her dosyayı oku ve Syntax Tree oluştur.
            foreach (var dosya in dosyaYollari)
            {
                // Dosyayı oku ve ağaca ekle
                string kodIcerigi = File.ReadAllText(dosya);
                tumAgaclar.Add(CSharpSyntaxTree.ParseText(kodIcerigi));
            }

            // 3. ADIM: SEMANTİK BÜTÜNLÜK (Beyin Kurulumu)
            // Tüm dosyaları tek bir Compilation (Derleme) içine atıyoruz.
            // Böylece A dosyasındaki metot B dosyasındakini çağırınca Roslyn bunu tanıyacak.
            var referanslar = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };

            // Sanal bir derleme (proje) oluşturuyoruz. Çünkü Semantic Model ancak bir “proje” varsa oluşur.
            // Hayali projeyi oluşturup ona temel c# bilgi referansı ve analiz edilecek kod yapısı (ağaç şeklinde) verilir
            var derleme = CSharpCompilation.Create("CokluDosyaAnalizi")
            .AddReferences(referanslar)
            .AddSyntaxTrees(tumAgaclar);

            // 4. ADIM: Her bir ağaç (dosya) için gezgini çalıştır.
            foreach (var agac in tumAgaclar)
            {
                SemanticModel model = derleme.GetSemanticModel(agac);
                var gezgin = new KodGezgini(model);

                gezgin.Visit(agac.GetRoot());

                // Gezginin o dosyada bulduklarını ana listeye aktar.
                GlobalNodes.AddRange(gezgin.Nodes);
                GlobalEdges.AddRange(gezgin.Edges);
            }

            MessageBox.Show($"Analiz Bitti!\nDosya Sayısı: {tumAgaclar.Count}\nToplam Düğüm: {GlobalNodes.Count}\nToplam Bağlantı: {GlobalEdges.Count}");
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
                // Çağrılan metodun ID’si. (Aynı isimli sınıflar çakışmaz)
                string targetId = $"{sembol.ContainingSymbol.ToDisplayString()}.{sembol.Name}";

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