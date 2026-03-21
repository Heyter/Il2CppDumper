using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;

namespace Il2CppDumper;

public sealed class Metadata : BinaryStream
{
    private static readonly Dictionary<uint, string> stringCache = new();
    private static readonly Dictionary<Type, IndexSize> indexSizeCache = new();
    private readonly Dictionary<Il2CppImageDefinition, Dictionary<uint, int>> attributeTypeRangesDic;
    private readonly Dictionary<int, Il2CppFieldDefaultValue> fieldDefaultValuesDic;
    private readonly Il2CppMetadataUsageList[] metadataUsageLists;
    private readonly Il2CppMetadataUsagePair[] metadataUsagePairs;
    private readonly Dictionary<int, Il2CppParameterDefaultValue> parameterDefaultValuesDic;
    public Il2CppAssemblyDefinition[] assemblyDefs;
    public Il2CppCustomAttributeDataRange[] attributeDataRanges;
    public Il2CppCustomAttributeTypeRange[] attributeTypeRanges;
    public int[] attributeTypes;
    public int[] constraintIndices;
    public Il2CppEventDefinition[] eventDefs;
    public Il2CppFieldDefinition[] fieldDefs;
    public Il2CppFieldRef[] fieldRefs;
    public Il2CppGenericContainer[] genericContainers;
    public Il2CppGenericParameter[] genericParameters;
    public Il2CppGlobalMetadataHeader header;
    public Il2CppImageDefinition[] imageDefs;
    public int[] interfaceIndices;
    public Dictionary<Il2CppMetadataUsage, SortedDictionary<uint, uint>> metadataUsageDic;
    public long metadataUsagesCount;
    public Il2CppMethodDefinition[] methodDefs;
    public int[] nestedTypeIndices;
    public Il2CppParameterDefinition[] parameterDefs;
    public Il2CppPropertyDefinition[] propertyDefs;
    public Il2CppRGCTXDefinition[] rgctxEntries;
    public Il2CppStringLiteral[] stringLiterals;
    public Il2CppTypeDefinition[] typeDefs;
    public uint[] vtableMethods;

