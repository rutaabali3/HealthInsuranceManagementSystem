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
    [Migration("20260524130000_AddFaqItems")]
    public partial class AddFaqItems : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FaqItems",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    Question = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Answer = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Category = table.Column<string>(type: "varchar(80)", maxLength: 80, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DisplayOrder = table.Column<int>(type: "int", nullable: false),
                    IsActive = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    CreatedBySupportId = table.Column<int>(type: "int", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FaqItems", x => x.Id);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_FaqItems_Category_DisplayOrder_IsActive",
                table: "FaqItems",
                columns: new[] { "Category", "DisplayOrder", "IsActive" });

            migrationBuilder.Sql("""
                INSERT INTO FaqItems (Question, Answer, Category, DisplayOrder, IsActive, CreatedBySupportId, CreatedAt, UpdatedAt)
                VALUES
                ('Who can use HealthInsure?', 'HealthInsure is for authorized users in the insurance workflow. Admins manage employees, companies, policies, assignments, requests, and reports. Employees search and request policies. Managers and finance users handle decision and payment stages. Support users manage contact queries.', 'Accounts and Access', 1, 1, NULL, UTC_TIMESTAMP(6), NULL),
                ('Why do different users see different menus?', 'Navigation is role-based. A user only sees the dashboard and actions intended for their assigned role, which keeps sensitive employee, policy, request, and billing areas from being exposed to unrelated users.', 'Accounts and Access', 2, 1, NULL, UTC_TIMESTAMP(6), NULL),
                ('How does an employee request insurance?', 'The employee signs in, opens Search Policies, reviews available policies, opens policy details, and submits a request for the selected policy. The request then moves into the approval workflow.', 'Employee Policy Requests', 1, 1, NULL, UTC_TIMESTAMP(6), NULL),
                ('What happens after a policy request is submitted?', 'The request is saved with the employee and policy details. Authorized staff can review the request status and continue the workflow through manager and finance stages when applicable.', 'Approvals and Workflow', 1, 1, NULL, UTC_TIMESTAMP(6), NULL),
                ('Who handles finance actions?', 'Finance users handle approved billing records assigned to their workflow. They can review request details, credit payments, and close records when the required payment step is complete.', 'Finance and Billing', 1, 1, NULL, UTC_TIMESTAMP(6), NULL),
                ('What can administrators manage?', 'Administrators can manage employee accounts, insurance companies, policies, policy assignments, policy requests, reports, and system-level records needed for the organization.', 'Reports and Admin Data', 1, 1, NULL, UTC_TIMESTAMP(6), NULL),
                ('How do visitors contact support?', 'Visitors can use the contact page to send a message. Support users can then review the inbox, open the query, and reply using the stored conversation.', 'Support, Privacy, and Troubleshooting', 1, 1, NULL, UTC_TIMESTAMP(6), NULL);
                """);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FaqItems");
        }
    }
}
