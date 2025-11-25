using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanHr.AuthApi.Persistence.RelationalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddOneTimeUseValidation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "UsedAtUtc",
                table: "RefreshTokens",
                type: "datetime2",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "UsedAtUtc",
                table: "RefreshTokens");
        }
    }
}
