namespace Tractor;

using System.Collections.Generic;
using System.Reflection.Emit;

using HarmonyLib;

using UnityEngine;

using static PluginConfig;

[HarmonyPatch(typeof(Player))]
static class PlayerPatch {
  static int _shortcutFrame = -1;

  [HarmonyPrefix]
  [HarmonyPatch(nameof(Player.TakeInput))]
  static bool TakeInputPrefix() {
    if (!IsModEnabled.Value) {
      return true;
    }

    if (_shortcutFrame == Time.frameCount) {
      return false;
    }

    if (IsShortcutPressed()) {
      _shortcutFrame = Time.frameCount;
      TractorPanel.ToggleHovered();
      return false;
    }

    return !TractorPanel.IsOpen;
  }

  [HarmonyTranspiler]
  [HarmonyPatch(nameof(Player.Update))]
  static IEnumerable<CodeInstruction> UpdateTranspiler(IEnumerable<CodeInstruction> instructions) {
    return new CodeMatcher(instructions)
        .Start()
        .MatchStartForward(
            new CodeMatch(OpCodes.Call, AccessTools.Method(typeof(Player), nameof(Player.UpdateHover))))
        .ThrowIfInvalid("Could not patch Player.Update()! (update-hover)")
        .Advance(offset: 1)
        .InsertAndAdvance(
            new CodeInstruction(
                OpCodes.Call, AccessTools.Method(typeof(PlayerPatch), nameof(UpdateHoverPostDelegate))))
        .InstructionEnumeration();
  }

  static void UpdateHoverPostDelegate() {
    if (!IsModEnabled.Value) {
      return;
    }

    TractorPanel.Update();
  }

  static bool IsShortcutPressed() {
    return ZInput.GetKeyDown(OpenKey.Value) || (IsShiftHeld() && ZInput.GetMouseButtonDown(1));
  }

  static bool IsShiftHeld() {
    return ZInput.GetKey(KeyCode.LeftShift) || ZInput.GetKey(KeyCode.RightShift);
  }
}
