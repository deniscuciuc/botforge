# Contributing to BotForge

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) — pinned in [`global.json`](global.json)
- Docker or Podman, for the integration tests
- An editor that honours `.editorconfig`

## Setup

```bash
git clone https://github.com/deniscuciuc/botforge.git
cd botforge
dotnet restore
dotnet build -c Release
dotnet test -c Release --filter "FullyQualifiedName!~Integration"
```

The integration suite starts RabbitMQ and Redis through Testcontainers and stands WireMock in
for the Telegram API. With Podman, point Testcontainers at the socket first:

```bash
export DOCKER_HOST=unix:///run/user/$(id -u)/podman/podman.sock
dotnet test tests/BotForge.Integration.Tests -c Release
```

Nothing here talks to the real Telegram API, and no bot token is needed to run the suite.

## Code style

Enforced by the build and by `dotnet format --verify-no-changes` in CI:

- `net10.0`, `LangVersion latest`, nullable reference types and implicit usings on
- `TreatWarningsAsErrors`, .NET analyzers at `Recommended`
- File-scoped namespaces; one namespace per project, matching the project name
- XML doc comments on public types and members
- `ArgumentNullException.ThrowIfNull` on every public entry point (CA1062)
- `ConfigureAwait(false)` on every await in `src/` (CA2007)
- `CultureInfo.InvariantCulture` on every format and parse (CA1305, CA1310)

Deliberate deviations are listed in [`.editorconfig`](.editorconfig), each with its reason —
`TelegramUpdateDelegate` and a `next` parameter, for instance, are kept because they are the
middleware convention everyone already knows from ASP.NET Core.

## Architecture

Two pipelines with a queue between them: updates come in through `BotForge.Consumer` onto a
bounded channel, get routed by `BotForge.Routing`, and replies go out through
`BotForge.Messaging` via a queue backend and a send pipeline. That queue is what lets the
sender be a separate process.

Every subsystem has an `.Abstractions` package, and implementations depend only on
abstractions. When adding a contract, put it in the abstractions package — a handler project
should never need to reference an implementation.

Everything is keyed by `botId`. New state must be too, or multi-bot breaks silently.

## Adding a handler type

1. Add the attribute to `BotForge.Routing.Abstractions/Attributes/` and the handler interface
   next to the existing ones in `Handlers/`.
2. Teach `ReflectionHandlerDiscovery` and the matching router about it.
3. Add routing tests, including the no-match and multiple-match cases.
4. Consider an analyzer rule in `tools/BotForge.Analyzers` if the handler has a shape that is
   easy to get wrong.
5. Document it in `docs/routing.md`, and add an example bot if it needs one.

## Adding a package

Add the project to `BotForge.slnx`, give it a `<Description>`, `<PackageTags>` and its own
`README.md`, then **add it to the expected package list in `.github/workflows/ci.yml`** — the
pack job asserts the exact set of 21 packages and will fail otherwise. That assertion is what
stops an example bot or a test project silently becoming a published package.

## Payments

Changes under `src/BotForge.Payments*` need tests. This code decides whether money moves, and
its failure mode is quiet: a pre-checkout that approves when it should reject takes a real
payment for something that will not be delivered. `tests/BotForge.Payments.Tests` covers the
payload serializer, the anti-fraud pipeline and the pre-checkout pipeline — extend it rather
than working around it.

## Documentation

Any C# you put in `README.md` must also exist in `tests/BotForge.Docs.Snippets`, which
compiles in CI. A documented snippet that does not compile is a bug.

## Pull requests

- Keep a PR to a single concern.
- New behaviour and bug fixes need tests.
- Conventional commits: `feat(routing): …`, `fix(payments): …`, `docs(readme): …`.
- Note breaking changes in `CHANGELOG.md` under `## [Unreleased]`, including changes to the
  YAML template schema.
- CI must be green: format, build and test on Linux and Windows, the integration suite, and
  the pack assertion.

## Releasing

See [docs/release-process.md](docs/release-process.md). Releases are tag-driven; only
maintainers can approve the publish step.
