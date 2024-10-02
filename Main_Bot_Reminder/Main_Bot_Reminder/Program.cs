using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;

class Program
{
    private static TelegramBotClient botClient;
    private static Dictionary<long, List<(string reminder, DateTime dueTime)>> reminders = new Dictionary<long, List<(string reminder, DateTime dueTime)>>();
    private static Dictionary<long, string> userNames = new Dictionary<long, string>();

    static async Task Main()
    {
        CancellationTokenSource cts = new CancellationTokenSource();
        botClient = new TelegramBotClient("7169323371:AAFcVVtYxw8iTHl_5jR6btTGK-m5h6-iZQA", cancellationToken: cts.Token);

        var me = await botClient.GetMeAsync();
        Console.WriteLine($"Bot id: {me.Id}. Bot name: {me.FirstName}");

        var receiverOptions = new ReceiverOptions
        {
            AllowedUpdates = { } // Получаем все типы обновлений
        };

        botClient.StartReceiving(
            HandleUpdateAsync,
            HandleErrorAsync,
            receiverOptions,
            cancellationToken: cts.Token
        );

        Console.WriteLine("Bot started...");

        // Запуск задачи для проверки напоминаний
        _ = CheckReminders(cts.Token);

        Console.CancelKeyPress += (s, e) => {
            e.Cancel = true;
            cts.Cancel();
        };

        await Task.Delay(-1, cts.Token);
    }

    private static async Task HandleUpdateAsync(ITelegramBotClient botClient, Update update, CancellationToken cancellationToken)
    {
        if (update.Type == UpdateType.Message && update.Message?.Text != null)
        {
            var chatId = update.Message.Chat.Id;
            var text = update.Message.Text.Trim();

            await HandleTextMessage(chatId, text, cancellationToken);
        }
        else if (update.Type == UpdateType.CallbackQuery)
        {
            await HandleCallbackQuery(update.CallbackQuery, cancellationToken);
        }
    }

