# 二进制序列化使用说明

## 1. 适用范围与当前状态

本方案统一描述两类二进制数据：

- 编译期可以确定线路大小的固定长度对象，由源代码生成器生成编解码代码。
- 帧大小或帧类型需要在运行期决定的协议，由协议实现者编写无状态编解码器，并复用通用流解码基础设施。

公共运行时和 `Common.Generators` 均以 `netstandard2.0` 为最低目标框架，可以由 C# 14 项目使用。固定长度源生成器、运行时抽象和通用流解码器均已实现。

核心命名空间为 `Lytec.Common.Serialization`。字节序类型和标记位于 `Lytec.Common.Data`。

## 2. 共同概念

### 2.1 状态值

所有正常的数据路径使用 `System.Buffers.OperationStatus`：

- `Done`：操作完成并产生完整结果。
- `NeedMoreData`：连续流中的候选帧尚未接收完整。
- `DestinationTooSmall`：序列化目标缓冲区不足。
- `InvalidData`：输入帧或待序列化值不符合协议。

畸形线路数据属于正常解析结果，不抛异常。空参数、非法配置、接口实现违反约定、错误的生命周期调用等编程错误才抛异常。除 `ArgumentNullException.ThrowIfNull`、`ArgumentOutOfRangeException.ThrowIf…` 一类简单参数检查外，库和生成代码主动创建的契约异常使用 `Exception.Localize(...)` 附加本地化键、默认中文消息和格式化参数；调用方可通过 `GetLocalizedMessage()` 取得附加信息。

### 2.2 有限长度边界

所有格式实现 `IBinaryFormat`：

```csharp
public interface IBinaryFormat
{
    int MinimumFrameLength { get; }
    int MaximumFrameLength { get; }
}
```

两个值都包含边界。`MinimumFrameLength` 必须大于零；`MaximumFrameLength` 不得小于最小值，并且必须是实际的有限上限，不能用 `int.MaxValue` 表示“无限”。这既是协议约束，也是流解码器的内存上限。

### 2.3 所有权和线程安全

- 编解码器应当是不可变、无状态且可并发复用的对象。
- `IBinaryStreamDecoder<T>` 保存接收状态，不是线程安全对象；每条独立字节流使用一个实例。
- 成功解析产生的结果拥有自己的数据。公共核心不提供借用内部缓冲区的零复制结果。
- 默认流解码器使用普通托管数组，不要求 `IDisposable`，也不使用 `ArrayPool<byte>`。

## 3. 固定长度对象

### 3.1 类型声明

使用项目必须把 `Common.Generators` 作为 Analyzer 引用。例如仓库内项目可写为：

```xml
<ProjectReference Include="..\Common.Generators\Lytec.Common.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

参与生成的类型必须：

1. 是非泛型的 `partial class` 或 `partial struct`；
2. 实现 `IBinarySerializable`；
3. 有效布局是 `LayoutKind.Sequential` 或 `LayoutKind.Explicit`。

`IBinarySerializable` 是触发生成的标记接口。`[BinaryObject]` 只在需要显式选择成员包含规则时使用；省略时等同于 `BinaryMemberInclusion.OptOut`。

有效布局按 CLR 和语言规则确定；结构体的默认顺序布局可以直接使用，类的自动布局则不受支持。需要 `Pack`、`Size`、`CharSet` 或显式偏移时应写出 `[StructLayout]`。

示意声明：

```csharp
using System.Runtime.InteropServices;
using Lytec.Common.Data;
using Lytec.Common.Serialization;

