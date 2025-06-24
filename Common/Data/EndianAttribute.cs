namespace Lytec.Common.Data
{
    /// <summary>
    /// 标记数据的字节序
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Enum, Inherited = true)]
    public class EndianAttribute : Attribute
    {
        public Endian Endian { get; }

        public EndianAttribute(Endian endian) => Endian = endian;
    }

    public class BigEndianAttribute : EndianAttribute
    {
        public BigEndianAttribute() : base(Endian.Big) { }
    }

    public class LittleEndianAttribute : EndianAttribute
    {
        public LittleEndianAttribute() : base(Endian.Little) { }
    }

}
