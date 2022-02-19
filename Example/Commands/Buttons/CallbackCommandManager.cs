﻿using System.Reflection;
using Example.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace Example.Commands.Buttons;

internal delegate Task<bool> ExecuteCallbackCommand(CallbackCommandContext context);

[AttributeUsage(AttributeTargets.Method)]
internal class CallbackCommandAttribute : Attribute
{
    public CallbackButtons.Id Id { get; }

    public CallbackCommandAttribute(CallbackButtons.Id id)
    {
        Id = id;
    }
}

internal class CallbackCommandContext
{
    public TelegramBotClient Client { get; }
    public CallbackQuery Callback { get; }

    public string[] Args { get; }
    
    public CallbackCommandContext(TelegramBotClient client, CallbackQuery query)
    {
        Client = client;
        Callback = query;
        Args = query.Data.Split(';');
    }
}

internal class CallbackCommandInfo
{
    public CallbackButtons.Id Id { get; }

    private readonly ExecuteCallbackCommand _command;

    public CallbackCommandInfo(CallbackButtons.Id id, ExecuteCallbackCommand action)
    {
        Id = id;
        _command = action;
    }

    public bool Execute(TelegramBotClient client, CallbackQuery callback)
    {
        return _command(new CallbackCommandContext(client, callback)).GetAwaiter().GetResult();
    }
}

internal class CallbackCommandManager : CommandManager<CallbackCommandInfo>
{
    public CallbackCommandManager(TelegramBotClient client) : base(client) { }

    public bool TryExecute(CallbackQuery callback)
    {

        var command = CommandDelegates.FirstOrDefault(info => callback.Data != null && callback.Data.Split(';')[0].ToInt() == info.Id.ToButtonIdInt());

        if (command == default)
            return false;

        Logger.Log($"{callback.From.Username} tap callback button {command.Id}");
        var result = command.Execute(Client, callback);

        if (result == false)
            Logger.Log($"{callback.From.Username} tried to execute {command.Id} but it's failed", LogSeverity.Warning);

        
        return result;
    }

    public override void Register(object target)
    {
        var methods = FindMethodsWithAttribute<CallbackCommandAttribute>(target);

        foreach (var method in methods)
        {
            try
            {
                var command = method.CreateDelegate<ExecuteCallbackCommand>(target);
                var attr = method.GetCustomAttribute<CallbackCommandAttribute>();
                CommandDelegates.Add(new CallbackCommandInfo(attr.Id, command));
                Logger.Log($"Registered callback button {attr.Id} as {method.DeclaringType.FullName}.{method.Name}");
            }
            catch
            {
                Logger.Log($"{method.DeclaringType.FullName}.{method.Name} can't be callback button", LogSeverity.Error);
            }
        }
    }
}