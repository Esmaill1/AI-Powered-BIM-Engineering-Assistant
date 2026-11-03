# RuleCheck — AI-Assisted Revit Model QA/QC Checker

> **Convert unstructured BIM requirements into automated, deterministic model checks — powered by LLM ingestion, grounded against live Revit model schema.**

[![Revit 2027](https://img.shields.io/badge/Revit-2027-blue?style=flat-square)](https://www.autodesk.com/products/revit/)
[![.NET 10](https://img.shields.io/badge/.NET-10.0-purple?style=flat-square)](https://dotnet.microsoft.com/)
[![License: Academic](https://img.shields.io/badge/License-Academic-green?style=flat-square)](#license)

---

## Overview

**RuleCheck** is a Revit 2027 add-in that automates BIM model QA/QC by:

1. **Ingesting** natural-language requirements from BIM Execution Plans (BEP) and Employer's Information Requirements (EIR)
2. **Translating** them into structured, executable validation rules via a grounded LLM pipeline (Google Gemini)
3. **Executing** those rules deterministically against an extracted model snapshot
4. **Highlighting** failing elements directly in Revit's 3D viewport with click-to-zoom navigation

The system is **strictly read-only** — it never modifies the BIM model.

---

## Architecture

```
[Revit 2027 Process]
  │
  ├── RuleCheck.RevitApp
  │     ├── WebView2 Frontend (HTML/CSS/JS, bidirectional postMessage bridge)
  │     ├── SnapshotExtractor ──► ModelSnapshot (Memory / JSON)
  │     └── ShowElementHandler ◄── Issue Element IDs (via ExternalEvent)
  │
  ─────────────────────── Assembly Boundary ───────────────────────
  │
  [Pure .NET 10 Class Libraries — Zero Revit Dependencies]
  │
  ├── RuleCheck.Core       Domain entities, ModelSnapshot, Issue models
  ├── RuleCheck.Engine     Deterministic rule execution & condition evaluators
  ├── RuleCheck.Schema     JSON Schema validation & rule serialization
  ├── RuleCheck.LLM        (Planned) LLM client, prompts, vocabulary grounding
  └── RuleCheck.Evaluation (Planned) CLI test harness & metric calculators
```

### Key Architectural Decisions

| Decision | Rationale |
|---|---|
| **WebView2 (Chromium) UI** | Hot-reload without restarting 10GB Revit; process isolation prevents UI crashes from affecting the host; modern HTML/CSS/JS stack for rapid iteration |
| **Headless Engine** | Pure .NET 10 libraries with zero Revit API references enable 100% reproducible unit testing and CLI evaluation |
| **ExternalEvent Bridge** | All Revit API calls route through a thread-safe ExternalEvent queue — mandatory for Revit's single-threaded COM architecture |
| **Snapshot Architecture** | Model data is extracted once into an immutable snapshot; rules evaluate against the snapshot, not the live model |

---

## Project Structure

```
├── src/
│   ├── RuleCheck.Core/            # Domain models (Snapshot, Issues, Rules, Vocabulary)
│   │   ├── Issues/                # Issue entity definitions
│   │   ├── Json/                  # JSON serialization helpers
│   │   ├── Rules/                 # RuleDefinition, RuleSet, RuleValue
│   │   ├── Snapshot/              # ModelSnapshot (elements, parameters, categories)
│   │   ├── Units/                 # UnitConverter for Revit internal units
│   │   └── Vocabulary/            # Parameter vocabulary for LLM grounding
│   │
│   ├── RuleCheck.Engine/          # Deterministic rule execution engine
│   │   ├── Evaluators/            # Check evaluators (7 types)
│   │   ├── RuleEngine.cs          # Main orchestrator
│   │   ├── ConditionEvaluator.cs  # Where-clause filter evaluation
│   │   └── EvaluationContext.cs   # Element-parameter resolution context
│   │
│   ├── RuleCheck.Schema/          # JSON Schema validation
│   │
│   └── RuleCheck.RevitApp/        # Revit 2027 add-in (WebView2 dockable panel)
│       ├── App.cs                 # IExternalApplication entry point
│       ├── Commands/              # Ribbon button commands
│       ├── Handlers/              # ShowElementHandler (zoom-to-element)
│       ├── Services/              # SnapshotExtractor (Revit API → ModelSnapshot)
│       ├── Views/                 # WPF dockable panel shell
│       ├── Web/                   # WebView2 HTML/CSS/JS frontend
│       └── RuleCheck.addin        # Revit manifest
│
├── tests/
│   ├── RuleCheck.Engine.Tests/    # Unit tests for all 7 evaluators
│   └── RuleCheck.Schema.Tests/    # Schema validation tests
│
├── schemas/
│   └── rule.schema.json           # Canonical rule schema (v1)
│
├── PRD_Revit_QAQC_Checker.md     # Product Requirements Document
├── initial-plan.md                # Initial project plan
├── RuleCheck.sln                  # Solution file
└── Directory.Build.props          # Shared MSBuild properties
```

---

## Supported Rule Types

The engine supports **7 deterministic check types**:

| Check Type | Description | Example |
|---|---|---|
| `required_parameter` | Parameter must exist and be non-empty | *"All doors must have FireRating specified"* |
| `pattern` | Parameter value must match a regex | *"Door marks must follow `DR-[0-9]{3}`"* |
| `allowed_values` | Parameter value must be in a whitelist | *"FireRating must be 30, 60, 90, or 120"* |
| `range` | Numeric parameter must be within min/max bounds | *"Sill height between 800mm and 1200mm"* |
| `unique` | Parameter values must be unique within a scope | *"All door marks must be unique per level"* |
| `placement` | Element must be assigned to a room or level | *"All rooms must be placed on a level"* |
| `not_unused` | Element type must have at least one placed instance | *"No unused door families in the model"* |

Each rule supports optional **where-clause filters** (AND-based) to narrow the target scope.

---

## Prerequisites

- **Revit 2027** (targets .NET 10)
- **.NET SDK 10.x**
- **Visual Studio 2026** or **JetBrains Rider**

---

## Building

```bash
# Restore and build the solution
dotnet build RuleCheck.sln

# Run unit tests (headless — no Revit required)
dotnet test
```

### Installing the Revit Add-in

1. Build `RuleCheck.RevitApp`
2. Copy the output DLLs and `RuleCheck.addin` to:
   ```
   %APPDATA%\Autodesk\Revit\Addins\2027\
   ```
3. Launch Revit 2027 → look for the **"RuleCheck QA/QC"** ribbon tab

---

## How It Works (User Workflow)

1. **Ingest** — Paste BEP text or import an Excel/CSV parameter matrix
2. **Translate** — AI parses requirements into structured rules, grounded against the model's live parameter vocabulary
3. **Review** — Inspect each rule with its verbatim source quote; unresolved or inexpressible clauses are flagged
4. **Execute** — Run approved rules; the engine evaluates the snapshot deterministically
5. **Navigate** — Click any issue to zoom Revit's 3D view directly to the failing element
6. **Export** — Save issues as CSV, rule sets as JSON for reuse

---

## Frontend Approach: WebView2

The add-in UI is built with **Microsoft WebView2** (Chromium) embedded in a WPF dockable panel:

- **Hot Reload**: Edit `Web/panel.html` → click 🔄 Reload — no Revit restart needed
- **Process Isolation**: Chromium runs in a separate process; UI bugs cannot crash Revit
- **Bidirectional Bridge**: `window.chrome.webview.postMessage()` ↔ C# `WebMessageReceived` event
- **Native Revit Integration**: Click an issue → `UIDocument.ShowElements(elementId)` zooms and selects the element in the 3D viewport

---

## Academic Context

This project is a **capstone/thesis deliverable** for a 4-person student team. The primary goal is **academic rigor and defensibility** — the software serves as a vehicle for empirical research evaluating LLM-driven rule translation against manually authored gold-standard rules.

Key evaluation metrics:
- Expressibility coverage, classification accuracy, behavioral rule match
- Issue-level precision, recall, and F₁ against benchmark models with injected errors
- LLM variance across repeated runs (temperature = 0)
- Cohen's κ inter-annotator agreement on gold labels

See [`PRD_Revit_QAQC_Checker.md`](PRD_Revit_QAQC_Checker.md) for the full product requirements document.

---

## License

Academic use. All rights reserved.
