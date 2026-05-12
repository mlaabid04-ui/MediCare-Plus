using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.API.Migrations
{
    public partial class AddDoctorPostalCode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "PostalCode",
                table: "Doctors",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PostalCode",
                table: "Doctors");
        }
    }
}
