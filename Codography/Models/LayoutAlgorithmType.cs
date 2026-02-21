namespace Codography.Models
{
    /// <summary>
    /// MSAGL grafik motorunun düğümleri yerleştirirken
    /// kullanabileceği farklı matematiksel yerleşim algoritmalarını tanımlar.
    /// </summary>

    // enum → Sabit seçenekler listesi oluşturur.
    // Yani LayoutAlgorithmType sadece aşağıdaki değerlerden biri olabilir.
    public enum LayoutAlgorithmType
    {
        // Sugiyama algoritması: Düğümleri katmanlı ve hiyerarşik şekilde dizer.
        // Sınıf → Metot gibi yapılar için en uygundur.
        // Genelde yazılım mimarisi grafiklerinde tercih edilir.
        Sugiyama,

        // MDS (Multidimensional Scaling): Düğümleri merkezden dışa doğru, daha serbest ve bulut benzeri bir şekilde dağıtır.
        MDS,

        // ForceDirected: Fiziksel simülasyon mantığıyla çalışır.
        // Düğümler birbirini iter, bağlantılar çeker.
        // Karmaşık ve yoğun ağ yapıları için uygundur.
        ForceDirected
    }
}