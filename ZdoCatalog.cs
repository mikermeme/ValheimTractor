namespace Tractor;

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text;

public static class ZdoCatalog {
  const string NameResourceName = "Tractor.catalog.tsv";
  const string KeyResourceName = "Tractor.keys.tsv";

  static Dictionary<int, string> _names;
  static HashSet<int> _boolHashes;
  static Dictionary<string, string[]> _classBases;
  static Dictionary<string, List<ZdoKnownKey>> _classKeys;

  public static string GetName(int hash) {
    EnsureLoaded();

    if (_names.TryGetValue(hash, out string name)) {
      return name;
    }

    return $"<{hash.ToString(CultureInfo.InvariantCulture)}>";
  }

  public static bool IsBool(int hash) {
    EnsureLoaded();
    return _boolHashes.Contains(hash);
  }

  public static void Register(string name) {
    EnsureLoaded();

    int hash = name.GetStableHashCode();

    if (_names.ContainsKey(hash)) {
      return;
    }

    _names.Add(hash, name);

    if (name.StartsWith("HasFields", StringComparison.Ordinal)) {
      _boolHashes.Add(hash);
    }
  }

  // Collects every literal ZDO key a component class (and its base classes) reads or writes.
  public static void CollectKnownKeys(string className, Dictionary<int, ZdoKnownKey> keys) {
    EnsureLoaded();
    CollectKnownKeys(className, keys, new HashSet<string>(StringComparer.Ordinal));
  }

  static void CollectKnownKeys(
      string className, Dictionary<int, ZdoKnownKey> keys, HashSet<string> visited) {
    if (!visited.Add(className)) {
      return;
    }

    if (_classKeys.TryGetValue(className, out List<ZdoKnownKey> classKeys)) {
      for (int i = 0; i < classKeys.Count; i++) {
        ZdoKnownKey key = classKeys[i];

        if (!keys.ContainsKey(key.Hash)) {
          keys.Add(key.Hash, key);
        }
      }
    }

    if (!_classBases.TryGetValue(className, out string[] bases)) {
      return;
    }

    for (int i = 0; i < bases.Length; i++) {
      CollectKnownKeys(bases[i], keys, visited);
    }
  }

  static void EnsureLoaded() {
    if (_names != null) {
      return;
    }

    _names = new Dictionary<int, string>(20595);
    _boolHashes = new HashSet<int>();
    _classBases = new Dictionary<string, string[]>(StringComparer.Ordinal);
    _classKeys = new Dictionary<string, List<ZdoKnownKey>>(StringComparer.Ordinal);

    LoadNames();
    LoadKeys();
  }

  static void LoadNames() {
    using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(NameResourceName);

    if (stream == null) {
      Tractor.LogError($"Missing embedded resource: {NameResourceName}");
      return;
    }

    using StreamReader reader = new(stream, Encoding.UTF8);

    string line;

    while ((line = reader.ReadLine()) != null) {
      if (line.Length < 3) {
        continue;
      }

      if (line[0] == 'B') {
        if (int.TryParse(
                line.Substring(1), NumberStyles.Integer, CultureInfo.InvariantCulture, out int boolHash)) {
          _boolHashes.Add(boolHash);
        }

        continue;
      }

      if (line[0] != 'K') {
        continue;
      }

      int tabIndex = line.IndexOf('\t');

      if (tabIndex < 2) {
        continue;
      }

      if (!int.TryParse(
              line.Substring(1, tabIndex - 1),
              NumberStyles.Integer,
              CultureInfo.InvariantCulture,
              out int hash)) {
        continue;
      }

      string name = line.Substring(tabIndex + 1);
      _names[hash] = name;

      if (name.StartsWith("HasFields", StringComparison.Ordinal)) {
        _boolHashes.Add(hash);
      }
    }
  }

  static void LoadKeys() {
    using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(KeyResourceName);

    if (stream == null) {
      Tractor.LogError($"Missing embedded resource: {KeyResourceName}");
      return;
    }

    using StreamReader reader = new(stream, Encoding.UTF8);

    string line;

    while ((line = reader.ReadLine()) != null) {
      string[] parts = line.Split('\t');

      if (parts.Length < 2) {
        continue;
      }

      string className = parts[1];

      if (parts[0] == "C") {
        string[] bases =
            parts.Length >= 3 && parts[2].Length > 0
                ? parts[2].Split([' '], StringSplitOptions.RemoveEmptyEntries)
                : [];

        _classBases[className] = bases;
        continue;
      }

      if (parts[0] != "K" || parts.Length < 5) {
        continue;
      }

      if (!int.TryParse(parts[3], NumberStyles.Integer, CultureInfo.InvariantCulture, out int hash)) {
        continue;
      }

      if (!TryParseKeyType(parts[4], out ZdoKeyType type, out bool isBool)) {
        continue;
      }

      if (!_classKeys.TryGetValue(className, out List<ZdoKnownKey> keys)) {
        keys = [];
        _classKeys.Add(className, keys);
      }

      keys.Add(new ZdoKnownKey(parts[2], hash, type, isBool));
    }
  }

  static bool TryParseKeyType(string text, out ZdoKeyType type, out bool isBool) {
    isBool = false;

    switch (text) {
      case "int":
        type = ZdoKeyType.Int;
        return true;
      case "bool":
        type = ZdoKeyType.Int;
        isBool = true;
        return true;
      case "float":
        type = ZdoKeyType.Float;
        return true;
      case "long":
        type = ZdoKeyType.Long;
        return true;
      case "string":
        type = ZdoKeyType.String;
        return true;
      case "vec3":
        type = ZdoKeyType.Vector3;
        return true;
      case "quat":
        type = ZdoKeyType.Quaternion;
        return true;
      case "bytes":
        type = ZdoKeyType.Bytes;
        return true;
      default:
        type = ZdoKeyType.String;
        return false;
    }
  }
}
