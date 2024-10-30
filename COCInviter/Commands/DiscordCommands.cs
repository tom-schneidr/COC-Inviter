using Discord;
using Discord.Interactions;
using System;
using System.Threading.Tasks;
using COCInviter.Helpers;

namespace COCInviter.Commands
{
  public class DiscordCommands : InteractionModuleBase<SocketInteractionContext>
  {
    private readonly MainWindow _mainWindow;
    private readonly ApiHelper apiHelper;

    // Constructor to accept MainWindow instance
    public DiscordCommands(MainWindow mainWindow)
    {
      _mainWindow = mainWindow;
      apiHelper = new ApiHelper(mainWindow);
    }

    [SlashCommand("addblacklistedplayer", "Add a player to the blacklist")]
    public async Task AddPlayerToBlacklist(string playerTag)
    {
      if (!_mainWindow.blacklistedPlayers.Contains(playerTag))
      {
        _mainWindow.blacklistedPlayers.Add(playerTag);
        await RespondAsync("Player: " + playerTag + " is now blacklisted and wont be invited anymore.");
      }
      else
      {
        await RespondAsync("Player: " + playerTag + " is already blacklisted.");
      }
    }

    [SlashCommand("removeblacklistedplayer", "Remove player from blacklist.")]
    public async Task RemovePlayerFromBlacklist(string clanTag)
    {
      if (_mainWindow.blacklistedPlayers.Contains(clanTag))
      {
        _mainWindow.blacklistedPlayers.Remove(clanTag);
        await RespondAsync("Player: " + clanTag + " is no longer blacklisted.");
      }
      else
      {
        await RespondAsync("Player: " + clanTag + " isn't blacklisted.");
      }
    }

    [SlashCommand("showblacklistedplayers", "Print all blacklisted players")]
    public async Task GetBlacklistedPlayers()
    {
      string message = "";
      foreach (string s in _mainWindow.blacklistedPlayers){

        message += "Player: " + s + "\n";
      }
      await RespondAsync(message);
    }

    [SlashCommand("addblacklistedclan", "Add a clan to the blacklist")]
    public async Task AddClanToBlacklist(string clanTag)
    {
      if (!_mainWindow.blacklistedClans.Contains(clanTag))
      {
        _mainWindow.blacklistedClans.Add(clanTag);
        await RespondAsync("Clan: " + clanTag + " is now blacklisted.");
      }
      else
      {
        await RespondAsync("Clan: " + clanTag + " is already blacklisted.");
      }
    }

    [SlashCommand("removeblacklistedclan", "Remove clan from blacklist.")]
    public async Task RemoveClanFromBlacklist(string clanTag)
    {
      if (_mainWindow.blacklistedClans.Contains(clanTag))
      {
        _mainWindow.blacklistedClans.Remove(clanTag);
        await RespondAsync("Clan: " + clanTag + " is no longer blacklisted.");
      }
      else
      {
        await RespondAsync("Clan: " + clanTag + " isn't blacklisted.");
      }
    }

    [SlashCommand("showblacklistedclans", "Print all blacklisted clans")]
    public async Task GetBlacklistedClans()
    {
      if (_mainWindow.blacklistedClans.Count == 0)
      {
        await RespondAsync("No clan is currently blacklisted.");
      }
      string message = "Clans:\n";
      foreach (string s in _mainWindow.blacklistedClans)
      {
        dynamic clanInfo = await apiHelper.FetchClanDetailsAsync(s);
        if (clanInfo == null) continue;
        string clanName = clanInfo.name;
        message += "- " + clanName + " (" + s +")\n";
      }
      await RespondAsync(message);
    }
  }
}