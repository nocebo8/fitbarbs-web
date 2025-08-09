using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace FitBarbs.Web.Data.Migrations
{
    /// <inheritdoc />
    public partial class ProfilePreferredDifficulty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "PreferredDifficulty",
                table: "UserProfiles",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "PreferredDifficulty",
                table: "UserProfiles");
        }
    }
}
