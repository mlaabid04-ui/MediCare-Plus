using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.API.Migrations
{
    public partial class AddScheduleTypeAndVacations : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ConsultationType",
                table: "DoctorSchedules",
                type: "text",
                nullable: false,
                defaultValue: "Présentiel");

            migrationBuilder.CreateTable(
                name: "DoctorVacations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DoctorId = table.Column<Guid>(type: "uuid", nullable: false),
                    Label = table.Column<string>(type: "text", nullable: false),
                    StartDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false),
                    EndDate = table.Column<DateTime>(type: "timestamp without time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DoctorVacations", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DoctorVacations_Doctors_DoctorId",
                        column: x => x.DoctorId,
                        principalTable: "Doctors",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_DoctorVacations_DoctorId",
                table: "DoctorVacations",
                column: "DoctorId");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "DoctorVacations");

            migrationBuilder.DropColumn(
                name: "ConsultationType",
                table: "DoctorSchedules");
        }
    }
}
