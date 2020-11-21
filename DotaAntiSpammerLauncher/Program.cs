using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using DotaAntiSpammerCommon;
using DotaAntiSpammerCommon.Models;
using DotaAntiSpammerNet;
using DotaAntiSpammerNet.models;
using DotaAntiSpammerPickDetector;
using Microsoft.Win32;
using Newtonsoft.Json;
using Application = System.Windows.Forms.Application;
using JsonSerializer = System.Text.Json.JsonSerializer;

namespace DotaAntiSpammerLauncher
{
    internal static class Program
    {
        private static LowLevelKeyboardHook _kbh;
        private static OverlayWindow _window;
        private static string _lastLobby;
        private static bool _altPressed;
        private static FileSystemWatcher _watcher;
        private static Match _currentMatch;
        private static Match _currentMatchForPicks;

        private static void RunPickDetect()
        {
            var pickDetector = new PickDetector();
            while (true)
            {
                try
                {
                    if (_window == null || _window.Visibility != Visibility.Visible ||
                        _currentMatch == _currentMatchForPicks)
                    {
                        Thread.Sleep(5000);
                        continue;
                    }

                    var makeScreenShot = PickDetector.MakeScreenShot();
                    var byteArray = makeScreenShot.ToByteArray();
                    var heroPixel = pickDetector.GetHeroPixels(byteArray);
                    if (heroPixel.Count == 10)
                    {
                        _currentMatchForPicks = _currentMatch;
                        pickDetector.DetectPicks(heroPixel);
                        if (heroPixel.Any(n => n.Name != null))
                        {
                            var list = new List<PlayerPick>();
                            for (var i = 0; i < _currentMatch.Players.Count; i++)
                            {
                                var pixel = heroPixel[i];
                                if (pixel.Name == null)
                                    continue;
                                var heroConfig = HeroConfigAll.Instance.Heroes.First(n => n.Name == pixel.Name);
                                var player = _currentMatch.Players[i];
                                list.Add(new PlayerPick
                                {
                                    AccountId = player.AccountId,
                                    HeroId = heroConfig.Id,
                                    Radiant = i < 5
                                });
                            }

                            var currentId = (long) (int) Registry.GetValue(
                                @"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess",
                                "ActiveUser",
                                (int) 0);
                            var index = _currentMatch.Players.FindIndex(n => n.AccountId == currentId);
                            var radiant = index < 5;
                            list = list.Where(n => n.Radiant != radiant).ToList();
                            RequestWards(list);
                        }

                        makeScreenShot.Save("debug.bmp");
                        File.WriteAllBytes("debug", byteArray);
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine(e);
                    File.WriteAllText("log", e.Message);
                }

                Thread.Sleep(5000);
            }
        }

        private static void RequestWards(List<PlayerPick> list)
        {
            var currentId = (long) (int) Registry.GetValue(
                @"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess",
                "ActiveUser",
                (int) 0);
            var wardsUrl = GlobalConfig.ApiUrl + GlobalConfig.WardsUrl;
            var url = wardsUrl + "?currentId=" + currentId;
            var webClient = new WebClient();
            webClient.Headers[HttpRequestHeader.ContentType] = "application/json";
            var json = webClient.UploadString(url, JsonSerializer.Serialize(list));
            var playerWards = JsonConvert.DeserializeObject<List<PlayerWards>>(json);
            _window.WardIni(playerWards);
        }

        [STAThread]
        public static void Main()
        {
            Task.Run(RunPickDetect);
            try
            {
                _lastLobby = FileManagement.GetLastLobby(FileManagement.ServerLogPath);

                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                _window = new OverlayWindow {Visibility = Visibility.Visible};
                AddHooks();

                AddFileWatcher();

                var application = new System.Windows.Application();
                LoadData(_window);

                application.Run(_window);
            }
            catch (Exception e)
            {
                File.WriteAllText("log", e.Message);
                throw;
            }
        }


        private static void AddFileWatcher()
        {
            var serverLogPath = FileManagement.ServerLogPath;
            while (serverLogPath == null)
            {
                Console.WriteLine(@"Server log not founded");
                Thread.Sleep(2000);
                serverLogPath = FileManagement.ServerLogPath;
            }

            var fileInfo = new FileInfo(serverLogPath);
            var fileInfoDirectory = fileInfo.Directory;
            if (fileInfoDirectory == null)
            {
                Console.WriteLine(@"Directory not found");
                return;
            }

            _watcher = new FileSystemWatcher(fileInfoDirectory.FullName)
            {
                EnableRaisingEvents = true
            };

            _watcher.Changed += (o, a) =>
            {
                try
                {
                    var tempLobby = FileManagement.GetLastLobby(serverLogPath);
                    if (_lastLobby == tempLobby)
                        return;

                    _watcher.EnableRaisingEvents = false;
                    LoadData(_window);
                    _window.ShowInvoke();
                    _lastLobby = tempLobby;
                }
                finally
                {
                    _watcher.EnableRaisingEvents = true;
                }
            };
        }

        private static void AddHooks()
        {
            _kbh = new LowLevelKeyboardHook();
            _kbh.OnKeyPressed += (sender, keys) =>
            {
                if (keys == Keys.LMenu)
                {
                    _altPressed = true;
                }

                if (keys == Keys.Oemtilde && _altPressed)
                {
                    LoadData(_window);
                    _window.ShowHideInvoke();
                }

                if (keys == Keys.M && _altPressed)
                {
                    _window.Map();
                }
            };
            _kbh.OnKeyUnpressed += (sender, keys) =>
            {
                if (keys != Keys.LMenu)
                    return;
                _altPressed = false;
            };
            _kbh.HookKeyboard();
        }


        private static void LoadData(OverlayWindow window)
        {
            window.Dispatcher.Invoke(() =>
            {
                window.Ini(new Match
                {
                    Players = new List<Player> {null, null, null, null, null, null, null, null, null, null}
                });
            });
            var match = new Match
            {
                Players = new List<Player>()
            };
            var playerIDs = FileManagement.GetPlayerIDs();
#if DEBUG
            playerIDs[0] = "215826874";
            playerIDs[1] = "484431738";
            playerIDs[2] = "1018432408";
            playerIDs[3] = "139287706";
#endif
            try
            {
                var statsUrl = GlobalConfig.ApiUrl + GlobalConfig.StatsUrl;
                var currentId = (long) (int) Registry.GetValue(@"HKEY_CURRENT_USER\Software\Valve\Steam\ActiveProcess",
                    "ActiveUser",
                    (int) 0);

#if DEBUG
                currentId = long.Parse(playerIDs[6]);
#endif
                var url = statsUrl + "?accounts=" + string.Join(",", playerIDs) + "&currentId=" + currentId +
                          "&includeWards=true";

                match.CurrentId = currentId;
                var description = new WebClient().DownloadString(url);
                match = JsonSerializer.Deserialize<Match>(description,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                match.Sort(playerIDs);
                match.CurrentId = currentId;
                _currentMatch = match;
                window.Dispatcher.Invoke(() => { window.Ini(match); });

#if DEBUG
                Task.Run(() =>
                {
                    try
                    {
                        RequestWards(new List<PlayerPick>
                        {
                            
                            new PlayerPick
                            {
                                HeroId = 17,
                                Radiant = true,
                                AccountId = 215826874
                            },
                            new PlayerPick
                            {
                                HeroId = 105,
                                Radiant = true,
                                AccountId = 1018432408
                            },
                            new PlayerPick
                            {
                                HeroId = 91,
                                Radiant = true,
                                AccountId = 139287706
                            },
                        });
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e);
                        throw;
                    }
                });
#endif
            }
            catch (Exception e)
            {
                match.Sort(playerIDs);
                Console.WriteLine(e);
            }
        }
    }
}