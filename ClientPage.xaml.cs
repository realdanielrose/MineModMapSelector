using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.IO;
using WinRT.Interop;
using System.Linq;
using System;
using Microsoft.UI.Xaml.Documents;
using Microsoft.UI.Xaml.Media;

namespace MineModMapSelector
{
    public sealed partial class ClientPage : Page
    {
        private string SourcePath =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Meine Mods");

        private string TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            ".minecraft", "mods");

        private ObservableCollection<string> Versions = new ObservableCollection<string>();

        public ClientPage()
        {
            this.InitializeComponent();
            UpdatePaths();
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

                ServerOutputTextBox.Blocks.Clear();

                if (jarFiles.Any())
                {
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = $"Gefundene Dateien: {jarFiles.Count}\n\n" });
                    ServerOutputTextBox.Blocks.Add(paragraph);

                    for (int i = 0; i < jarFiles.Count; i++)
                    {
                        paragraph = new Paragraph();
                        var file = jarFiles[i];
                        var namePart = file.Split(new[] { '-', '_' }, 2)[0];
                        var versionStart = file.IndexOfAny(new[] { '-', '_' });
                        var versionPart = versionStart != -1
                            ? $"- {file.Substring(versionStart + 1, file.Length - versionStart - 5)}"
                            : string.Empty;
                        paragraph.Inlines.Add(new Run { Text = $"{i + 1}. {namePart} {versionPart}.jar\n" });
                        ServerOutputTextBox.Blocks.Add(paragraph);
                    }
                }
                else
                {
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = "Keine JAR-Dateien im Quellpfad gefunden." });
                    ServerOutputTextBox.Blocks.Add(paragraph);
                }
            }
            else
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run { Text = "Der Quellpfad ist ungültig oder existiert nicht." });
                ServerOutputTextBox.Blocks.Add(paragraph);
            }
        }

        private void UpdateTargetJarList()
        {
            if (Directory.Exists(TargetPath))
            {
                var jarFiles = Directory.GetFiles(TargetPath, "*.jar")
                    .OrderBy(file => file, StringComparer.OrdinalIgnoreCase)
                    .ThenBy(file => Path.GetFileNameWithoutExtension(file).Length)
                    .Select(Path.GetFileName)
                    .ToList();

                ServerOutputTextBox.Blocks.Clear();

                if (jarFiles.Any())
                {
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = $"Gefundene Dateien im Zielpfad: {jarFiles.Count}\n\n" });
                    ServerOutputTextBox.Blocks.Add(paragraph);

                    for (int i = 0; i < jarFiles.Count; i++)
                    {
                        paragraph = new Paragraph();
                        var file = jarFiles[i];
                        var namePart = file.Split(new[] { '-', '_' }, 2)[0];
                        var versionStart = file.IndexOfAny(new[] { '-', '_' });
                        var versionPart = versionStart != -1
                            ? $"- {file.Substring(versionStart + 1, file.Length - versionStart - 5)}"
                            : string.Empty;
                        paragraph.Inlines.Add(new Run { Text = $"{i + 1}. {namePart} {versionPart}.jar\n" });
                        ServerOutputTextBox.Blocks.Add(paragraph);
                    }
                }
                else
                {
                    var paragraph = new Paragraph();
                    paragraph.Inlines.Add(new Run { Text = "Keine JAR-Dateien im Zielpfad gefunden." });
                    ServerOutputTextBox.Blocks.Add(paragraph);
                }
            }
            else
            {
                var paragraph = new Paragraph();
                paragraph.Inlines.Add(new Run { Text = "Der Zielpfad ist ungültig oder existiert nicht." });
                ServerOutputTextBox.Blocks.Add(paragraph);
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
                    UpdateJarList();
                }
                else
                {
                    ShowMessage($"Der Ordner für die Version \"{selectedVersion}\" existiert nicht.");
                }
            }
        }

        private async void ChangeSourcePath(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                SourcePath = folder.Path;
                UpdatePaths();
                LoadVersions();
            }
        }

        private async void ChangeTargetPath(object sender, RoutedEventArgs e)
        {
            var folderPicker = new FolderPicker();
            folderPicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
            folderPicker.FileTypeFilter.Add("*");

            var hwnd = WindowNative.GetWindowHandle(App.MainWindow);
            InitializeWithWindow.Initialize(folderPicker, hwnd);

            var folder = await folderPicker.PickSingleFolderAsync();
            if (folder != null)
            {
                TargetPath = folder.Path;
                UpdatePaths();
            }
        }

        private void ApplyMods(object sender, RoutedEventArgs e)
        {
            var selectedVersion = SourcePathTextBox.Text;
            if (string.IsNullOrEmpty(selectedVersion))
            {
                ShowMessage("Bitte w�hle eine Version aus.");
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