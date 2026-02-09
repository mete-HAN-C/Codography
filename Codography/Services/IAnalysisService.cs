using Codography.Models;

namespace Codography.Services
{
    // ANALİZ SERVİSİ ARAYÜZÜ (Interface). Kod analizi ile ilgili tüm işlemleri tanımlar. Gerçek implementasyon bu arayüzü uygular (Dependency Injection için)
    public interface IAnalysisService
    {
        // SON ANALİZ SONUCU: En son çalıştırılan analizden elde edilen tüm verileri tutar. Servisler bu property üzerinden son duruma erişebilir
        ProjectAnalysisResult LastResult { get; }

        // ANALİZİ BAŞLAT (ASENKRON): Seçilen klasör yolu içindeki tüm C# dosyalarını tarar. Sınıf, metot, property, field ve aralarındaki ilişkileri analiz eder
        // Uzun sürebileceği için Task yapısında asenkron çalışır
        Task<ProjectAnalysisResult> AnaliziBaslatAsync(string secilenKlasorYolu);

        // JSON'A KAYDET (ASENKRON): Verilen analiz sonucunu tek bir nesne olarak belirtilen dosya yoluna JSON formatı şeklinde kaydeder.
        // UI donmaması için asenkron olarak çalışır
        Task SaveResultToJsonAsync(ProjectAnalysisResult result, string filePath);

        // JSON'DAN YÜKLE (ASENKRON): Daha önce kaydedilmiş analiz verilerini JSON dosyasından okur. Okunan veriyi ProjectAnalysisResult nesnesine dönüştürür.
        // Yüklenen analiz sonucu geri döndürülür
        Task<ProjectAnalysisResult> LoadResultFromJsonAsync(string filePath);
    }
}