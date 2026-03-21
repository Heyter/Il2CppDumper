using JetBrains.Annotations;
using AssemblyIndex = int;
using ImageIndex = int;
using StringIndex = uint;
using StringLiteralIndex = int;
using FieldIndex = int;
using MethodIndex = int;
using GenericParameterIndex = int;
using GenericParameterConstraintIndex = short;
using CustomAttributeIndex = int;
using EventIndex = int;
using PropertyIndex = int;
using NestedTypeIndex = int;
using InterfacesIndex = int;
using VTableIndex = int;
using DefaultValueDataIndex = int;

namespace Il2CppDumper;

[NoReorder]
public class Il2CppGlobalMetadataHeader
{
    public uint sanity;
    public int version;

    [Version(Max = 35)] public int stringLiteralOffset; // string data for managed code

    [Version(Max = 35)] public int stringLiteralSize;

    [Version(Max = 35)] public int stringLiteralDataOffset;

    [Version(Max = 35)] public int stringLiteralDataSize;

    [Version(Max = 35)] public int stringOffset; // string data for metadata

    [Version(Max = 35)] public int stringSize;

    [Version(Max = 35)] public int eventsOffset; // Il2CppEventDefinition

    [Version(Max = 35)] public int eventsSize;

    [Version(Max = 35)] public int propertiesOffset; // Il2CppPropertyDefinition

    [Version(Max = 35)] public int propertiesSize;

    [Version(Max = 35)] public int methodsOffset; // Il2CppMethodDefinition

    [Version(Max = 35)] public int methodsSize;

    [Version(Max = 35)] public int parameterDefaultValuesOffset; // Il2CppParameterDefaultValue

    [Version(Max = 35)] public int parameterDefaultValuesSize;

    [Version(Max = 35)] public int fieldDefaultValuesOffset; // Il2CppFieldDefaultValue

    [Version(Max = 35)] public int fieldDefaultValuesSize;

    [Version(Max = 35)] public int fieldAndParameterDefaultValueDataOffset; // int8_t

    [Version(Max = 35)] public int fieldAndParameterDefaultValueDataSize;

    [Version(Max = 35)] public int fieldMarshaledSizesOffset; // Il2CppFieldMarshaledSize

    [Version(Max = 35)] public int fieldMarshaledSizesSize;

    [Version(Max = 35)] public int parametersOffset; // Il2CppParameterDefinition

    [Version(Max = 35)] public int parametersSize;

    [Version(Max = 35)] public int fieldsOffset; // Il2CppFieldDefinition

    [Version(Max = 35)] public int fieldsSize;

    [Version(Max = 35)] public int genericParametersOffset; // Il2CppGenericParameter

    [Version(Max = 35)] public int genericParametersSize;

    [Version(Max = 35)] public int genericParameterConstraintsOffset; // TypeIndex

    [Version(Max = 35)] public int genericParameterConstraintsSize;

    [Version(Max = 35)] public int genericContainersOffset; // Il2CppGenericContainer

    [Version(Max = 35)] public int genericContainersSize;

    [Version(Max = 35)] public int nestedTypesOffset; // TypeDefinitionIndex

    [Version(Max = 35)] public int nestedTypesSize;

    [Version(Max = 35)] public int interfacesOffset; // TypeIndex

    [Version(Max = 35)] public int interfacesSize;

    [Version(Max = 35)] public int vtableMethodsOffset; // EncodedMethodIndex

    [Version(Max = 35)] public int vtableMethodsSize;

    [Version(Max = 35)] public int interfaceOffsetsOffset; // Il2CppInterfaceOffsetPair

    [Version(Max = 35)] public int interfaceOffsetsSize;

    [Version(Max = 35)] public int typeDefinitionsOffset; // Il2CppTypeDefinition

    [Version(Max = 35)] public int typeDefinitionsSize;

    [Version(Max = 24.1)] public int rgctxEntriesOffset; // Il2CppRGCTXDefinition

    [Version(Max = 24.1)] public int rgctxEntriesCount;

    [Version(Max = 35)] public int imagesOffset; // Il2CppImageDefinition

    [Version(Max = 35)] public int imagesSize;

    [Version(Max = 35)] public int assembliesOffset; // Il2CppAssemblyDefinition

    [Version(Max = 35)] public int assembliesSize;

    [Version(Min = 19, Max = 24.5)] public int metadataUsageListsOffset; // Il2CppMetadataUsageList

    [Version(Min = 19, Max = 24.5)] public int metadataUsageListsCount;

    [Version(Min = 19, Max = 24.5)] public int metadataUsagePairsOffset; // Il2CppMetadataUsagePair

