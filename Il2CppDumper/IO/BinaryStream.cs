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
        if (stream.CanWrite) {
            Writer = new BinaryWriter(stream, Encoding.UTF8, true);
        }
        readClass = GetType().GetMethod("ReadClass", Type.EmptyTypes);
        readClassArray = GetType().GetMethod("ReadClassArray", new[] { typeof(long) });
        genericMethodCache = new Dictionary<Type, MethodInfo>();
        attributeCache = new Dictionary<FieldInfo, VersionAttribute[]>();
    }

    public ulong Position
    {
        get => (ulong)stream.Position;
        set => stream.Position = (long)value;
    }

    public ulong Length
    {
        get => (ulong)stream.Length;
    }

    public ulong PointerSize
    {
        get => Is32Bit ? 4ul : 8ul;
    }

    public BinaryReader Reader { get; }

    public BinaryWriter Writer { get; }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public bool ReadBoolean()
    {
        return Reader.ReadBoolean();
    }

    public byte ReadByte()
    {
        return Reader.ReadByte();
    }

    public byte[] ReadBytes(int count)
    {
        return Reader.ReadBytes(count);
    }

    public sbyte ReadSByte()
    {
        return Reader.ReadSByte();
    }

    public short ReadInt16()
    {
        return Reader.ReadInt16();
    }

    public ushort ReadUInt16()
    {
        return Reader.ReadUInt16();
    }

    public int ReadInt32()
    {
        return Reader.ReadInt32();
    }

    public uint ReadUInt32()
    {
        return Reader.ReadUInt32();
    }

    public long ReadInt64()
    {
        return Reader.ReadInt64();
    }

    public ulong ReadUInt64()
    {
        return Reader.ReadUInt64();
    }

    public float ReadSingle()
    {
        return Reader.ReadSingle();
    }

    public double ReadDouble()
    {
        return Reader.ReadDouble();
    }

    public uint ReadCompressedUInt32()
    {
        return Reader.ReadCompressedUInt32();
    }

    public int ReadCompressedInt32()
    {
        return Reader.ReadCompressedInt32();
    }

    public uint ReadULeb128()
    {
        return Reader.ReadULeb128();
    }

    public void Write(bool value)
    {
        Writer.Write(value);
    }

    public void Write(byte value)
    {
        Writer.Write(value);
    }

    public void Write(sbyte value)
    {
        Writer.Write(value);
    }

    public void Write(short value)
    {
        Writer.Write(value);
    }

    public void Write(ushort value)
    {
        Writer.Write(value);
    }

    public void Write(int value)
    {
        Writer.Write(value);
    }

    public void Write(uint value)
    {
        Writer.Write(value);
    }

    public void Write(long value)
    {
        Writer.Write(value);
    }

    public void Write(ulong value)
    {
        Writer.Write(value);
    }

    public void Write(float value)
    {
        Writer.Write(value);
    }

    public void Write(double value)
    {
        Writer.Write(value);
    }

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
            _ => throw new NotSupportedException()
        };
    }

    public int ReadIndex<T>() where T : IIl2CppIndex
    {
        switch (T.Size)
        {
            case IndexSize.Byte:
                {
                    var value = ReadByte();
                    return value == byte.MaxValue ? -1 : value;
                }
            case IndexSize.UShort:
                {
                    var value = ReadUInt16();
                    return value == ushort.MaxValue ? -1 : value;
                }
            case IndexSize.Int:
                {
                    return ReadInt32();
                }
            default:
                throw new NotSupportedException();
        }
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
        foreach (var i in t.GetType().GetFields())
        {
            if (!attributeCache.TryGetValue(i, out var versionAttributes))
            {
                if (Attribute.IsDefined(i, typeof(VersionAttribute)))
                {
                    versionAttributes = i.GetCustomAttributes<VersionAttribute>().ToArray();
                    attributeCache.Add(i, versionAttributes);
                }
            }

            if (versionAttributes?.Length > 0)
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

            var fieldType = i.FieldType;
            if (fieldType.IsPrimitive)
            {
                i.SetValue(t, ReadPrimitive(fieldType));
            }
            else if (fieldType.IsEnum)
            {
                var e = fieldType.GetField("value__").FieldType;
                i.SetValue(t, ReadPrimitive(e));
            }
            else if (fieldType.IsArray)
            {
                var arrayLengthAttribute = i.GetCustomAttribute<ArrayLengthAttribute>();
                if (!genericMethodCache.TryGetValue(fieldType, out var methodInfo))
                {
                    methodInfo = readClassArray.MakeGenericMethod(fieldType.GetElementType());
                    genericMethodCache.Add(fieldType, methodInfo);
                }

                i.SetValue(t, methodInfo.Invoke(this, new object[] { arrayLengthAttribute.Length }));
            }
            else if (typeof(IIl2CppIndex).IsAssignableFrom(fieldType))
            {
                var index = (IIl2CppIndex)Activator.CreateInstance(fieldType);
                index.Read(this);
                i.SetValue(t, index);
            }
            else
            {
                if (!genericMethodCache.TryGetValue(fieldType, out var methodInfo))
                {
                    methodInfo = readClass.MakeGenericMethod(fieldType);
                    genericMethodCache.Add(fieldType, methodInfo);
                }

                i.SetValue(t, methodInfo.Invoke(this, null));
            }
        }

        return (T)t;
    }

    public T[] ReadClassArray<T>(long count) where T : new()
    {
        var t = new T[count];
        for (var i = 0; i < count; i++)
        {
            t[i] = ReadClass<T>();
        }

        return t;
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
        byte b;
        while ((b = ReadByte()) != 0)
        {
            bytes.Add(b);
        }

        return Encoding.UTF8.GetString(bytes.ToArray());
    }

    public long ReadIntPtr()
    {
        return Is32Bit ? ReadInt32() : ReadInt64();
    }

    public virtual ulong ReadUIntPtr()
    {
        return Is32Bit ? ReadUInt32() : ReadUInt64();
    }

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
