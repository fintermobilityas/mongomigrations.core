# Cloud worker

The development profile uses 4 vCPU and 8 GB RAM. It is a CPU-only library
workspace with Claude and Codex; no browser or desktop is required.

The Dockerfile follows `.github/workflows/dotnet-core.yml`:

- `DOTNET_100_SDK_VERSION=10.0.x` resolves to SDK 10.0.401.
- `shell: pwsh` is provided by PowerShell 7.6.6 (CI leaves it to the runner).
- `GITVERSION_VERSION=6.4.0` is installed ahead of time; the unchanged `setup`
  job updates the user-global tool and emits the build job's `BUILD_VERSION`.
- The MongoDB matrix's `8.0` resolves to MongoDB 8.0.32 for Ubuntu 24.04.
- localci 0.1.0 is the latest release at setup time; its musl archive is checked
  against the release's SHA256SUMS, which is also checked against its digest.

Toolchain PATH and .NET settings are applied to SSH sessions through
`/etc/horizon-worker/session-env.d/20-mongomigrations.core.env`.

Run the selected Linux CI jobs on the worker:

```sh
localci run -w dotnet-core.yml --event push horizon-pins setup build
localci run --fast -w dotnet-core.yml --event push horizon-pins setup build
```

The localci wrapper starts MongoDB on localhost:27017 as the session user,
with data and logs under `$HOME/.local/state/mongomigrations-mongodb`.
This replaces the skipped Docker-based MongoDB setup action. To run the
repository build script directly, first run `horizon-mongomigrations-mongodb`,
then `pwsh ./build.ps1 -RunTests`. Concurrent full test runs should be serialized
because the fixtures share `MongoMigrationsUnitTests`.

localci skips checkout and setup-dotnet actions; Horizon supplies the checkout
and the image supplies the SDK. Proof selects `horizon-pins`, `setup` and `build`
on push. Horizon generates `.horizon/verify-pins.py`; commit it with the setup.
The verifier checks SDK and MongoDB version prefixes and the exact GitVersion
version against the workflow using `.horizon/pins.json`.
Publishing, release creation, and trusted publishing verification are excluded;
no credentials are stored in these files or the image.

The existing development profile is preserved. The company product map identifies
`youpark.no` as a package consumer with `check_together: true`, but this repository's
AGENTS.md does not require running its tests, so no same-worker companion is added.
There are no qualifying runtime contracts; `company` supplies maintenance docs.
