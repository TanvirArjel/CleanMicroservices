using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanHr.AuthApi.Persistence.RelationalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddTokenFamilyTracking : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "TokenFamilyId",
                table: "RefreshTokens",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_RefreshTokens_TokenFamilyId",
                table: "RefreshTokens",
                column: "TokenFamilyId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_RefreshTokens_TokenFamilyId",
                table: "RefreshTokens");

            migrationBuilder.DropColumn(
                name: "TokenFamilyId",
                table: "RefreshTokens");
        }
    }
}
