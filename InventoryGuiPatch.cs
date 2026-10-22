namespace Tractor;

using HarmonyLib;

using static PluginConfig;

[HarmonyPatch(typeof(InventoryGui))]
static class InventoryGuiPatch {
  [HarmonyPrefix]
  [HarmonyPatch(nameof(InventoryGui.Update))]
  static bool UpdatePrefix() {
    if (!IsModEnabled.Value) {
      return true;
    }

    return !TractorPanel.IsOpen;
  }
}
