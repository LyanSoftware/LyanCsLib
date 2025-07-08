using System;
using System.Collections.Generic;
using System.Text;
using Avalonia.Data;
using Lytec.Common;

namespace Lytec.AvaloniaUI;

[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field, AllowMultiple = false, Inherited = false)]
public class AvaloniaDirectPropertyAttribute : Attribute
{
    public object? UnsetValue { get; set; }

    public BindingMode BindingMode { get; set; } = BindingMode.OneWay;

    public bool EnableDataValidation { get; set; } = false;

    public string PropertyName { get; } = "";

    public AvaloniaDirectPropertyAttribute() { }

    public AvaloniaDirectPropertyAttribute(string propertyName) => PropertyName = propertyName;

}
