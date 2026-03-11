using Avalonia;
using Serilog;
using System;
using System.Threading;
using Velopack;

namespace CoreMDFe.Desktop
{
    class Program
    {
        private static Mutex? _mutex;

        [STAThread]
        public static void Main(string[] args)
        {
            // 1. INICIALIZAÇÃO DO VELOPACK (Deve ser a PRIMEIRA coisa a rodar)
            // Ele verifica se há atualizações pendentes para aplicar antes do app abrir.
            VelopackApp.Build().Run();

            // 2. CONFIGURAÇÃO GLOBAL DO SERILOG
            Log.Logger = new LoggerConfiguration()
#if DEBUG
                .MinimumLevel.Debug() // Se estiver no Visual Studio, regista tudo
#else
                .MinimumLevel.Information() // Em produção, regista apenas Info, Avisos e Erros
#endif
                .WriteTo.Console() // Continua a mostrar na aba Output do Visual Studio
                .WriteTo.File(
                    path: "Logs/log-.txt", // Cria uma pasta "Logs" ao lado do executável
                    rollingInterval: RollingInterval.Day, // Cria um ficheiro novo por dia
                    retainedFileCountLimit: 7, // Mantém apenas os últimos 7 dias de logs (apaga os mais velhos)
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
                .CreateLogger();

            try
            {
                Log.Information("Iniciando a aplicação CoreMDFe...");

                // 3. CONTROLE DE INSTÂNCIA ÚNICA (Mutex)
                const string appName = "CoreMDFeApp_SingleInstance_GlobalMutex";
                bool isNovaInstancia;

                _mutex = new Mutex(true, appName, out isNovaInstancia);

                if (!isNovaInstancia)
                {
                    Log.Warning("Tentativa de abrir uma nova instância. O sistema já está em execução e a ação foi abortada.");
                    return;
                }

                try
                {
                    // 4. INICIA A INTERFACE GRÁFICA
                    BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
                }
                finally
                {
                    if (_mutex != null)
                    {
                        _mutex.ReleaseMutex();
                        _mutex.Dispose();
                    }
                }
            }
            catch (Exception ex)
            {
                // SE ALGO CATASTRÓFICO ACONTECER (ex: falta de permissão no Windows), FICA REGISTADO!
                Log.Fatal(ex, "A aplicação sofreu um erro fatal e encerrou inesperadamente.");
            }
            finally
            {
                // Garante que a última linha de log é escrita no ficheiro antes do programa fechar
                Log.Information("Aplicação encerrada.");
                Log.CloseAndFlush();
            }
        }

        public static AppBuilder BuildAvaloniaApp()
            => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithInterFont()
                .LogToTrace();
    }
}