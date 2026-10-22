namespace Tractor;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using System.Text;

using UnityEngine;

using static PluginConfig;

public static class ZdoEditor {
  const string HasFieldsKey = "HasFields";

  public static List<ZdoKeyRow> ReadKeys(ZDO zdo, ZNetView netView, HashSet<int> hiddenHashes) {
    ZdoData data = ZdoData.Read(zdo);
    List<ZdoKeyRow> rows = new();

    AddRows(rows, ZdoKeyType.Float, data.Floats, FormatFloat);
    AddRows(rows, ZdoKeyType.Vector3, data.Vec3s, FormatVector3);
    AddRows(rows, ZdoKeyType.Quaternion, data.Quaternions, FormatQuaternion);
    AddIntRows(rows, data.Ints);
    AddRows(rows, ZdoKeyType.Long, data.Longs, FormatLong);
    AddRows(rows, ZdoKeyType.String, data.Strings, value => Escape(value));
    AddRows(rows, ZdoKeyType.Bytes, data.ByteArrays, FormatBytes);

    ZDOConnection connection = data.Connection;

    if (connection != null && connection.m_type != ZDOExtraData.ConnectionType.None) {
      rows.Add(
          new ZdoKeyRow(
              ZdoKeyType.Connection,
              0,
              "connection",
              $"{connection.m_type} -> {connection.m_target}",
              false));
    }

    AddKnownKeyRows(rows, data, netView);
    AddObservedKeyRows(zdo, rows, data.GetHashes());
    RecordObservedKeys(zdo, rows, hiddenHashes);
    RemoveHiddenRows(rows, hiddenHashes);

    rows.Sort(CompareKeyRows);
    return rows;
  }

  // Keys already seen on this prefab, so a key whose value was removed stays listed as not set.
  // keys.tsv only names keys per component type, and some keys (CheatUtils.cheated, for example)
  // belong to no component, so they would otherwise vanish with their value.
  static readonly Dictionary<int, Dictionary<int, ZdoKnownKey>> _observedKeys = new();

  static void AddObservedKeyRows(ZDO zdo, List<ZdoKeyRow> rows, HashSet<int> storedHashes) {
    int prefabHash = zdo.GetPrefab();

    if (!_observedKeys.TryGetValue(prefabHash, out Dictionary<int, ZdoKnownKey> observed)) {
      return;
    }

    foreach (KeyValuePair<int, ZdoKnownKey> pair in observed) {
      if (storedHashes.Contains(pair.Key) || HasKeyRow(rows, pair.Key)) {
        continue;
      }

      ZdoKnownKey key = pair.Value;

      rows.Add(
          new ZdoKeyRow(
              key.Type,
              key.Hash,
              key.Name,
              GetDefaultKeyText(key.Type, key.IsBool),
              key.IsBool,
              isMissing: true));
    }
  }

  static void RecordObservedKeys(ZDO zdo, List<ZdoKeyRow> rows, HashSet<int> hiddenHashes) {
    int prefabHash = zdo.GetPrefab();

    if (!_observedKeys.TryGetValue(prefabHash, out Dictionary<int, ZdoKnownKey> observed)) {
      observed = new Dictionary<int, ZdoKnownKey>();
      _observedKeys[prefabHash] = observed;
    }

    for (int i = 0; i < rows.Count; i++) {
      ZdoKeyRow row = rows[i];

      if (row.Type == ZdoKeyType.Connection
          || row.IsUnresolved
          || observed.ContainsKey(row.Hash)
          || (hiddenHashes != null && hiddenHashes.Contains(row.Hash))) {
        continue;
      }

      observed.Add(row.Hash, new ZdoKnownKey(row.Name, row.Hash, row.Type, row.IsBool));
    }
  }

  static bool HasKeyRow(List<ZdoKeyRow> rows, int hash) {
    for (int i = 0; i < rows.Count; i++) {
      if (rows[i].Hash == hash) {
        return true;
      }
    }

    return false;
  }

  // A stored field override is also a raw ZDO entry, so the field row owns it.
  static void RemoveHiddenRows(List<ZdoKeyRow> rows, HashSet<int> hiddenHashes) {
    if (hiddenHashes == null || hiddenHashes.Count == 0) {
      return;
    }

    for (int i = rows.Count - 1; i >= 0; i--) {
      if (hiddenHashes.Contains(rows[i].Hash)) {
        rows.RemoveAt(i);
      }
    }
  }

