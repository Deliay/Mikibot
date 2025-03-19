using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Bot.Images;

public class MemeCommandHandler(ILogger<MemeCommandHandler> logger)
{
    private readonly Dictionary<string, Memes.Factory> _memeProcessors = [];
    private readonly Dictionary<string, string> _memeHelper = [];
    private readonly Dictionary<string, Func<string, Memes.Factory>> _memeGroupProcessors = [];
    public IReadOnlyDictionary<string, string> MemeHelpers => _memeHelper;
    
    public void Register(string command, string help, Func<Memes.Factory> factoryGetter)
    {
        Register(command, help, factoryGetter());
    }
    public void Register(string command, string help, Memes.Factory factory)
    {
        _memeProcessors.Add(command, factory);
        _memeHelper.Add(command, help);
    }
    public void Register(string command, Memes.Factory factory)
    {
        _memeProcessors.Add(command, factory);
    }

    public void RegisterGroupCommand(string command, string help, Func<string, Memes.Factory> factoryGetter)
    {
        _memeGroupProcessors.Add(command, factoryGetter);
        _memeHelper.Add(command, help);
    }
    
    public void RegisterStaticMethods(Type type)
    {
        var methodInfos = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var methodInfo in methodInfos)
        {
            if (methodInfo.GetCustomAttribute<MemeCommandMappingAttribute>() is not {} attribute) continue;

            foreach (var attributeCommand in attribute.Commands)
            {
                logger.LogInformation("Adding {} with delegate {}", attributeCommand, methodInfo.Name);
                _memeProcessors.Add(attributeCommand, methodInfo.CreateDelegate<Func<Memes.Factory>>()());
                _memeHelper.Add(attributeCommand, attribute.Help);
            }
        }
    }
    
    public Memes.FactoryComposer? GetComposePipeline(string command, string groupId)
    {
        var possibleCommands = command.Split('/', StringSplitOptions.RemoveEmptyEntries);
        logger.LogInformation("Split input commands: {}", string.Join(", ", possibleCommands));
        var factories = possibleCommands
            .Select(s => s.Trim())
            .Select(s =>
            {
                if (s.Contains(':')) return s.Split(':', 2);
                else if (s.Contains('：')) return s.Split('：', 2);
                else if (s.Contains('-')) return s.Split('-', 2);
                else return [s];
            })
            .Select(s => (s[0], s.Length > 1 ? s[1] : ""))
            .Select(p =>
            {
                if (_memeGroupProcessors.TryGetValue(p.Item1, out var factoryFactory))
                    return (factoryFactory(groupId), p.Item2);
                
                if (_memeProcessors.TryGetValue(p.Item1, out var factory))
                    return (factory, p.Item2);

                return default;
            })
            .Where(f => f is { Item1 : not null })
            .ToList();
        
        logger.LogInformation("Match {} meme factory", factories.Count);
        
        return factories.Count switch
        {
            1 => Memes.Handle(factories[0]),
            0 => null,
            _ => ((seq, token) =>
            {
                var head = Memes.Handle(factories[0])(seq, token);
                return factories[1..].Aggregate(head,
                    (current, next) => Memes.Handle(next)(current.Frames).CombineError(current));
            })
        };
    }

}