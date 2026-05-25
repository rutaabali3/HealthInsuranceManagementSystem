# Health Insurance Management System

## Tech Stack
This application is built on a robust and modern technology stack and can be set up in different environments depending on your preference:
- **Framework**: ASP.NET Core MVC 8
- **ORM**: Entity Framework Core 8
- **Frontend**: HTML5, CSS3, Bootstrap 5.3, Bootstrap Icons
- **Supported Databases**: MySQL / MariaDB (via XAMPP) **OR** Microsoft SQL Server
- **Supported IDEs**: Visual Studio Code **OR** Visual Studio 2022

---

## Project Structure

```
HealthInsuranceManagement/
├── Controllers/                   # Contains application logic and routing
│   ├── HomeController.cs          # Public pages (Home, About, Contact)
│   ├── AuthController.cs          # Login / Logout (Admin & Employee)
│   ├── AdminController.cs         # All admin operations
│   ├── EmployeeController.cs      # Employee self-service portal
│   └── ReportController.cs        # Date-range reports
│
├── Data/                          # Database context and configurations
│   └── ApplicationDbContext.cs    # EF Core DbContext (all 9 tables)
│
├── Models/                        # Data entities and view models
│   ├── AdminLogin.cs              # Table 1
│   ├── CompanyDetails.cs          # Table 2
│   ├── EmpRegister.cs             # Table 3
│   ├── HospitalInfo.cs            # Table 4
│   ├── Policy.cs                  # Table 5
│   ├── PolicyOnEmployee.cs        # Table 6
│   ├── PolicyApprovalDetails.cs   # Table 7
│   ├── PolicyRequestDetails.cs    # Table 8
│   ├── PolicyTotalDescription.cs  # Table 9
│   └── ViewModels/ViewModels.cs   # All view-model classes
│
├── Migrations/                    # EF Core database migrations
│
├── Views/                         # Razor views for the UI
│   ├── Shared/_Layout.cshtml      # Master layout with navbar
│   ├── Home/                      # Index, About, Contact
│   ├── Auth/                      # Login
│   ├── Admin/                     # Dashboard, Companies, Policies, Employees, PolicyRequests
│   ├── Employee/                  # Dashboard, Details, SearchPolicy, PolicyDetails, OrderInsurance, ChangePassword, UpdateDetails
│   └── Report/                    # Index (date-range report)
│
├── wwwroot/                       # Static files (CSS, JS, images)
│   └── css/site.css               # Custom styles
│
├── appsettings.json               # Application configuration & DB connection string
├── Program.cs                     # App startup and service registration
├── HealthInsuranceManagement.sln  # Visual Studio Solution File
└── HealthInsuranceManagement.csproj # Project File
```

---

## Modules Implemented

| Module | Features |
|--------|----------|
| **Authentication** | Role-based login (Admin / Employee), BCrypt password hashing, session management |
| **Administration** | Dashboard with live stats, manage insurance companies, manage policies |
| **Employee Support** | Register employees with credentials, edit details, assign policies directly |
| **Policy Requests** | Employee submits request → Admin approves/rejects → auto policy assignment |
| **Employee Portal** | Self-service: view profile, change password, search & filter policies, request policy |
| **Reports** | Date-range reports for employees and policy requests |

---

## Database Tables

All 9 tables from the specification:

1. `AdminLogins` – Admin credentials
2. `CompanyDetails` – Insurance companies
3. `EmpRegisters` – Employee records & credentials
4. `HospitalInfos` – Empanelled hospitals
5. `Policies` – Insurance policies (linked to companies)
6. `PolicyOnEmployees` – Policies assigned to employees
7. `PolicyApprovalDetails` – Admin approval/rejection records
8. `PolicyRequestDetails` – Employee policy requests
9. `PolicyTotalDescriptions` – Coverage breakdown per policy

---

## Setup Instructions

This project can be run using either **VS Code with XAMPP (MySQL)** or **Visual Studio 2022 with SQL Server**. Choose the option that fits your environment.

### Option 1: Setup with VS Code & XAMPP (MySQL)

**Prerequisites:**
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8)
- XAMPP with MySQL / MariaDB running on `localhost:3306`
- Visual Studio Code

**Steps:**
1. **Clone or open the project** in VS Code:
   ```bash
   cd HealthInsuranceManagement
   ```
2. **Confirm the MySQL connection string** in `appsettings.json`. By default, the project is configured for MySQL:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "server=127.0.0.1;port=3306;database=healthinsurancemanagementdb;user=root;password=;SslMode=None"
   }
   ```
   > *Note: If your XAMPP `root` user has a password, add it to the `password=` value.*
3. **Restore packages**:
   ```bash
   dotnet restore
   ```
4. **Run the application**:
   ```bash
   dotnet run
   ```
   *The app creates the database and tables automatically on first startup.*
5. **Access the application**: Open your browser and navigate to `http://localhost:5000` (or the port specified in the console).

### Option 2: Setup with Visual Studio 2022 & SQL Server

**Prerequisites:**
- Visual Studio 2022 (with "ASP.NET and web development" workload)
- Microsoft SQL Server (e.g., SQL Server Express)

**Steps:**
1. **Open the Solution**: Double-click `HealthInsuranceManagement.sln` to open the project in Visual Studio 2022.
2. **Update Connection String**: Open `appsettings.json` and change the `DefaultConnection` to your SQL Server instance:
   ```json
   "ConnectionStrings": {
     "DefaultConnection": "Server=.\\SQLEXPRESS;Database=HealthInsuranceManagementDB;Trusted_Connection=True;TrustServerCertificate=True;"
   }
   ```
3. **Switch Database Provider** (from MySQL to SQL Server):
   - Open **NuGet Package Manager** and uninstall `Pomelo.EntityFrameworkCore.MySql`.
   - Install `Microsoft.EntityFrameworkCore.SqlServer`.
   - In `Program.cs`, replace the MySQL configuration line with:
     ```csharp
     builder.Services.AddDbContext<ApplicationDbContext>(options =>
         options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));
     ```
4. **Recreate Migrations** (Optional but recommended): 
   - Delete the existing `Migrations` folder in the project.
   - Open **Package Manager Console** (Tools > NuGet Package Manager > Package Manager Console).
   - Run the following commands to generate SQL Server specific migrations:
     ```powershell
     Add-Migration InitialCreate
     Update-Database
     ```
5. **Run the application**: Press `F5` or click the "Start" button in Visual Studio to run the project.

---

## Default Credentials

| Role | Username | Password |
|------|----------|----------|
| Admin | `admin` | `Admin@123` |

> To create employee accounts: Log in as Admin → Employees → Register Employee.

---

## Key Design Decisions

- **BCrypt** password hashing (BCrypt.Net-Next v4)
- **Session-based auth** (no Identity framework — kept lean per spec)
- **EF Core 8** setup (Supports both MySQL via Pomelo and SQL Server)
- **Bootstrap 5.3 + Bootstrap Icons** for UI (CDN, no build step)
- **Guard pattern** in controllers — every action checks session role
- **Soft delete** for Companies (IsActive flag), not hard delete
- **Auto policy assignment** when admin approves a request