    public Metadata(Stream stream) : base(stream)
    {
        var sanity = ReadUInt32();
        if (sanity != 0xFAB11BAF)
        {
            throw new InvalidDataException("ERROR: Metadata file supplied is not valid metadata file.");
        }

        var version = ReadInt32();
        if (version < 0 || version > 1000)
        {
            throw new InvalidDataException("ERROR: Metadata file supplied is not valid metadata file.");
        }

        if (version < 16 || version > 39)
        {
            throw new NotSupportedException($"ERROR: Metadata file supplied is not a supported version[{version}].");
        }

        Version = version;
        header = ReadClass<Il2CppGlobalMetadataHeader>(0);

        GenericContainerIndex.Size = IndexSize.Default;
        TypeDefinitionIndex.Size = IndexSize.Default;
        TypeIndex.Size = IndexSize.Default;
        ParameterIndex.Size = IndexSize.Default;

        if (Version >= 38)
        {
            IndexSize GetIndexSize<T>(Il2CppSectionMetadata section)
            {
                var indexSize = section.count switch
                {
                    <= byte.MaxValue => IndexSize.Byte,
                    <= ushort.MaxValue => IndexSize.UShort,
                    _ => IndexSize.Default
                };

                return indexSize;
            }

            GenericContainerIndex.Size = GetIndexSize<Il2CppGenericContainer>(header.genericContainers);
            TypeDefinitionIndex.Size = GetIndexSize<Il2CppTypeDefinition>(header.typeDefinitions);

            var expectedEventSize = header.events.size / header.events.count;
            var eventSize = SizeOf(typeof(Il2CppEventDefinition));

            if (expectedEventSize == eventSize)
            {
                TypeIndex.Size = IndexSize.Int;
            }
            else if (expectedEventSize == eventSize - 2)
            {
                TypeIndex.Size = IndexSize.UShort;
            }
            else if (expectedEventSize == eventSize - 3)
            {
                TypeIndex.Size = IndexSize.Byte;
            }
            else
            {
                throw new Exception("Failed to determine Type index size.");
            }

            if (Version >= 39)
            {
                ParameterIndex.Size = GetIndexSize<Il2CppParameterDefinition>(header.parameters);
            }
        }

        if (version == 24)
        {
            if (header.stringLiteralOffset == 264)
            {
                Version = 24.2;
                header = ReadClass<Il2CppGlobalMetadataHeader>(0);
            }
            else
            {
                imageDefs = ReadGlobalMetadataArray<Il2CppImageDefinition>(
                    header.imagesOffset,
                    header.imagesSize,
                    header.images);
                if (imageDefs.Any(x => x.token != 1))
                {
                    Version = 24.1;
                }
            }
        }

        imageDefs = ReadGlobalMetadataArray<Il2CppImageDefinition>(
            header.imagesOffset,
            header.imagesSize,
            header.images);
        if (Version == 24.2 && header.assembliesSize / 68 < imageDefs.Length)
        {
            Version = 24.4;
        }

        var v241Plus = false;
        if (Version == 24.1 && header.assembliesSize / 64 == imageDefs.Length)
        {
            v241Plus = true;
        }

        if (v241Plus)
        {
            Version = 24.4;
        }

        assemblyDefs = ReadGlobalMetadataArray<Il2CppAssemblyDefinition>(
            header.assembliesOffset,
            header.assembliesSize,
            header.assemblies);
        if (v241Plus)
        {
            Version = 24.1;
        }

        typeDefs = ReadGlobalMetadataArray<Il2CppTypeDefinition>(
            header.typeDefinitionsOffset,
            header.typeDefinitionsSize,
            header.typeDefinitions);
        methodDefs = ReadGlobalMetadataArray<Il2CppMethodDefinition>(
            header.methodsOffset,
            header.methodsSize,
            header.methods);
        parameterDefs = ReadGlobalMetadataArray<Il2CppParameterDefinition>(
            header.parametersOffset,
            header.parametersSize,
            header.parameters);
        fieldDefs = ReadGlobalMetadataArray<Il2CppFieldDefinition>(
            header.fieldsOffset,
            header.fieldsSize,
            header.fields);
        var fieldDefaultValues = ReadGlobalMetadataArray<Il2CppFieldDefaultValue>(
            header.fieldDefaultValuesOffset,
            header.fieldDefaultValuesSize,
            header.fieldDefaultValues);
        var parameterDefaultValues = ReadGlobalMetadataArray<Il2CppParameterDefaultValue>(
            header.parameterDefaultValuesOffset,
            header.parameterDefaultValuesSize,
            header.parameterDefaultValues);
        fieldDefaultValuesDic = fieldDefaultValues.ToDictionary(x => x.fieldIndex);
        parameterDefaultValuesDic = parameterDefaultValues.ToDictionary(x => (int)x.parameterIndex);
        propertyDefs = ReadGlobalMetadataArray<Il2CppPropertyDefinition>(
            header.propertiesOffset,
            header.propertiesSize,
            header.properties);
        interfaceIndices = ReadGlobalMetadataPrimitiveArray<int>(
            header.interfacesOffset,
            header.interfacesSize,
            header.interfaces,
            4);
        nestedTypeIndices = ReadGlobalMetadataPrimitiveArray<int>(
            header.nestedTypesOffset,
            header.nestedTypesSize,
            header.nestedTypes,
            4);
        eventDefs = ReadGlobalMetadataArray<Il2CppEventDefinition>(
            header.eventsOffset,
            header.eventsSize,
            header.events);
        genericContainers = ReadGlobalMetadataArray<Il2CppGenericContainer>(
            header.genericContainersOffset,
            header.genericContainersSize,
            header.genericContainers);
        genericParameters = ReadGlobalMetadataArray<Il2CppGenericParameter>(
            header.genericParametersOffset,
            header.genericParametersSize,
            header.genericParameters);
        constraintIndices = ReadGlobalMetadataPrimitiveArray<int>(
            header.genericParameterConstraintsOffset,
            header.genericParameterConstraintsSize,
            header.genericParameterConstraints,
            4);
        vtableMethods = ReadGlobalMetadataPrimitiveArray<uint>(
            header.vtableMethodsOffset,
            header.vtableMethodsSize,
            header.vtableMethods,
            4);
        stringLiterals = ReadGlobalMetadataArray<Il2CppStringLiteral>(
            header.stringLiteralOffset,
            header.stringLiteralSize,
            header.stringLiterals);
        if (Version > 16)
        {
            fieldRefs = ReadGlobalMetadataArray<Il2CppFieldRef>(
                header.fieldRefsOffset,
                header.fieldRefsSize,
                header.fieldRefs);
            if (Version < 27)
            {
                metadataUsageLists = ReadMetadataClassArray<Il2CppMetadataUsageList>(
                    header.metadataUsageListsOffset,
                    header.metadataUsageListsCount);
                metadataUsagePairs = ReadMetadataClassArray<Il2CppMetadataUsagePair>(
                    header.metadataUsagePairsOffset,
                    header.metadataUsagePairsCount);

                ProcessingMetadataUsage();
            }
        }

        if (Version > 20 && Version < 29)
        {
            attributeTypeRanges = ReadMetadataClassArray<Il2CppCustomAttributeTypeRange>(
                header.attributesInfoOffset,
                header.attributesInfoCount);
            attributeTypes = ReadClassArray<int>((ulong)header.attributeTypesOffset, header.attributeTypesCount / 4);
        }

        if (Version >= 29)
        {
            attributeDataRanges = ReadGlobalMetadataArray<Il2CppCustomAttributeDataRange>(
                header.attributeDataRangeOffset,
                header.attributeDataRangeSize,
                header.attributeDataRanges);
        }

        if (Version > 24)
        {
            attributeTypeRangesDic = new Dictionary<Il2CppImageDefinition, Dictionary<uint, int>>();
            foreach (var imageDef in imageDefs)
            {
                var dic = new Dictionary<uint, int>();
                attributeTypeRangesDic[imageDef] = dic;
                var end = imageDef.customAttributeStart + imageDef.customAttributeCount;
                for (var i = imageDef.customAttributeStart; i < end; i++)
                {
                    if (Version >= 29)
                    {
                        dic.Add(attributeDataRanges[i].token, i);
                    }
                    else
                    {
                        dic.Add(attributeTypeRanges[i].token, i);
                    }
                }
            }
        }

        if (Version <= 24.1)
        {
            rgctxEntries = ReadMetadataClassArray<Il2CppRGCTXDefinition>(
                header.rgctxEntriesOffset,
                header.rgctxEntriesCount);
        }
    }

