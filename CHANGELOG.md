# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

## [0.2.0] - 2026-09-11

### Breaking

- **`VelocityCheck` now enforces its limits instead of always passing.** It was a stub: it
  returned `Pass()` unconditionally, so a deployment that registered it believed payouts were
  rate-limited when nothing was checked. It now counts recent payouts and rejects over the
  hourly or daily limit, and **fails rather than passes when no `IPaymentStore` is
  registered** — silently approving everything because the store is missing is the worst
  outcome for a control that exists to stop money leaving.
- `IPaymentStore` gains `CountPayoutsSinceAsync(userId, since, ct)`, the query `VelocityCheck`
  needs. Implementers must add it.
- `AmountLimitCheckOptions.MaxDailyTotal` is removed. It was never read — a knob that looked
  like a spending cap and enforced nothing. A daily cap belongs with a store-backed check.

### Changed

- Dependencies brought current, including `Telegram.Bot` 22.10.3, `YamlDotNet` 18.1.0,
  `Testcontainers` 4.15.0, `WireMock.Net` 2.15.0, `Spectre.Console` 0.57.2,
  `ModelContextProtocol` 2.2.0 and `Microsoft.Extensions.*` 10.0.12.
- **MassTransit is pinned to 8.5.8 deliberately.** Version 9 dropped Apache-2.0 for a
  proprietary licence, the same move MediatR made at v13. 8.5.8 is the last Apache-2.0
  release. Do not let a dependency update cross that line without a decision.
- `EditMessageText` is now called with named arguments. Telegram.Bot 22.10 inserted a
  parameter into the middle of that overload; positional arguments had bound silently to the
  wrong ones.
- The Prometheus exporter replaced `UriPrefixes` with `Host`/`Port` plus a
  `ConfigureHttpListener` callback. `TelegramObservabilityOptions.PrometheusUriPrefixes` is
  unchanged and now maps onto the callback, because prefixes express things `Host`/`Port`
  cannot, such as binding every interface with `+`.

### Added

- Six tests for `VelocityCheck` covering both windows, the exclusive-limit boundary, the
  fail-closed path, and that the hourly window is checked before the daily one.

## [0.1.0] - 2026-09-11

Initial release. A Telegram bot framework for .NET across 21 packages: attribute routing, a
two-way middleware pipeline, YAML message templates, and a payments stack covering Telegram
Stars, TON and Fragment.

**This release is deliberately below 1.0.** The framework runs in production, but the
payments surface is large and this is its first public outing; the API stays unfrozen until
it has been exercised by someone other than its author.

### Architecture

Two independent pipelines with a queue between them: updates arrive through
`TeleForge.Consumer` onto a bounded channel and are routed by `TeleForge.Routing`; replies
leave through `TeleForge.Messaging` via a queue backend and a send pipeline. That queue is
what lets the sender run as a separate process — the handler code is identical either way.

Everything is keyed by `botId`, so one process can serve many bots.

Localization is a seam, not a dependency: `TeleForge.Templates` resolves `{@key}` tokens
through `ILocalizationKeyResolver`. Implement it against whatever holds your translations.
Nothing is bundled, so TeleForge does not pull a localization stack into your dependency graph.

### Build, CI and release

- .NET analyzers at `Recommended` with `TreatWarningsAsErrors`. `ConfigureAwait(false)`
  throughout `src/` (CA2007), argument validation on every public entry point (CA1062), and
  invariant formatting and comparison (CA1305, CA1310) are all build errors. Deliberate
  exceptions are documented in `.editorconfig` — `TelegramUpdateDelegate` and a `next`
  parameter are kept because they are the middleware convention everyone knows from ASP.NET
  Core.
- MinVer derives the version from the git tag. Symbol packages, SourceLink and deterministic
  CI builds; per-package README, description and tags.
- CI builds and tests on Linux and Windows with coverage, runs the integration suite against
  real RabbitMQ and Redis containers, and asserts the exact set of 21 packages so an example
  bot or a test project cannot silently become published.
- `tests/TeleForge.Docs.Snippets` compiles every code sample in `README.md`.
- Releases are tag-driven and publish through NuGet Trusted Publishing (OIDC) behind a manual
  environment gate — no long-lived API key.
- CodeQL, gitleaks and Dependabot are enabled.

### Security

`NuGetAudit` runs in `all` mode at `low` level with warnings as errors. Three transitive
dependencies carrying published advisories are pinned in `Directory.Packages.props` with
their GHSA identifiers: `Scriban.Signed` (GHSA-24c8-4792-22hx, high),
`SQLitePCLRaw.lib.e_sqlite3` (GHSA-2m69-gcr7-jv3q, high) and `SSH.NET`
(GHSA-q939-rpr3-3284, high).

### Testing

259 tests across seven suites. `tests/TeleForge.Payments.Tests` covers the payload serializer
(every downstream decision keys off parsing it), the anti-fraud pipeline (short-circuiting on
first failure, metrics for the failing check, cancellation) and the pre-checkout pipeline
(approve, reject, answer-exactly-once, and still answering when a validator throws — an
unanswered query leaves the payment hanging until it times out).

The integration suite starts RabbitMQ and Redis through Testcontainers and stands WireMock in
for the Telegram API. Nothing talks to the real Telegram API and no bot token is needed.

### Known limitations

- `TeleForge.Observability` depends on OpenTelemetry's Prometheus exporter, which has **never
  had a stable release** — every published version is `-beta`. NU5104 is suppressed with an
  explanation rather than silently. If a prerelease in your dependency graph is a problem,
  skip that package; the meters are plain `System.Diagnostics.Metrics` instruments.
- `VelocityCheck` is a hook, not a check: without a payment store it always passes. This is
  documented and covered by a test, so making it real is a visible change.
- `TeleForge.Payments.Fragment` and `.Payments.Crypto` talk to third-party services
  (`api.fragment-api.com`, `toncenter.com`, `app.tonkeeper.com`). Read their terms before
  putting them in front of real money.
- `TeleForge.Hosting`, `TeleForge.Consumer`, `TeleForge.Observability`, the rate-limiting
  packages and the development tools under `tools/` have no dedicated test projects yet.

[Unreleased]: https://github.com/deniscuciuc/teleforge/compare/v0.2.0...HEAD
[0.2.0]: https://github.com/deniscuciuc/teleforge/compare/v0.1.0...v0.2.0
[0.1.0]: https://github.com/deniscuciuc/teleforge/releases/tag/v0.1.0
