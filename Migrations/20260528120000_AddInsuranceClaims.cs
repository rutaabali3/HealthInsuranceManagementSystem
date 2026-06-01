using System;
using HealthInsuranceManagement.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace HealthInsuranceManagement.Migrations
{
    /// <inheritdoc />
    [DbContext(typeof(ApplicationDbContext))]
    [Migration("20260528120000_AddInsuranceClaims")]
    public partial class AddInsuranceClaims : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "InsuranceClaims",
                columns: table => new
                {
                    ClaimId = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    EmpId = table.Column<int>(type: "int", nullable: false),
                    PolicyOnEmployeeId = table.Column<int>(type: "int", nullable: false),
                    ClaimType = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ClaimAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    ApprovedAmount = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Reason = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Status = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SubmittedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ReviewedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    PaidAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ClosedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ManagerId = table.Column<int>(type: "int", nullable: true),
                    FinanceManagerId = table.Column<int>(type: "int", nullable: true),
                    Remarks = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_InsuranceClaims", x => x.ClaimId);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_EmpRegisters_EmpId",
                        column: x => x.EmpId,
                        principalTable: "EmpRegisters",
                        principalColumn: "EmpId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_EmpRegisters_FinanceManagerId",
                        column: x => x.FinanceManagerId,
                        principalTable: "EmpRegisters",
                        principalColumn: "EmpId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_EmpRegisters_ManagerId",
                        column: x => x.ManagerId,
                        principalTable: "EmpRegisters",
                        principalColumn: "EmpId",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_InsuranceClaims_PolicyOnEmployees_PolicyOnEmployeeId",
                        column: x => x.PolicyOnEmployeeId,
                        principalTable: "PolicyOnEmployees",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_EmpId",
                table: "InsuranceClaims",
                column: "EmpId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_FinanceManagerId",
                table: "InsuranceClaims",
                column: "FinanceManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_ManagerId",
                table: "InsuranceClaims",
                column: "ManagerId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_PolicyOnEmployeeId",
                table: "InsuranceClaims",
                column: "PolicyOnEmployeeId");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_Status",
                table: "InsuranceClaims",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_InsuranceClaims_SubmittedAt",
                table: "InsuranceClaims",
                column: "SubmittedAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "InsuranceClaims");
        }
    }
}
