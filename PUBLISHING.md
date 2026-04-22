# Client Library Publishing Runbook

Last checked: 2026-04-21

NuGet is already live for the four .NET client packages. npm, PyPI, Packagist, and Maven Central are also live.

## Current Publishing State

- NuGet: published manually for the four .NET client packages; add `NUGET_API_KEY` for future automated releases.
- npm `@loginformant/node`: published at `1.0.0`.
- PyPI `loginformant`: published at `1.0.0`.
- Packagist `loginformant/monolog-handler`: published at `1.0.0`.
- Maven Central `com.loginformant:logback-appender`: published at `1.0.0`.

Initial public registry checks returned 404 for the four non-.NET package names on 2026-04-21. npm and PyPI were published afterward.

## One Required Foundation Step

The workflows in `.github/workflows/` only run from GitHub Actions. This checkout uses Azure DevOps as `origin`:

```text
https://saganmarketing.visualstudio.com/DefaultCollection/LogInformant/_git/LogInformant
```

Use a separate `github` remote for the GitHub Actions copy:

```text
https://github.com/Sergey-PrudentDev/loginformant
```

Then make sure the local `github` remote points at it:

```powershell
git remote set-url github https://github.com/Sergey-PrudentDev/loginformant.git
```

If the `github` remote does not exist yet, add it instead:

```powershell
git remote add github https://github.com/Sergey-PrudentDev/loginformant.git
```

Do not push the full LogInformant `master` branch to GitHub. The Azure repo history contains local app database blobs that GitHub rejects, and the client libraries do not need the app source anyway.

Seed or update the GitHub repo with only the client publishing files:

```powershell
.\publish-client-release.ps1 -SyncOnly -Remote github
```

That mirror contains `.github/`, `Integrations/`, this runbook, the release helper, and a minimal root `.gitignore`. Package releases are triggered by pushing tags to that clients-only mirror.

## Release Script

Use [publish-client-release.ps1](publish-client-release.ps1) after the one-time registry setup is complete.

Examples:

```powershell
.\publish-client-release.ps1 -Package node -Version 1.0.0 -Remote github
.\publish-client-release.ps1 -Package python -Version 1.0.0 -Remote github
.\publish-client-release.ps1 -Package php -Version 1.0.0 -Remote github
.\publish-client-release.ps1 -Package java -Version 1.0.0 -Remote github
.\publish-client-release.ps1 -Package nuget -Version 1.0.0 -Remote github
```

Release all remaining non-.NET clients at the same version:

```powershell
.\publish-client-release.ps1 -Package all-non-dotnet -Version 1.0.0 -Remote github
```

Release every client package, including the four NuGet packages:

```powershell
.\publish-client-release.ps1 -Package all -Version 1.0.1 -Remote github
```

The script checks for a clean worktree, copies only the client publishing files to a temporary mirror under `.tmp/github-client-publish`, pushes that mirror to GitHub, checks that tags do not already exist, creates tags like `node/v1.0.0` on the mirror commit, and pushes them to GitHub. GitHub Actions does the actual publishing.

## Easiest Order

### NuGet: four .NET packages

The initial NuGet packages were published manually. For automated future releases, add this GitHub Actions repository secret:

```text
NUGET_API_KEY = <nuget.org API key with push access to LogInformant* packages>
```

The existing API key should allow `Push new packages and package versions` for the `LogInformant*` glob. If you did not save the current key value when it was created, regenerate it or create a new key and copy the value immediately; NuGet does not show the key value again later.

Future updates:

```powershell
.\publish-client-release.ps1 -Package nuget -Version 1.0.1 -Remote github
```

### 1. npm: `@loginformant/node`

Why first: one token, no namespace/domain verification beyond npm scope access.

One-time setup:

1. Sign in to npmjs.com.
2. Make sure your npm account owns or can publish to the `@loginformant` scope.
3. Create an automation access token.
4. In GitHub repo settings, add Actions secret:

```text
NPM_TOKEN = <npm automation token>
```

Publish:

```powershell
.\publish-client-release.ps1 -Package node -Version 1.0.0 -Remote github
```

Verify:

```powershell
npm view @loginformant/node version --registry=https://registry.npmjs.org
```

