using System.Collections.Generic;

namespace Il2CppDumper;

public class ScriptJson
{
    public ulong[] Addresses;
    public List<ScriptMetadata> ScriptMetadata = new();
    public List<ScriptMetadataMethod> ScriptMetadataMethod = new();
    public List<ScriptMethod> ScriptMethod = new();
    public List<ScriptString> ScriptString = new();
}

public class ScriptMethod
{
    public ulong Address;
    public string Name;
    public string Signature;
    public string TypeSignature;
}

public class ScriptString
{
    public ulong Address;
    public string Value;
}

public class ScriptMetadata
{
    public ulong Address;
    public string Name;
    public string Signature;
}

public class ScriptMetadataMethod
{
    public ulong Address;
    public ulong MethodAddress;
    public string Name;
}
