# Cloudflare Tunnel Setup Guide

This guide explains how to expose your local Health Insurance Management System (ASP.NET Core) to a temporary live URL using a Cloudflare Tunnel.

---

## Prerequisites
Ensure that your database is running. Since the project connects to a local MySQL/MariaDB database (configured for XAMPP on port `3306`), make sure **XAMPP / MySQL** is running.

---

## Step-by-Step Instructions

### Step 1: Install Cloudflare Tunnel (`cloudflared`)
Since you are on Windows, you can install the official Cloudflare CLI tool directly from PowerShell:

1. Open PowerShell or Command Prompt.
2. Run the following command:
   ```powershell
   winget install Cloudflare.cloudflared
   ```
3. **Restart your terminal** after the installation finishes so that the newly added path takes effect.

*(Alternatively, you can manually download the installer here: [cloudflared-windows-amd64.msi](https://github.com/cloudflare/cloudflared/releases/latest/download/cloudflared-windows-amd64.msi))*

---

### Step 2: Run Your ASP.NET Core Project
1. Open a terminal in the root folder of your project:
   ```text
   C:\Users\Fahad\OneDrive\Videos\HealthInsuranceManagement
   ```
2. Start the local server by running:
   ```powershell
   dotnet run
   ```
The application runs locally on:
* **`http://localhost:5000`**

---

### Step 3: Start the Temporary Tunnel
Once your project is running locally:

1. Open a **new, separate terminal window** (do not close the one running `dotnet run`).
2. Start a **Quick Tunnel** pointing to your local port `5000` by running:
   ```powershell
   cloudflared tunnel --url http://localhost:5000
   ```

---

### Step 4: Access Your Live Link
As soon as the tunnel starts, look closely at the terminal output. You will see a block of logs, including a section that looks like this:

```text
+-------------------------------------------------------------+
|  Your quick tunnel has been created! Visit it at:           |
|  https://some-random-words.trycloudflare.com                |
+-------------------------------------------------------------+
```

* **Copy the URL** (e.g., `https://*.trycloudflare.com`) and open it in any browser or share it with anyone. They will be securely connected to your local server running on this PC.
* When you are done, simply press **`Ctrl + C`** in both terminals to stop the tunnel and turn off the server.
