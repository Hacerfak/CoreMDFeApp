using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddDiretorioPdf : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "CaminhoSchemas",
                table: "Configuracoes",
                newName: "DiretorioSalvarPdf");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "DiretorioSalvarPdf",
                table: "Configuracoes",
                newName: "CaminhoSchemas");
        }
    }
}