  static void AddKnownKeyRows(List<ZdoKeyRow> rows, ZdoData data, ZNetView netView) {
    if (!netView || !netView.IsValid()) {
      return;
    }

    HashSet<int> storedHashes = data.GetHashes();
    Dictionary<int, ZdoKnownKey> knownKeys = new();
    MonoBehaviour[] components = netView.GetComponentsInChildren<MonoBehaviour>();

    for (int i = 0; i < components.Length; i++) {
      MonoBehaviour component = components[i];

      if (component) {
        ZdoCatalog.CollectKnownKeys(component.GetType().Name, knownKeys);
      }
    }

    foreach (KeyValuePair<int, ZdoKnownKey> pair in knownKeys) {
      ZdoKnownKey key = pair.Value;

      if (storedHashes.Contains(key.Hash)) {
        continue;
      }

      rows.Add(
          new ZdoKeyRow(
              key.Type,
              key.Hash,
              key.Name,
              GetDefaultKeyText(key.Type, key.IsBool),
              key.IsBool,
              isMissing: true));
    }
  }

  public static List<ZdoFieldRow> ReadFields(ZDO zdo, ZNetView netView) {
    HashSet<int> overrideHashes = ZdoData.Read(zdo).GetHashes();
    List<ZdoFieldRow> rows = new();
    HashSet<string> seen = new(StringComparer.Ordinal);

    MonoBehaviour[] components = netView.GetComponentsInChildren<MonoBehaviour>();

    for (int i = 0; i < components.Length; i++) {
      MonoBehaviour component = components[i];

      if (!component) {
        continue;
      }

      Type componentType = component.GetType();
      FieldInfo[] fields = componentType.GetFields(BindingFlags.Instance | BindingFlags.Public);

      ZdoCatalog.Register(HasFieldsKey + componentType.Name);

      for (int j = 0; j < fields.Length; j++) {
        FieldInfo field = fields[j];

        if (!IsLoadableField(field.FieldType)) {
          continue;
        }

        string name = componentType.Name + "." + field.Name;
        int hash = name.GetStableHashCode();
        bool hasOverride = overrideHashes.Contains(hash);

        if (!seen.Add(name)) {
          continue;
        }

        rows.Add(
            new ZdoFieldRow(
                component,
                field,
                ReadFieldText(zdo, field, component, hash, hasOverride),
                hasOverride));
      }
    }

    rows.Sort(CompareFieldRows);
    return rows;
  }

  public static bool TryApplyKey(ZdoTarget target, ZdoKeyRow row, string text, out string error) {
    if (!TryParseKeyValue(row, text, out object value, out error)) {
      return false;
    }

    if (!TryClaimOwnership(target, out ZDO zdo, out ZNetView netView, out error)) {
      return false;
    }

    SetKeyValue(zdo, row, value);
    ApplyLiveSetters(netView, row, value);
    row.IsMissing = false;
    row.ValueText = FormatKeyValue(row, value);
    return true;
  }

  public static bool TryApplyField(ZdoTarget target, ZdoFieldRow row, string text, out string error) {
    if (!TryParseFieldValue(row.Field.FieldType, text, out object zdoValue, out object liveValue, out error)) {
      return false;
    }

    if (ZdoFieldRow.IsPartOfSelf(row.Component, liveValue)) {
      error = "That is a part of this object. Enter a prefab name such as 'fx_crit'.";
      return false;
    }

    if (!TryClaimOwnership(target, out ZDO zdo, out ZNetView netView, out error)) {
      return false;
    }

    string componentName = row.ComponentType.Name;

    zdo.Set(HasFieldsKey, true);
    zdo.Set(HasFieldsKey + componentName, true);
    SetFieldValue(zdo, row, zdoValue);

    if (netView && netView.IsValid()) {
      netView.LoadFields();
    }

    if (row.Component) {
      row.Field.SetValue(row.Component, liveValue);
    }

    row.HasOverride = true;
    row.ValueText = FormatFieldValue(zdoValue);
    return true;
  }

