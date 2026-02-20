using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CoreMDFe.Core.Interfaces;
using CoreMDFe.Infrastructure.Data;
using Microsoft.Extensions.DependencyInjection;
using System;
using CoreMDFe.Desktop.ViewModels;
using CoreMDFe.Desktop.Views;

namespace CoreMDFe.Desktop
{
    public partial class App : Avalonia.Application
    {
        public static IServiceProvider? Services { get; private set; }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            var services = new ServiceCollection();

            // 1. Configurar Banco de Dados
            services.AddDbContext<AppDbContext>();
            services.AddScoped<IAppDbContext>(provider => provider.GetRequiredService<AppDbContext>());

            // 2. Configurar MediatR
            // Usamos o "global::" para o C# não confundir o seu projeto "Application" com a classe "Avalonia.Application"
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(global::CoreMDFe.Application.Features.Configuracoes.SalvarConfiguracaoCommand).Assembly));

            // 3. Registrar ViewModels
            services.AddTransient<MainViewModel>();
            services.AddTransient<ConfiguracoesViewModel>();

            Services = services.BuildServiceProvider();


            // Garante que o DB foi criado (ou aplica as migrations)
            using (var scope = Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                db.Database.EnsureCreated();
            }

            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow
                {
                    DataContext = Services.GetRequiredService<MainViewModel>()
                };
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}