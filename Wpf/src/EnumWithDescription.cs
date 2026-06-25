using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;

namespace Lytec.Wpf;

public abstract class EnumWithDescription : ObservableObject, IDisposable
{
    private static Func<string, string>? defaultLocalizer = null;
    public static Func<string, string>? DefaultLocalizer
    {
        get => defaultLocalizer;
        set
        {
            if (defaultLocalizer == value)
                return;
            defaultLocalizer = value;
            WeakReferenceMessenger.Default.Send(new LocalizerChangedMsg());
        }
    }

    public abstract void Dispose();

    public record LocalizerChangedMsg;
}

public partial class EnumWithDescription<T> : EnumWithDescription, IDisposable where T : Enum
{
    public T Value { get; }

    [ObservableProperty]
    private string description = "";

    [ObservableProperty]
    private string rawDescription = "";

    [ObservableProperty]
    private string localizationKey = "";

    private static Func<string, string>? localizer = null;
    public static Func<string, string>? Localizer
    {
        get => localizer;
        set
        {
            if (localizer == value)
                return;
            localizer = value;
            WeakReferenceMessenger.Default.Send(new LocalizerChangedMsg());
        }
    }

    public EnumWithDescription(T value, string? description = null)
    {
        Value = value;
        RawDescription = description ?? value.ToString();
        LocalizationKey = $"{typeof(T).FullName}.{value}";
        NotifyLangChanged();
        WeakReferenceMessenger.Default.Register<LangChangedMsg>(this, (sender, msg) => NotifyLangChanged());
        WeakReferenceMessenger.Default.Register<LocalizerChangedMsg>(this, (sender, msg) => NotifyLangChanged());
    }

    public override void Dispose() => WeakReferenceMessenger.Default.UnregisterAll(this);

    public void NotifyLangChanged() => Description = (Localizer ?? DefaultLocalizer)?.Invoke(LocalizationKey) ?? RawDescription;
    partial void OnRawDescriptionChanged(string value) => NotifyLangChanged();
    partial void OnLocalizationKeyChanged(string value) => NotifyLangChanged();

    public static ObservableCollection<EnumWithDescription<T>> FromValues(Func<T, string>? GetDescription = null)
    => new ObservableCollection<EnumWithDescription<T>>(Enum.GetValues(typeof(T))
        .Cast<T>()
        .Select(v => new EnumWithDescription<T>(v, GetDescription?.Invoke(v) ?? v.ToString()))
        );
}
