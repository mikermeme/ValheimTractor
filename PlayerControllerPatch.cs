namespace Tractor;

using HarmonyLib;

using static PluginConfig;

[HarmonyPatch(typeof(PlayerController))]
static class PlayerControllerPatch {
  [HarmonyPrefix]
  [HarmonyPatch(nameof(PlayerController.TakeInput))]
  static bool TakeInputPrefix() {
    if (!IsModEnabled.Value) {
      return true;
    }

    return !TractorPanel.IsOpen;
  }
}
