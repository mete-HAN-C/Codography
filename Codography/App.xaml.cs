using Microsoft.Extensions.DependencyInjection; // Dependency Injection altyapısını kullanmak için
using Codography.Services;
using Codography.ViewModels;
using System.Windows;

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