# AGENTS.md

Guidelines for AI agents working with this repository.

**See also**: [CLAUDE.md](CLAUDE.md) for technical development guidance, build commands, and code standards.

## Git Safety Rules

**CRITICAL**: Never run destructive git commands without explicit user request:

### Forbidden Commands (unless user explicitly requests)

- `git reset --hard` - wipes uncommitted changes
- `git checkout HEAD -- <file>` - discards file changes
- `git clean -fd` - removes untracked files
- `git push --force` or `git push -f` - rewrites remote history
- `git rebase` on shared branches
- `git stash drop` without confirmation
- `git stage` or `git add` - stages changes
- `git commit` - creates commits
- `git pull` - fetches and merges from remote

### Safe Git Operations

- `git status` - check current state
- `git diff` - view changes
- `git log` - view history
- `git fetch` - fetch remote refs

### When to Create Commits

**NEVER** create commits automatically after making changes. You MUST only create commits when the user explicitly asks (e.g., "Commit the change"). This is a hard rule.

### Commit Message Guidelines

- Draft messages from staged changes
- Follow existing commit style (check `git log`)
- Never include secrets or credentials

## Decision Making

**Always ask before implementing**:
- New features or functionality
- UI/UX changes
- Configuration modifications
- Any non-trivial changes

If in doubt, present options and let the user decide.

## Build & Test Workflow

1. Make code changes
2. Run `./src/Scripts/Prebuild.ps1`
3. Build and test if required. **Ensure all tests are pure unit tests**. Mock all OS-level dependencies (Registry, File System, Process).
4. Stop - DO NOT create git commits automatically

## Key Takeaway

Ask before running any git command that modifies state, deletes data, or is irreversible.
Ask before implementing new features or making changes.
