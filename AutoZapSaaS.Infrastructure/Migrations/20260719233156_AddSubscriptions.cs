using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoZapSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddSubscriptions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Subscriptions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    TenantId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Tier = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<int>(type: "int", nullable: false),
                    AsaasSubscriptionId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    AsaasCustomerId = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                    PeriodoFimEm = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CicloIniciadoEm = table.Column<DateTime>(type: "datetime2", nullable: false),
                    MensagensNoCiclo = table.Column<int>(type: "int", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Subscriptions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Subscriptions_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_AsaasSubscriptionId",
                table: "Subscriptions",
                column: "AsaasSubscriptionId");

            migrationBuilder.CreateIndex(
                name: "IX_Subscriptions_TenantId",
                table: "Subscriptions",
                column: "TenantId",
                unique: true);

            // Tenants que ja existiam precisam de assinatura: sem uma, ficariam sem
            // plano e os limites seriam indefinidos. Todos entram no Free (Tier 0)
            // com status Active (1).
            migrationBuilder.Sql("""
                INSERT INTO Subscriptions
                    (Id, TenantId, Tier, Status, AsaasSubscriptionId, AsaasCustomerId,
                     PeriodoFimEm, CicloIniciadoEm, MensagensNoCiclo, CreatedAt, UpdatedAt)
                SELECT NEWID(), t.Id, 0, 1, NULL, NULL, NULL, GETUTCDATE(), 0, GETUTCDATE(), NULL
                FROM Tenants t
                WHERE NOT EXISTS (SELECT 1 FROM Subscriptions s WHERE s.TenantId = t.Id);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Subscriptions");
        }
    }
}
