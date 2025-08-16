using Discord.WebSocket;
using Discord;
using System;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Reflection;
using Discord.Interactions;
using Microsoft.Extensions.DependencyInjection;
using COCInviter.Commands;
using DotNetEnv;

namespace COCInviter.Helpers
{
  public class DiscordHelper
  {
    private static DiscordSocketClient _client;
    private static InteractionService _commands;
    private IServiceProvider _services;
    private MainWindow _mainWindow;

    public DiscordHelper(MainWindow window)
    {
      _mainWindow = window;
    }

    public async void InitializeDiscordBot()
    {
      _client = new DiscordSocketClient(new DiscordSocketConfig
      {
        GatewayIntents = GatewayIntents.AllUnprivileged
      });

      _services = ConfigureServices();

      _client = _services.GetRequiredService<DiscordSocketClient>();
      _commands = _services.GetRequiredService<InteractionService>();

      _client.Log += LogAsync;
      _commands.Log += LogAsync;

      _client.Disconnected += OnDisconnectedAsync;

      await InstallCommandsAsync();

      Dispatcher dispatcher = _mainWindow.Dispatcher;
      _ = StartUpdateDiscordPresence(dispatcher);

      await StartBotAsync();

      // Keep the program running
      await Task.Delay(-1);
    }

    private async Task StartBotAsync()
    {
      try
      {
        Env.Load("../../../.env");
        string token = Environment.GetEnvironmentVariable("BOT_TOKEN");
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();

        Console.WriteLine("Bot is running.");
      }
      catch (Exception ex)
      {
        Console.WriteLine($"Error starting bot: {ex.Message}");
      }
    }

    private async Task OnDisconnectedAsync(Exception ex)
    {
      Console.WriteLine($"Bot disconnected: {ex.Message}");

      // Attempt reconnection
      while (_client.ConnectionState != ConnectionState.Connected)
      {
        Console.WriteLine("Attempting to reconnect...");
        try
        {
          await _client.StopAsync();
          await Task.Delay(5000); // Wait for 5 seconds before retrying
          await StartBotAsync();
        }
        catch (Exception reconnectEx)
        {
          Console.WriteLine($"Reconnection failed: {reconnectEx.Message}");
        }
      }

      Console.WriteLine("Reconnected successfully.");
    }

    private async Task InstallCommandsAsync()
    {
      var discordCommands = new DiscordCommands(_mainWindow);
      // Add the command modules
      await _commands.AddModulesAsync(Assembly.GetEntryAssembly(), _services);
      // Register commands when the bot is ready
      _client.Ready += async () =>
      {
        await _commands.RegisterCommandsGloballyAsync();
      };

      _client.InteractionCreated += HandleInteraction;
    }

    private async Task HandleInteraction(SocketInteraction interaction)
    {
      try
      {
        var context = new SocketInteractionContext(_client, interaction);
        await _commands.ExecuteCommandAsync(context, _services);
      }
      catch (Exception ex)
      {
        Console.WriteLine(ex);
      }
    }

    public async Task StartUpdateDiscordPresence(Dispatcher dispatcher)
    {
      while (true)
      {
        dispatcher.Invoke(() => { UpdateDiscordPresence(_mainWindow.totalPlayersInvited); });
        // Wait for 30 seconds
        await Task.Delay(TimeSpan.FromSeconds(30));
      }
    }

    // Function to update the playing status with number of players invited
    private static async void UpdateDiscordPresence(int numberOfPlayersInvited)
    {
      if (_client != null)
        await _client.SetGameAsync($"Invited {numberOfPlayersInvited} players!", type: ActivityType.Playing);
    }

    private Task LogAsync(LogMessage log)
    {
      Console.WriteLine(log.ToString());
      return Task.CompletedTask;
    }

    private IServiceProvider ConfigureServices()
    {
      var services = new ServiceCollection()
        .AddSingleton(_mainWindow) // If you need access to the MainWindow instance
        .AddSingleton<DiscordSocketClient>()
        .AddSingleton(x => new InteractionService(x.GetRequiredService<DiscordSocketClient>()))
        .AddSingleton<DiscordHelper>()
        .AddSingleton<DiscordCommands>()
        .BuildServiceProvider();

      return services;
    }
  }
}
