using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.IO;
using WinRT.Interop;
using System.Linq;
using System;
using Windows.Storage;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;
using Microsoft.WindowsAPICodePack.Dialogs;


namespace MineModMapSelector
{
    public sealed partial class ServerPage : Page
    {
        private string SourcePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Meine Mods");

        private string TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
            "Minecraft-Server", "mods");

        private ObservableCollection<string> Versions = new ObservableCollection<string>();

        public ServerPage()
        {
            this.InitializeComponent();

            // Quell- und Zielpfade initialisieren
            UpdatePaths();

            // Zielpfad zuerst anzeigen
            UpdateTargetJarList();
            // Versionen laden
            LoadVersions();
        }


        private void UpdatePaths()
        {
            SourcePathTextBox.Text = SourcePath;
            TargetPathTextBox.Text = TargetPath;
        }

        private void UpdateJarList()
        {
            if (Directory.Exists(SourcePath))
            {
                var jarFiles = Directory.GetFiles(SourcePath, "*.jar")
                    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(file => Path.GetFileNameWithoutExtension(file).Length)
                    .Select(Path.GetFileName)
                    .ToList();

                ServerOutputRichTextBlock.Blocks.Clear(); // Inhalt des RichTextBlock löschen

                if (jarFiles.Any())
                {
                    // Überschrift hinzufügen
                    var header = new Paragraph();
                    header.Inlines.Add(new Run
                    {
                        Text = $"Gefundene Dateien: {jarFiles.Count}\n\n",
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen)
                    });
                    ServerOutputRichTextBlock.Blocks.Add(header);

                    // Dateien hinzufügen
                    foreach (var file in jarFiles.Select((name, index) => new { Name = name, Index = index + 1 }))
                    {
                        var paragraph = new Paragraph();

                        // Nummer
                        paragraph.Inlines.Add(new Run
                        {
                            Text = $"{file.Index}. ",
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White)
                        });

                        // Dateiname bis zur Version
                        var namePart = file.Name.Split(new[] { '-', '_' }, 2)[0];
                        paragraph.Inlines.Add(new Run
                        {
                            Text = namePart,
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.DeepSkyBlue)
                        });

                        // Versionsnummer
                        var versionStart = file.Name.IndexOfAny(new[] { '-', '_' });
                        if (versionStart != -1)
                        {
                            var versionPart =
                                file.Name.Substring(versionStart + 1, file.Name.Length - versionStart - 5);
                            paragraph.Inlines.Add(new Run
                            {
                                Text = $" - {versionPart}",
                                Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen)
                            });
                        }

