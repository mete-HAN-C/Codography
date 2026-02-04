// Enumlar UI dosyasından (MainWindow.xaml.cs) çıkarılarak Codography.Models isim alanı altında bağımsız bir dosyaya taşındı.
namespace Codography.Models
{
    /// <summary>
    /// Kod öğesinin türünü belirler.
    /// </summary>
    public enum NodeType
    {
        Class,
        Method
    }

    /// <summary>
    /// İki öğe arasındaki ilişkinin türünü belirler.
    /// </summary>
    public enum EdgeType
    {
        // Metot çağrısı (Method A -> Method B)
        Call,

        // Kalıtım ilişkisi (Class A -> Class B)
        Inheritance
    }
}