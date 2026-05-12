using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.API.Migrations
{
    public partial class DoctorProfileFields : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SlotDurationMinutes",
                table: "Doctors",
                type: "integer",
                nullable: false,
                defaultValue: 30);

            migrationBuilder.AddColumn<string>(
                name: "CabinetImages",
                table: "Doctors",
                type: "text",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "SlotDurationMinutes", table: "Doctors");
            migrationBuilder.DropColumn(name: "CabinetImages", table: "Doctors");
        }
    }
}
