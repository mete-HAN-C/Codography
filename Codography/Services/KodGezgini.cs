using Codography.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

// Eskiden KodGezgini, MainWindow.xaml.cs dosyasının içinde bir yardımcı sınıftı. Şimdi ise Services klasörü altında bağımsız bir dosya olarak yer alıyor.
namespace Codography.Services
{
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
        private string _currentClassId; // O an içinde bulunulan sınıfın kimliği
        private string _currentMethodId; // O an içinde bulunulan metodun kimliği

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
            if (classSymbol == null) return;
            _currentClassId = classSymbol.ToDisplayString();

            // Sınıfı bir düğüm olarak ekle
            Nodes.Add(new CodeNode
            {
                Id = _currentClassId,
                Name = node.Identifier.Text,
                Type = NodeType.Class
            });

            // KALITIM VE ARAYÜZ (Inheritance & Interface)
            // Sınıfın türediği ana sınıfı (Base Class) bulur (System.Object değilse)
            if (classSymbol.BaseType != null && classSymbol.BaseType.SpecialType != SpecialType.System_Object)
            {
                // Eğer gerçek bir üst sınıf varsa, bu ilişki EdgeType.Inheritance(Kalıtım) türünde bir bağ olarak kaydedilir.
                Edges.Add(new CodeEdge
                {
                    SourceId = _currentClassId,
                    TargetId = classSymbol.BaseType.ToDisplayString(),
                    Type = EdgeType.Inheritance
                });
            }

            // Sınıfın kaç tane arayüz uyguladığını bulur ve her biri için yine bir kalıtım çizgisi oluşturur.
            foreach (var @interface in classSymbol.Interfaces)
            {
                Edges.Add(new CodeEdge
                {
                    SourceId = _currentClassId,
                    TargetId = @interface.ToDisplayString(),
                    Type = EdgeType.Inheritance
                });
            }

            // Eğer bu satır yazılmazsa, gezgin sınıfın kapısından içeri girmez. İçerideki metotları da görmesi için "yoluna devam et" komutu vermen gerekir.
            base.VisitClassDeclaration(node);

