﻿using Example.Commands.CallbackButtons;
using Telegram.Bot;
using Telegram.Bot.Types.ReplyMarkups;

namespace Example.Commands;

internal class BasicCommands
{
    [ChatCommand("test", "Тестовая команда")]
    private async Task<bool> Test(ChatCommandContext context)
    {
        await context.Client.SendTextMessageAsync(context.Message.Chat.Id, $"Тестовая команда, которая, кстати, работает!");
        return true;
    }

    [ChatCommand("callback", "Тест кнопок")]
    private async Task<bool> CallbackTest(ChatCommandContext context)
    {
        var buttons = new List<List<InlineKeyboardButton>>()
        {
            new List<InlineKeyboardButton>()
            {
                InlineKeyboardButton.WithCallbackData("Тестовая кнопка", ButtonId.FirstTest.ToButtonIdString()),
                InlineKeyboardButton.WithCallbackData("Вторая тестовая кнопка", ButtonId.SecondTest.ToButtonIdString())
            }
        };

        var markup = new InlineKeyboardMarkup(buttons);
        await context.Client.SendTextMessageAsync(context.Message.Chat.Id, $"Тест", replyMarkup: markup);
        return true;
    }

    [CallbackCommand(ButtonId.FirstTest)]
    private async Task<bool> FirstCallbackTest(CallbackCommandContext context)
    {
        _ = context.Client.AnswerCallbackQueryAsync(context.Callback.Id);
        await context.Client.EditMessageTextAsync(context.Callback.Message.Chat.Id, context.Callback.Message.MessageId, "Сообщение изменено");
        return true;
    }

    [CallbackCommand(ButtonId.SecondTest)]
    private async Task<bool> SecondCallbackTest(CallbackCommandContext context)
    {
        _ = context.Client.AnswerCallbackQueryAsync(context.Callback.Id);

        var buttons = context.Callback.Message.ReplyMarkup.InlineKeyboard.ToList();
        var newButtons = new List<List<InlineKeyboardButton>>();

        foreach (var row in buttons)
        {
            newButtons.Add(new List<InlineKeyboardButton>());
            var current = newButtons.Last();
            foreach (var button in row)
            {
                if (button.CallbackData == context.Callback.Data)
                    continue;

                current.Add(button);
            }
        }

        await context.Client.EditMessageReplyMarkupAsync(context.Callback.Message.Chat.Id, context.Callback.Message.MessageId, new InlineKeyboardMarkup(newButtons));
        return true;
    }
}