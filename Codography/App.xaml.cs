using Microsoft.Extensions.DependencyInjection; // Dependency Injection altyapısını kullanmak için
using Codography.Services;
using Codography.ViewModels;
using System.Windows;
using Codography.Tests; // Test sınıfımız için

namespace Codography
{
    public partial class App : Application
    {
        // Uygulama genelinde servisleri almak için kullanılacak DI container
        public static IServiceProvider ServiceProvider { get; private set; }

        // Uygulama başlatıldığında ilk çalışan constructor. Dependency Injection yapılandırması burada hazırlanır
        public App()
        {
            // Servisleri tutacak koleksiyon oluşturulur
            var serviceCollection = new ServiceCollection();

            // Servis ve ViewModel kayıtları yapılır
            ConfigureServices(serviceCollection);

            // Servis koleksiyonu gerçek ServiceProvider’a dönüştürülür
            ServiceProvider = serviceCollection.BuildServiceProvider();
        }
        // Uygulama ilk başlatıldığında otomatik olarak çalışan metot.
        protected override void OnStartup(StartupEventArgs e)
        {
            // Base (Application) sınıfının OnStartup metodunu çağırıyoruz. Bu satır olmazsa WPF’in normal başlatma süreci düzgün çalışmaz.
            base.OnStartup(e);

            // MSAGL Algoritma Testi 
            // Bu test : - Sahte düğümler oluşturur, - Layout hesaplatır, - Sonuçları Debug penceresine yazdırır
            // Uygulama açılır açılmaz GraphLayoutTester içindeki RunTest() metodunu çalıştırırsak test başlar.
            // İhtiyaç duyulduğunda aşağıdaki yorum satırı kaldırarak test başlatılabilir.

            // GraphLayoutTester.RunTest();
        }

        // Uygulama boyunca kullanılacak servislerin ve ViewModel'lerin Dependency Injection sistemine kayıt edildiği metottur
        private void ConfigureServices(IServiceCollection services)
        {
            // Servisleri kayıt et. IAnalysisService istendiğinde AnalysisService verilir
            // Singleton: Uygulama boyunca tek bir instance kullanılır
            services.AddSingleton<IAnalysisService, AnalysisService>();

            // ViewModel'leri kayıt et
            // Transient: Her istendiğinde yeni bir instance oluşturulur
            services.AddTransient<MainViewModel>();

            // MainWindow'u kayıt et
            // Pencere DI üzerinden oluşturulabilir hale gelir
            services.AddTransient<MainWindow>();

            // IGraphService istendiğinde GraphService kullanılacak demektir.
            // AddSingleton → Uygulama boyunca sadece 1 tane oluşturulur ve her yerde aynı nesne kullanılır.
            services.AddSingleton<IGraphService, GraphService>();
        }
    }
}