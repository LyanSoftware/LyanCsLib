using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lytec.Common.Serialization;

namespace Lytec.Common.Data.IntelHex;

using static StaticData;

public static partial class StaticData
{
    public const Endian DefaultEndian = Endian.Big;

    public const string NewLine = "\r\n";
    public const string EndOfFile = ":00000001FF";

    public const int MaxAddressDataWidth = sizeof(ushort) * 8;
    public const int SegmentAddressDataWidth = 4;
    public const int LinearAddressDataWidth = MaxAddressDataWidth;
    public const int MaxSegBlockLength = 1 << SegmentAddressDataWidth;
    public const int MaxLinearBlockLength = 1 << LinearAddressDataWidth;

    public static readonly Regex RecordFormatValidRegex = new Regex(@"^:[a-f0-9]{10}", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    public static readonly Regex HexRegex = new Regex(@"^[a-f0-9]+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static readonly Record EndOfFileRecord = new Record(0, RecordType.EndOfFile, new byte[0]);

    public static readonly IReadOnlyDictionary<Format, long> MaxDataSizes = new Dictionary<Format, long>()
    {
        { Format.I8HEX_HEX80, 0xFFFF },
        { Format.I16HEX_HEX86, 0xFFFFF },
        { Format.I32HEX_HEX386, 0xFFFFFFFF },
    };
}

public enum RecordType : byte
{
    Invalid = 0xff,
    Data = 0x00,
    EndOfFile = 0x01,
    /// <summary> Extended Segment Address </summary>
    ExtendedSegmentAddress = 0x02,
    /// <summary> Start Segment Address </summary>
    StartSegmentAddress = 0x03,
    /// <summary> Extended Linear Address </summary>
    ExtendedLinearAddress = 0x04,
    /// <summary> Start Linear Address </summary>
    StartLinearAddress = 0x05
}

public enum Format
{
    Invalid = 0,
    I8HEX_HEX80,
    I16HEX_HEX86,
    I32HEX_HEX386
}

public enum StartAddressType
{
    None = 0,
    Segment,
    Linear
}

public interface IStartAddress : ISerializable
{
    Record Encode();
}

[DebuggerDisplay("{" + nameof(DebugView) + "}")]
[Serializable]
[Endian(DefaultEndian)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct StartLinearAddress : IStartAddress
{
    public uint Value { get; }

    public string DebugView => $"0x{Value:X8}";

    public StartLinearAddress(uint value) => Value = value;
    public StartLinearAddress(int value) => Value = (uint)value;

    public Record Encode() => Records.EncodeStartAddress(Value);

    public byte[] Serialize() => this.ToBytes();

    public override string ToString() => Value.ToString("X8");

    public static implicit operator StartLinearAddress(int value) => new StartLinearAddress(value);
    public static implicit operator int(StartLinearAddress value) => (int)value.Value;
    public static implicit operator StartLinearAddress(uint value) => new StartLinearAddress(value);
    public static implicit operator uint(StartLinearAddress value) => value.Value;
}

[DebuggerDisplay("{" + nameof(DebugView) + "}")]
[Serializable]
[Endian(DefaultEndian)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly struct StartSegmentAddress : IStartAddress
{
    public ushort CodeSegment { get; }

    public ushort InstructionPointer { get; }

    public string DebugView => $"Code Segment: 0x{CodeSegment:X4}, Instruction Pointer : 0x{InstructionPointer:X4}";

    public StartSegmentAddress(ushort codeSegment, ushort instructionPointer)
    {
        CodeSegment = codeSegment;
        InstructionPointer = instructionPointer;
    }

    public void Deconstruct(out ushort CodeSegment, out ushort InstructionPointer)
    {
        CodeSegment = this.CodeSegment;
        InstructionPointer = this.InstructionPointer;
    }

    public Record Encode() => Records.EncodeStartAddress(this);

    public byte[] Serialize() => this.ToBytes();
}

[DebuggerDisplay("{" + nameof(DebugView) + "}")]
public readonly struct Record : ISerializable
{
    public const string NewLine = StaticData.NewLine;
    public const string EndOfFile = StaticData.EndOfFile;

    public int ByteCount => Data.Length;

    public ushort Address { get; }

    public RecordType Type { get; }

    public bool IsValid => Type != RecordType.Invalid;

    public byte[] Data { get; }

    public string DebugView
    {
        get
        {
            var t = Data != null ? Type : RecordType.Invalid;
            switch (t)
            {
                case RecordType.Invalid:
                    return "[Invalid]";
                default:
                case RecordType.EndOfFile:
                    return $"[{Type}]";
                case RecordType.Data:
                    return $"[{Type}] 0x{Address:X4}: {Data!.ToHex()}";
                case RecordType.ExtendedSegmentAddress:
                case RecordType.ExtendedLinearAddress:
                    return $"[{Type}] 0x{Data!.ToStruct<ushort>(DefaultEndian):X4}";
                case RecordType.StartSegmentAddress:
                case RecordType.StartLinearAddress:
                    return $"[{Type}] 0x{Data!.ToStruct<int>(DefaultEndian):X8}";
            }
        }
    }

    public Record(ushort address, RecordType type, byte[] data)
    {
        Address = address;
        Type = type;
        Data = data;
    }

    public Record ChangeAddress(ushort address) => new Record(address, Type, Data);

    public byte[] Serialize() => Serialize(false);
    public byte[] Serialize(bool addNewLine)
    {
        var r = ToString();
        return Encoding.ASCII.GetBytes(addNewLine ? r + NewLine : r);
    }

    public override string ToString()
    {
        switch (Type)
        {
            case RecordType.Invalid:
                return "Invalid";
            case RecordType.EndOfFile:
                return StaticData.EndOfFile;
            default:
                var str = $":{(byte)Data.Length:X2}{Address:X4}{(byte)Type:X2}{Data.ToHex("")}";
                return str + (-str[1..].HexToByteArray().Sum(b => b) & 0xFF).ToString("X2");
        }
    }

    public static bool TryParse(string data, out Record Record)
    {
        Record = default;
        data = new string(data.SkipWhile(c => c != ':').ToArray());
        if (!RecordFormatValidRegex.IsMatch(data))
            return false;
        var count = Convert.ToInt32(data.Substring(1, 2), 16);
        if (data.Length < 11 + count * 2)
            return false;
        var bytesStr = data.Substring(1, 10 + count * 2);
        if (!HexRegex.IsMatch(bytesStr))
            return false;
        var bytes = bytesStr.HexToByteArray("");
        var addr = (bytes[1] << 8) | bytes[2];
        var type = (RecordType)bytes[3];
        if ((bytes.Sum(b => b) & 0xff) != 0)
            return false;
        Record = new Record((ushort)addr, type, bytes.Subarray(4, count));
        return true;
    }

    public static Record Parse(string data) => TryParse(data, out var r) ? r : throw new FormatException();

}

[DebuggerDisplay("{" + nameof(DebugView) + "}")]
public readonly struct DataBlock
{
    public int Address { get; }
    public byte[] Data { get; }

    public int Length => Data.Length;

    public int EndAddress => Address + Length;

    public string DebugView => $"0x{Address:X8}: {Data.Length} bytes, Next: 0x{Address + Data.Length:X8}";

    public DataBlock(int address, params byte[] data)
    {
        Address = address;
        Data = data;
    }

    public DataBlock(params byte[] data) : this(0, data) { }

    public DataBlock(IEnumerable<byte> data) : this(data.ToArray()) { }

    public DataBlock(int address, IEnumerable<byte> data) : this(address, data.ToArray()) { }
}

public class Records : ISerializable, IReadOnlyList<Record>
{
    public const string NewLine = StaticData.NewLine;
    public const string EndOfFile = StaticData.EndOfFile;

    public static int EncodeMaxRecordDataLength { get; set; } = 16;

    #region 实现IReadOnlyList

    public int Count => _Records.Count;

    public Record this[int index] => _Records[index];

    public IEnumerator<Record> GetEnumerator() => _Records.GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

    #endregion

    protected List<Record> _Records { get; set; }

    public IStartAddress? StartAddress => (IStartAddress?)StartLinearAddress ?? StartSegmentAddress;

    public Record StartAddressRecord => _Records.GetStartAddressRecord();

    public StartAddressType StartAddressType => _Records.GetStartAddressType();

    public StartLinearAddress? StartLinearAddress => _Records.GetStartLinearAddress();

    public StartSegmentAddress? StartSegmentAddress => _Records.GetStartSegmentAddress();

    public byte FillByte { get; set; } = 0xFF;

    public virtual byte[] Data
    {
        get
        {
            var blocks = Serialize();
            if (blocks == null || blocks.Count < 1)
                return Array.Empty<byte>();
            return blocks.Serialize(FillByte).Data ?? Array.Empty<byte>();
        }
    }

    public virtual int DataLength
    {
        get
        {
            var blocks = Serialize();
            var sa = this.GetStartAddressRecord();
            return blocks.Max(b => b.EndAddress) - blocks.Min(b => b.Address);
        }
    }

    public virtual List<DataBlock> DataBlocks => Serialize();

    byte[] ISerializable.Serialize() => Data;

    public List<DataBlock> Serialize() => _Records.Serialize();

    public Records(IEnumerable<Record> records) => _Records = records.ToList();

    public Records(IEnumerable<byte> data, IStartAddress? startAddress = null, bool addEOF = true, Format format = Format.I32HEX_HEX386, int addressOffset = 0)
        : this(Encode(data, startAddress, addEOF, format, addressOffset)) { }
    public Records(IEnumerable<IEnumerable<byte>> data, IStartAddress? startAddress = null, bool addEOF = true, Format format = Format.I32HEX_HEX386, int addressOffset = 0)
        : this(Encode(data, startAddress, addEOF, format, addressOffset)) { }
    public Records(DataBlock data, IStartAddress? startAddress = null, bool addEOF = true, Format format = Format.I32HEX_HEX386)
        : this(Encode(data, startAddress, addEOF, format)) { }
    public Records(IEnumerable<DataBlock> data, IStartAddress? startAddress = null, bool addEOF = true, Format format = Format.I32HEX_HEX386)
        : this(Encode(data, startAddress, addEOF, format)) { }

    public static Record EncodeStartAddress(IStartAddress startAddress)
    {
        switch (startAddress)
        {
            case StartLinearAddress linearAddr: return EncodeStartAddress(linearAddr);
            case StartSegmentAddress segAddr: return EncodeStartAddress(segAddr);
            default: throw new NotImplementedException();
        }
    }

    public static Record EncodeStartAddress(StartLinearAddress address)
    => EncodeStartAddress(RecordType.StartLinearAddress, address.Serialize());

    public static Record EncodeStartAddress(int address)
    => EncodeStartAddress(new StartLinearAddress(address));

    public static Record EncodeStartAddress(StartSegmentAddress address)
    => EncodeStartAddress(RecordType.StartSegmentAddress, address.Serialize());

    public static Record EncodeStartAddress(ushort codeSegment, ushort instructionPointer)
    => EncodeStartAddress(new StartSegmentAddress(codeSegment, instructionPointer));

    private static Record EncodeStartAddress(RecordType type, byte[] address)
    => new Record(0, type, address);

    public static IEnumerable<Record> Encode(IEnumerable<byte> data, IStartAddress? startAddress = null, bool addEOF = true, Format format = Format.I32HEX_HEX386, int addressOffset = 0)
    => Encode(new DataBlock[] { new DataBlock(addressOffset, data) }, startAddress, addEOF, format);
    public static IEnumerable<Record> Encode(IEnumerable<IEnumerable<byte>> data, IStartAddress? startAddress = null, bool addEOF = true, Format format = Format.I32HEX_HEX386, int addressOffset = 0)
    => Encode(data.Select(d => new DataBlock(addressOffset, d)), startAddress, addEOF, format);
    public static IEnumerable<Record> Encode(DataBlock data, IStartAddress? startAddress = null, bool addEOF = true, Format format = Format.I32HEX_HEX386)
    => Encode(new DataBlock[] { data }, startAddress, addEOF, format);

    public static IEnumerable<Record> Encode(IEnumerable<DataBlock> data, IStartAddress? startAddress = null, bool addEOF = true, Format format = Format.I32HEX_HEX386)
    {
        var recs = new List<Record>();
        var maxBlockLen = 0;
        var addrWidth = MaxAddressDataWidth;
        var extAddrType = RecordType.Invalid;
        switch (format)
        {
            case Format.Invalid:
                throw new ArgumentException("Invalid Format", nameof(format));
            case Format.I8HEX_HEX80:
                break;
            case Format.I16HEX_HEX86:
                maxBlockLen = MaxSegBlockLength;
                addrWidth = SegmentAddressDataWidth;
                extAddrType = RecordType.ExtendedSegmentAddress;
                break;
            case Format.I32HEX_HEX386:
                maxBlockLen = MaxLinearBlockLength;
                addrWidth = LinearAddressDataWidth;
                extAddrType = RecordType.ExtendedLinearAddress;
                break;
        }
        int getMaxBlockEnd(int addr) => (addr + maxBlockLen) / maxBlockLen * maxBlockLen;
        foreach (var db in data)
        {
            void enc(DataBlock db1)
            {
                recs.Add(new Record(0, extAddrType, ((ushort)(db1.Address >> addrWidth)).ToBytes(DefaultEndian)));
                var addr = db1.Address;
                for (int i = 0, len; i < db1.Length; i += len)
                {
                    len = Math.Min(EncodeMaxRecordDataLength, db1.Length - i);
                    var buf = new byte[len];
                    Array.Copy(db1.Data, i, buf, 0, len);
                    recs.Add(new Record((ushort)(BitHelper.GetValue(addr + i, 0, addrWidth)), RecordType.Data, buf));
                }
            }

            if (db.Length >= maxBlockLen || db.Address + db.Length > getMaxBlockEnd(db.Address))
            {
                for (int i = 0, len; i < db.Length; i += len)
                {
                    var addr = db.Address + i;
                    len = Math.Min(maxBlockLen, db.Length - i);
                    var end = getMaxBlockEnd(addr - 1);
                    if (addr < end && addr + len > end)
                        len = end - addr;
                    var buf = new byte[len];
                    Array.Copy(db.Data, i, buf, 0, len);
                    enc(new DataBlock(addr, buf));
                }
            }
            else enc(db);
        }
        if (startAddress != null)
            recs.Add(EncodeStartAddress(startAddress));
        if (addEOF)
            recs.Add(EndOfFileRecord);
        return recs;
    }

    public static Records? Decode(string data)
    {
        var records = new List<Record>();
        foreach (var line in from line in data.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                             where !line.IsNullOrWhiteSpace()
                             select line)
        {
            if (Record.TryParse(line, out var rec))
                records.Add(rec);
        }
        if (records.Count == 0)
            return null;
        return new Records(records);
    }

    public static Records? Decode(Stream data)
    {
        var records = new List<Record>();
        using (var r = new StreamReader(data, Encoding.UTF8))
        {
            while (!r.EndOfStream)
            {
                var line = r.ReadLine();
                if (!line.IsNullOrWhiteSpace() && Record.TryParse(line, out var rec))
                    records.Add(rec);
            }
        }
        if (records.Count == 0)
            return null;
        return new Records(records);
    }

    public static Records? Decode(byte[] data) => Decode(Encoding.UTF8.GetString(data));
}

public static class Utils
{

    public static void Save(this IEnumerable<DataBlock> src, string filename, int startAddress)
    => src.Save(filename, (IStartAddress)new StartLinearAddress(startAddress));

    public static void Save(this IEnumerable<DataBlock> src, Stream stream, int startAddress)
    => src.Save(stream, (IStartAddress)new StartLinearAddress(startAddress));

    public static void Save(this IEnumerable<DataBlock> src, string filename, uint startAddress)
    => src.Save(filename, (IStartAddress)new StartLinearAddress(startAddress));

    public static void Save(this IEnumerable<DataBlock> src, Stream stream, uint startAddress)
    => src.Save(stream, (IStartAddress)new StartLinearAddress(startAddress));

    public static void Save(this IEnumerable<DataBlock> src, string filename, IStartAddress? startAddress = null)
    {
        using var fs = File.Open(filename, FileMode.Create, FileAccess.Write, FileShare.Read);
        src.Save(fs, startAddress);
    }

    private static void Save(this IEnumerable<DataBlock> src, Stream stream, IStartAddress? startAddress = null)
    {
        using (var fw = new StreamWriter(stream, Encoding.ASCII))
        {
            var writeTask = Task.CompletedTask;
            var blocks = new List<DataBlock>();
            var end = 0;
            void nextBlock()
            {
                if (blocks.Count < 1)
                    return;
                var bs = blocks.ToArray();
                end = 0;
                blocks.Clear();
                writeTask = writeTask.ContinueWith(_ =>
                {
                    IEnumerable<byte> data = new List<byte>();
                    var addr = bs.First().Address;
                    foreach (var b in bs)
                        data = data.Take(b.Address - addr).Concat(b.Data);
                    var recs = Records.Encode(new DataBlock[] { new DataBlock(addr, data) });
                    foreach (var r in recs)
                        fw.Write(r.ToString() + Record.NewLine);
                });
            }
            foreach (var b in src)
            {
                if (blocks.Count > 0 && b.Address > end)
                    nextBlock();
                blocks.Add(b);
                end = Math.Max(end, b.Address + b.Data.Length);
            }
            nextBlock();
            writeTask.Wait();
            if (startAddress != null)
                fw.Write(startAddress.Encode().ToString() + Record.NewLine);
            fw.Write(EndOfFile + Record.NewLine);
        }
    }
    public static (byte[] Data, int StartAddress) Serialize(this IEnumerable<DataBlock> src, byte FillByte = 0xFF)
    {
        var blocks = src.ToList();
        if (blocks == null || blocks.Count < 1)
            return (Array.Empty<byte>(), 0);
        if (blocks.Count == 1)
            return (blocks[0].Data, blocks[0].Address);
        blocks = blocks.OrderBy(b => b.Address).ToList();
        var data = new List<byte>();
        var addr = blocks[0].Address;
        foreach (var b in blocks)
        {
            var fillLen = b.Address - addr;
            if (fillLen > 0)
            {
                data.AddRange(Enumerable.Repeat(FillByte, fillLen));
                addr += fillLen;
            }
            else if (fillLen < 0)
            {
                for (var i = -fillLen; i > 0; i--)
                    data.RemoveAt(data.Count - 1);
                addr += fillLen;
            }
            data.AddRange(b.Data);
            addr += b.Length;
        }
        return (data.ToArray(), blocks[0].Address);
    }

    public static byte[] GetData(this IEnumerable<DataBlock> src, int address, int length)
    {
        var data = new List<byte>();
        var end = address + length;
        foreach (var block in src.Reverse()) // 总是从后往前找
        {
            if (block.Address <= address && block.EndAddress >= address)
            {
                var buf = block.Data;
                var c = data.Count;
                data.AddRange(buf.Skip(address - block.Address).Take(end - address));
                address += data.Count - c;
                if (address >= end)
                    break;
            }
        }
        return data.ToArray();
    }

    public static IEnumerable<byte> GetDataSequence(this IEnumerable<DataBlock> src, int address, int length)
    {
        var end = address + length;
        foreach (var block in src.Reverse()) // 总是从后往前找
        {
            if (block.Address <= address && block.EndAddress >= address)
            {
                var offset = 0;
                foreach (var b in block.Data.Skip(address - block.Address).Take(end - address))
                {
                    yield return b;
                    offset++;
                }
                address += offset;
                if (address >= end)
                    yield break;
            }
        }
    }

    public static bool IsMergeable(this DataBlock block1, DataBlock block2)
    => (block1.Address >= block2.Address && block1.Address < block2.EndAddress) // 块1的起点落在块2范围内
    || (block1.EndAddress >= block2.Address && block1.EndAddress < block2.EndAddress) // 块1的终点落在块2范围内
    || IsMergeable(block2, block1); // 交换两个块再判断一次

    public static IList<DataBlock> ToMerged(this IEnumerable<DataBlock> data)
    {
        var src = new LinkedList<DataBlock>(data);
        var blocks = new LinkedList<DataBlock>();
        foreach (var block in src.Select(b => new LinkedListNode<DataBlock>(b)))
        {
            var notFound = true;
            for (var node = blocks.First; node != null; node = node.Next)
            {
                if (node.Value.Length < 1)
                    continue;
                if (block.Value.IsMergeable(node.Value))
                {
                    var nodes = new List<LinkedListNode<DataBlock>>() { node };
                    var end = block.Value.EndAddress > node.Value.EndAddress ? block : node;
                    for (var next = node.Next; next != null && end.Value.IsMergeable(next.Value); next = next.Next)
                    {
                        nodes.Add(next);
                        end = end.Value.EndAddress > next.Value.EndAddress ? end : next;
                    }

                    var start = nodes.Min(v => v.Value.Address);
                    var buf = new byte[nodes.Max(v => v.Value.EndAddress) - start];
                    foreach (var b in nodes)
                        Array.Copy(b.Value.Data, 0, buf, b.Value.Address - start, b.Value.Length);
                    node.Value = new DataBlock(start, buf);

                    notFound = false;
                    break;
                }
                if (block.Value.Address < node.Value.Address)
                {
                    blocks.AddBefore(node, block);
                    notFound = false;
                    break;
                }
            }
            if (notFound)
                blocks.AddLast(block);
        }
        return blocks.ToList();
    }

    public static Records ToMerged(this IEnumerable<Record> records)
    {
        var recs = new Records(new Records(records).Serialize().ToMerged());
        if (records.GetStartAddressRecord() is Record r)
            recs = new Records(new Record[] { r }.Concat(recs));
        return recs;
    }

    public static IStartAddress? GetStartAddress(this IEnumerable<Record> Records)
    {
        if (GetStartAddressRecord(Records) is Record r)
        {
            switch (r.GetStartAddressType())
            {
                case StartAddressType.Segment:
                    return r.GetStartSegmentAddress();
                case StartAddressType.Linear:
                    return r.GetStartLinearAddress();
            }
        }
        return null;
    }

    public static IEnumerable<Record> GetStartAddressRecords(this IEnumerable<Record> Records)
    {
        if (Records == null || !Records.Any())
            return Array.Empty<Record>();
        return Records.SkipWhile(r =>
        {
            switch (r.Type)
            {
                case RecordType.StartSegmentAddress:
                case RecordType.StartLinearAddress:
                    return false;
                default:
                    return true;
            }
        }).Take(1);
    }

    public static Record GetStartAddressRecord(this IEnumerable<Record> Records)
    {
        var addr = Records.Reverse().Take(3).GetStartAddressRecords().ToArray(); // 取尾3个
        if (Records.Take(4).Count() > 3) // 不止3个
        {
            if (addr.Length < 1)
                addr = Records.Take(2).GetStartAddressRecords().ToArray(); // 取头2个
            if (addr.Length < 1)
                addr = Records.Reverse().Skip(3).GetStartAddressRecords().Take(1).ToArray(); // 都没有，老实遍历
        }
         return addr.Length > 0 ? addr[0] : default;
    }

    public static StartAddressType GetStartAddressType(this Record startAddrRecord)
    {
        switch (startAddrRecord.Type)
        {
            case RecordType.StartSegmentAddress:
                return StartAddressType.Segment;
            case RecordType.StartLinearAddress:
                return StartAddressType.Linear;
            default:
                return StartAddressType.None;
        }
    }

    public static StartAddressType GetStartAddressType(this IEnumerable<Record> Records)
    => Records.GetStartAddressRecord().GetStartAddressType();

    public static StartLinearAddress? GetStartLinearAddress(this Record startAddrRecord)
    => startAddrRecord.Type == RecordType.StartLinearAddress ? startAddrRecord.Data.ToStruct<int>(DefaultEndian) : (StartLinearAddress?)null;

    public static StartLinearAddress? GetStartLinearAddress(this IEnumerable<Record> Records)
    => Records.GetStartAddressRecord().GetStartLinearAddress();

    public static StartSegmentAddress? GetStartSegmentAddress(this Record startAddrRecord)
    {
        if (startAddrRecord.Type != RecordType.StartSegmentAddress)
            return null;
        var addr = startAddrRecord.Data.ToStruct<int>(DefaultEndian);
        var cs = BitHelper.GetValue(addr, 16, 16);
        var ip = BitHelper.GetValue(addr, 0, 16);
        return new StartSegmentAddress((ushort)cs, (ushort)ip);
    }

    public static StartSegmentAddress? GetStartSegmentAddress(this IEnumerable<Record> Records)
    => Records.GetStartAddressRecord().GetStartSegmentAddress();

    public static List<DataBlock> Serialize(this IEnumerable<Record> Records)
    {
        var blocks = new List<DataBlock>();

        var datas = new List<DataBlock>();
        var segAddr = 0;
        var linearAddr = 0;
        void endBlock()
        {
            if (!datas.Any())
                return;
            var data = new List<byte>();
            var start = datas.Min(r => r.Address);
            var addr = start;
            void endPart()
            {
                if (!data.Any())
                    return;
                var rdata = data as IEnumerable<byte>;
                if (blocks.Any())
                {
                    var lb = blocks.Last();
                    if (start == lb.Address + lb.Length)
                    {
                        start = lb.Address;
                        rdata = lb.Data.Concat(rdata);
                        blocks.RemoveAt(blocks.Count - 1);
                    }
                }
                blocks.Add(new DataBlock(start, rdata.ToArray()));
                data.Clear();
            }
            foreach (var r in datas.OrderBy(r => r.Address))
            {
                if (addr != r.Address)
                {
                    endPart();
                    start = addr = r.Address;
                }
                data.AddRange(r.Data);
                addr += r.Length;
            }
            endPart();
            datas.Clear();
        }

        {
            var addr = 0;
            foreach (var r in Records)
            {
                if (r.Type == RecordType.EndOfFile)
                    break;
                var segAddr2 = segAddr;
                var linearAddr2 = linearAddr;
                switch (r.Type)
                {
                    default:
                    case RecordType.Invalid:
                        break;
                    case RecordType.Data:
                        datas.Add(new DataBlock(addr + r.Address, r.Data));
                        continue;
                    case RecordType.ExtendedSegmentAddress:
                    case RecordType.ExtendedLinearAddress:
                        if (r.ByteCount == 2)
                        {
                            var addr2 = r.Data.ToStruct<ushort>(DefaultEndian);
                            switch (r.Type)
                            {
                                case RecordType.ExtendedSegmentAddress:
                                    segAddr2 = addr2 << SegmentAddressDataWidth;
                                    break;
                                case RecordType.ExtendedLinearAddress:
                                    linearAddr2 = addr2 << LinearAddressDataWidth;
                                    break;
                            }
                        }
                        break;
                }
                if (segAddr2 != segAddr)
                {
                    endBlock();
                    addr = segAddr2;
                    segAddr = segAddr2;
                }
                else if (linearAddr2 != linearAddr)
                {
                    endBlock();
                    addr = linearAddr2;
                    linearAddr = linearAddr2;
                }
            }
            endBlock();
        }

        return blocks;
    }

}
