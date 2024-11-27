using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.Storage.Pickers;
using System.Collections.ObjectModel;
using System.IO;
using WinRT.Interop;
using System.Linq;
using System;

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
            UpdatePaths();
            LoadVersions();
        }

        private void UpdatePaths()
        {
            SourcePathTextBox.Text = SourcePath;
            TargetPathTextBox.Text = TargetPath;
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
                SourcePathTextBox.Text = Path.Combine(SourcePath, selectedVersion);
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