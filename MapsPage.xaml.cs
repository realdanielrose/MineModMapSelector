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
        private const int MaxQueueSize = 50000; // Erhöhe die maximale Queue-Größe
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
            StartServerProcessChecker(); // Starte den Prozess-Status-Checker
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
                logKeywords = new List<string>(); // Leere Liste, falls die Datei nicht vorhanden ist
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
            var currentLines = ServerOutputTextBox.Text.Split(new[] { Environment.NewLine }, StringSplitOptions.None).ToList();

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
                            CreateNoWindow = true, // Verhindert das Anzeigen eines Fensters
                        }
                    };

                    ServerProcess.OutputDataReceived += (s, args) => AppendTextToServerOutput(args.Data);
                    ServerProcess.ErrorDataReceived += (s, args) => AppendTextToServerOutput(args.Data);

                    try
                    {
                        ServerProcess.Start();
                        ServerProcess.BeginOutputReadLine();
                        ServerProcess.BeginErrorReadLine();

                        _ = Task.Run(() =>
                        {
                            ServerProcess.WaitForExit();

                            DispatcherQueue.TryEnqueue(() =>
                            {
                                ShowMessage("Serverprozess wurde beendet.");
                            });

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
                    await ShowMessage("Die Datei run.bat wurde nicht gefunden.");
                }
            }
            else
            {
                if (Process.GetProcesses().Any(p => p.Id == ServerProcess.Id && !p.HasExited))
                {
                    await ShowMessage("Der Server läuft bereits.");
                }
                else
                {
                    ServerProcess = null;
                    StartServer(sender, e); // Erneut versuchen, den Server zu starten
                }
            }
        }

        private async void StopServer_Click(object sender, RoutedEventArgs e)
        {
            if (IsServerProcessRunning())
            {
                try
                {
                    Debug.WriteLine("Versuche den Serverprozess zu stoppen...");
                    ServerProcess.CloseMainWindow();

                    // Gebe dem Prozess etwas Zeit, um das Fenster zu schließen
                    await Task.Delay(2000);

                    if (!ServerProcess.HasExited)
                    {
                        ServerProcess.Kill();

                        await Task.Run(() =>
                        {
                            ServerProcess.WaitForExit(); // Warte auf die Beendigung
                        });
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
                    ServerProcess = null;
                }
            }
            else
            {
                Debug.WriteLine("Der Server läuft nicht.");
                await ShowMessage("Der Server läuft nicht.");
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
                return false;

            try
            {
                ServerProcess.Refresh(); // Aktualisiert die Prozessdaten
                return !ServerProcess.HasExited;
            }
            catch (Exception)
            {
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
                    await Task.Delay(1000); // Überprüfe alle 1 Sekunden

                    if (ServerProcess != null && ServerProcess.HasExited)
                    {
                        DispatcherQueue.TryEnqueue(() =>
                        {
                            // Aktualisiere UI-Status, um zu zeigen, dass der Server beendet wurde
                            ServerProcess = null;
                        });
                    }
                }
            });
        }
    }
}