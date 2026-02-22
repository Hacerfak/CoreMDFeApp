using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RenameXmlToRecibos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "XmlEncerramento",
                table: "Manifestos",
                newName: "ReciboEncerramento");

            migrationBuilder.RenameColumn(
                name: "XmlCancelamento",
                table: "Manifestos",
                newName: "ReciboCancelamento");

            migrationBuilder.RenameColumn(
                name: "XmlAutorizado",
                table: "Manifestos",
                newName: "ReciboAutorizacao");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "ReciboEncerramento",
                table: "Manifestos",
                newName: "XmlEncerramento");

            migrationBuilder.RenameColumn(
                name: "ReciboCancelamento",
                table: "Manifestos",
                newName: "XmlCancelamento");

            migrationBuilder.RenameColumn(
                name: "ReciboAutorizacao",
                table: "Manifestos",
                newName: "XmlAutorizado");
        }
    }
}
