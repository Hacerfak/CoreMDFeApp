using Avalonia;
using Avalonia.Controls;
using CoreMDFe.Desktop.ViewModels;

namespace CoreMDFe.Desktop.Views;

public partial class EmissaoView : UserControl
{
    public EmissaoView()
    {
        InitializeComponent();
    }

    // Heurística 5: Prevenção de Erros (Evita estado fantasma quando o usuário volta à tela)
    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);

        if (DataContext is EmissaoViewModel vm)
        {
            // Executa a limpeza silenciosa para garantir que a próxima visita seja uma tela zerada
            if (!vm.IsAutorizado)
            {
                vm.NovaEmissaoCommand.Execute(null);
            }
        }
    }
}