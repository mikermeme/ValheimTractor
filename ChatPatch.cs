namespace Tractor;

using HarmonyLib;

using static PluginConfig;

[HarmonyPatch(typeof(Chat))]
static class ChatPatch {
  [HarmonyPrefix]
  [HarmonyPatch(nameof(Chat.Update))]
  static bool UpdatePrefix() {
    if (!IsModEnabled.Value) {
      return true;
    }

    return !TractorPanel.IsOpen;
  }
}