    [Version(Min = 19, Max = 24.5)] public int metadataUsagePairsCount;

    [Version(Min = 19, Max = 35)] public int fieldRefsOffset; // Il2CppFieldRef

    [Version(Min = 19, Max = 35)] public int fieldRefsSize;

    [Version(Min = 20, Max = 35)] public int referencedAssembliesOffset; // int32_t

    [Version(Min = 20, Max = 35)] public int referencedAssembliesSize;

    [Version(Min = 21, Max = 27.2)] public int attributesInfoOffset; // Il2CppCustomAttributeTypeRange

    [Version(Min = 21, Max = 27.2)] public int attributesInfoCount;

    [Version(Min = 21, Max = 27.2)] public int attributeTypesOffset; // TypeIndex

    [Version(Min = 21, Max = 27.2)] public int attributeTypesCount;

    [Version(Min = 29, Max = 35)] public int attributeDataOffset;

    [Version(Min = 29, Max = 35)] public int attributeDataSize;

    [Version(Min = 29, Max = 35)] public int attributeDataRangeOffset;

    [Version(Min = 29, Max = 35)] public int attributeDataRangeSize;

    [Version(Min = 22, Max = 35)] public int unresolvedVirtualCallParameterTypesOffset; // TypeIndex

    [Version(Min = 22, Max = 35)] public int unresolvedVirtualCallParameterTypesSize;

    [Version(Min = 22, Max = 35)] public int unresolvedVirtualCallParameterRangesOffset; // Il2CppRange

    [Version(Min = 22, Max = 35)] public int unresolvedVirtualCallParameterRangesSize;

    [Version(Min = 23, Max = 35)] public int windowsRuntimeTypeNamesOffset; // Il2CppWindowsRuntimeTypeNamePair

    [Version(Min = 23, Max = 35)] public int windowsRuntimeTypeNamesSize;

    [Version(Min = 27, Max = 35)] public int windowsRuntimeStringsOffset; // const char*

    [Version(Min = 27, Max = 35)] public int windowsRuntimeStringsSize;

    [Version(Min = 24, Max = 35)] public int exportedTypeDefinitionsOffset; // TypeDefinitionIndex

    [Version(Min = 24, Max = 35)] public int exportedTypeDefinitionsSize;

    // - v38

    [Version(Min = 38)] public Il2CppSectionMetadata stringLiterals;

    [Version(Min = 38)] public Il2CppSectionMetadata stringLiteralData;

    [Version(Min = 38)] public Il2CppSectionMetadata strings;

    [Version(Min = 38)] public Il2CppSectionMetadata events;

    [Version(Min = 38)] public Il2CppSectionMetadata properties;

    [Version(Min = 38)] public Il2CppSectionMetadata methods;

    [Version(Min = 38)] public Il2CppSectionMetadata parameterDefaultValues;

    [Version(Min = 38)] public Il2CppSectionMetadata fieldDefaultValues;

    [Version(Min = 38)] public Il2CppSectionMetadata fieldAndParameterDefaultValueData;

    [Version(Min = 38)] public Il2CppSectionMetadata fieldMarshaledSizes;

    [Version(Min = 38)] public Il2CppSectionMetadata parameters;

    [Version(Min = 38)] public Il2CppSectionMetadata fields;

    [Version(Min = 38)] public Il2CppSectionMetadata genericParameters;

    [Version(Min = 38)] public Il2CppSectionMetadata genericParameterConstraints;

    [Version(Min = 38)] public Il2CppSectionMetadata genericContainers;

    [Version(Min = 38)] public Il2CppSectionMetadata nestedTypes;

    [Version(Min = 38)] public Il2CppSectionMetadata interfaces;

    [Version(Min = 38)] public Il2CppSectionMetadata vtableMethods;

    [Version(Min = 38)] public Il2CppSectionMetadata interfaceOffsets;

    [Version(Min = 38)] public Il2CppSectionMetadata typeDefinitions;

    [Version(Min = 38)] public Il2CppSectionMetadata images;

    [Version(Min = 38)] public Il2CppSectionMetadata assemblies;

    [Version(Min = 38)] public Il2CppSectionMetadata fieldRefs;

    [Version(Min = 38)] public Il2CppSectionMetadata referencedAssemblies;

    [Version(Min = 38)] public Il2CppSectionMetadata attributeData;

    [Version(Min = 38)] public Il2CppSectionMetadata attributeDataRanges;

    [Version(Min = 38)] public Il2CppSectionMetadata unresolvedIndirectCallParameterTypes;

    [Version(Min = 38)] public Il2CppSectionMetadata unresolvedIndirectCallParameterRanges;

