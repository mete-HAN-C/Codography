using Codography.ViewModels; // MainViewModel nesnesini oluşturup DataContext'e bağladığımız için gerekli.
using System.Windows; // WPF için gerekli (Window, MessageBox). Konsol değil, pencere uygulaması olduğu için var.
using Microsoft.Extensions.DependencyInjection; // Dependency Injection (Bağımlılık Enjeksiyonu) altyapısını kullanabilmek için gerekli GetRequiredService, AddSingleton, AddTransient gibi DI metotlarını sağlar

// Eskiden hem veri işleyen hem de UI güncelleyen bir sınıf olan MainWindow;
// Artık sadece kullanıcı etkileşimlerini (klasör seçme gibi) yakalayan ve bunları ViewModel'e ileten ince bir katmana dönüştü. İş mantığı, veri yönetimi ve UI senkronizasyonu tamamen ViewModel ve Service katmanlarına paylaştırıldı.
// PopulateTreeView Metodu Tamamen Kaldırıldı. Eskiden analizden gelen ham veriyi TreeView'a tek tek ekliyordu, Items.Clear() yapıyordu ve hiyerarşiyi manuel kuruyordu.
// Artık ViewModel içindeki ObservableCollection ve AnalysisService içindeki OrganizeHierarchy mantığı sayesinde, TreeView veriyi görür görmez kendini otomatik olarak güncelliyor.
namespace Codography
{
    public partial class MainWindow : Window  // Pencere sınıfından türer çünkü bu ana ekran bir pencere. Diğer yarısı ise XAML’de
    {
        // ViewModel referansını sınıf seviyesinde tanımlıyoruz
        private readonly MainViewModel _viewModel;
        public MainWindow()
        {
            InitializeComponent(); // XAML’de çizdiğimiz her şey yüklenir (Butonlar, grid’ler).

            // Dependency Injection (DI) container üzerinden MainViewModel örneği alınır
            // Gerekli bağımlılıkları otomatik olarak çözülmüş hazır bir ViewModel döner
            _viewModel = App.ServiceProvider.GetRequiredService<MainViewModel>();

            // MainWindow.xaml.cs dosyasına veri kaynağı olarak _viewModel nesnesini veriyoruz. Yani UI ile ViewModel'i birbirine bağlıyoruz.
            // Bu atama yapıldığı anda, XAML tarafındaki (MainWindow.xaml) tüm {Binding ...} ifadeleri canlanır.
            // ProgressBar görünmez olur (çünkü IsBusy false), buton aktifleşir ve ekranda "Analiz için bir C# proje klasörü seçin..." yazısı belirir.
            this.DataContext = _viewModel;
        }

        // Butona tıklandığında çalışacak olan metot
        // Bu metot artık sadece bir "tetikleyici"dir. Klasör seçildikten sonra tüm operasyonel işlemi ViewModel'e devrediyor.
        private async void btnAnaliz_Click(object sender, RoutedEventArgs e)
        {
            // Klasör seçme penceresini açıyoruz
            var dialog = new Microsoft.Win32.OpenFolderDialog
            {
                Title = "C# Projenizin Klasörünü Seçin"
            };

            // Kullanıcı analiz için klasör seçerse
            if (dialog.ShowDialog() == true)
            {
                // Seçilen dosya yolu alınıp tüm ağır iş ViewModel'e devredilir.
                await _viewModel.StartAnalysisAsync(dialog.FolderName);
            }
        }
        // Kaydet butonuna basıldığında mevcut analiz sonucunu JSON dosyası olarak kaydetmeyi sağlayan asenkron event metot.
        private async void btnKaydet_Click(object sender, RoutedEventArgs e)
        {
            // Kullanıcıya dosya kaydetme penceresi açılır.
            var dialog = new Microsoft.Win32.SaveFileDialog
            {
                // Sadece .json uzantılı dosyaların seçilmesine izin verilir.
                Filter = "JSON Dosyası (*.json)|*.json",

                // Varsayılan dosya adı belirlenir
                FileName = "analiz_sonucu.json"
            };

            // Kullanıcı Kaydet butonuna basarsa (iptal edilmezse).
            if (dialog.ShowDialog() == true)
            {
                // ViewModel içerisindeki kaydetme metodu çağrılır.
                // Analiz sonucu, kullanıcının seçtiği dosya yoluna JSON formatında asenkron olarak kaydedilir
                await _viewModel.SaveCurrentAnalysisAsync(dialog.FileName);
            }
        }
        // Daha önce kaydedilmiş olan analiz sonucunu JSON dosyasından yükleyen metot.
        private async void btnYukle_Click(object sender, RoutedEventArgs e)
        {
            // Kullanıcıya dosya seçme penceresi açılır.
            var dialog = new Microsoft.Win32.OpenFileDialog
            {
                // Sadece .json uzantılı dosyaların seçilmesine izin verilir.
                Filter = "JSON Dosyası (*.json)|*.json"
            };

            // Kullanıcı bir dosya seçip Aç butonuna basarsa.
            if (dialog.ShowDialog() == true)
            {
                // ViewModel içerisindeki asenkron yükleme metodu çağrılır.
                // Seçilen JSON dosyası okunur ve analiz verileri uygulamaya geri yüklenir.
                await _viewModel.LoadFromSavedFileAsync(dialog.FileName);
            }
        }
    }
}