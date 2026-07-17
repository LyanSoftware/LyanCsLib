using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Lytec.NetStandard20Compatibility;

public static class ReflectionExtensions
{
    public static ConstructorInfo? GetConstructor(this Type type, BindingFlags bindingAttr, Type[] types)
    => type.GetConstructor(bindingAttr, null, types, null);

    public static MethodInfo? GetMethod(this Type type, string name, BindingFlags bindingAttr, Type[] types)
    => type.GetMethod(name, bindingAttr, null, types, null);
}
