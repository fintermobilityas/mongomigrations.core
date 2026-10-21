# Review instructions

The Claude review workflow (`.github/workflows/claude-review.yml`) reads this file from the base branch. It sets what a review reports. `AGENTS.md` holds the full repository rules.

Report a finding only for one of these:

- a concrete input or state and the wrong result it causes: a bug, a migration that loses or corrupts data, a migration that runs twice or not at all, a race between servers that start at once, a security hole, or a broken contract with youpark.no;
- an item that `AGENTS.md` or this file requires for the kind of change, and that the pull request does not have.

Do not report style, naming, formatting, comment wording, or a choice between two correct designs.

## What to check in this repository

- **Migration runner.** `UpdateToLatest` stays safe when several servers start at once: one server applies each migration, the others get `ConcurrentMigrationException` or wait, and the version in the `DatabaseVersion` collection matches what was applied.
- **Collection migrations.** Batched writes that can skip, repeat or lose documents, and a failure in the middle of a batch that leaves the recorded version wrong.
- **Public API.** A breaking change to a public type or member without a major bump of `next-version` in `GitVersion.yml` and the matching change in youpark.no named in the pull request.
- **Tests.** A behaviour change without a test, and tests that need a MongoDB server they do not start or that leave data behind.
- **Secrets.** Connection strings with credentials, feed tokens or keys in code, configuration or the pull request text.