  public static bool TryResetField(ZdoTarget target, ZdoFieldRow row, out string error) {
    if (!TryClaimOwnership(target, out ZDO zdo, out ZNetView netView, out error)) {
      return false;
    }

    zdo.Remove(row.Hash);

    if (ZNetScene.instance) {
      GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());

      if (prefab) {
        Component prefabComponent = FindComponent(prefab, row.ComponentType);

        if (prefabComponent && row.Component) {
          row.Field.SetValue(row.Component, row.Field.GetValue(prefabComponent));
        }
      }
    }

    if (netView && netView.IsValid()) {
      netView.LoadFields();
    }

    row.HasOverride = false;

    if (row.Component) {
      row.ValueText = FormatFieldValue(row.Field.GetValue(row.Component));
    }

    return true;
  }

  public static bool TryRemoveKey(ZdoTarget target, ZdoKeyRow row, out string error) {
    if (!TryClaimOwnership(target, out ZDO zdo, out _, out error)) {
      return false;
    }

    zdo.Remove(row.Hash);

    if (row.Name.EndsWith("_u", StringComparison.Ordinal)
        || row.Name.EndsWith("_i", StringComparison.Ordinal)) {
      zdo.RemoveZDOID(row.Name.Substring(0, row.Name.Length - 2));
    }

    row.ValueText = string.Empty;
    return true;
  }

  public static int BulkApplyKey(
      ZdoTarget target,
      ZdoKeyRow row,
      string text,
      bool zoneOnly,
      out int candidateCount,
      out string error) {
    candidateCount = 0;

    if (!TryParseKeyValue(row, text, out object value, out error)) {
      return 0;
    }

    ZDO targetZdo = target.Zdo;

    if (targetZdo == null) {
      error = "Target ZDO is gone.";
      return 0;
    }

    List<ZDO> candidates = GetCandidates(targetZdo, zoneOnly, BulkRadius.Value);
    int applied = 0;

    for (int i = 0; i < candidates.Count; i++) {
      ZDO zdo = candidates[i];

      if (zdo.GetPrefab() != targetZdo.GetPrefab()) {
        continue;
      }

      candidateCount++;
      Claim(zdo);
      SetKeyValue(zdo, row, value);

      if (zdo.m_uid == targetZdo.m_uid) {
        row.IsMissing = false;
        row.ValueText = FormatKeyValue(row, value);
      }

      applied++;
    }

    error = null;
    return applied;
  }

  public static int BulkApplyField(
      ZdoTarget target,
      ZdoFieldRow row,
      string text,
      bool zoneOnly,
      bool matchComponent,
      out int candidateCount,
      out string error) {
    candidateCount = 0;

    if (!TryParseFieldValue(
            row.Field.FieldType, text, out object zdoValue, out object liveValue, out error)) {
      return 0;
    }

    if (ZdoFieldRow.IsPartOfSelf(row.Component, liveValue)) {
      error = "That is a part of this object. Enter a prefab name such as 'fx_crit'.";
      return 0;
    }

    ZDO targetZdo = target.Zdo;

    if (targetZdo == null) {
      error = "Target ZDO is gone.";
      return 0;
    }

    if (!ZNetScene.instance) {
      error = "ZNetScene is not loaded.";
      return 0;
    }

    List<ZDO> candidates = GetCandidates(targetZdo, zoneOnly, BulkRadius.Value);
    int applied = 0;
    string componentName = row.ComponentType.Name;

    for (int i = 0; i < candidates.Count; i++) {
      ZDO zdo = candidates[i];

      if (matchComponent) {
        GameObject prefab = ZNetScene.instance.GetPrefab(zdo.GetPrefab());

        if (!prefab || !FindComponent(prefab, row.ComponentType)) {
          continue;
        }
      } else if (zdo.GetPrefab() != targetZdo.GetPrefab()) {
        continue;
      }

      candidateCount++;
      Claim(zdo);

      zdo.Set(HasFieldsKey, true);
      zdo.Set(HasFieldsKey + componentName, true);
      SetFieldValue(zdo, row, zdoValue);

      ZNetView netView = ZNetScene.instance.FindInstance(zdo);

      if (netView && netView.IsValid()) {
        netView.LoadFields();
      }

      if (zdo.m_uid == targetZdo.m_uid) {
        row.HasOverride = true;
        row.ValueText = FormatFieldValue(zdoValue);
      }

      applied++;
    }

    error = null;
    return applied;
  }

  static Component FindComponent(GameObject gameObject, Type componentType) {
    Component[] components = gameObject.GetComponentsInChildren(componentType, includeInactive: true);

    for (int i = 0; i < components.Length; i++) {
      if (components[i].GetType() == componentType) {
        return components[i];
      }
    }

    return null;
  }

  static List<ZDO> GetCandidates(ZDO target, bool zoneOnly, float radius) {
    List<ZDO> candidates = new();
    ZDOMan zdoMan = ZDOMan.instance;

    if (zdoMan == null) {
      return candidates;
    }

    Dictionary<ZDOID, ZDO> zdos = Access.ZdosById(zdoMan);

    if (zdos == null) {
      return candidates;
    }

    Vector3 position = target.GetPosition();
    Vector2s sector = target.GetSector();
    float radiusSquared = radius * radius;

    foreach (KeyValuePair<ZDOID, ZDO> pair in zdos) {
      ZDO zdo = pair.Value;

      if (zdo == null) {
        continue;
      }

      if (zoneOnly) {
        if (zdo.GetSector() != sector) {
          continue;
        }
      } else if ((zdo.GetPosition() - position).sqrMagnitude > radiusSquared) {
        continue;
      }

      candidates.Add(zdo);
    }

    return candidates;
  }

  static bool TryClaimOwnership(
      ZdoTarget target, out ZDO zdo, out ZNetView netView, out string error) {
    error = null;
    zdo = target.Zdo;
    netView = null;

    if (zdo == null) {
      error = "Target ZDO is gone.";
      return false;
    }

    netView = target.NetView;

    if (netView && netView.IsValid()) {
      netView.ClaimOwnership();
    } else {
      Claim(zdo);
    }

    return true;
  }

  static void Claim(ZDO zdo) {
    if (!zdo.IsOwner()) {
      zdo.SetOwner(ZDOMan.GetSessionID());
    }
  }

  // Keys such as 'level' are cached in private component fields at spawn time. Mirroring the value
  // into the matching vanilla setter keeps the live instance in sync without a custom RPC.
  static void ApplyLiveSetters(ZNetView netView, ZdoKeyRow row, object value) {
    if (!netView || !netView.IsValid() || row.Type == ZdoKeyType.Connection) {
      return;
    }

    string methodName = "Set" + ToPascalCase(row.Name);
    MonoBehaviour[] components = netView.GetComponentsInChildren<MonoBehaviour>();

    for (int i = 0; i < components.Length; i++) {
      MonoBehaviour component = components[i];

      if (!component) {
        continue;
      }

      MethodInfo setter = FindSetter(component.GetType(), methodName, value, row.IsBool);

      if (setter == null) {
        continue;
      }

      try {
        setter.Invoke(
            component,
            [ConvertSetterArgument(setter.GetParameters()[0].ParameterType, value)]);
      } catch (Exception exception) {
        Tractor.LogWarning($"{component.GetType().Name}.{methodName} failed: {exception.Message}");
      }
    }
  }

  static MethodInfo FindSetter(Type type, string methodName, object value, bool isBooleanKey) {
    Type valueType = value.GetType();
    MethodInfo[] methods = type.GetMethods(BindingFlags.Instance | BindingFlags.Public);

    for (int i = 0; i < methods.Length; i++) {
      MethodInfo method = methods[i];

      if (method.Name != methodName) {
        continue;
      }

      ParameterInfo[] parameters = method.GetParameters();

      if (parameters.Length != 1) {
        continue;
      }

      Type parameterType = parameters[0].ParameterType;

      // Boolean ZDO keys are stored as int, so accept a bool setter for those keys only.
      if (parameterType == valueType
          || (isBooleanKey && parameterType == typeof(bool) && valueType == typeof(int))) {
        return method;
      }
    }

    return null;
  }

  static object ConvertSetterArgument(Type parameterType, object value) {
    if (parameterType == typeof(bool) && value is int intValue) {
      return intValue != 0;
    }

    return value;
  }

  static string ToPascalCase(string name) {
    StringBuilder builder = new(name.Length);
    bool capitalize = true;

    for (int i = 0; i < name.Length; i++) {
      char character = name[i];

      if (character == '_') {
        capitalize = true;
        continue;
      }

      builder.Append(capitalize ? char.ToUpperInvariant(character) : character);
      capitalize = false;
    }

    return builder.ToString();
  }

  static string GetDefaultKeyText(ZdoKeyType type, bool isBool) {
    switch (type) {
      case ZdoKeyType.Float:
      case ZdoKeyType.Long:
        return "0";
      case ZdoKeyType.Int:
        return isBool ? "false" : "0";
      case ZdoKeyType.Vector3:
        return "0, 0, 0";
      case ZdoKeyType.Quaternion:
        return "0, 0, 0, 1";
      default:
        return string.Empty;
    }
  }

  static void SetKeyValue(ZDO zdo, ZdoKeyRow row, object value) {
    switch (row.Type) {
      case ZdoKeyType.Float:
        zdo.Set(row.Hash, (float) value);
        break;
      case ZdoKeyType.Vector3:
        zdo.Set(row.Hash, (Vector3) value);
        break;
      case ZdoKeyType.Quaternion:
        zdo.Set(row.Hash, (Quaternion) value);
        break;
      case ZdoKeyType.Int:
        zdo.Set(row.Hash, (int) value);
        break;
      case ZdoKeyType.Long:
        zdo.Set(row.Hash, (long) value);
        break;
      case ZdoKeyType.String:
        zdo.Set(row.Hash, (string) value);
        break;
      case ZdoKeyType.Bytes:
        zdo.Set(row.Hash, (byte[]) value);
        break;
    }
  }

  static void SetFieldValue(ZDO zdo, ZdoFieldRow row, object value) {
    Type fieldType = row.Field.FieldType;

    if (fieldType == typeof(int)) {
      zdo.Set(row.Hash, (int) value);
    } else if (fieldType == typeof(float)) {
      zdo.Set(row.Hash, (float) value);
    } else if (fieldType == typeof(bool)) {
      zdo.Set(row.Hash, (bool) value);
    } else if (fieldType == typeof(Vector3)) {
      zdo.Set(row.Hash, (Vector3) value);
    } else if (fieldType == typeof(string)) {
      zdo.Set(row.Hash, (string) value);
    } else if (fieldType == typeof(GameObject) || fieldType == typeof(ItemDrop)) {
      zdo.Set(row.Hash, (string) value);
    }
  }

  static bool TryParseKeyValue(ZdoKeyRow row, string text, out object value, out string error) {
    value = null;

    switch (row.Type) {
      case ZdoKeyType.Float:
        if (!TryParseFloat(text, out float floatValue, out error)) {
          return false;
        }

        value = floatValue;
        return true;
      case ZdoKeyType.Vector3:
        if (!TryParseVector3(text, out Vector3 vec3Value, out error)) {
          return false;
        }

        value = vec3Value;
        return true;
      case ZdoKeyType.Quaternion:
        if (!TryParseQuaternion(text, out Quaternion quatValue, out error)) {
          return false;
        }

        value = quatValue;
        return true;
      case ZdoKeyType.Int:
        if (row.IsBool) {
          if (!TryParseBool(text, out bool boolValue, out error)) {
            return false;
          }

          value = boolValue ? 1 : 0;
          return true;
        }

        if (!TryParseInt(text, out int intValue, out error)) {
          // Item keys such as ShoulderItem store the stable hash of a prefab name as an int.
          string name = text.Trim();

          if (name.Length == 0) {
            error = "Enter a whole number or a prefab name.";
            return false;
          }

          value = name.GetStableHashCode();
          error = null;
          return true;
        }

        value = intValue;
        return true;
      case ZdoKeyType.Long:
        if (!TryParseLong(text, out long longValue, out error)) {
          return false;
        }

        value = longValue;
        return true;
      case ZdoKeyType.String:
        value = Unescape(text);
        error = null;
        return true;
      case ZdoKeyType.Bytes:
        try {
          value = Convert.FromBase64String(text.Trim());
          error = null;
          return true;
        } catch (FormatException) {
          error = "Expected a Base64 string.";
          return false;
        }
      default:
        error = "Connections are read-only.";
        return false;
    }
  }

  static bool TryParseFieldValue(
      Type fieldType, string text, out object zdoValue, out object liveValue, out string error) {
    zdoValue = null;
    liveValue = null;
    error = null;

    if (fieldType == typeof(int)) {
      if (!TryParseInt(text, out int value, out error)) {
        return false;
      }

      zdoValue = value;
      liveValue = value;
      return true;
    }

    if (fieldType == typeof(float)) {
      if (!TryParseFloat(text, out float value, out error)) {
        return false;
      }

      zdoValue = value;
      liveValue = value;
      return true;
    }

    if (fieldType == typeof(bool)) {
      if (!TryParseBool(text, out bool value, out error)) {
        return false;
      }

      zdoValue = value;
      liveValue = value;
      return true;
    }

    if (fieldType == typeof(Vector3)) {
      if (!TryParseVector3(text, out Vector3 value, out error)) {
        return false;
      }

      zdoValue = value;
      liveValue = value;
      return true;
    }

    if (fieldType == typeof(string)) {
      string value = Unescape(text);
      zdoValue = value;
      liveValue = value;
      return true;
    }

    if (fieldType == typeof(GameObject)) {
      if (!TryGetPrefab(text, out GameObject prefab, out error)) {
        return false;
      }

      zdoValue = prefab.name;
      liveValue = prefab;
      return true;
    }

    if (fieldType == typeof(ItemDrop)) {
      if (!TryGetPrefab(text, out GameObject prefab, out error)) {
        return false;
      }

      ItemDrop itemDrop = prefab.GetComponent<ItemDrop>();

      if (!itemDrop) {
        error = $"Prefab '{prefab.name}' has no ItemDrop component.";
        return false;
      }

      zdoValue = prefab.name;
      liveValue = itemDrop;
      return true;
    }

    error = $"Unsupported field type: {fieldType.Name}.";
    return false;
  }

  static bool TryGetPrefab(string text, out GameObject prefab, out string error) {
    prefab = null;
    error = null;
    string name = text.Trim();

    if (name.Length == 0) {
      error = "Enter a prefab name.";
      return false;
    }

    if (!ZNetScene.instance) {
      error = "ZNetScene is not loaded.";
      return false;
    }

    prefab = ZNetScene.instance.GetPrefab(name);

    if (!prefab) {
      error = $"No prefab named '{name}'.";
      return false;
    }

    return true;
  }

  static bool TryParseFloat(string text, out float value, out string error) {
    error = null;

    if (!float.TryParse(text.Trim(), NumberStyles.Float, CultureInfo.InvariantCulture, out value)
        || float.IsNaN(value)
        || float.IsInfinity(value)) {
      error = $"'{text}' is not a valid number.";
      return false;
    }

    return true;
  }

  static bool TryParseInt(string text, out int value, out string error) {
    error = null;

    if (!int.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) {
      error = $"'{text}' is not a whole number.";
      return false;
    }

    return true;
  }

  static bool TryParseLong(string text, out long value, out string error) {
    error = null;

    if (!long.TryParse(text.Trim(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value)) {
      error = $"'{text}' is not a whole number.";
      return false;
    }

    return true;
  }

  static bool TryParseBool(string text, out bool value, out string error) {
    error = null;
    string trimmed = text.Trim();

    if (bool.TryParse(trimmed, out value)) {
      return true;
    }

    if (trimmed == "1") {
      value = true;
      return true;
    }

    if (trimmed == "0") {
      value = false;
      return true;
    }

    error = $"'{text}' is not true or false.";
    return false;
  }

  static bool TryParseVector3(string text, out Vector3 value, out string error) {
    value = Vector3.zero;
    string[] parts = text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length != 3) {
      error = "Expected three numbers: x, y, z.";
      return false;
    }

    if (!TryParseFloat(parts[0], out float x, out error)
        || !TryParseFloat(parts[1], out float y, out error)
        || !TryParseFloat(parts[2], out float z, out error)) {
      return false;
    }

    value = new Vector3(x, y, z);
    return true;
  }

  static bool TryParseQuaternion(string text, out Quaternion value, out string error) {
    value = Quaternion.identity;
    string[] parts = text.Split(new[] { ',', ' ', ';' }, StringSplitOptions.RemoveEmptyEntries);

    if (parts.Length != 4) {
      error = "Expected four numbers: x, y, z, w.";
      return false;
    }

    if (!TryParseFloat(parts[0], out float x, out error)
        || !TryParseFloat(parts[1], out float y, out error)
        || !TryParseFloat(parts[2], out float z, out error)
        || !TryParseFloat(parts[3], out float w, out error)) {
      return false;
    }

    value = new Quaternion(x, y, z, w);
    return true;
  }

  static bool IsLoadableField(Type fieldType) {
    return fieldType == typeof(int)
        || fieldType == typeof(float)
        || fieldType == typeof(bool)
        || fieldType == typeof(Vector3)
        || fieldType == typeof(string)
        || fieldType == typeof(GameObject)
        || fieldType == typeof(ItemDrop);
  }

  static string ReadFieldText(
      ZDO zdo, FieldInfo field, MonoBehaviour component, int hash, bool hasOverride) {
    if (hasOverride && TryReadZdoValue(zdo, field.FieldType, hash, out object value)) {
      return FormatFieldValue(value);
    }

    return FormatFieldValue(field.GetValue(component));
  }

  static bool TryReadZdoValue(ZDO zdo, Type fieldType, int hash, out object value) {
    value = null;

    if (fieldType == typeof(int)) {
      if (!zdo.GetInt(hash, out int intValue)) {
        return false;
      }

      value = intValue;
      return true;
    }

    if (fieldType == typeof(float)) {
      if (!zdo.GetFloat(hash, out float floatValue)) {
        return false;
      }

      value = floatValue;
      return true;
    }

    if (fieldType == typeof(bool)) {
      if (!zdo.GetBool(hash, out bool boolValue)) {
        return false;
      }

      value = boolValue;
      return true;
    }

    if (fieldType == typeof(Vector3)) {
      if (!zdo.GetVec3(hash, out Vector3 vec3Value)) {
        return false;
      }

      value = vec3Value;
      return true;
    }

    if (fieldType == typeof(string) || fieldType == typeof(GameObject) || fieldType == typeof(ItemDrop)) {
      if (!zdo.GetString(hash, out string stringValue)) {
        return false;
      }

      value = stringValue;
      return true;
    }

    return false;
  }

  static void AddIntRows(List<ZdoKeyRow> rows, List<KeyValuePair<int, int>> values) {
    for (int i = 0; i < values.Count; i++) {
      KeyValuePair<int, int> pair = values[i];
      bool isBool = ZdoCatalog.IsBool(pair.Key);
      string text = isBool ? FormatBool(pair.Value != 0) : FormatInt(pair.Value);

      rows.Add(new ZdoKeyRow(ZdoKeyType.Int, pair.Key, ZdoCatalog.GetName(pair.Key), text, isBool));
    }
  }

  static void AddRows<T>(
      List<ZdoKeyRow> rows,
      ZdoKeyType type,
      List<KeyValuePair<int, T>> values,
      Func<T, string> format) {
    for (int i = 0; i < values.Count; i++) {
      KeyValuePair<int, T> pair = values[i];
      string name = ZdoCatalog.GetName(pair.Key);
      rows.Add(new ZdoKeyRow(type, pair.Key, name, format(pair.Value), ZdoCatalog.IsBool(pair.Key)));
    }
  }

  static int CompareKeyRows(ZdoKeyRow a, ZdoKeyRow b) {
    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
  }

  static int CompareFieldRows(ZdoFieldRow a, ZdoFieldRow b) {
    return string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase);
  }

  static string FormatFloat(float value) {
    return value.ToString("R", CultureInfo.InvariantCulture);
  }

  static string FormatBool(bool value) {
    return value ? "true" : "false";
  }

  static string FormatInt(int value) {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  static string FormatLong(long value) {
    return value.ToString(CultureInfo.InvariantCulture);
  }

  static string FormatVector3(Vector3 value) {
    return string.Join(", ", FormatFloat(value.x), FormatFloat(value.y), FormatFloat(value.z));
  }

  static string FormatQuaternion(Quaternion value) {
    return string.Join(
        ", ", FormatFloat(value.x), FormatFloat(value.y), FormatFloat(value.z), FormatFloat(value.w));
  }

  static string FormatBytes(byte[] value) {
    return value == null ? string.Empty : Convert.ToBase64String(value);
  }

  static string FormatKeyValue(ZdoKeyRow row, object value) {
    switch (row.Type) {
      case ZdoKeyType.Float:
        return FormatFloat((float) value);
      case ZdoKeyType.Vector3:
        return FormatVector3((Vector3) value);
      case ZdoKeyType.Quaternion:
        return FormatQuaternion((Quaternion) value);
      case ZdoKeyType.Int:
        return row.IsBool ? ((int) value != 0 ? "true" : "false") : FormatInt((int) value);
      case ZdoKeyType.Long:
        return FormatLong((long) value);
      case ZdoKeyType.String:
        return Escape((string) value);
      case ZdoKeyType.Bytes:
        return FormatBytes((byte[]) value);
      default:
        return string.Empty;
    }
  }

  static string FormatFieldValue(object value) {
    if (value is int intValue) {
      return FormatInt(intValue);
    }

    if (value is float floatValue) {
      return FormatFloat(floatValue);
    }

    if (value is bool boolValue) {
      return boolValue ? "true" : "false";
    }

    if (value is Vector3 vec3Value) {
      return FormatVector3(vec3Value);
    }

    if (value is string stringValue) {
      return Escape(stringValue);
    }

    if (value is GameObject gameObject) {
      return gameObject ? Utils.GetPrefabName(gameObject) : string.Empty;
    }

    if (value is ItemDrop itemDrop) {
      return itemDrop ? Utils.GetPrefabName(itemDrop.gameObject) : string.Empty;
    }

    return value?.ToString() ?? string.Empty;
  }

  static string Escape(string value) {
    if (string.IsNullOrEmpty(value)) {
      return string.Empty;
    }

    return value
        .Replace("\\", "\\\\")
        .Replace("\n", "\\n")
        .Replace("\r", "\\r")
        .Replace("\t", "\\t");
  }

  static string Unescape(string value) {
    if (string.IsNullOrEmpty(value)) {
      return string.Empty;
    }

    StringBuilder builder = new(value.Length);

    for (int i = 0; i < value.Length; i++) {
      char character = value[i];

      if (character != '\\' || i + 1 >= value.Length) {
        builder.Append(character);
        continue;
      }

      i++;
      char next = value[i];

      switch (next) {
        case 'n':
          builder.Append('\n');
          break;
        case 'r':
          builder.Append('\r');
          break;
        case 't':
          builder.Append('\t');
          break;
        case '\\':
          builder.Append('\\');
          break;
        default:
          builder.Append('\\').Append(next);
          break;
      }
    }

    return builder.ToString();
  }

  sealed class ZdoData {
    public List<KeyValuePair<int, float>> Floats;
    public List<KeyValuePair<int, Vector3>> Vec3s;
    public List<KeyValuePair<int, Quaternion>> Quaternions;
    public List<KeyValuePair<int, int>> Ints;
    public List<KeyValuePair<int, long>> Longs;
    public List<KeyValuePair<int, string>> Strings;
    public List<KeyValuePair<int, byte[]>> ByteArrays;
    public ZDOConnection Connection;

    public static ZdoData Read(ZDO zdo) {
      ZdoData data = new();

      ZDOExtraData.GetData(
          zdo.m_uid,
          out data.Floats,
          out data.Vec3s,
          out data.Quaternions,
          out data.Ints,
          out data.Longs,
          out data.Strings,
          out data.ByteArrays,
          out data.Connection);

      return data;
    }

    public HashSet<int> GetHashes() {
      HashSet<int> hashes = new();
      AddHashes(hashes, Floats);
      AddHashes(hashes, Vec3s);
      AddHashes(hashes, Quaternions);
      AddHashes(hashes, Ints);
      AddHashes(hashes, Longs);
      AddHashes(hashes, Strings);
      AddHashes(hashes, ByteArrays);
      return hashes;
    }

    static void AddHashes<T>(HashSet<int> hashes, List<KeyValuePair<int, T>> values) {
      for (int i = 0; i < values.Count; i++) {
        hashes.Add(values[i].Key);
      }
    }
  }
}