            // Sınıftan çıkarken Id'yi temizlemek önemlidir. (Sıralı ziyaretler için)
            _currentClassId = null;
        }

        // Bir sınıfın içindeki alanları (field) yakalamak için override edilen metottur.
        // Sınıfın içindeki private Motor _motor; gibi alanları bulur.
        public override void VisitFieldDeclaration(FieldDeclarationSyntax node)
        {
            // Şu an hangi sınıfın içinde olduğumuzu bilmiyorsak analiz yapılmaz.
            // Çünkü ilişki kurabilmek için bir "kaynak sınıf" bilinmelidir.
            if (string.IsNullOrEmpty(_currentClassId)) return;

            // Bir field declaration birden fazla değişken içerebilir
            // Motor m1, m2; Bu durumda m1 ve m2 ayrı ayrı dolaşılır
            foreach (var variable in node.Declaration.Variables)
            {
                // Syntax’tan Semantic’e geçiş yapılır. IFieldSymbol sayesinde Tip bilgisi, namespace, erişim belirleyicisi gibi derin bilgiler alınır.
                var fieldSymbol = _model.GetDeclaredSymbol(variable) as IFieldSymbol;

                // Field’ın tipi alınır (örneğin Motor, List<Motor>, Motor[]) ve bu tip başka bir sınıfla ilişki kuruyor mu diye analiz eden metoda gönderilir.
                AddCompositionEdge(fieldSymbol?.Type);
            }

            // Visitor zincirinin bozulmaması için base metot çağrılır. Böylece alt node’ların da gezilmesi sağlanır.
            base.VisitFieldDeclaration(node);
        }

        // Bir sınıfın içindeki Property’leri yakalamak için override edilen metottur.
        // Sınıfın içindeki public Motor Motor { get; set; } gibi property’leri bulur.
        public override void VisitPropertyDeclaration(PropertyDeclarationSyntax node)
        {
            // Yine: Eğer aktif bir sınıf bilinmiyorsa analiz yapılmaz
            if (string.IsNullOrEmpty(_currentClassId)) return;

            // Property’nin semantik karşılığı alınır. IPropertySymbol sayesinde Tip bilgisi, namespace, Getter / Setter gibi derin bilgiler alınır.
            var propertySymbol = _model.GetDeclaredSymbol(node) as IPropertySymbol;

            // Property'nin tipi alınır ve bu tip başka bir kullanıcı tanımlı sınıf mı diye kontrol edilmesi için gönderilir.
            AddCompositionEdge(propertySymbol?.Type);

            // Visitor zinciri devam etsin diye base çağrılır
            base.VisitPropertyDeclaration(node);
        }

        // Bu metot verilen tip, başka bir sınıfla ilişki (composition / association) kuruyor mu diye bakar.
        private void AddCompositionEdge(ITypeSymbol type)
        {
            // Tip yoksa analiz yapılmaz
            if (type == null) return;

            // 1. Dizi Desteği: Motor[] gibi tiplerde, asıl önemli olan içindeki Motor tipidir.
            if (type is IArrayTypeSymbol arrayType)
            {
                // Dizinin eleman tipi alınır (Motor[]) ve tekrar aynı metoda gönderilerek analiz edilir (recursive yapı)
                AddCompositionEdge(arrayType.ElementType);
                return; // Dizi zaten çözüldüğü için devam edilmez
            }

            // 2. Generic Tip Desteği : List<Motor>, ICollection<Tekerlek>
            if (type is INamedTypeSymbol namedType && namedType.IsGenericType)
            {
                // Generic tipin içindeki tüm tip parametreleri alınır.
                foreach (var arg in namedType.TypeArguments)
                {
                    // İçerideki her tip (Motor gibi) ayrı ayrı analiz edilir
                    AddCompositionEdge(arg);
                }
            }

            // Tipin bulunduğu namespace alınır. System, Microsoft gibi namespace'ler filtrelenir.
            var ns = type.ContainingNamespace?.ToDisplayString() ?? "";

            // Sadece: Class, Interface olan tipler dikkate alınır. Ayrıca System ve Microsoft namespace'leri hariç tutulur
            if ((type.TypeKind == TypeKind.Class || type.TypeKind == TypeKind.Interface) &&
                !ns.StartsWith("System") && !ns.StartsWith("Microsoft"))
            {
                // Aynı sınıflar arasında daha önce ilişki eklenmiş mi kontrol edilir. Böylece aynı edge'in tekrar tekrar eklenmesi önlenir
                if (!Edges.Any(e =>
                    e.SourceId == _currentClassId && 
                    e.TargetId == type.ToDisplayString()))
                {
                    // Kaynak sınıf ile hedef sınıf arasına bir ilişki (edge) eklenir.
                    Edges.Add(new CodeEdge
                    {
                        SourceId = _currentClassId, // İlişkiyi başlatan sınıf (örneğin Araba)
                        TargetId = type.ToDisplayString(), // İlişki kurulan sınıf (örneğin Motor)
                        Type = EdgeType.Call // Şu an Call olarak işaretledik. UML açısından ileride Association veya Composition yapılabilir
                    });
                }
            }
        }
        // Gezgin kodun içinde bir Metot (Method) gördüğü anda bu metot tetiklenir.
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Eğer sınıf dışında (örneğin namespace seviyesinde - nadir de olsa) bir metot varsa hata almamak için kontrol
            if (string.IsNullOrEmpty(_currentClassId)) return;

            // O anki metodun tam adını sakla (Örn: Proje.Helpers.Hesapla)
            // Sembol üzerinden tam ad alınarak daha güvenli hale getirildi.
            var methodSymbol = _model.GetDeclaredSymbol(node);
            _currentMethodId = methodSymbol?.ToDisplayString() ?? $"{_currentClassId}.{node.Identifier.Text}";

            // Metodu bir düğüm (node) olarak temsil etmek için yeni bir CodeNode oluşturulur
            var newNode = new CodeNode
            {
                // Metoda ait benzersiz kimlik bilgisi. Genellikle sınıf adı + metot adı gibi bir yapıdan oluşur
                Id = _currentMethodId,

                // Metodun kaynak koddaki adı alınır
                Name = node.Identifier.Text,

                // Bu düğümün bir metodu temsil ettiği belirtilir
                Type = NodeType.Method,

                // SEMANTİK ANALİZ: Metodun dönüş tipi alınır. (Örn: void, int, string, Task<bool>).
                // Eğer herhangi bir sebeple dönüş tipi alınamazsa varsayılan olarak "void" atanır
                ReturnType = methodSymbol?.ReturnType?.ToDisplayString() ?? "void"
            };

            // SEMANTİK ANALİZ: Metodun aldığı parametreler alınır
            if (methodSymbol != null)
            {
                // Metodun tüm parametreleri tek tek dolaşılır
                foreach (var parameter in methodSymbol.Parameters)
                {
                    // Parametrenin tipi ve adı birleştirilerek string olarak saklanır. (Örn: "string folderPath", "int retryCount", "Motor motor")
                    newNode.Parameters.Add($"{parameter.Type.ToDisplayString()} {parameter.Name}");
                }
            }

            // Oluşturulan metot düğümü, tüm düğümlerin tutulduğu koleksiyona eklenir. Böylece bu metot analiz ağacında (graph) yerini alır
            Nodes.Add(newNode);

            // Metodun gövdesine gir ve içindeki çağrıları tara
            base.VisitMethodDeclaration(node);

            // Metottan çıkarken "şu an bu metottayım" bilgisini temizle.
            _currentMethodId = null;
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
                // Sembol üzerinden çağrılan metodun tam kimliği (Hedef) alınıyor
                string targetId = sembol.ToDisplayString();

                // Kaynak ve Hedef ID karşılaştırılarak özyineleme kontrolü yapılıyor
                bool isRecursive = _currentMethodId == targetId;

                // ÇİZGİYİ (EDGE) EKLE: Kaynak metodumdan hedef metoda bir çağrı var
                // “Bu metot, şu metodu çağırıyor” bilgisi.
                Edges.Add(new CodeEdge
                {
                    SourceId = _currentMethodId, // Kim çağırdı?
                    TargetId = targetId, // // Kimi çağırdı?
                    Type = EdgeType.Call
                });
            }
            base.VisitInvocationExpression(node);
        }
    }
}