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

