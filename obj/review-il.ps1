$ErrorActionPreference = 'Stop'
[Reflection.Assembly]::LoadFrom('C:/Users/me/AppData/Roaming/r2modmanPlus-local/Valheim/profiles/test/BepInEx/core/Mono.Cecil.dll') | Out-Null
function Get-Methods($type) {
  foreach ($method in $type.Methods) {
    if ($method.HasBody) { $method }
  }
  foreach ($nested in $type.NestedTypes) { Get-Methods $nested }
}
function Read-Methods([string] $path) {
  $module = [Mono.Cecil.ModuleDefinition]::ReadModule($path)
  $result = @{}
  foreach ($type in $module.Types) {
    foreach ($method in (Get-Methods $type)) {
      $result[$method.FullName] = ($method.Body.Instructions | ForEach-Object { $_.ToString() }) -join "`n"
    }
  }
  $module.Dispose()
  return $result
}
$before = Read-Methods (Join-Path (Get-Content obj/review-backup-path.txt) 'Tractor.dll')
$after = Read-Methods 'D:/Codex/Valheim/Tractor/bin/Release/net48/Tractor.dll'
$same = 0
foreach ($name in ($before.Keys | Sort-Object)) {
  if (!$after.ContainsKey($name)) { Write-Output "Moved: $name" }
  elseif ($before[$name] -cne $after[$name]) { Write-Output "Changed: $name" }
  else { $same++ }
}
foreach ($name in ($after.Keys | Sort-Object)) {
  if (!$before.ContainsKey($name)) { Write-Output "Added: $name" }
}
Write-Output "Unchanged compiled method bodies: $same"
