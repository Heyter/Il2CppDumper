using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Il2CppDumperLauncher;

internal enum LaunchArchMode
{
    Auto,
    Force32,
    Force64
}

internal enum TargetArch
{
    Unknown,
    Bit32,
    Bit64
}

internal static class Program
{
    [STAThread]
    private static int Main(string[] args)
    {
        if (args.Length >= 1)
        {
            return RunFromArgs(args);
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
        return 0;
    }

    private static int RunFromArgs(string[] args)
    {
        try
        {
            var positional = ParseArchMode(args, out var mode);

            if (positional.Count < 3)
            {
                ShowHelp();
                return 1;
            }

            return LaunchAsync(
                    positional[0],
                    positional[1],
                    positional[2],
                    mode,
                    Console.WriteLine)
                .GetAwaiter()
                .GetResult();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    internal static async Task<int> LaunchAsync(
        string inputPath,
        string metadataPath,
        string outputDir,
        LaunchArchMode mode,
        Action<string>? log = null)
    {
        string? extractedTempDir = null;

        try
        {
            if (string.IsNullOrWhiteSpace(inputPath) || !File.Exists(inputPath))
            {
                WriteLog(log, "Input file was not found.");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(metadataPath) || !File.Exists(metadataPath))
            {
                WriteLog(log, "global-metadata.dat was not found.");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(outputDir))
            {
                WriteLog(log, "Output directory is not specified.");
                return 1;
            }

            Directory.CreateDirectory(outputDir);

            var baseDir = AppContext.BaseDirectory;
            var childDir = ResolveChildDir(inputPath, mode, log);
            var childExe = Path.Combine(baseDir, childDir, "Il2CppDumper.exe");

            if (!File.Exists(childExe))
            {
                WriteLog(log, $"Inner dumper executable was not found: {childExe}");
                return 1;
            }

            var actualInputPath = inputPath;

            if (IsApk(inputPath))
            {
                extractedTempDir = Path.Combine(Path.GetTempPath(), "Il2CppDumperLauncher", Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(extractedTempDir);

                actualInputPath = ExtractLibIl2CppFromApk(inputPath, extractedTempDir, childDir, log);

                if (string.IsNullOrWhiteSpace(actualInputPath) || !File.Exists(actualInputPath))
                {
                    WriteLog(log, "Failed to extract libil2cpp.so from APK.");
                    return 1;
                }
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = childExe,
                WorkingDirectory = Path.GetDirectoryName(childExe)!,
                UseShellExecute = false,
            };

            startInfo.ArgumentList.Add(actualInputPath);
            startInfo.ArgumentList.Add(metadataPath);
            startInfo.ArgumentList.Add(outputDir);

            var configPath = ResolveConfigPath(baseDir);
            if (configPath != null)
            {
                startInfo.Environment["IL2CPPDUMPER_CONFIG_PATH"] = configPath;
            }

            WriteLog(log, $"Selected runtime: {childDir}");
            WriteLog(log, $"Inner executable: {childExe}");
            WriteLog(log, $"Input: {actualInputPath}");
            WriteLog(log, $"Metadata: {metadataPath}");
            WriteLog(log, $"Output: {outputDir}");

            using var process = Process.Start(startInfo);
            if (process == null)
            {
                WriteLog(log, "Failed to start inner dumper process.");
                return 1;
            }

            await process.WaitForExitAsync();
            return process.ExitCode;
        }
        catch (Exception ex)
        {
            WriteLog(log, ex.ToString());
            return 1;
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(extractedTempDir))
            {
                TryDeleteDirectory(extractedTempDir);
            }
        }
    }

    private static List<string> ParseArchMode(string[] args, out LaunchArchMode mode)
    {
        mode = LaunchArchMode.Auto;
        var positional = new List<string>();

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (string.Equals(arg, "--32", StringComparison.OrdinalIgnoreCase))
            {
                mode = LaunchArchMode.Force32;
                continue;
            }

            if (string.Equals(arg, "--64", StringComparison.OrdinalIgnoreCase))
            {
                mode = LaunchArchMode.Force64;
                continue;
            }

            if (string.Equals(arg, "--auto", StringComparison.OrdinalIgnoreCase))
            {
                mode = LaunchArchMode.Auto;
                continue;
            }

            if (string.Equals(arg, "--arch", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(arg, "-a", StringComparison.OrdinalIgnoreCase))
            {
                if (i + 1 >= args.Length)
                {
                    throw new ArgumentException("Missing value for --arch.");
                }

                i++;
                var value = args[i];

                mode = value.ToLowerInvariant() switch
                {
                    "auto" => LaunchArchMode.Auto,
                    "32" => LaunchArchMode.Force32,
                    "x86" => LaunchArchMode.Force32,
                    "32bit" => LaunchArchMode.Force32,
                    "64" => LaunchArchMode.Force64,
                    "x64" => LaunchArchMode.Force64,
                    "64bit" => LaunchArchMode.Force64,
                    _ => throw new ArgumentException($"Unsupported --arch value: {value}")
                };

                continue;
            }

            positional.Add(arg);
        }

        return positional;
    }

    private static void ShowHelp()
    {
        Console.WriteLine("Usage:");
        Console.WriteLine("  Il2CppDumper.exe [--auto|--32|--64|--arch auto|32|64] <input-file> <global-metadata.dat> <output-folder>");
        Console.WriteLine();
        Console.WriteLine("Input file can be:");
        Console.WriteLine("  - APK");
        Console.WriteLine("  - libil2cpp.so");
        Console.WriteLine("  - GameAssembly.dll");
        Console.WriteLine("  - another executable/binary file");
    }

    private static string ResolveChildDir(string inputPath, LaunchArchMode mode, Action<string>? log)
    {
        if (mode == LaunchArchMode.Force64)
        {
            WriteLog(log, "Architecture mode: forced 64-bit");
            return "bin64bit";
        }

        if (mode == LaunchArchMode.Force32)
        {
            WriteLog(log, "Architecture mode: forced 32-bit");
            return "bin32bit";
        }

        TargetArch arch;

        if (IsApk(inputPath))
        {
            arch = DetectFromApk(inputPath, log);
        }
        else if (LooksLikeElf(inputPath))
        {
            arch = DetectFromElf(inputPath);
        }
        else
        {
            arch = DetectFromPe(inputPath);
        }

        return arch switch
        {
            TargetArch.Bit64 => "bin64bit",
            TargetArch.Bit32 => "bin32bit",
            _ => throw new InvalidOperationException("Could not determine 32/64-bit automatically. Select the mode manually.")
        };
    }

    private static bool IsApk(string path)
    {
        return string.Equals(Path.GetExtension(path), ".apk", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeElf(string path)
    {
        return string.Equals(Path.GetExtension(path), ".so", StringComparison.OrdinalIgnoreCase)
               || string.Equals(Path.GetFileName(path), "libil2cpp.so", StringComparison.OrdinalIgnoreCase);
    }

    private static TargetArch DetectFromApk(string apkPath, Action<string>? log)
    {
        using var stream = File.OpenRead(apkPath);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        var hasArm64 = zip.Entries.Any(e =>
            e.FullName.Equals("lib/arm64-v8a/libil2cpp.so", StringComparison.OrdinalIgnoreCase));

        var hasArm32 = zip.Entries.Any(e =>
            e.FullName.Equals("lib/armeabi-v7a/libil2cpp.so", StringComparison.OrdinalIgnoreCase));

        var hasX64 = zip.Entries.Any(e =>
            e.FullName.Equals("lib/x86_64/libil2cpp.so", StringComparison.OrdinalIgnoreCase));

        var hasX86 = zip.Entries.Any(e =>
            e.FullName.Equals("lib/x86/libil2cpp.so", StringComparison.OrdinalIgnoreCase));

        if (hasArm64 || hasX64)
        {
            WriteLog(log, "APK ABI detected: 64-bit");
            return TargetArch.Bit64;
        }

        if (hasArm32 || hasX86)
        {
            WriteLog(log, "APK ABI detected: 32-bit");
            return TargetArch.Bit32;
        }

        return TargetArch.Unknown;
    }

    private static string ExtractLibIl2CppFromApk(
        string apkPath,
        string tempDir,
        string childDir,
        Action<string>? log)
    {
        using var stream = File.OpenRead(apkPath);
        using var zip = new ZipArchive(stream, ZipArchiveMode.Read);

        var candidates = childDir == "bin64bit"
            ? new[]
            {
                "lib/arm64-v8a/libil2cpp.so",
                "lib/x86_64/libil2cpp.so",
                "lib/armeabi-v7a/libil2cpp.so",
                "lib/x86/libil2cpp.so"
            }
            : new[]
            {
                "lib/armeabi-v7a/libil2cpp.so",
                "lib/x86/libil2cpp.so",
                "lib/arm64-v8a/libil2cpp.so",
                "lib/x86_64/libil2cpp.so"
            };

        var entry = candidates
            .Select(candidate => zip.GetEntry(candidate))
            .FirstOrDefault(e => e != null);

        if (entry == null)
        {
            throw new FileNotFoundException("libil2cpp.so was not found inside the APK.");
        }

        var outputPath = Path.Combine(tempDir, "libil2cpp.so");
        entry.ExtractToFile(outputPath, true);

        WriteLog(log, $"Extracted {entry.FullName} to temporary file.");
        return outputPath;
    }

    private static TargetArch DetectFromElf(string soPath)
    {
        using var fs = File.OpenRead(soPath);
        using var br = new BinaryReader(fs);

        var magic = br.ReadBytes(4);
        if (magic.Length != 4 ||
            magic[0] != 0x7F ||
            magic[1] != (byte)'E' ||
            magic[2] != (byte)'L' ||
            magic[3] != (byte)'F')
        {
            return TargetArch.Unknown;
        }

        var eiClass = br.ReadByte();
        return eiClass switch
        {
            1 => TargetArch.Bit32,
            2 => TargetArch.Bit64,
            _ => TargetArch.Unknown
        };
    }

    private static TargetArch DetectFromPe(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);

        if (fs.Length < 0x40)
        {
            return TargetArch.Unknown;
        }

        fs.Seek(0, SeekOrigin.Begin);
        if (br.ReadUInt16() != 0x5A4D)
        {
            return TargetArch.Unknown;
        }

        fs.Seek(0x3C, SeekOrigin.Begin);
        var peOffset = br.ReadInt32();

        if (peOffset <= 0 || peOffset > fs.Length - 6)
        {
            return TargetArch.Unknown;
        }

        fs.Seek(peOffset, SeekOrigin.Begin);
        if (br.ReadUInt32() != 0x00004550)
        {
            return TargetArch.Unknown;
        }

        var machine = br.ReadUInt16();

        return machine switch
        {
            0x014c => TargetArch.Bit32,
            0x8664 => TargetArch.Bit64,
            _ => TargetArch.Unknown
        };
    }

    private static string? ResolveConfigPath(string baseDir)
    {
        var direct = Path.Combine(baseDir, "config.json");
        if (File.Exists(direct))
        {
            return direct;
        }

        return null;
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

    private static void WriteLog(Action<string>? log, string message)
    {
        Console.WriteLine(message);
        log?.Invoke(message);
    }
}