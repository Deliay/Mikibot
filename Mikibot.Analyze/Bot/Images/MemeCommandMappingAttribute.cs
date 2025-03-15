namespace Mikibot.Analyze.Bot.Images;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class MemeCommandMappingAttribute(params string[] commands) : Attribute
{
    public string[] Commands { get; } = commands;
}