# 🚀 Advanced Flow Creation Guide

**Complete Guide to Building Complex AI Workflows with FlowManager**

> Master system prompts, parallel execution, conditional branching, and advanced patterns through real-world examples

---

## 📚 Table of Contents

1. [Flow Architecture Overview](#flow-architecture-overview)
2. [Understanding the Advanced Code Review Flow](#understanding-the-advanced-code-review-flow)
3. [System Prompt Crafting Guide](#system-prompt-crafting-guide)
4. [Parallel Execution Patterns](#parallel-execution-patterns)
5. [Conditional Branching Guide](#conditional-branching-guide)
6. [Advanced Flow Patterns](#advanced-flow-patterns)
7. [JSON Structure Reference](#json-structure-reference)
8. [Step-by-Step: Building the Code Review Flow](#step-by-step-building-the-code-review-flow)
9. [Best Practices & Tips](#best-practices--tips)
10. [Troubleshooting](#troubleshooting)

---

## Flow Architecture Overview

### What is the Advanced Code Review Flow?

The Advanced Code Review Flow is a **production-grade example** that demonstrates all key FlowManager capabilities:

```
INPUT (Code)
    ↓
    ├──→ Security Analyzer  ──┐
    ├──→ Style Analyzer     ──┤
    ├──→ Test Analyzer      ──┤  PARALLEL EXECUTION (4 nodes run simultaneously)
    └──→ Performance Analyzer─┘
              ↓
         AGGREGATOR (Combines all results)
              ↓
         SCORE CALCULATOR (Weighted scoring + decision)
              ↓
         ┌────┴────┬────────────┐
         ↓         ↓            ↓         CONDITIONAL BRANCHING (based on decision)
    APPROVE    CHANGES      REJECT
    HANDLER    HANDLER      HANDLER
         ↓         ↓            ↓
         └────┬────┴────────────┘
              ↓
         FINAL NOTIFIER (Aggregates final output)
              ↓
         OUTPUT (Professional review report)
```

**Key Features Demonstrated:**
- ✅ **Parallel Execution**: 4 analyzers run simultaneously for speed
- ✅ **Conditional Branching**: Routes to different handlers based on score
- ✅ **Multi-Input Aggregation**: Aggregator and Notifier receive multiple inputs
- ✅ **Structured Output**: JSON-based communication between nodes
- ✅ **Weighted Decision Making**: Complex scoring logic with multiple criteria

---

## Understanding the Advanced Code Review Flow

### Flow Stages Breakdown

#### **Stage 1: Input (1 node)**
```json
{
  "id": "code_input",
  "type": "Input",
  "config": {
    "text": "{{userInput}}"
  }
}
```
- **Purpose**: Entry point for code to be reviewed
- **Output**: Raw code text
- **Variable**: `{{userInput}}` is replaced at runtime with user's input

---

#### **Stage 2: Parallel Analysis (4 nodes running simultaneously)**

**Why Parallel?**
- Each analyzer is independent (no dependencies between them)
- All receive the same input (code)
- Running in parallel reduces total execution time from ~40s to ~10s

**1. Security Analyzer**
```json
{
  "id": "security_analyzer",
  "type": "OpenAIChat",
  "config": {
    "model": "gpt-4o-mini",
    "temperature": 0.3,
    "systemPrompt": "You are a security code reviewer. Analyze for: SQL injection, XSS, auth issues, data exposure. Return JSON: {\"score\": 0-100, \"issues\": [\"issue1\"], \"severity\": \"low|medium|high|critical\"}"
  }
}
```

**System Prompt Analysis:**
- **Role Definition**: "You are a security code reviewer" (sets AI persona)
- **Specific Tasks**: Lists exact security concerns to check
- **Output Format**: Demands structured JSON response
- **Temperature**: 0.3 (low = more deterministic, consistent results)

**2. Style Analyzer**
```json
{
  "systemPrompt": "You are a code style reviewer. Check: naming, comments, structure, readability, best practices. Return JSON: {\"score\": 0-100, \"issues\": [], \"suggestions\": []}"
}
```

**3. Test Analyzer**
```json
{
  "systemPrompt": "You are a test coverage reviewer. Analyze: edge cases, happy paths, error handling, mocking. Return JSON: {\"score\": 0-100, \"coverage\": \"percentage\", \"missing_tests\": []}"
}
```

**4. Performance Analyzer**
```json
{
  "systemPrompt": "You are a performance reviewer. Check: loops, algorithms, memory leaks, DB queries. Return JSON: {\"score\": 0-100, \"bottlenecks\": [], \"optimizations\": []}"
}
```

---

#### **Stage 3: Aggregation (1 node)**
```json
{
  "id": "aggregator",
  "type": "Formatter",
  "config": {
    "systemPrompt": "Combine all outputs. Structure:\nSecurity: {{security}}\nStyle: {{style}}\nTest: {{testing}}\nPerformance: {{performance}}\n\nProvide clear summary."
  }
}
```

**How Aggregation Works:**
- Receives 4 inputs (from edges with different `toInput` names)
- Uses `{{security}}`, `{{style}}`, `{{testing}}`, `{{performance}}` variables
- Formatter node combines them into unified report

**Edges Feeding Aggregator:**
```json
{"from": "security_analyzer", "to": "aggregator", "toInput": "security"},
{"from": "style_analyzer", "to": "aggregator", "toInput": "style"},
{"from": "test_analyzer", "to": "aggregator", "toInput": "testing"},
{"from": "performance_analyzer", "to": "aggregator", "toInput": "performance"}
```

---

#### **Stage 4: Score Calculation (1 node)**
```json
{
  "id": "score_calculator",
  "type": "OpenAIChat",
  "config": {
    "temperature": 0.1,
    "systemPrompt": "Calculate final score. Weighting: Security 40%, Style 20%, Testing 25%, Performance 15%. Return ONLY JSON: {\"final_score\": 0-100, \"decision\": \"approve|request_changes|reject\", \"reasoning\": \"...\"}"
  }
}
```

**Critical Details:**
- **Temperature: 0.1**: Nearly deterministic (critical for consistent scoring)
- **Weighted Formula**: Security has highest weight (40%)
- **Decision Output**: `decision` field drives conditional routing
- **Return ONLY JSON**: Ensures clean parsing (no extra text)

---

#### **Stage 5: Conditional Branching (3 handler nodes)**

**How Conditional Routing Works:**

**Edges with Conditions:**
```json
{
  "from": "score_calculator",
  "to": "approve_handler",
  "condition": "{{score_calculator.decision}} == 'approve'"
},
{
  "from": "score_calculator",
  "to": "changes_handler",
  "condition": "{{score_calculator.decision}} == 'request_changes'"
},
{
  "from": "score_calculator",
  "to": "reject_handler",
  "condition": "{{score_calculator.decision}} == 'reject'"
}
```

**Condition Syntax Breakdown:**
- `{{score_calculator.decision}}`: References output from score_calculator node
- `== 'approve'`: Equality operator with string literal
- **Only ONE path is followed** based on the decision value

**Handler Nodes:**

**1. Approve Handler**
```json
{
  "systemPrompt": "Generate approval message. Be positive and congratulatory. Mention excellent score and key strengths. Format professionally."
}
```

**2. Changes Handler**
```json
{
  "systemPrompt": "Generate change request. Be constructive and specific. List improvements clearly. Acknowledge good parts. Format as actionable checklist."
}
```

**3. Reject Handler**
```json
{
  "systemPrompt": "Generate rejection. Be professional but firm. Explain critical issues requiring rework. Provide guidance on standards."
}
```

---

#### **Stage 6: Final Notification (1 node)**
```json
{
  "id": "final_notifier",
  "type": "Formatter",
  "config": {
    "systemPrompt": "Create final notification. Include: Review Summary, Final Score, Decision, detailed feedback. Professional and clear."
  }
}
```

**Multi-Input Convergence:**
```json
{"from": "approve_handler", "to": "final_notifier", "toInput": "approval_message"},
{"from": "changes_handler", "to": "final_notifier", "toInput": "changes_message"},
{"from": "reject_handler", "to": "final_notifier", "toInput": "rejection_message"},
{"from": "score_calculator", "to": "final_notifier", "toInput": "score_data"}
```

**Important**: Only ONE of the handler messages will have data (based on which branch was taken). The Formatter handles missing inputs gracefully.

---

## System Prompt Crafting Guide

### The Anatomy of an Effective System Prompt

A well-crafted system prompt has **5 key components**:

```
[ROLE] + [TASK] + [CONSTRAINTS] + [OUTPUT FORMAT] + [TONE/STYLE]
```

---

### 1. **ROLE Definition**

**Purpose**: Establishes the AI's persona and expertise

**Examples:**
```
✅ "You are a senior security code reviewer with 10 years of experience in web security"
✅ "You are a technical writer specializing in API documentation"
✅ "You are a data analyst focused on business intelligence reporting"

❌ "Analyze this code" (no role context)
❌ "Help me" (too vague)
```

**Best Practices:**
- Be specific about expertise level (junior, senior, expert)
- Include domain context (security, performance, style)
- Set realistic scope (don't claim AI is "perfect" or "infallible")

---

### 2. **TASK Specification**

**Purpose**: Defines exactly what to analyze or produce

**Good Task Definitions:**
```
✅ "Analyze the code for security vulnerabilities including:
    - SQL injection attacks
    - Cross-site scripting (XSS)
    - Authentication bypass
    - Sensitive data exposure
    - Insecure cryptography"

✅ "Review code style focusing on:
    - Naming conventions (camelCase for variables, PascalCase for classes)
    - Comment quality and documentation
    - Code structure and modularity
    - Adherence to SOLID principles"

❌ "Check for problems" (too vague)
❌ "Make it better" (no specific criteria)
```

**Best Practices:**
- List specific items to check
- Provide concrete examples
- Define success criteria
- Mention edge cases to consider

---

### 3. **CONSTRAINTS**

**Purpose**: Sets boundaries and priorities

**Examples:**
```
✅ "Focus only on critical and high-severity issues"
✅ "Limit response to 5 most important findings"
✅ "Consider performance implications over code aesthetics"
✅ "Assume the code runs in a Node.js 18+ environment"

✅ "Do NOT suggest rewriting the entire codebase"
✅ "Ignore minor style nitpicks if logic is sound"
```

**Constraint Types:**
- **Scope Limits**: What to include/exclude
- **Priority Weights**: What matters most
- **Context Assumptions**: Environment, framework, versions
- **Negative Constraints**: What NOT to do

---

### 4. **OUTPUT FORMAT**

**Purpose**: Ensures consistent, parseable responses

**Structured JSON Output (Recommended for inter-node communication):**
```json
"Return ONLY a valid JSON object with this exact structure:
{
  \"score\": <number 0-100>,
  \"issues\": [
    {
      \"severity\": \"low|medium|high|critical\",
      \"line\": <number>,
      \"description\": \"...\",
      \"fix\": \"...\"
    }
  ],
  \"summary\": \"...\"
}"
```

**Human-Readable Output (For final user-facing nodes):**
```
"Format your response as:

# Code Review Summary

## Overall Score: [0-100]

## Critical Issues
- Issue 1: [description]
- Issue 2: [description]

## Recommendations
1. [First recommendation]
2. [Second recommendation]

## Conclusion
[Final assessment]"
```

**Best Practices:**
- For inter-node communication: **Always use JSON**
- For final output: Use markdown or structured text
- Specify field types (string, number, array)
- Include examples of expected output
- Use `Return ONLY JSON` to prevent extra text

---

### 5. **TONE/STYLE**

**Purpose**: Sets communication style and approach

**Examples:**

**Professional & Constructive:**
```
"Be professional and constructive. Acknowledge good practices before mentioning issues. Provide actionable suggestions with examples."
```

**Firm but Fair:**
```
"Be direct and honest about critical issues, but maintain a respectful tone. Focus on code quality, not developer skill."
```

**Encouraging:**
```
"Be positive and encouraging. Frame issues as learning opportunities. Celebrate good practices."
```

**Concise & Technical:**
```
"Be concise and technical. Use industry-standard terminology. Avoid unnecessary explanations."
```

---

### System Prompt Examples for Common Scenarios

#### **Example 1: Data Validation Node**
```json
{
  "systemPrompt": "You are a data validation specialist. Analyze the input data for:\n\n1. **Completeness**: Are all required fields present?\n2. **Format**: Do fields match expected patterns (email, phone, date)?\n3. **Range**: Are numeric values within acceptable bounds?\n4. **Consistency**: Do related fields align logically?\n\nReturn ONLY JSON:\n{\n  \"valid\": true/false,\n  \"errors\": [\n    {\"field\": \"email\", \"issue\": \"Invalid format\", \"example\": \"user@example.com\"}\n  ],\n  \"warnings\": [\n    {\"field\": \"age\", \"issue\": \"Unusually high value\"}\n  ]\n}\n\nBe strict: reject data with ANY error. Warnings are informational only."
}
```

---

#### **Example 2: Content Moderation Node**
```json
{
  "systemPrompt": "You are a content moderation AI. Review text for:\n\n**REJECT if contains:**\n- Hate speech, slurs, or discrimination\n- Explicit violence or threats\n- Personal information (PII)\n- Spam or phishing attempts\n\n**FLAG for review if contains:**\n- Controversial political content\n- Medical or legal advice\n- Unverified claims\n\n**APPROVE if:**\n- Constructive discussion\n- Factual information\n- Personal opinions expressed respectfully\n\nReturn JSON:\n{\n  \"decision\": \"approve|flag|reject\",\n  \"reason\": \"Brief explanation\",\n  \"confidence\": 0.0-1.0,\n  \"categories\": [\"hate_speech\", \"spam\", ...]\n}\n\nWhen uncertain, choose 'flag' over 'approve'."
}
```

---

#### **Example 3: Report Generator Node**
```json
{
  "systemPrompt": "You are a business intelligence report writer. Generate an executive summary from the analytics data.\n\n**Structure:**\n1. **Key Metrics** (2-3 sentences highlighting main numbers)\n2. **Trends** (Compare to previous period, note changes)\n3. **Insights** (What the data tells us about business performance)\n4. **Recommendations** (2-3 actionable next steps)\n\n**Style Guidelines:**\n- Use business-appropriate language (avoid jargon unless necessary)\n- Lead with conclusions, support with data\n- Quantify everything (use percentages, ratios, growth rates)\n- Be concise (max 300 words)\n- Use bullet points for readability\n\n**Tone:** Professional, confident, data-driven. No hedging language (\"might\", \"possibly\"). State findings clearly."
}
```

---

#### **Example 4: Multi-Language Classifier**
```json
{
  "systemPrompt": "You are a language detection and classification system.\n\nAnalyze the text and return:\n{\n  \"primary_language\": \"en|es|fr|de|zh|ja|ar|...\",\n  \"confidence\": 0.0-1.0,\n  \"is_mixed\": true/false,\n  \"detected_languages\": [\"en\", \"es\"],\n  \"content_type\": \"formal|casual|technical|marketing\",\n  \"readability_level\": \"elementary|middle|high_school|college|professional\"\n}\n\n**Language Codes:** Use ISO 639-1 (two-letter codes)\n**Confidence:** >0.9 = certain, 0.7-0.9 = confident, 0.5-0.7 = uncertain, <0.5 = guess\n**Mixed:** True if multiple languages appear in same text\n\nFor very short text (<10 words), set confidence lower."
}
```

---

### Temperature Settings Guide

**Temperature** controls randomness/creativity in AI responses.

| Temperature | Use Case | Example Nodes |
|-------------|----------|---------------|
| **0.0 - 0.2** | Deterministic, consistent | Score calculators, validators, classifiers |
| **0.3 - 0.5** | Balanced (slight variation OK) | Code analyzers, data processors |
| **0.6 - 0.8** | Creative but coherent | Report writers, content generators |
| **0.9 - 1.0** | Highly creative, diverse | Creative writing, brainstorming |

**Examples from Code Review Flow:**
```json
{
  "score_calculator": {
    "temperature": 0.1  // Nearly identical scoring each time
  },
  "security_analyzer": {
    "temperature": 0.3  // Consistent findings, slight phrasing variation
  },
  "approve_handler": {
    "temperature": 0.5  // More varied, friendly messages
  }
}
```

---

## Parallel Execution Patterns

### What is Parallel Execution?

**Parallel execution** means multiple nodes run **simultaneously** instead of sequentially.

**Benefits:**
- ⚡ **Speed**: 4 nodes running in parallel = ~4x faster than sequential
- 🔄 **Efficiency**: Better resource utilization
- 🎯 **Independence**: Each node focuses on specific task

---

### How FlowManager Achieves Parallelism

**The Engine's Batch Execution:**

1. **Dependency Tracking**: Engine tracks how many parent nodes each node depends on
2. **Batching**: Nodes with ALL dependencies satisfied are grouped into a batch
3. **Parallel Execution**: All nodes in a batch run via `Task.WhenAll` (C# async)
4. **Next Batch**: Once batch completes, next batch of ready nodes executes

**Example Execution Timeline:**

```
TIME    BATCH 1          BATCH 2 (Parallel)                    BATCH 3        BATCH 4
0s   → [code_input]
10s                    → [security_analyzer]
                         [style_analyzer]
                         [test_analyzer]
                         [performance_analyzer]
20s                                                         → [aggregator]
30s                                                                         → [score_calculator]
```

Total: ~30 seconds (vs ~50+ seconds if sequential)

---

### Creating Parallel Execution

**Pattern: Fan-Out from Single Source**

```json
{
  "nodes": [
    {"id": "input", "type": "Input"},
    {"id": "analyzer_1", "type": "OpenAIChat"},
    {"id": "analyzer_2", "type": "OpenAIChat"},
    {"id": "analyzer_3", "type": "OpenAIChat"}
  ],
  "edges": [
    {"from": "input", "to": "analyzer_1"},  // All 3 edges from same source
    {"from": "input", "to": "analyzer_2"},  // No dependencies between analyzers
    {"from": "input", "to": "analyzer_3"}   // = PARALLEL EXECUTION
  ]
}
```

**Key Requirements:**
1. ✅ All parallel nodes have the **same parent** (or no dependencies between them)
2. ✅ No edges **between** the parallel nodes (they don't depend on each other)
3. ✅ All receive their inputs at the same time

---

### Pattern: Fan-In (Aggregation After Parallel)

```json
{
  "nodes": [
    {"id": "parallel_1", "type": "OpenAIChat"},
    {"id": "parallel_2", "type": "OpenAIChat"},
    {"id": "parallel_3", "type": "OpenAIChat"},
    {"id": "aggregator", "type": "Formatter"}
  ],
  "edges": [
    {"from": "parallel_1", "to": "aggregator", "toInput": "result_1"},
    {"from": "parallel_2", "to": "aggregator", "toInput": "result_2"},
    {"from": "parallel_3", "to": "aggregator", "toInput": "result_3"}
  ]
}
```

**How It Works:**
1. `parallel_1`, `parallel_2`, `parallel_3` run simultaneously
2. Aggregator waits for **ALL 3** to complete
3. Each result comes in through different `toInput` name
4. Aggregator can reference: `{{result_1}}`, `{{result_2}}`, `{{result_3}}`

---

### Real-World Parallel Patterns

#### **Pattern 1: Multi-Model Consensus**
Run same prompt through multiple AI models, compare results.

```json
{
  "nodes": [
    {"id": "gpt4", "type": "OpenAIChat", "config": {"model": "gpt-4"}},
    {"id": "claude", "type": "OpenAIChat", "config": {"model": "claude-3"}},
    {"id": "gemini", "type": "OpenAIChat", "config": {"model": "gemini-pro"}},
    {"id": "consensus", "type": "Formatter"}
  ]
}
```

**Use Case**: Critical decisions requiring multiple perspectives

---

#### **Pattern 2: Multi-Aspect Analysis**
Analyze different aspects of same input simultaneously.

```
Input → [Sentiment Analyzer]  →┐
     → [Entity Extractor]     →┤
     → [Topic Classifier]     →├→ Summary Generator
     → [Language Detector]    →┘
```

**Use Case**: Content analysis pipelines, document processing

---

#### **Pattern 3: A/B Testing Flows**
Run two different prompting strategies in parallel, compare results.

```json
{
  "nodes": [
    {"id": "strategy_a", "config": {"systemPrompt": "Approach A..."}},
    {"id": "strategy_b", "config": {"systemPrompt": "Approach B..."}},
    {"id": "comparator", "type": "OpenAIChat",
     "config": {"systemPrompt": "Compare A and B, determine which is better based on: accuracy, clarity, completeness"}}
  ]
}
```

---

## Conditional Branching Guide

### Understanding Conditional Edges

**Conditional edges** allow dynamic routing based on **node outputs**.

**Syntax:**
```json
{
  "from": "source_node",
  "to": "target_node",
  "condition": "{{source_node.output_key}} operator value"
}
```

---

### Supported Operators

| Operator | Description | Example |
|----------|-------------|---------|
| `==` | Equals (string or number) | `{{score.decision}} == 'approve'` |
| `!=` | Not equals | `{{status.code}} != 200` |
| `>` | Greater than | `{{confidence.score}} > 0.8` |
| `<` | Less than | `{{risk.level}} < 5` |
| `>=` | Greater or equal | `{{review.points}} >= 80` |
| `<=` | Less or equal | `{{errors.count}} <= 0` |
| `contains` | String contains | `{{tags.list}} contains 'urgent'` |
| `is true` | Boolean true | `{{validation.passed}} is true` |
| `is false` | Boolean false | `{{has_errors}} is false` |

---

### Condition Examples

#### **Example 1: String Equality**
```json
{
  "condition": "{{classifier.category}} == 'spam'"
}
```

**Requirements:**
- `classifier` node must output a field called `category`
- Value must exactly match `'spam'` (case-sensitive)

---

#### **Example 2: Numeric Comparison**
```json
{
  "condition": "{{scorer.final_score}} >= 75"
}
```

**Requirements:**
- `scorer` node outputs `final_score` as a number
- Condition is true if score is 75 or higher

---

#### **Example 3: String Contains**
```json
{
  "condition": "{{analyzer.issues}} contains 'security'"
}
```

**Use Case:** Check if a comma-separated list or text contains a keyword

---

#### **Example 4: Boolean Check**
```json
{
  "condition": "{{validator.is_valid}} is true"
}
```

**Requirements:**
- `validator` node outputs `is_valid` as boolean

---

### Multi-Way Branching Pattern

**Scenario:** Route to different handlers based on score ranges

```json
{
  "edges": [
    {
      "from": "scorer",
      "to": "excellent_handler",
      "condition": "{{scorer.final_score}} >= 90"
    },
    {
      "from": "scorer",
      "to": "good_handler",
      "condition": "{{scorer.final_score}} >= 70"
    },
    {
      "from": "scorer",
      "to": "needs_improvement_handler",
      "condition": "{{scorer.final_score}} >= 50"
    },
    {
      "from": "scorer",
      "to": "poor_handler",
      "condition": "{{scorer.final_score}} < 50"
    }
  ]
}
```

**How It Works:**
- If score is 95: Only `excellent_handler` runs
- If score is 75: Only `good_handler` runs
- If score is 60: Only `needs_improvement_handler` runs
- If score is 30: Only `poor_handler` runs

**Important**: Only edges with **TRUE** conditions are followed.

---

### Combining Conditions with Parallelism

**Advanced Pattern: Conditional Fan-Out**

```json
{
  "nodes": [
    {"id": "classifier", "type": "OpenAIChat"},
    {"id": "technical_processor", "type": "OpenAIChat"},
    {"id": "business_processor", "type": "OpenAIChat"},
    {"id": "general_processor", "type": "OpenAIChat"}
  ],
  "edges": [
    {
      "from": "classifier",
      "to": "technical_processor",
      "condition": "{{classifier.category}} contains 'technical'"
    },
    {
      "from": "classifier",
      "to": "business_processor",
      "condition": "{{classifier.category}} contains 'business'"
    },
    {
      "from": "classifier",
      "to": "general_processor",
      "condition": "{{classifier.category}} == 'general'"
    }
  ]
}
```

**Use Case:** Different processing paths for different content types

---

### Error Handling with Conditions

**Pattern: Fallback on Failure**

```json
{
  "nodes": [
    {"id": "primary_api", "type": "OpenAIChat"},
    {"id": "fallback_api", "type": "OpenAIChat"},
    {"id": "error_handler", "type": "OpenAIChat"}
  ],
  "edges": [
    {
      "from": "primary_api",
      "to": "error_handler",
      "condition": "{{primary_api.error}} is true"
    }
  ]
}
```

---

## Advanced Flow Patterns

### Pattern 1: Pipeline with Quality Gates

**Scenario:** Multi-stage content creation with approval checkpoints

```
Draft Generator → Quality Checker → [PASS] → Formatter → Publisher
                                   ↓
                                  [FAIL] → Revision Prompt → Draft Generator (loop)
```

**JSON Structure:**
```json
{
  "nodes": [
    {"id": "draft", "type": "OpenAIChat"},
    {"id": "checker", "type": "OpenAIChat"},
    {"id": "formatter", "type": "OpenAIChat"},
    {"id": "publisher", "type": "OpenAIChat"}
  ],
  "edges": [
    {"from": "draft", "to": "checker"},
    {"from": "checker", "to": "formatter",
     "condition": "{{checker.quality_score}} >= 8"},
    {"from": "formatter", "to": "publisher"}
  ]
}
```

---

### Pattern 2: Hierarchical Classification

**Scenario:** Multi-level categorization (broad → specific)

```
Input → Level1_Classifier → [Tech] → TechSubClassifier → [AI/Web/Mobile]
                           ↓
                          [Business] → BusinessSubClassifier → [Finance/HR/Sales]
                           ↓
                          [Other] → GeneralHandler
```

---

### Pattern 3: Weighted Voting System

**Scenario:** Multiple evaluators vote on a decision

```json
{
  "nodes": [
    {"id": "evaluator_1", "type": "OpenAIChat"},
    {"id": "evaluator_2", "type": "OpenAIChat"},
    {"id": "evaluator_3", "type": "OpenAIChat"},
    {"id": "vote_counter", "type": "OpenAIChat",
     "config": {
       "systemPrompt": "Count votes from 3 evaluators. Each returns approve/reject. Return {\"decision\": \"approve|reject\", \"vote_count\": {\"approve\": X, \"reject\": Y}}. Majority wins."
     }}
  ]
}
```

---

### Pattern 4: Dynamic Department Routing (Orchestrator Pattern)

**From the spam-and-router-flow:**

```
Input → Orchestrator (AI decides which departments) → [Search|Data|Function] (parallel) → Aggregator
```

**Key Feature**: Orchestrator node uses AI to decide which downstream nodes should run based on query analysis.

---

## JSON Structure Reference

### Complete Flow Schema

```json
{
  "id": "unique-flow-id",
  "name": "Human-Readable Flow Name",
  "description": "Brief description of what this flow does",
  "tenantId": "default",
  "nodes": [
    {
      "id": "node_id",
      "type": "Input|OpenAIChat|Formatter|Transform|Orchestrator|SearchDepartment|DataDepartment|FunctionDepartment|ImageDepartment",
      "config": {
        // Node-specific configuration
      },
      "condition": "optional node-level condition"
    }
  ],
  "edges": [
    {
      "from": "source_node_id",
      "to": "target_node_id",
      "fromOutput": "output_key_name",
      "toInput": "input_key_name",
      "condition": "optional edge-level condition"
    }
  ],
  "createdAt": "2025-01-01T00:00:00Z",
  "updatedAt": "2025-01-01T00:00:00Z",
  "lastModified": "2025-01-01T00:00:00Z"
}
```

---

### Node Type Configurations

#### **Input Node**
```json
{
  "id": "input1",
  "type": "Input",
  "config": {
    "text": "{{userInput}}"  // Special variable replaced at runtime
  }
}
```

#### **OpenAIChat Node**
```json
{
  "id": "llm1",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "your-api-key",
    "model": "gpt-4o-mini|gpt-4|gpt-4-turbo",
    "temperature": 0.0-2.0,
    "systemPrompt": "Your detailed system prompt here...",
    "maxTokens": 1000  // Optional
  }
}
```

#### **Formatter Node**
```json
{
  "id": "formatter1",
  "type": "Formatter",
  "config": {
    "apiKey": "your-api-key",
    "systemPrompt": "Template with variables: {{input1}}, {{input2}}"
  }
}
```

#### **Transform Node**
```json
{
  "id": "transform1",
  "type": "Transform",
  "config": {
    "operation": "uppercase|lowercase|trim|regex|csharp",
    "pattern": "regex_pattern",  // For regex operation
    "replacement": "replacement_text",  // For regex operation
    "code": "C# code here"  // For csharp operation
  }
}
```

#### **Orchestrator Node**
```json
{
  "id": "orchestrator1",
  "type": "Orchestrator",
  "config": {
    "apiKey": "your-api-key"
  }
}
```

---

### Edge Configuration

**Basic Edge (No Condition):**
```json
{
  "from": "node_a",
  "to": "node_b",
  "fromOutput": "output",
  "toInput": "input",
  "condition": null
}
```

**Conditional Edge:**
```json
{
  "from": "scorer",
  "to": "approve_handler",
  "fromOutput": "output",
  "toInput": "input",
  "condition": "{{scorer.decision}} == 'approve'"
}
```

**Multi-Input Edge (for Aggregation):**
```json
{
  "from": "analyzer1",
  "to": "aggregator",
  "fromOutput": "output",
  "toInput": "analysis_1",  // Unique input name
  "condition": null
}
```

---

## Step-by-Step: Building the Code Review Flow

### Prerequisites

✅ FlowManager application running
✅ OpenAI API key configured
✅ Understanding of JSON structure

---

### Step 1: Plan the Flow Architecture

**Define Stages:**

1. **Input**: Receive code to review
2. **Parallel Analysis**: Security, Style, Testing, Performance (4 nodes)
3. **Aggregation**: Combine all analysis results
4. **Scoring**: Calculate weighted score and make decision
5. **Conditional Handlers**: Approve/Changes/Reject (3 nodes)
6. **Final Output**: Professional notification

**Sketch the Data Flow:**
```
INPUT → [4 Analyzers in Parallel] → AGGREGATOR → SCORER
                                                    ↓
                                    [Conditional Branch: Approve|Changes|Reject]
                                                    ↓
                                              FINAL NOTIFIER
```

---

### Step 2: Create the Base Flow Structure

```json
{
  "id": "advanced-code-review-flow",
  "name": "Advanced Code Review System",
  "description": "Parallel code analysis → Aggregation → Scoring → Conditional approval → Notification",
  "tenantId": "default",
  "nodes": [],
  "edges": [],
  "createdAt": "2025-11-16T11:00:00Z",
  "updatedAt": "2025-11-16T11:00:00Z",
  "lastModified": "2025-11-16T11:00:00Z"
}
```

---

### Step 3: Add Input Node

```json
{
  "nodes": [
    {
      "id": "code_input",
      "type": "Input",
      "config": {
        "text": "{{userInput}}"
      },
      "condition": null
    }
  ]
}
```

**Purpose**: Entry point for code submission

---

### Step 4: Add Parallel Analyzer Nodes

**Security Analyzer:**
```json
{
  "id": "security_analyzer",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "model": "gpt-4o-mini",
    "temperature": 0.3,
    "systemPrompt": "You are a security code reviewer. Analyze the code for security vulnerabilities:\n\n**Check for:**\n- SQL injection vulnerabilities\n- Cross-site scripting (XSS) risks\n- Authentication and authorization issues\n- Sensitive data exposure\n- Insecure cryptography\n- Command injection\n- Path traversal\n- CSRF vulnerabilities\n\n**Return JSON:**\n{\n  \"score\": <0-100>,\n  \"issues\": [\n    \"Issue 1 description\",\n    \"Issue 2 description\"\n  ],\n  \"severity\": \"low|medium|high|critical\"\n}\n\n**Scoring:**\n- 90-100: No issues found\n- 70-89: Minor issues, low severity\n- 50-69: Moderate issues requiring attention\n- 0-49: Critical issues, immediate fix required"
  },
  "condition": null
}
```

**Style Analyzer:**
```json
{
  "id": "style_analyzer",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "model": "gpt-4o-mini",
    "temperature": 0.3,
    "systemPrompt": "You are a code style and quality reviewer.\n\n**Check for:**\n- Naming conventions (variables, functions, classes)\n- Code comments and documentation\n- Code structure and modularity\n- Readability and clarity\n- Best practices for the language\n- DRY principle (Don't Repeat Yourself)\n- SOLID principles adherence\n\n**Return JSON:**\n{\n  \"score\": <0-100>,\n  \"issues\": [\n    \"Issue description\"\n  ],\n  \"suggestions\": [\n    \"Improvement suggestion\"\n  ]\n}\n\n**Scoring:**\n- 90-100: Excellent style, well-documented\n- 70-89: Good with minor improvements needed\n- 50-69: Needs significant style improvements\n- 0-49: Poor style, major refactoring required"
  },
  "condition": null
}
```

**Test Coverage Analyzer:**
```json
{
  "id": "test_analyzer",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "model": "gpt-4o-mini",
    "temperature": 0.3,
    "systemPrompt": "You are a test coverage and quality reviewer.\n\n**Analyze:**\n- Edge cases coverage\n- Happy path testing\n- Error handling tests\n- Boundary condition tests\n- Mock usage appropriateness\n- Test assertions quality\n- Test maintainability\n\n**Return JSON:**\n{\n  \"score\": <0-100>,\n  \"coverage\": \"estimated percentage or assessment\",\n  \"missing_tests\": [\n    \"Test scenario 1\",\n    \"Test scenario 2\"\n  ]\n}\n\n**Scoring:**\n- 90-100: Comprehensive test coverage\n- 70-89: Good coverage with gaps\n- 50-69: Insufficient coverage\n- 0-49: Minimal or no testing"
  },
  "condition": null
}
```

**Performance Analyzer:**
```json
{
  "id": "performance_analyzer",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "model": "gpt-4o-mini",
    "temperature": 0.3,
    "systemPrompt": "You are a performance and optimization reviewer.\n\n**Check for:**\n- Unnecessary loops or iterations\n- Inefficient algorithms (O(n²) when O(n) possible)\n- Memory leaks or excessive memory usage\n- Database query optimization\n- N+1 query problems\n- Caching opportunities\n- Async/await usage\n- Resource cleanup (files, connections)\n\n**Return JSON:**\n{\n  \"score\": <0-100>,\n  \"bottlenecks\": [\n    \"Performance issue description\"\n  ],\n  \"optimizations\": [\n    \"Optimization suggestion\"\n  ]\n}\n\n**Scoring:**\n- 90-100: Highly optimized\n- 70-89: Good with optimization opportunities\n- 50-69: Performance concerns exist\n- 0-49: Serious performance issues"
  },
  "condition": null
}
```

---

### Step 5: Connect Input to Analyzers (Parallel Edges)

```json
{
  "edges": [
    {
      "from": "code_input",
      "to": "security_analyzer",
      "fromOutput": "output",
      "toInput": "input",
      "condition": null
    },
    {
      "from": "code_input",
      "to": "style_analyzer",
      "fromOutput": "output",
      "toInput": "input",
      "condition": null
    },
    {
      "from": "code_input",
      "to": "test_analyzer",
      "fromOutput": "output",
      "toInput": "input",
      "condition": null
    },
    {
      "from": "code_input",
      "to": "performance_analyzer",
      "fromOutput": "output",
      "toInput": "input",
      "condition": null
    }
  ]
}
```

**Result**: All 4 analyzers receive input simultaneously and run in parallel.

---

### Step 6: Add Aggregator Node

```json
{
  "id": "aggregator",
  "type": "Formatter",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "systemPrompt": "Combine all code review analysis outputs into a comprehensive summary.\n\n**Structure:**\n\n# Code Review Analysis Report\n\n## Security Analysis\n{{security}}\n\n## Style & Quality Analysis\n{{style}}\n\n## Test Coverage Analysis\n{{testing}}\n\n## Performance Analysis\n{{performance}}\n\n---\n\n**Provide a clear, structured summary of all findings.**"
  },
  "condition": null
}
```

---

### Step 7: Connect Analyzers to Aggregator

```json
{
  "edges": [
    {
      "from": "security_analyzer",
      "to": "aggregator",
      "fromOutput": "output",
      "toInput": "security",
      "condition": null
    },
    {
      "from": "style_analyzer",
      "to": "aggregator",
      "fromOutput": "output",
      "toInput": "style",
      "condition": null
    },
    {
      "from": "test_analyzer",
      "to": "aggregator",
      "fromOutput": "output",
      "toInput": "testing",
      "condition": null
    },
    {
      "from": "performance_analyzer",
      "to": "aggregator",
      "fromOutput": "output",
      "toInput": "performance",
      "condition": null
    }
  ]
}
```

**Note**: Each edge has a unique `toInput` name matching variables in aggregator's system prompt.

---

### Step 8: Add Score Calculator

```json
{
  "id": "score_calculator",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "model": "gpt-4o-mini",
    "temperature": 0.1,
    "systemPrompt": "Calculate the final code review score based on all analyses.\n\n**Weighting Formula:**\n- Security: 40% (highest priority)\n- Testing: 25%\n- Style: 20%\n- Performance: 15%\n\n**Decision Rules:**\n- **Approve** (final_score >= 85): Code meets all quality standards\n- **Request Changes** (70 <= final_score < 85): Code needs improvements but is salvageable\n- **Reject** (final_score < 70): Code requires major rework\n\n**Critical Override:**\nIf Security severity is 'critical' → Automatically REJECT regardless of overall score\n\n**Return ONLY valid JSON (no extra text):**\n{\n  \"final_score\": <0-100>,\n  \"decision\": \"approve|request_changes|reject\",\n  \"reasoning\": \"Brief explanation of the decision\"\n}"
  },
  "condition": null
}
```

**Connect Aggregator to Score Calculator:**
```json
{
  "from": "aggregator",
  "to": "score_calculator",
  "fromOutput": "output",
  "toInput": "input",
  "condition": null
}
```

---

### Step 9: Add Conditional Handler Nodes

**Approve Handler:**
```json
{
  "id": "approve_handler",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "model": "gpt-4o-mini",
    "temperature": 0.5,
    "systemPrompt": "Generate a professional approval message for code that passed review.\n\n**Tone:** Positive, congratulatory, professional\n\n**Include:**\n- Congratulations on excellent code quality\n- Mention the high score achieved\n- Highlight 2-3 key strengths observed\n- Encourage continued good practices\n\n**Format:** Professional, warm, encouraging"
  },
  "condition": null
}
```

**Changes Requested Handler:**
```json
{
  "id": "changes_handler",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "model": "gpt-4o-mini",
    "temperature": 0.5,
    "systemPrompt": "Generate a constructive change request message.\n\n**Tone:** Constructive, specific, balanced\n\n**Include:**\n- Acknowledge good aspects of the code\n- List required improvements clearly\n- Provide actionable next steps\n- Offer encouragement\n\n**Format as:**\n# Changes Requested\n\n## What Went Well\n- [Positive point 1]\n- [Positive point 2]\n\n## Required Improvements\n☐ [Action item 1]\n☐ [Action item 2]\n☐ [Action item 3]\n\n## Next Steps\n[Clear guidance on how to proceed]"
  },
  "condition": null
}
```

**Reject Handler:**
```json
{
  "id": "reject_handler",
  "type": "OpenAIChat",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "model": "gpt-4o-mini",
    "temperature": 0.5,
    "systemPrompt": "Generate a professional but firm rejection message.\n\n**Tone:** Professional, firm, informative\n\n**Include:**\n- Clear statement that code does not meet standards\n- Explanation of critical issues\n- Guidance on expected quality standards\n- Resources or references for improvement\n\n**Avoid:**\n- Personal criticism\n- Vague feedback\n- Discouragement\n\n**Format:** Professional, direct, educational"
  },
  "condition": null
}
```

---

### Step 10: Add Conditional Edges

```json
{
  "edges": [
    {
      "from": "score_calculator",
      "to": "approve_handler",
      "fromOutput": "output",
      "toInput": "input",
      "condition": "{{score_calculator.decision}} == 'approve'"
    },
    {
      "from": "score_calculator",
      "to": "changes_handler",
      "fromOutput": "output",
      "toInput": "input",
      "condition": "{{score_calculator.decision}} == 'request_changes'"
    },
    {
      "from": "score_calculator",
      "to": "reject_handler",
      "fromOutput": "output",
      "toInput": "input",
      "condition": "{{score_calculator.decision}} == 'reject'"
    }
  ]
}
```

**Important**: Only ONE of these edges will be followed based on the decision.

---

### Step 11: Add Final Notifier

```json
{
  "id": "final_notifier",
  "type": "Formatter",
  "config": {
    "apiKey": "YOUR_API_KEY",
    "systemPrompt": "Create the final code review notification.\n\n**Combine:**\n- Review summary from score calculator: {{score_data}}\n- Handler message (one of): {{approval_message}} {{changes_message}} {{rejection_message}}\n\n**Format:**\n\n═══════════════════════════════════════════\n         CODE REVIEW NOTIFICATION\n═══════════════════════════════════════════\n\n**Final Score:** [score]/100\n**Decision:** [APPROVED/CHANGES REQUESTED/REJECTED]\n\n---\n\n[Handler message content]\n\n---\n\n**Review completed at:** [timestamp]\n**Reviewed by:** FlowManager Code Review System\n\n═══════════════════════════════════════════"
  },
  "condition": null
}
```

---

### Step 12: Connect Everything to Final Notifier

```json
{
  "edges": [
    {
      "from": "approve_handler",
      "to": "final_notifier",
      "fromOutput": "output",
      "toInput": "approval_message",
      "condition": null
    },
    {
      "from": "changes_handler",
      "to": "final_notifier",
      "fromOutput": "output",
      "toInput": "changes_message",
      "condition": null
    },
    {
      "from": "reject_handler",
      "to": "final_notifier",
      "fromOutput": "output",
      "toInput": "rejection_message",
      "condition": null
    },
    {
      "from": "score_calculator",
      "to": "final_notifier",
      "fromOutput": "output",
      "toInput": "score_data",
      "condition": null
    }
  ]
}
```

**Note**: `final_notifier` receives 4 inputs, but only 2 will have data:
- `score_data` (always)
- ONE of the handler messages (based on which branch was taken)

---

### Step 13: Save and Test

**Save the Flow:**
1. Save as `advanced-code-review-flow.json`
2. Place in `Data/Flows/` directory
3. Application auto-discovers it

**Test with Sample Code:**

**Good Code Example:**
```javascript
// POST /api/users - Create new user
async function createUser(req, res) {
  try {
    const { email, name } = req.body;

    // Validate input
    if (!email || !name) {
      return res.status(400).json({ error: 'Email and name required' });
    }

    // Check if user exists
    const existing = await User.findOne({ email });
    if (existing) {
      return res.status(409).json({ error: 'User already exists' });
    }

    // Create user
    const user = new User({ email, name });
    await user.save();

    res.status(201).json({ id: user._id, email, name });
  } catch (error) {
    console.error('Error creating user:', error);
    res.status(500).json({ error: 'Internal server error' });
  }
}
```

**Expected Result**: Approve (Score ~85-90)

**Bad Code Example:**
```javascript
function doStuff(x) {
  var result = eval(x); // SECURITY ISSUE: eval()
  query = "SELECT * FROM users WHERE id=" + x; // SQL INJECTION
  return result;
}
```

**Expected Result**: Reject (Score <50, Critical security issues)

---

## Best Practices & Tips

### System Prompt Best Practices

1. **Be Specific, Not Generic**
   - ❌ "Check for errors"
   - ✅ "Check for: SQL injection, XSS, auth bypass"

2. **Define Output Format First**
   - Start with "Return JSON: {...}"
   - Prevents AI from adding extra text

3. **Use Examples**
   - Show expected output format with examples
   - Clarifies edge cases

4. **Set Expectations**
   - Define what "good" vs "bad" means
   - Provide scoring rubrics

5. **Control Verbosity**
   - "Be concise" vs "Provide detailed explanation"
   - Match tone to use case

---

### Flow Design Best Practices

1. **Name Nodes Descriptively**
   - ✅ `security_analyzer`, `final_notifier`
   - ❌ `node1`, `llm_node`

2. **Use Consistent Naming**
   - Snake_case for node IDs
   - Descriptive suffixes: `_handler`, `_analyzer`, `_checker`

3. **Plan Data Flow First**
   - Sketch on paper before coding
   - Identify parallel opportunities
   - Map all conditional paths

4. **Test Incrementally**
   - Start with 2-3 nodes
   - Add complexity gradually
   - Test each stage

5. **Document with Comments**
   - Use `description` field
   - Explain non-obvious logic

---

### Performance Optimization

1. **Maximize Parallelism**
   - Identify independent operations
   - Run simultaneously when possible

2. **Use Appropriate Models**
   - `gpt-4o-mini` for analysis (faster, cheaper)
   - `gpt-4` for complex reasoning

3. **Set Reasonable Temperatures**
   - Low (0.1-0.3) for consistency
   - Higher (0.5-0.8) for creativity

4. **Limit Token Usage**
   - Set `maxTokens` to prevent runaway costs
   - Use concise prompts

---

### Debugging Tips

1. **Check Execution Order**
   - Look at `ExecutionBatches` in result
   - Verify parallel nodes run together

2. **Inspect Node Outputs**
   - Use `ExecutionContext.NodeOutputs`
   - Check if outputs match expected format

3. **Validate JSON Responses**
   - AI sometimes adds extra text
   - Use "Return ONLY JSON" in prompts

4. **Test Conditions Separately**
   - Temporarily remove conditions
   - Verify node logic works
   - Re-add conditions

---

## Troubleshooting

### Issue 1: Nodes Not Running in Parallel

**Symptoms:** Nodes run sequentially instead of simultaneously

**Causes:**
- Edges create unintended dependencies
- Nodes reference each other's outputs

**Solution:**
```json
// BAD: Creates dependency
{"from": "analyzer1", "to": "analyzer2"}
{"from": "code_input", "to": "analyzer2"}

// GOOD: Both independent
{"from": "code_input", "to": "analyzer1"}
{"from": "code_input", "to": "analyzer2"}
```

---

### Issue 2: Conditional Edge Not Working

**Symptoms:** All branches execute or none execute

**Causes:**
- Wrong variable name in condition
- Output field doesn't exist
- Type mismatch (string vs number)

**Solution:**
1. Check exact output key name from source node
2. Verify output exists in `NodeOutputs`
3. Match types: `'approve'` (string) vs `true` (boolean)

---

### Issue 3: Aggregator Missing Inputs

**Symptoms:** Some `{{variables}}` are empty in aggregator

**Causes:**
- Edges don't specify unique `toInput` names
- Source nodes didn't execute

**Solution:**
```json
// Ensure each edge has unique toInput
{"from": "node1", "to": "aggregator", "toInput": "input1"}
{"from": "node2", "to": "aggregator", "toInput": "input2"}
```

---

### Issue 4: AI Returns Non-JSON

**Symptoms:** Node outputs contain text before/after JSON

**Causes:**
- Prompt doesn't emphasize JSON-only
- Temperature too high

**Solution:**
```json
{
  "systemPrompt": "Return ONLY valid JSON with no additional text:\n{...}",
  "temperature": 0.1
}
```

---

### Issue 5: Flow Hangs/Times Out

**Symptoms:** Flow never completes

**Causes:**
- Circular dependencies
- Missing edges (node has no path to completion)

**Solution:**
- Use DAG validator (automatically runs)
- Ensure all nodes have path to an end node

---

## Conclusion

You now have a complete understanding of:

✅ **System Prompt Crafting**: Role, Task, Constraints, Format, Tone
✅ **Parallel Execution**: Fan-out, fan-in, batching
✅ **Conditional Branching**: Operators, multi-way routing
✅ **Advanced Patterns**: Quality gates, hierarchical classification, voting
✅ **JSON Structure**: Nodes, edges, conditions
✅ **Real-World Example**: Complete code review flow from scratch

---

**Questions? Issues?**

Refer to:
- `USER_GUIDE.md` - Basic flow creation
- API documentation - Node type reference
- Example flows in `Data/Flows/`


