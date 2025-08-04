using System.Runtime.InteropServices;
using Lytec.Common.Data;
using Newtonsoft.Json;

namespace Lytec.Common.Number;

[Serializable]
[Endian(Endian.Little)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct DWORDBool : IEquatable<DWORDBool>
{
    [JsonProperty(nameof(Value))]
    public bool SerializedValue { get => Value != 0; set => Value = value ? 1 : 0; }

    [JsonIgnore]
    public int Value { get; set; }

    public DWORDBool(bool v) : this() => SerializedValue = v;

    public static implicit operator bool(DWORDBool v) => v.SerializedValue;
    public static implicit operator DWORDBool(bool v) => new DWORDBool(v);

    public override string ToString() => ((bool)this).ToString();

    public static bool operator ==(DWORDBool left, DWORDBool right) => left.Equals(right);
    public static bool operator !=(DWORDBool left, DWORDBool right) => !(left == right);
    public static bool operator !(DWORDBool v) => v.Value == 0;
    public override bool Equals(object obj) => obj is DWORDBool v && Equals(v);
    public bool Equals(DWORDBool other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
}
