using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;

namespace Il2CppDumper;

internal class Program
{
    private static Config config = null!;
    private static Action<string>? log;

    [STAThread]
    private static void Main(string[] args)
    {
        if (args.Length >= 3)
        {
            Environment.ExitCode = RunDumpFromArgs(args);
            return;
        }

        if (args.Length == 1)
        {
            if (args[0] == "-h" || args[0] == "--help" || args[0] == "/?" || args[0] == "/h")
            {
                EnsureConfigLoaded();
                ShowHelp();
                return;
            }
        }

        Application.EnableVisualStyles();
        Application.SetCompatibleTextRenderingDefault(false);
        Application.Run(new MainForm());
    }

    internal static int RunDump(string il2cppPath, string metadataPath, string outputDir, Action<string>? logger = null)
    {
        log = logger;
        Metadata? metadata = null;

        try
        {
            EnsureConfigLoaded();

            if (!File.Exists(il2cppPath))
            {
                WriteLine("ERROR: The specified executable/binary file does not exist.");
                return 1;
            }

            if (!File.Exists(metadataPath))
            {
                WriteLine("ERROR: The specified metadata file does not exist.");
                return 1;
            }

            if (string.IsNullOrWhiteSpace(outputDir))
            {
                WriteLine("ERROR: The specified output folder is empty.");
                return 1;
            }

            Directory.CreateDirectory(outputDir);
            outputDir = Path.GetFullPath(outputDir) + Path.DirectorySeparatorChar;

            if (Init(il2cppPath, metadataPath, out metadata, out var il2Cpp))
            {
                Dump(metadata, il2Cpp, outputDir);
                return 0;
            }

            return 1;
        }
        catch (Exception e)
        {
            WriteLine(e.ToString());
            return 1;
        }
        finally
        {
            metadata?.Dispose();
            log = null;
        }
    }

    private static int RunDumpFromArgs(string[] args)
    {
        EnsureConfigLoaded();

        string? il2cppPath = null;
        string? metadataPath = null;
        string? outputDir = null;

        foreach (var arg in args)
        {
            if (File.Exists(arg))
            {
                uint magicBytes;
                using (var fileStream = File.OpenRead(arg))
                using (var reader = new BinaryReader(fileStream))
                {
                    magicBytes = reader.ReadUInt32();
                }

                if (magicBytes == 0xFAB11BAF)
                {
                    metadataPath = arg;
                }
                else
                {
                    il2cppPath = arg;
                }
            }
            else if (Directory.Exists(arg))
            {
                outputDir = Path.GetFullPath(arg);
            }
        }

        if (string.IsNullOrWhiteSpace(il2cppPath) || string.IsNullOrWhiteSpace(metadataPath))
        {
            WriteLine("ERROR: Missing required input files.");
            ShowHelp();
            return 1;
        }

        if (string.IsNullOrWhiteSpace(outputDir))
        {
            WriteLine("ERROR: The specified output folder does not exist.");
            ShowHelp();
            return 1;
        }

        return RunDump(il2cppPath, metadataPath, outputDir);
    }

    private static void EnsureConfigLoaded()
    {
        if (config != null)
        {
            return;
        }

        var configPath = ResolveConfigPath();
        config = JsonSerializer.Deserialize<Config>(File.ReadAllText(configPath))
                 ?? throw new InvalidOperationException("config.json could not be loaded.");

        GenerateReplaceNameMap();
    }

