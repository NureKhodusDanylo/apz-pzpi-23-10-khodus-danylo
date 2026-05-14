using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class FileSystem : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Nodes",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Nodes", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Robots",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Model = table.Column<string>(type: "TEXT", nullable: false),
                    Type = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    BatteryLevel = table.Column<double>(type: "REAL", nullable: false),
                    SerialNumber = table.Column<string>(type: "TEXT", nullable: true),
                    AccessKeyHash = table.Column<string>(type: "TEXT", nullable: true),
                    CurrentNodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    CurrentLatitude = table.Column<double>(type: "REAL", nullable: true),
                    CurrentLongitude = table.Column<double>(type: "REAL", nullable: true),
                    TargetNodeId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Robots", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Robots_Nodes_CurrentNodeId",
                        column: x => x.CurrentNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Robots_Nodes_TargetNodeId",
                        column: x => x.TargetNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Files",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    FileName = table.Column<string>(type: "TEXT", nullable: false),
                    FilePath = table.Column<string>(type: "TEXT", nullable: false),
                    ContentType = table.Column<string>(type: "TEXT", nullable: false),
                    FileSize = table.Column<long>(type: "INTEGER", nullable: false),
                    UploadedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    OrderId = table.Column<int>(type: "INTEGER", nullable: true),
                    UserId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Files", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    UserName = table.Column<string>(type: "TEXT", nullable: false),
                    Email = table.Column<string>(type: "TEXT", nullable: false),
                    GoogleId = table.Column<string>(type: "TEXT", nullable: true),
                    PhoneNumber = table.Column<string>(type: "TEXT", nullable: true),
                    PasswordHash = table.Column<string>(type: "TEXT", nullable: false),
                    Role = table.Column<int>(type: "INTEGER", nullable: true),
                    PersonalNodeId = table.Column<int>(type: "INTEGER", nullable: true),
                    ProfilePhotoId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Files_ProfilePhotoId",
                        column: x => x.ProfilePhotoId,
                        principalTable: "Files",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Users_Nodes_PersonalNodeId",
                        column: x => x.PersonalNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    Description = table.Column<string>(type: "TEXT", nullable: false),
                    Weight = table.Column<double>(type: "REAL", nullable: false),
                    DeliveryPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    ProductPrice = table.Column<decimal>(type: "TEXT", nullable: false),
                    IsProductPaid = table.Column<bool>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "TEXT", nullable: false),
                    CompletedAt = table.Column<DateTime>(type: "TEXT", nullable: true),
                    SenderId = table.Column<int>(type: "INTEGER", nullable: false),
                    RecipientId = table.Column<int>(type: "INTEGER", nullable: false),
                    RobotId = table.Column<int>(type: "INTEGER", nullable: true),
                    PickupNodeId = table.Column<int>(type: "INTEGER", nullable: false),
                    DropoffNodeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Orders_Nodes_DropoffNodeId",
                        column: x => x.DropoffNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Nodes_PickupNodeId",
                        column: x => x.PickupNodeId,
                        principalTable: "Nodes",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Robots_RobotId",
                        column: x => x.RobotId,
                        principalTable: "Robots",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Orders_Users_RecipientId",
                        column: x => x.RecipientId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Orders_Users_SenderId",
                        column: x => x.SenderId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Files_OrderId",
                table: "Files",
                column: "OrderId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_DropoffNodeId",
                table: "Orders",
                column: "DropoffNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_PickupNodeId",
                table: "Orders",
                column: "PickupNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RecipientId",
                table: "Orders",
                column: "RecipientId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_RobotId",
                table: "Orders",
                column: "RobotId");

            migrationBuilder.CreateIndex(
                name: "IX_Orders_SenderId",
                table: "Orders",
                column: "SenderId");

            migrationBuilder.CreateIndex(
                name: "IX_Robots_CurrentNodeId",
                table: "Robots",
                column: "CurrentNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Robots_SerialNumber",
                table: "Robots",
                column: "SerialNumber",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Robots_TargetNodeId",
                table: "Robots",
                column: "TargetNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_PersonalNodeId",
                table: "Users",
                column: "PersonalNodeId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_ProfilePhotoId",
                table: "Users",
                column: "ProfilePhotoId",
                unique: true);

            migrationBuilder.AddForeignKey(
                name: "FK_Files_Orders_OrderId",
                table: "Files",
                column: "OrderId",
                principalTable: "Orders",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Files_Orders_OrderId",
                table: "Files");

            migrationBuilder.DropTable(
                name: "Orders");

            migrationBuilder.DropTable(
                name: "Robots");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Files");

            migrationBuilder.DropTable(
                name: "Nodes");
        }
    }
}
