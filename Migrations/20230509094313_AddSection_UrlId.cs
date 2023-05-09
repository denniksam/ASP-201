using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ASP_201.Migrations
{
    /// <inheritdoc />
    public partial class AddSection_UrlId : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "UrlId",
                table: "Sections",
                type: "longtext",
                nullable: true)
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_Sections_AuthorId",
                table: "Sections",
                column: "AuthorId");

            //migrationBuilder.AddForeignKey(
            //    name: "FK_Sections_Users_AuthorId",
            //    table: "Sections",
            //    column: "AuthorId",
            //    principalTable: "Users",
            //    principalColumn: "Id",
            //    onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            //migrationBuilder.DropForeignKey(
            //    name: "FK_Sections_Users_AuthorId",
            //    table: "Sections");

            migrationBuilder.DropIndex(
                name: "IX_Sections_AuthorId",
                table: "Sections");

            migrationBuilder.DropColumn(
                name: "UrlId",
                table: "Sections");
        }
    }
}
