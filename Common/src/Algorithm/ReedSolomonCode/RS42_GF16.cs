using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Lytec.Common.Algorithm.ReedSolomonCode.RS42_GF16
{
    /// <summary>
    /// <br/>
    /// <br/>里德-所罗门码（Reed-solomon codes，简称里所码或RS codes）
    /// <br/>
    /// <br/>
    /// <br/>RS(4,2)码的实现
    /// <br/>
    /// <br/>参数:
    /// <br/>	码长(n)			: 4 符号(16 bits)
    /// <br/>	信息符号数(k)	: 2 符号( 8 bits)
    /// <br/>	校验符号数(n-k)	: 2 符号( 8 bits) (必须在总共2字符内)
    /// <br/>	纠错能力(t)		: 1 符号( 4 bits) (必须在同一字符内)
    /// <br/>	有限域			: GF(2^4)
    /// <br/>	本原多项式		: x^4 + x + 1 ( 0b10011 / 0x13 )
    /// <br/>	生成矩阵		: G = [I|P], P = [[C,9],[4, F]]
    /// <br/>
    /// <br/>操作数仅限4bit有效
    /// <br/>
    /// <br/>理论能力:
    /// <br/>	全空间		: 0x1000000
    /// <br/>	正确解码	: 256*61=15616	(  0.093% )
    /// <br/>	解码失败	: 12779520		( 76.17%  )
    /// <br/>	错误解码	: 3982080		( 23.74%  )
    /// <br/>	实际通讯中, 大部分情况下应该是无错误或单错误, 小概率双错误
    /// <br/>	不会随机错特别多位, 错误解码的几率应该远低于23.74%
    /// <br/>
    /// </summary>
    public static class RS42
    {
        /// <summary>
        /// 至少uint4, 快速计算用int32
        /// </summary>
        public struct Element
        {
            public int Value { get; set; }

            public static implicit operator int(Element v) => v.Value;
            public static implicit operator Element(int v) => new() { Value = v };
        }

        /*** 极简 伽罗华域GF(2 ^ 4) 实现 ***/
        public static class GF16
        {
            /// <summary>
            /// 是否使用异常机制（是否抛出异常）
            /// </summary>
            public static bool ThrowException { get; set; } = true;

            /// <summary>
            /// 指数表
            /// </summary>
            public static readonly Element[] Exp = {
                0x01,0x02,0x04,0x08,0x03,0x06,0x0c,0x0b,0x05,0x0a,0x07,0x0e,0x0f,0x0d,0x09,0x01,
                0x02,0x04,0x08,0x03,0x06,0x0c,0x0b,0x05,0x0a,0x07,0x0e,0x0f,0x0d,0x09,0x01,0x02,
            };

            /// <summary>
            /// 对数表
            /// </summary>
            public static readonly Element[] Log = {
                0x00,0x00,0x01,0x04,0x02,0x08,0x05,0x0a,0x03,0x0e,0x09,0x07,0x06,0x0d,0x0b,0x0c,
            };

            // 生成指数表与对数表
            static void gf16_gen_table()
            {
                int x = 1;
                for (int i = 0; i < 15; i++)
                {
                    Exp[i] = x; Log[x] = i;
                    x = (x << 1) ^ (((x & 8) != 0) ? 0b10011 : 0);
                }
                // 扩展指数表
                for (int i = 15; i < 32; i++) Exp[i] = Exp[i - 15];
            }

            /// <summary>
            /// <br/>加法, 等价于异或
            /// <br/>减法也等价于异或
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static Element Add(Element a, Element b) => a ^ b;

            /// <summary>
            /// <br/>乘法
            /// </summary>
            /// <param name="a"></param>
            /// <param name="b"></param>
            /// <returns></returns>
            public static Element Mul(Element a, Element b) => (a == 0 || b == 0) ? 0 : Exp[(Log[a] + Log[b]) % 15];

            /// <summary>
            /// <br/>除法(未使用)
            /// <br/>b为0, 且<see cref="ThrowException"/>为true时将抛出<see cref="DivideByZeroException"/>
            /// </summary>
            /// <param name="a">被除数</param>
            /// <param name="b">除数</param>
            /// <returns></returns>
            /// <exception cref="DivideByZeroException"/>
            public static Element Div(Element a, Element b)
            {
                if (a == 0)
                    return 0;
                if (b == 0)
                {
                    if (ThrowException)
                        throw new DivideByZeroException();
                    else return 0;
                }
                return Exp[(Log[a] + Log[b]) % 15];
            }

            /// <summary>
            /// <br/>乘方(幂运算)
            /// </summary>
            /// <param name="a">底数</param>
            /// <param name="n">指数</param>
            /// <returns></returns>
            public static Element Pow(Element a, int n)
            {
                if (n == 0)
                    return 1;
                if (a == 0)
                    return 0;
                return Exp[Log[a] * n % 15];
            }

            /// <summary>
            /// <br/>逆元(未使用)
            /// <br/>a为0, 且<see cref="ThrowException"/>为true时将抛出<see cref="DivideByZeroException"/>
            /// </summary>
            /// <param name="a"></param>
            /// <returns></returns>
            /// <exception cref="DivideByZeroException"/>
            public static Element Inv(Element a)
            {
                if (a == 0)
                {
                    if (ThrowException)
                        throw new DivideByZeroException();
                    else return 0;
                }
                return Exp[(15 - Log[a]) % 15];
            }
        }

        /*** 极简 码长16bit的RS(4,2) 实现 ***/

        /// <summary>
        /// <br/>生成矩阵
        /// <br/>等价于生成多项式 g(x) = (x + α)(x + α²) = x² + 6x + 8
        /// </summary>
        public static readonly Element[,] GenerateMatrix = new Element[,]
        {
            {0x01,0x00,0x0C,0x09},
            {0x00,0x01,0x04,0x0F},
        };

        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static ushort Encode(byte data)
        {
            var cw = new Element[4];
            var msg = new Element[] { data & 0xf, data >> 4 };
            for (int j = 0; j < 4; j++)
                for (int i = 0; i < 2; i++)
                    cw[j] = GF16.Add(cw[j], GF16.Mul(msg[i], GenerateMatrix[i, j]));
            // 丢弃无效位缩短到16bit
            return (ushort)((cw[0] & 0xF)
                         | ((cw[1] & 0xF) << 4)
                         | ((cw[2] & 0xF) << 8)
                         | ((cw[3] & 0xF) << 12)
                   );
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="receivedData"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        public static bool Decode(ushort receivedData, out byte data)
        {
            // 扩展为数组方便计算
            var recv = new Element[]
            {
                 receivedData        & 0xF,
                (receivedData >>  4) & 0xF,
                (receivedData >>  8) & 0xF,
                (receivedData >> 12) & 0xF,
            };

            // 计算伴随式 S1, S2
            Element s1 = 0, s2 = 0;
            for (int i = 0; i < 4; i++)
            {
                s1 = GF16.Add(s1, GF16.Mul(recv[i], GF16.Pow(2, i)));
                s2 = GF16.Add(s2, GF16.Mul(recv[i], GF16.Pow(2, 2 * i)));
            }

            if (s1 == 0 && s2 == 0) // 无错误
            {
                data = (byte)(recv[0] | (recv[1] << 4));
                return true;
            }

            // 单错误纠正
            for (int i = 0; i < 4; i++)
            {
                //uint8_t alpha_i = gf16_pow(2, i);
                //uint8_t e = gf16_mul(s1, gf16_pow(alpha_i, 14)); // s1/α^i
                var e = GF16.Mul(s1, GF16.Pow(GF16.Pow(2, i), 14)); // s1/α^i
                if (GF16.Mul(e, GF16.Pow(2, 2 * i)) == s2)
                {
                    recv[i] = GF16.Add(recv[i], e);
                    data = (byte)(recv[0] | (recv[1] << 4));
                    return true;
                }
            }

            data = 0;
            // 无法纠正
            return false;
        }

        /// <summary>
        /// 测试
        /// </summary>
        public static void Test()
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
                        Debug.Assert(test(data, i, v));
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
            Debug.Assert(missCount == 3982080);
            Debug.Assert(failCount == 12779520);
            Debug.Assert(total - failCount - missCount == 15616);
        }
    }
}
