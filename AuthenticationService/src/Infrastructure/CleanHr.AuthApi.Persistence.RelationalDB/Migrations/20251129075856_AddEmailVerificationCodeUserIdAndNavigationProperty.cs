using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CleanHr.AuthApi.Persistence.RelationalDB.Migrations
{
    /// <inheritdoc />
    public partial class AddEmailVerificationCodeUserIdAndNavigationProperty : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "EmailVerificationCodes",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_EmailVerificationCodes_UserId",
                table: "EmailVerificationCodes",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_EmailVerificationCodes_AspNetUsers_UserId",
                table: "EmailVerificationCodes",
                column: "UserId",
                principalTable: "AspNetUsers",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_EmailVerificationCodes_AspNetUsers_UserId",
                table: "EmailVerificationCodes");

            migrationBuilder.DropIndex(
                name: "IX_EmailVerificationCodes_UserId",
                table: "EmailVerificationCodes");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "EmailVerificationCodes");
        }
    }
}
