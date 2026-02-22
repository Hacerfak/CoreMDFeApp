using CommunityToolkit.Mvvm.ComponentModel;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class ResumoViewModel : ObservableObject
    {
        // Aqui buscaremos os contadores de emissões depois
        [ObservableProperty] private int _emitidosHoje = 0;
        [ObservableProperty] private int _emTransito = 0;
        [ObservableProperty] private int _rejeitados = 0;
    }
}