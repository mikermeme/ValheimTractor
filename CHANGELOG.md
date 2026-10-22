## Changelog

### 1.8.2

  * Fixed the selected-key line and the status line going blank with `uiFont = AveriaSans`. Those two labels sit
    in the shortest row, and TextMeshPro drops a line that is taller than its box when overflow is set to ellipsis,
    so that row lost its text. Row heights now follow the font.
  * AveriaSans is a little smaller than the game face at the same point size, so panel text is scaled up by 10%
    while it is selected.
  * `AveriaSans` is now the default for `uiFont`. A config that already stores `uiFont = Game` keeps it; delete the
    line to pick up the new default.

### 1.8.1

  * Fixed a `FieldAccessException` (then a `NullReferenceException` on every later open) when the panel was
    built with `uiFont` on either value. Reading `UnifiedPopup.instance` and `UnifiedPopup.bodyText` directly
    compiles against the publicized assembly but fails Mono's JIT access check in the real one, so the sans-font
    lookup now uses reflection.
  * `ZNetScene.m_instances` for the look search is read through reflection for the same reason.
  * A panel build that fails part way no longer leaves a half-built panel behind: the partial panel is dropped,
    the exception is logged as `Could not build the panel: ...`, and the next press starts over.
  * The sans font is only applied when the asset carries a material.

### 1.8.0

  * Pinned rows keep two groups: pinned keys first, then pinned fields, each sorted by name.
  * `lookRadius` accepts `0`, and `0` disables the look-for-a-target search.
  * New `uiFont` setting: `Game` keeps the HUD hover-name face, `AveriaSans` uses the popup body font that
    ComfySpawner also uses. Changing it rebuilds the panel on the next open, and the resolved face is logged once.

### 1.7.0

  * `Shift+RightClick` with no hover target now looks for a networked object in front of the player. The search
    sphere touches the player and reaches `lookRadius` meters along the look direction, then grows to 2x and 4x.
    Objects with no collider are preferred, and among those the ones with a renderer, so untouchable effects and
    markers win over zone controllers and solid objects. The status line reports the pick and its distance.
  * The key icon now sits inline at the start of the key name instead of in its own column, so it reads like the
    component name that prefixes a field. The name column grew by the freed width.
  * The scope strip labels (`All prefabs in:`, `All components in:`) now size to their text, and the spare width
    goes to wider gaps between the radius field, the prefab pair, and the component pair.

### 1.6.0

  * Keys are marked with a key icon in the amber of an overridden field name. It falls back through the emoji
    candidates the font can render, then to a plain dot. Field rows leave the icon column empty, so names stay
    aligned.
  * The scope controls are one line: `Radius:` field, `All prefabs in:` `Radius` `Sector`, and
    `All components in:` `Radius` `Sector`, with small buttons that match `Reload`. The component pair is disabled
    while a key is selected.
  * `Radius:` is 40px wide and the `m` suffix is gone.

### 1.5.0

  * The action status now shares the line above the value editor with the selected key or field, right-aligned.
  * `Apply` and `Reset` sit next to the value editor, like `Reload` next to the filter.
  * The radius slider is a `Radius:` number field ahead of the scope buttons.
  * Replaced `Match: prefab` / `Match: component` and the `Bulk` / `Zone` buttons with explicit
    `Radius (prefab)`, `Sector (prefab)`, `Radius (component)`, and `Sector (component)` buttons. The component
    pair only shows for fields.
  * Resetting a key keeps the list scroll position.
  * Field names are no longer prefixed with `*` and no longer indented against key names. An override is bold
    instead.
  * The panel opens at `-620, 0` by default, so it starts on the left half of the screen.

### 1.4.0

  * The name column is about 20% wider and the value column is narrower; the panel is 600 wide.
  * `Apply`, `Reset`, `Match`, `Bulk`, and `Zone` fit on one row.
  * Added a `Radius` slider for the bulk radius, wired to the `bulkRadius` config entry.
  * A key that was seen on the prefab stays listed as `(not set)` after its value is reset, even when `keys.tsv`
    does not tie it to a component (for example `cheated`).
  * `GameObject` and `ItemDrop` fields that point at a part of the object are editable. Pointing them at a real
    prefab such as `fx_crit` works, so an object can be hidden by setting `WearNTear.m_broken`.

### 1.3.0

  * The panel title is now the prefab name instead of "Tractor", and the panel is 60% as wide. The editor buttons
    wrap onto two rows.
  * Removed the `Keys` and `Fields` toggles. Both kinds are always listed and the filter always searches both.
  * New row order: favorites first (keys and fields in one group), then stored field overrides (`*`), then keys,
    then `<hash>` keys, then the remaining fields. A stored field override no longer appears a second time as a
    key row.
  * Removed the `+` prefix and the greyed name from keys that are not set. The value column still shows
    `(not set)`.
  * Skinned to match the current ComfySpawner: lit wood panel, wood buttons with sprite states, inset list,
    vanilla scrollbar, and a `★`/`☆` favorite toggle in the same amber.
  * Selecting a row selects the editor text, so typing replaces the old value.
  * An `int` key accepts a prefab name and stores its stable hash, which is what keys such as `ShoulderItem`
    expect.

### 1.2.0

  * Keys and fields are now listed together, keys first. `Keys` and `Fields` are toggles, at least one stays on, and
    the filter always searches both.
  * Every row shows the value type in a third column.
  * Added favorites: the star on the left of a row pins it, pinned rows sort first in the list and in search
    results, and the pins are saved to `BepInEx/config/miker.valheim.tractor.favorites.txt`.
  * Keys the catalog cannot name (`<hash>`) are listed at the bottom of the keys.
  * `Shift+RightClick` no longer opens and immediately closes the panel when one click is reported on two frames.
  * Fixed row highlighting for real: selecting a row highlighted every row because the selection test matched rows
    without a binding.
  * Bulk actions now have a `Match: prefab` / `Match: component` choice, so a field can be copied to same-prefab
    objects or to every object with the owning component, in a radius or the current zone.
  * Vanilla skin: darker brown panel, lighter brown buttons, and a light scrollbar handle.
  * `GameObject` and `ItemDrop` fields that point at a part of the object itself are read-only, because
    `LoadFields()` would replace them with a shared prefab. This stops `WearNTear.m_new`-style fields from breaking
    every piece that shares the prefab.

### 1.1.0

  * Click anywhere outside the panel to close it, like `Shift+RightClick`.
  * The `Keys` list now includes keys that the object's components know about but that are not set. They show a `+`
    prefix and `(not set)` and are written when you apply a value.
  * Applying a key now calls the matching vanilla setter on the live components (`level` calls `Character.SetLevel`)
    so cached fields update without a reload.
  * Fixed row highlighting: only the selected row and the hovered row are highlighted.
  * Added a vertical scrollbar, and the mouse wheel now scrolls three rows per notch.
  * Bulk button labels say whether they match the same prefab or every object with the same component, and bulk
    status messages name the match.
  * Added the `uiTheme` config: `Vanilla` skins the panel with the game's sprites, `Dark` keeps flat colors.

### 1.0.0

  * `Tractor` initial release: inspect and edit ZDO keys and loadable component fields on the hovered object.
  * Bulk-apply a key to same-prefab objects, or a field to every object with the owning component, in a radius or zone.
  * Gameplay input is suppressed while the panel is open, including camera zoom from the mouse wheel.