[BinaryObject]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[Endian(Endian.Big)]
public partial struct DeviceHeader : IBinarySerializable
{
    public ushort Command;
    public uint Sequence { get; }

    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 6, ArraySubType = UnmanagedType.U1)]
    public byte[] Address { get; }
}
```

生成器不能用于泛型类型、开放泛型成员、`Nullable<T>` 或带可空语义的可序列化成员。遇到这些形式时必须报告编译错误，而不是在运行期猜测布局。

### 3.2 参与序列化的成员

只有实际存储数据的实例成员参与布局：

- 实例字段参与，包括 `private`、`protected`、`internal`、`readonly` 字段。
- 自动实现属性参与，包括非公开、仅 `get`、`init` 或私有 setter 的自动属性。
- 计算属性、显式实现但没有独立存储的属性、静态成员和常量不参与。
- 属性必须确实是自动实现属性；生成器不得把普通计算属性当作存储成员。

可访问性不影响二进制布局。生成代码位于同一个 partial 类型中，因此可以读取非公开成员。反序列化所需的只读赋值由生成的专用构造路径完成；基类与派生类分别生成自己的构造片段并正确串联。若用户声明使生成构造路径无法唯一、安全地建立对象，生成器必须报告错误。

对于 `LayoutKind.Sequential`，一个类型自身的全部存储成员必须来自同一个 partial 声明。该声明可以由用户或其他生成器产生，并不强制要求它一定写在用户源码中。这个限制用于保证成员顺序明确。

对于 `LayoutKind.Explicit`，带 `[FieldOffset]` 的成员可以分散在多个 partial 声明中，因为偏移已经显式确定。

### 3.3 成员包含规则

`BinaryObjectAttribute.MemberInclusion` 有两种模式：

- `OptOut`：默认包含所有具有实际存储且能够表示的成员；不参与的成员必须使用 `[BinaryIgnore]` 显式排除。这是默认模式。
- `OptIn`：默认不包含未标记成员；成员必须使用 `[BinaryMember]`、`[MarshalAs]` 或生成器认可的二进制表示标记显式包含。

`LayoutKind.Explicit` 不支持 `OptIn`。显式布局的全部线路位置必须能够从 `[FieldOffset]`、成员大小和 `StructLayout.Size` 一致地推导；需要保留但不映射到成员的区域由填充字节表示。要排除派生类型新增的运行时状态，使用 `[BinaryIgnore]`。

不得同时对同一成员应用互相冲突的包含和排除标记。含义不明确时必须报告编译错误。

### 3.4 支持的值类型

直接支持具有固定线路宽度的基元数值和以这些基元为基础类型的枚举。多字节整数和浮点数遵守字节序规则。`bool` 等线路宽度不能仅由 CLR 类型唯一确定的值，必须用 `[MarshalAs]` 指定明确的非托管表示。

不支持：

- `char` 和 `string`；
- 指针、函数指针和任何以地址为线路表示的成员；
- 动态长度数组、`LPArray`、`SafeArray` 和其他长度来自运行期元数据的形式；
- `CustomMarshaler` 及自定义封送器；
- 无法在编译期确定固定大小的对象；
- 泛型或可空成员。

文本应当显式选择 `Encoding`，把编码结果保存到定长 `byte[]` 中，并用不参与序列化的计算属性提供字符串视图。这样编码、终止符和填充规则都由协议代码明确控制。

### 3.5 `MarshalAs` 规则

生成器只接受能够得到固定大小、无指针线路表示的 `MarshalAs` 形式，并沿用 `MarshalAsAttribute` 的宽度、元素类型和 `SizeConst` 含义。任何不明确、互相冲突或不支持的组合都产生编译错误。

数组必须使用：

```csharp
[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16, ArraySubType = UnmanagedType.U1)]
public byte[] Data { get; }
```

规则如下：

- 必须是 `ByValArray`，并提供大于零的 `SizeConst`。
- 必须提供与数组元素相符的 `ArraySubType`；生成器不能依赖平台默认值猜测线路宽度。
- 建议在对象创建时把数组初始化为 `Length == SizeConst`，以后只修改元素，不替换数组引用。
- 序列化时 `Length > SizeConst` 直接抛出附带本地化信息的异常，禁止静默截断。
- 较短数组按已经存在的元素编码，剩余固定槽位写入零值；空引用不是有效的固定数组值。
- 反序列化时为新对象建立 `SizeConst` 长度的数组；对于已经由构造路径建立的数组，填充元素而不是替换整个数组引用，因此只读数组属性也能工作。

实现 `IBinarySerializable` 的嵌套值类型或引用类型会作为内联对象展开，不会写入对象引用。嵌套数组同样可以通过 `ByValArray` 和 `ArraySubType` 展开元素；元素的 CLR 类型必须实现相同标记接口，且元素固定大小在编译期可知。

引用类型元素允许在运行期出现派生实例，但实际派生类型的固定大小必须与数组声明元素类型的固定大小相同；不相同则序列化失败并抛出附带本地化信息的异常。无需为了这个限制把所有类型声明为 `sealed`。派生类型中不属于线路布局的状态应使用 `[BinaryIgnore]`。

`ByValTStr`、`char` 和 `string` 不在支持范围内，因此不存在“终止空字符是否占用 `SizeConst`”或“遇到空字符后是否必须继续接收完整槽位”的隐式选项。需要这种协议时使用 `byte[]` 和计算属性明确实现：`SizeConst` 始终表示真实线路字节数，流解码仍须收满这些字节。

### 3.6 `StructLayout` 语义

生成布局遵守标准库的 `StructLayoutAttribute` 语义：

- `Sequential` 按声明顺序布局，并遵守 `Pack`、成员自然对齐、尾部对齐和显式 `Size`。
- 继承成员始终位于派生类型自身成员之前。
- `Explicit` 使用 `[FieldOffset]` 和显式 `Size`，允许标准意义上的重叠布局。
- 未映射到成员的间隙和尾部空间作为线路填充字节处理。
- `Auto` 没有稳定线路布局，必须报告错误。

复杂的 `Explicit` 布局不支持再通过继承继续扩展。此类协议应使用组合：把显式布局部分作为一个独立固定对象，外围类型顺序包含它。单一显式布局中若重叠成员对字节序或表示提出互相冲突的解释，也必须报告错误，不能依赖“最后写入者”。

显式设置 `StructLayout.CharSet` 时，生成器发出可被 `#pragma warning disable` 或项目 `NoWarn` 明确屏蔽的 Warning，说明 `CharSet` 不影响本二进制序列化行为。因为 `char`、`string` 和 `ByValTStr` 均不受支持，生成器不会对 `CharSet.Auto`、`None`、`Ansi` 或 `Unicode` 赋予额外线路含义。

