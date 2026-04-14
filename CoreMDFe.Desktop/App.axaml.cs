using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CoreMDFe.Core.Interfaces;
using CoreMDFe.Infrastructure.Data;
using CoreMDFe.Application.Services;
using CoreMDFe.Application.Mediator;
using CoreMDFe.Application.Features.Configuracoes;
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
            services.AddTransient<IAppDbContext>(provider =>
            {
                var tenant = provider.GetRequiredService<CurrentTenantService>();
                var path = tenant.CurrentDbPath ?? "design_time.db";
                return new AppDbContext(path);
            });

            // 3. Configura o Logging e o MediatR
            services.AddLogging();
            services.AddNativeMediator(typeof(SalvarConfiguracaoCommand).Assembly);

            // 4. Registrar ViewModels (O MainViewModel TEM que ser Singleton para a navegação funcionar)
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SeletorEmpresaViewModel>();

            services.AddTransient<DashboardViewModel>();
            services.AddTransient<EmissaoViewModel>();
            services.AddTransient<ConfiguracoesViewModel>();
            services.AddTransient<OnboardingViewModel>();
            services.AddTransient<ResumoViewModel>();
            services.AddTransient<VeiculosViewModel>();
            services.AddTransient<CondutoresViewModel>();
            services.AddTransient<HistoricoViewModel>();

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