using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.API.Migrations
{
    public partial class AddDoctorArabicFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "FirstNameAr",
                table: "Doctors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastNameAr",
                table: "Doctors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AddressAr",
                table: "Doctors",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "FirstNameAr", table: "Doctors");
            migrationBuilder.DropColumn(name: "LastNameAr", table: "Doctors");
            migrationBuilder.DropColumn(name: "AddressAr", table: "Doctors");
        }
    }
}
