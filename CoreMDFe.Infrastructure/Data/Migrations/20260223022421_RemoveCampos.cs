using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CoreMDFe.Infrastructure.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveCampos : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "VersaoLayout",
                table: "Configuracoes");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "VersaoLayout",
                table: "Configuracoes",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }
    }
}
