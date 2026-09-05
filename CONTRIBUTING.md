# Contributing to Health Insurance Management System

Thank you for taking the time to contribute to the Health Insurance Management System project. This document outlines the standards, guidelines, and procedures for contributing.

---

## Table of Contents
1. Code of Conduct
2. How to Contribute
3. Development Environment Setup
4. Branching and Git Commit Conventions
5. Pull Request Process
6. Coding Standards and Guidelines
7. Reporting Issues

---

## Code of Conduct

All contributors are expected to maintain a professional, respectful, and collaborative environment. Be respectful of differing opinions, accept constructive feedback gracefully, and focus on delivering high-quality, secure code.

---

## How to Contribute

Contributions to this repository can come in various forms:
- Reporting bugs or security vulnerabilities
- Proposing new features or workflow enhancements
- Improving documentation and code comments
- Submitting bug fixes or feature implementation pull requests

---

## Development Environment Setup

To set up the development environment locally:

1. Clone the repository:
   ```bash
   git clone https://github.com/your-repo/HealthInsuranceManagement.git
   cd HealthInsuranceManagement
   ```

2. Ensure .NET 8 SDK is installed on your environment:
   ```bash
   dotnet --version
   ```

3. Restore NuGet dependencies:
   ```bash
   dotnet restore
   ```

4. Configure your database connection string in `appsettings.json`:
   - MySQL / MariaDB (Pomelo EF Core Provider)
   - SQL Server (Microsoft EF Core Provider)

5. Run database migrations and launch the project:
   ```bash
   dotnet run
   ```

---

## Branching and Git Commit Conventions

### Branch Naming Conventions
- Feature branches: `feature/short-description`
- Bug fix branches: `fix/short-description`
- Documentation branches: `docs/short-description`
- Refactoring branches: `refactor/short-description`

### Commit Message Guidelines
Follow standardized commit messages:
- Use imperative mood in the subject line (e.g., "Add policy status tracking" instead of "Added policy status tracking").
- Keep subject lines under 50 characters.
- Capitalize the subject line.
- Do not end the subject line with a period.
- Separate subject from body with a blank line when providing extended descriptions.

---

## Pull Request Process

1. Create a dedicated topic branch from `main`.
2. Ensure code compiles cleanly without errors or warnings.
3. Write or update relevant unit/integration tests where applicable.
4. Verify all tests pass before requesting review.
5. Push your topic branch to the remote repository.
6. Open a Pull Request (PR) against the `main` branch with a clear summary of changes, rationale, and testing evidence.
7. Address any code review feedback promptly.

---

## Coding Standards and Guidelines

### C# and .NET Conventions
- Follow standard C# naming conventions: PascalCase for public properties, methods, and class names; camelCase for private fields and local variables.
- Maintain strong encapsulation and validation logic in Controller actions and Models.
- Use explicit session guard checks for role-based access control.
- Ensure proper password hashing (e.g., BCrypt) for all authentication workflows.
- Keep business logic structured within Controller or Service layers.

### UI and Frontend Conventions
- Follow Bootstrap 5 grid layout standards.
- Ensure full responsiveness across standard mobile, tablet, and desktop viewports.
- Maintain clean markup separation in ASP.NET Core Razor Views (`.cshtml`).

---

## Reporting Issues

When submitting an issue report, please include:
- A concise summary of the problem or feature request.
- Detailed step-by-step instructions to reproduce the issue.
- Expected behavior versus actual behavior observed.
- Environment details (OS, .NET version, database engine, browser version).
- Relevant stack traces or logs, where applicable.

---

## License

By contributing to this repository, you agree that your contributions will be licensed under the MIT License accompanying this project.
