using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace test_main.Migrations
{
    /// <inheritdoc />
    public partial class semailsrimaryey : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_users_Email",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "Id",
                table: "users");

            migrationBuilder.DropColumn(
                name: "RevokedAt",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<string>(
                name: "UserEmail",
                table: "refresh_tokens",
                type: "text",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Email");

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserEmail",
                table: "refresh_tokens",
                column: "UserEmail");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_UserEmail",
                table: "refresh_tokens",
                column: "UserEmail",
                principalTable: "users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_UserEmail",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropIndex(
                name: "IX_refresh_tokens_UserEmail",
                table: "refresh_tokens");

            migrationBuilder.DropColumn(
                name: "UserEmail",
                table: "refresh_tokens");

            migrationBuilder.AddColumn<int>(
                name: "Id",
                table: "users",
                type: "integer",
                nullable: false,
                defaultValue: 0)
                .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn);

            migrationBuilder.AddColumn<DateTime>(
                name: "RevokedAt",
                table: "refresh_tokens",
                type: "timestamp with time zone",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<int>(
                name: "UserId",
                table: "refresh_tokens",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Id");

            migrationBuilder.CreateIndex(
                name: "IX_users_Email",
                table: "users",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_refresh_tokens_UserId",
                table: "refresh_tokens",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_UserId",
                table: "refresh_tokens",
                column: "UserId",
                principalTable: "users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
