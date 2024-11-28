using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Windows.Storage.Pickers;
using WinRT.Interop;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;

namespace MineModMapSelector
{
    public sealed partial class MapsPage : Page
    {
        private const int MaxQueueSize = 50000; // Maximale Queue-Größe für Logs
        private const int BatchSize = 50000; // Anzahl zu verarbeitender Zeilen pro Runde
        private const int DelayMilliseconds = 200; // Verarbeitungsverzögerung in ms

        private string ServerPath = $@"C:\Users\{Environment.UserName}\Desktop\Minecraft-Server";
        private ObservableCollection<string> Maps = new ObservableCollection<string>();
        private Process ServerProcess;
        private ConcurrentQueue<string> outputQueue = new ConcurrentQueue<string>();
        private CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();
        private List<string> logKeywords;

        public MapsPage()
        {
            this.InitializeComponent();
            ServerPathTextBox.Text = ServerPath;
            LoadMaps();
            LoadLogKeywords();
            StartQueueProcessor();
            StartServerProcessChecker(); // Startet den Prozess-Status-Checker

            // Lesen der aktuell ausgewählten Map
            DisplayCurrentMap();
        }
        
        private void DisplayCurrentMap()
        {
            var serverPropertiesPath = Path.Combine(ServerPath, "server.properties");
            if (File.Exists(serverPropertiesPath))
            {
                var lines = File.ReadAllLines(serverPropertiesPath);
                var levelNameLine = lines.FirstOrDefault(line => line.StartsWith("level-name="));
                if (levelNameLine != null)
                {
                    var selectedMap = levelNameLine.Replace("level-name=", string.Empty);
                    MapFlyoutButton.Content = selectedMap;
                    SelectedMapTextBlock.Text = $"Aktuelle Map: {selectedMap}";
                }
                else
                {
                    MapFlyoutButton.Content = "Keine Map ausgewählt";
                    SelectedMapTextBlock.Text = "Keine Map ausgewählt";
                }
            }
            else
            {
                ShowMessage("server.properties wurde nicht gefunden!");
            }
        }

        private void LoadLogKeywords()
        {
            var keywordsFilePath = @"C:\Users\danie\source\repos\MineModMapSelector\keywords.txt";
            if (File.Exists(keywordsFilePath))
            {
                logKeywords = File.ReadAllLines(keywordsFilePath).ToList();
            }
            else
            {
                logKeywords = new List<string>(); // Leere Liste, falls Datei nicht vorhanden
            }
        }

        private void StartQueueProcessor()
        {
            Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    if (outputQueue.Count > MaxQueueSize)
                    {
                        StopServerProcess();
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            ShowMessage("Zu viele Zeilen im Server-Log. Der Serverprozess wurde gestoppt.");
                        });
                        break;
                    }

                    await Task.Delay(DelayMilliseconds);

