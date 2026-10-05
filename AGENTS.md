# Repository guidelines

A .NET library for typed MongoDB schema and data migrations: each migration is a numbered C# class, and a runner applies the missing ones at app startup, safely when several servers start at once. Used by youpark.no.

Build, test and release instructions: `README.md`.

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
