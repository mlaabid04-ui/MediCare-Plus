using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddDoctorLocation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "City",
                table: "Doctors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "HospitalName",
                table: "Doctors",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Latitude",
                table: "Doctors",
                type: "double precision",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Longitude",
                table: "Doctors",
                type: "double precision",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "City",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "HospitalName",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Latitude",
                table: "Doctors");

            migrationBuilder.DropColumn(
                name: "Longitude",
                table: "Doctors");
        }
    }
}
