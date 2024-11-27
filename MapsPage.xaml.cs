using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using Windows.Storage;
using WinRT.Interop;
using System.Diagnostics;
using System;
using System.Collections.ObjectModel;
using System.IO;
using Microsoft.UI.Xaml.Media;

namespace MineModMapSelector
{
    public sealed partial class MapsPage : Page
    {
        private string ServerPath = $@"C:\Users\{Environment.UserName}\Desktop\Minecraft-Server";
        private ObservableCollection<string> Maps = new ObservableCollection<string>();
        private Process ServerProcess;

        // Variable zum Speichern des Logs
        private static string ServerLog = "";

        public MapsPage()
        {
            this.InitializeComponent();
            ServerPathTextBox.Text = ServerPath;
            ServerOutputTextBox.Text = ServerLog; // Bestehendes Log beim Initialisieren setzen
            LoadMaps();
        }

        private void LoadMaps()
        {
            Maps.Clear();
            if (Directory.Exists(ServerPath))
            {
                foreach (var directory in Directory.GetDirectories(ServerPath))
                {
                    Maps.Add(Path.GetFileName(directory));
                }
                MapComboBox.ItemsSource = Maps;
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

        private void MapComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MapComboBox.SelectedItem != null)
            {
                var selectedMap = MapComboBox.SelectedItem.ToString();
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

        private void ApplyMap(object sender, RoutedEventArgs e)
        {
            if (MapComboBox.SelectedItem != null)
            {
                ShowMessage($"Map {MapComboBox.SelectedItem} wurde angewendet!");
            }
            else
            {
                ShowMessage("Bitte wähle eine Map aus.");
            }
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

                    ServerProcess.OutputDataReceived += (s, args) => LogServerOutput(args.Data);
                    ServerProcess.ErrorDataReceived += (s, args) => LogServerOutput(args.Data);

                    ServerProcess.Start();
                    ServerProcess.BeginOutputReadLine();
                    ServerProcess.BeginErrorReadLine();

                    LogServerOutput("Server gestartet...");
                }
                else
                {
                    ShowMessage("Die Datei run.bat wurde nicht gefunden.");
                }
            }
            else
            {
                ShowMessage("Der Server läuft bereits.");
            }
        }

        private void LogServerOutput(string message)
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                if (!string.IsNullOrWhiteSpace(message))
                {
                    ServerLog += message + Environment.NewLine; // Log in der statischen Variable speichern
                    ServerOutputTextBox.Text = ServerLog;

                    // Simuliere das Scrollen zum Ende
                    var scrollViewer = ServerOutputTextBox.FindDescendant<ScrollViewer>();
                    if (scrollViewer != null)
                    {
                        scrollViewer.ChangeView(null, scrollViewer.ScrollableHeight, null);
                    }
                }
            });
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

    public static class UIHelper
    {
        public static T FindDescendant<T>(this DependencyObject d) where T : DependencyObject
        {
            if (d == null) return null;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(d); i++)
            {
                var child = VisualTreeHelper.GetChild(d, i);
                if (child is T result)
                {
                    return result;
                }

                var descendant = FindDescendant<T>(child);
                if (descendant != null)
                {
                    return descendant;
                }
            }

            return null;
        }
    }
}
