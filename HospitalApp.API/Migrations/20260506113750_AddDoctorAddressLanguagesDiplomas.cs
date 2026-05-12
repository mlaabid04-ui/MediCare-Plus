using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorAddressLanguagesDiplomas : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Address",
                table: "Doctors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Diplomas",
                table: "Doctors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Languages",
                table: "Doctors",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Address",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Diplomas",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Languages",
                table: "Doctors");
        }
    }
}
