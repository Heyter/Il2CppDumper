using System;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;

namespace Il2CppDumper;

internal class Program
{
    private static Config config;

    [STAThread]
    private static void Main(string[] args)
    {
        config = JsonSerializer.Deserialize<Config>(File.ReadAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json")));
        GenerateReplaceNameMap();
        string il2cppPath = null;
        string metadataPath = null;
        string outputDir = null;

        if (args.Length == 1)
        {
            if (args[0] == "-h" || args[0] == "--help" || args[0] == "/?" || args[0] == "/h")
            {
                ShowHelp();
                return;
            }
        }

        if (args.Length < 3)
        {
            Console.WriteLine("ERROR: Not enough arguments.");
            ShowHelp();
            return;
        }

        if (args.Length > 1)
        {
            foreach (var arg in args)
            {
                if (File.Exists(arg))
                {
                    UInt32 magicBytes = 0;
                    using (FileStream fileStream = File.OpenRead(arg))
                    {
                        magicBytes = new BinaryReader(fileStream).ReadUInt32();
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
                    outputDir = Path.GetFullPath(arg) + Path.DirectorySeparatorChar;
                }
            }
        }

        if (string.IsNullOrEmpty(outputDir) || !Directory.Exists(outputDir))
        {
            Console.WriteLine("ERROR: The specified output folder does not exist.");
            ShowHelp();
            return;
        }
        outputDir = Path.GetFullPath(outputDir) + Path.DirectorySeparatorChar;
        {
            if (il2cppPath == null || metadataPath == null)
            {
                Console.WriteLine("ERROR: Missing required input files.");
                ShowHelp();
                return;
            }
        }
        if (il2cppPath == null)
        {
            ShowHelp();
            return;
        }
        if (metadataPath == null)
        {
            Console.WriteLine($"ERROR: Metadata file not found or encrypted.");
        }
        else
        {
            try
            {
                if (Init(il2cppPath, metadataPath, out var metadata, out var il2Cpp))
                {
                    Dump(metadata, il2Cpp, outputDir);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }
    }

    static void GenerateReplaceNameMap()
    {
        if (config.ReplaceHashNames != null && config.ReplaceHashNames.Count > 0)
        {
            config.ReplaceHashNameMap = new System.Collections.Generic.Dictionary<string, string>();
            for (int i = 0; i < config.ReplaceHashNames.Count; i++)
            {
                config.ReplaceHashNameMap.Add(config.ReplaceHashNames[i].TargetName, config.ReplaceHashNames[i].ReplaceToName);
            }
        }
    }

    public static string TryGetReplaceName(string szTargetName)
    {
        string szRet = null;
        if(config.ReplaceHashNameMap != null)
        {
            config.ReplaceHashNameMap.TryGetValue(szTargetName, out szRet);
        }
        return szRet;
    }

    private static void ShowHelp()
    {
        Console.WriteLine(
            $"usage: {AppDomain.CurrentDomain.FriendlyName} <executable-file> <global-metadata> <output-directory>");
    }

    private static bool Init(string il2cppPath, string metadataPath, out Metadata metadata, out Il2Cpp il2Cpp)
    {
        Console.WriteLine("Initializing metadata...");
        var metadataStream = File.OpenRead(metadataPath);
        metadata = new Metadata(metadataStream);
        Console.WriteLine($"Metadata Version: {metadata.Version}");

        Console.WriteLine("Initializing il2cpp file...");
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
            case 0x464c457f: //ELF
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
                // Auto-select 64bit if available, otherwise first entry
                var index = 0;
                for (var i = 0; i < machofat.fats.Length; i++)
                {
                    if (machofat.fats[i].magic == 0xFEEDFACF)
                    {
                        index = i;
                        break;
                    }
                }
                Console.WriteLine($"Auto-selected: {(machofat.fats[index].magic == 0xFEEDFACF ? "64bit" : "32bit")}");
                var magic = machofat.fats[index].magic;
                il2cppBytes = machofat.GetMacho(index);
                il2CppMemory = new MemoryStream(il2cppBytes);
                if (magic == 0xFEEDFACF)
                    goto case 0xFEEDFACF;
                else
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
        Console.WriteLine($"Il2Cpp Version: {il2Cpp.Version}");
        if (config.ForceDump || il2Cpp.CheckDump())
        {
            if (il2Cpp is ElfBase elf)
            {
                Console.WriteLine("Detected this may be a dump file. Auto-continuing with address 0.");
                var DumpAddr = (ulong)0;
                if (DumpAddr != 0)
                {
                    il2Cpp.ImageBase = DumpAddr;
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

        Console.WriteLine("Searching...");
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
                    Console.WriteLine("Use custom PE loader");
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
                Console.WriteLine("ERROR: Can't use auto mode to process file. Manual mode not available in headless mode.");
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
            Console.WriteLine(e);
            Console.WriteLine("ERROR: An error occurred while processing.");
            return false;
        }

        return true;
    }

    private static void Dump(Metadata metadata, Il2Cpp il2Cpp, string outputDir)
    {
        Console.WriteLine("Dumping...");
        var executor = new Il2CppExecutor(metadata, il2Cpp);
        var decompiler = new Il2CppDecompiler(executor);
        decompiler.Decompile(config, outputDir);
        Console.WriteLine("Done!");
        if (config.GenerateStruct)
        {
            Console.WriteLine("Generate struct...");
            var scriptGenerator = new StructGenerator(executor);
            scriptGenerator.WriteScript(outputDir, config.EscapeJsonValues);
            Console.WriteLine("Done!");
        }

        if (config.GenerateDummyDll)
        {
            Console.WriteLine("Generate dummy dll...");
            DummyAssemblyExporter.Export(executor, outputDir, config.DummyDllAddToken);
            Console.WriteLine("Done!");
        }
    }
}