### 3.7 字节序

使用现有 `Endian` 和 `EndianAttribute`，不扩展 `EndianAttribute`。有效优先级从高到低为：

1. 成员上的 `[Endian]`；
2. 成员类型或枚举类型上的 `[Endian]`；
3. 当前声明类型上可继承的 `[Endian]`；
4. 调用序列化或反序列化 API 时传入的默认字节序；
5. 未指定时使用当前平台本机字节序。

字节序只作用于具有多个字节的数值表示，并递归应用于数组元素和内联对象。单字节值、填充区域和原始 `byte[]` 的字节顺序不反转。

协议代码应优先显式声明线路字节序，避免依赖本机字节序。

### 3.8 继承与运行期派生实例

布局总是先处理基类，随后处理当前类型。派生类型可以增加参与序列化的成员，因此它自己的固定大小可以大于基类。

当某个成员的声明类型是基类引用时，该槽位大小由声明类型决定。实际值可以是派生实例，但运行期必须检查：

```text
实际派生类型的固定大小 == 声明类型的固定大小
```

不相等时抛出附带本地化信息的异常，防止后续成员被错位。若派生类只是增加运行期状态，应对这些成员使用 `[BinaryIgnore]`，使派生类保持相同固定大小。

复杂显式布局不要与继承组合；使用内联组合对象。

### 3.9 生成 API

生成器为每个可构造的有效类型提供以下公开入口：

```csharp
public static IFixedBinaryCodec<T> BinaryCodec { get; }

public static IFixedBinaryCodec<T> GetBinaryCodec(
    Endian? endian = null);

public static OperationStatus TryDeserialize(
    ReadOnlySpan<byte> source,
    out T value,
    Endian? endian = null);
```

`BinaryCodec` 是使用 Attribute 和本机默认端序的不可变单例。`GetBinaryCodec` 为 `null`、小端和大端分别复用不可变实例，适合被可变长度协议组合。

此外还生成以下实例便捷能力：

