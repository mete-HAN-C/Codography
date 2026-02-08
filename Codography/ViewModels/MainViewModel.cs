using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Codography.Models;
using Codography.Services;

// Eskiden MainWindow içinde bulunan tüm analiz yönetimi ve UI güncelleme mantığı MainViewModel sınıfına taşındı. ObservableCollection ve INotifyPropertyChanged kullanımıyla manuel UI güncellemeleri bırakılıp otomatik "Binding" (bağlama) sistemine geçildi.
namespace Codography.ViewModels
{
    public class MainViewModel : INotifyPropertyChanged
    {
        private readonly AnalysisService _analysisService;

        // Eskiden ProgressBar kontrolü (pbAnaliz.IsIndeterminate) ve durum mesajları (txtDurum.Text) doğrudan MainWindow.xaml.cs dosyasında yönetiliyordu.
        // Artık bunu MainViewModel içerisine taşıdık. IsBusy özelliği ProgressBar'ı, StatusMessage ise durum metnini temsil ediyor. Bu sayede arayüz (UI) sadece veriyi göstermekle yükümlü hale geldi.
        private string _statusMessage = "Analiz için bir C# proje klasörü seçin...";
        private bool _isBusy;

        // UI'daki TreeView bu listeye bağlanacak (Binding)
        // Artık, koleksiyona bir eleman eklendiğinde veya silindiğinde TreeView bunu otomatik olarak algılayıp kendini günceller. Ayrıca INotifyPropertyChanged arayüzü sayesinde IsBusy gibi özellikler değiştiğinde UI anında haberdar olur.
        public ObservableCollection<CodeNode> RootNodes { get; } = new ObservableCollection<CodeNode>();

        public MainViewModel()
        {
            _analysisService = new AnalysisService();
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
                var result = await Task.Run(() => _analysisService.AnaliziBaslat(folderPath));

                // 3. Sonuçları UI listesine aktarma
                // (Bu kısım UI thread'inde çalışır çünkü ObservableCollection UI ile bağlıdır)
                foreach (var node in result.Nodes)
                {
                    RootNodes.Add(node);
                }

                StatusMessage = $"Analiz başarıyla tamamlandı! {result.Nodes.Count} sınıf bulundu.";
            }
            catch (System.Exception ex)
            {
                // 4. Hata yönetimi
                StatusMessage = $"Hata oluştu: {ex.Message}";
                // Burada loglama yapabilirsin (Örn: NLog, Serilog)
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
                var result = await Task.Run(() => _analysisService.LoadResultFromJson(filePath));

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

                    // Kullanıcıya başarılı yükleme bilgisi gösterilir
                    StatusMessage = $"{result.ProjectName} (Analiz Tarihi: {result.AnalysisDate}) başarıyla yüklendi.";
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
        public void SaveCurrentAnalysis(string filePath)
        {
            // Şu anki analiz verileri tek bir nesne altında toplanır. Bu nesne JSON dosyasına dönüştürülerek kaydedilecektir
            var resultToSave = _analysisService.LastResult;

            // Kaydedilecek bir analiz sonucu var mı kontrol edilir
            if (resultToSave != null)
            {
                // Eğer kullanıcı UI tarafında analizle ilgili bir değişiklik yaptıysa (proje adı, etiketler vb.) bu değişiklikler kaydetmeden önce analiz nesnesine yansıtılır
                resultToSave.ProjectName = "Tam Kapasite Analiz";

                // Analiz sonucu JSON formatına çevrilerek belirtilen dosya yoluna kaydedilir. Bu işlem bağlantılar, ters indeksler ve tüm hiyerarşi bilgilerini içerir
                _analysisService.SaveResultToJson(resultToSave, filePath);
                StatusMessage = "Tüm analiz verileri (Bağlantılar ve İndeks dahil) başarıyla kaydedildi.";
            }
            else
            {
                // Henüz analiz yapılmamışsa veya analiz sonucu bellekte yoksa kullanıcıya kaydedilecek bir veri olmadığı bilgisi verilir
                StatusMessage = "Kaydedilecek aktif bir analiz bulunamadı.";
            }
        }

        // Property değiştiğinde UI'ı haberdar eden mekanizma
        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}