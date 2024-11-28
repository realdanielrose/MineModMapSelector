using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.IO;
using WinRT.Interop;
using System.Linq;
using System;
using Windows.Storage;
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
                    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase) // Alphabetisch sortieren
                    .ThenBy(file => Path.GetFileNameWithoutExtension(file).Length) // Numerisch sortieren
                    .Select(Path.GetFileName)
                    .ToList();

                if (jarFiles.Any())
                {
                    // Gesamtanzahl oben hinzufügen
                    string fileCountMessage = $"Gefundene Dateien: {jarFiles.Count}";
                    // Nummerierte Liste erstellen
                    var numberedFiles = jarFiles.Select((file, index) => $"{index + 1}. {file}");
                    // Dateiübersicht erstellen
                    ServerOutputTextBox.Text = $"{fileCountMessage}\n\n{string.Join(Environment.NewLine, numberedFiles)}";
                }
                else
                {
                    ServerOutputTextBox.Text = "Keine JAR-Dateien im Quellordner gefunden.";
                }
            }
            else
            {
                ServerOutputTextBox.Text = "Der Quellpfad ist ungültig oder existiert nicht.";
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

                if (jarFiles.Any())
                {
                    // Gesamtanzahl oben hinzufügen
                    string fileCountMessage = $"Gefundene Dateien im Zielpfad: {jarFiles.Count}";
                    // Nummerierte Liste erstellen
                    var numberedFiles = jarFiles.Select((file, index) => $"{index + 1}. {file}");
                    // TextBox aktualisieren
                    ServerOutputTextBox.Text = $"{fileCountMessage}\n\n{string.Join(Environment.NewLine, numberedFiles)}";
                }
                else
                {
                    ServerOutputTextBox.Text = "Keine JAR-Dateien im Zielpfad gefunden.";
                }
            }
            else
            {
                ServerOutputTextBox.Text = "Der Zielpfad ist ungültig oder existiert nicht.";
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