- `SerializedSize`：当前声明类型的固定线路大小。
- `Serialize(Endian? endian = null)`：返回精确长度的新 `byte[]`。
- `TrySerialize(Span<byte>, out int written, Endian? endian = null)`：写入调用方缓冲区。
- `TryDeserialize(...)`：唯一的直接反序列化便捷 API；不生成会抛出数据格式异常的 `Deserialize`。
- 创建 `IFixedBinaryStreamDecoder<T>`，用于分批接收固定长度对象。

`TrySerialize` 在失败时必须令 `written == 0`，并保持目标缓冲区完全不变。`TryDeserialize` 对畸形数据返回 `InvalidData`；目标不足或输入不足按调用场景返回相应状态。类型化 codec 用于需要返回 `T` 的统一调用点，避免旧式反序列化工厂。

直接 `TryDeserialize` 要求 `source.Length == SerializedSize`，多一个或少一个字节都返回 `InvalidData`。从较大帧前缀读取固定对象时使用 `BinaryCodec.Parse`，并通过 `BinaryParseResult.Consumed` 取得明确的消费长度。

若通过基类声明的固定 codec 序列化派生实例，它按声明类型固定大小进行运行期检查并拒绝不同大小的实例。由实例自身提供的便捷方法可以虚分派到实际派生类型的生成实现。

抽象类型只生成实例序列化、固定大小和供派生类串联的受保护解码构造路径；不生成 `BinaryCodec`、`GetBinaryCodec` 或 `TryDeserialize`。抽象类型不能作为内联成员或定长数组的声明元素类型。

### 3.10 固定长度增量解码和临时预览

`IFixedBinaryStreamDecoder<T>` 在通用流解码器之上提供：

```csharp
int RemainingLength { get; }

OperationStatus TryGetTemporary(
    TryFillMissingBytes fillMissing,
    out T value);

bool TryDiscardOldest(int count);
```

- 可以通过 `Append(byte, out T)` 每次追加一个字节，也可以通过 `Append(ReadOnlySpan<byte>, out T)` 一次追加多个字节。
- `BufferedLength` 是已经接收并保留的有效字节数，`RemainingLength` 是完成固定对象仍需的字节数。
- `TryGetTemporary` 不改变解码器状态。它复制已接收前缀，并调用 `fillMissing(offset, destination)` 填充所有尚未接收的槽位，再尝试产生临时对象。委托返回 `false` 时预览失败。该能力可用于接收部分头部后提前验证候选帧。
- `TryDiscardOldest(count)` 只能丢弃当前缓冲区最旧的 `count` 个字节；不能删除中间或尾部区域。参数越界时返回 `false`，成功时保留剩余顺序。
- 缓冲区收满但内容无效时，解码器保留整帧、返回 `InvalidData` 并进入 `Faulted`。此时可以调用 `TryDiscardOldest` 手动删除前缀；成功后按剩余长度恢复为 `Receiving` 或 `Ready`。
- `fillMissing` 返回 `false` 时 `TryGetTemporary` 返回 `NeedMoreData`；填充成功但临时内容无效时返回 `InvalidData`。
- 正常完成后产生的结果不借用内部缓冲区。

### 3.11 基元类型便捷 API

`PrimitiveBinarySerializationExtensions` 为生成器支持的固定宽度基元类型提供不依赖对象声明的便捷方法。支持 `sbyte`、`byte`、`short`、`ushort`、`int`、`uint`、`long`、`ulong`、`float`、`double` 和具有显式线路表示的 `bool`。

序列化使用按 CLR 类型重载的 `SerializeToBytes`，每次返回一个新分配的精确长度数组：

```csharp
byte[] command = ((ushort)0x1234).SerializeToBytes(Endian.Big);
byte[] enabled = true.SerializeToBytes(UnmanagedType.U1, Endian.Big);
```

这组便捷扩展不提供写入 `Span<byte>` 的无分配重载；需要复用目标缓冲区时，应使用固定对象 codec 或 `FixedBinaryPrimitives`。

反序列化方法按结果类型命名，避免泛型调用掩盖实际线路宽度：

