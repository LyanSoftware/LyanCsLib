using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common.Algorithm.HammingCode;

/// <summary>
/// 汉明码(15,11)扩展一位奇偶校验位,数据位只使用8位,剩余3位使用CRC-3填充的实现
/// </summary>
public static class Hamming16_8
{
    static readonly byte[] DataBitPos = GetDataBitPos(16).Take(11).Select(i => (byte)i).ToArray();

    /// <summary>
    /// 计算可用数据位
    /// </summary>
    /// <param name="len"></param>
    /// <returns></returns>
    public static IEnumerable<int> GetDataBitPos(int len)
    {
        var p = 1;
        for (var i = 1; i < len; i++)
        {
            if (i == p)
                p <<= 1;
            else yield return i;
        }
    }

    /// <summary>
    /// 将8bit映射到3bit的CRC3算法, 填入汉明(16,11)的未使用位中, 用于额外的错误检测
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static byte CRC3(byte data)
    {
        var crc = 0;
        var poly = 0x03;  // 多项式

        for (int i = 0; i < 8; i++)
        {
            crc ^= (data >> i) & 1;
            if ((crc & 0x04) != 0)
            {
                // 最高位为1
                crc = (crc << 1) ^ poly;
            }
            else
            {
                crc <<= 1;
            }
        }
        return (byte)(crc & 0x07);
    }

    /// <summary>
    /// 编码
    /// </summary>
    /// <param name="data"></param>
    /// <returns></returns>
    public static ushort Encode(byte data)
    {
        var input = data | (CRC3(data) << 8);
        var v = 0;
        for (var i = 0; i < 11; i++)
        {
            if (((input >> i) & 1) != 0)
            {
                v |= 1 << DataBitPos[i];
                for (var j = 0; j < 4; j++)
                    if ((DataBitPos[i] & (1 << j)) != 0)
                        v ^= 1 << (1 << j);
            }
        }
        for (var i = 1; i < 16; i++)
            if (((v >> i) & 1) != 0)
                v ^= 1;
        return (ushort)v;
    }

    public static bool Decode(ushort receivedData, out byte data)
    {
        int recv = receivedData;
        var s1 = 0; // 汉明码算出的错误位
        var s2 = 0; // 第0位的奇偶校验
        for (var i = 0; i < 16; i++)
        {
            if (((recv >> i) & 1) != 0)
            {
                if (i != 0)
                    s1 ^= i;
                s2 ^= 1;
            }
        }

        var ok = false;

        if (s1 == 0 && s2 == 0)
        {
            // 无错误
            ok = true;
        }
        else if (s1 != 0 && s2 != 0)
        {
            // 有1bit错误
            recv ^= 1 << s1;
            ok = true;
        }
        if (ok)
        {
            var d = 0;
            var crc3 = 0;
            for (var i = 0; i < 11; i++)
            {
                if (i < 8)
                    d |= ((recv >> DataBitPos[i]) & 1) << i;
                else crc3 |= ((recv >> DataBitPos[i]) & 1) << (i - 8);
            }
            data = (byte)d;
            // 检查CRC
            return CRC3(data) == crc3;
        }
        else
        {
            data = 0;
            return false;
        }
    }
}
