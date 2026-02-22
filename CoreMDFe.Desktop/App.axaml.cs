using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CoreMDFe.Core.Interfaces;
using CoreMDFe.Infrastructure.Data;
using CoreMDFe.Application.Services;
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

            // 1. Registra o serviço de Sessão (Tenant) como Singleton (Único para todo o app)
            services.AddSingleton<CurrentTenantService>();

            // 2. Configura Banco de Dados DINÂMICO
            services.AddScoped<IAppDbContext>(provider =>
            {
                var tenant = provider.GetRequiredService<CurrentTenantService>();
                // Se não tem empresa selecionada, usa um banco em memória ou de design (só para evitar erro de injeção)
                var path = tenant.CurrentDbPath ?? "design_time.db";
                return new AppDbContext(path);
            });

            // 3. Configura MediatR
            services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(global::CoreMDFe.Application.Features.Configuracoes.SalvarConfiguracaoCommand).Assembly));

            // 4. Registrar ViewModels (O MainViewModel TEM que ser Singleton para a navegação funcionar)
            services.AddSingleton<MainViewModel>();
            services.AddTransient<OnboardingViewModel>();
            services.AddTransient<SeletorEmpresaViewModel>();
            services.AddTransient<DashboardViewModel>();

            Services = services.BuildServiceProvider();

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