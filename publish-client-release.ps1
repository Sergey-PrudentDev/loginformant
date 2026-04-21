param(
    [ValidateSet('node', 'python', 'php', 'java', 'nuget', 'all-non-dotnet', 'all')]
    [string]$Package,

    [ValidatePattern('^\d+\.\d+\.\d+(-[0-9A-Za-z.-]+)?$')]
    [string]$Version,

    [string]$Remote = 'github',
    [string]$MirrorBranch = 'master',
    [string]$MirrorDirectory = '.tmp/github-client-publish',

    # Accepted for compatibility with older examples. The clients-only mirror
    # branch is always pushed before release tags.
    [switch]$PushBranch,
    [switch]$SyncOnly,
    [switch]$DryRun,
    [switch]$Force
)

$ErrorActionPreference = 'Stop'
Set-StrictMode -Version Latest

function Write-Step {
    param([string]$Message)
    Write-Host "`n==> $Message" -ForegroundColor Cyan
}

function Format-Command {
    param([string]$File, [string[]]$Arguments)

    $formattedArgs = $Arguments | ForEach-Object {
        if ($_ -match '\s') { '"' + $_ + '"' } else { $_ }
    }

    return "$File $($formattedArgs -join ' ')"
}

function Invoke-CommandChecked {
    param(
        [string]$File,
        [string[]]$Arguments,
        [string]$WorkingDirectory
    )

    $display = Format-Command $File $Arguments
    Write-Host $display -ForegroundColor DarkGray

    Push-Location $WorkingDirectory
    try {
        & $File @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "Command failed: $display"
        }
    }
    finally {
        Pop-Location
    }
}

function Get-CommandOutput {
    param(
        [string]$File,
        [string[]]$Arguments,
        [string]$WorkingDirectory
    )

    Push-Location $WorkingDirectory
    try {
        $output = & $File @Arguments
        if ($LASTEXITCODE -ne 0) {
            $display = Format-Command $File $Arguments
            throw "Command failed: $display"
        }
        return $output
    }
    finally {
        Pop-Location
    }
}

function Get-ReleasePackages {
    param([string]$PackageName)

    switch ($PackageName) {
        'all-non-dotnet' { return @('node', 'python', 'php', 'java') }
        'all' { return @('node', 'python', 'php', 'java', 'nuget') }
        default { return @($PackageName) }
    }
}