    private static string ResolveConfigPath()
    {
        var envPath = Environment.GetEnvironmentVariable("IL2CPPDUMPER_CONFIG_PATH");
        if (!string.IsNullOrWhiteSpace(envPath) && File.Exists(envPath))
        {
            return envPath;
        }

        var localPath = Path.Combine(AppContext.BaseDirectory, "config.json");
        if (File.Exists(localPath))
        {
            return localPath;
        }

        var parentPath = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "config.json"));
        if (File.Exists(parentPath))
        {
            return parentPath;
        }

        throw new FileNotFoundException("config.json not found.");
    }

    private static void WriteLine(string message)
    {
        Console.WriteLine(message);
        log?.Invoke(message);
    }

    private static void GenerateReplaceNameMap()
    {
        if (config.ReplaceHashNames != null && config.ReplaceHashNames.Count > 0)
        {
            config.ReplaceHashNameMap = new System.Collections.Generic.Dictionary<string, string>();
            for (var i = 0; i < config.ReplaceHashNames.Count; i++)
            {
                config.ReplaceHashNameMap.Add(
                    config.ReplaceHashNames[i].TargetName,
                    config.ReplaceHashNames[i].ReplaceToName);
            }
        }
    }

    public static string? TryGetReplaceName(string szTargetName)
    {
        string? szRet = null;
        if (config.ReplaceHashNameMap != null)
        {
            config.ReplaceHashNameMap.TryGetValue(szTargetName, out szRet);
        }

        return szRet;
    }

    private static void ShowHelp()
    {
        WriteLine($"usage: {AppDomain.CurrentDomain.FriendlyName} <il2cpp-file> <global-metadata.dat> <output-folder>");
        WriteLine("If launched without arguments, a GUI window will open.");
    }

    private static bool Init(string il2cppPath, string metadataPath, out Metadata metadata, out Il2Cpp il2Cpp)
    {
        WriteLine("Initializing metadata...");
        var metadataStream = File.OpenRead(metadataPath);
        metadata = new Metadata(metadataStream);
        WriteLine($"Metadata Version: {metadata.Version}");

        WriteLine("Initializing il2cpp file...");
        var il2cppBytes = File.ReadAllBytes(il2cppPath);
        var il2cppMagic = BitConverter.ToUInt32(il2cppBytes, 0);
        var il2CppMemory = new MemoryStream(il2cppBytes);

        switch (il2cppMagic)
        {
            default:
                throw new NotSupportedException("ERROR: il2cpp file not supported.");
            case 0x6D736100:
                var web = new WebAssembly(il2CppMemory);
                il2Cpp = web.CreateMemory();
                break;
            case 0x304F534E:
                var nso = new NSO(il2CppMemory);
                il2Cpp = nso.UnCompress();
                break;
            case 0x905A4D: //PE
                il2Cpp = new PE(il2CppMemory);
                break;
            case 0x464C457F: //ELF
                if (il2cppBytes[4] == 2) //ELF64
                {
                    il2Cpp = new Elf64(il2CppMemory);
                }
                else
                {
                    il2Cpp = new Elf(il2CppMemory);
                }
                break;
            case 0xCAFEBABE: //FAT Mach-O
            case 0xBEBAFECA:
                var machofat = new MachoFat(new MemoryStream(il2cppBytes));
                var index = 0;
                for (var i = 0; i < machofat.fats.Length; i++)
                {
                    if (machofat.fats[i].magic == 0xFEEDFACF)
                    {
                        index = i;
                        break;
                    }
                }

                WriteLine($"Auto-selected: {(machofat.fats[index].magic == 0xFEEDFACF ? "64bit" : "32bit")}");
                var magic = machofat.fats[index].magic;
                il2cppBytes = machofat.GetMacho(index);
                il2CppMemory = new MemoryStream(il2cppBytes);
                if (magic == 0xFEEDFACF)
                {
                    goto case 0xFEEDFACF;
                }

                goto case 0xFEEDFACE;
            case 0xFEEDFACF: // 64bit Mach-O
                il2Cpp = new Macho64(il2CppMemory);
                break;
            case 0xFEEDFACE: // 32bit Mach-O
                il2Cpp = new Macho(il2CppMemory);
                break;
        }

        var version = config.ForceIl2CppVersion ? config.ForceVersion : metadata.Version;
        il2Cpp.SetProperties(version, metadata.metadataUsagesCount);
        WriteLine($"Il2Cpp Version: {il2Cpp.Version}");

        if (config.ForceDump || il2Cpp.CheckDump())
        {
            if (il2Cpp is ElfBase elf)
            {
                WriteLine("Detected this may be a dump file.");
                WriteLine("Auto-continuing with address 0.");
                const ulong dumpAddr = 0;
                if (dumpAddr != 0)
                {
                    il2Cpp.ImageBase = dumpAddr;
                    il2Cpp.IsDumped = true;
                    if (!config.NoRedirectedPointer)
                    {
                        elf.Reload();
                    }
                }
            }
            else
            {
                il2Cpp.IsDumped = true;
            }
        }

        WriteLine("Searching...");
        try
        {
            var flag = il2Cpp.PlusSearch(
                metadata.methodDefs.Count(x => x.methodIndex >= 0),
                metadata.typeDefs.Length,
                metadata.imageDefs.Length);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!flag && il2Cpp is PE)
                {
                    WriteLine("Use custom PE loader");
                    il2Cpp = PELoader.Load(il2cppPath);
                    il2Cpp.SetProperties(version, metadata.metadataUsagesCount);
                    flag = il2Cpp.PlusSearch(
                        metadata.methodDefs.Count(x => x.methodIndex >= 0),
                        metadata.typeDefs.Length,
                        metadata.imageDefs.Length);
                }
            }

            if (!flag)
            {
                flag = il2Cpp.Search();
            }

            if (!flag)
            {
                flag = il2Cpp.SymbolSearch();
            }

            if (!flag)
            {
                WriteLine("ERROR: Can't use auto mode to process file.");
                WriteLine("Manual mode not available in headless mode.");
                return false;
            }

            if (il2Cpp.Version >= 27 && il2Cpp.IsDumped)
            {
                var typeDef = metadata.typeDefs[0];
                var il2CppType = il2Cpp.types[typeDef.byvalTypeIndex];
                var typeDefinitionsOffset = metadata.Version >= 38
                    ? metadata.header.typeDefinitions.offset
                    : metadata.header.typeDefinitionsOffset;
                metadata.ImageBase = il2CppType.data.typeHandle - (ulong)typeDefinitionsOffset;
            }
        }
        catch (Exception e)
        {
            WriteLine(e.ToString());
            WriteLine("ERROR: An error occurred while processing.");
            return false;
        }

        return true;
    }

    private static void Dump(Metadata metadata, Il2Cpp il2Cpp, string outputDir)
    {
        WriteLine("Dumping...");
        var executor = new Il2CppExecutor(metadata, il2Cpp);
        var decompiler = new Il2CppDecompiler(executor);
        decompiler.Decompile(config, outputDir);
        WriteLine("Done!");

        if (config.GenerateStruct)
        {
            WriteLine("Generate struct...");
            var scriptGenerator = new StructGenerator(executor);
            scriptGenerator.WriteScript(outputDir, config.EscapeJsonValues);
            WriteLine("Done!");
        }

        if (config.GenerateDummyDll)
        {
            WriteLine("Generate dummy dll...");
            DummyAssemblyExporter.Export(executor, outputDir, config.DummyDllAddToken);
            WriteLine("Done!");
        }
    }
}
