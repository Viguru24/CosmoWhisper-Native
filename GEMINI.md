# CosmoWhisper-Native — Antigravity Rules

## Master Workspaces (STRICT ADHERENCE REQUIRED)
- **Native Windows App (MASTER)**: `c:\CosmoWhisper-Dev-Restore\CosmoWhisper-Windows`
- **Project Documentation & Backups**: `c:\CosmoWhisper-Dev-Restore`
- **Backend & Frontend (Web)**: `c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-App`

## Project
This is the CosmoWhisper Windows native application — the active development occurs in the `CosmoWhisper-Dev-Restore` workspace to maintain audio fidelity and premium UI state.

## Mandatory Workflow: Planning with Files (Manus-style)

For every task that involves more than a single file edit, you MUST follow the planning-with-files skill located at `.gemini/skills/planning-with-files/SKILL.md`.

### The non-negotiable rules:
1. **Create `task_plan.md` BEFORE writing any code** — no exceptions
2. **Create `findings.md` and `progress.md`** at the start of every session
3. **Log every error** to the plan file — never retry silently
4. **3-Strike protocol** — after 3 failures, stop and ask Louis
5. **Talk before acting** — state your plan to Louis before executing it
6. **Never run off and do things** — discuss first, implement after confirmation

### The 3 planning files always live in the repo root:
- `task_plan.md` — what we're doing and phase progress
- `findings.md` — research, discoveries, technical decisions
- `progress.md` — session log, test results, error log

## Key People
- **Louis** — the developer and owner of this project
- **Antigravity** — the AI coding assistant (you)

## Backend
- Node.js backend located at: `c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-App\backend`
- Runs locally on `http://localhost:5000`

## Remember
You are always working locally. Never deploy to production or push to GitHub unless Louis explicitly asks.
