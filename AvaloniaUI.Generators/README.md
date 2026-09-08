# Lytec.AvaloniaUI.Generators

为 `Avalonia.Markup.Declarative.ViewBase` 子类生成函数式控件工厂。

## 使用

将生成器作为 Analyzer 引用：

```xml
<ProjectReference Include="path\to\Lytec.AvaloniaUI.Generators.csproj"
                  OutputItemType="Analyzer"
                  ReferenceOutputAssembly="false" />
```

在需要工厂的基类上添加特性：

```csharp
using Avalonia.Markup.Declarative;
using Lytec.AvaloniaUI.Generators;

[GenerateControlFactories]
public abstract partial class AppViewBase()
    : ViewBase(ViewInitializationStrategy.Lazy);
```

生成的工厂可以直接用于声明式 UI：

```csharp
MenuItem("文件");
TextBlock("状态");
Grid([TextBlock("A"), TextBlock("B")]).Rows("Auto,*");
```

普通 Avalonia 主属性会生成直接值、`BindingBase` 和编译绑定重载。`Children`、`Items` 等集合主属性遵循 `Avalonia.Markup.Declarative` 的 `params` 数组规则。

## 第三方控件

第三方控件必须在目标类上显式启用：

```csharp
[GenerateControlFactories]
[IncludeControlFactory(
    typeof(ColorPicker),
    PrimaryProperty = nameof(ColorPicker.Color),
    Alias = "ColorPicker")]
public abstract partial class AppViewBase()
    : ViewBase(ViewInitializationStrategy.Lazy);
```

`Alias` 没有冲突时可以省略。`PrimaryProperty` 省略时使用控件的 `ContentAttribute`；无法推断时只生成无参工厂并报告警告。

本生成器只负责工厂。若还需要第三方控件的其他 Declarative 链式属性，请同时按照 `Avalonia.Markup.Declarative` 的要求启用对应程序集：

```csharp
[assembly: GenerateMarkupExtensionsForAssembly(typeof(ColorPicker))]
```

## 名称解析

工厂方法与控件类型同名。在派生 View 中的 `typeof(...)`、`nameof(Type.Member)` 或静态属性访问位置，应在发生遮蔽时使用完全限定类型名，例如：

```csharp
global::Avalonia.Controls.TextBlock.ForegroundProperty
```
