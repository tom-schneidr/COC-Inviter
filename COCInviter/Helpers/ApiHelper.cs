using Discord;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reactive.Joins;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Threading;

namespace COCInviter.Helpers
{
  internal class ApiHelper
  {
    private MainWindow _mainWindow;

    public ApiHelper(MainWindow window)
    {
      _mainWindow = window;
    }

    public async void FindPlayersAsync()
    {
      string lastFirstClan = "";
      while (true)
      {
        List<dynamic> clans = await GetListOfClansAsync();
        if (clans == null) continue;
        if (clans[0].tag == lastFirstClan) continue;
        lastFirstClan = clans[0].tag;
        foreach (dynamic clan in clans)
        {
          if (!CheckIsFindPlayersActive()) return;

          FindWantedPlayers(clan);
        }
        if (!CheckIsFindPlayersActive()) return;
      }
    }

    public async Task<List<dynamic>> GetListOfClansAsync()
    {
      using (HttpClient httpClient = CreateHttpClient())
      {
        UriBuilder uriBuilder = new UriBuilder("https://api.clashofclans.com/v1/clans?");
        uriBuilder.Query = "locationId=32000094&limit=1000";

        HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.Uri);
        if (response.IsSuccessStatusCode)
        {
          string jsonResponse = await response.Content.ReadAsStringAsync();
          dynamic jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
          JArray itemsArray = jsonObject.items as JArray;
          return itemsArray.ToObject<List<dynamic>>();
        }
      }
      return null;
    }

    public async void FindWantedPlayers(dynamic clan)
    {
      if (!IsClanEligible(clan)) return;

      List<dynamic> members = await GetMembersOfClanAsync((string)clan.tag);
      if (members == null) return;
      foreach (dynamic member in members)
      {
        dynamic playerDetails = await FetchPlayerDetailsAsync((string)member.tag);
        if (IsPlayerEligible(playerDetails))
        {
          AddPlayerToInviteList(playerDetails);
        }
      }
      
    }

    private bool IsClanEligible(dynamic clan)
    {
      if (_mainWindow.blacklistedClans.Contains((string)clan.tag)) return false;

      if (clan.chatLanguage?.name != "Deutsch") return false;

      int league = clan.warLeague?.id ?? 0;
      return league <= _mainWindow.maxLeague;
    }