```csharp
OperationStatus status = source.TryDeserializeToUInt16(
    out ushort command,
    Endian.Big);

OperationStatus boolStatus = boolSource.TryDeserializeToBoolean(
    UnmanagedType.Bool,
    out bool enabled,
    Endian.Big);
```

可用名称为 `TryDeserializeToSByte`、`TryDeserializeToByte`、`TryDeserializeToInt16`、`TryDeserializeToUInt16`、`TryDeserializeToInt32`、`TryDeserializeToUInt32`、`TryDeserializeToInt64`、`TryDeserializeToUInt64`、`TryDeserializeToSingle`、`TryDeserializeToDouble` 和 `TryDeserializeToBoolean`。接收方可以是 `ReadOnlySpan<byte>` 或非空 `byte[]`。

这些方法只解析一个完整、独立的基元值，要求输入长度与目标线路宽度完全相等。成功返回 `Done`；长度过短或过长均返回 `InvalidData`，不会返回 `NeedMoreData`，也不会自动消费较大输入的前缀。需要读取帧中的字段时，应先按协议明确切片。

`Endian?` 省略或传入 `null` 时使用本机字节序。端序参数在单字节类型上不会改变结果，但仍会验证枚举值是否合法。

`bool` 没有默认表示，序列化和反序列化都必须显式传入 `UnmanagedType`。支持 `I1`、`U1`、`I2`、`U2`、`I4`、`U4`、`Bool`；其他值抛出 `ArgumentOutOfRangeException`。序列化遵循相应表示的标准规范值：普通整数和 `Bool` 的 `true` 写为 `1`。反序列化遵循零为 `false`、任意非零为 `true`，因此使用四字节 `Bool` 或 `I4` 时也兼容 Pascal 的 `false = 0`、`true = 0xffffffff` 表示。

## 4. 可变长度协议

### 4.1 编解码器接口

可变长度协议由协议定义方实现：

```csharp
public interface IBinarySerializer<in T> : IBinaryFormat
{
    OperationStatus TryGetSerializedLength(T value, out int length);
    OperationStatus TrySerialize(T value, Span<byte> destination, out int written);
}

public interface IBinaryFrameParser<T> : IBinaryFormat
{
    BinaryParseResult Parse(
        ReadOnlySpan<byte> source,
        bool isFinalBlock,
        out T value);
}

public interface IBinaryDeserializer<T> : IBinaryFrameParser<T>
{
    BinaryResynchronizationMode ResynchronizationMode { get; }
    IBinaryStreamDecoder<T> CreateStreamDecoder();
}

public interface IBinaryCodec<T> :
    IBinarySerializer<T>,
    IBinaryDeserializer<T>
{
}
```

这不是全局 codec 注册表。调用方显式持有并传递所需 codec。`CreateStreamDecoder()` 只是从当前不可变配置建立有状态会话，不恢复旧式独立反序列化工厂抽象。

### 4.2 序列化约定

`TryGetSerializedLength` 必须先完整验证值并返回精确长度：

- 可表示时返回 `Done` 和位于声明边界内的长度。
- 不可表示时返回 `InvalidData` 和零。
- 不允许返回 `NeedMoreData` 或 `DestinationTooSmall`。

`TrySerialize` 的允许结果为：

- `Done`：完整写入，`written` 是精确长度。
- `DestinationTooSmall`：缓冲区不足，`written == 0`，目标不变。
- `InvalidData`：值无效，`written == 0`，目标不变。

因此实现者通常先调用相同的完整验证逻辑，再确认目标长度，最后一次性写入。不得先写一部分再报告失败。

便利扩展：

```csharp
byte[] data = codec.Serialize(value);

var writer = new ArrayBufferWriter<byte>();
codec.Serialize(value, writer);
```

两个便利 API 都先测量长度，并检查 codec 是否履行约定。值本身无效时，便利 API 抛出带本地化信息的编程层异常；需要无异常数据路径时直接调用 `TryGetSerializedLength` 和 `TrySerialize`。

### 4.3 无状态解析结果

解析器每次只从 `source[0]` 开始检查候选帧，不保存跨调用状态。`BinaryParseResult` 包含：

