using System;
using System.Threading;
using Telegram.Bot;
using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using Microsoft.VisualBasic;
using System.Collections.Specialized;
using static System.Runtime.InteropServices.JavaScript.JSType;

using var cts = new CancellationTokenSource();
var bot = new TelegramBotClient("7576584257:AAHP8B5Km2O50-yX_L6LMPtPB7cWKU_F9eE", cancellationToken: cts.Token);
var me = await bot.GetMeAsync();
Dictionary<string, string> UserName = new Dictionary<string, string>();
bool nameExists1 = false;
var doo = "";
var time = 0;

bot.OnMessage += OnName;
bot.OnMessage += OnName1;
bot.OnMessage += OnMessage;
bot.OnMessage += OnReminder;


Console.WriteLine($"@{me.Username} is running... Press Enter to terminate");
Console.ReadLine();
cts.Cancel();
async Task OnName(Message msg, UpdateType type)
{
    nameExists1 = UserName.ContainsKey("name");
    if (nameExists1 != true && msg.Text == "/start")
    {
        await bot.SendTextMessageAsync(msg.Chat, "<i>Здравствуйте, как вас зовут,</i>", parseMode: ParseMode.Html);
        nameExists1 = UserName.ContainsKey("name");
        
    }
        
}
async Task OnName1(Message msg, UpdateType type)
{
    bool nameExists1 = UserName.ContainsKey(Convert.ToString(msg.From));
    if (nameExists1 != true)
    {
        if (msg.Text != "/start" && msg.Text != "/task")
        {
            for (int i = 0; i < msg.Text.Length; i++)
            {
                if (msg.Text[i] >= '0' && msg.Text[i] <= '9')
                {
                    await bot.SendTextMessageAsync(msg.Chat, "<b>Напишите нормальное имя!</b>", parseMode: ParseMode.Html);
                    break;
                }
                else
                {
                    UserName.Add(Convert.ToString(msg.From), msg.Text);
                    bot.OnMessage -= OnName;

                }
            }
            
        }

    }



}
async Task OnMessage(Message msg, UpdateType type)
{

    if (msg.Text == "/tasks")
    {
        await bot.SendTextMessageAsync(msg.Chat, $"Здравствуйте, {UserName[Convert.ToString(msg.From)]}", parseMode: ParseMode.Html);
        await bot.SendTextMessageAsync(msg.Chat, "<b>Выберите команду!</b>\n1.Привет\n2.Картинка\n3.Видео\n4.Стикер\n5.Кнопочки", parseMode: ParseMode.Html);

        bot.OnMessage += OnMessage1;

    }
}
async Task OnMessage1(Message msg, UpdateType type)
{
    switch (msg.Text)
    {
        case "Привет":
        case "1":
            await bot.SendTextMessageAsync(msg.Chat, $"Привет, {UserName[Convert.ToString(msg.From)]}");
            bot.OnMessage -= OnMessage1;
            break;
        case "Картинка":
        case "2":
            await bot.SendPhotoAsync(msg.Chat, "https://cdn.pixabay.com/photo/2017/04/11/21/34/giraffe-2222908_640.jpg");
            bot.OnMessage -= OnMessage1;
            break;
        case "Видео":
        case "3":
            await bot.SendVideoAsync(msg.Chat, "https://3d-galleru.ru/cards/23/65/321temu6718098hq5j44u0oae/privet-xoroshego-nastroeniya.mp4");
            bot.OnMessage -= OnMessage1;
            break;
        case "Стикер":
        case "4":
            await bot.SendStickerAsync(msg.Chat, "https://t.me/TgSticker/38034");
            bot.OnMessage -= OnMessage1;
            break;
        case "Кнопочки":
        case "5":
            bot.OnUpdate += OnUpdateButton;
            await bot.SendTextMessageAsync(msg.Chat, "Супер кнопки)",
           replyMarkup: new InlineKeyboardMarkup().AddButtons("Привет", "Пока"));
            bot.OnMessage -= OnMessage1;
            break;
    }

}
async Task OnUpdateButton(Update update)
{
    if (update is { CallbackQuery: { } query }) // non-null CallbackQuery
    {
        await bot.SendTextMessageAsync(query.Message!.Chat, $"Ты нажал {query.Data}");
        bot.OnUpdate -= OnUpdateButton;
    }
}

async Task OnReminder(Message msg, UpdateType type)
{

    if (msg.Text == "/reminder")
    {
        Console.WriteLine(msg.Chat);
        bot.OnUpdate += OnUpdateReminder;
        await bot.SendTextMessageAsync(msg.Chat, "Добавить напоминание или убрать",
           replyMarkup: new InlineKeyboardMarkup().AddButtons("Создать", "Удалить"));
    }
}
async Task OnUpdateReminder(Update update)
{
    if (update is { CallbackQuery: { } query }) // non-null CallbackQuery
    {
        if(query.Data == "Создать")
        {

            await bot.SendTextMessageAsync(query.Message.Chat, "Напишите что вам прислать)");
            bot.OnMessage += OnReminder1;
            bot.OnUpdate -= OnUpdateReminder;
        }
    }
}
async Task OnReminder1(Message msg, UpdateType type)
{
    doo = msg.Text;
    await bot.SendTextMessageAsync(msg.Chat, "Напишите через сколько минут вам прислать)");
    bot.OnMessage += OnReminder2;
    bot.OnMessage -= OnReminder1;
}
async Task OnReminder2(Message msg, UpdateType type)
{
    string[] values = { msg.Text };
    foreach (var value in values)
    {
        int number;

        bool success = int.TryParse(value, out number);
        if (success)
        {
            Console.WriteLine($"Converted '{value}' to {number}.");
            time = Convert.ToInt32(msg.Text);
        }
        else
        {
            await bot.SendTextMessageAsync(msg.Chat, "Напиши через сколько минут. Цыфрами!!!");
            bot.OnMessage += OnReminder1;
            bot.OnMessage -= OnReminder2;
        }
    }
    Thread.Sleep(1000 * 60 * time);
    await bot.SendTextMessageAsync(msg.Chat, doo);
    bot.OnMessage -= OnReminder2;


}

