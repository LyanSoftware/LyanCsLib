using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;

namespace Lytec.Common.Threading;

public static class ReentrancyGuard
{
    // 存储每个方法名的执行标志（用 byte 占位，节省内存）
    private static readonly ConcurrentDictionary<string, byte> _executingMap = new();

    /// <summary>
    /// 执行受保护的同步操作，防止同实例同方法的重入。
    /// </summary>
    /// <param name="instance">当前类的实例（传入 this）</param>
    /// <param name="action">业务逻辑</param>
    /// <param name="methodName">方法名（由编译器自动传入）</param>
    public static void ExecuteGuarded<T>(T instance, Action action, [CallerMemberName] string methodName = "")
    {
        // 对于引用类型，instance 不可能为 null
        // 但为了安全，可以加 null 检查
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        string key = $"{typeof(T).FullName}.{methodName}_{RuntimeHelpers.GetHashCode(instance)}";
        ExecuteInternal(key, action);
    }

    /// <summary>
    /// 执行受保护的异步操作（支持 async/await）
    /// </summary>
    /// <param name="instance">当前类的实例（传入 this）</param>
    /// <param name="action">业务逻辑</param>
    /// <param name="methodName">方法名（由编译器自动传入）</param>
    public static async Task ExecuteGuardedAsync<T>(T instance, Func<Task> action, [CallerMemberName] string methodName = "")
    {
        // 对于引用类型，instance 不可能为 null
        // 但为了安全，可以加 null 检查
        if (instance is null) throw new ArgumentNullException(nameof(instance));
        string key = $"{typeof(T).FullName}.{methodName}_{RuntimeHelpers.GetHashCode(instance)}";
        await ExecuteInternalAsync(key, action);
    }

    /// <summary>
    /// 执行受保护的同步操作，防止同类型同方法的重入。
    /// </summary>
    /// <param name="declaringType">所在类型</param>
    /// <param name="action">业务逻辑</param>
    /// <param name="methodName">方法名（由编译器自动传入）</param>
    public static void ExecuteGuarded(Type declaringType, Action action, [CallerMemberName] string methodName = "")
    {
        string key = $"{declaringType.FullName}.{methodName}";
        ExecuteInternal(key, action);
    }

    /// <summary>
    /// 执行受保护的同步操作，防止同类型同方法的重入。
    /// </summary>
    /// <param name="declaringType">所在类型</param>
    /// <param name="action">业务逻辑</param>
    /// <param name="methodName">方法名（由编译器自动传入）</param>
    public static async Task ExecuteGuardedAsync(Type declaringType, Func<Task> action, [CallerMemberName] string methodName = "")
    {
        string key = $"{declaringType.FullName}.{methodName}";
        await ExecuteInternalAsync(key, action);
    }

    // ---- 内部通用实现 ----
    private static void ExecuteInternal(string key, Action action)
    {
        if (!_executingMap.TryAdd(key, 0)) return;
        try { action(); }
        finally { _executingMap.TryRemove(key, out _); }
    }

    private static async Task ExecuteInternalAsync(string key, Func<Task> asyncAction)
    {
        if (!_executingMap.TryAdd(key, 0)) return;
        try { await asyncAction(); }
        finally { _executingMap.TryRemove(key, out _); }
    }
}
