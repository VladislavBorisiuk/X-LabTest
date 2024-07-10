using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Windows;
using XLabApp.Services;
using XLabApp.Services.Interfaces;
using XLabApp.ViewModels;

namespace XLabApp
{
    public partial class App : Application
    {
        public static bool IsDesignMode { get; private set; } = true;
        public static Window ActivedWindow => Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsActive);
        public static Window FocusedWindow => Current.Windows.Cast<Window>().FirstOrDefault(w => w.IsFocused);

        private static IHost __Host;

        public static IHost Host => __Host ??= new HostBuilder()
            .ConfigureAppConfiguration(cfg => cfg.AddJsonFile("appsettings.json", true, true))
            .ConfigureServices((hostContext, services) =>
            {
                ConfigureServices(hostContext, services);
            })
            .Build();

        public static IServiceProvider Services => Host.Services;

        protected override async void OnStartup(StartupEventArgs e)
        {
            IsDesignMode = false;
            var host = Host;
            base.OnStartup(e);
            await host.StartAsync();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            base.OnExit(e);
            using var host = Host;
            await host.StopAsync();
        }

        public static void ConfigureServices(HostBuilderContext context, IServiceCollection services)
        {
            
            services.AddHttpClient("AuthServer", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7168/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });

            services.AddHttpClient("ResourceServer", client =>
            {
                client.BaseAddress = new Uri("https://localhost:7169/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
            })
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            });


            services.AddTransient<IDataService, DataService>();
            services.AddTransient<MainWindowViewModel>();
            services.AddSingleton<ViewModelLocator>();
        }

#pragma warning disable CS8603 // Возможно, возврат ссылки, допускающей значение NULL.
        public static string CurrentDirectory => IsDesignMode ? Path.GetDirectoryName(GetSourceCodePath())
            : Environment.CurrentDirectory;
#pragma warning restore CS8603 // Возможно, возврат ссылки, допускающей значение NULL.

        private static string GetSourceCodePath([CallerFilePath] string Path = null) => Path;
    }
}
