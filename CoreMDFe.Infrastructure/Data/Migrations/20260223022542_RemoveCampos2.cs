using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCampos2 : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "NumeroSerieCertificado",
                table: "Configuracoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "NumeroSerieCertificado",
                table: "Configuracoes",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");
        }
    }
}
