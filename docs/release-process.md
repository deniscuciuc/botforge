# Release process

Releases are tag-driven. Pushing a `v*.*.*` tag builds, tests, packs, creates the GitHub
release and publishes to nuget.org. There is no manual `dotnet nuget push` and no long-lived
API key.

## Versioning

The version comes from the git tag via [MinVer](https://github.com/adamralph/minver) — no
`<Version>` element exists anywhere in the repository. A build from an untagged commit
produces a prerelease version derived from the last tag.

All 21 packages are versioned and released together, so a fix in one ships as one version
bump across the whole set. That keeps a consumer from having to reason about which
combination of package versions is compatible.

[Semantic versioning](https://semver.org/) applies to the public API: the handler and
middleware contracts, the message and invoice builders, the routing attributes, and the
YAML template schema. **The template schema counts**: a change that makes an existing
template render differently is breaking even though no C# signature moved.

While the version is below 1.0 the API may still change between minor releases; each change
is called out in the changelog.

## Cutting a release

1. Make sure `main` is green.
2. Move the `## [Unreleased]` entries in [`CHANGELOG.md`](../CHANGELOG.md) into a new version
   section with today's date, and update the link definitions at the bottom. The release
   workflow reads that section for the release notes and **fails if it is missing**.
3. Commit, tag and push:

   ```bash
   git tag -a v1.1.0 -m "v1.1.0"
   git push origin main --follow-tags
   ```

4. The `Release` workflow runs. It packs, uploads the `.nupkg` and `.snupkg` files, creates
   the GitHub release, and then waits on the `nuget` environment before publishing.
5. Approve the `nuget` environment. Publishing to nuget.org is irreversible — a version can
   be unlisted but never replaced — so it sits behind a manual gate.

## Trusted Publishing

The workflow authenticates to nuget.org with [Trusted
Publishing](https://learn.microsoft.com/nuget/nuget-org/trusted-publishing) (OIDC). There is
no `NUGET_API_KEY` secret to rotate or leak.

Setup, once per package prefix:

1. On nuget.org, go to your account then **Trusted Publishing**.
2. Add a policy for the `TeleForge` package prefix, owner `deniscuciuc`, repository
   `deniscuciuc/teleforge`, workflow `release.yml`, environment `nuget`.
3. Set the repository variable `NUGET_USERNAME` to your nuget.org username.

## Local verification before tagging

```bash
dotnet build -c Release
dotnet test -c Release
dotnet pack -c Release -o ./artifacts
ls ./artifacts   # expect exactly 21 .nupkg and 19 .snupkg, nothing else
```

The package count is the check that matters: any of the fifteen example bots, the two
service hosts or a test project accidentally becoming packable shows up here and nowhere
else. There are 19 rather than 21 symbol packages because `TeleForge.Analyzers` and
`TeleForge.Templates.DotNet` ship no `lib/` output.
