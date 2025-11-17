# FlowManager User Guide
## AI Orchestration & DAG Execution Engine

**Version:** 1.0
**Last Updated:** November 2025
**Difficulty:** Beginner to Advanced

---

## Table of Contents

1. [Prerequisites & Installation](#prerequisites--installation)
2. [Introduction](#introduction)
3. [Core Concepts](#core-concepts)
4. [Quick Start](#quick-start)
5. [Creating Your First Flow](#creating-your-first-flow)
6. [Working with Branches](#working-with-branches)
7. [Conditional Execution](#conditional-execution)
8. [Parallel Execution](#parallel-execution)
9. [Available Node Types](#available-node-types)
10. [Advanced Flow Patterns](#advanced-flow-patterns)
11. [Best Practices](#best-practices)
12. [Troubleshooting](#troubleshooting)

---

## Prerequisites & Installation

### System Requirements

Before you begin, ensure your system meets these requirements:

| Requirement | Specification |
|------------|---------------|
| **Operating System** | Windows 10/11, macOS 10.15+, Ubuntu 20.04+, or any Linux distro |
| **RAM** | 4 GB minimum (8 GB recommended) |
| **Disk Space** | 500 MB for .NET SDK + 100 MB for application |
| **Internet Connection** | Required for OpenAI API calls |

---

### Step 1: Install .NET 9.0 SDK

FlowManager is a **.NET 9.0 application**. You must install the .NET SDK first.

#### **Windows Installation**

**Option A: Download Installer (Recommended)**

1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0
2. Download **".NET 9.0 SDK"** (not just Runtime)
3. Run the installer (.exe file)
4. Follow the installation wizard
5. Restart your terminal/command prompt

**Option B: Using winget**

```powershell
# Open PowerShell as Administrator
winget install Microsoft.DotNet.SDK.9
```

**Option C: Using Chocolatey**

```powershell
choco install dotnet-sdk
```

#### **macOS Installation**

**Option A: Download Installer**

1. Visit: https://dotnet.microsoft.com/download/dotnet/9.0
2. Download **".NET 9.0 SDK"** for macOS
3. Open the .pkg file and install
4. Restart Terminal

**Option B: Using Homebrew**

```bash
brew install dotnet-sdk
```

#### **Linux Installation (Ubuntu/Debian)**

```bash
# Add Microsoft package repository
wget https://packages.microsoft.com/config/ubuntu/$(lsb_release -rs)/packages-microsoft-prod.deb -O packages-microsoft-prod.deb
sudo dpkg -i packages-microsoft-prod.deb
rm packages-microsoft-prod.deb

# Update package list
sudo apt-get update

# Install .NET SDK 9.0
sudo apt-get install -y dotnet-sdk-9.0
```

#### **Linux Installation (Fedora/RHEL/CentOS)**

```bash
# Add Microsoft repository
sudo dnf install dotnet-sdk-9.0
```

#### **Verify Installation**

After installation, open a **new** terminal/command prompt and run:

```bash
dotnet --version
```

**Expected output:**
```
9.0.xxx
```

If you see version 9.0 or higher, you're ready to proceed! ✅

---

### Step 2: Get FlowManager

#### **Option A: Download ZIP (Easiest)**

1. Download the project ZIP file
2. Extract to a folder (e.g., `C:\FlowManager` or `~/FlowManager`)
3. Open terminal and navigate to the extracted folder

#### **Option B: Clone from Git**

If the project is in a Git repository:

```bash
git clone <repository-url>
cd FlowManager
```

---

### Step 3: Get an OpenAI API Key

FlowManager uses OpenAI's GPT models for AI processing. You need an API key.

1. **Sign up** at https://platform.openai.com/signup
2. **Navigate** to https://platform.openai.com/api-keys
3. **Click** "Create new secret key"
4. **Copy** the key (starts with `sk-...`)
5. **Keep it safe** - you'll need it in the next step!

**Cost:** OpenAI charges per API call. GPT-4o-mini is very affordable (~$0.15 per 1M tokens).

---

### Step 4: Configure API Key

Open `FlowManager.Web/appsettings.json` in a text editor:

**Location:**
```
FlowManager/FlowManager.Web/appsettings.json
```

**Find this section:**
```json
{
  "KnowledgeBase": {
    "OpenAIApiKey": "YOUR_API_KEY_HERE"
  }
}
```

**Replace `YOUR_API_KEY_HERE` with your actual OpenAI API key:**
```json
{
  "KnowledgeBase": {
    "OpenAIApiKey": "sk-proj-abc123..."
  }
}
```

**Save the file.**

⚠️ **Security Note:** Never commit `appsettings.json` with your API key to Git! Use environment variables or user secrets for production.

---

### Step 5: Restore Dependencies

FlowManager uses NuGet packages. Restore them with:

```bash
# Navigate to project root
cd FlowManager

# Restore all dependencies
dotnet restore
```

**Expected output:**
```
Restore succeeded.
```

**What this does:**
- Downloads `Newtonsoft.Json` (for JSON handling)
- Downloads `OpenAI` SDK (for AI integration)
- Downloads all ASP.NET Core dependencies

---

### Step 6: Build the Application

Compile the project to ensure everything is set up correctly:

```bash
dotnet build
```

**Expected output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)
```

If you see **errors**, check:
- ✅ .NET 9.0 SDK is installed
- ✅ You ran `dotnet restore`
- ✅ All files were extracted/cloned correctly

---

### Step 7: Run the Application

Start the web server:

```bash
# Navigate to the web project
cd FlowManager.Web

# Run the application
dotnet run
```

**Expected output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: https://localhost:port number
      Now listening on: http://localhost:port number
      Application started. Press Ctrl+C to shut down.
```

---

### Step 8: Open in Browser

1. **Open your web browser**
2. **Navigate to:** https://localhost:portnumber (indicated in the terminal)
3. **Accept the self-signed certificate warning** (it's safe - this is local development)

**You should see the FlowManager interface!** 

---

### Step 9: Verify Installation

#### **Test 1: Check Flows Page**

1. Click **"Flows"** in the navigation
2. You should see **3 sample flows**:
   - Advanced Code Review System
   - Spam and Router Flow
   - Flow 52d51e69 Fixed

If you see these, your installation is working! 

#### **Test 2: Run a Simple Flow**

1. Click **"Run"** on aspam classifier flow
2. Enter some test input (e.g., "I got an email saying that I won the lottery, I just need to click it")
3. Click **"Execute Flow"**
4. You should see AI-generated results

If the flow executes successfully, everything is configured correctly! ✅

---

### Troubleshooting Installation

#### **Problem: "dotnet: command not found"**

**Cause:** .NET SDK not in system PATH

**Solution:**
1. Close and reopen terminal (PATH is updated on new sessions)
2. On Windows: Log out and log back in
3. Verify installation: Run .NET installer again

---

#### **Problem: "Build failed"**

**Error Example:**
```
error MSB4236: The SDK 'Microsoft.NET.Sdk.Web' specified could not be found.
```

**Solution:**
```bash
# Verify .NET version
dotnet --version

# Should be 9.0.xxx
# If not, reinstall .NET 9.0 SDK
```

---

#### **Problem: "Unable to find package 'Newtonsoft.Json'"**

**Cause:** NuGet package restore failed

**Solution:**
```bash
# Clear NuGet cache
dotnet nuget locals all --clear

# Restore again
dotnet restore

# Rebuild
dotnet build
```

---

#### **Problem: "OpenAI API error: Invalid API key"**

**Cause:** Incorrect or missing API key

**Solution:**
1. Check `appsettings.json` has your correct OpenAI key
2. Verify key starts with `sk-`
3. Test key at https://platform.openai.com/playground
4. Regenerate key if needed

---

#### **Problem: "Port 5001 already in use"**

**Cause:** Another application is using port 5001

**Solution:**

**Option A: Stop the other application**

**Option B: Change FlowManager's port**

Edit `FlowManager.Web/Properties/launchSettings.json`:

```json
{
  "applicationUrl": "https://localhost:6001;http://localhost:6000"
}
```

Then run again:
```bash
dotnet run
```

Now access at https://localhost:6001

---

### Quick Reference: Common Commands

```bash
# Restore dependencies
dotnet restore

# Build project
dotnet build

# Run application
cd FlowManager.Web
dotnet run

# Run on specific port
dotnet run --urls="https://localhost:6001"

# Clean build artifacts
dotnet clean

# Update dependencies
dotnet list package --outdated
dotnet add package Newtonsoft.Json

# Check .NET version
dotnet --version

# List installed SDKs
dotnet --list-sdks
```

---

### File Structure Overview

```
FlowManager/
├── FlowManager.Core/              # Core business logic
│   ├── Engine/                    # Flow execution engine
│   ├── Models/                    # Data models
│   ├── Nodes/                     # Node type implementations
│   └── Services/                  # Helper services
├── FlowManager.Web/               # Web application
│   ├── Components/                # Blazor components
│   ├── Data/
│   │   └── Flows/                 # Flow definitions (JSON files)
│   ├── appsettings.json           # Configuration ← API KEY HERE
│   └── Program.cs                 # Application entry point
├── FlowManager.Tests.Manual/      # Manual test project
├── USER_GUIDE.md                  # This file
├── ADVANCED_FLOW_CREATION_GUIDE.md # Advanced flow guide
└── DEPENDENCIES.md                # Dependency documentation
```

---

✅ **Installation complete!**

Now you can:

1. **Learn the basics** → Continue to [Introduction](#introduction)
2. **Create your first flow** → Jump to [Creating Your First Flow](#creating-your-first-flow)
3. **Explore examples** → Check `FlowManager.Web/Data/Flows/`
4. **Build advanced flows** → Read `ADVANCED_FLOW_CREATION_GUIDE.md`

---

## Introduction

**FlowManager** is a powerful AI orchestration engine that lets you build complex workflows using a **Directed Acyclic Graph (DAG)** architecture. Think of it as a visual programming language for AI:

- **Nodes** = Processing units (like functions)
- **Edges** = Connections between nodes (like function calls)
- **Flows** = Complete workflows (like programs)

### What Can You Build?

- ✅ Multi-department AI routing systems
- ✅ Conditional workflows based on AI decisions
- ✅ Parallel processing pipelines
- ✅ Complex decision trees
- ✅ Multi-stage data transformations
- ✅ Knowledge base search + AI response systems

---

## Core Concepts

### 1. Nodes

**Nodes** are the building blocks of your flow. Each node performs a specific task.

```
┌──────────────┐
│  Input Node  │  ← Takes user input
└──────┬───────┘
       │
       ▼
┌──────────────┐
│ OpenAI Chat  │  ← Processes with AI
└──────┬───────┘
       │
       ▼
┌──────────────┐
│   Formatter  │  ← Formats output
└──────────────┘
```

### 2. Edges

**Edges** connect nodes and define data flow.

```json
{
  "from": "input1",
  "to": "llm1",
  "fromOutput": "output",
  "toInput": "input",
  "condition": null
}
```

### 3. Conditions

**Conditions** control when edges or nodes execute.

```json
{
  "from": "orchestrator",
  "to": "tech_dept",
  "condition": "{{orchestrator.targets}} contains 'tech'"
}
```

### 4. Execution Flow

FlowManager uses **Kahn's Algorithm** to determine execution order:

1. **Find start nodes** (no incoming edges)
2. **Execute nodes** whose dependencies are satisfied
3. **Evaluate edge conditions** to determine next nodes
4. **Continue** until all reachable nodes complete

---

## Quick Start

### Installation

```bash
# Clone the repository
git clone https://github.com/yourorg/FlowManager.git

# Navigate to the project
cd FlowManager

# Restore dependencies
dotnet restore

# Run the web application
cd FlowManager.Web
dotnet run
```

### Your First Flow (Simple)

Create a file `my-first-flow.json`:

```json
{
  "id": "hello-flow",
  "name": "Hello World Flow",
  "description": "A simple greeting flow",
  "tenantId": "default",
  "nodes": [
    {
      "id": "input1",
      "type": "Input",
      "config": {
        "text": "{{userInput}}"
      },
      "condition": null
    },
    {
      "id": "greeter",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key-here",
        "model": "gpt-4o-mini",
        "temperature": 0.7,
        "systemPrompt": "You are a friendly assistant. Greet the user warmly."
      },
      "condition": null
    }
  ],
  "edges": [
    {
      "from": "input1",
      "to": "greeter",
      "fromOutput": "output",
      "toInput": "input",
      "condition": null
    }
  ]
}
```

**Execution Flow:**
```
User Input → Input Node → OpenAI Chat → Greeting Response
```

---

## Creating Your First Flow

### Step-by-Step Guide

#### Step 1: Define the Flow Structure

Every flow needs:
- **id**: Unique identifier
- **name**: Human-readable name
- **description**: What does this flow do?
- **tenantId**: Multi-tenancy support (use "default")
- **nodes**: Array of node definitions
- **edges**: Array of connections

#### Step 2: Add Nodes

Let's create a **spam detection** flow:

```json
{
  "id": "spam-detector",
  "name": "Spam Detection Flow",
  "description": "Detects and classifies spam messages",
  "tenantId": "default",
  "nodes": [
    {
      "id": "input1",
      "type": "Input",
      "config": {
        "text": "{{userInput}}"
      }
    },
    {
      "id": "spam_classifier",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "model": "gpt-4o-mini",
        "temperature": 0.2,
        "systemPrompt": "Analyze the message and return JSON: {\"classification\": \"SPAM\" or \"NOT_SPAM\", \"confidence\": 0-100, \"reason\": \"explanation\"}"
      }
    },
    {
      "id": "formatter",
      "type": "Formatter",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "Format the spam analysis into a user-friendly report."
      }
    }
  ]
}
```

#### Step 3: Connect Nodes with Edges

```json
{
  "edges": [
    {
      "from": "input1",
      "to": "spam_classifier",
      "fromOutput": "output",
      "toInput": "input"
    },
    {
      "from": "spam_classifier",
      "to": "formatter",
      "fromOutput": "output",
      "toInput": "spam_analysis"
    }
  ]
}
```

**Execution Flow:**
```
Input → Spam Classifier → Formatter → Result
```

---

## Working with Branches

Branches allow your flow to take different paths based on conditions.

### Example: Department Router with Branches

```json
{
  "id": "department-router",
  "name": "Multi-Department Router",
  "description": "Routes to different departments based on user query",
  "nodes": [
    {
      "id": "input1",
      "type": "Input",
      "config": { "text": "{{userInput}}" }
    },
    {
      "id": "orchestrator",
      "type": "Orchestrator",
      "config": {
        "apiKey": "your-api-key",
        "model": "gpt-4o-mini",
        "systemPrompt": "Route to departments: 'tech', 'billing', 'sales', 'support'. Return JSON: {\"Action\": \"DepartmentHandoff\", \"Pipeline\": [{\"Target\": \"tech\"}]}"
      }
    },
    {
      "id": "tech_dept",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "You are technical support. Provide troubleshooting steps."
      }
    },
    {
      "id": "billing_dept",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "You are billing support. Help with payment issues."
      }
    },
    {
      "id": "sales_dept",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "You are sales. Help with product inquiries."
      }
    },
    {
      "id": "formatter",
      "type": "Formatter",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "Combine all department responses into one coherent answer."
      }
    }
  ],
  "edges": [
    {
      "from": "input1",
      "to": "orchestrator"
    },
    {
      "from": "orchestrator",
      "to": "tech_dept",
      "condition": "{{orchestrator.targets}} contains 'tech'"
    },
    {
      "from": "orchestrator",
      "to": "billing_dept",
      "condition": "{{orchestrator.targets}} contains 'billing'"
    },
    {
      "from": "orchestrator",
      "to": "sales_dept",
      "condition": "{{orchestrator.targets}} contains 'sales'"
    },
    {
      "from": "tech_dept",
      "to": "formatter",
      "toInput": "tech_response"
    },
    {
      "from": "billing_dept",
      "to": "formatter",
      "toInput": "billing_response"
    },
    {
      "from": "sales_dept",
      "to": "formatter",
      "toInput": "sales_response"
    }
  ]
}
```

### Visualization

```
                          ┌──────────────┐
                          │   Input1     │
                          └──────┬───────┘
                                 │
                                 ▼
                          ┌──────────────┐
                          │ Orchestrator │
                          └──────┬───────┘
                                 │
                ┌────────────────┼────────────────┐
                │                │                │
                ▼                ▼                ▼
         ┌──────────┐     ┌──────────┐    ┌──────────┐
         │   Tech   │     │ Billing  │    │  Sales   │
         │   Dept   │     │   Dept   │    │   Dept   │
         └─────┬────┘     └────┬─────┘    └────┬─────┘
               │               │               │
               └───────────────┼───────────────┘
                               │
                               ▼
                        ┌──────────────┐
                        │  Formatter   │
                        └──────────────┘
```

**How It Works:**

1. **Input1** captures user query
2. **Orchestrator** analyzes and decides which departments to trigger
3. **Conditional edges** activate only the selected departments
4. **Formatter** combines all responses

**Key Point:** Only the branches where conditions are **true** will execute!

---

## Conditional Execution

### Understanding Conditions

Conditions use a simple template syntax:

#### Syntax

```
{{nodeId.outputKey}} operator value
```

#### Supported Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `contains` | Check if string/list contains value | `{{orch.targets}} contains 'tech'` |
| `is` | Exact equality | `{{node1.status}} is 'success'` |
| `>` | Greater than | `{{node1.confidence}} > 80` |
| `<` | Less than | `{{node1.score}} < 50` |
| `>=` | Greater or equal | `{{node1.count}} >= 5` |
| `<=` | Less or equal | `{{node1.count}} <= 10` |

### Node Conditions

**Node conditions** determine if a node should execute at all.

```json
{
  "id": "premium_feature",
  "type": "OpenAIChat",
  "config": { "..." },
  "condition": "{{user.tier}} is 'premium'"
}
```

### Edge Conditions

**Edge conditions** determine if data should flow through that edge.

```json
{
  "from": "classifier",
  "to": "urgent_handler",
  "condition": "{{classifier.urgency}} is 'high'"
}
```

### Complex Conditions Example

```json
{
  "id": "spam-router-flow",
  "nodes": [
    {
      "id": "spam_check",
      "type": "OpenAIChat",
      "config": {
        "systemPrompt": "Return JSON: {\"isSpam\": true/false, \"confidence\": 0-100}"
      }
    },
    {
      "id": "high_confidence_spam",
      "type": "OpenAIChat",
      "config": {
        "systemPrompt": "Block this spam message immediately."
      }
    },
    {
      "id": "low_confidence_spam",
      "type": "OpenAIChat",
      "config": {
        "systemPrompt": "Flag for manual review."
      }
    },
    {
      "id": "not_spam",
      "type": "OpenAIChat",
      "config": {
        "systemPrompt": "Process this legitimate message."
      }
    }
  ],
  "edges": [
    {
      "from": "spam_check",
      "to": "high_confidence_spam",
      "condition": "{{spam_check.isSpam}} is true AND {{spam_check.confidence}} > 90"
    },
    {
      "from": "spam_check",
      "to": "low_confidence_spam",
      "condition": "{{spam_check.isSpam}} is true AND {{spam_check.confidence}} <= 90"
    },
    {
      "from": "spam_check",
      "to": "not_spam",
      "condition": "{{spam_check.isSpam}} is false"
    }
  ]
}
```

**Flow Diagram:**

```
                    ┌──────────────┐
                    │  Spam Check  │
                    └──────┬───────┘
                           │
           ┌───────────────┼───────────────┐
           │               │               │
           ▼               ▼               ▼
    [confidence>90]  [confidence<=90]  [not spam]
           │               │               │
           ▼               ▼               ▼
    ┌──────────┐    ┌──────────┐   ┌──────────┐
    │  Block   │    │  Review  │   │ Process  │
    │   Spam   │    │   Spam   │   │ Message  │
    └──────────┘    └──────────┘   └──────────┘
```

---

## Parallel Execution

FlowManager automatically executes nodes **in parallel** when they have no dependencies on each other.

### Example: Parallel Analysis

```json
{
  "id": "parallel-analysis",
  "name": "Parallel Analysis Flow",
  "description": "Analyzes message for spam AND routes to departments simultaneously",
  "nodes": [
    {
      "id": "input1",
      "type": "Input",
      "config": { "text": "{{userInput}}" }
    },
    {
      "id": "spam_classifier",
      "type": "OpenAIChat",
      "config": {
        "systemPrompt": "Classify as SPAM or NOT_SPAM"
      }
    },
    {
      "id": "sentiment_analyzer",
      "type": "OpenAIChat",
      "config": {
        "systemPrompt": "Analyze sentiment: POSITIVE, NEUTRAL, or NEGATIVE"
      }
    },
    {
      "id": "language_detector",
      "type": "OpenAIChat",
      "config": {
        "systemPrompt": "Detect the language of the message"
      }
    },
    {
      "id": "aggregator",
      "type": "Formatter",
      "config": {
        "systemPrompt": "Combine all analysis results into one report"
      }
    }
  ],
  "edges": [
    {
      "from": "input1",
      "to": "spam_classifier"
    },
    {
      "from": "input1",
      "to": "sentiment_analyzer"
    },
    {
      "from": "input1",
      "to": "language_detector"
    },
    {
      "from": "spam_classifier",
      "to": "aggregator",
      "toInput": "spam_result"
    },
    {
      "from": "sentiment_analyzer",
      "to": "aggregator",
      "toInput": "sentiment_result"
    },
    {
      "from": "language_detector",
      "to": "aggregator",
      "toInput": "language_result"
    }
  ]
}
```

### Execution Timeline

```
Time ─────────────────────────────────────────────►

T0:   Input1 ■■■
               │
T1:            ├─► Spam Classifier ■■■■■■
               │
               ├─► Sentiment Analyzer ■■■■■
               │
               └─► Language Detector ■■■■■■■
                      │       │          │
T2:                   └───────┴──────────┴─► Aggregator ■■■
```

**Key Points:**
- `spam_classifier`, `sentiment_analyzer`, and `language_detector` all run **at the same time**
- `aggregator` waits for **all three** to complete before executing
- **Total time = longest path**, not sum of all nodes!

---

## Available Node Types

### 1. Input Node

**Purpose:** Entry point for user data

```json
{
  "id": "input1",
  "type": "Input",
  "config": {
    "text": "{{userInput}}"
  }
}
```

**Outputs:**
- `output`: The input text

---

### 2. OpenAIChat Node

**Purpose:** Call OpenAI's API for AI responses

```json
{
  "id": "llm1",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "sk-...",
    "model": "gpt-4o-mini",
    "temperature": 0.7,
    "systemPrompt": "You are a helpful assistant."
  }
}
```

**Config Options:**
- `apiKey` (required): Your OpenAI API key
- `model` (required): Model name (`gpt-4`, `gpt-4o-mini`, etc.)
- `temperature` (optional): Creativity (0.0 = deterministic, 2.0 = very creative)
- `systemPrompt` (required): Instructions for the AI

**Outputs:**
- `output`: The AI's response

---

### 3. Orchestrator Node

**Purpose:** Intelligent routing to multiple departments/targets

```json
{
  "id": "orchestrator",
  "type": "Orchestrator",
  "config": {
    "apiKey": "sk-...",
    "model": "gpt-4o-mini",
    "systemPrompt": "Route to: Search, Data, Function, Image departments..."
  }
}
```

**Outputs:**
- `action`: "DepartmentHandoff" or "None"
- `targets`: Comma-separated list of targets (e.g., "Search,Function")
- `query`: The original user question (passed through)
- `confidence`: Routing confidence (0-100)
- `needs_clarification`: Boolean
- `clarification_question`: Question to ask user (if needed)
- `reasoning`: Why this routing decision

**Example Usage:**

```json
{
  "from": "orchestrator",
  "to": "search_dept",
  "condition": "{{orchestrator.targets}} contains 'Search'"
}
```

---

### 4. Formatter Node

**Purpose:** Combine multiple inputs into one formatted response

```json
{
  "id": "formatter",
  "type": "Formatter",
  "config": {
    "apiKey": "sk-...",
    "systemPrompt": "Combine all department responses into a clear, organized answer."
  }
}
```

**Outputs:**
- `output`: The formatted response

---

### 5. Transform Node

**Purpose:** Data transformation and manipulation

```json
{
  "id": "transformer",
  "type": "Transform",
  "config": {
    "operation": "uppercase"
  }
}
```

---

### 6. SearchDepartment Node

**Purpose:** Search knowledge base and web

```json
{
  "id": "search_dept",
  "type": "SearchDepartment",
  "config": {
    "apiKey": "sk-...",
    "maxResults": 5
  }
}
```

**Outputs:**
- `output`: Search results and AI-generated response

---

### 7. DataDepartment Node

**Purpose:** Handle data queries and analysis

```json
{
  "id": "data_dept",
  "type": "DataDepartment",
  "config": {
    "apiKey": "sk-..."
  }
}
```

---

### 8. FunctionDepartment Node

**Purpose:** Execute functions, extract codes, generate reports

```json
{
  "id": "function_dept",
  "type": "FunctionDepartment",
  "config": {
    "apiKey": "sk-..."
  }
}
```

---

### 9. ImageDepartment Node

**Purpose:** Analyze images with AI vision

```json
{
  "id": "image_dept",
  "type": "ImageDepartment",
  "config": {
    "apiKey": "sk-..."
  }
}
```

---

### 10. PromptTemplate Node

**Purpose:** Build prompts from templates

```json
{
  "id": "prompt_builder",
  "type": "PromptTemplate",
  "config": {
    "template": "Hello {{name}}, your order {{orderId}} is ready!"
  }
}
```

---

## Advanced Flow Patterns

### Pattern 1: Fan-Out / Fan-In

**Use Case:** Process data through multiple parallel paths, then combine

```
        Input
          │
    ┌─────┼─────┐
    ▼     ▼     ▼
  Node1 Node2 Node3
    │     │     │
    └─────┼─────┘
          ▼
      Combiner
```

### Pattern 2: Conditional Branching with Fallback

**Use Case:** Try primary path, fall back if it fails

```
      Start
        │
        ▼
    Decision
        │
    ┌───┴───┐
    ▼       ▼
 Primary  Fallback
    │       │
    └───┬───┘
        ▼
       End
```

### Pattern 3: Multi-Stage Pipeline

**Use Case:** Data flows through multiple transformation stages

```
Input → Validate → Transform → Enrich → Format → Output
```

### Pattern 4: Dynamic Department Routing

**Use Case:** Route to 1+ departments based on AI decision

```
                Input
                  │
                  ▼
             Orchestrator
                  │
      ┌───────────┼───────────┐
      ▼           ▼           ▼
   Search      Function     Image
   (if Search) (if Function) (if Image)
      │           │           │
      └───────────┼───────────┘
                  ▼
              Formatter
```

---

## Best Practices

### 1. Node Naming

✅ **Good:**
```json
{ "id": "spam_classifier" }
{ "id": "tech_department" }
{ "id": "final_formatter" }
```

❌ **Bad:**
```json
{ "id": "node1" }
{ "id": "n2" }
{ "id": "x" }
```

### 2. Error Handling

Always include a **Formatter** node at the end to handle:
- Missing department responses
- Partial failures
- Edge cases

```json
{
  "id": "formatter",
  "type": "Formatter",
  "config": {
    "systemPrompt": "Combine responses. If a department didn't respond, indicate it's unavailable."
  }
}
```

### 3. System Prompts

Be **specific and explicit**:

✅ **Good:**
```
"Classify the message as SPAM or NOT_SPAM.
Return ONLY valid JSON: {\"classification\": \"SPAM\", \"confidence\": 95, \"reason\": \"...\"}"
```

❌ **Bad:**
```
"Check if this is spam"
```

### 4. Temperature Settings

| Use Case | Temperature |
|----------|-------------|
| Factual answers, classification | 0.0 - 0.3 |
| Balanced responses | 0.5 - 0.7 |
| Creative writing | 0.8 - 1.2 |
| Experimental/random | 1.5 - 2.0 |

### 5. Conditional Logic

✅ **Use conditions when:**
- Routing to optional departments
- Implementing fallback logic
- Filtering based on confidence scores

❌ **Don't use conditions when:**
- All paths should always execute
- You're connecting sequential steps

### 6. Testing Flows

1. **Start simple:** Single path, no conditions
2. **Add branches:** One at a time
3. **Test edge cases:** What if all conditions are false?
4. **Check parallel execution:** Are nodes truly independent?

---

## Troubleshooting

### Problem: "Flow contains a cycle"

**Cause:** Your edges create a loop

```
Node A → Node B → Node C → Node A  ❌ (cycle!)
```

**Solution:** Remove the back-edge to eliminate the cycle

---

### Problem: "Node not executing"

**Possible Causes:**

1. **Node condition is false**
   - Check: `"condition": "{{...}}"`
   - Solution: Verify the condition evaluates to true

2. **Incoming edge condition is false**
   - Check: Edge conditions pointing to this node
   - Solution: Review edge conditions

3. **Parent node hasn't completed**
   - Check: Are dependencies satisfied?
   - Solution: Verify parent nodes execute successfully

---

### Problem: "Node outputs not available"

**Cause:** Trying to access outputs before node executes

**Solution:** Ensure execution order:
```json
// ✅ Correct: orchestrator runs before tech_dept
{
  "from": "orchestrator",
  "to": "tech_dept",
  "condition": "{{orchestrator.targets}} contains 'tech'"
}
```

---

### Problem: "Parallel nodes not waiting for each other"

**Expected:** Formatter should wait for all departments

**Solution:** Ensure **all** department edges point to formatter:

```json
{ "from": "tech_dept", "to": "formatter" },
{ "from": "billing_dept", "to": "formatter" },
{ "from": "sales_dept", "to": "formatter" }
```

The formatter will **automatically wait** for all parent nodes to complete.

---

### Problem: "Edge condition syntax error"

**Common Mistakes:**

❌ `"condition": "orchestrator.targets contains tech"`
✅ `"condition": "{{orchestrator.targets}} contains 'tech'"`

❌ `"condition": "{{node1.score}} > '80'"`
✅ `"condition": "{{node1.score}} > 80"`

**Rules:**
- Always wrap variable references in `{{...}}`
- Use single quotes for string literals
- No quotes for numbers or booleans

---

## Example: Complete Multi-Branch Flow

Here's a **real-world example** combining everything you've learned:

```json
{
  "id": "customer-support-flow",
  "name": "Intelligent Customer Support Router",
  "description": "Routes customer queries to appropriate departments with spam detection",
  "tenantId": "default",
  "nodes": [
    {
      "id": "input1",
      "type": "Input",
      "config": {
        "text": "{{userInput}}"
      }
    },
    {
      "id": "spam_classifier",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "model": "gpt-4o-mini",
        "temperature": 0.2,
        "systemPrompt": "Classify as SPAM or NOT_SPAM. Return JSON: {\"classification\": \"SPAM\" or \"NOT_SPAM\", \"confidence\": 0-100, \"reason\": \"...\"}"
      }
    },
    {
      "id": "orchestrator",
      "type": "Orchestrator",
      "config": {
        "apiKey": "your-api-key",
        "model": "gpt-4o-mini",
        "systemPrompt": "Route to departments: tech, billing, sales, support. Return JSON: {\"Action\": \"DepartmentHandoff\", \"Pipeline\": [{\"Target\": \"tech\"}]}"
      }
    },
    {
      "id": "tech_dept",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "Technical support specialist. Provide troubleshooting steps."
      }
    },
    {
      "id": "billing_dept",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "Billing support. Help with payments and invoices."
      }
    },
    {
      "id": "sales_dept",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "Sales specialist. Answer product questions."
      }
    },
    {
      "id": "support_dept",
      "type": "OpenAIChat",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "General support. Help with account questions."
      }
    },
    {
      "id": "final_formatter",
      "type": "Formatter",
      "config": {
        "apiKey": "your-api-key",
        "systemPrompt": "Combine spam analysis and department responses. If spam confidence > 80%, warn user. Otherwise, present department responses clearly."
      }
    }
  ],
  "edges": [
    {
      "from": "input1",
      "to": "spam_classifier"
    },
    {
      "from": "input1",
      "to": "orchestrator"
    },
    {
      "from": "orchestrator",
      "to": "tech_dept",
      "condition": "{{orchestrator.targets}} contains 'tech'"
    },
    {
      "from": "orchestrator",
      "to": "billing_dept",
      "condition": "{{orchestrator.targets}} contains 'billing'"
    },
    {
      "from": "orchestrator",
      "to": "sales_dept",
      "condition": "{{orchestrator.targets}} contains 'sales'"
    },
    {
      "from": "orchestrator",
      "to": "support_dept",
      "condition": "{{orchestrator.targets}} contains 'support'"
    },
    {
      "from": "spam_classifier",
      "to": "final_formatter",
      "toInput": "spam_analysis"
    },
    {
      "from": "tech_dept",
      "to": "final_formatter",
      "toInput": "tech_response"
    },
    {
      "from": "billing_dept",
      "to": "final_formatter",
      "toInput": "billing_response"
    },
    {
      "from": "sales_dept",
      "to": "final_formatter",
      "toInput": "sales_response"
    },
    {
      "from": "support_dept",
      "to": "final_formatter",
      "toInput": "support_response"
    },
    {
      "from": "orchestrator",
      "to": "final_formatter",
      "toInput": "routing_info"
    }
  ]
}
```

### Flow Visualization

```
                          ┌──────────────┐
                          │   Input1     │
                          └──────┬───────┘
                                 │
                    ┌────────────┴───────────────┐
                    ▼                            ▼
            ┌───────────────┐          ┌─────────────────┐
            │ Spam Classifier│          │  Orchestrator   │
            └───────┬───────┘          └─────────┬───────┘
                    │                             │
                    │                ┌────────────┼────────────┐
                    │                │            │            │
                    │                ▼            ▼            ▼
                    │          ┌─────────┐  ┌─────────┐  ┌─────────┐
                    │          │  Tech   │  │ Billing │  │  Sales  │
                    │          │  Dept   │  │  Dept   │  │  Dept   │
                    │          └────┬────┘  └────┬────┘  └────┬────┘
                    │               │            │            │
                    └───────────────┴────────────┴────────────┘
                                    │
                                    ▼
                            ┌───────────────┐
                            │Final Formatter│
                            └───────────────┘
```

---


