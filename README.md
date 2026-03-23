# Il2CppDumper GUI Bundle

A bundled Il2CppDumper distribution with a root launcher, shared configuration, shared helper scripts, and separate inner 32-bit and 64-bit runtimes.

## Highlights

- Root launcher with GUI and CLI modes
- Shared root `config.json`
- Shared root `scripts/` folder
- Separate inner runtimes in `bin32bit/` and `bin64bit/`
- Automatic runtime selection for APK and XAPK inputs
- Manual runtime override: `Auto`, `32-bit`, or `64-bit`
- Automatic extraction of `libil2cpp.so` and `global-metadata.dat` from supported package inputs

## Resulting release layout

```text
Il2CppDumper.exe
config.json
scripts/
bin32bit/
bin64bit/
```

## Supported inputs

The launcher supports:

- `APK`
- `XAPK`
- `libil2cpp.so`
- `GameAssembly.dll`
- other supported il2cpp executable or binary files

## GUI usage

Run:

```bash
Il2CppDumper.exe
```

Then choose:

- input file
- optional `global-metadata.dat`
- output folder
- runtime mode

`global-metadata.dat` is optional for APK and XAPK inputs when auto-resolve is used.

## CLI usage

### Standard manual mode

```bash
Il2CppDumper.exe <binary> <global-metadata.dat> <output-folder>
```

### Package mode

```bash
Il2CppDumper.exe <apk-or-xapk> <output-folder>
```

### Force 32-bit

```bash
Il2CppDumper.exe <apk-or-xapk> <output-folder> --arch=32
```

### Force 64-bit

```bash
Il2CppDumper.exe <apk-or-xapk> <output-folder> --arch=64
```

## Automatic package resolution

### APK

The launcher can automatically:

- locate a supported `libil2cpp.so`
- select the correct inner runtime
- use a manually selected `global-metadata.dat`, or keep manual metadata mode

### XAPK

The launcher can automatically:

- open the outer XAPK package
- locate embedded APK files
- inspect each APK for `libil2cpp.so`
- locate `global-metadata.dat`
- extract only the required files to a temporary directory
- launch the correct inner dumper automatically

## ABI selection rules

In `Auto` mode:

- `arm64-v8a` and `x86_64` prefer the 64-bit runtime
- `armeabi-v7a` and `x86` prefer the 32-bit runtime
- if both 32-bit and 64-bit binaries exist, the launcher prefers 64-bit

## Helper scripts

Helper scripts are stored once in the shared root `scripts/` folder.

Typical files include:

- `ida.py`
- `ida_py3.py`
- `ida_with_struct.py`
- `ida_with_struct_py3.py`
- `ghidra.py`
- `ghidra_wasm.py`
- `ghidra_with_struct.py`
- `il2cpp_header_to_ghidra.py`

## Included fixes

This bundle also includes code fixes for the inner dumper:

- fixed `VersionAttribute` filtering in `BinaryStream`
- fixed metadata stream lifetime during dump
- fixed launcher CLI argument parsing

## Notes

Auto package resolution is a convenience feature. If a package uses a non-standard layout or protected metadata, manual selection may still be required.

## Credits

- Jumboperson - [Il2CppDumper](https://github.com/Jumboperson/Il2CppDumper)