- `Status`：`Done`、`NeedMoreData` 或 `InvalidData`。
- `Consumed`：`Done` 时为完整帧长度；`InvalidData` 时为可安全丢弃的前缀建议；`NeedMoreData` 时必须为零。
- `Examined`：本次已检查的源长度。

`Consumed` 和 `Examined` 不能为负数，不能超过源长度，且 `Consumed <= Examined`。`Done` 必须至少消费一个字节。

`isFinalBlock == true` 表示以后不会再追加字节。规范实现不应对最终块返回 `NeedMoreData`，而应把截断输入报告为 `InvalidData`。

解析成功后只消费第一帧。源中属于后续帧的尾部由调用方持有，不能被隐藏地缓存或丢弃。

### 4.4 数据报解析

UDP 等“一包最多一帧，包头之前不会有噪声”的场景使用：

```csharp
BinaryParseResult result = codec.ParseDatagram(datagram, out Packet packet);
```

该扩展从偏移零解析，并以最终块调用解析器。它绝不向后滑动寻找另一个起点；即使 codec 的连续流模式配置为自动重新同步也一样。防御性地遇到 `NeedMoreData` 时，扩展会转换成 `InvalidData`。

是否允许完整帧后仍有数据由协议定义方决定。严格协议应在自己的 `Parse` 实现中把尾部视为无效；允许一个数据报携带附加区或多帧的协议可以定义不同规则。

### 4.5 连续流解码

默认实现为：

```csharp
var decoder = new BufferedBinaryStreamDecoder<Packet>(
    codec,
    codec.ResynchronizationMode);
```

通常由 `codec.CreateStreamDecoder()` 封装上述构造。

解码器提供两种输入方式：

```csharp
BinaryDecodeResult one = decoder.Append(nextByte, out Packet packet);
BinaryDecodeResult many = decoder.Append(buffer, out packet);
```

一次 `Append` 最多返回一帧。`BinaryDecodeResult.Consumed` 只表示本次传入 `source` 中直到第一帧结束为止的长度；调用前已经缓存的字节不计入该值。第一帧后的尾部仍归调用方所有，调用方必须用 `source.Slice(result.Consumed)` 再次提交。

`BinaryDecodeResult.Discarded` 是本次自动重新同步丢弃的字节总数，可能包含以前缓存的字节，因此可以大于 `Consumed`。它只保留一个整数，不标准化错误原因；最终结果仍统一为有效帧或无效数据。

若返回 `NeedMoreData`，当前输入已经被接收，通常 `Consumed == source.Length`。解码器内部缓冲绝不超过 codec 的 `MaximumFrameLength`。候选已经达到最大长度但解析器仍要求更多数据时，该候选不可能合法，将按重新同步策略处理。

### 4.6 重新同步策略

`BinaryResynchronizationMode` 由协议 codec 决定：

- `Automatic`：遇到 `InvalidData` 时先丢弃解析器建议的 `Consumed` 前缀；建议为零时丢弃一个字节，然后继续搜索下一候选帧。
- `StopOnInvalidData`：不向后搜索，立即返回 `InvalidData` 并进入 `Faulted`。

自动搜索适合没有外部报文边界的串口或 TCP 字节流。数据报解析始终禁用向后搜索。带转义、校验和特殊帧界定的协议也可以实现自己的专用 `IBinaryStreamDecoder<T>`，而不必勉强套用默认缓冲解析器。

### 4.7 生命周期

`BinaryDecoderState`：

- `Ready`：可以接收新帧，没有未完成候选。
- `Receiving`：保留尚未收满的候选帧。
- `Completed`：输入已经正常结束。
- `Faulted`：发生终止性无效数据或截断。

`Complete()` 明确结束输入：

- 无残留候选时返回 `Done` 并进入 `Completed`。
- 有残留候选时返回 `InvalidData` 并进入 `Faulted`。
- EOF 不充当隐式帧分隔符；完整帧应当已经由 `Append` 返回。

处于 `Completed` 或 `Faulted` 时不能继续 `Append`，也不能再次 `Complete`。必须先调用 `Reset()`；错误调用抛出带本地化信息的异常。

