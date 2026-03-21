using JetBrains.Annotations;

namespace Il2CppDumper;

[NoReorder]
public class DataSection
{
    public uint Index;
    public uint Offset;
    public byte[] Data;
}
