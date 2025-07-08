using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace Lytec.Common.Localization;

public class Language
{
    public static CultureInfo GlobalCurrent
    {
        get => CultureInfo.CurrentUICulture;
        set => CultureInfo.CurrentUICulture = value;
    }

    public readonly CultureInfo Auto = GlobalCurrent;
    public CultureInfo Default { get; set; } = GlobalCurrent;
    public CultureInfo BuiltIn { get; set; } = CultureInfo.GetCultureInfo("en");

    private CultureInfo _Current = GlobalCurrent;
    public CultureInfo Current
    {
        get => _Current;
        set
        {
            if (Current == value)
                return;
            _Current = value;
            Changed?.Invoke();
        }
    }

    public event Action? Changed;

    private string? _Id;
    public string CurrentId
    {
        get => _Id ?? LanguageId.Auto;
        set
        {
            switch (value?.ToLower())
            {
                case null:
                case var x when x == LanguageId.Auto.ToLower():
                    _Id = null;
                    Current = Auto;
                    break;
                case var x when x == LanguageId.BuiltIn.ToLower():
                    _Id = LanguageId.BuiltIn;
                    Current = BuiltIn;
                    break;
                case var x when x == LanguageId.Default.ToLower():
                    _Id = LanguageId.Default;
                    Current = Default;
                    break;
                default:
                    try
                    {
                        var ci = CultureInfo.GetCultureInfo(value);
                        _Id = ci.Name;
                        Current = ci;
                    }
                    catch (Exception) { }
                    break;
            }
        }
    }
}
