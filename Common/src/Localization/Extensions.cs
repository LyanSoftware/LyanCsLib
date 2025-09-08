using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Newtonsoft.Json;

namespace Lytec.Common.Localization;

public static class Extensions
{
    public static IServiceCollection AddLocalization<T>(this IServiceCollection collection) where T : Localization, new()
    => collection.AddSingleton<Localization>(new T());
    public static IServiceCollection AddLocalization<T>(this IServiceCollection collection, T i18n) where T : Localization
    => collection.AddSingleton<Localization>(i18n);

    public static string Query([AllowNull] this IStringLocalizer localizer, string key, string? defaultValue = null)
    => localizer == null ? defaultValue ?? key : (string)localizer[key];
}
