# TelegramBotExtensions

讓你快速的利用.NET搭建起自己的TelegramBot的擴充套件。

# 簡介

---

TelegramBotExtensions 是一個快速讓.NET開發者快速搭建起Telegram Bot Server Side的一個套件，主要提供兩種方式來接收使用者發出的訊息，分別為LongPolling 與 WebHook兩種機制，詳細可參考[Telegram Bot API的文件說明](https://core.telegram.org/bots/api#getting-updates)。



.NET 版本:  [**.NET Core 3.1**]()

[core/release-notes/3.1 at main · dotnet/core](https://github.com/dotnet/core/tree/main/release-notes/3.1)

Telegram Client端套件 :  [TelegramBot.Client]()

[GitHub - TelegramBots/Telegram.Bot: .NET Client for Telegram Bot API](https://github.com/TelegramBots/Telegram.Bot)

# 前置作業(取得token)

---

透過Telegram 提供的Bot Father 取得 Telegram Bot token，詳細步驟請參考此[連結]()

[Bots: An introduction for developers](https://core.telegram.org/bots#6-botfather)

只要簡單三行指令，就可以取得建立的Telegram Bot token

![Untitled](https://github.com/zccheng8320/TelegramBotExtensions/blob/main/Image/46ae48e47b5d4dcba5555ac6974eb26b.png?raw=trueg)

# LongPolling Quick Start

---

> 此章節的程式碼可參考[SimpleTelegramBot_LongPolling](https://github.com/zccheng8320/TelegramBotExtensions/tree/main/SimpleTelegramBot_LongPolling)。

1. 先建立一個.Net Console App Project
2. Add TelegramBotExtensions Reference。
3. 建立appsetting.json，並在TelegramApiToken 輸入你的telegram bot api token

    ```json
    {
        "Logging": {
            "LogLevel": {
                "Default": "Debug",
                "System": "Information",
                "Microsoft": "Information"
            }
        },
        "TelegramSetting": {
            "TelegramApiToken": "your api token"
        }
    }
    ```

4. 建立SampleUpdateHandler.cs，此class 需繼承IUpdateHandler。
    > [Update](https://core.telegram.org/bots/api#update)是telegram 用來傳遞使用者輸入的訊息至Telegram Bot的主要物件，因此在此你可以根據自己的需求來建立回傳給使用者的訊息。

    ```csharp
    public class SampleUpdateHandler :IUpdateHandler
        {
            private readonly ITelegramBotClient _client;

            public SampleUpdateHandler(ITelegramBotClient client)
            {
                _client = client;
            }
            public async Task Process(Update update)
            {
                var chatId = update.Message.Chat.Id;
                var userName = update.Message.From.FirstName + update.Message.From.LastName;
                var text = update.Message.Text;
                await _client.SendTextMessageAsync(chatId, $"Hello,{userName}.You said {text}");
            }
        }
    ```

5. 至Program.cs主程式將各服務加入DI Container之中

    ```csharp
    class Program
        {
            static async Task Main(string[] args)
            {
                await CreateHostBuilder(args).Build().RunAsync();
            }
            static IHostBuilder CreateHostBuilder(string[] args)
            {
                return Host.CreateDefaultBuilder(args)
                    .ConfigureServices((_, services) =>
                    {
                        // add telegram bot client
                        services.AddTelegramBotClient();
                        // register your update handeler
                        services.AddLongPolling<SampleUpdateHandler>(50);
                    });
            }
        }
    ```

    > 下方程式碼，泛行類別為IUpdateHandler，可替換成自己Implement的Class。
    而輸入的參數指的是一次request 要從Telegram API 中拿幾個Update物件，詳細可參考此連結。

    ```csharp
    services.AddLongPolling<SampleUpdateHandler>(50);
    ```

6. 測試。

![Untitled](https://raw.githubusercontent.com/zccheng8320/TelegramBotExtensions/main/Image/2791106c0738427aad65bc754b1896a1.png)

# Webhook Quick Start

---

> 此章節的程式碼可參考[SimpleTelegramBot_Webhook](https://github.com/zccheng8320/TelegramBotExtensions/tree/main/SimpleTelegramBot_Webhook)。

1. 建立任何一種.net Core Web應用程式，可以使用Web API的template即可。
2. Add TelegramBotExtensions Reference。
3. 建立appsetting.json，並在TelegramApiToken 輸入你的telegram bot api token、與webhook的Url。

    > 注意!! : Webhook的Url，必需可讓外網連線，測試時可以先使用[Ngrok](https://ngrok.com/)工具來讓外網可以連線本地網址。

    ```json
    {
    "Logging": {
        "LogLevel": {
        "Default": "Information",
        "Microsoft": "Warning",
        "Microsoft.Hosting.Lifetime": "Information"
        }
    },
    "AllowedHosts": "*",
    "TelegramSetting": {
        "TelegramApiToken": "your api token",
        "TelegramApiWebhookUrl": "your webhook url"
    }
    }
    ```

1.  建立SampleUpdateHandler.cs，此class 需繼承IUpdateHandler。

    > [Update](https://core.telegram.org/bots/api#update)是telegram 用來傳遞使用者輸入的訊息至Telegram Bot的主要物件，因此在此你可以根據自己的需求來建立回傳給使用者的訊息。

    ```csharp
    public class SampleUpdateHandler :IUpdateHandler
        {
            private readonly ITelegramBotClient _client;

            public SampleUpdateHandler(ITelegramBotClient client)
            {
                _client = client;
            }
            public async Task Process(Update update)
            {
                var chatId = update.Message.Chat.Id;
                var userName = update.Message.From.FirstName + update.Message.From.LastName;
                var text = update.Message.Text;
                await _client.SendTextMessageAsync(chatId, $"Hello,{userName}.You said {text}");
            }
        }
    ```

1. 設定StartUp.cs。

    > 將不必要的Services與Middleware刪除，並加入TelegramBotExtensions提供的Services與Middleware。

    ```csharp
    public class Startup
        {
            public Startup(IConfiguration configuration)
            {
                Configuration = configuration;
            }

            public IConfiguration Configuration { get; }

            // This method gets called by the runtime. Use this method to add services to the container.
            public void ConfigureServices(IServiceCollection services)
            {
                **services.AddTelegramBotClient();
                services.AddTelegramWebhook<SampleUpdateHandler>();**
            }

            // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
            public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
            {
                if (env.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                }
                //app.UseHttpsRedirection();
                app.UseRouting();
                //app.UseAuthorization();
                app.UseTelegramApiWebhookEndPoint();
            }
        }
    ```

6. 最後一步，調整Program.cs

    > host.Services.SetWebhookInfo() ，主要是執行Telegram API中的[setWebhook](https://core.telegram.org/bots/api#setwebhook)的動作(其中Url 參數使用appsetting.json的設定)。
    因此也可以不用加入這段，改用瀏覽器或者Postman的工具來完成設定，詳細可參考此[連結](https://core.telegram.org/bots/api#setwebhook)。

    ```csharp
    public class Program
        {
            public static async Task Main(string[] args)
            {
                var host = CreateHostBuilder(args).Build();
                await host.Services.SetWebhookInfo();
                await host.RunAsync();
            }

            public static IHostBuilder CreateHostBuilder(string[] args) =>
                Host.CreateDefaultBuilder(args)
                    .ConfigureWebHostDefaults(webBuilder =>
                    {
                        webBuilder.UseStartup<Startup>();
                    });
        }
    ```