                    DispatcherQueue.TryEnqueue(() =>
                    {
                        int processedLines = 0;
                        string batchOutput = string.Empty;

                        while (processedLines < BatchSize && outputQueue.TryDequeue(out var line))
                        {
                            batchOutput += line + Environment.NewLine;
                            processedLines++;
                        }

                        if (!string.IsNullOrEmpty(batchOutput))
                        {
                            UpdateServerOutputText(batchOutput);
                        }
                    });
                }
            }, cancellationTokenSource.Token);
        }

        private void StopServerProcess()
        {
            cancellationTokenSource.Cancel();

            if (ServerProcess != null && !ServerProcess.HasExited)
            {
                ServerProcess.Kill();
            }
        }

        private void UpdateServerOutputText(string newText)
        {
            const int maxLines = 10000;
            var currentLines = ServerOutputTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .ToList();

            var newLines = newText.Split(new[] { Environment.NewLine }, StringSplitOptions.None)
                .Where(line => logKeywords.Any(keyword => line.Contains(keyword)))
                .ToList();

            currentLines.AddRange(newLines);

            if (currentLines.Count > maxLines)
            {
                currentLines = currentLines.Skip(currentLines.Count - maxLines).ToList();
            }

            ServerOutputTextBox.Text = string.Join(Environment.NewLine, currentLines);
            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            ServerOutputScrollViewer.UpdateLayout();
            ServerOutputScrollViewer.ChangeView(null, ServerOutputScrollViewer.ScrollableHeight, null);
        }

        private void LoadMaps()
        {
            Maps.Clear();
            MapMenuFlyout.Items.Clear();

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

                // Aktualisieren der MapFlyoutButton Anzeige
                MapFlyoutButton.Content = selectedMap;
                SelectedMapTextBlock.Text = $"Ausgewählte Map: {selectedMap}";

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

                    // Bestätigung anzeigen
                    ShowMessage($"Map '{selectedMap}' wurde angewendet.");
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

        private async void ApplyMap(object sender, RoutedEventArgs e)
        {
            // Beispiel: Ersetzen Sie dies durch Ihre gewünschte Funktionalität
            if (MapFlyoutButton.Content != null)
            {
                string selectedMap = MapFlyoutButton.Content.ToString();

                // Aktualisieren Sie server.properties mit der ausgewählten Map
                var serverPropertiesPath = Path.Combine(ServerPath, "server.properties");
                if (File.Exists(serverPropertiesPath))
                {
                    var lines = File.ReadAllLines(serverPropertiesPath).ToList();
                    for (int i = 0; i < lines.Count; i++)
                    {
                        if (lines[i].StartsWith("level-name="))
                        {
                            lines[i] = $"level-name={selectedMap}";
                        }
                    }

                    File.WriteAllLines(serverPropertiesPath, lines);
                    await ShowMessage($"Map '{selectedMap}' wurde angewendet.");
                }
                else
                {
                    await ShowMessage("server.properties wurde nicht gefunden!");
                }
            }
            else
            {
                await ShowMessage("Es wurde keine Map ausgewählt.");
            }
        }

        private async void StartServer(object sender, RoutedEventArgs e)
        {
            if (ServerProcess == null || ServerProcess.HasExited)
            {
                string javaPath = @"C:\Program Files\Java\jdk-17\bin\java.exe"; // Pfad zu Ihrer Java-Installation
                string jvmArgsPath = Path.Combine(ServerPath, "user_jvm_args.txt");
                string programArgsPath = Path.Combine(ServerPath,
                    @"libraries\net\minecraftforge\forge\1.19.2-43.2.14\win_args.txt");

                if (File.Exists(javaPath) && File.Exists(jvmArgsPath) && File.Exists(programArgsPath))
                {
                    var startInfo = new ProcessStartInfo
                    {
                        FileName = javaPath,
                        Arguments = $"@{jvmArgsPath} @{programArgsPath} nogui",
                        WorkingDirectory = ServerPath,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        RedirectStandardInput = true, // Aktiviert Eingaben
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };


                    ServerProcess = new Process
                    {
                        StartInfo = startInfo
                    };

                    ServerProcess.OutputDataReceived += (s, args) => AppendTextToServerOutput(args.Data);
                    ServerProcess.ErrorDataReceived += (s, args) => AppendTextToServerOutput(args.Data);

                    try
                    {
                        ServerProcess.Start();
                        ServerProcess.BeginOutputReadLine();
                        ServerProcess.BeginErrorReadLine();

                        await Task.Run(() =>
                        {
                            ServerProcess.WaitForExit();
                            DispatcherQueue.TryEnqueue(() => { ShowMessage("Serverprozess wurde beendet."); });

                            ServerProcess = null;
                        });
                    }
                    catch (Exception ex)
                    {
                        await ShowMessage($"Fehler beim Starten des Servers: {ex.Message}");
                    }
                }
                else
                {
                    await ShowMessage(
                        "Java oder erforderliche Dateien (user_jvm_args.txt, win_args.txt) wurden nicht gefunden.");
                }
            }
            else
            {
                await ShowMessage("Der Server läuft bereits.");
            }
        }

        private async void StopServer_Click(object sender, RoutedEventArgs e)
        {
            if (ServerProcess != null) // Überprüfen, ob ein Serverprozess existiert
            {
                try
                {
                    Debug.WriteLine("Versuche den Serverprozess sauber zu stoppen...");

                    // Prüfen, ob der Prozess noch läuft
                    if (IsServerProcessRunning())
                    {
                        // Prüfen, ob StandardInput verfügbar ist
                        if (ServerProcess.StandardInput != null)
                        {
                            try
                            {
                                using (var streamWriter = ServerProcess.StandardInput)
                                {
                                    streamWriter.WriteLine("stop"); // "stop"-Befehl senden
                                }

                                Debug.WriteLine("Stop-Befehl erfolgreich gesendet.");
                            }
                            catch (Exception ex)
                            {
                                Debug.WriteLine($"Fehler beim Senden des Stop-Befehls: {ex.Message}");
                            }
                        }
                        else
                        {
                            Debug.WriteLine("StandardInput ist nicht verfügbar.");
                        }

                        // Warten, damit der Prozess Zeit hat, sich selbst zu beenden
                        await Task.Delay(5000);
                    }

                    // Wenn der Prozess noch läuft, wird er zwangsweise beendet
                    if (IsServerProcessRunning())
                    {
                        Debug.WriteLine("Serverprozess läuft noch. Erzwinge das Beenden...");
                        ServerProcess.Kill(); // Prozess zwangsweise beenden
                        await Task.Run(() => ServerProcess.WaitForExit());
                        Debug.WriteLine("Serverprozess wurde zwangsweise beendet.");
                    }

                    Debug.WriteLine("Serverprozess wurde erfolgreich gestoppt.");
                    await ShowMessage("Serverprozess wurde gestoppt.");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Fehler beim Stoppen des Servers: {ex.Message}");
                    await ShowMessage($"Fehler beim Stoppen des Servers: {ex.Message}");
                }
                finally
                {
                    ServerProcess = null; // Prozess-Referenz auf null setzen
                }
            }
            else
            {
                Debug.WriteLine("Kein Serverprozess gefunden.");
                await ShowMessage("Kein Serverprozess gefunden.");
            }
        }


        private void AppendTextToServerOutput(string text)
        {
            if (!string.IsNullOrEmpty(text))
            {
                outputQueue.Enqueue(text);
            }
        }

        private bool IsServerProcessRunning()
        {
            if (ServerProcess == null)
            {
                Debug.WriteLine("ServerProcess ist null.");
                return false;
            }

            try
            {
                ServerProcess.Refresh(); // Aktualisiert den Status des Prozesses
                return !ServerProcess.HasExited;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Fehler beim Überprüfen des Serverprozesses: {ex.Message}");
                return false;
            }
        }



        private async Task ShowMessage(string message)
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

        private void StartServerProcessChecker()
        {
            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);

                    if (ServerProcess != null && ServerProcess.HasExited)
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            Debug.WriteLine("Serverprozess wurde beendet.");
                            ServerProcess = null;
                        });
                    }
                }
            });
        }
    }
}