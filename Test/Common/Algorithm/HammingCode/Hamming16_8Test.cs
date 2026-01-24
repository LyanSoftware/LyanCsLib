using Lytec.Common.Algorithm.HammingCode;

namespace Test;

using static Hamming16_8;

public class Hamming16_8Test
{
    [Fact]
    public static void FullSpaceTest()
    {
        // 无错误
        for (var data = 0; data < 0x100; data++)
        {
            Assert.True(Decode(Encode((byte)data), out var decoded) && decoded == data);
        }
    }

    [Fact]
    public static void OneBitErrTest()
    {
        // 1bit 纠错
        for (var data = 0; data < 0x100; data++)
        {
            var d = Encode((byte)data);
            for (var i = 0; i < 16; i++)
            {
                var errd = (ushort)(d ^ (1 << i));
                if (Decode(errd, out var decoded))
                    Assert.True(decoded == data);
            }
        }
    }

    [Fact]
    public static void FullSpaceDecodeTest()
    {
        // 全空间搜索
        var total = 0;
        var failCount = 0;
        var missCount = 0;
        for (var data = 0; data < 0x100; data++)
        {
            for (var test = 0; test < 0x10000; test++)
            {
                total++;
                if (Decode((ushort)test, out var decoded))
                {
                    if (decoded != data)
                        missCount++;
                }
                else failCount++;
            }
        }
        // 以下为理论性能
        Assert.True(missCount == 0xFF000);
        Assert.True(failCount == 0xF00000);
        Assert.True(total - failCount - missCount == 0x1000);
    }
}
