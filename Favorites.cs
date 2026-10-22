namespace Tractor;

using System;
using System.Collections.Generic;
using System.IO;

using BepInEx;

public static class Favorites {
  const string FileName = "miker.valheim.tractor.favorites.txt";

  static readonly HashSet<string> _keys = new(StringComparer.Ordinal);
  static readonly HashSet<string> _fields = new(StringComparer.Ordinal);

  public static void Load() {
    _keys.Clear();
    _fields.Clear();

    string path = GetFilePath();

    if (!File.Exists(path)) {
      return;
    }

    try {
      string[] lines = File.ReadAllLines(path);

      for (int i = 0; i < lines.Length; i++) {
        string line = lines[i].Trim();

        if (line.Length == 0 || line[0] == '#') {
          continue;
        }

        int separator = line.IndexOf('\t');

        if (separator < 0) {
          continue;
        }

        string kind = line.Substring(0, separator).Trim();
        string name = line.Substring(separator + 1).Trim();

        if (name.Length == 0) {
          continue;
        }

        if (kind == "key") {
          _keys.Add(name);
        } else if (kind == "field") {
          _fields.Add(name);
        }
      }
    } catch (Exception exception) {
      Tractor.LogWarning($"Could not read favorites: {exception.Message}");
    }
  }

  public static bool IsKey(string name) {
    return _keys.Contains(name);
  }

  public static bool IsField(string name) {
    return _fields.Contains(name);
  }

  public static bool ToggleKey(string name) {
    return Toggle(_keys, name);
  }

  public static bool ToggleField(string name) {
    return Toggle(_fields, name);
  }

  static bool Toggle(HashSet<string> names, string name) {
    bool isFavorite = names.Add(name);

    if (!isFavorite) {
      names.Remove(name);
    }

    Save();
    return isFavorite;
  }

  static void Save() {
    List<string> lines = new(_keys.Count + _fields.Count);

    foreach (string name in _keys) {
      lines.Add("key\t" + name);
    }

    foreach (string name in _fields) {
      lines.Add("field\t" + name);
    }

    lines.Sort(StringComparer.OrdinalIgnoreCase);

    try {
      File.WriteAllLines(GetFilePath(), lines);
    } catch (Exception exception) {
      Tractor.LogWarning($"Could not write favorites: {exception.Message}");
    }
  }

  static string GetFilePath() {
    return Path.Combine(Paths.ConfigPath, FileName);
  }
}
