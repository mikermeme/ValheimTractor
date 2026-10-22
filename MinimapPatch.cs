namespace Tractor;

using HarmonyLib;

using static PluginConfig;

[HarmonyPatch(typeof(Minimap))]
static class MinimapPatch {
  [HarmonyPrefix]
  [HarmonyPatch(nameof(Minimap.Update))]
  static bool UpdatePrefix() {
    if (!IsModEnabled.Value) {
      return true;
    }

    return !TractorPanel.IsOpen;
  }
}
