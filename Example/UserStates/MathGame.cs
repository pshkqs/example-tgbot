using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace Example.UserStates;

public class MathGame : UserState
{
    private enum MathOperation
    {
        Add,
        Subtract,
        Multiply
    }

    private struct MathProblem
    {
        public int First;
        public int Second;
        public int Answer;
        public MathOperation Operation;


        public static MathProblem Create()
        {
            var operation = (MathOperation)Random.Shared.Next(0, 3);
            var first = 0;
            var second = 0;
            var answer = 0;
            switch (operation)
            {
                case MathOperation.Add:
                    first = Random.Shared.Next(5, 500);
                    second = Random.Shared.Next(5, 500);
                    answer = first + second;
                    break;
                case MathOperation.Subtract:
                    first = Random.Shared.Next(5, 500);
                    second = Random.Shared.Next(5, first);
                    answer = first - second;
                    break;

                case MathOperation.Multiply:
                    first = Random.Shared.Next(2, 100);
                    second = Random.Shared.Next(2, 15);
                    answer = first * second;
                    break;
            }

            return new MathProblem()
            {
                First = first,
                Second = second,
                Operation = operation,
                Answer = answer
            };
        }

        public bool Solve(int answer) => Answer == answer;
    }

    private MathProblem _current;
    private int _solved;

    public override async Task Enter(UserStateManager manager, long userId, TelegramBotClient client)
    {
        _ = base.Enter(manager, userId, client);
        await Client.SendTextMessageAsync(userId, $"Для завершения игры нужно ввести команду /math ещё раз");
        await NextProblem();
    }

    public override async Task Exit() => await Client.SendTextMessageAsync(UserId, $"Игра закончена. Решено: `{_solved}`", ParseMode.Markdown);

    public override async void Update(Message message)
    {
        if (int.TryParse(message.Text, out var answer) == false)
        {
            await Client.SendTextMessageAsync(UserId, "В ответе должно быть `целое число`", ParseMode.Markdown);
            return;
        }

        if (_current.Solve(answer) == false)
        {
            await Client.SendTextMessageAsync(UserId, "Ответ неверный");
            return;
        }

        _solved++;
        await NextProblem();
    }

    private async Task NextProblem()
    {
        _current = MathProblem.Create();

        var operation = _current.Operation switch
        {
            MathOperation.Add => '+',
            MathOperation.Subtract => '-',
            MathOperation.Multiply => '*',
            _ => '?',
        };

        await Client.SendTextMessageAsync(UserId, $"{_current.First} {operation} {_current.Second} = <code>?</code>", ParseMode.Html);
    }
}
