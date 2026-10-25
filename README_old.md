# Tractor

Client-side mod. Hover an object, press `Shift+RightClick` (or `F8`), and Tractor opens a panel that lists every ZDO
key and every loadable component field on that object. Select a row, edit the value, and apply it. With nothing
hovered, the same shortcut picks the networked object in front of you.

The panel works on the hovered object's own ZDO, not on the prefab template. Every write uses vanilla ZDO
replication and `ZNetView.LoadFields()`, so the mod sends no custom RPCs and the world stays compatible with vanilla
clients.

## Usage

  * `Shift+RightClick` or `F8` opens and closes the panel. `Escape` also closes it, and a click anywhere outside the
    panel closes it like `Shift+RightClick`.
  * If nothing is hovered, `Shift+RightClick` can look for a networked object in front of the player instead. The
    search starts with a sphere that touches the player and reaches `lookRadius` meters along the look direction,
    then doubles that distance twice before it gives up. Effects and markers that have no collider win over solid
    objects, and the status line says what it picked. The search is off by default; set `lookRadius` above `0` to
    turn it on.
  * The panel title is the prefab name. The list below shows keys and fields together. The filter box searches both
    kinds.
  * Rows are ordered by group, then by name: pinned keys, then pinned fields, then stored field overrides,
    then keys, then keys whose hash the catalog cannot name (`<hash>`), then the remaining fields. A stored
    override never appears twice; the field row owns the ZDO entry it stores.
  * Every row shows the name, the current value, and the value type (`int`, `float`, `bool`, `string`, `vec3`,
    `quat`, `bytes`, `long`, or the component field's type). Key rows start with a key icon in the amber of an
    override, the way a field starts with its component name.
  * Key rows cover every stored key: float, Vector3, quaternion, int, long, string, byte array, and the ZDO
    connection. Keys that the object's components know about but that are not set on this object yet show
    `(not set)` and are written when you apply a value. A key that Tractor has already seen on the prefab stays
    listed as `(not set)` after its value is removed, even when the catalog does not tie it to a component.
  * Field rows cover every `Type.field` that `ZNetView.LoadFields()` can load. A stored override is bold and amber.
  * The star on the left of a row pins it. Pinned keys sort above pinned fields at the top of the list, in both
    the unfiltered list and a search result. Pins are saved to
    `BepInEx/config/miker.valheim.tractor.favorites.txt`.
  * Selecting a row selects the text in the editor, so typing replaces the old value.
  * The mouse wheel scrolls three rows per notch, the scrollbar drags, and neither zooms the camera.
  * The selected row is named on the left of the editor and the last action is reported on the right.
  * `Apply` writes the value in the editor. With `autoApply` on, the value is also written when the editor loses
    focus, including `Enter` and closing the panel.
  * `Reset` removes the selected key or field override. Connection rows and rows that are not set are read-only.
  * `Radius:` takes the bulk radius in meters (default `32`).
  * The scope strip is one line: `Radius:` field, `All prefabs in:` `Radius` `Sector`, then
    `All components in:` `Radius` `Sector`. The group labels size to their text, so the spare width shows up as
    space between the three groups.
  * `All prefabs in:` `Radius` writes the selected key or field to every object with the same prefab
    within the radius; `Sector` does the same for the hovered object's zone.
  * `All components in:` `Radius` writes the selected field to every object that has the owning component type
    within the radius; `Sector` does the same for the hovered object's zone.
  * The component scopes only apply to fields, so those two buttons stay disabled while a key is selected.
    Keys always match the same prefab. The hovered object is always included.

Value formats:

  * Numbers use invariant culture. Vector3 is `x, y, z`; quaternion is `x, y, z, w`.
  * Strings escape `\`, newline, carriage return, and tab as `\\`, `\n`, `\r`, and `\t`.
  * Byte arrays are Base64.
  * An `int` key also takes a prefab name. Tractor stores the string's stable hash, which is what keys such as
    `ShoulderItem` expect. The editor then shows the stored number.
  * `GameObject` and `ItemDrop` fields take a prefab name.
  * A `GameObject` or `ItemDrop` field that points at a part of the object itself (`WearNTear.m_new`, for example)
    is still editable, so you can point it at a real prefab such as `fx_crit`. Tractor refuses a value that is a
    part of the object, because `LoadFields()` would replace it with one shared object and break the component.

## Config

  * `isModEnabled`: globally enable or disable the mod.
  * `openKey`: secondary open and close key (default `F8`).
  * `position`: panel position, saved when you drag the title bar. The default is `-620, 0`, which puts the panel
    on the left half of the screen; a saved `0, 0` counts as unset.
  * `bulkRadius`: radius in meters for the radius bulk action (default `32`). The `Radius:` field in the panel
    writes this value.
  * `lookRadius`: radius in meters of the sphere used when `Shift+RightClick` has no hover target (default `0`, which
    turns the search off). The sphere touches the player and reaches along the look direction, and the search grows
    to twice and four times this radius before giving up.
  * `autoApply`: apply the editor value when it loses focus (default `true`).
  * `uiTheme`: `Vanilla` skins the panel with the game's lit wood panel, wood buttons, text field, and scrollbar
    sprites; `Dark` uses flat colors. A change applies the next time the panel opens.
  * `uiFont`: `AveriaSans` (default) uses the game's sans face, the one ComfySpawner builds its panels from;
    `Game` uses the HUD hover-name face. A change applies the next time the panel opens.

## Permissions

The mod always claims ownership of the target ZDO before writing, which is what a vanilla client does when it edits
its own objects. Run it on a client that the server trusts, such as an admin or a solo host. The server can still
refuse ownership, in which case the edit does not replicate.

## Notes

  * The panel lists fields from the live instance. Objects outside the loaded area have no instance, so only their
    keys are available.
  * A key write also calls the matching vanilla setter on the live components when one exists (`level` calls
    `Character.SetLevel`, for example). That keeps cached fields in sync. Keys without a matching setter are stored
    on the ZDO and take effect when the game reads them or reloads the object.
  * `Reset` removes the selected key or field override on this client. Vanilla ZDO sync has no delete message, so a
    dedicated server still has the old key. Run Tractor on the host to keep a removal in the world save.
  * Other clients load field overrides when they create the object. A client that already has the object live does
    not run `LoadFields()` again, so the value can stay stale until that object reloads.

See `USAGE.md` for a player-facing walkthrough of the panel.
