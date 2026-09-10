# Security Policy

## Reporting a Vulnerability

**Please do not open a public issue for a security vulnerability.**

Report it through GitHub's private vulnerability reporting, which is the preferred channel:

<https://github.com/deniscuciuc/botforge/security/advisories/new>

If you cannot use GitHub, email **denis@deniscuciuc.dev** instead.

Please include a description of the vulnerability and its impact, steps to reproduce or a
proof of concept, the affected module, and any suggested mitigation.

## What to Expect

| Stage | Target |
|---|---|
| Acknowledgement of your report | Within 48 hours |
| Initial assessment and severity triage | Within 5 working days |
| Fix released for a high or critical issue | Within 30 days of triage |
| Fix released for a moderate or low issue | Next scheduled release |

If you have not heard back within 48 hours, please follow up — an unanswered report usually
means it did not arrive.

## Supported Versions

| Version | Supported |
|---|---|
| 0.1.x   | Yes |
| < 0.1.0 | No  |

Only the latest patch of the latest minor release receives security fixes. All BotForge
packages are versioned and released together, so a fix ships as a new patch across all of
them.

## Scope

In scope: the BotForge packages in this repository. The highest-value targets, roughly in
order:

- **`BotForge.Payments`** — anything that could approve a pre-checkout that should have been
  rejected, fulfil an order that was not paid for, refund to the wrong recipient, or bypass
  the anti-fraud pipeline. Payload parsing is the entry point for all of it: a payload is
  attacker-influenced input that decides which handler runs.
- **`BotForge.Routing`** — authorization attributes and the middleware pipeline. A route that
  reaches a handler with `[Authorize]` unsatisfied is a vulnerability.
- **`BotForge.RateLimiting.Redis`** — a limit that can be evaded across instances.
- **`BotForge.Templates`** — untrusted values reaching a template in a way that changes its
  structure rather than filling a placeholder.
- Bot token handling anywhere, including whether one can end up in a log line.

Out of scope: vulnerabilities in `Telegram.Bot`, MassTransit, StackExchange.Redis or the
third-party payment services themselves — report those upstream, though we would still like
to know so the version can be pinned or worked around. The projects under `examples/` and the
development tools under `tools/` are also out of scope.

## Dependency Advisories

The build does not suppress NuGet vulnerability warnings. `NuGetAudit` is on with
`NuGetAuditMode=all` and `NuGetAuditLevel=low`, and `TreatWarningsAsErrors` makes any
published advisory — including transitive ones — fail the build. Advisories are resolved by upgrading, or pinned in
[`Directory.Packages.props`](Directory.Packages.props) with the GHSA identifier in a comment.

Dependabot is enabled for NuGet and GitHub Actions, CodeQL runs on every push to `main` and
weekly, and gitleaks scans the full history on every push and pull request.
