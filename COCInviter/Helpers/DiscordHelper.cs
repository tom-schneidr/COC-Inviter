using Discord.WebSocket;
using Discord;
using Discord.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
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
      _services = ConfigureServices();

      _client = _services.GetRequiredService<DiscordSocketClient>();
      _commands = _services.GetRequiredService<InteractionService>();

      _client.Log += LogAsync;
      _commands.Log += LogAsync;

      Env.Load("../../../.env");
      string botToken = Environment.GetEnvironmentVariable("BOT_TOKEN");

      await _client.LoginAsync(TokenType.Bot, botToken);
      await _client.StartAsync();

      await InstallCommandsAsync();

      Dispatcher dispatcher = _mainWindow.Dispatcher;
      _ = StartUpdateDiscordPresence(dispatcher);
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
