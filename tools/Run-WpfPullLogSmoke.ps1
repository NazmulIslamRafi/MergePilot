<#
.SYNOPSIS
Runs an integrated WPF pull/log smoke test against a generated profiling fixture.

.DESCRIPTION
Creates a local profiling fixture, enables log streaming in the generated
settings file, launches MergePilot with MERGEPILOT_SETTINGS_PATH, selects the
first generated repository and branch through Windows UI Automation, runs Pull,
waits for the streamed log summary, and closes the app. The active user settings
file is not modified.
#>
[CmdletBinding()]
param(
    [string]$Root = (Join-Path $env:TEMP "MergePilotProfilingPullLogSmoke"),
    [int]$BranchCount = 20,
    [int]$RepeatCount = 1,
    [int]$MinimumStreamLines = 1,
    [string]$ExecutablePath = (Join-Path (Get-Location) "MergePilot\bin\Debug\net8.0-windows10.0.26100.0\MergePilot.exe"),
    [int]$WindowTimeoutSeconds = 45,
    [int]$OperationTimeoutSeconds = 90,
    [int]$CloseTimeoutMilliseconds = 10000
)

$ErrorActionPreference = "Stop"

function Get-LatestFixtureRun {
    param([string]$FixtureRoot)

    $latestRun = Get-ChildItem -LiteralPath $FixtureRoot -Directory |
        Sort-Object LastWriteTime -Descending |
        Select-Object -First 1

    if ($null -eq $latestRun) {
        throw "No profiling fixture run directory was created."
    }

    return $latestRun.FullName
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

function Find-ByNameAndControlType {
    param(
        [System.Windows.Automation.AutomationElement]$Root,
        [string]$Name,
        [System.Windows.Automation.ControlType]$ControlType
    )

    $nameCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::NameProperty,
        $Name)
    $controlCondition = [System.Windows.Automation.PropertyCondition]::new(
        [System.Windows.Automation.AutomationElement]::ControlTypeProperty,
        $ControlType)
    $condition = [System.Windows.Automation.AndCondition]::new($nameCondition, $controlCondition)

    return $Root.FindFirst([System.Windows.Automation.TreeScope]::Descendants, $condition)
}

