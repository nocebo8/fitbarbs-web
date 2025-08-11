using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitBarbs.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserPreferencesToUserProfile : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .OldAnnotation("Sqlite:Autoincrement", true);

            migrationBuilder.AddColumn<bool>(
                name: "Preferences_AutoCompleteLessonAfterWatch",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Preferences_EmailProgressSummaries",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "Preferences_PlayNextAutomatically",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "Preferences_TargetDailyStudyMinutes",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Preferences_AutoCompleteLessonAfterWatch",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Preferences_EmailProgressSummaries",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Preferences_PlayNextAutomatically",
                table: "UserProfiles");

            migrationBuilder.DropColumn(
                name: "Preferences_TargetDailyStudyMinutes",
                table: "UserProfiles");

            migrationBuilder.AlterColumn<int>(
                name: "Id",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER")
                .Annotation("Sqlite:Autoincrement", true);
        }
    }
}
