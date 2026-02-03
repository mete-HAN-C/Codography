using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

// Analiz tamamen ayrı bir servis sınıfına taşındı.
// MainWindow → sadece kullanıcıyla ilgilenir
// AnalysisService → sadece kodu analiz eder
namespace Codography
{
    public class AnalysisService
    {
        // Analizin ürettiği veri, analizi yapan sınıfa ait.
        public List<CodeNode> GlobalNodes = new List<CodeNode>();
        public List<CodeEdge> GlobalEdges = new List<CodeEdge>();

        public void AnaliziBaslat(string secilenKlasorYolu)
        {
            GlobalNodes.Clear();
            GlobalEdges.Clear();

            if (!Directory.Exists(secilenKlasorYolu)) return;

            // 1. ADIM: .cs dosyalarını bul ve bin/obj klasörlerini ele
            var dosyaYollari = Directory.GetFiles(secilenKlasorYolu, "*.cs", SearchOption.AllDirectories)
                .Where(dosya => !dosya.Contains("\\bin\\") && !dosya.Contains("\\obj\\"))
                .ToArray();

            List<SyntaxTree> tumAgaclar = new List<SyntaxTree>();

            // 2. ADIM: Dosyaları oku ve ağaçları oluştur
            foreach (var dosya in dosyaYollari)
            {
                string kodIcerigi = File.ReadAllText(dosya);
                tumAgaclar.Add(CSharpSyntaxTree.ParseText(kodIcerigi));
            }

            // 3. ADIM: Roslyn Derleme (Beyin)
            var referanslar = new[] { MetadataReference.CreateFromFile(typeof(object).Assembly.Location) };
            var derleme = CSharpCompilation.Create("CokluDosyaAnalizi")
                .AddReferences(referanslar)
                .AddSyntaxTrees(tumAgaclar);

            // 4. ADIM: Gezgin ile verileri topla
            foreach (var agac in tumAgaclar)
            {
                SemanticModel model = derleme.GetSemanticModel(agac);
                var gezgin = new KodGezgini(model);
                gezgin.Visit(agac.GetRoot());

                GlobalNodes.AddRange(gezgin.Nodes);
                GlobalEdges.AddRange(gezgin.Edges);
            }

            // NOT: MessageBox'ı buradan kaldırmak ve MainWindow'a taşımak mimari açıdan daha doğrudur.
        }
    }
}