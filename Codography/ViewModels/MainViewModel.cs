using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Codography.Models;
using Codography.Services;

// Eskiden MainWindow içinde bulunan tüm analiz yönetimi ve UI güncelleme mantığı MainViewModel sınıfına taşındı. ObservableCollection ve INotifyPropertyChanged kullanımıyla manuel UI güncellemeleri bırakılıp otomatik "Binding" (bağlama) sistemine geçildi.
namespace Codography.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        // Analiz işlemlerini gerçekleştiren servis katmanını temsil eder. Sınıf, somut bir sınıfa değil IAnalysisService arayüzüne bağımlıdır.
        // Bu sayede: Bağımlılıklar gevşek olur (loosely coupled), Servisin iç implementasyonu değişse bile bu sınıf etkilenmez 
        // readonly: Bu servis yalnızca constructor’da atanır, sonradan yanlışlıkla değiştirilmesi engellenir.
        private readonly IAnalysisService _analysisService;

        // IGraphService : Grafik yerleşimini hesaplayan servis arayüzüdür.
        // Gerçek nesne genelde Dependency Injection ile gelir.
        // _graphService : Bu sınıf içinde layout hesaplamak için kullanacağımız servis referansıdır.
        private readonly IGraphService _graphService;

        // Eskiden ProgressBar kontrolü (pbAnaliz.IsIndeterminate) ve durum mesajları (txtDurum.Text) doğrudan MainWindow.xaml.cs dosyasında yönetiliyordu.
        // Artık bunu MainViewModel içerisine taşıdık. IsBusy özelliği ProgressBar'ı, StatusMessage ise durum metnini temsil ediyor. Bu sayede arayüz (UI) sadece veriyi göstermekle yükümlü hale geldi.
        private string _statusMessage = "Analiz için bir C# proje klasörü seçin...";
        private bool _isBusy;

        // private alan (backing field): Bu değişken, CanvasWidth değerinin hafızada tutulduğu asıl yerdir.
        // Dışarıdan (UI veya başka sınıflardan) doğrudan erişilemez. Sadece bu sınıf içinden erişilir.
        // Amaç: Değeri kontrolsüz şekilde değiştirilmesini engellemek.
        private double _canvasWidth;

        // public property: Bu özellik (property), UI'nin (XAML tarafının) CanvasWidth değerine erişmesini sağlar.
        // Yani XAML’de Binding yaptığımız yer burasıdır.
        // UI bu property üzerinden değeri okur ve değiştirir.
        public double CanvasWidth
        {
            get => _canvasWidth; // get → UI CanvasWidth değerini okumak istediğinde çalışır. Mevcut genişlik değerini döndürür. 

            // set → UI veya kod tarafından yeni bir değer atandığında çalışır.
            // Yeni gelen değer private alana kaydedilir.
            // OnPropertyChanged() "değer değişti" bilgisini UI’ya bildirir. Eğer bu olmazsa, ekranda değişiklik görünmez.
            set { _canvasWidth = value; OnPropertyChanged(); }
        }

        // private alan : Gerçek yüksekliği tutar.
        private double _canvasHeight;

        // public property : UI tarafından bind edilir.
        public double CanvasHeight
        {
            get => _canvasHeight; // get → Mevcut yüksekliği döndürür.

            // set → Yeni değer geldiğinde çalışır, değeri private alana atar
            // OnPropertyChanged() ile UI’ya değişiklik bildirimi yapıyoruz.
            // Böylece Canvas yüksekliği anında güncellenir.
            set { _canvasHeight = value; OnPropertyChanged(); } 
        }

        // ObservableCollection<T> : İçindeki liste değiştiğinde (Add, Remove, Clear vs.) arayüze otomatik bildirim yapar.
        // Böylece ekranda çizim otomatik güncellenir.

        // UI'daki TreeView bu listeye bağlanacak (Binding)
        // Artık, koleksiyona bir eleman eklendiğinde veya silindiğinde TreeView bunu otomatik olarak algılayıp kendini günceller.
        // Ayrıca INotifyPropertyChanged arayüzü sayesinde IsBusy gibi özellikler değiştiğinde UI anında haberdar olur.
        public ObservableCollection<CodeNode> RootNodes { get; } = new ObservableCollection<CodeNode>();

        // GraphNodeViewModel : Canvas üzerinde çizilecek kutuları temsil eder.
        // CanvasNodes : Ekrandaki tüm düğüm (kutu) ViewModel’lerini tutan koleksiyon.
        // Yeni nodes eklenince arayüz otomatik güncellenir.
        public ObservableCollection<GraphNodeViewModel> CanvasNodes { get; } = new ObservableCollection<GraphNodeViewModel>();

        // GraphEdgeViewModel : Canvas üzerinde çizilecek çizgileri temsil eder.
        // CanvasEdges : Ekrandaki tüm bağlantı (çizgi) ViewModel’lerini tutan koleksiyon.
        // Yeni edge eklenince arayüz otomatik güncellenir.
        public ObservableCollection<GraphEdgeViewModel> CanvasEdges { get; } = new ObservableCollection<GraphEdgeViewModel>();

        // Constructor üzerinden IAnalysisService bağımlılığı dışarıdan alınır. Bu yaklaşıma Dependency Injection denir.
        // Amaç: ViewModel’in servis oluşturmasını engellemek, Test edilebilirliği artırmak, Farklı servis implementasyonlarının kolayca takılabilmesini sağlamak
        // analysisService parametresi, daha önce tanımlanan readonly _analysisService alanına atanır. Böylece analiz işlemleri bu servis üzerinden yapılır.
        // graphService parametresi, sınıf içinde tanımlı readonly _graphService alanına atanır. Böylece grafik yerleşim hesaplamaları bu servis üzerinden yapılır.
        public MainViewModel(IAnalysisService analysisService, IGraphService graphService)
        {
            _analysisService = analysisService; // Dışarıdan gelen analiz servisini private alana kaydediyoruz.
            _graphService = graphService; // Dışarıdan gelen grafik servisini private alana kaydediyoruz.
        }

        // Analiz sırasında ProgressBar'ı ve butonu kontrol etmek için
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set { _statusMessage = value; OnPropertyChanged(); }
        }

        // Bu metot MainWindow.xaml.cs'den çağrılacak
        // Eskiden AnalysisService.AnaliziBaslat(yol) metodu çağrılıyordu sonuçlar servisin kendi içindeki genel listelerde saklanıyordu.
        // Artık StartAnalysisAsync metodu, servis katmanından dönen ProjectAnalysisResult paketini alıyor ve içindeki hiyerarşik Nodes listesini doğrudan RootNodes koleksiyonuna aktarıyor.
        public async Task StartAnalysisAsync(string folderPath)
        {
            try
            {
                // 1. İşlem başlangıcı hazırlıkları
                // Analiz sırasında buton kilitlenir ve ProgressBar görünür.
                IsBusy = true;

                // StatusMessage güncellenerek ekranda "Analiz ediliyor..." yazısı çıkar.
                StatusMessage = "Analiz ediliyor, lütfen bekleyin...";
                RootNodes.Clear();

                // 2. Arka plan işlemi
                // Analiz işi AnalysisService.cs sınıfına devredilir. O sınıftaki AnaliziBaslat metodu dosya yolu gönderilerek tetiklenir
                var result = await _analysisService.AnaliziBaslatAsync(folderPath);

                // Verilen analiz sonucuna göre grafik yerleşimini hesaplayan async metodu çağırır.
                // (result : İçinde Node ve Edge bilgileri olan analiz sonucudur)
                // Böylece Grafik yerleşimi arka planda hesaplanır, işlem bitince kaldığı yerden devam eder.
                await UpdateGraphLayoutAsync(result);

                // 3. Sonuçları UI listesine aktarma
                // (Bu kısım UI thread'inde çalışır çünkü ObservableCollection UI ile bağlıdır)
                foreach (var node in result.Nodes)
                {
                    RootNodes.Add(node);
                }

                // Analiz tamamlandığında kullanıcıya gösterilecek durum mesajını oluşturur.
                // Toplam sınıf sayısı, toplam metot sayısı ve ortalama Maintainability Index (MI) değerini ekrana yazar.
                // {result.AverageMaintainabilityIndex:F1} → MI değerini virgülden sonra 1 basamak olacak şekilde formatlar.
                StatusMessage = $"Analiz Tamamlandı! | Sınıf: {result.TotalClassCount} | " +
                    $"Metot: {result.TotalMethodCount} | " +
                    $"Genel Sağlık (MI): {result.AverageMaintainabilityIndex:F1}/100";
            }

            // Yetki (permission) kaynaklı hatalar burada özel olarak yakalanır. Kullanıcının seçtiği klasöre okuma izni yoksa bu blok çalışır.
            catch (UnauthorizedAccessException)
            {
                StatusMessage = "Hata: Seçilen klasöre erişim izniniz yok.";
            }

            catch (Exception ex)
            {
                // 4. Hata yönetimi : Yukarıdaki özel durumlar dışında kalan tüm beklenmeyen hatalar burada yakalanır.
                // ex.Message kullanılarak hatanın teknik açıklaması kullanıcıya iletilir. Böylece uygulama çökmez, hata kontrollü şekilde yönetilmiş olur.
                StatusMessage = $"Hata oluştu: {ex.Message}";
            }
            finally
            {
                // 5. İşlem ne olursa olsun (başarı veya hata) meşguliyet modundan çık
                IsBusy = false;
            }
        }

        // Daha önce JSON olarak kaydedilmiş analiz sonucunu asenkron şekilde yükler
        public async Task LoadFromSavedFileAsync(string filePath)
        {
            try
            {
                // UI'da yükleme sırasında kullanıcıyı bilgilendirmek için
                // işlem başladığında IsBusy true yapılır (örneğin loading spinner göstermek için)
                IsBusy = true;

                // Durum mesajı güncellenir
                StatusMessage = "Kayıtlı analiz yükleniyor...";

                // Dosya okuma ve JSON parse işlemi arka planda çalıştırılır. Böylece UI thread bloke edilmez
                var result = await Task.Run(() => _analysisService.LoadResultFromJsonAsync(filePath));

                // JSON içeriği başarıyla okunmuşsa
                if (result != null)
                {
                    // Mevcut analizdeki tüm kök node'lar temizlenir. Böylece eski analiz verileri kalmaz
                    RootNodes.Clear();

                    // Yüklenen analizdeki node'lar tek tek ViewModel koleksiyonuna eklenir
                    // ObservableCollection olduğu için UI otomatik olarak güncellenir
                    foreach (var node in result.Nodes)
                    {
                        RootNodes.Add(node);
                    }

                    // Verilen analiz sonucuna göre grafik yerleşimini hesaplayan async metodu çağırır.
                    // (result : İçinde Node ve Edge bilgileri olan analiz sonucudur)
                    // Böylece Grafik yerleşimi arka planda hesaplanır, işlem bitince kaldığı yerden devam eder.
                    await UpdateGraphLayoutAsync(result);

                    // Daha önce analiz edilmiş bir proje yüklendiğinde gösterilecek durum mesajını oluşturur.
                    // Proje adını, ortalama Maintainability Index (sağlık puanı) değerini ve analiz tarihini kullanıcıya bilgi amaçlı gösterir.
                    // {result.AverageMaintainabilityIndex:F1} → MI değerini virgülden sonra 1 basamak olacak şekilde formatlar.
                    StatusMessage = $"{result.ProjectName} | Sağlık Puanı: {result.AverageMaintainabilityIndex:F1}/100 | (Analiz Tarihi: {result.AnalysisDate}) başarıyla yüklendi.";
                }
            }
            catch (Exception ex)
            {

                // Dosya bulunamazsa, JSON bozuksa veya parse hatası oluşursa. Hata mesajı kullanıcıya gösterilir
                StatusMessage = $"Yükleme hatası: {ex.Message}";
            }
            finally
            {
                // Hata olsun ya da olmasın işlem sonunda IsBusy false yapılır. Böylece UI tekrar etkileşime açılır
                IsBusy = false;
            }
        }

        // Mevcut analiz sonucunu JSON dosyası olarak diske kaydeder
        public async Task SaveCurrentAnalysisAsync(string filePath)
        {
            // Şu anki analiz verileri tek bir nesne altında toplanır. Bu nesne JSON dosyasına dönüştürülerek kaydedilecektir
            var resultToSave = _analysisService.LastResult;

            // Kaydedilecek bir analiz sonucu var mı kontrol edilir
            if (resultToSave != null)
            {
                try
                {
                    // UI tarafında işlem devam ederken kullanıcıyı bilgilendirmek için
                    IsBusy = true;
                    StatusMessage = "Analiz verileri kaydediliyor...";

                    // Analiz sonucu asenkron olarak JSON dosyasına kaydedilir
                    await _analysisService.SaveResultToJsonAsync(resultToSave, filePath);

                    StatusMessage = "Tüm analiz verileri başarıyla kaydedildi.";
                }
                catch (Exception ex)
                {
                    // Dosya yazma veya serileştirme sırasında oluşabilecek hatalar yakalanır
                    StatusMessage = $"Kaydetme hatası: {ex.Message}";
                }
                finally
                {
                    // İşlem bitince UI tekrar etkileşime açılır
                    IsBusy = false;
                }
            }
            else
            {
                // Henüz analiz yapılmadıysa veya sonuç yoksa kullanıcı bilgilendirilir
                StatusMessage = "Kaydedilecek aktif bir analiz bulunamadı.";
            }
        }

        // Grafik yerleşimini hesaplayan ve sonucu UI'a aktaran async metot.
        // async : İçinde await kullanılacağı anlamına gelir.
        // Task : Geriye değer döndürmez ama asenkron çalışır.
        private async Task UpdateGraphLayoutAsync(ProjectAnalysisResult result)
        {
            // Kullanıcıya bilgi mesajı gösteriyoruz.
            StatusMessage = "Grafik yerleşimi hesaplanıyor...";

            // 1) MSAGL Hesaplaması (Arka Planda)
            // Task.Run : Hesaplamayı arka thread'de çalıştırır. Böylece UI donmaz.
            var geometryGraph = await Task.Run(() => _graphService.CalculateLayout(result));

            // 2) Arayüz Koleksiyonlarını (UI) Temizle
            // Önceki çizimleri temizliyoruz.
            CanvasNodes.Clear();
            CanvasEdges.Clear();

            // geometryGraph → MSAGL tarafından hesaplanan tüm grafiği temsil eder.
            // BoundingBox → Grafikteki tüm node’ları kapsayan en dış dikdörtgendir. Yani grafiğin "kapladığı alanın sınırlarını" verir.
            // Left → Bu dikdörtgenin en sol kenarının X koordinatıdır. Yani grafikteki en soldaki node’un X başlangıç noktasıdır.
            // Bunu saklamamızın sebebi: Daha sonra tüm node’ları 0 noktasına göre hizalayabilmek.
            double minX = geometryGraph.BoundingBox.Left;

            // Top → Bu dikdörtgenin en üst kenarının Y koordinatıdır. (MSAGL koordinat sisteminde Y ekseni yukarı doğru artar.)
            // Bu değer grafikteki en üst noktayı temsil eder.
            // Bunu saklamamızın sebebi: WPF koordinat sistemine çevirirken Y eksenini ters çevirebilmek için referans olarak kullanmak.
            double maxY = geometryGraph.BoundingBox.Top;

            // CanvasWidth → WPF tarafındaki çizim alanının genişliğini belirler.
            // geometryGraph.BoundingBox.Width → grafiğin toplam genişliği.
            // +100 → kenarlarda boşluk (margin/padding) bırakmak için ekstra alan ekliyoruz. Böylece Node'lar kenara yapışmaz.
            CanvasWidth = geometryGraph.BoundingBox.Width + 100;

            // CanvasHeight → WPF Canvas'ın yüksekliğini belirler.
            // geometryGraph.BoundingBox.Height → grafiğin toplam yüksekliği.
            // +100 → üst ve alt boşluk bırakmak için ek alan. Böylece grafik daha rahat ve estetik görünür.
            CanvasHeight = geometryGraph.BoundingBox.Height + 100;

            // 3) MAPPER: MSAGL → WPF ViewModel Dönüşümü
            // MSAGL Node'larını WPF’in anlayacağı ViewModel’e çeviriyoruz.
            foreach (var msaglNode in geometryGraph.Nodes)
            {
                // UserData içine daha önce CodeNode koymuştuk.
                if (msaglNode.UserData is CodeNode codeNode)
                {
                    // MSAGL verisini UI modeline dönüştürüyoruz.
                    CanvasNodes.Add(new GraphNodeViewModel
                    {
                        Data = codeNode, // Gerçek veri (Id, Name, Type vb.)
                        Width = msaglNode.BoundingBox.Width, // MSAGL’in hesapladığı genişlik
                        Height = msaglNode.BoundingBox.Height, // MSAGL’in hesapladığı yükseklik

                        // X koordinatını hesaplarken:
                        // msaglNode.BoundingBox.Left - minX : Node’un MSAGL koordinat sistemindeki sol X değerinden
                        // - minX ile Tüm grafikteki en küçük X değerini çıkarıyoruz.
                        // Amaç: Grafiği 0 noktasına yaslamak (normalize etmek). Böylece en soldaki node X=0 civarında
                        // + 50 : Canvas’ın sol tarafında boşluk bırakıyoruz. Böylece Node’lar kenara yapışmaz, En soldaki node ≈ 50’den başlar
                        X = msaglNode.BoundingBox.Left - minX + 50,

                        // Y koordinatını hesaplarken:
                        // maxY : Grafikteki en üst Y değeri (MSAGL sistemine göre).
                        // - msaglNode.BoundingBox.Top : Bu çıkarma işlemi Y eksenini ters çevirir. Çünkü :
                        // MSAGL’den gelen Y değeri yukarı doğru artar. WPF (Canvas) da ise Y değeri aşağı doğru artar.
                        // Bu yüzden negatife çeviriyoruz ve WPF koordinat sistemine uygun hale getiriyoruz.
                        // + 50 : Üst tarafta boşluk bırakmak için margin ekliyoruz. Böylece node’lar Canvas’ın en üstüne yapışmaz. En üstteki node ≈ 50’den başlar
                        Y = maxY - msaglNode.BoundingBox.Top + 50
                    });
                }
            }

            // MSAGL Edge'lerini WPF ViewModel'e dönüştürüyoruz.
            foreach (var msaglEdge in geometryGraph.Edges)
            {
                // UserData içine daha önce CodeEdge koymuştuk.
                if (msaglEdge.UserData is CodeEdge codeEdge)
                {
                    // Şimdilik sadece Data'yı aktarıyoruz.
                    // Routing (kıvrım noktaları) ileride doldurulacak.
                    CanvasEdges.Add(new GraphEdgeViewModel
                    {
                        Data = codeEdge
                    });
                }
            }
        }

        // Property değiştiğinde UI'ı haberdar eden mekanizma
        public event PropertyChangedEventHandler PropertyChanged;

        // [CallerMemberName] özelliği, metodu çağıran property'nin adını (örn: "IsBusy") 
        // otomatik olarak 'name' parametresine atar. Böylece string olarak hardcode yazmaktan kurtuluruz.
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}