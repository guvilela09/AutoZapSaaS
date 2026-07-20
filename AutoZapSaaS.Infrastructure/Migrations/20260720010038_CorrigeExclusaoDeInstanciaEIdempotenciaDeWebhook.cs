using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace AutoZapSaaS.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class CorrigeExclusaoDeInstanciaEIdempotenciaDeWebhook : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppMessages_Instances_InstanceId",
                table: "WhatsAppMessages");

            migrationBuilder.AlterColumn<Guid>(
                name: "InstanceId",
                table: "WhatsAppMessages",
                type: "uniqueidentifier",
                nullable: true,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier");

            migrationBuilder.AddColumn<string>(
                name: "PayloadHash",
                table: "WebhookEvents",
                type: "nvarchar(64)",
                maxLength: 64,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_WebhookEvents_TenantId_PayloadHash",
                table: "WebhookEvents",
                columns: new[] { "TenantId", "PayloadHash" });

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppMessages_Instances_InstanceId",
                table: "WhatsAppMessages",
                column: "InstanceId",
                principalTable: "Instances",
                principalColumn: "Id",
                onDelete: ReferentialAction.SetNull);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_WhatsAppMessages_Instances_InstanceId",
                table: "WhatsAppMessages");

            migrationBuilder.DropIndex(
                name: "IX_WebhookEvents_TenantId_PayloadHash",
                table: "WebhookEvents");

            migrationBuilder.DropColumn(
                name: "PayloadHash",
                table: "WebhookEvents");

            migrationBuilder.AlterColumn<Guid>(
                name: "InstanceId",
                table: "WhatsAppMessages",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"),
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AddForeignKey(
                name: "FK_WhatsAppMessages_Instances_InstanceId",
                table: "WhatsAppMessages",
                column: "InstanceId",
                principalTable: "Instances",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }
    }
}