    private async Task<List<dynamic>> GetMembersOfClanAsync(string clanTag)
    {
      using (HttpClient httpClient = CreateHttpClient())
      {
        UriBuilder uriBuilder = new UriBuilder($"https://api.clashofclans.com/v1/clans/%23{clanTag.Replace("#", "")}/members");

        HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.Uri);

        if (response.IsSuccessStatusCode)
        {
          string jsonResponse = await response.Content.ReadAsStringAsync();
          dynamic jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);
          JArray itemsArray = jsonObject.items as JArray;
          return itemsArray.ToObject<List<dynamic>>();
        }
      }
      return null;
    }

    private async Task<dynamic> FetchPlayerDetailsAsync(string playerTag)
    {
      using (HttpClient httpClient = CreateHttpClient())
      {
        UriBuilder uriBuilder = new UriBuilder($"https://api.clashofclans.com/v1/players/%23{playerTag.Replace("#", "")}");

        HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.Uri);

        if (response.IsSuccessStatusCode)
        {
          string jsonResponse = await response.Content.ReadAsStringAsync();
          return JsonConvert.DeserializeObject<dynamic>(jsonResponse);
        }
      }
      return null;
    }

    private bool IsPlayerEligible(dynamic playerDetails)
    {
      if (playerDetails == null) return false;

      string memberTag = playerDetails["tag"];
      if (_mainWindow.uniquePlayers.Contains(memberTag)) return false;
      if (_mainWindow.blacklistedPlayers.Contains(memberTag)) return false;

      string role = playerDetails["role"];
      if (!_mainWindow.acceptedRoles.Contains(role.ToLower())) return false;

      int level = playerDetails["expLevel"];
      if (level < _mainWindow.minLevel) return false;

      return IsPlayerStatsEligible(playerDetails);
    }

    private bool IsPlayerStatsEligible(dynamic playerDetails)
    {
      int thLevel = playerDetails.townHallLevel;
      int attacks = playerDetails.attackWins;
      int trophies = playerDetails.trophies;
      int donations = playerDetails.achievements[14]?.value ?? 0;

      if (thLevel < _mainWindow.minTh || attacks < _mainWindow.minAttacks || trophies < _mainWindow.minTrophies || donations < _mainWindow.minDonations) return false;

      int king = (playerDetails.heroes.Count > 0 && playerDetails.heroes[0] != null) ? playerDetails.heroes[0].level : 0;
      int queen = (playerDetails.heroes.Count > 1 && playerDetails.heroes[1] != null) ? playerDetails.heroes[1].level : 0;
      int warden = (playerDetails.heroes.Count > 2 && playerDetails.heroes[2] != null) ? playerDetails.heroes[2].level : 0;
      int champion = (playerDetails.heroes.Count > 4 && playerDetails.heroes[4] != null) ? playerDetails.heroes[4].level : 0;


      return king >= _mainWindow.minKing && queen >= _mainWindow.minQueen && warden >= _mainWindow.minWarden && champion >= _mainWindow.minChampion;
    }

    private void AddPlayerToInviteList(dynamic playerDetails)
    {
      string memberTag = playerDetails["tag"];

      _mainWindow.Dispatcher.Invoke(() =>
      {
        _mainWindow.uniquePlayers.Add(memberTag);

        _mainWindow.players.Add(new Player
        {
          Tag = memberTag,
          Townhall = playerDetails.townHallLevel,
          Level = playerDetails.expLevel,
          Queen = playerDetails.heroes[1]?.level ?? 0,
          King = playerDetails.heroes[0]?.level ?? 0,
          Warden = playerDetails.heroes[2]?.level ?? 0,
          Champion = playerDetails.heroes[4]?.level ?? 0,
          Attacks = playerDetails.attackWins,
          Trophies = playerDetails.trophies,
          Donations = playerDetails.achievements[14]?.value ?? 0
        });

        UpdateUI();
      });
    }

    private void UpdateUI()
    {
      _mainWindow.sessionPlayersFound++;
      _mainWindow.SessionPlayersFoundText.Content = $"Session players found: {_mainWindow.sessionPlayersFound}";
      _mainWindow.PlayersToInviteText.Content = $"Players to invite: {_mainWindow.players.Count}";
      _mainWindow.EstimatedTimeLeftText.Content = $"Estimated invite time: {(int)Math.Floor(_mainWindow.players.Count * 2.9 / 60)}m {(int)Math.Floor((_mainWindow.players.Count * 2.9) % 60)}s";
      _mainWindow.totalPlayersFound++;
      _mainWindow.TotalPlayersFoundText.Content = $"Total players found: {_mainWindow.totalPlayersFound}";
    }

    private HttpClient CreateHttpClient()
    {
      HttpClient httpClient = new HttpClient();
      httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
      httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + _mainWindow.API_TOKEN);
      return httpClient;
    }

    private bool CheckIsFindPlayersActive()
    {
      bool isChecked = false;
      _mainWindow.Dispatcher.Invoke(() =>
      {
        if (_mainWindow.FindPlayersCheckBox.IsChecked == true) isChecked = true;
      });
      return isChecked;
    }

    public async Task<dynamic> FetchClanDetailsAsync(string clanTag)
    {
      Console.Write("Api test");
      using (HttpClient httpClient = CreateHttpClient())
      {
        UriBuilder uriBuilder = new UriBuilder($"https://api.clashofclans.com/v1/clans/%23{clanTag.Replace("#", "")}");
        
        HttpResponseMessage response = await httpClient.GetAsync(uriBuilder.Uri);
        Console.WriteLine(response.ToString());
        if (response.IsSuccessStatusCode)
        {
          string jsonResponse = await response.Content.ReadAsStringAsync();
          Console.WriteLine(jsonResponse);
          return JsonConvert.DeserializeObject<dynamic>(jsonResponse);
        }
      }
      return null;
    }
  }
}