function Wait-ForElement {
    param(
        [scriptblock]$Lookup,
        [string]$Description,
        [int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $element = & $Lookup
        if ($null -ne $element) {
            return $element
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "$Description was not found within timeout."
}

function Set-Checked {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [bool]$Checked,
        [string]$Name
    )

    $patternObject = $null
    if (-not $Element.TryGetCurrentPattern(
            [System.Windows.Automation.TogglePattern]::Pattern,
            [ref]$patternObject)) {
        throw "$Name does not support TogglePattern."
    }

    $pattern = [System.Windows.Automation.TogglePattern]$patternObject
    $isChecked = $pattern.Current.ToggleState -eq [System.Windows.Automation.ToggleState]::On
    if ($isChecked -ne $Checked) {
        $pattern.Toggle()
        Start-Sleep -Milliseconds 250
    }
}

function Invoke-Button {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [string]$Name
    )

    $patternObject = $null
    if (-not $Element.TryGetCurrentPattern(
            [System.Windows.Automation.InvokePattern]::Pattern,
            [ref]$patternObject)) {
        throw "$Name does not support InvokePattern."
    }

    ([System.Windows.Automation.InvokePattern]$patternObject).Invoke()
}

function Wait-ForEnabled {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [string]$Name,
        [int]$TimeoutSeconds
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        if ($Element.Current.IsEnabled) {
            return
        }

        Start-Sleep -Milliseconds 250
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "$Name was not enabled within timeout."
}

function Expand-And-Collapse {
    param(
        [System.Windows.Automation.AutomationElement]$Element,
        [bool]$Expand
    )

    $patternObject = $null
    if (-not $Element.TryGetCurrentPattern(
            [System.Windows.Automation.ExpandCollapsePattern]::Pattern,
            [ref]$patternObject)) {
        throw "Element does not support ExpandCollapsePattern."
    }

    $pattern = [System.Windows.Automation.ExpandCollapsePattern]$patternObject
    if ($Expand) {
        $pattern.Expand()
    }
    else {
        $pattern.Collapse()
    }
}

function Count-Occurrences {
    param(
        [string]$Text,
        [string]$Value
    )

    if ([string]::IsNullOrEmpty($Text) -or [string]::IsNullOrEmpty($Value)) {
        return 0
    }

    $count = 0
    $startIndex = 0
    while ($true) {
        $index = $Text.IndexOf($Value, $startIndex, [StringComparison]::Ordinal)
        if ($index -lt 0) {
            return $count
        }

        $count++
        $startIndex = $index + $Value.Length
    }
}

function Get-StreamLogInfo {
    param([string]$Path)

    if (-not (Test-Path -LiteralPath $Path)) {
        return [pscustomobject]@{
            Content = ""
            LineCount = 0
            SummaryCount = 0
        }
    }

    $content = Get-Content -LiteralPath $Path -Raw
    if ([string]::IsNullOrEmpty($content)) {
        $content = ""
    }

    return [pscustomobject]@{
        Content = $content
        LineCount = ($content -split "`n" | Where-Object { $_.Length -gt 0 }).Count
        SummaryCount = Count-Occurrences -Text $content -Value "Total:"
    }
}

function Wait-ForStreamProgress {
    param(
        [string]$Path,
        [int]$TimeoutSeconds,
        [int]$MinimumSummaryCount,
        [int]$MinimumLineCount
    )

    $deadline = [DateTime]::UtcNow.AddSeconds($TimeoutSeconds)
    do {
        $info = Get-StreamLogInfo -Path $Path
        if ($info.Content.Contains("PULL / FETCH OPERATION") -and
            $info.SummaryCount -ge $MinimumSummaryCount -and
            $info.LineCount -ge $MinimumLineCount) {
            return $info
        }

        Start-Sleep -Milliseconds 500
    } while ([DateTime]::UtcNow -lt $deadline)

    throw "Streamed log progress did not reach $MinimumSummaryCount summaries and $MinimumLineCount lines within timeout: $Path"
}

if ($BranchCount -lt 1) {
    throw "BranchCount must be at least 1."
}

if ($RepeatCount -lt 1) {
    throw "RepeatCount must be at least 1."
}

if ($MinimumStreamLines -lt 1) {
    throw "MinimumStreamLines must be at least 1."
}

if (-not (Test-Path -LiteralPath $ExecutablePath)) {
    throw "App executable not found: $ExecutablePath. Run dotnet build first."
}

& (Join-Path $PSScriptRoot "Prepare-ManualProfilingFixture.ps1") `
    -Root $Root `
    -RepositoryCount 1 `
    -BranchCount $BranchCount | Out-Host

$runRoot = Get-LatestFixtureRun -FixtureRoot $Root
$settingsPath = Join-Path $runRoot "settings.profile.json"
if (-not (Test-Path -LiteralPath $settingsPath)) {
    throw "Fixture settings file not found: $settingsPath"
}

$streamLogPath = Join-Path $runRoot "pull-stream.log"
$settings = Get-Content -LiteralPath $settingsPath -Raw | ConvertFrom-Json
$settings.StreamLogs = $true
$settings.LogFilePath = $streamLogPath
$settings | ConvertTo-Json -Depth 8 | Set-Content -LiteralPath $settingsPath -Encoding UTF8

Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes

$branchName = "feature/team-00/area-00/branch-0000"
$processStartInfo = [System.Diagnostics.ProcessStartInfo]::new()
$processStartInfo.FileName = $ExecutablePath
$processStartInfo.WorkingDirectory = Split-Path -Parent $ExecutablePath
$processStartInfo.UseShellExecute = $false
$processStartInfo.EnvironmentVariables["MERGEPILOT_SETTINGS_PATH"] = $settingsPath

$process = [System.Diagnostics.Process]::Start($processStartInfo)
try {
    $rootElement = Wait-ForWindowElement -Process $process -TimeoutSeconds $WindowTimeoutSeconds
    $desktopElement = [System.Windows.Automation.AutomationElement]::RootElement

    $repoCheckBox = Wait-ForElement `
        -Description "Repository checkbox ProfileRepo01" `
        -TimeoutSeconds $WindowTimeoutSeconds `
        -Lookup { Find-ByNameAndControlType -Root $rootElement -Name "ProfileRepo01" -ControlType ([System.Windows.Automation.ControlType]::CheckBox) }
    Set-Checked -Element $repoCheckBox -Checked $true -Name "ProfileRepo01"

    $sourceBranchBox = Wait-ForElement `
        -Description "SourceBranchBox" `
        -TimeoutSeconds $WindowTimeoutSeconds `
        -Lookup { Find-ByAutomationId -Root $rootElement -AutomationId "SourceBranchBox" }
    Expand-And-Collapse -Element $sourceBranchBox -Expand $true
    Start-Sleep -Milliseconds 500

    $branchCheckBox = Wait-ForElement `
        -Description "Source branch checkbox $branchName" `
        -TimeoutSeconds $WindowTimeoutSeconds `
        -Lookup {
            $candidate = Find-ByNameAndControlType -Root $rootElement -Name $branchName -ControlType ([System.Windows.Automation.ControlType]::CheckBox)
            if ($null -eq $candidate) {
                $candidate = Find-ByNameAndControlType -Root $desktopElement -Name $branchName -ControlType ([System.Windows.Automation.ControlType]::CheckBox)
            }
            return $candidate
        }
    Set-Checked -Element $branchCheckBox -Checked $true -Name $branchName
    Expand-And-Collapse -Element $sourceBranchBox -Expand $false

    $pullButton = Wait-ForElement `
        -Description "PullButton" `
        -TimeoutSeconds $WindowTimeoutSeconds `
        -Lookup { Find-ByAutomationId -Root $rootElement -AutomationId "PullButton" }

    $streamInfo = $null
    for ($run = 1; $run -le $RepeatCount; $run++) {
        Wait-ForEnabled -Element $pullButton -Name "PullButton" -TimeoutSeconds $OperationTimeoutSeconds
        Invoke-Button -Element $pullButton -Name "PullButton"
        $streamInfo = Wait-ForStreamProgress `
            -Path $streamLogPath `
            -TimeoutSeconds $OperationTimeoutSeconds `
            -MinimumSummaryCount $run `
            -MinimumLineCount 1
    }

    $streamInfo = Wait-ForStreamProgress `
        -Path $streamLogPath `
        -TimeoutSeconds $OperationTimeoutSeconds `
        -MinimumSummaryCount $RepeatCount `
        -MinimumLineCount $MinimumStreamLines

    if (-not $process.CloseMainWindow()) {
        throw "CloseMainWindow returned false."
    }

    if (-not $process.WaitForExit($CloseTimeoutMilliseconds)) {
        throw "MergePilot did not exit within $CloseTimeoutMilliseconds ms after close request."
    }

    [pscustomobject]@{
        Result = "Passed"
        BranchName = $branchName
        StreamLogPath = $streamLogPath
        StreamLineCount = $streamInfo.LineCount
        StreamSummaryCount = $streamInfo.SummaryCount
        SettingsPath = $settingsPath
        BranchCount = $BranchCount
        RepeatCount = $RepeatCount
        ExitCode = $process.ExitCode
    }
}
finally {
    if ($null -ne $process -and -not $process.HasExited) {
        $process.Kill()
        $process.WaitForExit()
    }
}
