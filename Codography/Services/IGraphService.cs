using Codography.Models;
using Microsoft.Msagl.Core.Layout; // MSAGL'nin matematiksel yerleşim (layout) motoru. GeometryGraph sınıfı burada bulunur.

namespace Codography.Services
{
    // IGraphService interface'i bir sınıfın hangi metotları içermesi gerektiğini tanımlar, ancak metotların içini (gövdesini) yazmaz.
    public interface IGraphService
    {
        /// <summary>
        /// Bu metot, analiz sonuçlarını parametre olarak alır. Gelen analizResult içindeki Node bilgilerine göre
        /// her bir düğümün (node) X ve Y koordinatlarını hesaplar.
        /// 
        /// Sonuç olarak, yerleşimi (layout'u) hesaplanmış bir GeometryGraph nesnesi döndürür.
        /// </summary>

        // GeometryGraph : MSAGL’nin hesapladığı, düğümlerin (Node) ve kenarların (Edge) X,Y koordinatlarını içeren grafik nesnesini döndürür.
        // CalculateLayout : Yerleşim (layout) hesaplayan metodun adı.

        // ProjectAnalysisResult analysisResult : Analiz sonucu oluşan tüm düğüm ve bağlantı verilerini parametre olarak alır. 
        // LayoutAlgorithmType algorithmType : Hangi yerleşim algoritmasının kullanılacağını belirler.
        // = LayoutAlgorithmType.Sugiyama : Eğer dışarıdan bir algoritma belirtilmezse, varsayılan olarak Sugiyama algoritması kullanılır.
        GeometryGraph CalculateLayout(ProjectAnalysisResult analysisResult, LayoutAlgorithmType algorithmType = LayoutAlgorithmType.Sugiyama);

    }
}