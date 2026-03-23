using System.Diagnostics;
using System.IO.Compression;
using System.Text;
using System.Windows.Forms;

namespace Il2CppDumperLauncher;

internal enum LaunchArchMode
{
    Auto,
    Force32,
    Force64,
}

internal enum RuntimeBitness
{
    Bit32,
    Bit64,
}

internal static class Program
{
    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length == 0)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return;
        }

        if (args.Length == 1 && IsHelpSwitch(args[0]))
        {
            ShowHelp();
            return;
        }

        Environment.ExitCode = RunFromArgs(args);
    }

    internal static bool IsPackagePath(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".apk", StringComparison.OrdinalIgnoreCase)
               || extension.Equals(".xapk", StringComparison.OrdinalIgnoreCase);
    }

    internal static async Task<int> LaunchAsync(
        string inputPath,
        string? metadataPath,
        string outputPath,
        LaunchArchMode mode,
        Action<string>? log = null)
    {
        if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
        {
            log?.Invoke("ERROR: Specify a valid binary, APK, or XAPK path.");
            return 1;
        }

        if (string.IsNullOrWhiteSpace(outputPath))
        {
            log?.Invoke("ERROR: Specify an output folder.");
            return 1;
        }

        if (!IsPackagePath(inputPath) && (string.IsNullOrWhiteSpace(metadataPath) || !File.Exists(metadataPath)))
        {
            log?.Invoke("ERROR: Specify a valid global-metadata.dat path.");
            return 1;
        }

        if (!string.IsNullOrWhiteSpace(metadataPath) && !File.Exists(metadataPath))
        {
            log?.Invoke("ERROR: The selected global-metadata.dat file does not exist.");
            return 1;
        }

        Directory.CreateDirectory(outputPath);

        PreparedInput? prepared = null;
        try
        {
            prepared = PrepareInput(inputPath, metadataPath, mode, log);
            var childDir = prepared.TargetBitness == RuntimeBitness.Bit64 ? "bin64bit" : "bin32bit";
            var childExe = Path.Combine(AppContext.BaseDirectory, childDir, "Il2CppDumper.exe");

            if (!File.Exists(childExe))
            {
                log?.Invoke($"ERROR: Inner dumper was not found: {childExe}");
                return 1;
            }

            var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
            if (!File.Exists(configPath))
            {
                log?.Invoke($"ERROR: config.json was not found next to the launcher: {configPath}");
                return 1;
            }

            log?.Invoke($"Selected runtime: {(prepared.TargetBitness == RuntimeBitness.Bit64 ? "64-bit" : "32-bit")}");
            log?.Invoke($"Input binary: {prepared.BinaryPath}");
            log?.Invoke($"Metadata: {prepared.MetadataPath}");
            log?.Invoke($"Output: {outputPath}");

            var startInfo = new ProcessStartInfo
            {
                FileName = childExe,
                WorkingDirectory = Path.GetDirectoryName(childExe)!,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };

            startInfo.Environment["IL2CPPDUMPER_CONFIG_PATH"] = configPath;
            startInfo.ArgumentList.Add(prepared.BinaryPath);
            startInfo.ArgumentList.Add(prepared.MetadataPath);
            startInfo.ArgumentList.Add(outputPath);

            using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
            process.OutputDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    log?.Invoke(e.Data);
                }
            };
            process.ErrorDataReceived += (_, e) =>
            {
                if (!string.IsNullOrWhiteSpace(e.Data))
                {
                    log?.Invoke(e.Data);
                }
            };

            if (!process.Start())
            {
                log?.Invoke("ERROR: Failed to start the inner Il2CppDumper process.");
                return 1;
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            log?.Invoke(ex.ToString());
            return 1;
        }
        finally
        {
            prepared?.Dispose();
        }
    }

    private static int RunFromArgs(string[] args)
    {
        try
        {
            var positional = ParseArchMode(args, out var mode);
            if (positional.Count == 2 && IsPackagePath(positional[0]))
            {
                return LaunchAsync(positional[0], null, positional[1], mode, Console.WriteLine)
                    .GetAwaiter()
                    .GetResult();
            }

            if (positional.Count < 3)
            {
                ShowHelp();
                return 1;
            }

            return LaunchAsync(positional[0], positional[1], positional[2], mode, Console.WriteLine)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private static List<string> ParseArchMode(string[] args, out LaunchArchMode mode)
    {
        mode = LaunchArchMode.Auto;
        var positional = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.Equals("--32", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("/32", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-32", StringComparison.OrdinalIgnoreCase))
            {
                mode = LaunchArchMode.Force32;
                continue;
            }

            if (arg.Equals("--64", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("/64", StringComparison.OrdinalIgnoreCase) ||
                arg.Equals("-64", StringComparison.OrdinalIgnoreCase))
            {
                mode = LaunchArchMode.Force64;
                continue;
            }

            if (arg.Equals("--auto", StringComparison.OrdinalIgnoreCase))
            {
                mode = LaunchArchMode.Auto;
                continue;
            }

            if (arg.StartsWith("--arch=", StringComparison.OrdinalIgnoreCase))
            {
                mode = ParseArchValue(arg.Substring("--arch=".Length));
                continue;
            }

            if ((arg.Equals("--arch", StringComparison.OrdinalIgnoreCase) || arg.Equals("-a", StringComparison.OrdinalIgnoreCase)) && i + 1 < args.Length)
            {
                i++;
                mode = ParseArchValue(args[i]);
                continue;
            }

            positional.Add(arg);
        }

        return positional;
    }

    private static LaunchArchMode ParseArchValue(string value)
    {
        return value.Trim().ToLowerInvariant() switch
        {
            "32" or "x86" or "32bit" => LaunchArchMode.Force32,
            "64" or "x64" or "64bit" => LaunchArchMode.Force64,
            _ => LaunchArchMode.Auto,
        };
    }

    private static bool IsHelpSwitch(string arg)
    {
        return arg is "-h" or "--help" or "/?" or "/h";
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Il2CppDumper.exe <binary> <global-metadata.dat> <output-folder> [--arch=auto|32|64]");
        Console.WriteLine("  Il2CppDumper.exe <apk-or-xapk> <output-folder> [--arch=auto|32|64]");
        Console.WriteLine();
        Console.WriteLine("If launched without arguments, a GUI window will open.");
        Console.WriteLine("Auto mode for APK/XAPK prefers 64-bit when both 32-bit and 64-bit binaries are available.");
    }

    private static PreparedInput PrepareInput(string inputPath, string? metadataPath, LaunchArchMode mode, Action<string>? log)
    {
        if (IsPackagePath(inputPath))
        {
            return ResolveFromPackage(inputPath, metadataPath, mode, log);
        }

        var bitness = mode switch
        {
            LaunchArchMode.Force32 => RuntimeBitness.Bit32,
            LaunchArchMode.Force64 => RuntimeBitness.Bit64,
            _ => DetectBinaryBitness(inputPath),
        };

        log?.Invoke($"Detected input type: {DescribeInputType(inputPath)}");
        return new PreparedInput(inputPath, metadataPath!, bitness, null);
    }

    private static PreparedInput ResolveFromPackage(string packagePath, string? metadataPath, LaunchArchMode mode, Action<string>? log)
    {
        var tempRoot = Path.Combine(Path.GetTempPath(), "Il2CppDumperLauncher", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempRoot);

        try
        {
            var apkSources = GetApkSources(packagePath, tempRoot, log);
            if (apkSources.Count == 0)
            {
                throw new InvalidOperationException("No APK files were found inside the selected package.");
            }

            var libCandidates = new List<PackageLibCandidate>();
            PackageMetadataCandidate? packageMetadata = null;

            foreach (var apkSource in apkSources)
            {
                using var apkStream = File.OpenRead(apkSource.ApkPath);
                using var apkZip = new ZipArchive(apkStream, ZipArchiveMode.Read);

                packageMetadata ??= FindMetadataCandidate(apkZip, apkSource.DisplayName, apkSource.ApkPath);
                libCandidates.AddRange(FindLibCandidates(apkZip, apkSource.DisplayName, apkSource.ApkPath));
            }

            if (libCandidates.Count == 0)
            {
                throw new InvalidOperationException("No libil2cpp.so for a supported ABI was found in the selected package.");
            }

            var selectedLib = SelectLibCandidate(libCandidates, mode);
            var extractedBinary = Path.Combine(tempRoot, "libil2cpp.so");
            ExtractNestedZipEntry(selectedLib.ApkPath, selectedLib.EntryPath, extractedBinary);
            log?.Invoke($"Selected ABI: {selectedLib.AbiName}");
            log?.Invoke($"Extracted libil2cpp.so from {selectedLib.SourceName}");

            string resolvedMetadataPath;
            if (!string.IsNullOrWhiteSpace(metadataPath))
            {
                resolvedMetadataPath = metadataPath;
                log?.Invoke("Using manually selected global-metadata.dat file.");
            }
            else
            {
                if (packageMetadata == null)
                {
                    throw new InvalidOperationException("global-metadata.dat was not found inside the selected package.");
                }

                resolvedMetadataPath = Path.Combine(tempRoot, "global-metadata.dat");
                ExtractNestedZipEntry(packageMetadata.ApkPath, packageMetadata.EntryPath, resolvedMetadataPath);
                log?.Invoke($"Extracted global-metadata.dat from {packageMetadata.SourceName}");
            }

            return new PreparedInput(extractedBinary, resolvedMetadataPath, selectedLib.Bitness, tempRoot);
        }
        catch
        {
            TryDeleteDirectory(tempRoot);
            throw;
        }
    }

    private static List<ApkSource> GetApkSources(string packagePath, string tempRoot, Action<string>? log)
    {
        if (Path.GetExtension(packagePath).Equals(".apk", StringComparison.OrdinalIgnoreCase))
        {
            log?.Invoke("Detected package type: APK");
            return new List<ApkSource> { new(Path.GetFileName(packagePath), packagePath) };
        }

        log?.Invoke("Detected package type: XAPK");

        var extractedApksDir = Path.Combine(tempRoot, "apks");
        Directory.CreateDirectory(extractedApksDir);

        var apkSources = new List<ApkSource>();
        using var xapkStream = File.OpenRead(packagePath);
        using var xapkZip = new ZipArchive(xapkStream, ZipArchiveMode.Read);

        var apkEntries = xapkZip.Entries
            .Where(entry => entry.FullName.EndsWith(".apk", StringComparison.OrdinalIgnoreCase))
            .OrderBy(entry => entry.FullName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        foreach (var entry in apkEntries)
        {
            var fileName = Path.GetFileName(entry.FullName);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var targetPath = Path.Combine(extractedApksDir, $"{apkSources.Count:D2}_{fileName}");
            entry.ExtractToFile(targetPath, true);
            apkSources.Add(new ApkSource(entry.FullName, targetPath));
        }

        log?.Invoke($"Found {apkSources.Count} embedded APK file(s).");
        return apkSources;
    }

    private static PackageMetadataCandidate? FindMetadataCandidate(ZipArchive apkZip, string sourceName, string apkPath)
    {
        var entry = apkZip.Entries.FirstOrDefault(x =>
            NormalizePath(x.FullName).Equals("assets/bin/Data/Managed/Metadata/global-metadata.dat", StringComparison.OrdinalIgnoreCase));

        entry ??= apkZip.Entries.FirstOrDefault(x =>
        {
            var normalized = NormalizePath(x.FullName);
            return normalized.Equals("global-metadata.dat", StringComparison.OrdinalIgnoreCase)
                   || normalized.EndsWith("/global-metadata.dat", StringComparison.OrdinalIgnoreCase);
        });

        return entry == null ? null : new PackageMetadataCandidate(sourceName, apkPath, entry.FullName);
    }

    private static IEnumerable<PackageLibCandidate> FindLibCandidates(ZipArchive apkZip, string sourceName, string apkPath)
    {
        foreach (var entry in apkZip.Entries)
        {
            if (TryMapApkEntry(entry.FullName, out var bitness, out var abiName))
            {
                yield return new PackageLibCandidate(sourceName, apkPath, entry.FullName, bitness, abiName);
            }
        }
    }

    private static PackageLibCandidate SelectLibCandidate(List<PackageLibCandidate> candidates, LaunchArchMode mode)
    {
        return mode switch
        {
            LaunchArchMode.Force32 => candidates.FirstOrDefault(x => x.Bitness == RuntimeBitness.Bit32)
                                      ?? throw new InvalidOperationException("The selected package does not contain a 32-bit libil2cpp.so."),
            LaunchArchMode.Force64 => candidates.FirstOrDefault(x => x.Bitness == RuntimeBitness.Bit64)
                                      ?? throw new InvalidOperationException("The selected package does not contain a 64-bit libil2cpp.so."),
            _ => candidates.FirstOrDefault(x => x.Bitness == RuntimeBitness.Bit64)
                 ?? candidates.First(x => x.Bitness == RuntimeBitness.Bit32),
        };
    }

    private static void ExtractNestedZipEntry(string apkPath, string entryPath, string outputPath)
    {
        using var apkStream = File.OpenRead(apkPath);
        using var apkZip = new ZipArchive(apkStream, ZipArchiveMode.Read);
        var entry = apkZip.GetEntry(entryPath)
            ?? throw new InvalidOperationException($"Failed to locate {entryPath} inside {Path.GetFileName(apkPath)}.");

        Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
        entry.ExtractToFile(outputPath, true);
    }

    private static string DescribeInputType(string inputPath)
    {
        if (IsPackagePath(inputPath))
        {
            return Path.GetExtension(inputPath).TrimStart('.').ToUpperInvariant();
        }

        using var fs = File.OpenRead(inputPath);
        using var br = new BinaryReader(fs);
        var magic = br.ReadUInt32();

        return magic switch
        {
            0x464C457F => "ELF",
            0x905A4D => "PE",
            0xCAFEBABE or 0xBEBAFECA or 0xFEEDFACE or 0xFEEDFACF => "Mach-O",
            _ => "binary",
        };
    }

    private static RuntimeBitness DetectBinaryBitness(string inputPath)
    {
        using var fs = File.OpenRead(inputPath);
        using var br = new BinaryReader(fs);
        var magic = br.ReadUInt32();

        return magic switch
        {
            0x464C457F => DetectElfBitness(fs, br),
            0x905A4D => DetectPeBitness(fs, br),
            0xCAFEBABE or 0xBEBAFECA => RuntimeBitness.Bit64,
            0xFEEDFACF => RuntimeBitness.Bit64,
            0xFEEDFACE => RuntimeBitness.Bit32,
            _ => throw new InvalidOperationException("Could not determine 32/64-bit automatically. Choose the mode manually."),
        };
    }

    private static RuntimeBitness DetectElfBitness(FileStream fs, BinaryReader br)
    {
        fs.Position = 4;
        var elfClass = br.ReadByte();
        return elfClass switch
        {
            1 => RuntimeBitness.Bit32,
            2 => RuntimeBitness.Bit64,
            _ => throw new InvalidOperationException("Unknown ELF class."),
        };
    }

    private static RuntimeBitness DetectPeBitness(FileStream fs, BinaryReader br)
    {
        fs.Position = 0x3C;
        var peOffset = br.ReadInt32();
        fs.Position = peOffset + 4;
        var machine = br.ReadUInt16();

        return machine switch
        {
            0x014c => RuntimeBitness.Bit32,
            0x8664 => RuntimeBitness.Bit64,
            _ => throw new InvalidOperationException($"Unknown PE machine: 0x{machine:X4}"),
        };
    }

    private static bool TryMapApkEntry(string fullName, out RuntimeBitness bitness, out string abiName)
    {
        var normalized = NormalizePath(fullName);

        if (normalized.Equals("lib/arm64-v8a/libil2cpp.so", StringComparison.OrdinalIgnoreCase))
        {
            bitness = RuntimeBitness.Bit64;
            abiName = "arm64-v8a";
            return true;
        }

        if (normalized.Equals("lib/armeabi-v7a/libil2cpp.so", StringComparison.OrdinalIgnoreCase))
        {
            bitness = RuntimeBitness.Bit32;
            abiName = "armeabi-v7a";
            return true;
        }

        if (normalized.Equals("lib/x86_64/libil2cpp.so", StringComparison.OrdinalIgnoreCase))
        {
            bitness = RuntimeBitness.Bit64;
            abiName = "x86_64";
            return true;
        }

        if (normalized.Equals("lib/x86/libil2cpp.so", StringComparison.OrdinalIgnoreCase))
        {
            bitness = RuntimeBitness.Bit32;
            abiName = "x86";
            return true;
        }

        bitness = default;
        abiName = string.Empty;
        return false;
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/');
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch
        {
        }
    }

    private sealed class ApkSource
    {
        public ApkSource(string displayName, string apkPath)
        {
            DisplayName = displayName;
            ApkPath = apkPath;
        }

        public string DisplayName { get; }
        public string ApkPath { get; }
    }

    private sealed class PackageMetadataCandidate
    {
        public PackageMetadataCandidate(string sourceName, string apkPath, string entryPath)
        {
            SourceName = sourceName;
            ApkPath = apkPath;
            EntryPath = entryPath;
        }

        public string SourceName { get; }
        public string ApkPath { get; }
        public string EntryPath { get; }
    }

    private sealed class PackageLibCandidate
    {
        public PackageLibCandidate(string sourceName, string apkPath, string entryPath, RuntimeBitness bitness, string abiName)
        {
            SourceName = sourceName;
            ApkPath = apkPath;
            EntryPath = entryPath;
            Bitness = bitness;
            AbiName = abiName;
        }

        public string SourceName { get; }
        public string ApkPath { get; }
        public string EntryPath { get; }
        public RuntimeBitness Bitness { get; }
        public string AbiName { get; }
    }

    private sealed class PreparedInput : IDisposable
    {
        public PreparedInput(string binaryPath, string metadataPath, RuntimeBitness targetBitness, string? tempDirectory)
        {
            BinaryPath = binaryPath;
            MetadataPath = metadataPath;
            TargetBitness = targetBitness;
            TempDirectory = tempDirectory;
        }

        public string BinaryPath { get; }
        public string MetadataPath { get; }
        public RuntimeBitness TargetBitness { get; }
        public string? TempDirectory { get; }

        public void Dispose()
        {
            if (string.IsNullOrWhiteSpace(TempDirectory) || !Directory.Exists(TempDirectory))
            {
                return;
            }

            TryDeleteDirectory(TempDirectory);
        }
    }
}
