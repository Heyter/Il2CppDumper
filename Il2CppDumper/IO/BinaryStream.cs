using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace Il2CppDumper;

public class BinaryStream : IDisposable
{
    private readonly Dictionary<FieldInfo, VersionAttribute[]> attributeCache;
    private readonly Dictionary<Type, MethodInfo> genericMethodCache;
    private readonly MethodInfo readClass;
    private readonly MethodInfo readClassArray;
    private readonly Stream stream;

    public ulong ImageBase;
    public bool Is32Bit;
    public double Version;

    public BinaryStream(Stream input)
    {
        stream = input;
        Reader = new BinaryReader(stream, Encoding.UTF8, true);
        if (stream.CanWrite)
        {
            Writer = new BinaryWriter(stream, Encoding.UTF8, true);
        }

        readClass = GetType().GetMethod("ReadClass", Type.EmptyTypes)!;
        readClassArray = GetType().GetMethod("ReadClassArray", new[] { typeof(long) })!;
        genericMethodCache = new Dictionary<Type, MethodInfo>();
        attributeCache = new Dictionary<FieldInfo, VersionAttribute[]>();
    }

    public ulong Position
    {
        get => (ulong)stream.Position;
        set => stream.Position = (long)value;
    }

    public ulong Length => (ulong)stream.Length;
    public ulong PointerSize => Is32Bit ? 4ul : 8ul;
    public BinaryReader Reader { get; }
    public BinaryWriter? Writer { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool ReadBoolean() => Reader.ReadBoolean();
    public byte ReadByte() => Reader.ReadByte();
    public byte[] ReadBytes(int count) => Reader.ReadBytes(count);
    public sbyte ReadSByte() => Reader.ReadSByte();
    public short ReadInt16() => Reader.ReadInt16();
    public ushort ReadUInt16() => Reader.ReadUInt16();
    public int ReadInt32() => Reader.ReadInt32();
    public uint ReadUInt32() => Reader.ReadUInt32();
    public long ReadInt64() => Reader.ReadInt64();
    public ulong ReadUInt64() => Reader.ReadUInt64();
    public float ReadSingle() => Reader.ReadSingle();
    public double ReadDouble() => Reader.ReadDouble();
    public uint ReadCompressedUInt32() => Reader.ReadCompressedUInt32();
    public int ReadCompressedInt32() => Reader.ReadCompressedInt32();
    public uint ReadULeb128() => Reader.ReadULeb128();

    public void Write(bool value) => Writer!.Write(value);
    public void Write(byte value) => Writer!.Write(value);
    public void Write(sbyte value) => Writer!.Write(value);
    public void Write(short value) => Writer!.Write(value);
    public void Write(ushort value) => Writer!.Write(value);
    public void Write(int value) => Writer!.Write(value);
    public void Write(uint value) => Writer!.Write(value);
    public void Write(long value) => Writer!.Write(value);
    public void Write(ulong value) => Writer!.Write(value);
    public void Write(float value) => Writer!.Write(value);
    public void Write(double value) => Writer!.Write(value);

    private object ReadPrimitive(Type type)
    {
        return type.Name switch
        {
            "Int32" => ReadInt32(),
            "UInt32" => ReadUInt32(),
            "Int16" => ReadInt16(),
            "UInt16" => ReadUInt16(),
            "Byte" => ReadByte(),
            "Int64" => ReadIntPtr(),
            "UInt64" => ReadUIntPtr(),
            _ => throw new NotSupportedException(),
        };
    }

    public int ReadIndex<T>() where T : IIl2CppIndex
    {
        return T.Size switch
        {
            IndexSize.Byte => ReadByte() is var valueByte && valueByte == byte.MaxValue ? -1 : valueByte,
            IndexSize.UShort => ReadUInt16() is var valueUShort && valueUShort == ushort.MaxValue ? -1 : valueUShort,
            IndexSize.Int => ReadInt32(),
            _ => throw new NotSupportedException(),
        };
    }

    public T ReadClass<T>(ulong addr) where T : new()
    {
        Position = addr;
        return ReadClass<T>();
    }

    public T ReadClass<T>() where T : new()
    {
        var type = typeof(T);
        if (type.IsPrimitive)
        {
            return (T)ReadPrimitive(type);
        }

        object t = new T();
        foreach (var field in t.GetType().GetFields())
        {
            if (!attributeCache.TryGetValue(field, out var versionAttributes))
            {
                versionAttributes = field.GetCustomAttributes<VersionAttribute>().ToArray();
                attributeCache.Add(field, versionAttributes);
            }

            if (versionAttributes.Length > 0)
            {
                var read = false;
                foreach (var versionAttribute in versionAttributes)
                {
                    if (Version >= versionAttribute.Min && Version <= versionAttribute.Max)
                    {
                        read = true;
                        break;
                    }
                }

                if (!read)
                {
                    continue;
                }
            }

            var fieldType = field.FieldType;
            if (fieldType.IsPrimitive)
            {
                field.SetValue(t, ReadPrimitive(fieldType));
            }
            else if (fieldType.IsEnum)
            {
                var enumValueType = fieldType.GetField("value__")!.FieldType;
                field.SetValue(t, ReadPrimitive(enumValueType));
            }
            else if (fieldType.IsArray)
            {
                var arrayLengthAttribute = field.GetCustomAttribute<ArrayLengthAttribute>()!;
                if (!genericMethodCache.TryGetValue(fieldType, out var methodInfo))
                {
                    methodInfo = readClassArray.MakeGenericMethod(fieldType.GetElementType()!);
                    genericMethodCache.Add(fieldType, methodInfo);
                }

                field.SetValue(t, methodInfo.Invoke(this, new object[] { arrayLengthAttribute.Length }));
            }
            else if (typeof(IIl2CppIndex).IsAssignableFrom(fieldType))
            {
                var index = (IIl2CppIndex)Activator.CreateInstance(fieldType)!;
                index.Read(this);
                field.SetValue(t, index);
            }
            else
            {
                if (!genericMethodCache.TryGetValue(fieldType, out var methodInfo))
                {
                    methodInfo = readClass.MakeGenericMethod(fieldType);
                    genericMethodCache.Add(fieldType, methodInfo);
                }

                field.SetValue(t, methodInfo.Invoke(this, null));
            }
        }

        return (T)t;
    }

    public T[] ReadClassArray<T>(long count) where T : new()
    {
        var items = new T[count];
        for (var i = 0; i < count; i++)
        {
            items[i] = ReadClass<T>();
        }

        return items;
    }

    public T[] ReadClassArray<T>(ulong addr, ulong count) where T : new()
    {
        return ReadClassArray<T>(addr, (long)count);
    }

    public T[] ReadClassArray<T>(ulong addr, long count) where T : new()
    {
        Position = addr;
        return ReadClassArray<T>(count);
    }

    public string ReadStringToNull(ulong addr)
    {
        Position = addr;
        var bytes = new List<byte>();
        byte value;
        while ((value = ReadByte()) != 0)
        {
            bytes.Add(value);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    public long ReadIntPtr() => Is32Bit ? ReadInt32() : ReadInt64();
    public virtual ulong ReadUIntPtr() => Is32Bit ? ReadUInt32() : ReadUInt64();

    protected virtual void Dispose(bool disposing)
    {
        if (disposing)
        {
            Reader.Dispose();
            Writer?.Dispose();
            stream.Close();
        }
    }
}
