using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Auth.API.Migrations
{
    /// <inheritdoc />
    public partial class AddUserVerification : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_UserExternalLogins_AuthProviders_Provider",
                table: "UserExternalLogins");

            migrationBuilder.DropForeignKey(
                name: "FK_UserExternalLogins_Users_UserId1",
                table: "UserExternalLogins");

            migrationBuilder.DropIndex(
                name: "IX_UserExternalLogins_UserId1",
                table: "UserExternalLogins");

            migrationBuilder.DropColumn(
                name: "UserId1",
                table: "UserExternalLogins");

            migrationBuilder.CreateTable(
                name: "UserVerifications",
                columns: table => new
                {
                    VerificationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Type = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Code = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    VerifiedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UserVerifications", x => x.VerificationId);
                    table.ForeignKey(
                        name: "FK_UserVerifications_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "UserId",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_UserVerifications_UserId",
                table: "UserVerifications",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UserVerifications");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId1",
                table: "UserExternalLogins",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.CreateIndex(
                name: "IX_UserExternalLogins_UserId1",
                table: "UserExternalLogins",
                column: "UserId1");

            migrationBuilder.AddForeignKey(
                name: "FK_UserExternalLogins_AuthProviders_Provider",
                table: "UserExternalLogins",
                column: "Provider",
                principalTable: "AuthProviders",
                principalColumn: "Provider",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_UserExternalLogins_Users_UserId1",
                table: "UserExternalLogins",
                column: "UserId1",
                principalTable: "Users",
                principalColumn: "UserId");
        }
    }
}
