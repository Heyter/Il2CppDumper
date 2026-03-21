using JetBrains.Annotations;

namespace Il2CppDumper;

[NoReorder]
public class MachoSection
{
    public string sectname;
    public uint addr;
    public uint size;
    public uint offset;
    public uint flags;
}

[NoReorder]
public class MachoSection64Bit
{
    public string sectname;
    public ulong addr;
    public ulong size;
    public ulong offset;
    public uint flags;
}

[NoReorder]
public class Fat
{
    public uint offset;
    public uint size;
    public uint magic;
}
