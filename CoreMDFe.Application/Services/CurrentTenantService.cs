namespace CoreMDFe.Application.Services
{
    // Singleton que guarda qual o banco de dados da empresa logada atualmente
    public class CurrentTenantService
    {
        public string? CurrentDbPath { get; private set; }

        public void SetTenant(string dbPath)
        {
            CurrentDbPath = dbPath;
        }

        public void ClearTenant()
        {
            CurrentDbPath = null;
        }
    }
}