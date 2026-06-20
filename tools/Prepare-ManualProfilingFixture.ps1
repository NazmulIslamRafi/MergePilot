<#
.SYNOPSIS
Creates a repeatable local fixture for MergePilot manual performance profiling.

.DESCRIPTION
The fixture contains one or more local Git repositories, matching local bare
remotes, many remote branch refs, and a generated settings.profile.json file.
By default this script does not modify the user's active MergePilot settings.
Use the generated settings with MERGEPILOT_SETTINGS_PATH for a non-destructive
profiling run. Pass -ApplySettings only when you explicitly want to back up and
replace %LOCALAPPDATA%\MergePilot\settings.json with the generated profile settings.
#>
[CmdletBinding()]
param(
    [string]$Root = (Join-Path $env:TEMP "MergePilotProfilingFixture"),
    [int]$RepositoryCount = 3,
    [int]$BranchCount = 1200,
    [switch]$ApplySettings
)

$ErrorActionPreference = "Stop"

function Invoke-Git {
    param(
        [string]$WorkingDirectory,
        [string[]]$Arguments
    )

    $previousLocation = Get-Location
    try {
        Set-Location -LiteralPath $WorkingDirectory
        & git @Arguments
        if ($LASTEXITCODE -ne 0) {
            throw "git $($Arguments -join ' ') failed with exit code $LASTEXITCODE"
        }
    }
    finally {
        Set-Location $previousLocation
    }
}

function New-BranchName {
    param([int]$Index)

    $team = [int][Math]::Floor($Index / 250)
    $area = [int][Math]::Floor($Index / 25)
    return "feature/team-{0:D2}/area-{1:D2}/branch-{2:D4}" -f $team, $area, $Index
}

if ($RepositoryCount -lt 1) {
    throw "RepositoryCount must be at least 1."
}

if ($BranchCount -lt 1) {
    throw "BranchCount must be at least 1."
}

& git --version | Out-Null
if ($LASTEXITCODE -ne 0) {
    throw "git must be available on PATH."
}

$runRoot = Join-Path $Root ("run-{0:yyyyMMdd-HHmmss}" -f (Get-Date))
$repositoriesRoot = Join-Path $runRoot "repositories"
$remotesRoot = Join-Path $runRoot "remotes"
New-Item -ItemType Directory -Path $repositoriesRoot, $remotesRoot -Force | Out-Null

$repositories = New-Object System.Collections.Generic.List[object]
$customBranches = New-Object System.Collections.Generic.List[object]
$recentBranches = New-Object System.Collections.Generic.List[string]

for ($repoIndex = 1; $repoIndex -le $RepositoryCount; $repoIndex++) {
    $repoName = "ProfileRepo{0:D2}" -f $repoIndex
    $repoPath = Join-Path $repositoriesRoot $repoName
    $remotePath = Join-Path $remotesRoot "$repoName.git"

    New-Item -ItemType Directory -Path $repoPath | Out-Null
    Invoke-Git -WorkingDirectory $repoPath -Arguments @("init")
    Invoke-Git -WorkingDirectory $repoPath -Arguments @("config", "user.email", "mergepilot-profile@example.local")
    Invoke-Git -WorkingDirectory $repoPath -Arguments @("config", "user.name", "MergePilot Profile")

    Set-Content -LiteralPath (Join-Path $repoPath "README.md") -Value "# $repoName" -Encoding UTF8
    Invoke-Git -WorkingDirectory $repoPath -Arguments @("add", "README.md")
    Invoke-Git -WorkingDirectory $repoPath -Arguments @("commit", "-m", "Initial profiling commit")

    New-Item -ItemType Directory -Path $remotePath | Out-Null
    Invoke-Git -WorkingDirectory $runRoot -Arguments @("init", "--bare", $remotePath)
    Invoke-Git -WorkingDirectory $repoPath -Arguments @("remote", "add", "origin", $remotePath)
    Invoke-Git -WorkingDirectory $repoPath -Arguments @("push", "origin", "HEAD:main")

    $commitHash = (& git -C $repoPath rev-parse HEAD).Trim()
    if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace($commitHash)) {
        throw "Unable to read commit hash for $repoName."
    }

    $repositories.Add([ordered]@{
        Name = $repoName
        Path = $repoPath
        RemoteUrl = $remotePath
    })

    for ($branchIndex = 0; $branchIndex -lt $BranchCount; $branchIndex++) {
        $branchName = New-BranchName -Index $branchIndex
        Invoke-Git -WorkingDirectory $runRoot -Arguments @("--git-dir=$remotePath", "update-ref", "refs/heads/$branchName", $commitHash)

        $customBranches.Add([ordered]@{
            BranchName = $branchName
            Repository = $repoName
        })

        if ($repoIndex -eq 1 -and $recentBranches.Count -lt 100) {
            $recentBranches.Add($branchName)
        }
    }
}

$settings = [ordered]@{
    AutoOpenLogs = $true
    StreamLogs = $false
    LogFilePath = $null
    FlushIntervalMs = 200
    Repositories = $repositories
    CustomBranches = $customBranches
    RecentBranches = $recentBranches
    LastSourceBranch = $null
    LastTargetBranch = $null
    BranchCheckedState = @{}
    BranchExpandedState = @{}
    InlineLogsVisible = $false
    OutputErrorsOnly = $false
    LastSearchOutput = $null
    LastSearchError = $null
    LogFontSize = 13.0
    LogMaxChars = 200000
}

$profileSettingsPath = Join-Path $runRoot "settings.profile.json"
$settings | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $profileSettingsPath -Encoding UTF8

Write-Host "Profiling fixture created:"
Write-Host "  Root: $runRoot"
Write-Host "  Settings: $profileSettingsPath"
Write-Host "  Repositories: $RepositoryCount"
Write-Host "  Branches per repository: $BranchCount"
Write-Host "Non-destructive app run:"
Write-Host "  `$env:MERGEPILOT_SETTINGS_PATH = '$profileSettingsPath'"
Write-Host "  dotnet run --project MergePilot\MergePilot.csproj"

if ($ApplySettings) {
    $settingsDirectory = Join-Path $env:LOCALAPPDATA "MergePilot"
    $targetSettingsPath = Join-Path $settingsDirectory "settings.json"
    New-Item -ItemType Directory -Path $settingsDirectory -Force | Out-Null

    if (Test-Path -LiteralPath $targetSettingsPath) {
        $backupPath = "$targetSettingsPath.backup-{0:yyyyMMdd-HHmmss}" -f (Get-Date)
        Copy-Item -LiteralPath $targetSettingsPath -Destination $backupPath
        Write-Host "  Existing settings backup: $backupPath"
    }

    Copy-Item -LiteralPath $profileSettingsPath -Destination $targetSettingsPath -Force
    Write-Host "  Applied settings to: $targetSettingsPath"
}
else {
    Write-Host "Run again with -ApplySettings only if you want to back up and replace the active MergePilot settings."
}
