# CosmoWhisper-Native — Antigravity 2.0 Rules

## Master Workspaces
- **Native Windows App (MASTER)**: `C:\Users\louis\Documents\GitHub\CosmoWhisper-Native\CosmoWhisper-Windows`
- **Project Documentation & Backups**: `C:\Users\louis\Documents\GitHub\CosmoWhisper-Native`
- **Backend & Frontend (Web)**: `C:\Users\louis\Documents\GitHub\CosmoWhisper-App`

## Project
This is the CosmoWhisper Windows native application — active development occurs in the `CosmoWhisper-Native` workspace.

## Workflow & Execution
1. **Local-First:** Always work locally (`localhost:5000` backend).
2. **Native Artifact Planning:** For multi-component or architectural changes, formulate a plan using Antigravity Native Artifacts (`RequestFeedback: true`) or `/plan`. Do NOT dump temporary planning markdown files (`task_plan.md`, `findings.md`, `progress.md`) in the repository.
3. **Circuit-Breaker:** After 3 consecutive failed test or build attempts, stop and consult Louis with the exact error diagnosis.
4. **Clean Git Tree:** Keep the repository clean of scratch files and memory dumps.

## Backend
- Node.js backend located at: `C:\Users\louis\Documents\GitHub\CosmoWhisper-App\backend`
- Runs locally on `http://localhost:5000`
