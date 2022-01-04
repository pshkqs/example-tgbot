﻿using Example.Logging;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Example.Commands;

public delegate Task<bool> ExecuteChatCommand(ChatCommandContext context);

[AttributeUsage(AttributeTargets.Method)]
public class ChatCommandAttribute : Attribute
{
    public string Name { get; private set; }
    public string Description { get; private set; }
    public bool Hidden { get; private set; }

    public ChatCommandAttribute(string name, string desc, bool hidden = false)
    {
        if (name == "" || desc == "")
            throw new ArgumentException("Name and description of command can't be empty");

        Name = name;
        Description = desc;
        Hidden = hidden;
    }
}

public class ChatCommandContext
{
    public TelegramBotClient Client { get; private set; }
    public Message Message { get; private set; }
    public string[] Args { get; private set; }

    public ChatCommandContext(TelegramBotClient client, Message message)
    {
        Client = client;
        Message = message;
        var splitted = message.Text!.Split(' ');
        Args = new string[splitted.Length - 1];
        Array.Copy(splitted, 1, Args, 0, splitted.Length - 1);
    }
}

internal class ChatCommandInfo
{
    public string Name => _attribute.Name;
    public string Description => _attribute.Description;
    public bool Hidden => _attribute.Hidden;

    private readonly ExecuteChatCommand _command;
    private readonly ChatCommandAttribute _attribute;

    public ChatCommandInfo(ExecuteChatCommand command, ChatCommandAttribute attribute)
    {
        _command = command;
        _attribute = attribute;
    }

    public bool ExecuteAsync(TelegramBotClient client, Message message)
    {
        return _command(new ChatCommandContext(client, message)).GetAwaiter().GetResult();
    }
}

internal class ChatCommandProvider
{
    private readonly HashSet<ChatCommandInfo> _commands;
    private readonly TelegramBotClient _client;

    public ChatCommandProvider(TelegramBotClient client)
    {
        _client = client;
        _commands = ResolveCommandMethods();
    }

    public bool TryExecuteCommand(string name, Message message)
    {
        name = name.TrimStart('/');
        var command = _commands.FirstOrDefault(cmd => string.Compare(cmd.Name, name, StringComparison.OrdinalIgnoreCase) == 0);

        if (command == default)
            return false;

        Logger.Log($"{message.From!.Username} executing command {command.Name}");
        var result = command.ExecuteAsync(_client, message);

        if (result == false)
            Logger.Log($"{message.From.Username} tried to execute {command.Name} but it's failed", LogSeverity.WARNING);

        return result;
    }

    public BotCommand[] GetBotCommands()
    {
        var commands = _commands.Where(x => x.Hidden == false);
        var result = new BotCommand[commands.Count()];
        var index = 0;
        foreach (var command in commands)
        {
            result[index++] = new BotCommand()
            {
                Command = command.Name,
                Description = command.Description
            };
        }

        return result;
    }

    private static HashSet<ChatCommandInfo> ResolveCommandMethods()
    {
        var methodInfos = Assembly
            .GetExecutingAssembly()
            .GetTypes()
            .SelectMany(type => type.GetMethods())
            .Where(method => method.GetCustomAttribute<ChatCommandAttribute>() != null);

        var result = new HashSet<ChatCommandInfo>();

        foreach (var method in methodInfos)
            result.Add(new ChatCommandInfo(method.CreateDelegate<ExecuteChatCommand>(), method.GetCustomAttribute<ChatCommandAttribute>()!));

        Logger.Log($"Loaded {result.Count} commands: {string.Join(", ", result.Select(x => x.Name))}");
        var hidden = result.Where(x => x.Hidden == true);
        Logger.Log($"{hidden.Count()} hidden commands: {string.Join(", ", hidden.Select(x => x.Name))}");
        return result;
    }
}

