using System;
using System.Windows; // WPF için gerekli (Window, MessageBox). Konsol değil, pencere uygulaması olduğu için var.
using Microsoft.CodeAnalysis; // Roslyn’in çekirdeği (Semantic, Symbol, Compilation vb.).
using Microsoft.CodeAnalysis.CSharp; // “Bu kod C# kodu” demek (Parse işlemi).
using Microsoft.CodeAnalysis.CSharp.Syntax; // ClassDeclarationSyntax, MethodDeclarationSyntax, InvocationExpressionSyntax (kod parçalarının türleri için).
using System.IO; // Artık kodu string olarak yazmıyoruz, DISK’ten okuyacağız.
using System.Windows.Controls; // WPF için gerekli (Window, MessageBox). Konsol değil, pencere uygulaması olduğu için var.

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
        // Analiz işlerini artık bu servis yapacak
        private AnalysisService _analysisService = new AnalysisService();
        public MainWindow()
        {
            InitializeComponent(); // XAML’de çizdiğimiz her şey yüklenir (Butonlar, grid’ler).

            // Artık analiz otomatik başlamıyor, kullanıcıdan klasör bekliyor.
        }

        // Butona tıklandığında çalışacak olan metot
        private async void btnAnaliz_Click(object sender, RoutedEventArgs e)
        {
            // Klasör seçme penceresini açıyoruz
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "C# Projenizin Klasörünü Seçin"
            };

            if (dialog.ShowDialog() == true)
            {
                try
                {

                    txtDurum.Text = "Analiz ediliyor, lütfen bekleyin...";
                    pbAnaliz.IsIndeterminate = true; // Yükleme animasyonunu başlat (ProgressBar)
                    btnAnaliz.IsEnabled = false; // Analiz sürerken butonu kilitleyelim

                    // Seçilen klasörün tam yolu
                    string yol = dialog.FolderName;

                    // --- ASENKRON ÇALIŞTIRMA ---
                    // Task.Run ile analiz işini arka plana atıyoruz, UI donmaması için.
                    // Seçilen klasörün tam yolu Analiz metoduna gönderilir. Analiz burada başlar.
                    await Task.Run(() =>
                    {
                        _analysisService.AnaliziBaslat(yol);
                    });

                    // TreeView'ı hiyerarşik olarak doldur
                    PopulateTreeView();

                    txtDurum.Text = "Analiz başarıyla tamamlandı!";

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Analiz sırasında bir hata oluştu: " + ex.Message);
                }
                finally
                {
                    // Analiz bitince her şey eski haline dönüyor
                    pbAnaliz.IsIndeterminate = false;
                    btnAnaliz.IsEnabled = true;
                }
            }
        }

        // TreeView ile kullanıcıya ilk gerçek görselleştirme
        // Analiz sonucu:
        // Sınıflar → kök düğüm
        // Metotlar → alt düğüm şeklinde gösteren metot
        private void PopulateTreeView()
        {
            trvProje.Items.Clear();

            // 1. Önce sınıfları bul ve kök düğüm olarak ekle
            // Sınıfları ID'ye göre tekilleştiriyoruz. Bu sayede aynı sınıf tekrar gösterilmez (DistictBy)
            var classes = _analysisService.GlobalNodes
                .Where(n => n.Type == NodeType.Class)
                .GroupBy(n => n.Id)
                .Select(g => g.First())
                .ToList();

            foreach (var cls in classes)
            {
                TreeViewItem classItem = new TreeViewItem
                {
                    Header = $"[C] {cls.Name}", // UI tarafında bunun sınıf olduğunu net göstermek için [C]
                    Tag = cls.Id, // İleride tıklayınca detay görmek için Id'yi sakla
                    IsExpanded = false // Başlangıçta varsayılan olarak kapalı kalsın, daha temiz durur
                };

                // 2. Bu sınıfa ait metotları bul ve altına ekle
                // Metotları bulurken de tekilleştirme yapıyoruz. Bu sayede aynı sınıf tekrar gösterilmez
                var methods = _analysisService.GlobalNodes
                    .Where(n => n.Type == NodeType.Method && n.Id.StartsWith(cls.Id))
                    .GroupBy(n => n.Id)
                    .Select(g => g.First())
                    .ToList();

                foreach (var met in methods)
                {
                    classItem.Items.Add(
                        new TreeViewItem
                        { 
                            Header = $"[M] {met.Name}", // UI tarafında bunun metot olduğunu net göstermek için [M]
                            Tag = met.Id // // İleride tıklayınca detay görmek için Id'yi sakla
                        }
                    );
                }

                trvProje.Items.Add(classItem); // En son sınıfı TreeView’a ekleme
            }
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
            // Aynı isimli sınıfların çakışmaması için Namespace bilgisi eklendi artık : (Namespace + ClassName)
            var classSymbol = _model.GetDeclaredSymbol(node);
            _currentClassId = classSymbol?.ToDisplayString() ?? node.Identifier.Text;

            // Sınıfı bir düğüm olarak ekle
            Nodes.Add(new CodeNode
            {
                Id = _currentClassId,
                Name = node.Identifier.Text,
                Type = NodeType.Class
            });

            // 2. KALITIM (Inheritance) KONTROLÜ
            if (node.BaseList != null) // Eğer sınıfın bir base listesi varsa ( : Arac gibi)
            {
                foreach (var baseType in node.BaseList.Types)
                {
                    // Semantik modelden miras alınan sınıfın tam adını alıyoruz
                    var sembol = _model.GetSymbolInfo(baseType.Type).Symbol;
                    if (sembol != null)
                    {
                        // Hedef ID'yi tam isim olarak alıyoruz (ToDisplayString)
                        string targetId = sembol.ToDisplayString();

                        Edges.Add(new CodeEdge
                        {
                            SourceId = _currentClassId,
                            TargetId = targetId,
                            Type = EdgeType.Inheritance // İlişki tipi artık Inheritance
                        });
                    }
                }
            }

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
                // Çağrılan metodun ID’si. (Aynı isimli sınıflar ve metotlar çakışmaz)
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