    [Version(Min = 38)] public Il2CppSectionMetadata windowsRuntimeTypeNames;

    [Version(Min = 38)] public Il2CppSectionMetadata windowsRuntimeStrings;

    [Version(Min = 38)] public Il2CppSectionMetadata exportedTypeDefinitions;
}

[NoReorder]
public struct Il2CppSectionMetadata
{
    public int offset;

    public int size;

    public int count;
}

[NoReorder]
public class Il2CppAssemblyDefinition
{
    [Version(Max = 15.0)] public Il2CppAssemblyNameDefinition legacyAname;

    public ImageIndex imageIndex;

    [Version(Min = 24.1)] public uint token;

    [Version(Min = 38.0)] public uint moduleToken;

    [Version(Max = 24)] public int customAttributeIndex;

    [Version(Min = 20)] public int referencedAssemblyStart;

    [Version(Min = 20)] public int referencedAssemblyCount;

    public Il2CppAssemblyNameDefinition aname;
}

[NoReorder]
public class Il2CppAssemblyNameDefinition
{
    public StringIndex nameIndex;
    public StringIndex cultureIndex;

    [Version(Max = 24.3)] public int hashValueIndex;

    public StringIndex publicKeyIndex;

    [Version(Max = 15.0)] [ArrayLength(Length = 8)]
    public byte[] _legacyPublicKeyToken;

    public uint hash_alg;
    public int hash_len;

    public uint flags;

    public int major;
    public int minor;
    public int build;
    public int revision;

    [ArrayLength(Length = 8)] public byte[] public_key_token;
}

[NoReorder]
public class Il2CppImageDefinition
{
    public StringIndex nameIndex;
    public AssemblyIndex assemblyIndex;

    public TypeDefinitionIndex typeStart;
    public uint typeCount;

    [Version(Min = 24)] public TypeDefinitionIndex exportedTypeStart;

    [Version(Min = 24)] public uint exportedTypeCount;

    public MethodIndex entryPointIndex;

    [Version(Min = 19)] public uint token;

    [Version(Min = 24.1)] public CustomAttributeIndex customAttributeStart;

    [Version(Min = 24.1)] public uint customAttributeCount;

    public bool IsValid
    {
        get => nameIndex != 0;
    }
}

[NoReorder]
public class Il2CppTypeDefinition
{
    public StringIndex nameIndex;
    public StringIndex namespaceIndex;

    [Version(Max = 24)] public int customAttributeIndex;

    public TypeIndex byvalTypeIndex;

    [Version(Max = 24.5)] public TypeIndex byrefTypeIndex;

    public TypeIndex declaringTypeIndex;
    public TypeIndex parentIndex;

    [Version(Max = 31)] public TypeIndex elementTypeIndex; // we can probably remove this one. Only used for enums

    [Version(Max = 24.1)] public int rgctxStartIndex;
    [Version(Max = 24.1)] public int rgctxCount;

    public GenericContainerIndex genericContainerIndex;

    [Version(Max = 22)] public int delegateWrapperFromManagedToNativeIndex;

    [Version(Max = 22)] public int marshalingFunctionsIndex;

    [Version(Min = 21, Max = 22)] public int ccwFunctionIndex;

    [Version(Min = 21, Max = 22)] public int guidIndex;

    public uint flags;

    public FieldIndex fieldStart;
    public MethodIndex methodStart;
    public EventIndex eventStart;
    public PropertyIndex propertyStart;
    public NestedTypeIndex nestedTypesStart;
    public InterfacesIndex interfacesStart;
    public VTableIndex vtableStart;
    public InterfacesIndex interfaceOffsetsStart;

    public ushort method_count;
    public ushort property_count;
    public ushort field_count;
    public ushort event_count;
    public ushort nested_type_count;
    public ushort vtable_count;
    public ushort interfaces_count;
    public ushort interface_offsets_count;

    public Il2CppTypeDefinitionBitfield bitfield;

    [Version(Min = 19)] public uint token;
}

[NoReorder]
public struct Il2CppTypeDefinitionBitfield
{
    public uint _value;

    public bool ValueType
    {
        get => ((_value >> 0) & 1) == 1;
    }

    public bool EnumType
    {
        get => ((_value >> 1) & 1) == 1;
    }

    public bool HasFinalize
    {
        get => ((_value >> 2) & 1) == 1;
    }

    public bool HasCctor
    {
        get => ((_value >> 3) & 1) == 1;
    }

    public bool IsBlittable
    {
        get => ((_value >> 4) & 1) == 1;
    }

    public bool IsImportOrWindowsRuntime
    {
        get => ((_value >> 5) & 1) == 1;
    }

