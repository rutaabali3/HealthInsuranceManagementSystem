<div align="center">

# Health Insurance Management System

<p align="center">
  <strong>Enterprise-Grade Healthcare Insurance, Policy Administration, Billing, and Claims Platform</strong>
</p>

[![Framework](https://img.shields.io/badge/Framework-ASP.NET%20Core%208.0%20MVC-512BD4?style=for-the-badge&logo=dotnet)](https://dotnet.microsoft.com/)
[![ORM](https://img.shields.io/badge/ORM-Entity%20Framework%20Core%208.0-512BD4?style=for-the-badge)](https://learn.microsoft.com/en-us/ef/core/)
[![Database](https://img.shields.io/badge/Database-MySQL%20%7C%20MariaDB%20%7C%20SQL%20Server-4479A1?style=for-the-badge&logo=mysql)](https://www.mysql.com/)
[![Frontend](https://img.shields.io/badge/Frontend-Bootstrap%205.3%20%7C%20HTML5%20%7C%20CSS3-7952B3?style=for-the-badge&logo=bootstrap)](https://getbootstrap.com/)
[![Security](https://img.shields.io/badge/Security-BCrypt%20Hashing%20%26%20RBAC-00599C?style=for-the-badge)](https://github.com/)
[![License](https://img.shields.io/badge/License-MIT-blue?style=for-the-badge)](LICENSE)

</div>

---

## Executive Overview

The **Health Insurance Management System** is a full-featured, secure ASP.NET Core MVC web platform built for corporate health insurance lifecycle operations. It bridges healthcare providers, insurance companies, corporate managers, finance officers, support agents, and enterprise employees into a unified system.

The system features multi-role authorization, self-service employee policy management, real-time coverage verification, strict separation of policy billing and insurance claims processing, support query ticketing, automated email logs, and detailed auditing capabilities.

---

## Navigation Table of Contents

- [Executive Overview](#executive-overview)
- [Key Features](#key-features)
- [System Architecture](#system-architecture)
- [Role-Based Access Control](#role-based-access-control)
- [Data Models and Database Schema](#data-models-and-database-schema)
- [Core Workflow Lifecycles](#core-workflow-lifecycles)
  - [1. Policy Request & Billing Lifecycle](#1-policy-request--billing-lifecycle)
  - [2. Insurance Claims Lifecycle](#2-insurance-claims-lifecycle)
- [Module Breakdown](#module-breakdown)
- [Environment Setup and Installation](#environment-setup-and-installation)
  - [Prerequisites](#prerequisites)
  - [Option A: Setup with VS Code & XAMPP (MySQL)](#option-a-setup-with-vs-code--xampp-mysql)
  - [Option B: Setup with Visual Studio 2022 & SQL Server](#option-b-setup-with-visual-studio-2022--sql-server)
- [Default System Credentials](#default-system-credentials)
- [Security Practices](#security-practices)
- [Contributing](#contributing)
- [License](#license)

---

## Key Features

<details>
<summary><strong>Expand to View Key Feature Details</strong></summary>

<br />

* **Role-Based Access Control (RBAC)**: Secure multi-tier permissions for System Admin, Manager, Finance Officer, Support Agent, and Employee roles with dedicated dashboards.
* **Separation of Concerns for Billing & Claims**: Distinct processing tracks for `PolicyBills` (policy enrollment and finance invoicing) and `InsuranceClaims` (medical claims against active policies).
* **Policy Coverage Enforcement**: Automated real-time calculations ensuring claims do not exceed available policy coverage limits (`Policy.CoverageAmount`).
* **Empanelled Hospital Directory**: Searchable directory linking policies to network hospitals for streamlined healthcare access.
* **Interactive Self-Service Portal**: Employee dashboard to discover policies, track policy approval status, calculate coverage limits, and submit claims.
* **Support Ticket & FAQ Hub**: Integrated help desk supporting user inquiries, ticket assignment, and structured FAQ lookup.
* **Date-Range Analytics & Reporting**: Exportable operational reports covering policy approvals, active enrolments, billing statuses, and claim payouts.
* **Security & Passwords**: BCrypt password hashing, session guards, and token-based password reset options.

</details>

---

## System Architecture

```
+-----------------------------------------------------------------------------------+
|                                  BROWSER CLIENT                                   |
|                          (Bootstrap 5.3 + HTML5 + CSS3)                          |
+-----------------------------------------------------------------------------------+
                                          |
                                   HTTP / HTTPS
                                          v
+-----------------------------------------------------------------------------------+
|                            ASP.NET CORE 8.0 MVC ENGINE                            |
|                                                                                   |
|  [ Controllers Layer ]                                                            |
|  ├── AdminController.cs      ├── AuthController.cs      ├── EmployeeController.cs  |
|  ├── ManagerController.cs    ├── FinanceController.cs   ├── SupportController.cs   |
|  ├── HomeController.cs       └── ReportController.cs                             |
|                                                                                   |
|  [ Business & Service Layer ]                                                     |
|  ├── Custom Session Guard Filters                                                |
|  └── EmailNotificationService / BCrypt Password Hashing                           |
|                                                                                   |
|  [ Data Access Layer ]                                                            |
|  └── ApplicationDbContext (Entity Framework Core 8)                               |
+-----------------------------------------------------------------------------------+
                                          |
                                    EF CORE ORM
                                          v
+-----------------------------------------------------------------------------------+
|                                DATABASE STORAGE                                   |
|             (MySQL / MariaDB via XAMPP OR Microsoft SQL Server)                   |
+-----------------------------------------------------------------------------------+
```

---

## Role-Based Access Control

The application enforces role-based access control across five primary operational roles:

| Role | Primary Responsibilities & Permissions | Access Scope |
| :--- | :--- | :--- |
| **Admin** | System administration, company onboarding, policy creation, employee enrollment management, hospital listings. | Full Administrative Portal (`/Admin/*`) |
| **Manager** | Strategic policy approval, employee request reviews, claim initial approval/rejection workflows. | Management Dashboard (`/Manager/*`) |
| **Finance** | Invoice billing management, policy payment settlements, claim disbursement processing. | Finance Portal (`/Finance/*`) |
| **Support** | Ticket triage, user query resolution, FAQ management, customer service interaction. | Support Desk Portal (`/Support/*`) |
| **Employee** | Self-service profile, policy searching, policy request submission, live claim filing and tracking. | Employee Portal (`/Employee/*`) |

---

## Data Models and Database Schema

The database consists of 14 core entity tables defined in `Data/ApplicationDbContext.cs`:

<details>
<summary><strong>Expand Database Table Matrix</strong></summary>

<br />

| Table Name | Entity Class | Description |
| :--- | :--- | :--- |
| `AdminLogins` | `AdminLogin` | System administrative and elevated role login credentials. |
| `CompanyDetails` | `CompanyDetails` | Registered insurance provider companies and partners. |
| `EmpRegisters` | `EmpRegister` | Enterprise employee user records and authentication data. |
| `HospitalInfos` | `HospitalInfo` | Empanelled hospital network details linked to locations. |
| `Policies` | `Policy` | Policy definitions, coverage amounts, premiums, and durations. |
| `PolicyOnEmployees` | `PolicyOnEmployee` | Active policies assigned and mapped to individual employees. |
| `PolicyApprovalDetails` | `PolicyApprovalDetails` | Audit records for admin and manager approval/rejection decisions. |
| `PolicyRequestDetails` | `PolicyRequestDetails` | Employee policy enrollment requests waiting for review. |
| `PolicyTotalDescriptions`| `PolicyTotalDescription` | Extended terms, conditions, and coverage breakdowns per policy. |
| `PolicyBills` | `PolicyBill` | Policy enrolment billing records and payment statuses. |
| `InsuranceClaims` | `InsuranceClaim` | Medical claim filings submitted against assigned active policies. |
| `ContactQueries` | `ContactQuery` | Support queries and help desk communication records. |
| `FaqItems` | `FaqItem` | System FAQ items managed by support and admin staff. |
| `EmailNotificationLogs` | `EmailNotificationLog` | Audit log of outgoing notifications and email dispatches. |

</details>

---

## Core Workflow Lifecycles

### 1. Policy Request & Billing Lifecycle

```
[Employee] Request Policy ---> [Admin/Manager] Review Request ---> [System] Generate PolicyBill
                                         |                                     |
                                   (If Approved)                       (Finance Settlement)
                                         v                                     v
                           [PolicyOnEmployee Created] <--- [Payment Status: Paid]
```

1. **Request Submission**: An employee explores available policies and submits an enrollment request.
2. **Review & Approval**: An Admin or Manager reviews the request (`PolicyRequestDetails`).
3. **Assignment & Invoicing**: Upon approval, the policy is linked to the employee (`PolicyOnEmployee`) and an automated invoice (`PolicyBill`) is created.
4. **Finance Settlement**: Finance processes payment, updating the billing record to `Paid`.

### 2. Insurance Claims Lifecycle

```
[Employee] Submit Claim ---> [Manager] Review & Approve ---> [Finance] Disburse Funds ---> [System] Deduct Coverage
```

1. **Claim Filing**: Employee selects an active assigned policy (`PolicyOnEmployee`) and enters the claim amount and medical documentation details.
2. **Coverage Limit Check**: System validates that `Claim Amount <= (Policy.CoverageAmount - Sum of Settled Claims)`.
3. **Manager Review**: Manager inspects claims evidence and approves or rejects the submission.
4. **Finance Disburse & Close**: Finance processes the claim payout, marking the claim as `Paid/Closed`. Remaining coverage is updated in real-time.

---

## Module Breakdown

<details>
<summary><strong>Expand Module & Controller Reference</strong></summary>

<br />

* **Authentication Module (`AuthController.cs`)**:
  * Role-based user login (`Admin` and `Employee`).
  * BCrypt password validation.
  * Password reset workflow via `PasswordResetToken`.
  * Clean session termination on logout.

* **Administration Module (`AdminController.cs`)**:
  * Dashboard displaying active metrics across companies, policies, and employees.
  * Company management with soft delete support (`IsActive`).
  * Policy creation linked to specific insurance providers.
  * Empanelled hospital network management.
  * Direct assignment of policies to registered employees.

* **Employee Self-Service Portal (`EmployeeController.cs`)**:
  * Interactive employee dashboard.
  * Policy search and filter interface with detailed coverage breakdowns.
  * Policy enrollment request filing.
  * Insurance claim submission with real-time coverage validation.
  * Password update and profile management.

* **Manager Portal (`ManagerController.cs`)**:
  * Review panel for pending policy enrollment requests.
  * Approval and rejection workflows for medical claims.

* **Finance Portal (`FinanceController.cs`)**:
  * Management of policy billing invoices (`PolicyBills`).
  * Claim payment disbursement and financial record settlement.

* **Support & Help Desk (`SupportController.cs`)**:
  * Support inquiry submission for employees and public visitors.
  * Query status tracking and response management.
  * Knowledgebase FAQ listing.

* **Reporting & Analytics (`ReportController.cs`)**:
  * Date-range policy enrollment reports.
  * Claim disbursement summary reports.

</details>

---

## Environment Setup and Installation

### Prerequisites

* [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* One of the following database options:
  * **Option A**: XAMPP with MySQL / MariaDB (running on `localhost:3306`)
  * **Option B**: Microsoft SQL Server (LocalDB or SQL Express)
* Visual Studio Code or Visual Studio 2022 (with ASP.NET and web development workload)

---

### Option A: Setup with VS Code & XAMPP (MySQL)

1. **Clone the repository**:
   ```bash
   git clone https://github.com/your-repo/HealthInsuranceManagement.git
   cd HealthInsuranceManagement
   ```

2. **Configure Database Connection**:
   Open `appsettings.json` and verify the MySQL connection string:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "server=127.0.0.1;port=3306;database=healthinsurancemanagementdb;user=root;password=;SslMode=None"
   }
   ```

3. **Restore Dependencies**:
   ```bash
   dotnet restore
   ```

4. **Build and Run Application**:
   ```bash
   dotnet run
   ```
   *The database and required tables will be automatically created on initial launch.*

5. **Access Application**:
   Open your browser and navigate to `http://localhost:5000` (or `https://localhost:5001`).

---

### Option B: Setup with Visual Studio 2022 & SQL Server

1. **Open Solution**:
   Open `HealthInsuranceManagement.sln` in Visual Studio 2022.

2. **Configure Connection String**:
   In `appsettings.json`, set `DefaultConnection` to target SQL Server:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.\\SQLEXPRESS;Database=HealthInsuranceManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```

3. **Configure Database Provider**:
   * Open **NuGet Package Manager** and uninstall `Pomelo.EntityFrameworkCore.MySql`.
   * Install `Microsoft.EntityFrameworkCore.SqlServer`.
   * In `Program.cs`, configure SQL Server:
     ```csharp
     builder.Services.AddDbContext<ApplicationDbContext>(options =>
         options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
     ```

4. **Update Database via Migrations**:
   Open **Package Manager Console** (`Tools > NuGet Package Manager > Package Manager Console`) and run:
   ```powershell
   Add-Migration InitialCreate
   Update-Database
   ```

5. **Start Application**:
   Press `F5` or click **Start Debugging** in Visual Studio.

---

## Default System Credentials

| Role | Default Username | Default Password | Initial Access URL |
| :--- | :--- | :--- | :--- |
| **Admin** | `admin` | `Admin@123` | `/Auth/Login` |

> *Note: Employee accounts are created via the Admin portal under **Employees > Register Employee**.*

---

## Security Practices

* **Password Security**: Passwords are hashed using BCrypt (`BCrypt.Net-Next`) prior to storage.
* **Session Protection**: Custom guard filters prevent unauthorized URL access by checking role claims on every request.
* **Input Validation**: Strongly-typed model validation attributes prevent malformed data submissions.
* **Data Privacy**: Soft deletion flags (`IsActive`) preserve audit trails without discarding historic records.

---

## Contributing

Contributions are welcome. Please read our [CONTRIBUTING.md](CONTRIBUTING.md) guide for details on our code of conduct, development workflow, and pull request submissions.

---

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for complete terms.
