using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HospitalApp.API.Migrations
{
    public partial class FrenchSpecialties : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Rename existing 18 specialties to French
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Cardiologue', \"Description\"='Coeur et système cardiovasculaire' WHERE \"Id\"='11111111-0000-0000-0000-000000000001'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Dermatologue', \"Description\"='Peau, cheveux et ongles' WHERE \"Id\"='11111111-0000-0000-0000-000000000002'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Neurologue', \"Description\"='Cerveau et système nerveux' WHERE \"Id\"='11111111-0000-0000-0000-000000000003'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Chirurgien orthopédique', \"Description\"='Os, articulations et muscles' WHERE \"Id\"='11111111-0000-0000-0000-000000000004'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Pédiatre', \"Description\"='Santé de l''enfant' WHERE \"Id\"='11111111-0000-0000-0000-000000000005'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Médecin généraliste', \"Description\"='Soins de santé généraux' WHERE \"Id\"='11111111-0000-0000-0000-000000000006'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Ophtalmologue', \"Description\"='Yeux et vision' WHERE \"Id\"='11111111-0000-0000-0000-000000000007'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Gynécologue obstétricien', \"Description\"='Santé reproductive féminine' WHERE \"Id\"='11111111-0000-0000-0000-000000000008'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Psychiatre', \"Description\"='Santé mentale' WHERE \"Id\"='11111111-0000-0000-0000-000000000009'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Radiologue', \"Description\"='Imagerie médicale' WHERE \"Id\"='11111111-0000-0000-0000-000000000010'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Chirurgien dentiste', \"Description\"='Soins dentaires' WHERE \"Id\"='11111111-0000-0000-0000-000000000011'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Gastrologue entérologue', \"Description\"='Système digestif' WHERE \"Id\"='11111111-0000-0000-0000-000000000012'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Kinésithérapeute', \"Description\"='Rééducation physique' WHERE \"Id\"='11111111-0000-0000-0000-000000000013'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Médecin légal et de travail', \"Description\"='Santé en milieu professionnel' WHERE \"Id\"='11111111-0000-0000-0000-000000000014'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Endocrinologue – maladies métaboliques', \"Description\"='Hormones et glandes endocrines' WHERE \"Id\"='11111111-0000-0000-0000-000000000015'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Oto-rhino-laryngologue', \"Description\"='Oreille, nez et gorge' WHERE \"Id\"='11111111-0000-0000-0000-000000000016'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Pneumologue', \"Description\"='Poumons et système respiratoire' WHERE \"Id\"='11111111-0000-0000-0000-000000000017'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Rhumatologue', \"Description\"='Articulations et maladies auto-immunes' WHERE \"Id\"='11111111-0000-0000-0000-000000000018'");

            // Insert new specialties (ON CONFLICT DO NOTHING so re-runs are safe)
            var newSpecialties = new[]
            {
                ("22222222-0000-0000-0000-000000000001", "Acupuncteur"),
                ("22222222-0000-0000-0000-000000000002", "Algologue"),
                ("22222222-0000-0000-0000-000000000003", "Allergologue"),
                ("22222222-0000-0000-0000-000000000004", "Anatomo-pathologiste"),
                ("22222222-0000-0000-0000-000000000005", "Anesthésiste-réanimateur"),
                ("22222222-0000-0000-0000-000000000006", "Angiologue"),
                ("22222222-0000-0000-0000-000000000007", "Autre"),
                ("22222222-0000-0000-0000-000000000008", "Biologiste vétérinaire"),
                ("22222222-0000-0000-0000-000000000009", "Cancérologue"),
                ("22222222-0000-0000-0000-000000000010", "Chirurgien cancérologue"),
                ("22222222-0000-0000-0000-000000000011", "Chirurgien cardiovasculaire"),
                ("22222222-0000-0000-0000-000000000012", "Chirurgien général"),
                ("22222222-0000-0000-0000-000000000013", "Chirurgien maxillo-facial"),
                ("22222222-0000-0000-0000-000000000014", "Chirurgien plasticien"),
                ("22222222-0000-0000-0000-000000000015", "Chirurgien thoracique"),
                ("22222222-0000-0000-0000-000000000016", "Diabétologue"),
                ("22222222-0000-0000-0000-000000000017", "Génétique médicale"),
                ("22222222-0000-0000-0000-000000000018", "Gériatre"),
                ("22222222-0000-0000-0000-000000000019", "Gérontologue"),
                ("22222222-0000-0000-0000-000000000020", "Gynécologue sexologue"),
                ("22222222-0000-0000-0000-000000000021", "Homéopathe"),
                ("22222222-0000-0000-0000-000000000022", "Infectiologue"),
                ("22222222-0000-0000-0000-000000000023", "Infirmier"),
                ("22222222-0000-0000-0000-000000000024", "Médecin biologiste"),
                ("22222222-0000-0000-0000-000000000025", "Médecin interne"),
                ("22222222-0000-0000-0000-000000000026", "Médecin morphologique et anti-âge"),
                ("22222222-0000-0000-0000-000000000027", "Médecin Ostéopathe"),
                ("22222222-0000-0000-0000-000000000028", "Médecin physique et réadaptation fonctionnelle"),
                ("22222222-0000-0000-0000-000000000029", "Médecin sportif"),
                ("22222222-0000-0000-0000-000000000030", "Médecin urgentiste"),
                ("22222222-0000-0000-0000-000000000031", "Médecine nucléaire"),
                ("22222222-0000-0000-0000-000000000032", "Néphrologue"),
                ("22222222-0000-0000-0000-000000000033", "Neurochirurgien"),
                ("22222222-0000-0000-0000-000000000034", "Neuropsychiatre"),
                ("22222222-0000-0000-0000-000000000035", "Nutritionniste"),
                ("22222222-0000-0000-0000-000000000036", "Odontologue chirurgicale"),
                ("22222222-0000-0000-0000-000000000037", "Oncologue médicale"),
                ("22222222-0000-0000-0000-000000000038", "Opticien"),
                ("22222222-0000-0000-0000-000000000039", "Orthodontiste"),
                ("22222222-0000-0000-0000-000000000040", "Orthopédiste dento-faciale"),
                ("22222222-0000-0000-0000-000000000041", "Orthophoniste"),
                ("22222222-0000-0000-0000-000000000042", "Orthoptiste"),
                ("22222222-0000-0000-0000-000000000043", "Parodontologue"),
                ("22222222-0000-0000-0000-000000000044", "Pédodontiste"),
                ("22222222-0000-0000-0000-000000000045", "Pharmacologue"),
                ("22222222-0000-0000-0000-000000000046", "Podologue"),
                ("22222222-0000-0000-0000-000000000047", "Psychologue"),
                ("22222222-0000-0000-0000-000000000048", "Psychomotricité"),
                ("22222222-0000-0000-0000-000000000049", "Radiothérapeute"),
                ("22222222-0000-0000-0000-000000000050", "Réanimateur"),
                ("22222222-0000-0000-0000-000000000051", "Stomatologue"),
                ("22222222-0000-0000-0000-000000000052", "Urologue"),
            };

            foreach (var (id, name) in newSpecialties)
            {
                var safeName = name.Replace("'", "''");
                migrationBuilder.Sql($"INSERT INTO \"Specialties\" (\"Id\", \"Name\", \"Description\", \"IconName\", \"Color\") VALUES ('{id}', '{safeName}', '{safeName}', 'stethoscope', '#4F46E5') ON CONFLICT (\"Id\") DO NOTHING");
            }
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Cardiology' WHERE \"Id\"='11111111-0000-0000-0000-000000000001'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Dermatology' WHERE \"Id\"='11111111-0000-0000-0000-000000000002'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Neurology' WHERE \"Id\"='11111111-0000-0000-0000-000000000003'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Orthopedics' WHERE \"Id\"='11111111-0000-0000-0000-000000000004'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Pediatrics' WHERE \"Id\"='11111111-0000-0000-0000-000000000005'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='General Medicine' WHERE \"Id\"='11111111-0000-0000-0000-000000000006'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Ophthalmology' WHERE \"Id\"='11111111-0000-0000-0000-000000000007'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Gynecology' WHERE \"Id\"='11111111-0000-0000-0000-000000000008'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Psychiatry' WHERE \"Id\"='11111111-0000-0000-0000-000000000009'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Radiology' WHERE \"Id\"='11111111-0000-0000-0000-000000000010'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Dentaire' WHERE \"Id\"='11111111-0000-0000-0000-000000000011'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Gastrologie' WHERE \"Id\"='11111111-0000-0000-0000-000000000012'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Kinésithérapie' WHERE \"Id\"='11111111-0000-0000-0000-000000000013'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Médecine du travail' WHERE \"Id\"='11111111-0000-0000-0000-000000000014'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Endocrinologie' WHERE \"Id\"='11111111-0000-0000-0000-000000000015'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='ORL' WHERE \"Id\"='11111111-0000-0000-0000-000000000016'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Pneumologie' WHERE \"Id\"='11111111-0000-0000-0000-000000000017'");
            migrationBuilder.Sql("UPDATE \"Specialties\" SET \"Name\"='Rhumatologie' WHERE \"Id\"='11111111-0000-0000-0000-000000000018'");

            for (int i = 1; i <= 52; i++)
                migrationBuilder.Sql($"DELETE FROM \"Specialties\" WHERE \"Id\"='22222222-0000-0000-0000-{i:D12}'");
        }
    }
}
