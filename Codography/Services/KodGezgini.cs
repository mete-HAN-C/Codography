using Codography.Models;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

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

            // Sınıftan çıkarken Id'yi temizlemek önemlidir. (Sıralı ziyaretler için)
            _currentClassId = null;
        }

        // Gezgin kodun içinde bir Metot (Method) gördüğü anda bu metot tetiklenir.
        public override void VisitMethodDeclaration(MethodDeclarationSyntax node)
        {
            // Eğer sınıf dışında (örneğin namespace seviyesinde - nadir de olsa) bir metot varsa hata almamak için kontrol
            if (string.IsNullOrEmpty(_currentClassId)) return;
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

            // Metottan çıkarken temizle
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