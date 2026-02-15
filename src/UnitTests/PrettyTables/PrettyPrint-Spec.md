
# PrettyPrint Specification (Lean Refinement)

### Core Objectives
* **Human Readable**: Clean alignment, GFM-standard delimiters.
* **Lean API**: Intent-based naming (`Vertical`, `Inline`, `Table`).
* **AI Ready**: Predictable structure, alignment hints, and contextual headers.

---

## API Surface

### 1. Headers (Context)
Headers provide the "Anchor" for both humans and AI agents.
* `Title`: Rendered as `### Title` (H3).
* `Section`: Rendered as `#### Section` (H4).
* Automatically adds leading/trailing empty lines for Markdown compliance.

### 2. Rendering Modes (Visual Naming)

| Method | Scope | Output Style | Best For |
|:---|:---|:---|:---|
| **`PrintMarkdownVertical`** | POCO / Dict | 2-column table | High-density records (Config, Regimes) |
| **`PrintMarkdownInline`** | POCO / Dict | Pipe-separated line | Tight console logs, status updates |
| **`PrintMarkdownTable`** | DataTable | N-column grid | Historical returns, transition matrices |

---

## Input Formats & Transcription (The Bridge)
Centering rendering around `DataTable` internally ensures feature parity (alignment hints, formatting). We provide transcription bridges to "feed" the engine:

| Input Type | Inference Logic | Notes |
|:---|:---|:---|
| **Anonymous / POCO** | Reflection (Shallow) | Top-level primitives/strings only |
| **Dictionary** | Keys as Labels | Bypasses C# naming restrictions |
| **TwoDMatrix** | 2D Grid | Explicit row/col label support |
| **Span<double>** | 1D Vector | Indexed or custom labeling |

---

## Design Decisions
* **One Standard**: Dropped legacy box-drawing in favor of GFM Markdown.
* **No Enums**: Replaced `MarkdownMode` with explicit method names for better discoverability.
* **Shallow Reflection**: Accepts complex graphs but **does not crawl**.
* **Internal Engine**: `DataTable` is the master engine but is hidden for record-level API calls.

---

## Technical Patterns
* **Self-Contained**: Zero external dependencies.
* **Allocation Efficient**: Uses `AsSpan()` slicing for dash pools.
* **Flexible Sinks**: Targets `TextWriter` (Console, File, StringBuilder).

## Sample Tall Record

| Property     | Value        |
|:-------------|:-------------|
| NumClusters  |            5 |
| Algorithm    | K-Means++    |