# FlowManager Dependencies

> **Note**: This is a **.NET 9.0 application** (C#), not a Python application. It uses **NuGet packages** instead of `requirements.txt`.

---

## 🎯 Runtime Requirements

### .NET Runtime
- **Framework**: .NET 9.0
- **SDK Download**: https://dotnet.microsoft.com/download/dotnet/9.0
- **Minimum Version**: 9.0.0

**Check installed version:**
```bash
dotnet --version
```

**Expected output:**
```
9.0.xxx
```

---

## 📦 NuGet Package Dependencies

### FlowManager.Core

Core business logic and flow execution engine.

| Package | Version | Purpose |
|---------|---------|---------|
| **Newtonsoft.Json** | 13.0.4 | JSON serialization/deserialization for flows, nodes, and configuration |

**Project File:** `FlowManager.Core\FlowManager.Core.csproj`

```xml
<ItemGroup>
  <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
</ItemGroup>
```

---

### FlowManager.Web

ASP.NET Core Blazor Server web application.

| Package | Version | Purpose |
|---------|---------|---------|
| **Microsoft.AspNetCore.App** | 9.0.0 (implicit) | ASP.NET Core framework |
| **Microsoft.NET.Sdk.Web** | 9.0.0 (implicit) | Web SDK for Razor, Blazor |

**Project File:** `FlowManager.Web\FlowManager.Web.csproj`

**Dependencies:**
- Project reference to `FlowManager.Core`
- All ASP.NET Core 9.0 framework packages (included automatically)

**Implicit Packages (provided by SDK):**
- Microsoft.AspNetCore.Components
- Microsoft.AspNetCore.Components.Web
- Microsoft.AspNetCore.Components.Server
- Microsoft.Extensions.Configuration
- Microsoft.Extensions.DependencyInjection
- Microsoft.Extensions.Logging

---

### FlowManager.Tests.Manual

Manual testing project for flow execution.

| Package | Version | Purpose |
|---------|---------|---------|
| **OpenAI** | 2.1.0 | Official OpenAI .NET SDK for AI integrations |

**Project File:** `FlowManager.Tests.Manual\FlowManager.Tests.Manual.csproj`

```xml
<ItemGroup>
  <PackageReference Include="OpenAI" Version="2.1.0" />
</ItemGroup>
```

---

## 📋 Complete Dependency List

### Direct NuGet Dependencies

```
FlowManager.Core:
  └── Newtonsoft.Json (13.0.4)

FlowManager.Web:
  └── [No direct NuGet packages - uses .NET SDK packages]
  └── Project Reference: FlowManager.Core

FlowManager.Tests.Manual:
  └── OpenAI (2.1.0)
  └── Project Reference: FlowManager.Core
```

---

## 🔧 Installation & Setup

### 1. Install .NET 9.0 SDK

**Windows:**
```powershell
# Download and install from:
https://dotnet.microsoft.com/download/dotnet/9.0

# Or use winget:
winget install Microsoft.DotNet.SDK.9
```

**macOS:**
```bash
# Using Homebrew:
brew install dotnet-sdk

# Or download from:
https://dotnet.microsoft.com/download/dotnet/9.0
```

**Linux (Ubuntu/Debian):**
```bash
wget https://dot.net/v1/dotnet-install.sh
chmod +x dotnet-install.sh
./dotnet-install.sh --channel 9.0

# Or use package manager:
sudo apt-get update
sudo apt-get install -y dotnet-sdk-9.0
```

---

### 2. Restore NuGet Packages

**Automatic Restore (Recommended):**
```bash
cd "FlowManager"
dotnet restore
```

**This command:**
- Downloads all NuGet packages specified in `.csproj` files
- Creates `obj/` folders with package metadata
- Restores packages for all projects in solution

**Manual Restore (if needed):**
```bash
# Restore specific project
dotnet restore FlowManager.Core/FlowManager.Core.csproj
dotnet restore FlowManager.Web/FlowManager.Web.csproj
dotnet restore FlowManager.Tests.Manual/FlowManager.Tests.Manual.csproj
```

---

### 3. Build the Application

```bash
cd "FlowManager.Web"
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

---

### 4. Run the Application

```bash
cd "FlowManager.Web"
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:5001
      Application started. Press Ctrl+C to shut down.
```

---

## 🐍 Python Equivalent (For Reference)

If this were a Python application, the equivalent `requirements.txt` would be:

```txt
# Python equivalent (REFERENCE ONLY - NOT USED BY THIS APP)
# This app uses .NET, not Python

# Core dependencies
newtonsoft-json==13.0.4  # → Newtonsoft.Json 13.0.4
openai==2.1.0            # → OpenAI 2.1.0

# Framework (ASP.NET Core equivalent)
# flask==3.0.0           # → ASP.NET Core 9.0
# jinja2==3.1.0          # → Razor Pages
```

**However, this is a .NET app, so use `dotnet restore` instead of `pip install -r requirements.txt`**

---

## 📊 Dependency Tree (Transitive)

### Newtonsoft.Json 13.0.4
```
Newtonsoft.Json (13.0.4)
  └── No additional dependencies
```

### OpenAI 2.1.0
```
OpenAI (2.1.0)
  ├── Microsoft.Bcl.AsyncInterfaces (>= 8.0.0)
  ├── System.Text.Json (>= 8.0.0)
  └── System.Net.Http.Json (>= 8.0.0)
```

### ASP.NET Core 9.0 (Implicit)
```
Microsoft.AspNetCore.App (9.0.0)
  ├── Microsoft.AspNetCore.Components (9.0.0)
  ├── Microsoft.AspNetCore.Components.Web (9.0.0)
  ├── Microsoft.AspNetCore.Components.Server (9.0.0)
  ├── Microsoft.Extensions.Configuration (9.0.0)
  ├── Microsoft.Extensions.DependencyInjection (9.0.0)
  ├── Microsoft.Extensions.Logging (9.0.0)
  └── [100+ framework packages]
```

---

## 🔍 Package Details

### Newtonsoft.Json 13.0.4

**Purpose:** JSON serialization and deserialization

**Used For:**
- Serializing/deserializing Flow definitions
- Reading/writing flow configuration files
- Parsing node outputs
- Converting objects to/from JSON

**Official Site:** https://www.newtonsoft.com/json

**NuGet:** https://www.nuget.org/packages/Newtonsoft.Json/

**License:** MIT

---

### OpenAI 2.1.0

**Purpose:** Official OpenAI .NET SDK

**Used For:**
- OpenAIChatNode integration
- Calling GPT-4, GPT-4o-mini models
- Embedding generation (if used)
- AI chat completions

**Official Site:** https://github.com/openai/openai-dotnet

**NuGet:** https://www.nuget.org/packages/OpenAI/

**License:** MIT

**API Key Required:** Yes (set in `appsettings.json`)

---

### Microsoft.AspNetCore.App (Framework)

**Purpose:** ASP.NET Core web framework

**Used For:**
- Blazor Server components
- Razor Pages
- Dependency injection
- Configuration system
- Logging
- HTTP server (Kestrel)

**Official Site:** https://dotnet.microsoft.com/apps/aspnet

**License:** MIT

---

## 🚀 Quick Start Commands

### Complete Setup (From Scratch)

```bash
# 1. Clone/navigate to project
cd "C:\Users\mushf\Desktop\LAST SATBLE VERSION\FlowManager"

# 2. Restore NuGet packages
dotnet restore

# 3. Build solution
dotnet build

# 4. Run web application
cd FlowManager.Web
dotnet run

# 5. Open browser
start https://localhost:5001
```

---

## 🔄 Update Dependencies

### Update All Packages to Latest

```bash
# Update all NuGet packages in solution
dotnet list package --outdated
dotnet add package Newtonsoft.Json
dotnet add package OpenAI
```

### Update Specific Package

```bash
# Update Newtonsoft.Json
cd FlowManager.Core
dotnet add package Newtonsoft.Json --version 13.0.4

# Update OpenAI SDK
cd ../FlowManager.Tests.Manual
dotnet add package OpenAI --version 2.1.0
```

---

## 🐛 Troubleshooting

### Issue 1: "SDK not found"

**Error:**
```
The SDK 'Microsoft.NET.Sdk.Web' specified could not be found.
```

**Solution:**
```bash
# Install .NET 9.0 SDK
dotnet --version  # Check current version
# Download from: https://dotnet.microsoft.com/download/dotnet/9.0
```

---

### Issue 2: "Package restore failed"

**Error:**
```
error NU1102: Unable to find package 'Newtonsoft.Json'
```

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore
```

---

### Issue 3: "Version conflict"

**Error:**
```
error NU1107: Version conflict detected
```

**Solution:**
```bash
# Clean solution
dotnet clean

# Remove bin/obj folders
rm -rf **/bin **/obj

# Restore fresh
dotnet restore
```

---

## 📦 Package Manager Equivalents

| .NET | Python | Node.js | Ruby |
|------|--------|---------|------|
| `dotnet restore` | `pip install -r requirements.txt` | `npm install` | `bundle install` |
| `.csproj` | `requirements.txt` | `package.json` | `Gemfile` |
| NuGet | PyPI | npm | RubyGems |
| `packages.config` | `requirements.txt` | `package-lock.json` | `Gemfile.lock` |

---

## 📁 Configuration Files

### FlowManager.Core.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Newtonsoft.Json" Version="13.0.4" />
  </ItemGroup>
</Project>
```

### FlowManager.Web.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk.Web">
  <ItemGroup>
    <ProjectReference Include="..\FlowManager.Core\FlowManager.Core.csproj" />
  </ItemGroup>

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
  </PropertyGroup>
</Project>
```

### FlowManager.Tests.Manual.csproj
```xml
<Project Sdk="Microsoft.NET.Sdk">
  <ItemGroup>
    <ProjectReference Include="..\FlowManager.Core\FlowManager.Core.csproj" />
  </ItemGroup>

  <ItemGroup>
    <PackageReference Include="OpenAI" Version="2.1.0" />
  </ItemGroup>

  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net9.0</TargetFramework>
    <ImplicitUsings>enable</ImplicitUsings>
    <Nullable>enable</Nullable>
  </PropertyGroup>
</Project>
```

---

## 🎓 Summary

**Runtime:** .NET 9.0 SDK

**Direct Dependencies:**
- Newtonsoft.Json 13.0.4
- OpenAI 2.1.0

**Framework:** ASP.NET Core 9.0 (Blazor Server)

**Installation:**
```bash
dotnet restore  # Equivalent to: pip install -r requirements.txt
dotnet build    # Equivalent to: python setup.py build
dotnet run      # Equivalent to: python app.py
```

**Package Management:**
- **Tool**: NuGet Package Manager
- **Config**: `.csproj` files (XML-based)
- **Registry**: https://www.nuget.org/
- **Cache**: `~/.nuget/packages/`

---

## 📞 Support

**Official Documentation:**
- .NET: https://docs.microsoft.com/dotnet/
- NuGet: https://docs.microsoft.com/nuget/
- ASP.NET Core: https://docs.microsoft.com/aspnet/core/

**Package Registries:**
- NuGet Gallery: https://www.nuget.org/
- OpenAI .NET: https://github.com/openai/openai-dotnet
- Newtonsoft.Json: https://www.newtonsoft.com/json

---

**Last Updated:** 2025-01-17