    private T[] ReadGlobalMetadataArray<T>(int legacyOffset, int legacySize, Il2CppSectionMetadata metadata)
        where T : new()
    {
        if (Version >= 36)
        {
            return ReadClassArray<T>((uint)metadata.offset, metadata.size / SizeOf(typeof(T)));
        }

        return ReadClassArray<T>((uint)legacyOffset, legacySize / SizeOf(typeof(T)));
    }

    private T[] ReadGlobalMetadataPrimitiveArray<T>(
        int legacyOffset,
        int legacySize,
        Il2CppSectionMetadata metadata,
        int itemSize) where T : new()
    {
        if (Version >= 36)
        {
            return ReadClassArray<T>((uint)metadata.offset, metadata.size / itemSize);
        }

        return ReadClassArray<T>((uint)legacyOffset, legacySize / itemSize);
    }

    private T[] ReadMetadataClassArray<T>(int addr, int count) where T : new()
    {
        return ReadClassArray<T>((uint)addr, count / SizeOf(typeof(T)));
    }

    public bool GetFieldDefaultValueFromIndex(int index, out Il2CppFieldDefaultValue value)
    {
        return fieldDefaultValuesDic.TryGetValue(index, out value);
    }

    public bool GetParameterDefaultValueFromIndex(int index, out Il2CppParameterDefaultValue value)
    {
        return parameterDefaultValuesDic.TryGetValue(index, out value);
    }

    public uint GetDefaultValueFromIndex(int index)
    {
        var offset = Version >= 38
            ? header.fieldAndParameterDefaultValueData.offset
            : header.fieldAndParameterDefaultValueDataOffset;
        return (uint)(offset + index);
    }

