# Tractor

*Inspect and edit every ZDO key and every loadable component field on the object you are looking at.*

Look at a build, a chest, a portal, a creature or a piece of scenery, press the shortcut, and Tractor opens a panel
that shows what that object actually stores. Change a value, apply it, and the object updates.

Tractor is client-side. It writes through vanilla ZDO replication and `ZNetView.LoadFields()`, which means it sends no
custom RPCs and the world stays valid for vanilla clients.

## Opening the Panel

    Shift + RightClick        open or close the panel on the hovered object
    F8                        same as Shift + RightClick
    Escape                    close the panel
    Click outside the panel   close the panel

  * The panel opens on the object under the crosshair, not on the prefab it was built from.
  * Nothing opens while a game menu is up, or while the map or chat has the cursor.
  * The title shows the prefab name of the object you opened.
  * `F8` can be rebound in the config if another mod already uses it.

## The List

Every row is three things: a name, the value it holds, and the type of that value.

    key name or Type.field          value                type

Which rows appear:

  * **Keys** -- everything the object's ZDO stores: floats, vectors, quaternions, ints, longs, strings, byte arrays
    and the ZDO connection. A key row starts with a small key icon.
  * **Fields** -- every `Type.field` that `ZNetView.LoadFields()` can load for this object, such as
    `WearNTear.m_health` or `Door.m_consumeKey`. The name always starts with its component type. Only the types the
    game restores show up: `int`, `float`, `bool`, `Vector3`, `string`, `GameObject` and `ItemDrop`.

What the styling means:

  * **Bold amber name** -- this object stores an override for that field, and the value shown is the override.
  * Normal field name -- no override, so the value comes from the prefab.
  * `(not set)` -- the object's components know this key, but the ZDO does not store it yet. Type a value to store it.
  * `<hash>` -- a stored key whose name is not in Tractor's catalog. The number next to it is the key hash.

Order of the list:

  1. Pinned keys
  2. Pinned fields
  3. Fields with an override
  4. Keys
  5. Unnamed `<hash>` keys
  6. Fields without an override

Each group is sorted by name. The filter box narrows the list without changing the order, so pinned rows stay on top.

## Finding a Row

    Filter keys and fields...        matches names of keys and fields as you type
    Reload                           re-reads the object
    Mouse wheel                      scroll three rows per notch
    Scrollbar                        drag the handle
    Star (left of a row)             pin or unpin

  * Pins survive restarts. They are saved to `BepInEx/config/miker.valheim.tractor.favorites.txt`.
  * While the panel is open the wheel scrolls the list and does not zoom the camera.

## Editing a Value

  * Click a row to select it. The selected row is named on the left of the editor, above the value field.
  * The value text is selected when you click the row, so typing replaces the old value.
  * `Apply` writes the value. `Enter` also writes it.
  * With `autoApply` on (default), leaving the value field writes the pending value too: clicking another row,
    pressing `Escape`, or closing the panel while the field still has focus.
  * `Reset` removes the key or the field override. Rows that are not set, and the connection row, cannot be reset.
  * The status line on the right of the editor reports the last action or an error.

Value formats:

    int, long         1234                (-1234 works too)
    float             0.85
    bool              true  false
    string            any text; \\ \n \r \t are escaped
    vec3              1.5, 0, -2
    quat              0, 0, 0, 1           (keys only)
    bytes             Base64              (keys only)
    GameObject        prefab name, such as fx_crit
    ItemDrop          prefab name, such as Wood

  * Numbers always use `.` as the decimal separator, whatever your game language.
  * An `int` key also accepts a prefab name and stores that name's stable hash. Keys such as `ShoulderItem` expect
    exactly that.
  * A `GameObject` or `ItemDrop` field that points at a part of the object itself is still editable, so you can
    replace it with a real prefab. Tractor refuses a value that is a part of the object, because `LoadFields()`
    would point the component at one shared object and break it.

## Copying a Value to Other Objects

The bottom row does bulk writes.

    Radius: [32]   All prefabs in: Radius Sector   All components in: Radius Sector

  * `Radius:` is the bulk radius in meters, clamped between `1` and `256`. It is the same value as the `bulkRadius`
    config, and it is saved.
  * `All prefabs in:` writes the selected key or field to every object in range that uses the same prefab.
  * `All components in:` writes the selected field to every object in range that has the same component type, even
    when the prefab differs.
  * `Radius` searches a sphere around the player. `Sector` covers the zone the object is in.
  * The two component buttons are disabled while a key is selected, because a key lives on a prefab rather than on a
    component. Keys always match the same prefab.
  * The object you opened is always included in a bulk write.
  * The status line reports how many objects were written.

## Looking at Nothing

By default the shortcut only works on something under the crosshair. If you set `lookRadius` above `0`, Tractor also
searches in front of you when nothing is hovered:

  * The first search sphere touches the player and reaches `lookRadius` meters along the look direction.
  * The search then doubles that distance, and doubles it again, before it gives up.
  * Effects and markers without a collider win over solid objects, so hovering a mist effect does not hide the
    spawner behind it.
  * The status line names what it picked and how far away it was.

A radius of `4` is a comfortable start.

## Config

The file is `BepInEx/config/miker.valheim.tractor.cfg`.

    isModEnabled      true             turn the whole mod on or off
    openKey           F8               second open/close key
    position          -620, 0          panel position; saved when you drag the title row
    bulkRadius        32               radius for the bulk buttons, also the Radius: field
    lookRadius        0                search radius when nothing is hovered; 0 is off
    autoApply         true             write the value when the editor loses focus
    uiTheme           Vanilla          Vanilla = game wood panel; Dark = flat colors
    uiFont            AveriaSans        AveriaSans = the game's sans face; Game = HUD hover-name face

  * `uiTheme` and `uiFont` apply the next time the panel opens.
  * A saved `position` of `0, 0` is treated as unset, so the panel returns to its default corner.

## Multiplayer

  * Tractor claims ownership of the object before it writes, which is what a vanilla client does when it edits its
    own objects. Run it on a client the server trusts, such as an admin or a solo host.
  * If the server refuses the ownership claim, the write does not replicate and the value can snap back the next
    time the object reloads.
  * Fields are loaded by `LoadFields()` when a client creates the object. A client that already has the object in the
    world does not re-run it, so a field change can look stale there until that object reloads.
  * Vanilla ZDO sync has no delete message. `Reset` removes a key for you, but a dedicated server still holds the old
    value in its own copy. Edit on the host to keep a removal in the world save.

## Notes

  * Objects outside the loaded area have no live instance, so only their keys are listed, not their fields.
  * A key write also calls the matching vanilla setter on the live component when one exists, for example `level`
    calls `Character.SetLevel`. That keeps the component's cached value in sync with the ZDO.
  * Keys without a setter are stored on the ZDO and take effect when the game reads them or reloads the object.
  * Pins are per client and are not shared with other players.

## Install

  * Install BepInEx for Valheim, then drop the `miker-Tractor` folder into `BepInEx/plugins`.
  * Client-side only. Nothing is required on the server, and other players do not need the mod.
  * Check `BepInEx/LogOutput.log` if the panel does not open; Tractor logs the reason when it fails to build the panel.
