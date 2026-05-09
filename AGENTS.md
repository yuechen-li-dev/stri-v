# AGENTS.md

Stri-V is a hard fork of the Stride Engine. The original Stride code contained within the repository should be considered legacy content used for reference only. See /striv folder for the current project workspace.

## Dominatus

Read ARCHITECTURE.md and AUTHORING_GUIDE.md under /striv/external/Dominatus/ as well as samples under /striv/external/Dominatus/samples for Dominatus usage and style guidance, in addition to vendored source code.

## Convergence rule

Every substantial task must end in exactly one of three states:

1. **Success**  
The intended capability works in the real path and the real motivating case materially improves.
2. **Meaningful progression**  
The capability is not complete, but one genuine blocker is removed and the next blocker is isolated with evidence.
3. **Honest stop**  
Further work would require overbroad scope expansion, excessive debt, brittle patching, or tangled logic. Stop and report the reason with concrete evidence.

Do not continue producing patches once the work stops converging.

Do not confuse activity with progress.
A failed attempt is only acceptable if it leaves behind a narrower problem, stronger evidence, or a justified stop.

Any partial work must leave the codebase in a cleaner, more legible, and more diagnosable state than before.
