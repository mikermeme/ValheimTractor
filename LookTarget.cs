namespace Tractor;

using System.Collections.Generic;
using System.Reflection;

using HarmonyLib;

using UnityEngine;

// Shift+RightClick on nothing used to do nothing. This finds a networked object in a sphere that
// touches the player and reaches along the look direction, so effects and markers with no hover
// collider can still be opened.
public static class LookTarget {
  const int MaxStages = 3;

  // ZNetScene keeps its live instances in a private dictionary. A direct field access fails Mono's JIT
  // access check in the real assembly, so read it through reflection.
  static readonly FieldInfo InstancesField = AccessTools.Field(typeof(ZNetScene), "m_instances");

  static bool _reportedMissingField;

  static Dictionary<ZDO, ZNetView> GetInstances(ZNetScene scene) {
    if (InstancesField == null) {
      if (!_reportedMissingField) {
        _reportedMissingField = true;
        Tractor.LogError("Look target disabled, cannot find ZNetScene.m_instances.");
      }

      return null;
    }

    return InstancesField.GetValue(scene) as Dictionary<ZDO, ZNetView>;
  }

  public static ZNetView FindInFront(Player player, float baseRadius) {
    if (!player || !ZNetScene.instance) {
      return null;
    }

    Vector3 origin = player.m_eye
        ? player.m_eye.position
        : player.transform.position + Vector3.up * 1.5f;

    Vector3 direction = player.GetLookDir();

    if (direction.sqrMagnitude < 0.001f) {
      return null;
    }

    direction.Normalize();

    Dictionary<ZDO, ZNetView> instances = GetInstances(ZNetScene.instance);

    if (instances == null) {
      return null;
    }

    // The sphere starts tangent to the player and doubles twice, so a small miss still finds the effect.
    for (int stage = 0; stage < MaxStages; stage++) {
      float radius = baseRadius * (1 << stage);
      ZNetView found = FindInSphere(instances, origin + direction * radius, radius, player);

      if (found) {
        return found;
      }
    }

    return null;
  }

  static ZNetView FindInSphere(
      Dictionary<ZDO, ZNetView> instances, Vector3 center, float radius, Player player) {
    float sqrRadius = radius * radius;
    Transform playerTransform = player.transform;

    ZNetView bestVisible = null;
    float bestVisibleDistance = float.MaxValue;
    ZNetView bestInvisible = null;
    float bestInvisibleDistance = float.MaxValue;
    ZNetView bestSolid = null;
    float bestSolidDistance = float.MaxValue;

    foreach (ZNetView netView in instances.Values) {
      if (!netView || !netView.IsValid()) {
        continue;
      }

      Transform transform = netView.transform;

      if (transform == playerTransform || transform.IsChildOf(playerTransform)) {
        continue;
      }

      float sqrDistance = (transform.position - center).sqrMagnitude;

      if (sqrDistance > sqrRadius) {
        continue;
      }

      if (netView.GetComponentInChildren<Collider>(includeInactive: true)) {
        if (sqrDistance < bestSolidDistance) {
          bestSolid = netView;
          bestSolidDistance = sqrDistance;
        }
      } else if (netView.GetComponentInChildren<Renderer>(includeInactive: true)) {
        if (sqrDistance < bestVisibleDistance) {
          bestVisible = netView;
          bestVisibleDistance = sqrDistance;
        }
      } else if (sqrDistance < bestInvisibleDistance) {
        bestInvisible = netView;
        bestInvisibleDistance = sqrDistance;
      }
    }

    // Effects and markers first, then bare objects, then anything with a collider.
    return bestVisible ?? bestInvisible ?? bestSolid;
  }
}
