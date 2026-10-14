using Lytec.Common.Communication;
using Lytec.Protocol;

namespace Test;

public class STBootloaderTests
{
    const byte Ack = (byte)STBootloader.SpBytes.ACK;
    const byte Nack = (byte)STBootloader.SpBytes.NACK;
    const int FlashStart = 0x08000000;

    [Fact]
    public void Call_AcceptsOnlyAck()
    {
        using var ackConfig = new ScriptedConfig();
        ackConfig.Add([0x7F], [Ack]);
        Assert.True(STBootloader.Call(ackConfig, 10));
        ackConfig.AssertComplete();

        using var nackConfig = new ScriptedConfig();
        nackConfig.Add([0x7F], [Nack]);
        Assert.False(STBootloader.Call(nackConfig, 10));
        nackConfig.AssertComplete();
    }

    [Fact]
    public void GetBootloaderInfo_HandlesFragmentedResponseAndTrailingAck()
    {
        using var config = new ScriptedConfig();
        config.Add(Command(STBootloader.Command.Get), [Ack], [0x02], [0x31], [0x11, 0x44], [Ack]);

        Assert.True(STBootloader.GetBootloaderInfo(config, out var info));
        Assert.NotNull(info);
        Assert.Equal(new Version(3, 1), info.Version);
        Assert.Equal([0x11, 0x44], info.SupportedRawCommands);
        config.AssertComplete();
    }

    [Fact]
    public void GetBootloaderInfo_DrainsSplitDoubleNackBeforeRetry()
    {
        using var config = new ScriptedConfig(retries: 1);
        config.Add(Command(STBootloader.Command.Get), [Nack], [Nack]);
        config.Add(Command(STBootloader.Command.Get), [Ack, 0x00, 0x31, Ack]);

        Assert.True(STBootloader.GetBootloaderInfo(config, out var info));
        Assert.NotNull(info);
        Assert.Equal(new Version(3, 1), info.Version);
        Assert.Equal(1, config.FixedTimeoutReadCount);
        config.AssertComplete();
    }

    [Fact]
    public void Read_AdvancesAddressAcrossBlocks()
    {
        using var config = new ScriptedConfig();
        var first = Enumerable.Range(0, 256).Select(value => (byte)value).ToArray();
        var second = new byte[] { 0xA1, 0xA2, 0xA3, 0xA4 };
        AddRead(config, FlashStart, first);
        AddRead(config, FlashStart + 256, second);

        Assert.True(STBootloader.Read(config, FlashStart, 260, out var data));
        Assert.Equal(first.Concat(second), data);
        config.AssertComplete();
    }

    [Fact]
    public void Read_RetriesTheCompleteBlockTransaction()
    {
        using var config = new ScriptedConfig(retries: 1);
        config.Add(Command(STBootloader.Command.ReadMemory));
        var expected = new byte[] { 1, 2, 3, 4 };
        AddRead(config, FlashStart, expected);

        Assert.True(STBootloader.Read(config, FlashStart, expected.Length, out var data));
        Assert.Equal(expected, data);
        config.AssertComplete();
    }

    [Fact]
    public void Write_UsesRequestedAddressWithoutHiddenAlignment()
    {
        using var config = new ScriptedConfig();
        var data = new byte[] { 1, 2, 3, 4 };
        AddWrite(config, FlashStart + 1, data);

        Assert.True(STBootloader.Write(config, FlashStart + 1, data));
        config.AssertComplete();
    }

    [Fact]
    public void Write_RetriesTheCompleteBlockTransaction()
    {
        using var config = new ScriptedConfig(retries: 1);
        config.Add(Command(STBootloader.Command.WriteMemory));
        var data = new byte[] { 1, 2, 3, 4 };
        AddWrite(config, FlashStart, data);

        Assert.True(STBootloader.Write(config, FlashStart, data));
        config.AssertComplete();
    }

