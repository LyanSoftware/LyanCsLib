using System;
using Lytec.Common.Localization.Extensions;

namespace Lytec.Common.Serialization;

/// <summary>为生成代码建立附带本地化信息的契约异常。</summary>
public static class BinarySerializationExceptionFactory
{
    private const string LocalizeScope = "Lytec.Common.Serialization.FixedBinarySerialization";

    public static InvalidOperationException InvalidOperation(string key, string defaultMessage)
        => new InvalidOperationException(defaultMessage).Localize(LocalizeScope, key, defaultMessage);

    public static ArgumentException InvalidArgument(string key, string defaultMessage)
        => new ArgumentException(defaultMessage).Localize(LocalizeScope, key, defaultMessage);
}
