# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.1.0] - 2026-09-11

First public release. Extracted from a private monorepo, relicensed under MIT, and renamed
from `QGCore.Telegram`.

It stays below 1.0 deliberately: the framework runs in production, but the payments surface
had no tests at all until this release and deserves exercising by someone other than its
author before the API is frozen.

### Breaking

- Every namespace, package and assembly renamed from `QGCore.Telegram.*` to `BotForge.*`.
- **`QGCore.Telegram.Templates.Localization` is gone.** It bound the template engine to one
  specific private localization library. `ILocalizationKeyResolver` still lives in
  `BotForge.Templates` — implement it against whatever holds your translations and register
  it. That is a better public API than a hard binding, and it keeps a localization stack out
  of your dependency graph.
- The `dotnet new` template short names are now `botforge-bot` and `botforge-handler`.

### Fixed

- **The `dotnet new` template pack shipped empty.** Its content glob used backslashes
  (`templates\**\*`), which matches nothing on Linux, where the package was built.
- **A PackageId collision.** The template pack declared `<PackageId>QGCore.Telegram.Templates</PackageId>`
  — the same id as the real template-engine library. Two different projects packed to one
  package id. The template pack is now `BotForge.Templates.DotNet`.
- **Every example bot was a published package.** `examples/Directory.Build.props` never set
  `IsPackable=false`, so `dotnet pack` produced `EchoBot.nupkg`, `CryptoShop.nupkg` and
  thirteen more, plus the two service hosts and the development tools. Packing is now opt-in,
  and CI asserts the exact set of 21 packages.
- **`BotForge.TestUtilities` pulled xUnit into consumers.** It referenced xunit without using
  a single type from it. The reference is gone; the package is assertion-framework agnostic.
- A rate-limit test fixture registered its 429 response at a *lower* precedence than the
  default 200 mapping — in WireMock a lower priority number wins — so the override never
  applied and `Mock_SetupRateLimitResponse_Returns429` had never passed.
- `ChannelQueueBackend` owned a `CancellationTokenSource` it never disposed. It now
  implements `IDisposable`.
- `InMemoryRateLimitStore` and `TelegramBotClientProvider` implemented `Dispose` without
  suppressing finalization.
- `ArgumentOutOfRangeException` was thrown with a property name where a parameter name
  belongs, so the exception pointed at the wrong thing.
- `ConfigureAwait(false)` is applied throughout `src/`, and public entry points validate
  their arguments (CA2007 and CA1062, both build errors from here on).
- Culture-sensitive formatting and comparison replaced with invariant equivalents (CA1305,
  CA1310).

### Added

- MIT license. The source repository claimed PolyForm Strict — source-available, not open
  source — and shipped no `LICENSE` file at all.
- **`tests/BotForge.Payments.Tests`: 37 tests where there were none.** `BotForge.Payments` is
  5,245 lines that decide whether money moves and had no test project. Covers the payload
  serializer (every downstream decision keys off parsing it), the anti-fraud pipeline
  (short-circuiting on first failure, metrics for the failing check, cancellation) and the
  pre-checkout pipeline (approve, reject, answer-exactly-once, and still answering when a
  validator throws — an unanswered query leaves the payment hanging).
- Package metadata on all 21 packages: description, tags, per-package README, symbol packages
  and SourceLink. MinVer tag-based versioning replaces a hardcoded `<Version>` and a set of
  version-bumping shell scripts.
- .NET analyzers at `Recommended` with `TreatWarningsAsErrors`, and an `.editorconfig` that
  explains every deliberately loosened rule.
- `tests/BotForge.Docs.Snippets`, which compiles every code sample in `README.md`.
- CI on Linux and Windows with coverage, a separate job that runs the integration suite
  against real RabbitMQ and Redis containers, and a pack job that asserts the exact package
  set. CodeQL, gitleaks, Dependabot, `SECURITY.md`, `CODEOWNERS` and templates.
- Release via NuGet Trusted Publishing (OIDC) behind a manual environment gate — no
  long-lived API key.

### Security

- **A live Telegram bot token was committed** in the API probe's `appsettings.json`, along
  with real private and group chat ids and 108 committed probe reports. All removed before
  the first commit of this repository, so none of it is in this history. The token itself
  must be revoked via BotFather — removing it from the source does not invalidate it.
- Three transitive dependencies carrying published advisories are pinned in
  `Directory.Packages.props` with their GHSA ids: `Scriban.Signed` (high),
  `SQLitePCLRaw.lib.e_sqlite3` (high) and `SSH.NET` (high). Vulnerability warnings are not
  suppressed — `NuGetAudit` runs in `all` mode at `low` level with warnings as errors.

### Known limitations

- `BotForge.Observability` depends on OpenTelemetry's Prometheus exporter, which has never
  had a stable release. NU5104 is suppressed with an explanation rather than silently.
- `VelocityCheck` is a hook, not a check: without a payment store it always passes. This is
  documented and covered by a test so that making it real is a visible change.
- `BotForge.Hosting`, `BotForge.Consumer`, `BotForge.Observability`, the rate-limiting
  packages and the development tools under `tools/` still have no dedicated test projects.

[Unreleased]: https://github.com/deniscuciuc/botforge/compare/v0.1.0...HEAD
[0.1.0]: https://github.com/deniscuciuc/botforge/releases/tag/v0.1.0
