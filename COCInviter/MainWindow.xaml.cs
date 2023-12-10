using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Drawing;
using System.Windows.Forms;
using System.Net.Http;
using System.Xml.Linq;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Collections.Specialized;
using System.Security.Cryptography;

namespace COCInviter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
      [DllImport("user32.dll")]
      private static extern bool SetCursorPos(int x, int y);

      [DllImport("user32.dll")]
      private static extern void mouse_event(uint dwFlags, uint dx, uint dy, uint dwData, int dwExtraInfo);

      private const uint MOUSEEVENTF_LEFTDOWN = 0x02;
      private const uint MOUSEEVENTF_LEFTUP = 0x04;

      [DllImport("user32.dll")]
      public static extern short GetAsyncKeyState(int vKey);

      private int maxLeague;
      private List<string> acceptedRoles = new List<string>();
      private int minTh;
      private int minLevel;
      private int minAttacks;
      private int minKing;
      private int minQueen;
      private int minWarden;
      private int minChampion;
      private int minTrophies;
      private int minDonations;

      private ObservableCollection<Player> players { get; } = new ObservableCollection<Player>();
      private StringCollection uniquePlayers = new StringCollection();
      // Stats
      private int sessionPlayersFound = 0;
      private int sessionPlayersInvited = 0;
      // Total
      private int totalPlayersFound = 0;
      private int totalPlayersInvited = 0;

      private string API_TOKEN = "";
       
      public MainWindow()
      {
        Closing += ApplicationClosing;

        InitializeComponent();

        PlayerTable.ItemsSource = players;

        LoadSettings();

        PlayersToInviteText.Content = "Players to invite: " + players.Count;
        EstimatedTimeLeftText.Content = "Estimated time left: " + (int)Math.Floor(players.Count * 2.9 / 60) + "m " + (int)Math.Floor((players.Count * 2.9) % 60) + "s";

      Thread keybindListenerThread = new Thread(KeybindListener)
        {
          IsBackground = true
        };
        keybindListenerThread.Start();
      }

      private void ApplicationClosing(object sender, System.ComponentModel.CancelEventArgs e)
      {
        Properties.Settings.Default.totalPlayersFoundSetting = totalPlayersFound;
        Properties.Settings.Default.totalPlayersInvitedSetting = totalPlayersInvited;
        string serializedPlayers = JsonConvert.SerializeObject(players);
        Properties.Settings.Default.savedPlayersSetting = serializedPlayers;
        Properties.Settings.Default.uniquePlayersSetting = uniquePlayers;

        Properties.Settings.Default.Save();
    }

      public void KeybindListener()
        {
          while (true)
          {
            // Minimum CPU usage
            Thread.Sleep(150);
            // Loop through all possible keys
            for (int i = 120; i < 130; i++)
            {
              int keyState = GetAsyncKeyState(i);

              // Check if a set keybind is being pressed, trigger event if detected
              if (keyState != 0)
              {
                if (Enum.GetName(typeof(Keys), i) == "F9")
                {
                  Dispatcher.Invoke(() =>
                  {
                    if (FindPlayersCheckBox.IsChecked is true) FindPlayersCheckBox.IsChecked = false;
                    else if (!FindPlayersCheckBox.IsChecked is true) FindPlayersCheckBox.IsChecked = true;
                  });
                }
                if (Enum.GetName(typeof(Keys), i) == "F10")
                {
                  Dispatcher.Invoke(() =>
                  {
                    if (InvitePlayersCheckBox.IsChecked is true) InvitePlayersCheckBox.IsChecked = false;
                    else if (!InvitePlayersCheckBox.IsChecked is true) InvitePlayersCheckBox.IsChecked = true;
                  });
                }
              }
            }
          }
        }


      public async void FindPlayersAsync()
      {
      string lastFirstClan = "";
        bool isChecked = true;
        while (isChecked)
        {
          using (HttpClient httpClient = new HttpClient())
          {
            try
            {
              // API endpoint with query parameters
              UriBuilder apiUrl = new UriBuilder("https://api.clashofclans.com/v1/clans?");
              apiUrl.Query = "locationId=32000094&limit=1000"; // Add query parameters here

              httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
              httpClient.DefaultRequestHeaders.Add("Authorization", "Bearer " + API_TOKEN);

              // Send GET request with parameters
              HttpResponseMessage response = await httpClient.GetAsync(apiUrl.Uri);

              if (response.IsSuccessStatusCode)
              {
                // Read the JSON response as a string
                string jsonResponse = await response.Content.ReadAsStringAsync();

                // Parse the JSON response as a JObject
                dynamic jsonObject = JsonConvert.DeserializeObject<dynamic>(jsonResponse);

                // Access the list of objects (clans in this case) and iterate through them
                if (jsonObject.items[0].tag != lastFirstClan)
                {
                  lastFirstClan = jsonObject.items[0].tag;
                  foreach (dynamic clan in jsonObject.items)
                  {
                    Dispatcher.Invoke(() =>
                    {
                      isChecked = FindPlayersCheckBox.IsChecked is true;
                    });
                    if (!isChecked) return;
                    string language = "";
                    int league = 0;
                    string clanTag = clan.tag;

                    try
                    {
                      language = clan.chatLanguage.name;

                      if (language != "Deutsch") continue;
                    }
                    catch { }

                    try
                    {
                      league = clan.warLeague.id;
                      if (league > maxLeague) continue;
                    }
                    catch { }

                    using (HttpClient httpClient2 = new HttpClient())
                    {
                      try
                      {
                        // API endpoint with query parameters
                        UriBuilder apiUrl2 = new UriBuilder($"https://api.clashofclans.com/v1/clans/%23{clanTag.Replace("#", "")}/members");

                        httpClient2.DefaultRequestHeaders.Add("Accept", "application/json");
                        httpClient2.DefaultRequestHeaders.Add("Authorization", "Bearer " + API_TOKEN);

                        // Send GET request with parameters
                        HttpResponseMessage response2 = await httpClient2.GetAsync(apiUrl2.Uri);

                        if (response2.IsSuccessStatusCode)
                        {
                          // Read the JSON response as a string
                          string jsonResponse2 = await response2.Content.ReadAsStringAsync();

                          // Parse the JSON response as a JObject
                          dynamic jsonObject2 = JsonConvert.DeserializeObject<dynamic>(jsonResponse2);

                          // Access the list of objects (clans in this case) and iterate through them
                          foreach (dynamic member in jsonObject2.items)
                          {
                            if (!isChecked) return;
                            int level = member["expLevel"];
                            string memberTag = member["tag"];
                            if (uniquePlayers.Contains(memberTag)) continue;
                            string role = member["role"];
                            if (!acceptedRoles.Contains(role.ToLower())) continue;

                            if (level < minLevel) continue;

                            using (HttpClient httpClient3 = new HttpClient())
                            {
                              try
                              {
                                // API endpoint with query parameters
                                UriBuilder apiUrl3 = new UriBuilder($"https://api.clashofclans.com/v1/players/%23{memberTag.Replace("#", "")}");

                                httpClient3.DefaultRequestHeaders.Add("Accept", "application/json");
                                httpClient3.DefaultRequestHeaders.Add("Authorization", "Bearer " + API_TOKEN);

                                // Send GET request with parameters
                                HttpResponseMessage response3 = await httpClient3.GetAsync(apiUrl3.Uri);

                                if (response3.IsSuccessStatusCode)
                                {
                                  // Read the JSON response as a string
                                  string jsonResponse3 = await response3.Content.ReadAsStringAsync();

                                  // Parse the JSON response as a JObject
                                  dynamic player = JsonConvert.DeserializeObject<dynamic>(jsonResponse3);

                                  int thLevel = player.townHallLevel;
                                  if (thLevel < minTh) continue;

                                  int attacks = player.attackWins;
                                  if (attacks < minAttacks) continue;

                                  int trophies = player.trophies;
                                  if (trophies < minTrophies) continue;

                                  int donations = player.achievements[14].value;
                                  if (donations < minDonations) continue;

                                  int king = 0;
                                  int queen = 0;
                                  int warden = 0;
                                  int champion = 0;
                                  try
                                  {
                                    king = player.heroes[0].level;
                                    queen = player.heroes[1].level;
                                    warden = player.heroes[2].level;
                                    champion = player.heroes[4].level;
                                  }
                                  catch { }

                                  if (king < minKing || queen < minQueen || warden < minWarden || champion < minChampion) continue;

                                  Dispatcher.Invoke(() =>
                                  {
                                    uniquePlayers.Add(memberTag);

                                    players.Add(new Player { Tag = memberTag, Townhall = thLevel, Level = level, Queen = queen, King = king, Warden = warden, Champion = champion, Attacks = attacks, Trophies = trophies, Donations = donations });
                                    sessionPlayersFound++;
                                    SessionPlayersFoundText.Content = "Session players found: " + sessionPlayersFound;
                                    PlayersToInviteText.Content = "Players to invite: " + players.Count;
                                    EstimatedTimeLeftText.Content = "Estimated invite time: " + (int)Math.Floor(players.Count * 2.9 / 60) + "m " + (int)Math.Floor((players.Count * 2.9) % 60) + "s";
                                    totalPlayersFound++;
                                    TotalPlayersFoundText.Content = "Total players found: " + totalPlayersFound;
                                    isChecked = FindPlayersCheckBox.IsChecked is true;
                                  });
                                  if (!isChecked) return;
                                }
                              }
                              catch { }
                            }
                          }
                        }
                      }
                      catch { }
                    }

                  }
                }
              }
            }
            catch { }
          }
        }
      }

      public void InvitePlayers()
      {
        bool isChecked = true;
        while (isChecked)
        {
          if (players.Count == 0) continue;
          Dispatcher.Invoke(() =>
          {
            isChecked = InvitePlayersCheckBox.IsChecked is true;
          });
          if (!isChecked ) return;

          Player p = players[0];
          string tag = p.Tag.Replace("#", "");

          SetCursorPos(700, 325);
          mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
          mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
          Thread.Sleep(250);
          for (int i = 0; i < 10; i++)
            SendKeys.SendWait("{BACKSPACE}");
          Thread.Sleep(250);
          SendKeys.SendWait(tag);
          Thread.Sleep(350);
          SetCursorPos(1200, 325);
          mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
          mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0); 
          Thread.Sleep(1500);
          SetCursorPos(700, 550);
          mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
          mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
          Thread.Sleep(300);
          SetCursorPos(300, 125);
          mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
          mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
          Thread.Sleep(250);

          Dispatcher.Invoke(() =>
          {
            players.RemoveAt(0);
            sessionPlayersInvited++;
            SessionPlayersInvitedText.Content = "Session players invited: " + sessionPlayersInvited;
            PlayersToInviteText.Content = "Players to invite: " + players.Count;
            EstimatedTimeLeftText.Content = "Estimated invite time: " + (int)Math.Floor(players.Count * 2.9 / 60) + "m " + (int)Math.Floor((players.Count * 2.9) % 60) + "s";
            totalPlayersInvited++;
            TotalPlayersInvitedText.Content = "Total players invited: " + totalPlayersInvited;
            isChecked = InvitePlayersCheckBox.IsChecked is true;
          });
          if (!isChecked) return;
        }
      }


      private void FindPlayersCheckBox_Checked(object sender, RoutedEventArgs e)
      {
        if (!FindPlayersCheckBox.IsChecked is true) return;

        if (MaxLeagueComboBox.SelectedIndex == -1 || MinTownhallTextBox.ToString() == null ||
            MinLevelTextBox.ToString() == null || MinAttacksTextBox.ToString() == null
            || MinKingTextBox.ToString() == null || MinQueenTextBox.ToString() == null || MinWardenTextBox.ToString() == null
            || MinChampionTextBox.ToString() == null || MinTrophiesTextBox.ToString() == null || MinDonationsTextBox.ToString() == null || ApiKeyTextBox.ToString() == null)
        {
          e.Handled = true;
          FindPlayersCheckBox.IsChecked = false;
          return;
        }
        GetSelectedRoles();
        maxLeague = MaxLeagueComboBox.SelectedIndex + 48000001;
        minTh = int.Parse(MinTownhallTextBox.Text);
        minLevel = int.Parse(MinLevelTextBox.Text);
        minAttacks = int.Parse(MinAttacksTextBox.Text);
        minKing = int.Parse(MinKingTextBox.Text);
        minQueen = int.Parse(MinQueenTextBox.Text);
        minWarden = int.Parse(MinWardenTextBox.Text);
        minChampion = int.Parse(MinChampionTextBox.Text);
        minTrophies = int.Parse(MinTrophiesTextBox.Text);
        minDonations = int.Parse(MinDonationsTextBox.Text);
        API_TOKEN = ApiKeyTextBox.Text;

        Thread findPlayersThread = new Thread(FindPlayersAsync)
        {
          IsBackground = true
        };
        findPlayersThread.Start();
      }

      private void InvitePlayersCheckBox_Checked(object sender, RoutedEventArgs e)
      {
        if (!InvitePlayersCheckBox.IsChecked is true) return;

        Thread invitePlayersThread = new Thread(InvitePlayers)
        {
          IsBackground = true
        };
        invitePlayersThread.Start();
      }

      private void TextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
      {
        if (!int.TryParse(e.Text, out _)) { e.Handled = true; return; }
        if (sender is System.Windows.Controls.TextBox textBox)
        {
          Properties.Settings.Default[textBox.Name + "Setting"] = int.Parse(textBox.Text + e.Text);
          Properties.Settings.Default.Save();
        }
      }

      private void GetSelectedRoles()
      {
        acceptedRoles.Clear();
        foreach (var selectedItem in RolesBox.SelectedItems)
        {
          acceptedRoles.Add(((ListBoxItem)selectedItem).Content.ToString().ToLower());
        }
      }

    private void RolesBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      // Create a StringCollection to store the selected items as strings
      StringCollection selectedItemsCollection = new StringCollection();

      // Convert and save the selected items to the StringCollection
      foreach (ListBoxItem selectedItem in RolesBox.SelectedItems)
      {
        selectedItemsCollection.Add(selectedItem.Content.ToString());
      }

      // Save the StringCollection to the application settings
      Properties.Settings.Default.acceptableRolesSetting = selectedItemsCollection;
      Properties.Settings.Default.Save();
    }

    private void ApiKeyTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
      if (sender is System.Windows.Controls.TextBox textBox)
      {
        Properties.Settings.Default.apiKeySetting = textBox.Text;
        Properties.Settings.Default.Save();
      }
    }

    private void MaxLeagueComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
      if (sender is System.Windows.Controls.ComboBox comboBox) {
        Properties.Settings.Default.maxLeagueTextBoxSetting = comboBox.SelectedIndex;
        Properties.Settings.Default.Save();
      }
    }

    private void LoadSettings()
    {
      if (Properties.Settings.Default.totalPlayersFoundSetting != 0)
        totalPlayersFound = Properties.Settings.Default.totalPlayersFoundSetting;
      TotalPlayersFoundText.Content = "Total players found: " + totalPlayersFound;
      if (Properties.Settings.Default.totalPlayersInvitedSetting != 0)
        totalPlayersInvited = Properties.Settings.Default.totalPlayersInvitedSetting;
      TotalPlayersInvitedText.Content = "Total players invited: " + totalPlayersInvited;

      if (Properties.Settings.Default.maxLeagueTextBoxSetting != -1)
        MaxLeagueComboBox.SelectedIndex = Properties.Settings.Default.maxLeagueTextBoxSetting;
      if (Properties.Settings.Default.minTownhallTextBoxSetting != 0)
        MinTownhallTextBox.Text = Properties.Settings.Default.minTownhallTextBoxSetting.ToString();
      if (Properties.Settings.Default.minLevelTextBoxSetting != 0)
        MinLevelTextBox.Text = Properties.Settings.Default.minLevelTextBoxSetting.ToString();
      if (Properties.Settings.Default.minAttacksTextBoxSetting != 0)
        MinAttacksTextBox.Text = Properties.Settings.Default.minAttacksTextBoxSetting.ToString();
      if (Properties.Settings.Default.minKingTextBoxSetting != 0)
        MinKingTextBox.Text = Properties.Settings.Default.minKingTextBoxSetting.ToString();
      if (Properties.Settings.Default.minQueenTextBoxSetting != 0)
        MinQueenTextBox.Text = Properties.Settings.Default.minQueenTextBoxSetting.ToString();
      if (Properties.Settings.Default.minWardenTextBoxSetting != 0)
        MinWardenTextBox.Text = Properties.Settings.Default.minWardenTextBoxSetting.ToString();
      if (Properties.Settings.Default.minChampionTextBoxSetting != 0)
        MinChampionTextBox.Text = Properties.Settings.Default.minChampionTextBoxSetting.ToString();
      if (Properties.Settings.Default.minTrophiesTextBoxSetting != 0)
        MinTrophiesTextBox.Text = Properties.Settings.Default.minTrophiesTextBoxSetting.ToString();
      if (Properties.Settings.Default.minDonationsTextBoxSetting != 0)
        MinDonationsTextBox.Text = Properties.Settings.Default.minDonationsTextBoxSetting.ToString();
      if (Properties.Settings.Default.apiKeySetting != "")
        ApiKeyTextBox.Text = Properties.Settings.Default.apiKeySetting;

      StringCollection selectedItemsCollection = Properties.Settings.Default.acceptableRolesSetting;
      if (selectedItemsCollection != null)
      {
        foreach (string selectedItem in selectedItemsCollection)
        {
          // Find the ListBoxItem that matches the saved string value.
          ListBoxItem listBoxItem = RolesBox.Items.Cast<ListBoxItem>().FirstOrDefault(item => item.Content.ToString() == selectedItem);

          if (listBoxItem != null)
          {
            RolesBox.SelectedItems.Add(listBoxItem);
          }
        }
      }
      string serializedPlayers = Properties.Settings.Default.savedPlayersSetting;
      if (!string.IsNullOrEmpty(serializedPlayers))
      {
        ObservableCollection<Player> loadedPlayers = JsonConvert.DeserializeObject<ObservableCollection<Player>>(serializedPlayers);
        foreach (var player in loadedPlayers)
        {
          players.Add(player);
        }
      }

      if (Properties.Settings.Default.uniquePlayersSetting != null)
        uniquePlayers = Properties.Settings.Default.uniquePlayersSetting;
    }

    private void ResetButton_Click(object sender, RoutedEventArgs e)
    {
      if (uniquePlayers != null) uniquePlayers.Clear();
      if (players != null) players.Clear();
      Properties.Settings.Default.totalPlayersFoundSetting = 0;
      totalPlayersFound = 0;
      TotalPlayersFoundText.Content = "Total players found: 0";
      Properties.Settings.Default.totalPlayersInvitedSetting = 0;
      totalPlayersInvited = 0;
      TotalPlayersInvitedText.Content = "Total players invited: 0";
      sessionPlayersFound = 0;
      SessionPlayersFoundText.Content = "Session players found: 0";
      sessionPlayersInvited = 0;
      SessionPlayersInvitedText.Content = "Session players invited: 0";
      PlayersToInviteText.Content = "Players to invite: 0";
      EstimatedTimeLeftText.Content = "Estimated invite time: 0";
      Properties.Settings.Default.Save();
    }
  }
}
