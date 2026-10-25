namespace Tractor;

using System;
using System.Reflection;

using UnityEngine;

public enum ZdoKeyType {
  Float,
  Vector3,
  Quaternion,
  Int,
  Long,
  String,
  Bytes,
  Connection,
}

public sealed class ZdoKeyRow {
  public ZdoKeyType Type { get; }
  public int Hash { get; }
  public string Name { get; }
  public bool IsBool { get; }
  public string ValueText { get; set; }

  // Known to the prefab's components but not stored on this ZDO yet.
  public bool IsMissing { get; set; }

  public bool CanEdit => Type != ZdoKeyType.Connection;
  public bool CanRemove => Type != ZdoKeyType.Connection && !IsMissing;

  public string TypeText => Type switch {
    ZdoKeyType.Int => IsBool ? "bool" : "int",
    ZdoKeyType.Float => "float",
    ZdoKeyType.Vector3 => "vec3",
    ZdoKeyType.Quaternion => "quat",
    ZdoKeyType.Long => "long",
    ZdoKeyType.String => "string",
    ZdoKeyType.Bytes => "bytes",
    _ => "connection",
  };

  // The catalog does not know this hash, so the name is rendered as '<hash>'.
  public bool IsUnresolved => Name.Length > 2 && Name[0] == '<' && Name[Name.Length - 1] == '>';

  public ZdoKeyRow(ZdoKeyType type, int hash, string name, string valueText, bool isBool)
      : this(type, hash, name, valueText, isBool, isMissing: false) {}

  public ZdoKeyRow(
      ZdoKeyType type, int hash, string name, string valueText, bool isBool, bool isMissing) {
    Type = type;
    Hash = hash;
    Name = name;
    ValueText = valueText;
    IsBool = isBool;
    IsMissing = isMissing;
  }
}

public sealed class ZdoKnownKey {
  public string Name { get; }
  public int Hash { get; }
  public ZdoKeyType Type { get; }
  public bool IsBool { get; }

  public ZdoKnownKey(string name, int hash, ZdoKeyType type, bool isBool) {
    Name = name;
    Hash = hash;
    Type = type;
    IsBool = isBool;
  }
}

public sealed class ZdoFieldRow {
  public MonoBehaviour Component { get; }
  public FieldInfo Field { get; }
  public Type ComponentType { get; }
  public string Name { get; }
  public int Hash { get; }
  public bool HasOverride { get; set; }
  public string ValueText { get; set; }

  public string TypeText {
    get {
      Type fieldType = Field.FieldType;

      if (fieldType == typeof(int)) {
        return "int";
      }

      if (fieldType == typeof(float)) {
        return "float";
      }

      if (fieldType == typeof(bool)) {
        return "bool";
      }

      if (fieldType == typeof(string)) {
        return "string";
      }

      if (fieldType == typeof(Vector3)) {
        return "vec3";
      }

      return fieldType.Name;
    }
  }

  public ZdoFieldRow(MonoBehaviour component, FieldInfo field, string valueText, bool hasOverride) {
    Component = component;
    Field = field;
    ComponentType = component.GetType();
    Name = ComponentType.Name + "." + field.Name;
    Hash = Name.GetStableHashCode();
    ValueText = valueText;
    HasOverride = hasOverride;
  }

  // LoadFields() assigns the prefab that a GameObject or ItemDrop field names, so storing a part of
  // this object would point every copy of the prefab at one shared object. A real prefab is fine.
  public static bool IsPartOfSelf(MonoBehaviour component, object value) {
    GameObject gameObject = null;

    if (value is GameObject gameObjectValue) {
      gameObject = gameObjectValue;
    } else if (value is ItemDrop itemDrop && itemDrop) {
      gameObject = itemDrop.gameObject;
    }

    if (!gameObject || !component) {
      return false;
    }

    Transform rootTransform = component.transform;
    Transform valueTransform = gameObject.transform;
    return valueTransform == rootTransform || valueTransform.IsChildOf(rootTransform);
  }
}

public sealed class ZdoTarget {
  public ZDOID Id { get; }

  public ZdoTarget(ZDOID id) {
    Id = id;
  }

  public ZDO Zdo {
    get {
      ZDOMan zdoMan = ZDOMan.instance;
      return zdoMan == null ? null : zdoMan.GetZDO(Id);
    }
  }

  public ZNetView NetView {
    get {
      ZDO zdo = Zdo;

      if (zdo == null || !ZNetScene.instance) {
        return null;
      }

      return ZNetScene.instance.FindInstance(zdo);
    }
  }
}
