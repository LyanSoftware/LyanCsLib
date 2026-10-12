using System.Globalization;
using System.Text.Encodings.Web;
using global::Fluid;
using global::Fluid.Ast;
using global::Fluid.Values;

namespace Lytec.Common.Localization.Fluid;

public sealed class FluidLocalizationTemplateRenderer : ILocalizationTemplateRenderer
{
    private readonly FluidParser parser = new();
    private readonly TemplateOptions fluidOptions;
    private readonly LocalizationTemplateOptions templateOptions;

    public FluidLocalizationTemplateRenderer(LocalizationTemplateOptions templateOptions)
    {
        this.templateOptions = templateOptions ?? throw new ArgumentNullException(nameof(templateOptions));
        fluidOptions = CreateFluidOptions();
    }

    public string Render(
        string template,
        IReadOnlyDictionary<string, object?>? variables,
        CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(template);
        ArgumentNullException.ThrowIfNull(culture);

        if (!parser.TryParse(template, out var parsedTemplate, out var parseError))
            throw new LocalizationTemplateException($"The localization template could not be parsed: {parseError}");
        if (parsedTemplate is not IStatementList statementList)
            throw new LocalizationTemplateException("The localization template does not expose a Fluid statement list.");

        var model = NormalizeVariables(variables);
        ValidateStatements(statementList.Statements, model);

        var context = new TemplateContext(model, fluidOptions, allowModelMembers: false)
        {
            CultureInfo = culture,
        };

        try
        {
            return parsedTemplate.Render(context);
        }
        catch (LocalizationTemplateException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new LocalizationTemplateException("The localization template could not be rendered.", exception);
        }
    }

    private static TemplateOptions CreateFluidOptions()
    {
        var options = new TemplateOptions
        {
            ModelNamesComparer = StringComparer.Ordinal,
        };
        options.MemberAccessStrategy.IgnoreCasing = false;

        if (!options.Filters.TryGetValue("default", out var defaultFilter))
            throw new InvalidOperationException("Fluid does not provide the required Liquid 'default' filter.");

        options.Filters.Clear();
        options.Filters.AddFilter("default", defaultFilter);
        options.Filters.AddFilter("format", Format);
        options.ValueConverters.Insert(0, ConvertFormattable);
        return options;
    }

    private static object? ConvertFormattable(object value)
        => value is IFormattable
            ? new FormattableFluidValue(value)
            : null;

    private static ValueTask<FluidValue> Format(
        FluidValue input,
        FilterArguments arguments,
        TemplateContext context)
    {
        if (arguments.Count != 1 || arguments.At(0).ToObjectValue() is not string format)
            throw new LocalizationTemplateException("The 'format' filter requires one string literal argument.");

        var result = LocalizationTemplateFormatter.Format(
            input.ToObjectValue(),
            format,
            context.CultureInfo);
        return new ValueTask<FluidValue>(new StringValue(result));
    }

    private void ValidateStatements(
        IReadOnlyList<Statement> statements,
        IReadOnlyDictionary<string, object?> variables)
    {
        foreach (var statement in statements)
        {
            if (statement is TextSpanStatement)
                continue;
            if (statement is OutputStatement output)
            {
                ValidateOutput(output.Expression, variables);
                continue;
            }

            throw new LocalizationTemplateException(
                $"The Liquid statement '{statement.GetType().Name}' is not supported by localization templates.");
        }
    }

    private void ValidateOutput(
        Expression expression,
        IReadOnlyDictionary<string, object?> variables)
    {
        while (expression is FilterExpression filter)
        {
            ValidateFilter(filter);
            expression = filter.Input;
        }

        if (expression is not MemberExpression member ||
            member.Segments.Count == 0 ||
            member.Segments.Any(static segment => segment is not IdentifierSegment))
        {
            throw new LocalizationTemplateException(
                "Localization output expressions must contain only a named variable or nested dictionary path.");
        }

        if (templateOptions.DebugMode && !PathExists(member, variables))
        {
            throw new LocalizationTemplateException(
                $"The localization template variable '{GetPath(member)}' is missing.");
        }
    }

    private static void ValidateFilter(FilterExpression filter)
    {
        if (filter.Name == "default")
        {
            if (filter.Parameters.Count == 1 &&
                string.IsNullOrEmpty(filter.Parameters[0].Name) &&
                filter.Parameters[0].Expression is LiteralExpression)
            {
                return;
            }

            throw new LocalizationTemplateException(
                "The 'default' filter requires one scalar literal argument.");
        }

        if (filter.Name == "format")
        {
            if (filter.Parameters.Count == 1 &&
                string.IsNullOrEmpty(filter.Parameters[0].Name) &&
                filter.Parameters[0].Expression is LiteralExpression literal &&
                literal.Value.ToObjectValue() is string)
            {
                return;
            }

            throw new LocalizationTemplateException(
                "The 'format' filter requires one string literal argument.");
        }

        throw new LocalizationTemplateException(
            $"The Liquid filter '{filter.Name}' is not supported by localization templates.");
    }

    private static bool PathExists(
        MemberExpression member,
        IReadOnlyDictionary<string, object?> variables)
    {
        object? current = variables;
        foreach (var segment in member.Segments.Cast<IdentifierSegment>())
        {
            if (current is not IReadOnlyDictionary<string, object?> dictionary ||
                !dictionary.TryGetValue(segment.Identifier, out current))
            {
                return false;
            }
        }

        return true;
    }

    private static string GetPath(MemberExpression member)
        => string.Join(
            ".",
            member.Segments.Cast<IdentifierSegment>().Select(static segment => segment.Identifier));

    private static IReadOnlyDictionary<string, object?> NormalizeVariables(
        IReadOnlyDictionary<string, object?>? variables)
    {
        var normalized = new Dictionary<string, object?>(StringComparer.Ordinal);
        if (variables is null)
            return normalized;

        foreach (var variable in variables)
        {
            normalized.Add(
                variable.Key,
                variable.Value is IReadOnlyDictionary<string, object?> nested
                    ? NormalizeVariables(nested)
                    : variable.Value);
        }

        return normalized;
    }

    private sealed class FormattableFluidValue(object value) : ObjectValueBase(value)
    {
        public override string ToStringValue()
            => LocalizationTemplateFormatter.Format(Value, null, CultureInfo.InvariantCulture);

        [Obsolete]
        public override void WriteTo(TextWriter writer, TextEncoder encoder, CultureInfo culture)
            => writer.Write(encoder.Encode(LocalizationTemplateFormatter.Format(Value, null, culture)));

        public override ValueTask WriteToAsync(
            TextWriter writer,
            TextEncoder encoder,
            CultureInfo culture)
            => new(writer.WriteAsync(
                encoder.Encode(LocalizationTemplateFormatter.Format(Value, null, culture))));
    }
}
