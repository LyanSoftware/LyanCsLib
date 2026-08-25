using SmartFormat;

namespace Lytec.Common.Localization;

public interface ILocalizer
{
    string Format(ILocalizeString str);
}

public abstract class Localizer : ILocalizer
{
    public const string ScopeConnector = ":";

    public abstract bool TryQuery(string key, out string Value);

    public virtual string Format(ILocalizeString str)
    {
        if (TryQuery(str.Key, out var format))
            return Smart.Format(format, str.Arguments);
        return str.DefaultMessage ?? str.Key;
    }

}
