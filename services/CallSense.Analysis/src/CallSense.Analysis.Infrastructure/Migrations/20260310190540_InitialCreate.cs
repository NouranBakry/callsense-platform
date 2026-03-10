using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CallSense.Analysis.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CallAnalyses",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    CallId = table.Column<Guid>(type: "uuid", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    OverallScore = table.Column<int>(type: "integer", nullable: false),
                    Report = table.Column<string>(type: "text", nullable: true),
                    Strengths = table.Column<string>(type: "text", nullable: true),
                    Improvements = table.Column<string>(type: "text", nullable: true),
                    Status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                    ErrorMessage = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CallAnalyses", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_CallAnalyses_CallId",
                table: "CallAnalyses",
                column: "CallId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CallAnalyses_TenantId",
                table: "CallAnalyses",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CallAnalyses");
        }
    }
}