Future updates:

```powershell
.\publish-client-release.ps1 -Package node -Version 1.0.1 -Remote github
```

### 2. PyPI: `loginformant`

Why second: no API token if using Trusted Publishing, but PyPI must know about the GitHub workflow.

One-time setup:

1. Sign in to pypi.org.
2. Add a pending Trusted Publisher for project `loginformant`.
3. Use:

```text
Owner: Sergey-PrudentDev
Repository: loginformant
Workflow: publish-python.yml
Environment: blank
```

Publish:

```powershell
.\publish-client-release.ps1 -Package python -Version 1.0.0 -Remote github
```

Verify:

```powershell
python -m pip index versions loginformant
```

Future updates:

```powershell
.\publish-client-release.ps1 -Package python -Version 1.0.1 -Remote github
```

### 3. PHP / Packagist: `loginformant/monolog-handler`

Why third: Packagist needs `composer.json` at the repository root, so this uses a GitHub split repo.

One-time setup:

1. Create a second GitHub repo, for example:

```text
https://github.com/Sergey-PrudentDev/loginformant-monolog
```

2. Create a GitHub token with write access to that split repo.
3. In the main GitHub repo settings, add Actions variable:

```text
PHP_SPLIT_REPO = Sergey-PrudentDev/loginformant-monolog
```

4. In the main GitHub repo settings, add Actions secret:

```text
PHP_SPLIT_REPO_TOKEN = <GitHub token with write access to Sergey-PrudentDev/loginformant-monolog>
```

5. Submit the split repo URL on packagist.org:

```text
https://github.com/Sergey-PrudentDev/loginformant-monolog
```

6. Optional, for immediate Packagist re-indexing, add these GitHub Actions secrets:

```text
PACKAGIST_USERNAME = <packagist username>
PACKAGIST_TOKEN = <packagist API token>
```

Publish:

```powershell
.\publish-client-release.ps1 -Package php -Version 1.0.0 -Remote github
```

Verify:

```powershell
composer show loginformant/monolog-handler --all
```

Future updates:

```powershell
.\publish-client-release.ps1 -Package php -Version 1.0.1 -Remote github
```

### 4. Maven Central: `com.loginformant:logback-appender`

Why last: namespace verification plus GPG signing makes it the most fiddly.

One-time setup:

1. Sign in to central.sonatype.com.
2. Claim and verify the namespace:

```text
com.loginformant
```

3. Generate a Sonatype Central user token.
4. Create or choose a GPG key for signing.
5. Export the armored private key:

```powershell
gpg --armor --export-secret-keys YOUR_KEY_ID
```

6. In GitHub repo settings, add Actions secrets:

```text
MAVEN_USERNAME = <Sonatype Central token username>
MAVEN_CENTRAL_TOKEN = <Sonatype Central token password>
MAVEN_GPG_PRIVATE_KEY = <full armored private key export>
MAVEN_GPG_PASSPHRASE = <GPG key passphrase>
```

Publish:

```powershell
.\publish-client-release.ps1 -Package java -Version 1.0.0 -Remote github
```

Verify:

```powershell
mvn dependency:get "-Dartifact=com.loginformant:logback-appender:1.0.0"
```

Future updates:

```powershell
.\publish-client-release.ps1 -Package java -Version 1.0.1 -Remote github
```

## Tag Map

| Package | Tag | Workflow |
| --- | --- | --- |
| npm | `node/v1.0.0` | `.github/workflows/publish-node.yml` |
| PyPI | `python/v1.0.0` | `.github/workflows/publish-python.yml` |
| PHP | `php/v1.0.0` | `.github/workflows/publish-php.yml` |
| Maven | `java/v1.0.0` | `.github/workflows/publish-java.yml` |
| NuGet | `nuget/v1.0.0` | `.github/workflows/publish-nuget.yml` |

## Notes

- The workflows stamp package versions from the tag at publish time.
- The GitHub repo is a clients-only publishing mirror; Azure DevOps remains the full application repo.
- Do not reuse registry versions; most package registries treat versions as immutable.
- The release script intentionally refuses to run with local uncommitted changes unless `-Force` is passed.
- For first release, publish packages one at a time so registry setup errors are easy to isolate.
