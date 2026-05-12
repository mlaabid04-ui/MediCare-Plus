using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace HospitalApp.API.Migrations
{
    /// <inheritdoc />
    public partial class AddMissingSpecialties : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Specialties",
                columns: new[] { "Id", "Color", "Description", "IconName", "Name" },
                values: new object[,]
                {
                    { new Guid("11111111-0000-0000-0000-000000000011"), "#F9A825", "Soins dentaires et bucco-dentaires", "tooth", "Dentaire" },
                    { new Guid("11111111-0000-0000-0000-000000000012"), "#43A047", "Système digestif et gastro-intestinal", "stomach", "Gastrologie" },
                    { new Guid("11111111-0000-0000-0000-000000000013"), "#EF6C00", "Rééducation physique et motrice", "physio", "Kinésithérapie" },
                    { new Guid("11111111-0000-0000-0000-000000000014"), "#1565C0", "Santé en milieu professionnel", "work", "Médecine du travail" },
                    { new Guid("11111111-0000-0000-0000-000000000015"), "#7B1FA2", "Hormones et glandes endocrines", "hormone", "Endocrinologie" },
                    { new Guid("11111111-0000-0000-0000-000000000016"), "#E65100", "Oreille, nez et gorge", "ear", "ORL" },
                    { new Guid("11111111-0000-0000-0000-000000000017"), "#00838F", "Poumons et système respiratoire", "lungs", "Pneumologie" },
                    { new Guid("11111111-0000-0000-0000-000000000018"), "#546E7A", "Articulations, os et maladies auto-immunes", "joint", "Rhumatologie" }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000011"));

            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000012"));

            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000013"));

            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000014"));

            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000015"));

            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000016"));

            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000017"));

            migrationBuilder.DeleteData(
                table: "Specialties",
                keyColumn: "Id",
                keyValue: new Guid("11111111-0000-0000-0000-000000000018"));
        }
    }
}