### 4.8 常见的“固定头 + 固定结构体正文”协议

可变长度帧经常由固定头和若干编译期固定大小的正文类型组成。推荐做法是：

1. 在变量 codec 中先读取足够的固定头字节。
2. 使用生成的固定头 codec 解析头部。
3. 根据头中的类型码选择正文 CLR 类型和对应固定 codec。
4. 用正文 codec 的 `FixedSize` 计算整帧长度。
5. 数据不足时返回 `NeedMoreData`；类型码未知、声明长度不一致或校验失败时返回 `InvalidData`。
6. 数据完整后调用选中的固定 codec 解析正文，再组装最终帧对象。

这种组合复用固定布局、字节序、数组和诊断规则，同时把协议分派保留在最了解协议的代码中。公共核心不提供全局类型注册表，也不通过 Attribute 自动生成可变协议分派。

### 4.9 转义和定界协议

例如以 `0x02` 开头、`0x03` 结尾，并把 `0x02`、`0x03`、`0x1B` 转义成 `0x1B` 加变换字节的协议，其线路长度会随内容变化，而且有效数据长度可能依赖解转义后的总长度。这类协议可以实现专用流解码器：逐字节维护“帧内/转义中”状态、边接收边还原、到帧尾后验证校验。

通用运行时不会为这种协议提供 Attribute 或通用转义流水线。手写实现更清晰，也能精确处理数据报边界、错误恢复和校验时机。它仍可实现 `IBinaryCodec<T>` 与 `IBinaryStreamDecoder<T>`，并在已还原的固定片段上调用生成的固定 codec。

## 5. 诊断和失败原则

源生成器必须对以下情况报告编译错误：

- 类型或成员为泛型、可空或大小不能在编译期确定；
- 非自动实现属性被当作存储成员；
- `Sequential` 成员分散在多个 partial 声明；
- `Explicit` 缺少必要的 `FieldOffset`，或使用 `OptIn`；
- `Auto` 布局；
- 不支持的 `MarshalAs`、动态长度、指针或自定义封送；
- `ByValArray` 的 `SizeConst`、`ArraySubType` 或元素表示不明确；
- `char`、`string`、`ByValTStr` 或依赖 `CharSet` 的表示；
- 显式重叠区域存在冲突的字节序或线路解释；
- 继承、只读构造或嵌套类型使生成代码无法安全建立对象；
- 同一成员具有冲突的包含、排除或表示标记。

显式设置 `CharSet` 只报告 Warning，因为它可以安全忽略且不会改变二进制结果。该 Warning 必须使用稳定的诊断 ID，允许调用方按普通 Roslyn 方式显式屏蔽。

对无法明确判断支持与否的形式，生成器必须优先报告诊断，不得静默采用平台相关行为。

当前诊断编号：

- `LYBIN001`：类型本身不受支持，例如非 partial、泛型、ref-like、无效基类或抽象内联类型。
- `LYBIN002`：成员表示不受支持，例如可空、动态数组、不匹配的 `MarshalAs` 或非自动属性。
- `LYBIN003`：布局无效，例如 `Auto`、非法 `Pack`、跨 partial 的顺序成员、缺少 `FieldOffset` 或有歧义的重叠。
- `LYBIN004`：同一成员的包含和排除标记冲突。
- `LYBIN005`：用户成员占用了生成 API 的保留名称。
- `LYBIN101`：显式 `CharSet` 不影响二进制行为；这是可以按诊断 ID 屏蔽的 Warning。

## 6. 非目标

公共核心不直接提供：

- `Stream`、`Socket`、`PipeReader`、异步 I/O 或网络重试；
- 全局 codec 注册和反射工厂；
- 动态长度成员的 Attribute 驱动生成；
- 字符串编码、终止符推断或 `CharSet` 封送；
- 自定义封送器、指针线路表示；
- 通用字节转义协议引擎；
- 借用内部缓冲区的结果对象。

这些能力应在传输层或具体协议层组合实现，核心只负责确定性的二进制表示、无状态解析契约和有界流状态管理。