    public PackingSize PackingSize
    {
        get => (PackingSize)((_value >> 6) & 0b1111);
    }

    public bool DefaultPackingSize
    {
        get => ((_value >> 10) & 1) == 1;
    }

    public bool DefaultClassSize
    {
        get => ((_value >> 11) & 1) == 1;
    }

    public PackingSize ClassSize
    {
        get => (PackingSize)((_value >> 12) & 0b1111);
    }

    public bool IsByRefLike
    {
        get => ((_value >> 13) & 1) == 1;
    }
}

public enum PackingSize
{
    Zero,
    One,
    Two,
    Four,
    Eight,
    Sixteen,
    ThirtyTwo,
    SixtyFour,
    OneHundredTwentyEight
}

[NoReorder]
public class Il2CppMethodDefinition
{
    public StringIndex nameIndex;

    [Version(Min = 16)] public TypeDefinitionIndex declaringType;
    public TypeIndex returnType;

    [Version(Min = 31)] public int returnParameterToken;

    public ParameterIndex parameterStart;

    [Version(Max = 24)] public int customAttributeIndex;

    public GenericContainerIndex genericContainerIndex;

    [Version(Max = 24.1)] public int methodIndex;

    [Version(Max = 24.1)] public int invokerIndex;

    [Version(Max = 24.1)] public int delegateWrapperIndex;

    [Version(Max = 24.1)] public int rgctxStartIndex;

    [Version(Max = 24.1)] public int rgctxCount;

    public uint token;
    public ushort flags;
    public ushort iflags;
    public ushort slot;
    public ushort parameterCount;

    public bool IsValid
    {
        get => nameIndex != 0;
    }
}

[NoReorder]
public class Il2CppParameterDefinition
{
    public StringIndex nameIndex;
    public uint token;

    [Version(Max = 24)] public int customAttributeIndex;

    public TypeIndex typeIndex;

    public bool IsValid
    {
        get => nameIndex != 0;
    }
}

[NoReorder]
public class Il2CppFieldDefinition
{
    public StringIndex nameIndex;
    public TypeIndex typeIndex;

    [Version(Max = 24)] public int customAttributeIndex;

    [Version(Min = 19)] public uint token;

    public bool IsValid
    {
        get => nameIndex != 0;
    }
}

[NoReorder]
public struct Il2CppFieldDefaultValue
{
    public FieldIndex fieldIndex;
    public TypeIndex typeIndex;
    public DefaultValueDataIndex dataIndex;
}

[NoReorder]
public class Il2CppPropertyDefinition
{
    public StringIndex nameIndex;
    public MethodIndex get;
    public MethodIndex set;
    public uint attrs;

    [Version(Max = 24)] public int customAttributeIndex;

    [Version(Min = 19)] public uint token;

    public bool IsValid
    {
        get => nameIndex != 0;
    }
}

[NoReorder]
public struct Il2CppCustomAttributeTypeRange
{
    [Version(Min = 24.1)] public uint token;

    public int start;
    public int count;
}

[NoReorder]
public struct Il2CppMetadataUsageList
{
    public int start;
    public int count;
}

[NoReorder]
public struct Il2CppMetadataUsagePair
{
    public uint destinationIndex;
    public uint encodedSourceIndex;
}

[NoReorder]
public struct Il2CppStringLiteral
{
    [Version(Max = 31)] public uint length;
    public StringLiteralIndex dataIndex;
}

[NoReorder]
public struct Il2CppParameterDefaultValue
{
    public ParameterIndex parameterIndex;
    public TypeIndex typeIndex;
    public DefaultValueDataIndex dataIndex;
}

[NoReorder]
public class Il2CppEventDefinition
{
    public StringIndex nameIndex;
    public TypeIndex typeIndex;
    public MethodIndex add;
    public MethodIndex remove;
    public MethodIndex raise;

    [Version(Max = 24)] public int customAttributeIndex;

    [Version(Min = 19)] public uint token;

    public bool IsValid
    {
        get => nameIndex != 0;
    }
}

[NoReorder]
public class Il2CppGenericContainer
{
    /* index of the generic type definition or the generic method definition corresponding to this container */
    public int ownerIndex; // either index into Il2CppClass metadata array or Il2CppMethodDefinition array

    public int type_argc;

    /* If true, we're a generic method, otherwise a generic type definition. */
    public int is_method;

    /* Our type parameters. */
    public GenericParameterIndex genericParameterStart;
}

[NoReorder]
public struct Il2CppFieldRef
{
    public TypeIndex typeIndex;
    public FieldIndex fieldIndex; // local offset into type fields
}

