using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CoreMDFe.Application.Features.Onboarding;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace CoreMDFe.Desktop.ViewModels
{
    public partial class SeletorEmpresaViewModel : ObservableObject
    {
        private readonly IMediator _mediator;

        public ObservableCollection<EmpresaResumoDto> Empresas { get; } = new();

        [ObservableProperty] private bool _isCarregando;

        public SeletorEmpresaViewModel(IMediator mediator)
        {
            _mediator = mediator;
            _ = CarregarEmpresas();
        }

        private async Task CarregarEmpresas()
        {
            IsCarregando = true;
            Empresas.Clear();
            var result = await _mediator.Send(new ListarEmpresasQuery());
            foreach (var emp in result) Empresas.Add(emp);
            IsCarregando = false;
        }

        [RelayCommand]
        private void NovaEmpresa()
        {
            App.Services!.GetRequiredService<MainViewModel>().NavegarParaOnboarding();
        }

        [RelayCommand]
        private void EntrarEmpresa(EmpresaResumoDto empresa)
        {
            App.Services!.GetRequiredService<MainViewModel>().NavegarParaDashboard(empresa.DbPath);
        }
    }
}