    public string GetStringFromIndex(uint index)
    {
        if (!stringCache.TryGetValue(index, out var result))
        {
            var offset = Version >= 36 ? header.strings.offset : header.stringOffset;
            result = ReadStringToNull((ulong)offset + index);
            stringCache.Add(index, result);
        }

        return result;
    }

    public int GetCustomAttributeIndex(Il2CppImageDefinition imageDef, int customAttributeIndex, uint token)
    {
        if (Version > 24)
        {
            if (attributeTypeRangesDic[imageDef].TryGetValue(token, out var index))
            {
                return index;
            }

            return -1;
        }

        return customAttributeIndex;
    }

    public string GetStringLiteralFromIndex(uint index)
    {
        var stringLiteral = stringLiterals[index];
        var offset = Version >= 38 ? header.stringLiterals.offset : header.stringLiteralOffset;
        Position = (uint)(offset + stringLiteral.dataIndex);
        return Encoding.UTF8.GetString(ReadBytes((int)stringLiteral.length));
    }

    private void ProcessingMetadataUsage()
    {
        metadataUsageDic = new Dictionary<Il2CppMetadataUsage, SortedDictionary<uint, uint>>();
        for (uint i = 1; i <= 6; i++)
        {
            metadataUsageDic[(Il2CppMetadataUsage)i] = new SortedDictionary<uint, uint>();
        }

        foreach (var metadataUsageList in metadataUsageLists)
        {
            for (var i = 0; i < metadataUsageList.count; i++)
            {
                var offset = metadataUsageList.start + i;
                if (offset >= metadataUsagePairs.Length)
                {
                    continue;
                }

                var metadataUsagePair = metadataUsagePairs[offset];
                var usage = GetEncodedIndexType(metadataUsagePair.encodedSourceIndex);
                var decodedIndex = GetDecodedMethodIndex(metadataUsagePair.encodedSourceIndex);
                metadataUsageDic[(Il2CppMetadataUsage)usage][metadataUsagePair.destinationIndex] = decodedIndex;
            }
        }

        //metadataUsagesCount = metadataUsagePairs.Max(x => x.destinationIndex) + 1;
        metadataUsagesCount = metadataUsageDic.Max(x => x.Value.Select(y => y.Key).DefaultIfEmpty().Max()) + 1;
    }

    public static uint GetEncodedIndexType(uint index)
    {
        return (index & 0xE0000000) >> 29;
    }

    public uint GetDecodedMethodIndex(uint index)
    {
        if (Version >= 27)
        {
            return (index & 0x1FFFFFFEU) >> 1;
        }

        return index & 0x1FFFFFFFU;
    }

    public int SizeOf(Type type)
    {
        var size = 0;
        foreach (var i in type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public))
        {
            if (!i.IsPublic)
            {
                throw new Exception($"Metadata must have only public fields. \"{type.Name}.{i.Name}\" is not public.");
            }

            var attr = (VersionAttribute)Attribute.GetCustomAttribute(i, typeof(VersionAttribute));
            if (attr != null)
            {
                if (Version < attr.Min || Version > attr.Max)
                {
                    continue;
                }
            }

            var fieldType = i.FieldType;
            if (fieldType.IsPrimitive)
            {
                size += Marshal.SizeOf(fieldType);
            }
            else if (fieldType.IsEnum)
            {
                var enumType = Enum.GetUnderlyingType(fieldType);
                size += Marshal.SizeOf(enumType);
            }
            else if (fieldType.IsArray)
            {
                var arrayLengthAttribute = i.GetCustomAttribute<ArrayLengthAttribute>();
                size += arrayLengthAttribute.Length;
            }
            else if (typeof(IIl2CppIndex).IsAssignableFrom(fieldType))
            {
                if (!indexSizeCache.TryGetValue(fieldType, out var indexSize))
                {
                    var prop = fieldType.GetProperty("Size", BindingFlags.Public | BindingFlags.Static);

                    indexSize = (IndexSize)prop.GetValue(null);

                    indexSizeCache.Add(fieldType, indexSize);
                }

                size += (int)indexSize;
            }
            else
            {
                size += SizeOf(fieldType);
            }
        }

        return size;
    }
}
