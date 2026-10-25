param([string] $AssemblyPath, [string] $OutputPath)
$ErrorActionPreference = 'Stop'
$managedPath = 'D:/SteamLibrary/steamapps/common/Valheim/valheim_Data/Managed'
$corePath = 'C:/Users/me/AppData/Roaming/r2modmanPlus-local/Valheim/profiles/test/BepInEx/core'
foreach ($name in @('UnityEngine.CoreModule', 'UnityEngine', 'assembly_utils', 'assembly_valheim')) {
  [Reflection.Assembly]::LoadFrom("$managedPath/$name.dll") | Out-Null
}
[Reflection.Assembly]::LoadFrom("$corePath/BepInEx.dll") | Out-Null
$assembly = [Reflection.Assembly]::LoadFile($AssemblyPath)
$catalog = $assembly.GetType('Tractor.ZdoCatalog', $true)
$flags = [Reflection.BindingFlags]'NonPublic,Static'
$catalog.GetMethod('EnsureLoaded', $flags).Invoke($null, @()) | Out-Null
$lines = [Collections.Generic.List[string]]::new()
$names = $catalog.GetField('_names', $flags).GetValue($null)
$bools = $catalog.GetField('_boolHashes', $flags).GetValue($null)
foreach ($hash in ($names.Keys | Sort-Object)) {
  $lines.Add("Name|$hash|$($names[$hash])|$($bools.Contains($hash))")
}
foreach ($hash in ($bools | Sort-Object)) { $lines.Add("Bool|$hash") }
$bases = $catalog.GetField('_classBases', $flags).GetValue($null)
$keys = $catalog.GetField('_classKeys', $flags).GetValue($null)
$classes = @($bases.Keys) + @($keys.Keys) + @('MissingClass') | Sort-Object -Unique
$collect = $catalog.GetMethod('CollectKnownKeys', [Reflection.BindingFlags]'Public,Static')
$dictionaryType = $collect.GetParameters()[1].ParameterType
foreach ($class in $classes) {
  $found = [Activator]::CreateInstance($dictionaryType)
  $collect.Invoke($null, @([string] $class, $found)) | Out-Null
  foreach ($hash in ($found.Keys | Sort-Object)) {
    $key = $found[$hash]
    $lines.Add("Class|$class|$hash|$($key.Name)|$($key.Type)|$($key.IsBool)")
  }
}
foreach ($name in @('ReviewKey', 'HasFieldsReviewComponent', 'ReviewKey', 'HasFieldsReviewComponent')) {
  $catalog.GetMethod('Register').Invoke($null, @($name)) | Out-Null
}
foreach ($hash in ($names.Keys | Sort-Object)) {
  $lines.Add("Registered|$hash|$($names[$hash])|$($bools.Contains($hash))")
}
$rowType = $assembly.GetType('Tractor.ZdoKeyRow', $true)
$enumType = $assembly.GetType('Tractor.ZdoKeyType', $true)
foreach ($number in -1..8) {
  foreach ($isBool in @($false, $true)) {
    foreach ($missing in @($false, $true)) {
      foreach ($name in @('level', '<123>', '<>', '')) {
        $row = [Activator]::CreateInstance($rowType, @([Enum]::ToObject($enumType, $number), 123, $name, 'value', $isBool, $missing))
        $lines.Add("Row|$number|$isBool|$missing|$name|$($row.TypeText)|$($row.CanEdit)|$($row.CanRemove)|$($row.IsUnresolved)")
      }
    }
  }
}
[IO.File]::WriteAllLines($OutputPath, $lines)
Write-Output "$($names.Count) names; $($bools.Count) bool hashes; $($classes.Count) component classes; 160 row cases; $($lines.Count) snapshot lines."
