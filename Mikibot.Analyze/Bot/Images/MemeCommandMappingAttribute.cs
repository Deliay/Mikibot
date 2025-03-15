namespace Mikibot.Analyze.Bot.Images;

[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class MemeCommandMappingAttribute(string help, params string[] commands) : Attribute
{
    public string Help { get; } = help;
    public string[] Commands { get; } = commands;
}