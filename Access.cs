namespace Tractor;

using System.Collections.Generic;

using HarmonyLib;

public static class Access {
  static readonly AccessTools.FieldRef<ZDOMan, Dictionary<ZDOID, ZDO>> _zdosById =
      AccessTools.FieldRefAccess<ZDOMan, Dictionary<ZDOID, ZDO>>("m_objectsByID");

  public static Dictionary<ZDOID, ZDO> ZdosById(ZDOMan zdoMan) {
    return _zdosById(zdoMan);
  }
}
