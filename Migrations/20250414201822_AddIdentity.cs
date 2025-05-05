using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace test_main.Migrations
{
    /// <inheritdoc />
    public partial class AddIdentity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_refresh_tokens_users_UserEmail",
                table: "refresh_tokens");

            migrationBuilder.DropPrimaryKey(
                name: "PK_users",
                table: "users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens");

            migrationBuilder.RenameTable(
                name: "users",
                newName: "app_users");

            migrationBuilder.RenameTable(
                name: "refresh_tokens",
                newName: "app_refresh_tokens");

            migrationBuilder.RenameColumn(
                name: "Salt",
                table: "app_users",
                newName: "Id");

            migrationBuilder.RenameColumn(
                name: "UserEmail",
                table: "app_refresh_tokens",
                newName: "UserId");

            migrationBuilder.RenameIndex(
                name: "IX_refresh_tokens_UserEmail",
                table: "app_refresh_tokens",
                newName: "IX_app_refresh_tokens_UserId");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "app_users",
                type: "text",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "app_users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "text");

            migrationBuilder.AddColumn<int>(
                name: "AccessFailedCount",
                table: "app_users",
                type: "integer",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "ConcurrencyStamp",
                table: "app_users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "EmailConfirmed",
                table: "app_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "FirstName",
                table: "app_users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LastName",
                table: "app_users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "LockoutEnabled",
                table: "app_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LockoutEnd",
                table: "app_users",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedEmail",
                table: "app_users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "NormalizedUserName",
                table: "app_users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "PhoneNumber",
                table: "app_users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "PhoneNumberConfirmed",
                table: "app_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "SecurityStamp",
                table: "app_users",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "TwoFactorEnabled",
                table: "app_users",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "UserName",
                table: "app_users",
                type: "character varying(256)",
                maxLength: 256,
                nullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_app_users",
                table: "app_users",
                column: "Id");

            migrationBuilder.AddPrimaryKey(
                name: "PK_app_refresh_tokens",
                table: "app_refresh_tokens",
                column: "TokenId");

            migrationBuilder.CreateTable(
                name: "app_roles",
                columns: table => new
                {
                    Id = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    NormalizedName = table.Column<string>(type: "character varying(256)", maxLength: 256, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_roles", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "app_user_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    UserId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_app_user_claims_app_users_UserId",
                        column: x => x.UserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_logins",
                columns: table => new
                {
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    ProviderKey = table.Column<string>(type: "text", nullable: false),
                    ProviderDisplayName = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user_logins", x => new { x.LoginProvider, x.ProviderKey });
                    table.ForeignKey(
                        name: "FK_app_user_logins_app_users_UserId",
                        column: x => x.UserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_tokens",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    LoginProvider = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Value = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user_tokens", x => new { x.UserId, x.LoginProvider, x.Name });
                    table.ForeignKey(
                        name: "FK_app_user_tokens_app_users_UserId",
                        column: x => x.UserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_role_claims",
                columns: table => new
                {
                    Id = table.Column<int>(type: "integer", nullable: false)
                        .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                    RoleId = table.Column<string>(type: "text", nullable: false),
                    ClaimType = table.Column<string>(type: "text", nullable: true),
                    ClaimValue = table.Column<string>(type: "text", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_role_claims", x => x.Id);
                    table.ForeignKey(
                        name: "FK_app_role_claims_app_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "app_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "app_user_roles",
                columns: table => new
                {
                    UserId = table.Column<string>(type: "text", nullable: false),
                    RoleId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_app_user_roles", x => new { x.UserId, x.RoleId });
                    table.ForeignKey(
                        name: "FK_app_user_roles_app_roles_RoleId",
                        column: x => x.RoleId,
                        principalTable: "app_roles",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_app_user_roles_app_users_UserId",
                        column: x => x.UserId,
                        principalTable: "app_users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "EmailIndex",
                table: "app_users",
                column: "NormalizedEmail");

            migrationBuilder.CreateIndex(
                name: "UserNameIndex",
                table: "app_users",
                column: "NormalizedUserName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_role_claims_RoleId",
                table: "app_role_claims",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "RoleNameIndex",
                table: "app_roles",
                column: "NormalizedName",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_app_user_claims_UserId",
                table: "app_user_claims",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_logins_UserId",
                table: "app_user_logins",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_app_user_roles_RoleId",
                table: "app_user_roles",
                column: "RoleId");

            migrationBuilder.AddForeignKey(
                name: "FK_app_refresh_tokens_app_users_UserId",
                table: "app_refresh_tokens",
                column: "UserId",
                principalTable: "app_users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_app_refresh_tokens_app_users_UserId",
                table: "app_refresh_tokens");

            migrationBuilder.DropTable(
                name: "app_role_claims");

            migrationBuilder.DropTable(
                name: "app_user_claims");

            migrationBuilder.DropTable(
                name: "app_user_logins");

            migrationBuilder.DropTable(
                name: "app_user_roles");

            migrationBuilder.DropTable(
                name: "app_user_tokens");

            migrationBuilder.DropTable(
                name: "app_roles");

            migrationBuilder.DropPrimaryKey(
                name: "PK_app_users",
                table: "app_users");

            migrationBuilder.DropIndex(
                name: "EmailIndex",
                table: "app_users");

            migrationBuilder.DropIndex(
                name: "UserNameIndex",
                table: "app_users");

            migrationBuilder.DropPrimaryKey(
                name: "PK_app_refresh_tokens",
                table: "app_refresh_tokens");

            migrationBuilder.DropColumn(
                name: "AccessFailedCount",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "ConcurrencyStamp",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "EmailConfirmed",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "FirstName",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "LastName",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "LockoutEnabled",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "LockoutEnd",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "NormalizedEmail",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "NormalizedUserName",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "PhoneNumber",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "PhoneNumberConfirmed",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "SecurityStamp",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "TwoFactorEnabled",
                table: "app_users");

            migrationBuilder.DropColumn(
                name: "UserName",
                table: "app_users");

            migrationBuilder.RenameTable(
                name: "app_users",
                newName: "users");

            migrationBuilder.RenameTable(
                name: "app_refresh_tokens",
                newName: "refresh_tokens");

            migrationBuilder.RenameColumn(
                name: "Id",
                table: "users",
                newName: "Salt");

            migrationBuilder.RenameColumn(
                name: "UserId",
                table: "refresh_tokens",
                newName: "UserEmail");

            migrationBuilder.RenameIndex(
                name: "IX_app_refresh_tokens_UserId",
                table: "refresh_tokens",
                newName: "IX_refresh_tokens_UserEmail");

            migrationBuilder.AlterColumn<string>(
                name: "PasswordHash",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "text",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "Email",
                table: "users",
                type: "text",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "character varying(256)",
                oldMaxLength: 256,
                oldNullable: true);

            migrationBuilder.AddPrimaryKey(
                name: "PK_users",
                table: "users",
                column: "Email");

            migrationBuilder.AddPrimaryKey(
                name: "PK_refresh_tokens",
                table: "refresh_tokens",
                column: "TokenId");

            migrationBuilder.AddForeignKey(
                name: "FK_refresh_tokens_users_UserEmail",
                table: "refresh_tokens",
                column: "UserEmail",
                principalTable: "users",
                principalColumn: "Email",
                onDelete: ReferentialAction.Cascade);
        }
    }
}
