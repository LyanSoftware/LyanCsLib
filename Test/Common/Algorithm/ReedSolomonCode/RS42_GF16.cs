using Lytec.Common.Algorithm.ReedSolomonCode.RS42_GF16;

namespace Test
{
    using static RS42;
    public class RS42_GF16
    {
        [Fact]
        public static void Test1()
        {
            static bool test(int input, int erri, int errv)
            {
                var v = (int)Encode((byte)input);
                v &= ~(0xF << (erri * 4));
                v |= (errv & 0xF) << (erri * 4);
                return Decode((ushort)v, out var d) && d == input;
            }
            for (var data = 0; data < 256; data++)
                for (var i = 0; i < 4; i++)
                    for (var v = 0; v < 16; v++)
                        Assert.True(test(data, i, v));
            var total = 0;
            var failCount = 0;
            var missCount = 0;
            for (var data = 0; data < 256; data++)
            {
                for (var testd = 0; testd < 65536; testd++)
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
            Assert.True(missCount == 3982080);
            Assert.True(failCount == 12779520);
            Assert.True(total - failCount - missCount == 15616);
        }
    }
}
