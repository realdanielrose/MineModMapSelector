using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace MineModMapSelector
{
    public sealed partial class MapsPage : Page
    {
        private string ServerPath = $@"C:\Users\{Environment.UserName}\Desktop\Minecraft-Server";
        private ObservableCollection<string> Maps = new ObservableCollection<string>();
        private Process ServerProcess;

        public MapsPage()
        {
            this.InitializeComponent();
            ServerPathTextBox.Text = ServerPath;
            LoadMaps();
        }

        private void LoadMaps()
        {
            Maps.Clear();
            MapMenuFlyout.Items.Clear(); // Entferne alte Eintr�ge

            if (Directory.Exists(ServerPath))
            {
                foreach (var directory in Directory.GetDirectories(ServerPath))
                {
                    var requiredFolders = new[] { "region", "playerdata", "advancements", "data" };
                    int matchCount = 0;

                    foreach (var folder in requiredFolders)
                    {
                        string folderPath = Path.Combine(directory, folder);
                        if (Directory.Exists(folderPath))
                        {
                            matchCount++;
                        }
                    }

                    if (matchCount >= 4)
                    {
                        string mapName = Path.GetFileName(directory);
                        Maps.Add(mapName);

                        // F�ge die Map als Men�punkt hinzu
                        var menuItem = new MenuFlyoutItem
                        {
                            Text = mapName
                        };
                        menuItem.Click += MapFlyoutItem_Click;
                        MapMenuFlyout.Items.Add(menuItem);
                    }
                }
            }
        }

        private void MapFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem)
            {
                string selectedMap = menuItem.Text;

                // Setze die Map als aktiv
                var serverPropertiesPath = Path.Combine(ServerPath, "server.properties");
                if (File.Exists(serverPropertiesPath))
                {
                    var lines = File.ReadAllLines(serverPropertiesPath);
                    for (int i = 0; i < lines.Length; i++)
                    {
                        if (lines[i].StartsWith("level-name="))
                        {
                            lines[i] = $"level-name={selectedMap}";
                        }
                    }

                    File.WriteAllLines(serverPropertiesPath, lines);
                }
                else
                {
                    ShowMessage("server.properties wurde nicht gefunden!");
                }
            }
        }

        private async void ChangeServerPath(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker
            {
                SuggestedStartLocation = PickerLocationId.Desktop
            };
            folderPicker.FileTypeFilter.Add("*");
            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                ServerPath = folder.Path;
                ServerPathTextBox.Text = ServerPath;
                LoadMaps();
            }
        }

        private void ApplyMap(object sender, RoutedEventArgs e)
        {
            ShowMessage("Die Map wurde angewendet.");
        }

        private async void StartServer(object sender, RoutedEventArgs e)
        {
            if (ServerProcess == null || ServerProcess.HasExited)
            {
                var runBatPath = Path.Combine(ServerPath, "run.bat");
                if (File.Exists(runBatPath))
                {
                    ServerProcess = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            FileName = runBatPath,
                            WorkingDirectory = ServerPath,
                            RedirectStandardOutput = true,
                            RedirectStandardError = true,
                            UseShellExecute = false,
                            CreateNoWindow = true
                        }
                    };

                    ServerProcess.OutputDataReceived += (s, args) => Debug.WriteLine(args.Data);
                    ServerProcess.ErrorDataReceived += (s, args) => Debug.WriteLine(args.Data);

                    ServerProcess.Start();
                    ServerProcess.BeginOutputReadLine();
                    ServerProcess.BeginErrorReadLine();
                }
                else
                {
                    ShowMessage("Die Datei run.bat wurde nicht gefunden.");
                }
            }
            else
            {
                ShowMessage("Der Server l�uft bereits.");
            }
        }

        private async void ShowMessage(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Information",
                Content = message,
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };

            await dialog.ShowAsync();
        }
    }
}