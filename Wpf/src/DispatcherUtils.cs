using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace Lytec.Wpf;

public interface IUiObj { }

public static class DispatcherUtils
{
    public static Dispatcher Dispatcher => Application.Current.Dispatcher;

    public static void Invoke(this IUiObj _, Action action)
    {
        if (Dispatcher.CheckAccess())
            action();
        else Dispatcher.Invoke(action);
    }
    
    public static void Invoke(this IUiObj _, Action action, DispatcherPriority priority)
    => Dispatcher.Invoke(action, priority);

    public static T Invoke<T>(this IUiObj _, Func<T> action)
    => Dispatcher.CheckAccess() ? action() : Dispatcher.Invoke(action);

    public static T Invoke<T>(this IUiObj _, Func<T> action, DispatcherPriority priority)
    => Dispatcher.Invoke(action, priority);

    public static Task InvokeAsync(this IUiObj _, Action action)
    {
        if (Dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        else return Dispatcher.InvokeAsync(action).Task;
    }
    
    public static Task InvokeAsync(this IUiObj _, Action action, DispatcherPriority priority)
    => Dispatcher.InvokeAsync(action, priority).Task;

    public static async Task<T> InvokeAsync<T>(this IUiObj _, Func<T> action)
    => Dispatcher.CheckAccess() ? action() : await Dispatcher.InvokeAsync(action);
    
    public static Task<T> InvokeAsync<T>(this IUiObj _, Func<T> action, DispatcherPriority priority)
    => Dispatcher.InvokeAsync(action, priority).Task;

    public static async Task InvokeAsync(this IUiObj _, Func<Task> action)
    => await (Dispatcher.CheckAccess() ? action() : await Dispatcher.InvokeAsync(action));
    
    public static async Task InvokeAsync(this IUiObj _, Func<Task> action, DispatcherPriority priority)
    => await await Dispatcher.InvokeAsync(action, priority);

    public static async Task<T> InvokeAsync<T>(this IUiObj _, Func<Task<T>> action)
    => await (Dispatcher.CheckAccess() ? action() : await Dispatcher.InvokeAsync(action));
    
    public static async Task<T> InvokeAsync<T>(this IUiObj _, Func<Task<T>> action, DispatcherPriority priority)
    => await await Dispatcher.InvokeAsync(action, priority);
    
    public static void Invoke(this DispatcherObject obj, Action action)
    {
        if (obj.Dispatcher.CheckAccess())
            action();
        else obj.Dispatcher.Invoke(action);
    }
    
    public static void Invoke(this DispatcherObject obj, Action action, DispatcherPriority priority)
    => obj.Dispatcher.Invoke(action, priority);

    public static T Invoke<T>(this DispatcherObject obj, Func<T> action)
    => obj.Dispatcher.CheckAccess() ? action() : obj.Dispatcher.Invoke(action);

    public static T Invoke<T>(this DispatcherObject obj, Func<T> action, DispatcherPriority priority)
    => obj.Dispatcher.Invoke(action, priority);

    public static Task InvokeAsync(this DispatcherObject obj, Action action)
    {
        if (obj.Dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }
        else return obj.Dispatcher.InvokeAsync(action).Task;
    }
    
    public static Task InvokeAsync(this DispatcherObject obj, Action action, DispatcherPriority priority)
    => obj.Dispatcher.InvokeAsync(action, priority).Task;

    public static async Task<T> InvokeAsync<T>(this DispatcherObject obj, Func<T> action)
    => obj.Dispatcher.CheckAccess() ? action() : await obj.Dispatcher.InvokeAsync(action);
    
    public static Task<T> InvokeAsync<T>(this DispatcherObject obj, Func<T> action, DispatcherPriority priority)
    => obj.Dispatcher.InvokeAsync(action, priority).Task;

    public static async Task InvokeAsync(this DispatcherObject obj, Func<Task> action)
    => await (obj.Dispatcher.CheckAccess() ? action() : await obj.Dispatcher.InvokeAsync(action));
    
    public static async Task InvokeAsync(this DispatcherObject obj, Func<Task> action, DispatcherPriority priority)
    => await await obj.Dispatcher.InvokeAsync(action, priority);

    public static async Task<T> InvokeAsync<T>(this DispatcherObject obj, Func<Task<T>> action)
    => await (obj.Dispatcher.CheckAccess() ? action() : await obj.Dispatcher.InvokeAsync(action));
    
    public static async Task<T> InvokeAsync<T>(this DispatcherObject obj, Func<Task<T>> action, DispatcherPriority priority)
    => await await obj.Dispatcher.InvokeAsync(action, priority);

}