[NoReorder]
public class Il2CppGenericParameter
{
    public GenericContainerIndex ownerIndex; /* Type or method this parameter was defined in. */
    public StringIndex nameIndex;
    public GenericParameterConstraintIndex constraintsStart;
    public short constraintsCount;
    public ushort num;
    public ushort flags;
}

public enum Il2CppRGCTXDataType
{
    IL2CPP_RGCTX_DATA_INVALID,
    IL2CPP_RGCTX_DATA_TYPE,
    IL2CPP_RGCTX_DATA_CLASS,
    IL2CPP_RGCTX_DATA_METHOD,
    IL2CPP_RGCTX_DATA_ARRAY,
    IL2CPP_RGCTX_DATA_CONSTRAINED
}

[NoReorder]
public struct Il2CppRGCTXDefinitionData
{
    public int rgctxDataDummy;

    public int methodIndex
    {
        get => rgctxDataDummy;
    }

    public int typeIndex
    {
        get => rgctxDataDummy;
    }
}

[NoReorder]
public class Il2CppRGCTXDefinition
{
    public Il2CppRGCTXDataType type
    {
        get => type_post29 == 0 ? (Il2CppRGCTXDataType)type_pre29 : (Il2CppRGCTXDataType)type_post29;
    }

    [Version(Max = 27.1)] public int type_pre29;
    [Version(Min = 29)] public ulong type_post29;
    [Version(Max = 27.1)] public Il2CppRGCTXDefinitionData data;
    [Version(Min = 27.2)] public ulong _data;
}

public enum Il2CppMetadataUsage
{
    kIl2CppMetadataUsageInvalid,
    kIl2CppMetadataUsageTypeInfo,
    kIl2CppMetadataUsageIl2CppType,
    kIl2CppMetadataUsageMethodDef,
    kIl2CppMetadataUsageFieldInfo,
    kIl2CppMetadataUsageStringLiteral,
    kIl2CppMetadataUsageMethodRef
}

[NoReorder]
public struct Il2CppCustomAttributeDataRange
{
    public uint token;
    public uint startOffset;
}

public enum IndexType
{
    Type,
    TypeDefinition,
    GenericContainer,
    Parameter
}

public enum IndexSize
{
    Byte = 1,
    UShort = 2,
    Int = 4,
    Default = Int
}

public interface IIl2CppIndex
{
    static IndexType Type { get; }
    static abstract IndexSize Size { get; set; }
    int Value { get; }

    void Read(BinaryStream stream);
}

[NoReorder]
public struct TypeIndex(int value) : IIl2CppIndex
{
    public static IndexSize Size { get; set; } = IndexSize.Default;
    public int Value { get; private set; } = value;

    public void Read(BinaryStream stream)
    {
        Value = stream.ReadIndex<TypeIndex>();
    }

    public static implicit operator int(TypeIndex idx)
    {
        return idx.Value;
    }

    public static implicit operator TypeIndex(int idx)
    {
        return new TypeIndex(idx);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

[NoReorder]
public struct TypeDefinitionIndex(int value) : IIl2CppIndex
{
    public static IndexSize Size { get; set; } = IndexSize.Default;
    public int Value { get; private set; } = value;

    public void Read(BinaryStream stream)
    {
        Value = stream.ReadIndex<TypeDefinitionIndex>();
    }

    public static implicit operator int(TypeDefinitionIndex idx)
    {
        return idx.Value;
    }

    public static implicit operator TypeDefinitionIndex(int idx)
    {
        return new TypeDefinitionIndex(idx);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

[NoReorder]
public struct GenericContainerIndex(int value) : IIl2CppIndex
{
    public static IndexSize Size { get; set; } = IndexSize.Default;
    public int Value { get; private set; } = value;

    public void Read(BinaryStream stream)
    {
        Value = stream.ReadIndex<GenericContainerIndex>();
    }

    public static implicit operator int(GenericContainerIndex idx)
    {
        return idx.Value;
    }

    public static implicit operator GenericContainerIndex(int idx)
    {
        return new GenericContainerIndex(idx);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}

[NoReorder]
public struct ParameterIndex(int value) : IIl2CppIndex
{
    public static IndexSize Size { get; set; } = IndexSize.Default;
    public int Value { get; private set; } = value;

    public void Read(BinaryStream stream)
    {
        Value = stream.ReadIndex<ParameterIndex>();
    }

    public static implicit operator int(ParameterIndex idx)
    {
        return idx.Value;
    }

    public static implicit operator ParameterIndex(int idx)
    {
        return new ParameterIndex(idx);
    }

    public override string ToString()
    {
        return Value.ToString();
    }
}
