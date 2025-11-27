using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanHr.AuthApi.Persistence.RelationalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddPasswordResetCodeUserIdAndNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "PasswordResetCodes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_PasswordResetCodes_UserId",
                table: "PasswordResetCodes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_PasswordResetCodes_AspNetUsers_UserId",
                table: "PasswordResetCodes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_PasswordResetCodes_AspNetUsers_UserId",
                table: "PasswordResetCodes");

            migrationBuilder.DropIndex(
                name: "IX_PasswordResetCodes_UserId",
                table: "PasswordResetCodes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "PasswordResetCodes");
        }
    }
}
