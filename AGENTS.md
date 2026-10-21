# Repository guidelines

A .NET library for typed MongoDB schema and data migrations: each migration is a numbered C# class, and a runner applies the missing ones at app startup, safely when several servers start at once. Used by youpark.no.

Build, test and release instructions: `README.md`.

## Claude review

Claude reviews every pull request through `.github/workflows/claude-review.yml`, with at most 2 rounds per PR. Never request Copilot or another bot reviewer. This rule replaces the Copilot reviewer steps in the shared company procedure for this repository.

- Round 1 runs automatically when a ready PR from a branch of this repository opens. A pull request from a fork gets a review only after a maintainer has examined its diff and added the `claude-review` label. Its summary comment ends with `<!-- claude-review head=<sha> findings=<n> -->` and covers only that head.
- Fix or answer every finding on the same PR and push. Wait until `gh api repos/fintermobilityas/mongomigrations.core/pulls/<n> -q .head.sha` shows the new head, then add the `claude-review` label (`gh pr edit <n> --add-label claude-review`). That runs round 2, and the job removes the label when it finishes.
- Do not request a third round. After round 2, fix or answer every finding; the PR then merges on green CI.
- `.github/review-instructions.md` sets what the review reports. Put review rules there, not in a reviewer-specific file.

<!-- company-products:begin -->
## Company and products

`mongomigrations.core` is one of Finter Mobility's repositories. Before changing anything that another repo depends on, read the shared map in the private `fintermobilityas/company` repo, checked out beside this one as `../company`:

- What every repo does and how they connect: `../company/docs/products.md` (machine-readable: `../company/docs/products.json`).
- This repo's page: `../company/docs/products/mongomigrations.core.md`.
- Shared maintenance rules (Dependabot, .NET, MongoDB, PRs, native packages, rollout order): `../company/AGENTS.md`.

Companions of `mongomigrations.core`:

- Uses: `company` (shared maintenance procedures).
- Used by: `youpark.no` (MongoMigrations.Core).

When you change a contract shared with a companion (API, package, model files, fixtures, deploy order), check that repo too and update `../company/docs/products.json` if the link itself changes.
<!-- company-products:end -->
