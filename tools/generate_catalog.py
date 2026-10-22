"""Generate Tractor's embedded hash-name and class-key catalogs from ZdoCatalog's catalog.json."""

import json
import pathlib

ROOT = pathlib.Path(__file__).resolve().parent.parent
CATALOG_PATH = ROOT.parent / "ZdoCatalog" / "out" / "catalog.json"
NAMES_PATH = ROOT / "catalog.tsv"
KEYS_PATH = ROOT / "keys.tsv"


def load_catalog():
    with CATALOG_PATH.open(encoding="utf-8") as catalog_file:
        return json.load(catalog_file)


def collect_bool_hashes(catalog):
    bool_hashes = set()

    for class_entry in catalog["classes"].values():
        for zdo_entry in class_entry["z"]:
            hash_value = zdo_entry[1]
            types = zdo_entry[2]

            if hash_value is not None and "bool" in types:
                bool_hashes.add(int(hash_value))

    for hash_text, entries in catalog["hashes"].items():
        for _, sources in entries:
            if "field_flag" in sources.split(","):
                bool_hashes.add(int(hash_text))

    return bool_hashes


def write_names(catalog, bool_hashes):
    lines = []

    for hash_text in sorted((int(value) for value in catalog["hashes"]), reverse=True):
        name = catalog["hashes"][str(hash_text)][0][0]
        lines.append(f"K{hash_text}\t{name}")

    for hash_value in sorted(bool_hashes, reverse=True):
        lines.append(f"B{hash_value}")

    NAMES_PATH.write_text("\n".join(lines) + "\n", encoding="utf-8", newline="\n")


def write_keys(catalog):
    class_lines = []
    key_lines = []

    for class_name in sorted(catalog["classes"]):
        class_entry = catalog["classes"][class_name]
        bases = [str(base) for base in class_entry["b"] or []]
        class_lines.append(f"C\t{class_name}\t{' '.join(bases)}")

        seen = set()

        for zdo_entry in class_entry["z"]:
            key_name = str(zdo_entry[0])
            hash_value = zdo_entry[1]
            types = zdo_entry[2]

            if hash_value is None or key_name in seen:
                continue

            zdo_type = pick_type(types)

            if zdo_type is None:
                continue

            seen.add(key_name)
            key_lines.append(f"K\t{class_name}\t{key_name}\t{int(hash_value)}\t{zdo_type}")

    KEYS_PATH.write_text(
        "\n".join(class_lines + key_lines) + "\n", encoding="utf-8", newline="\n")
    return len(class_lines), len(key_lines)


def pick_type(types):
    for name in ("int", "float", "long", "bool", "vec3", "quat", "string", "bytes"):
        if name in types:
            return name

    return None


def main():
    catalog = load_catalog()
    bool_hashes = collect_bool_hashes(catalog)

    write_names(catalog, bool_hashes)
    class_count, key_count = write_keys(catalog)

    print(f"Wrote {len(catalog['hashes'])} names and {len(bool_hashes)} bool hashes.")
    print(f"Wrote {class_count} classes and {key_count} class keys.")
    print(f"Output: {NAMES_PATH}")
    print(f"Output: {KEYS_PATH}")


if __name__ == "__main__":
    main()
