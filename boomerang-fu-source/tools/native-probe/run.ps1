$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
[Console]::OutputEncoding = [System.Text.Encoding]::UTF8
$directory = 'D:\arkena\work\codex-boomerang-native-20261027'
$executable = Join-Path $directory 'Boomerang Fu.exe'
$existing = @(Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -eq $executable })
if ($existing.Count) { throw 'The isolated probe already has a process; refusing a duplicate.' }
if (!(Test-Path "$directory\BepInEx\plugins\NativeProbe.dll")) { throw 'Probe plugin is missing.' }
$info = New-Object System.Diagnostics.ProcessStartInfo
$info.FileName = $executable
$info.WorkingDirectory = $directory
$info.UseShellExecute = $false
$info.CreateNoWindow = $true
$info.WindowStyle = [System.Diagnostics.ProcessWindowStyle]::Hidden
$info.Arguments = '-batchmode -nographics -job-worker-count 2 -logFile "' + $directory + '\native-player.log"'
$process = New-Object System.Diagnostics.Process
$process.StartInfo = $info
$timer = [Diagnostics.Stopwatch]::StartNew()
$startedUtc = [DateTime]::UtcNow
$peak = 0
$reason = 'normal exit'
$process.Start() | Out-Null
$ownedId = $process.Id
try {
    try { if (!$process.HasExited) { $process.ProcessorAffinity = [IntPtr]3 } } catch { if (!$process.HasExited) { throw } }
    while (!$process.WaitForExit(100)) {
        $process.Refresh()
        $peak = [math]::Max($peak, $process.WorkingSet64)
        if ($timer.Elapsed.TotalSeconds -ge 30) { $reason = '30 second timeout'; break }
        if ($peak -gt 2GB) { $reason = '2 GiB memory limit'; break }
        if ((Test-Path "$directory\native-player.log") -and (Get-Item "$directory\native-player.log").Length -gt 5MB) { $reason = '5 MiB log limit'; break }
    }
} finally {
    # The Process object retains the handle of the process started above.
    if (!$process.HasExited) { $process.Kill(); $process.WaitForExit(5000) | Out-Null }
    $exitCode = $process.ExitCode
    $process.Dispose()
}
$remaining = @(Get-CimInstance Win32_Process | Where-Object { $_.ExecutablePath -eq $executable } | Select-Object ProcessId,ExecutablePath)
[pscustomobject]@{
    pid = $ownedId; exitCode = $exitCode; reason = $reason
    seconds = $timer.Elapsed.TotalSeconds; peakWorkingSetBytes = $peak
    processLimit = 1; cpuAffinityMask = 3; jobWorkerCount = 2
    memoryLimitBytes = 2GB; timeoutSeconds = 30
    remaining = $remaining
    resultExists = (Test-Path "$directory\native-probe.json") -and ((Get-Item "$directory\native-probe.json").LastWriteTimeUtc -ge $startedUtc)
} | ConvertTo-Json -Depth 4 | Tee-Object -FilePath "$directory\launch-report.json"