    private static async Task HandleTextMessage(long chatId, string text, CancellationToken cancellationToken)
    {
        if (text.Equals("/start", StringComparison.OrdinalIgnoreCase))
        {
            if (!userNames.ContainsKey(chatId))
            {
                await botClient.SendTextMessageAsync(chatId, "Привет! Как тебя зовут?", cancellationToken: cancellationToken);
                userNames[chatId] = null; // Инициализация
            }
        }
        else if (userNames.ContainsKey(chatId) && userNames[chatId] == null)
        {
            userNames[chatId] = text;
            await botClient.SendTextMessageAsync(chatId, $"Отлично, запомнил, что тебя зовут {text}!", cancellationToken: cancellationToken);
        }
        else if (text.Equals("/tasks", StringComparison.OrdinalIgnoreCase))
        {
            await ShowTasks(chatId, cancellationToken);
        }
        else if (text.StartsWith("/remind "))
        {
            await HandleRemindCommand(chatId, text, cancellationToken);
        }
        else if (text.Equals("/list", StringComparison.OrdinalIgnoreCase))
        {
            await ShowReminders(chatId, cancellationToken);
        }
        else if (text.Equals("/help", StringComparison.OrdinalIgnoreCase))
        {
            await ShowHelp(chatId, cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Команда не распознана. Используйте /help, чтобы узнать доступные команды.", cancellationToken: cancellationToken);
        }
    }

    private static async Task HandleCallbackQuery(CallbackQuery callbackQuery, CancellationToken cancellationToken)
    {
        var chatId = callbackQuery.Message.Chat.Id;

        switch (callbackQuery.Data)
        {
            case "Привет":
                var userName = userNames.ContainsKey(chatId) ? userNames[chatId] : "друг";
                await botClient.SendTextMessageAsync(chatId, $"Здравствуй, {userName}!", cancellationToken: cancellationToken);
                break;
            case "Картинка":
                await botClient.SendPhotoAsync(chatId, "https://cdn.pixabay.com/photo/2017/04/11/21/34/giraffe-2222908_640.jpg");
                break;
            case "Видео":
                await botClient.SendVideoAsync(chatId, "https://3d-galleru.ru/cards/23/65/321temu6718098hq5j44u0oae/privet-xoroshego-nastroeniya.mp4"); break;
            case "Стикер":
                await botClient.SendStickerAsync(chatId, "https://t.me/TgSticker/38034", cancellationToken: cancellationToken); // Укажите ID стикера
                break;
            case "Кнопки":
                var inlineKeyboard = new InlineKeyboardMarkup(new[]
                {
                    new[] { InlineKeyboardButton.WithCallbackData("Кнопка 1", "button1") },
                    new[] { InlineKeyboardButton.WithCallbackData("Кнопка 2", "button2") }
                });

                await botClient.SendTextMessageAsync(chatId, "Вот сообщение с кнопками:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
                break;
        }

        await botClient.AnswerCallbackQueryAsync(callbackQuery.Id, cancellationToken: cancellationToken);
    }

    private static async Task ShowTasks(long chatId, CancellationToken cancellationToken)
    {
        var inlineKeyboard = new InlineKeyboardMarkup(new[]
        {
            new[] { InlineKeyboardButton.WithCallbackData("Привет", "Привет") },
            new[] { InlineKeyboardButton.WithCallbackData("Картинка", "Картинка") },
            new[] { InlineKeyboardButton.WithCallbackData("Видео", "Видео") },
            new[] { InlineKeyboardButton.WithCallbackData("Стикер", "Стикер") },
            new[] { InlineKeyboardButton.WithCallbackData("Кнопки", "Кнопки") }
        });

        await botClient.SendTextMessageAsync(chatId, "Выберите задание:", replyMarkup: inlineKeyboard, cancellationToken: cancellationToken);
    }

    private static async Task HandleRemindCommand(long chatId, string text, CancellationToken cancellationToken)
    {
        var parts = text[8..].Trim().Split(' ', 2);
        if (parts.Length != 2)
        {
            await botClient.SendTextMessageAsync(chatId, "Формат команды: /remind [время] [напоминание]. Пример: /remind 5m Напоминание", cancellationToken: cancellationToken);
            return;
        }

        var timeSpan = ParseTimeSpan(parts[0]);
        if (timeSpan.HasValue)
        {
            var reminder = parts[1];
            var dueTime = DateTime.UtcNow.Add(timeSpan.Value);

            if (!reminders.ContainsKey(chatId))
            {
                reminders[chatId] = new List<(string reminder, DateTime dueTime)>();
            }

            reminders[chatId].Add((reminder, dueTime));
            await botClient.SendTextMessageAsync(chatId, $"Напоминание добавлено: '{reminder}' через {timeSpan.Value.TotalMinutes} минут.", cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "Неправильный формат времени. Используйте '5m' для минут, '1h' для часов.", cancellationToken: cancellationToken);
        }
    }

    private static async Task ShowReminders(long chatId, CancellationToken cancellationToken)
    {
        if (reminders.ContainsKey(chatId) && reminders[chatId].Any())
        {
            var reminderList = string.Join("\n", reminders[chatId].Select(r => $"{r.reminder} (время: {r.dueTime})"));
            await botClient.SendTextMessageAsync(chatId, "Ваши напоминания:\n" + reminderList, cancellationToken: cancellationToken);
        }
        else
        {
            await botClient.SendTextMessageAsync(chatId, "У вас нет напоминаний.", cancellationToken: cancellationToken);
        }
    }

    private static async Task CheckReminders(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            foreach (var userId in reminders.Keys.ToList())
            {
                var userReminders = reminders[userId];
                var dueReminders = userReminders.Where(r => r.dueTime <= DateTime.UtcNow).ToList();

                foreach (var reminder in dueReminders)
                {
                    await botClient.SendTextMessageAsync(userId, $"Напоминание: {reminder.reminder}", cancellationToken: cancellationToken);
                    userReminders.Remove(reminder);
                }
            }

            await Task.Delay(1000, cancellationToken); // Проверка каждую секунду для более точного определения времени
        }
    }

    private static async Task ShowHelp(long chatId, CancellationToken cancellationToken)
    {
        var helpMessage = @"
Доступные команды:
1. /start - Начать взаимодействие с ботом и ввести свое имя.
2. /tasks - Показать доступные задания.
3. /remind [время] [напоминание] - Добавить напоминание. Пример: /remind 5m Напоминание.
4. /list - Показать все ваши добавленные напоминания.
5. /help - Показать список доступных команд.";

        await botClient.SendTextMessageAsync(chatId, helpMessage, cancellationToken: cancellationToken);
    }

    private static TimeSpan? ParseTimeSpan(string timeString)
    {
        if (timeString.EndsWith("m"))
        {
            if (int.TryParse(timeString[0..^1], out int minutes))
                return TimeSpan.FromMinutes(minutes);
        }
        else if (timeString.EndsWith("h"))
        {
            if (int.TryParse(timeString[0..^1], out int hours))
                return TimeSpan.FromHours(hours);
        }
        return null;
    }

    private static Task HandleErrorAsync(ITelegramBotClient botClient, Exception exception, CancellationToken cancellationToken)
    {
        Console.WriteLine(exception);
        return Task.CompletedTask;
    }
}
