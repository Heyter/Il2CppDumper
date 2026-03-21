namespace Il2CppDumper;

public enum SearchSectionType
{
    Exec,
    Data,
    Bss
}

public class SearchSection
{
    public ulong address;
    public ulong addressEnd;
    public ulong offset;
    public ulong offsetEnd;
}
