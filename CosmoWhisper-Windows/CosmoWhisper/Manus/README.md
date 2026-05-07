# MANUS FOLDER — CONTEXT FOR ANTIGRAVITY

## What Is This?

This folder contains the **Manus Agent** — an autonomous, planning-first AI sub-system built into the CosmoWhisper Windows app.

There are TWO separate but related things called "Manus" in this project:

---

## 1. Manus Inside the App (This Folder)

`ManusAgent.cs` is an AI agent that **CosmoWhisper users** can trigger via voice or text.  
It is powered by `AIService`, `PlanningTool`, and `LocalFileOperator`.  
It manages task plans, logs steps, and runs commands on the user's behalf.

**Integration points:**
- Voice input → `AudioRecorder` → `CommandController` → if not handled → `ManusAgent.Shared.ProcessTask()`
- UI subscribes to `ManusStatusChanged` and `ManusResponseReceived` events
- Plans are persisted in `%AppData%/CosmoWhisper/plans.json`

---

## 2. Antigravity Behaving AS Manus (The Development Workflow)

When **Antigravity** (the AI coding assistant, i.e. me) works with Louis on this project, I am supposed to operate in **Manus mode**:

### The Rules:
1. **Plan before coding** — Before writing any code, update `MANUS_PLAN.md` in the repo root with a clear task plan
2. **Markdown = working memory** — Log every decision, outcome, and error to markdown files on disk
3. **Three-strike error correction** — If a build or test fails: diagnose → fix → retry. Max 3 attempts before stopping and asking Louis
4. **Talk first, act second** — Never run off and do things. State the plan, confirm with Louis, then execute
5. **Ownership** — All context lives on disk, readable and editable by Louis at any time

### Why This Matters:
The Antigravity + Manus workflow (popularised by Julian Goldie SEO) turns Antigravity from a code assistant into an **autonomous project manager** — one that plans, remembers, self-corrects, and collaborates rather than just executing commands blindly.

---

## Current State

See `MANUS_PLAN.md` in the repo root for the live task plan and progress log.

---

## Key Files

| File | Purpose |
|------|---------|
| `Manus/ManusAgent.cs` | In-app Manus agent for CosmoWhisper users |
| `Planning/PlanningTool.cs` | Plan CRUD with JSON persistence |
| `File/FileOperator.cs` | File read/write + shell command execution |
| `MANUS_PLAN.md` (repo root) | Live task plan — Antigravity's working memory |
| `Desktop/Manus_Universal_Integration.md` | Integration guide for deploying Manus to any project |
