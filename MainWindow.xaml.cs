using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace MineModMapSelector
{
    public sealed partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();

            // Standardm‰ﬂig "ClientPage" laden
            ContentFrame.Navigate(typeof(ClientPage));
        }

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer != null)
            {
                var tag = args.SelectedItemContainer.Tag.ToString();

                // Navigation basierend auf dem Tag
                switch (tag)
                {
                    case "Client":
                        ContentFrame.Navigate(typeof(ClientPage));
                        break;
                    case "Server":
                        ContentFrame.Navigate(typeof(ServerPage));
                        break;
                    case "Maps":
                        ContentFrame.Navigate(typeof(MapsPage));
                        break;
                }
            }
        }
    }
}
