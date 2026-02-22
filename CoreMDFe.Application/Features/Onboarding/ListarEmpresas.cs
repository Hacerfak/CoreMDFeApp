using MediatR;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CoreMDFe.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CoreMDFe.Application.Features.Onboarding
{
    public record ListarEmpresasQuery() : IRequest<List<EmpresaResumoDto>>;
    public record EmpresaResumoDto(string Cnpj, string RazaoSocial, string DbPath);

    public class ListarEmpresasHandler : IRequestHandler<ListarEmpresasQuery, List<EmpresaResumoDto>>
    {
        public async Task<List<EmpresaResumoDto>> Handle(ListarEmpresasQuery request, CancellationToken cancellationToken)
        {
            var lista = new List<EmpresaResumoDto>();
            var basePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ZeusMDFe");

            if (!Directory.Exists(basePath)) return lista;

            // Varre as pastas de CNPJ criadas pelo Onboarding
            foreach (var dir in Directory.GetDirectories(basePath))
            {
                var dbPath = Path.Combine(dir, "empresa_data.db");
                if (File.Exists(dbPath))
                {
                    try
                    {
                        // Dá uma "espiada" rápida no banco da empresa para pegar o nome
                        using var db = new AppDbContext(dbPath);
                        var emp = await db.Empresas.FirstOrDefaultAsync(cancellationToken);
                        if (emp != null)
                            lista.Add(new EmpresaResumoDto(emp.Cnpj, emp.Nome, dbPath));
                    }
                    catch { /* Ignora se o banco estiver corrompido ou em uso */ }
                }
            }
            return lista;
        }
    }
}