<#
.SYNOPSIS
Runs an integrated WPF dropdown smoke test against a generated profiling fixture.

.DESCRIPTION
Creates a local profiling fixture, launches MergePilot with MERGEPILOT_SETTINGS_PATH
pointing at the generated settings file, expands/collapses the source and target
branch dropdowns through Windows UI Automation, and closes the app. The active
user settings file is not modified.
#>
[CmdletBinding()]
param(
    [string]$Root = (Join-Path $env:TEMP "MergePilotProfilingDropdownSmoke"),
    [int]$RepositoryCount = 1,
    [int]$BranchCount = 1000,
    [string]$ExecutablePath = (Join-Path (Get-Location) "MergePilot\bin\Debug\net8.0-windows10.0.26100.0\MergePilot.exe"),
    [int]$WindowTimeoutSeconds = 45,
    [int]$CloseTimeoutMilliseconds = 10000
)

$ErrorActionPreference = "Stop"

function Get-LatestFixtureSettingsPath {
    param([string]$FixtureRoot)

    $latestRun = Get-ChildItem -LiteralPath $FixtureRoot -Directory |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $latestRun) {
        throw "No profiling fixture run directory was created."
    }

    $settingsPath = Join-Path $latestRun.FullName "settings.profile.json"
    if (-not (Test-Path -LiteralPath $settingsPath)) {
        throw "Fixture settings file not found: $settingsPath"
    }

    return $settingsPath
}

function Wait-ForWindowElement {
    param(
        [System.Diagnostics.Process]$Process,
        [int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        Start-Sleep -Milliseconds 250
        $Process.Refresh()
        if ($Process.HasExited) {
            throw "MergePilot exited early with code $($Process.ExitCode)."
        }

        if ($Process.MainWindowHandle -ne [IntPtr]::Zero) {
            $element = [System.Windows.Automation.AutomationElement]::FromHandle($Process.MainWindowHandle)
            if ($null -ne $element) {
                return $element
            }
        }
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "MergePilot main window automation element did not appear within timeout."
}

function Find-ByAutomationId {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$AutomationId
    )

    $condition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::AutomationIdProperty,
        $AutomationId)

    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Expand-And-Collapse {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [string]$Name
    )

    if ($null -eq $Element) {
        throw "$Name automation element not found."
    }

    $patternObject = $null
    if (-not $Element.TryGetCurrentPattern(
            [System.Windows.Automation.ExpandCollapsePattern]::Pattern,
            [ref]$patternObject)) {
        throw "$Name does not support ExpandCollapsePattern."
    }

    $pattern = [System.Windows.Automation.ExpandCollapsePattern]$patternObject
    $stopwatch = [System.Diagnostics.Stopwatch]::StartNew()
    $pattern.Expand()
    Start-Sleep -Milliseconds 500
    $pattern.Collapse()
    $stopwatch.Stop()

    return $stopwatch.ElapsedMilliseconds
}

if (-not (Test-Path -LiteralPath $ExecutablePath)) {
    throw "App executable not found: $ExecutablePath. Run dotnet build first."
}

& (Join-Path $PSScriptRoot "Prepare-ManualProfilingFixture.ps1") `
    -Root $Root `
    -RepositoryCount $RepositoryCount `
    -BranchCount $BranchCount | Out-Host

$settingsPath = Get-LatestFixtureSettingsPath -FixtureRoot $Root

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$processStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$processStartInfo.FileName = $ExecutablePath
$processStartInfo.WorkingDirectory = Split-Path -Parent $ExecutablePath
$processStartInfo.UseShellExecute = $false
$processStartInfo.EnvironmentVariables["MERGEPILOT_SETTINGS_PATH"] = $settingsPath

$process = [System.Diagnostics.Process]::Start($processStartInfo)
try {
    $rootElement = Wait-ForWindowElement -Process $process -TimeoutSeconds $WindowTimeoutSeconds
    $sourceBranchBox = Find-ByAutomationId -Root $rootElement -AutomationId "SourceBranchBox"
    $targetBranchBox = Find-ByAutomationId -Root $rootElement -AutomationId "TargetBranchBox"

    $sourceMilliseconds = Expand-And-Collapse -Element $sourceBranchBox -Name "SourceBranchBox"
    $targetMilliseconds = Expand-And-Collapse -Element $targetBranchBox -Name "TargetBranchBox"

    if (-not $process.CloseMainWindow()) {
        throw "CloseMainWindow returned false."
    }

    if (-not $process.WaitForExit($CloseTimeoutMilliseconds)) {
        throw "MergePilot did not exit within $CloseTimeoutMilliseconds ms after close request."
    }

    [pscustomobject]@{
        Result = "Passed"
        SourceMilliseconds = $sourceMilliseconds
        TargetMilliseconds = $targetMilliseconds
        SettingsPath = $settingsPath
        RepositoryCount = $RepositoryCount
        BranchCount = $BranchCount
        ExitCode = $process.ExitCode
    }
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit()
    }
}
