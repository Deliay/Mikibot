using System.Reflection;
using Microsoft.Extensions.Logging;

namespace Mikibot.Analyze.Bot.Images;

public class MemeCommandHandler(ILogger<MemeCommandHandler> logger)
{
    private readonly Dictionary<string, Memes.Factory> _memeProcessors = [];
    
    
    public void Register(string command, Func<Memes.Factory> factoryGetter)
    {
        _memeProcessors.Add(command, factoryGetter());
    }
    public void Register(string command, Memes.Factory factory)
    {
        _memeProcessors.Add(command, factory);
    }
    
    public void RegisterStaticMethods(Type type)
    {
        var methodInfos = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
        foreach (var methodInfo in methodInfos)
        {
            if (methodInfo.GetCustomAttribute<MemeCommandMappingAttribute>() is not {} attribute) continue;

            foreach (var attributeCommand in attribute.Commands)
            {
                _memeProcessors.Add(attributeCommand, methodInfo.CreateDelegate<Memes.Factory>());
            }
        }
    }
    
    public Memes.FactoryComposer? GetComposePipeline(string command)
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
            .Where(p => _memeProcessors.ContainsKey(p.Item1))
            .Select(p => (factory: _memeProcessors[p.Item1], argument: p.Item2))
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
                    (current, next) => current.Combine(Memes.Handle(next)(current.Frames)));
            })
        };
    }

}