function Get-FullPathUnderRepo {
    param(
        [string]$RepoRoot,
        [string]$Path
    )

    if ([System.IO.Path]::IsPathRooted($Path)) {
        $fullPath = [System.IO.Path]::GetFullPath($Path)
    }
    else {
        $fullPath = [System.IO.Path]::GetFullPath((Join-Path $RepoRoot $Path))
    }

    $repoRootFull = [System.IO.Path]::GetFullPath($RepoRoot).TrimEnd('\', '/')
    $candidateFull = $fullPath.TrimEnd('\', '/')
    $prefix = $repoRootFull + [System.IO.Path]::DirectorySeparatorChar

    if ($candidateFull -eq $repoRootFull -or -not $candidateFull.StartsWith($prefix, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "MirrorDirectory must be inside the repository. Refusing to use: $fullPath"
    }

    return $fullPath
}

if ($SyncOnly) {
    if (-not [string]::IsNullOrWhiteSpace($Package) -or -not [string]::IsNullOrWhiteSpace($Version)) {
        throw "Use either -SyncOnly or -Package/-Version, not both."
    }
}
else {
    if ([string]::IsNullOrWhiteSpace($Package) -or [string]::IsNullOrWhiteSpace($Version)) {
        throw "Pass -SyncOnly to update the GitHub clients mirror, or pass both -Package and -Version to publish."
    }
}

Write-Step "Checking source repository"
$repoRoot = (Get-CommandOutput git @('rev-parse', '--show-toplevel') (Get-Location).Path | Select-Object -First 1).Trim()
$sourceCommit = (Get-CommandOutput git @('rev-parse', '--short', 'HEAD') $repoRoot | Select-Object -First 1).Trim()
$sourceBranch = (Get-CommandOutput git @('branch', '--show-current') $repoRoot | Select-Object -First 1).Trim()
if ([string]::IsNullOrWhiteSpace($sourceBranch)) {
    $sourceBranch = '(detached HEAD)'
}

$remoteNames = Get-CommandOutput git @('remote') $repoRoot
if ($remoteNames -contains $Remote) {
    $remoteUrl = (Get-CommandOutput git @('remote', 'get-url', $Remote) $repoRoot | Select-Object -First 1).Trim()
}
else {
    $looksLikeRemoteUrl =
        $Remote -match '^[a-zA-Z][a-zA-Z0-9+.-]*://' -or
        $Remote -match '^[^@]+@[^:]+:.+' -or
        $Remote -match '\.git$' -or
        (Test-Path -LiteralPath $Remote)

    if (-not $looksLikeRemoteUrl) {
        throw "Git remote '$Remote' was not found. Add it first, for example: git remote add github https://github.com/Sergey-PrudentDev/loginformant.git"
    }

    $remoteUrl = $Remote
}
$status = Get-CommandOutput git @('status', '--porcelain') $repoRoot
if (-not $Force -and -not [string]::IsNullOrWhiteSpace(($status | Out-String))) {
    throw "Working tree is not clean. Commit or stash changes before publishing. Use -Force only if you intentionally want to mirror the current dirty tree."
}

$releasePackages = @()
$tags = @()
if (-not $SyncOnly) {
    $releasePackages = @(Get-ReleasePackages $Package)
    $tags = @($releasePackages | ForEach-Object { "$_/v$Version" })
}

$clientPaths = @(
    '.github',
    'Integrations',
    'PUBLISHING.md',
    'publish-client-release.ps1',
    '.gitignore',
    'README.md',
    'LICENSE',
    'LICENSE.txt'
)

$mirrorPath = Get-FullPathUnderRepo $repoRoot $MirrorDirectory

Write-Host "Source:  $sourceBranch@$sourceCommit"
Write-Host "Remote:  $Remote ($remoteUrl)"
Write-Host "Mirror:  $MirrorBranch in $mirrorPath"
if ($SyncOnly) {
    Write-Host "Mode:    sync clients-only mirror"
}
else {
    Write-Host "Version: $Version"
    Write-Host "Tags:    $($tags -join ', ')"
}

foreach ($tag in $tags) {
    if (-not $DryRun -and -not $Force) {
        $remoteTag = & git ls-remote --tags $remoteUrl "refs/tags/$tag"
        if ($LASTEXITCODE -ne 0) {
            throw "Could not check remote tag $tag on $Remote."
        }
        if (-not [string]::IsNullOrWhiteSpace(($remoteTag | Out-String))) {
            throw "Remote tag '$tag' already exists on '$Remote'. Pick a new version or rerun with -Force."
        }
    }
}

if ($PushBranch) {
    Write-Host "-PushBranch is accepted for older examples; the clients-only mirror branch is always pushed." -ForegroundColor DarkGray
}

if ($DryRun) {
    Write-Step "Dry run"
    Write-Host "Would recreate mirror directory: $mirrorPath"
    Write-Host "Would copy client paths:"
    foreach ($relativePath in $clientPaths) {
        if (Test-Path -LiteralPath (Join-Path $repoRoot $relativePath)) {
            Write-Host "  $relativePath"
        }
    }
    Write-Host "Would push $MirrorBranch to $Remote without src/ or app database history."
    foreach ($tag in $tags) {
        Write-Host "Would create and push tag: $tag"
    }
    return
}

Write-Step "Preparing clients-only GitHub mirror"
$mirrorParent = Split-Path -Parent $mirrorPath
New-Item -ItemType Directory -Force -Path $mirrorParent | Out-Null
if (Test-Path -LiteralPath $mirrorPath) {
    Remove-Item -LiteralPath $mirrorPath -Recurse -Force
}

Invoke-CommandChecked git @('clone', '--no-tags', $remoteUrl, $mirrorPath) $repoRoot

$remoteBranch = & git -C $mirrorPath ls-remote --heads origin $MirrorBranch
if ($LASTEXITCODE -ne 0) {
    throw "Could not check mirror branch '$MirrorBranch' on '$Remote'."
}

if ([string]::IsNullOrWhiteSpace(($remoteBranch | Out-String))) {
    $currentMirrorBranch = & git -C $mirrorPath symbolic-ref --quiet --short HEAD
    if ($LASTEXITCODE -eq 0 -and $currentMirrorBranch -eq $MirrorBranch) {
        Write-Host "Using empty mirror branch $MirrorBranch" -ForegroundColor DarkGray
    }
    else {
        Invoke-CommandChecked git @('switch', '--orphan', $MirrorBranch) $mirrorPath
    }
}
else {
    Invoke-CommandChecked git @('switch', '-C', $MirrorBranch, "origin/$MirrorBranch") $mirrorPath
}

Get-ChildItem -LiteralPath $mirrorPath -Force |
    Where-Object { $_.Name -ne '.git' } |
    ForEach-Object { Remove-Item -LiteralPath $_.FullName -Recurse -Force }

foreach ($relativePath in $clientPaths) {
    $sourcePath = Join-Path $repoRoot $relativePath
    if (-not (Test-Path -LiteralPath $sourcePath)) {
        continue
    }

    $destinationPath = Join-Path $mirrorPath $relativePath
    $destinationParent = Split-Path -Parent $destinationPath
    New-Item -ItemType Directory -Force -Path $destinationParent | Out-Null
    Copy-Item -LiteralPath $sourcePath -Destination $destinationPath -Recurse -Force
}

$gitUserName = & git -C $repoRoot config --get user.name
if ($LASTEXITCODE -ne 0) {
    $gitUserName = $null
}
$gitUserName = $gitUserName | Select-Object -First 1

$gitUserEmail = & git -C $repoRoot config --get user.email
if ($LASTEXITCODE -ne 0) {
    $gitUserEmail = $null
}
$gitUserEmail = $gitUserEmail | Select-Object -First 1
if ([string]::IsNullOrWhiteSpace($gitUserName)) {
    $gitUserName = 'LogInformant Release Bot'
}
if ([string]::IsNullOrWhiteSpace($gitUserEmail)) {
    $gitUserEmail = 'release@loginformant.com'
}

Invoke-CommandChecked git @('config', 'user.name', $gitUserName.Trim()) $mirrorPath
Invoke-CommandChecked git @('config', 'user.email', $gitUserEmail.Trim()) $mirrorPath
Invoke-CommandChecked git @('add', '-A') $mirrorPath

& git -C $mirrorPath diff --cached --quiet
if ($LASTEXITCODE -eq 1) {
    Invoke-CommandChecked git @('commit', '-m', "Sync client libraries from LogInformant $sourceCommit") $mirrorPath
}
elseif ($LASTEXITCODE -eq 0) {
    Write-Host "Mirror content already matches the current client files." -ForegroundColor DarkGray
}
else {
    throw "Could not inspect mirror changes."
}

Write-Step "Pushing clients-only mirror branch"
Invoke-CommandChecked git @('push', 'origin', "${MirrorBranch}:${MirrorBranch}") $mirrorPath

if ($tags.Count -gt 0) {
    Write-Step "Creating release tags on mirror commit"
    foreach ($tag in $tags) {
        if ($Force) {
            Invoke-CommandChecked git @('tag', '-f', $tag) $mirrorPath
        }
        else {
            Invoke-CommandChecked git @('tag', $tag) $mirrorPath
        }
    }

    Write-Step "Pushing release tags"
    foreach ($tag in $tags) {
        if ($Force) {
            Invoke-CommandChecked git @('push', '--force', 'origin', $tag) $mirrorPath
        }
        else {
            Invoke-CommandChecked git @('push', 'origin', $tag) $mirrorPath
        }
    }
}

Write-Host "`nGitHub clients-only mirror is updated. Watch the matching GitHub Actions workflow run in the GitHub repo." -ForegroundColor Green
