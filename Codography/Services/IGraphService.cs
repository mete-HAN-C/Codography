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

        // ProjectAnalysisResult : Analiz sonrası oluşan proje verilerini tutar.
        // GeometryGraph : MSAGL tarafından hesaplanmış konum bilgilerini içeren grafik yapısıdır.
        GeometryGraph CalculateLayout(ProjectAnalysisResult analysisResult);

    }
}