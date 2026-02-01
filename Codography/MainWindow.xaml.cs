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
                using System;
                namespace Projem {
                    class Hesaplayici {
                        void Topla() { }
                        void Cikar() { }
                    }
                }";

            // 2. Adım: Ağaca dönüştürme
            // ParseText komutu, düz metni alıp parçalar, her bir kelimeyi (class, void, {, }) analiz eder ve en son bunlar bir Sözdizimi Ağacı (Syntax Tree) yapısı haline getirilir.
            SyntaxTree agac = CSharpSyntaxTree.ParseText(kodMetni);
            // Şuan metin olan kod ilk kez Class → Method şeklinde ağaç yapısına dönüştü.

            // 3. Adım: Kökü alma
            // Her ağacın bir kökü vardır. kok değişkeni, kodun en dış katmanını (dosyanın kendisini) temsil eder. Tüm sınıflar ve metotlar bu kökün altındadır.
            CompilationUnitSyntax kok = agac.GetCompilationUnitRoot();

            // 4. Adım: Gezgini çalıştırma
            var gezgin = new KodGezgini();
            // Gezgini en tepeden başlattık ve her yeri dolaşacak. Eğer sadece belirli bir metodun içini merak etseydik, gezgin.Visit(metotNode) da diyebilirdik. Yani gezgin sadece verdiğimiz düğümden aşağısını tarar.
            gezgin.Visit(kok);
        }
    }

    // GEZGİN SINIFI
    // Bu sınıf, kodun içinde dolaşan bir gezgindir. CSharpSyntaxWalker sınıfından miras aldığı için C# kodunun içindeki her bir parçayı (token) tek tek ziyaret etme yeteneğine sahiptir.
    public class KodGezgini : CSharpSyntaxWalker
    {
        // Gezgin kodun içinde bir Sınıf (Class) gördüğü anda bu metot tetiklenir.
        public override void VisitClassDeclaration(ClassDeclarationSyntax node)
        {
            // WPF'te Console.WriteLine yerine MessageBox kullanarak sonucu ekranda görebiliriz.
            // Sınıfın ismini (örneğin: "Hesaplayici") verir.
            MessageBox.Show("Bulunan Sınıf: " + node.Identifier.Text);

            // Eğer bu satır yazılmazsa, gezgin sınıfın kapısından içeri girmez. İçerideki metotları da görmesi için "yoluna devam et" komutu vermen gerekir.
            base.VisitClassDeclaration(node);
        }

        // Gezgin kodun içinde bir Metot (Method) gördüğü anda bu metot tetiklenir.
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Metodun ismini (örneğin: "Topla") verir.
            // Identifier: Neden node.Name değil de node.Identifier? Çünkü Roslyn dünyasında her şeyin bir "kimliği" (Identifier) vardır. Bu sadece bir metin değil, dilin bir parçasıdır.
            MessageBox.Show("   -> Metot: " + node.Identifier.Text);
            base.VisitMethodDeclaration(node);
        }
    }
}