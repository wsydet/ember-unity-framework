param([int]$TargetPid,[string]$OutputRoot,[int]$MaxQueries=0,[int]$IntervalMs=100)
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class EmberWindowList {
 public delegate bool EnumCallback(IntPtr hwnd, IntPtr data);
 [DllImport("user32.dll")] public static extern bool EnumWindows(EnumCallback callback, IntPtr data);
 [DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd, out uint pid);
}
'@
$taskEnd=[DateTime]::UtcNow.AddSeconds(40)
$taskCount=0
$taskWriter=[System.IO.StreamWriter]::new((Join-Path $OutputRoot 'uia-events.jsonl'),$false,[System.Text.UTF8Encoding]::new($false))
$taskWriter.AutoFlush=$true
try {
 while([DateTime]::UtcNow -lt $taskEnd) {
  if(!(Get-Process -Id $TargetPid -ErrorAction SilentlyContinue)){break}
  $taskHandles=[System.Collections.Generic.List[IntPtr]]::new()
  $taskCallback=[EmberWindowList+EnumCallback]{param($window,$data);[uint32]$taskOwner=0;[void][EmberWindowList]::GetWindowThreadProcessId($window,[ref]$taskOwner);if($taskOwner -eq $TargetPid){$taskHandles.Add($window)};return $true}
  [void][EmberWindowList]::EnumWindows($taskCallback,[IntPtr]::Zero)
  foreach($taskWindow in $taskHandles) {
   try {
    $taskElement=[System.Windows.Automation.AutomationElement]::FromHandle($taskWindow)
    $taskName=$taskElement.Current.Name
    $taskChildren=$taskElement.FindAll([System.Windows.Automation.TreeScope]::Children,[System.Windows.Automation.Condition]::TrueCondition)
    $taskWriter.WriteLine(([pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');hwnd=$taskWindow.ToInt64();name=$taskName;children=$taskChildren.Count}|ConvertTo-Json -Compress))
    $taskCount++
    [System.IO.File]::WriteAllText((Join-Path $OutputRoot 'uia-ready.txt'),[string]$taskCount)
   } catch {$taskWriter.WriteLine(([pscustomobject]@{utc=[DateTime]::UtcNow.ToString('o');error=$_.Exception.Message}|ConvertTo-Json -Compress))}
  }
  if($MaxQueries -gt 0 -and $taskCount -ge $MaxQueries){break}
  Start-Sleep -Milliseconds $IntervalMs
 }
} finally {$taskWriter.Dispose()}