                        // `.jar`
                        paragraph.Inlines.Add(new Run
                        {
                            Text = ".jar",
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow)
                        });

                        ServerOutputRichTextBlock.Blocks.Add(paragraph);
                    }
                }
                else
                {
                    // Keine Dateien gefunden
                    var noFilesParagraph = new Paragraph();
                    noFilesParagraph.Inlines.Add(new Run
                    {
                        Text = "Keine JAR-Dateien im Quellpfad gefunden.",
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red)
                    });
                    ServerOutputRichTextBlock.Blocks.Add(noFilesParagraph);
                }
            }
            else
            {
                // Ungültiger Pfad
                var invalidPathParagraph = new Paragraph();
                invalidPathParagraph.Inlines.Add(new Run
                {
                    Text = "Der Quellpfad ist ungültig oder existiert nicht.",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red)
                });
                ServerOutputRichTextBlock.Blocks.Add(invalidPathParagraph);
            }
        }

        private void UpdateTargetJarList()
        {
            if (Directory.Exists(TargetPath))
            {
                var jarFiles = Directory.GetFiles(TargetPath, "*.jar")
                    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase) // Alphabetisch sortieren
                    .ThenBy(file => Path.GetFileNameWithoutExtension(file).Length) // Numerisch sortieren
                    .Select(Path.GetFileName)
                    .ToList();

                // RichTextBlock-Inhalt leeren
                ServerOutputRichTextBlock.Blocks.Clear();

                if (jarFiles.Any())
                {
                    // Gesamtanzahl oben hinzufügen
                    var header = new Paragraph();
                    header.Inlines.Add(new Run
                    {
                        Text = $"Gefundene Dateien im Zielpfad: {jarFiles.Count}\n\n",
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen) // Grün für Überschrift
                    });
                    ServerOutputRichTextBlock.Blocks.Add(header);

                    // Dateien mit Formatierung auflisten
                    foreach (var file in jarFiles.Select((name, index) => new { Name = name, Index = index + 1 }))
                    {
                        var paragraph = new Paragraph();

                        // Nummer
                        paragraph.Inlines.Add(new Run
                        {
                            Text = $"{file.Index}. ",
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.White) // Nummer in Weiß
                        });

                        // Dateiname bis zur Version
                        var namePart = file.Name.Split(new[] { '-', '_' }, 2)[0];
                        paragraph.Inlines.Add(new Run
                        {
                            Text = namePart,
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.DeepSkyBlue) // Blau
                        });

                        // Versionsnummer
                        var versionStart = file.Name.IndexOfAny(new[] { '-', '_' });
                        if (versionStart != -1)
                        {
                            var versionPart =
                                file.Name.Substring(versionStart + 1, file.Name.Length - versionStart - 5);
                            paragraph.Inlines.Add(new Run
                            {
                                Text = $" - {versionPart}",
                                Foreground = new SolidColorBrush(Microsoft.UI.Colors.LimeGreen) // Grün
                            });
                        }

                        // `.jar`
                        paragraph.Inlines.Add(new Run
                        {
                            Text = ".jar",
                            Foreground = new SolidColorBrush(Microsoft.UI.Colors.Yellow) // Gelb
                        });

                        // Paragraph hinzufügen
                        ServerOutputRichTextBlock.Blocks.Add(paragraph);
                    }
                }
                else
                {
                    // Keine Dateien gefunden
                    var noFilesParagraph = new Paragraph();
                    noFilesParagraph.Inlines.Add(new Run
                    {
                        Text = "Keine JAR-Dateien im Zielpfad gefunden.",
                        Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) // Rot für Fehler
                    });
                    ServerOutputRichTextBlock.Blocks.Add(noFilesParagraph);
                }
            }
            else
            {
                // Ungültiger Zielpfad
                var invalidPathParagraph = new Paragraph();
                invalidPathParagraph.Inlines.Add(new Run
                {
                    Text = "Der Zielpfad ist ungültig oder existiert nicht.",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red) // Rot für Fehler
                });
                ServerOutputRichTextBlock.Blocks.Add(invalidPathParagraph);
            }
        }


        private void LoadVersions()
        {
            if (Directory.Exists(SourcePath))
            {
                VersionFlyout.Items.Clear(); // Vorherige Eintr�ge entfernen
                foreach (var dir in Directory.GetDirectories(SourcePath))
                {
                    var version = Path.GetFileName(dir);
                    var menuItem = new MenuFlyoutItem
                    {
                        Text = version
                    };
                    menuItem.Click += VersionMenuFlyoutItem_Click; // Ereignis f�r Klick hinzuf�gen
                    VersionFlyout.Items.Add(menuItem); // Men�punkt hinzuf�gen
                }
            }
        }


        private void VersionMenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem menuItem)
            {
                string selectedVersion = menuItem.Text;
                string newSourcePath = Path.Combine(SourcePath, selectedVersion);

                if (Directory.Exists(newSourcePath))
                {
                    SourcePath = newSourcePath;
                    SourcePathTextBox.Text = SourcePath;

                    // Zielpfad-Inhalte entfernen und nur Quellpfad anzeigen
                    UpdateJarList();
                }
                else
                {
                    ShowMessage($"Der Ordner für die Version \"{selectedVersion}\" existiert nicht.");
                }
            }
        }


        private void ChangeSourcePath(object sender, RoutedEventArgs e)
        {
            // Überprüfen, ob das Startverzeichnis existiert
            if (!Directory.Exists(SourcePath))
            {
                ShowMessage("Das Startverzeichnis existiert nicht. Der Dialog startet im Standardverzeichnis.");
                SourcePath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments); // Fallback-Verzeichnis
            }

            using (var dialog = new CommonOpenFileDialog
                   {
                       IsFolderPicker = true, // Aktiviert den Ordnerauswahlmodus
                       InitialDirectory = SourcePath // Setzt das Startverzeichnis
                   })
            {
                // Zeige den Dialog an
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    // Wenn der Benutzer einen Ordner auswählt, aktualisiere den Pfad
                    SourcePath = dialog.FileName;
                    SourcePathTextBox.Text = SourcePath;
                    LoadVersions();
                    ShowMessage("Quellpfad erfolgreich aktualisiert.");
                }
                else
                {
                    ShowMessage("Auswahl abgebrochen. Der Quellpfad wurde nicht geändert.");
                }
            }
        }

        private void ChangeTargetPath(object sender, RoutedEventArgs e)
        {
            // Überprüfen, ob das Startverzeichnis existiert
            if (!Directory.Exists(TargetPath))
            {
                ShowMessage("Das Startverzeichnis existiert nicht. Der Dialog startet im Standardverzeichnis.");
                TargetPath = Environment.GetFolderPath(Environment.SpecialFolder.Desktop); // Fallback-Verzeichnis
            }

            using (var dialog = new CommonOpenFileDialog
                   {
                       IsFolderPicker = true,
                       InitialDirectory = TargetPath // Startverzeichnis setzen
                   })
            {
                if (dialog.ShowDialog() == CommonFileDialogResult.Ok)
                {
                    TargetPath = dialog.FileName;
                    TargetPathTextBox.Text = TargetPath;
                    ShowMessage("Zielpfad erfolgreich aktualisiert.");
                }
                else
                {
                    ShowMessage("Auswahl abgebrochen. Der Zielpfad wurde nicht geändert.");
                }
            }
        }


        private void ApplyMods(object sender, RoutedEventArgs e)
        {
            var selectedVersion = SourcePathTextBox.Text;
            if (string.IsNullOrEmpty(selectedVersion))
            {
                ShowMessage("Bitte wähle eine Version aus.");
                return;
            }

            if (!Directory.Exists(SourcePath) || !Directory.Exists(TargetPath))
            {
                ShowMessage("Pfad existiert nicht.");
                return;
            }

            try
            {
                Directory.GetFiles(TargetPath).ToList().ForEach(File.Delete);
                foreach (var file in Directory.GetFiles(selectedVersion))
                {
                    string destFile = Path.Combine(TargetPath, Path.GetFileName(file));
                    File.Copy(file, destFile);
                }

                ShowMessage("Mods wurden erfolgreich angewendet.");
                UpdateJarList(); // Aktualisieren Sie die Liste der .jar-Dateien
            }
            catch (Exception ex)
            {
                ShowMessage($"Fehler: {ex.Message}");
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