    [Fact]
    public void Write_RejectsNonProtocolLengthBeforeCommunication()
    {
        using var config = new ScriptedConfig();

        Assert.Throws<ArgumentException>(() => STBootloader.Write(config, FlashStart, [1, 2, 3]));
        config.AssertComplete();
    }

    [Fact]
    public void EmptyOperationsSucceedWithoutCommunication()
    {
        using var config = new ScriptedConfig();
        var device = CreateDevice([0x400]);

        Assert.True(STBootloader.Read(config, FlashStart, 0, out var data));
        Assert.Empty(data);
        Assert.True(STBootloader.Write(config, FlashStart, []));
        Assert.True(STBootloader.Erase(config, device, false, FlashStart, 0));
        config.AssertComplete();
    }

    [Fact]
    public void Erase_RejectsOutOfFlashRangeBeforeCommunication()
    {
        using var config = new ScriptedConfig();
        var device = CreateDevice([16], 16);

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            STBootloader.Erase(config, device, false, FlashStart + 15, 2));
        config.AssertComplete();
    }

    [Fact]
    public void ExtendedErase_SelectsBothF407SectorsAcrossBoundary()
    {
        using var config = new ScriptedConfig();
        var device = CreateDevice([0x4000, 0x4000, 0x4000, 0x4000, 0x10000, 0x20000], 0x100000);
        config.Add(Command(STBootloader.Command.ExtendedErase), [Ack]);
        config.Add(Frame([0x00, 0x01, 0x00, 0x03, 0x00, 0x04]), [Ack]);

        Assert.True(STBootloader.Erase(config, device, true, FlashStart + 0xFFFF, 2));
        config.AssertComplete();
    }

    [Fact]
    public void SafeErase_StopsWhenBoundaryBackupFails()
    {
        using var config = new ScriptedConfig();
        var device = CreateDevice([16], 16);
        config.Add(Command(STBootloader.Command.ReadMemory));

        Assert.False(STBootloader.SafeErase(config, device, false, FlashStart + 4, 4));
        config.AssertComplete();
    }

    [Fact]
    public void SafeErase_ReturnsFalseWhenRestoredDataDoesNotVerify()
    {
        using var config = new ScriptedConfig();
        var device = CreateDevice([16], 16);
        var before = new byte[] { 1, 2, 3, 4 };
        var after = new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 };
        AddRead(config, FlashStart, before);
        AddRead(config, FlashStart + 8, after);
        config.Add(Command(STBootloader.Command.Erase), [Ack]);
        config.Add(Frame([0x00, 0x00]), [Ack]);
        AddWrite(config, FlashStart, before);
        AddRead(config, FlashStart, [1, 2, 3, 0xFF]);

        Assert.False(STBootloader.SafeErase(config, device, false, FlashStart + 4, 4));
        config.AssertComplete();
    }

    [Fact]
    public void SafeErase_RestoresAndVerifiesBothBoundaries()
    {
        using var config = new ScriptedConfig();
        var device = CreateDevice([16], 16);
        var before = new byte[] { 1, 2, 3, 4 };
        var after = new byte[] { 9, 10, 11, 12, 13, 14, 15, 16 };
        AddRead(config, FlashStart, before);
        AddRead(config, FlashStart + 8, after);
        config.Add(Command(STBootloader.Command.Erase), [Ack]);
        config.Add(Frame([0x00, 0x00]), [Ack]);
        AddWrite(config, FlashStart, before);
        AddRead(config, FlashStart, before);
        AddWrite(config, FlashStart + 8, after);
        AddRead(config, FlashStart + 8, after);

        Assert.True(STBootloader.SafeErase(config, device, false, FlashStart + 4, 4));
        config.AssertComplete();
    }

    [Fact]
    public void ResettingProtectionCommandsAreNotRetried()
    {
        using var failedConfig = new ScriptedConfig(retries: 3);
        failedConfig.Add(Command(STBootloader.Command.WriteUnprotect));
        Assert.False(STBootloader.WriteUnprotectAndReset(failedConfig));
        failedConfig.AssertComplete();

        using var successfulConfig = new ScriptedConfig(retries: 3);
        successfulConfig.Add(Command(STBootloader.Command.WriteUnprotect), [Ack], [Ack]);
        Assert.True(STBootloader.WriteUnprotectAndReset(successfulConfig));
        successfulConfig.AssertComplete();
    }

    static STBootloader.DeviceInfo CreateDevice(int[] pageSizes, int flashSize = 0x20000)
    => new(
        0x413,
        "Test device",
        0x20000000,
        0x20010000,
        FlashStart,
        FlashStart + flashSize,
        1,
        pageSizes,
        0,
        0,
        0,
        0,
        0,
        FlashSizeAddress: 0);

    static void AddRead(ScriptedConfig config, int address, byte[] data)
    {
        config.Add(Command(STBootloader.Command.ReadMemory), [Ack]);
        config.Add(Frame(Address(address)), [Ack]);
        config.Add(Frame([(byte)(data.Length - 1)], 0xFF), [[.. new[] { Ack }.Concat(data)]]);
    }

    static void AddWrite(ScriptedConfig config, int address, byte[] data)
    {
        config.Add(Command(STBootloader.Command.WriteMemory), [Ack]);
        config.Add(Frame(Address(address)), [Ack]);
        config.Add(Frame([.. new[] { (byte)(data.Length - 1) }.Concat(data)]), [Ack]);
    }

    static byte[] Command(STBootloader.Command command)
    => [(byte)command, (byte)((byte)command ^ 0xFF)];

    static byte[] Address(int address)
    =>
    [
        (byte)(address >> 24),
        (byte)(address >> 16),
        (byte)(address >> 8),
        (byte)address,
    ];

    static byte[] Frame(byte[] data, byte initialValue = 0)
    {
        var checksum = data.Aggregate(initialValue, (value, item) => (byte)(value ^ item));
        return [.. data, (byte)checksum];
    }

    sealed class ScriptedConfig : ISendAndGetAnswerConfig
    {
        sealed class Exchange
        {
            public byte[] Request { get; }
            public Queue<byte[]> Responses { get; }

            public Exchange(byte[] request, IEnumerable<byte[]> responses)
            {
                Request = request;
                Responses = new Queue<byte[]>(responses);
            }
        }

        readonly Queue<Exchange> exchanges = new();
        Queue<byte[]> responses = new();

        public int? AddrCode { get; set; }
        public int Retries { get; }
        public int Timeout => 10;
        public bool IsStream => true;
        public int FixedTimeoutReadCount { get; private set; }

        public ScriptedConfig(int retries = 0) => Retries = retries;

        public void Add(byte[] request, params byte[][] responseParts)
        => exchanges.Enqueue(new Exchange(request, responseParts));

        public bool Send(byte[] data)
        {
            Assert.NotEmpty(exchanges);
            var exchange = exchanges.Dequeue();
            Assert.Equal(exchange.Request, data);
            responses = exchange.Responses;
            return true;
        }

        public bool TryGetAnswer(out byte[] data, int extTimeout = 0)
        => TryGetAnswerCore(out data);

        public bool TryGetAnswerWithFixedTimeout(out byte[] data, int timeout = 0)
        {
            FixedTimeoutReadCount++;
            return TryGetAnswerCore(out data);
        }

        bool TryGetAnswerCore(out byte[] data)
        {
            if (responses.Count == 0)
            {
                data = [];
                return false;
            }
            data = responses.Dequeue();
            return true;
        }

        public void ClearReceiveBuffer() => responses.Clear();

        public void AssertComplete()
        {
            Assert.Empty(exchanges);
            Assert.Empty(responses);
        }

        public void Dispose()
        {
        }
    }
}
