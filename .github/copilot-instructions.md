# Copilot Instructions

## General Guidelines
- Every merge operation should follow a retry×3 strategy.
- Automatically skip if already merged (merge-base check).
- Use `--no-edit` for pulls/merges.
- Prompt the user to resolve conflicts or abort.
- Present a final summary of successes, skips, and failures.