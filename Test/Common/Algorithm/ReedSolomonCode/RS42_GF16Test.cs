using Lytec.Common.Algorithm.ReedSolomonCode.RS42_GF16;

namespace Test;

using static RS42;
public class RS42_GF16Test
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
    public static void OneSmybolErrTest()
    {
        // 单符号错误
        static bool test(int input, int erri, int errv)
        {
            var v = (int)Encode((byte)input);
            v &= ~(0xF << (erri * 4));
            v |= (errv & 0xF) << (erri * 4);
            return Decode((ushort)v, out var d) && d == input;
        }
        for (var data = 0; data < 0x100; data++)
            for (var i = 0; i < 4; i++)
                for (var v = 0; v < 16; v++)
                    Assert.True(test(data, i, v));
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
            for (var testd = 0; testd < 0x10000; testd++)
            {
                total++;
                if (Decode((ushort)testd, out var d))
                {
                    if (d != data)
                        missCount++;
                }
                else failCount++;
            }
        }
        // 以下为理论性能
        Assert.True(missCount == 3982080);
        Assert.True(failCount == 12779520);
        Assert.True(total - failCount - missCount == 15616);
    }
}
