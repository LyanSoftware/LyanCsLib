using System.Runtime.InteropServices;
using Lytec.Common.Data;
using Newtonsoft.Json;

namespace Lytec.Common.Number;

[Serializable]
[Endian(Endian.Little)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct WORDBool : IEquatable<WORDBool>
{
    [JsonProperty(nameof(Value))]
    public bool SerializedValue { get => Value != 0; set => Value = (ushort)(value ? 1 : 0); }

    [JsonIgnore]
    public ushort Value { get; set; }

    public WORDBool(bool v) : this() => SerializedValue = v;

    public static implicit operator bool(WORDBool v) => v.SerializedValue;
    public static implicit operator WORDBool(bool v) => new WORDBool(v);

    public override string ToString() => ((bool)this).ToString();

    public static bool operator ==(WORDBool left, WORDBool right) => left.Equals(right);
    public static bool operator !=(WORDBool left, WORDBool right) => !(left == right);
    public static bool operator !(WORDBool v) => v.Value == 0;
    public override bool Equals(object? obj) => obj is WORDBool v && Equals(v);
    public bool Equals(WORDBool other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
