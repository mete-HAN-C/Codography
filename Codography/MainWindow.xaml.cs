using System;
using System.Windows; // WPF için gerekli (Window, MessageBox). Konsol değil, pencere uygulaması olduğu için var.
using Microsoft.CodeAnalysis; // Roslyn’in çekirdeği (Semantic, Symbol, Compilation vb.).
using Microsoft.CodeAnalysis.CSharp; // “Bu kod C# kodu” demek (Parse işlemi).
using Microsoft.CodeAnalysis.CSharp.Syntax; // ClassDeclarationSyntax, MethodDeclarationSyntax, InvocationExpressionSyntax (kod parçalarının türleri için).

namespace Codography
{
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
        }
    }

    // GEZGİN SINIFI
    // Bu sınıf, kodun içinde dolaşan bir gezgindir. CSharpSyntaxWalker sınıfından miras aldığı için C# kodunun içindeki her bir parçayı (token) tek tek ziyaret etme yeteneğine sahiptir.
    public class KodGezgini : CSharpSyntaxWalker
    {
        // Gezgin her yerde senantic model ile anlam sorabilsin diye bu değişkeni tanımladık.
        private readonly SemanticModel _model;

        // Kurucu Metot: Gezgin oluşturulurken modeli (senantic modeli) yani beynini ona teslim ediyoruz
        public KodGezgini(SemanticModel model)
        {
            _model = model;
        }

        // Gezgin kodun içinde bir Sınıf (Class) gördüğü anda bu metot tetiklenir.
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // WPF'te Console.WriteLine yerine MessageBox kullanarak sonucu ekranda görebiliriz.
            // Sınıfın ismini (örneğin: "Araba") verir.
            MessageBox.Show("Bulunan Sınıf: " + node.Identifier.Text);

            // Eğer bu satır yazılmazsa, gezgin sınıfın kapısından içeri girmez. İçerideki metotları da görmesi için "yoluna devam et" komutu vermen gerekir.
            base.VisitClassDeclaration(node);
        }

        // Gezgin kodun içinde bir Metot (Method) gördüğü anda bu metot tetiklenir.
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Metodun ismini (örneğin: "Calistir") verir.
            // Identifier: Neden node.Name değil de node.Identifier? Çünkü Roslyn dünyasında her şeyin bir "kimliği" (Identifier) vardır. Bu sadece bir metin değil, dilin bir parçasıdır.
            MessageBox.Show("İncelenen Metot Gövdesi: " + node.Identifier.Text);
            base.VisitMethodDeclaration(node);
        }

        // --- METOT ÇAĞRILARI YAKALAMA ---
        // Kod içinde bir metot çağrıldığında (Örn: MotoruKontrolEt();) burası çalışır.
        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // Semantik modelden bu çağrının kimliğini soruyoruz. _model.GetSymbolInfo(node).Symbol : “Bu çağrı hangi metodu çağırıyor?” as IMethodSymbol : "Bu gerçekten bir metot mu?"
            var sembol = _model.GetSymbolInfo(node).Symbol as IMethodSymbol;

            // Eğer gerçekten bir metotsa
            if (sembol != null)
            {
                // sembol.Name: Çağrılan metodun adı
                // sembol.ContainingSymbol.Name: O metodun bulunduğu sınıf adı.
                MessageBox.Show($"BAĞLANTI BULDUM!\n" +
                $"Şu anki kod, {sembol.ContainingSymbol.Name} sınıfındaki " +
                $"{sembol.Name} metodunu çağırıyor.");
            }
            // ÇIKTI : BAĞLANTI BULDUM! Şu anki kod, Araba sınıfındaki MotoruKontrolEt metodunu çağırıyor.

            base.VisitInvocationExpression(node);
        }
    }
}