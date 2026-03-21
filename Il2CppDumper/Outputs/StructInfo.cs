using System;
using System.Collections.Generic;

namespace Il2CppDumper;

public class StructInfo
{
    public List<StructFieldInfo> Fields = new();
    public bool IsValueType;
    public string Parent;
    public List<StructRGCTXInfo> RGCTXs = new();
    public List<StructFieldInfo> StaticFields = new();
    public string TypeName;
    public StructVTableMethodInfo[] VTableMethod = Array.Empty<StructVTableMethodInfo>();
}

public class StructFieldInfo
{
    public string FieldName;
    public string FieldTypeName;
    public bool IsCustomType;
    public bool IsValueType;
}

public class StructVTableMethodInfo
{
    public string MethodName;
}

public class StructRGCTXInfo
{
    public string ClassName;
    public string MethodName;
    public Il2CppRGCTXDataType Type;
    public string TypeName;
}
