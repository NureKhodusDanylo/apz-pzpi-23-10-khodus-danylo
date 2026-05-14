using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AdminCodes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "BatteryCapacityJoules",
                table: "Robots",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<double>(
                name: "EnergyConsumptionPerMeterJoules",
                table: "Robots",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<string>(
                name: "IpAddress",
                table: "Robots",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "MaxFlightRangeMeters",
                table: "Robots",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);

            migrationBuilder.AddColumn<int>(
                name: "Port",
                table: "Robots",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "DeliveryPayer",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<bool>(
                name: "IsDeliveryPaid",
                table: "Orders",
                type: "INTEGER",
                nullable: false,
                defaultValue: false);

            migrationBuilder.CreateTable(
                name: "AdminKeys",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    KeyCode = table.Column<string>(type: "TEXT", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    IsUsed = table.Column<bool>(type: "INTEGER", nullable: false),
                    UsedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    UsedByUserId = table.Column<int>(type: "INTEGER", nullable: true),
                    CreatedByAdminId = table.Column<int>(type: "INTEGER", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AdminKeys", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AdminKeys_Users_CreatedByAdminId",
                        column: x => x.CreatedByAdminId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AdminKeys_Users_UsedByUserId",
                        column: x => x.UsedByUserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AdminKeys_CreatedByAdminId",
                table: "AdminKeys",
                column: "CreatedByAdminId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminKeys_KeyCode",
                table: "AdminKeys",
                column: "KeyCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AdminKeys_UsedByUserId",
                table: "AdminKeys",
                column: "UsedByUserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AdminKeys");

            migrationBuilder.DropColumn(
                name: "BatteryCapacityJoules",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "EnergyConsumptionPerMeterJoules",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "IpAddress",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "MaxFlightRangeMeters",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "Port",
                table: "Robots");

            migrationBuilder.DropColumn(
                name: "DeliveryPayer",
                table: "Orders");

            migrationBuilder.DropColumn(
                name: "IsDeliveryPaid",
                table: "Orders");
        }
    }
}
