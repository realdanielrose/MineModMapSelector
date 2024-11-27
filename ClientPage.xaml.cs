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
    public sealed partial class ClientPage : Page
    {
        private string SourcePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Meine Mods");
        private string TargetPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft", "mods");
        private ObservableCollection<string> Versions = new ObservableCollection<string>();

        public ClientPage()
        {
            this.InitializeComponent();
            UpdatePaths();
            LoadVersions();
        }

        private void UpdatePaths()
        {
            SourcePathText.Text = SourcePath;
            TargetPathText.Text = TargetPath;
        }

        private void LoadVersions()
        {
            if (Directory.Exists(SourcePath))
            {
                Versions.Clear();
                foreach (var dir in Directory.GetDirectories(SourcePath))
                {
                    Versions.Add(Path.GetFileName(dir));
                }
                VersionComboBox.ItemsSource = Versions;
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
            if (VersionComboBox.SelectedItem == null)
            {
                ShowMessage("Bitte wähle eine Version aus.");
                return;
            }

            string selectedVersion = VersionComboBox.SelectedItem.ToString();
            string selectedSourcePath = Path.Combine(SourcePath, selectedVersion);

            if (!Directory.Exists(selectedSourcePath))
            {
                ShowMessage("Der ausgewählte Ordner existiert nicht.");
                return;
            }

            if (!Directory.Exists(TargetPath))
            {
                ShowMessage("Der Zielordner existiert nicht.");
                return;
            }

            try
            {
                Directory.GetFiles(TargetPath).ToList().ForEach(File.Delete);
                foreach (var file in Directory.GetFiles(selectedSourcePath))
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

        private void VersionComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (VersionComboBox.SelectedItem != null)
            {
                string selectedVersion = VersionComboBox.SelectedItem.ToString();
                SourcePathText.Text = Path.Combine(SourcePath, selectedVersion);
            }
        }
    }
}
