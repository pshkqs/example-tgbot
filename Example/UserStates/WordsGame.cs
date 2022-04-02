﻿using Example.Commands;
using System;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Example.UserStates;

public class WordsGame : UserState
{
    #region static

    private static readonly Dictionary<char, List<string>> _words = new();
    private static readonly char[] _blackList = new char[]
    {
        'ь',
        'ъ',
        'ы'
    };

    static WordsGame()
    {
        using var reader = new StreamReader("nouns.txt");
        while (reader.EndOfStream == false)
        {
            var word = reader.ReadLine();
            if (word == null)
                break;

            if (_words.ContainsKey(word[0]) == false)
                _words[word[0]] = new();

            _words[word[0]].Add(word);
        }
        reader.Close();
    }

    #endregion

    private HashSet<string> _used;
    private char _letter;

    public override async Task Enter(UserStateManager manager, long userId, TelegramBotClient client)
    {
        _ = base.Enter(manager, userId, client);
        await Client.SendTextMessageAsync(userId, "Для выхода нужно ввести команду /words ещё раз");
        _used = new();
        await NextWord("а");
    }

    public override async Task Exit() => await Client.SendTextMessageAsync(UserId, "Игра в слова закончена");

    public override async void Update(Message message)
    {
        if (message == null || string.IsNullOrEmpty(message.Text) || message.Text[0] == '/')
            return;

        var word = message.Text.Split(' ')[0].ToLowerInvariant();

        if (word[0] != _letter)
        {
            await Client.SendTextMessageAsync(UserId, $"Слово должно быть на букву `{_letter}`", ParseMode.Markdown);
            return;
        }

        if (_used.Contains(word))
        {
            await Client.SendTextMessageAsync(UserId, $"Слово `{word}` уже было использовано", ParseMode.Markdown);
            return;
        }

        if (_words[word[0]].Contains(word) == false)
        {
            await Client.SendTextMessageAsync(UserId, $"Я не знаю слово `{word}`", ParseMode.Markdown);
            return;
        }

        await NextWord(word);
    }

    private async Task NextWord(string word)
    {
        var next = GetNotUsedWord(GetLastValideLetter(word));
        _letter = GetLastValideLetter(next);

        _used.Add(word);
        _used.Add(next);

        await Client.SendTextMessageAsync(UserId, $"Следующее слово `{next}`", ParseMode.Markdown);
        Logger.Log($"WordsGame[user {UserId}] update: {word} -> {next}");
    }

    private static char GetLastValideLetter(string word)
    {
        var chars = word.ToCharArray();
        for (var i = chars.Length - 1; i >= 0; i--)
        {
            if (_blackList.Contains(chars[i]))
                continue;

            return chars[i];
        }
        return chars[0];
    }

    private string GetNotUsedWord(char letter)
    {
        var word = _words[letter][Random.Shared.Next(0, _words[letter].Count)];
        while (_used.Contains(word))
            word = _words[letter][Random.Shared.Next(0, _words[letter].Count)];

        return word;
    }
}