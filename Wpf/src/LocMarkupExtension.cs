using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Markup;
using System.Windows.Threading;
using Lytec.Common;
using Exp = System.Linq.Expressions.Expression;

namespace Lytec.Wpf;

public static class Loc
{
    public static readonly DependencyProperty LocalizerProperty =
        DependencyProperty.RegisterAttached(
            "Localizer",
            typeof(object),
            typeof(Loc),
            new FrameworkPropertyMetadata(
                null,
                FrameworkPropertyMetadataOptions.Inherits));

    public static void SetLocalizer(DependencyObject obj, object? value) => obj.SetValue(LocalizerProperty, value);
    public static object? GetLocalizer(DependencyObject obj) => obj.GetValue(LocalizerProperty);
}

public interface ILocalizer
{
    public string this[string key] { get; }
}

public class LocExtension : MarkupExtension
{
    //public sealed class LocConverter : IValueConverter
    //{
    //    public static readonly LocConverter Instance = new();

    //    public object Convert(
    //        object value,
    //        Type targetType,
    //        object parameter,
    //        CultureInfo culture)
    //    {
    //        var key = parameter as string ?? "";

    //        if (value is null)
    //            return key;

    //        if (value is ILocalizer localizer)
    //            return localizer[key] ?? key;

    //        return key;
    //    }

    //    public object ConvertBack(
    //        object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        return "";
    //    }
    //}

    public sealed class LocConverter : IValueConverter
    {
        public static readonly LocConverter Instance = new();

        private static readonly ConcurrentDictionary<Type, Func<object, string, string?>> AccessorCache = new();

        public object Convert(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            var key = parameter as string ?? "";

            if (value is null)
                return key;

            // 字典类型直接走强类型分支，最快
            if (value is IReadOnlyDictionary<string, string> roDict)
                return roDict.TryGetValue(key, out var text) ? text : key;

            if (value is IDictionary<string, string> dict)
                return dict.TryGetValue(key, out var text) ? text : key;

            var accessor = AccessorCache.GetOrAdd(value.GetType(), CreateAccessor);

            try
            {
                return accessor(value, key) ?? key;
            }
            catch
            {
                return key;
            }
        }

        public object ConvertBack(
            object value,
            Type targetType,
            object parameter,
            CultureInfo culture)
        {
            return Binding.DoNothing;
        }

        private static Func<object, string, string?> CreateAccessor(Type type)
        {
            var indexer = type
                .GetDefaultMembers()
                .OfType<PropertyInfo>()
                .FirstOrDefault(p =>
                {
                    var ps = p.GetIndexParameters();
                    return ps.Length == 1 && ps[0].ParameterType == typeof(string);
                });

            if (indexer is null)
                return static (_, _) => null;

            var obj = Exp.Parameter(typeof(object), "obj");
            var key = Exp.Parameter(typeof(string), "key");

            var typedObj = Exp.Convert(obj, type);
            var indexAccess = Exp.MakeIndex(typedObj, indexer, new[] { key });

            Exp result;

            if (indexer.PropertyType == typeof(string))
            {
                result = indexAccess;
            }
            else
            {
                var toStringMethod = typeof(object).GetMethod(nameof(ToString))!;
                result = Exp.Condition(
                    Exp.Equal(indexAccess, Exp.Constant(null, indexer.PropertyType)),
                    Exp.Constant(null, typeof(string)),
                    Exp.Call(
                        Exp.Convert(indexAccess, typeof(object)),
                        toStringMethod));
            }

            return Exp
                .Lambda<Func<object, string, string?>>(result, obj, key)
                .Compile();
        }
    }

    public string Key { get; }

    public LocExtension(string key) => Key = Regex.Unescape(key);

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        object ret = Key;

        do
        {
            if (IsInDesignMode())
                break;

            if (serviceProvider.GetService(typeof(IProvideValueTarget)) is not IProvideValueTarget target)
                break;

            if (target.TargetObject is not DependencyObject
                || target.TargetProperty is not DependencyProperty)
                break;

            var binding = new Binding()
            {
                RelativeSource = new RelativeSource(RelativeSourceMode.Self),
                //Path = new PropertyPath($"(0).Localizer[{EscapeIndexer(Key)}]", Loc.LocalizerProperty),
                Path = new PropertyPath("(0)", Loc.LocalizerProperty),
                Converter = LocConverter.Instance,
                ConverterParameter = Key,
                Mode = BindingMode.OneWay,
                FallbackValue = Key,
                TargetNullValue = Key
            };

            ret = binding.ProvideValue(serviceProvider);
        }
        while (false);

        return ret;
    }

    public static string EscapeIndexer(string value)
    {
        return value
            //.Replace("\\", "\\\\")
            //.Replace("]", "\\]")
            //.Replace(",", "\\,")
            ;
    }

    public static bool IsInDesignMode()
    {
        return DesignerProperties.GetIsInDesignMode(new DependencyObject());
        //return Avalonia.Controls.Design.IsDesignMode;
    }
}
