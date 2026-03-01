using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

namespace HuFu.Pages;

public sealed partial class ShellPage : Page
{
    public ShellPage()
    {
        InitializeComponent();
        NavView.SelectionChanged += NavView_SelectionChanged;

        // default
        ContentFrame.Navigate(typeof(ChatPage));
        NavView.SelectedItem = NavView.MenuItems[0];
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.SelectedItem is NavigationViewItem item && item.Tag is string tag)
        {
            switch (tag)
            {
                case "conversation":
                    ContentFrame.Navigate(typeof(ChatPage));
                    break;
                case "community":
                    ContentFrame.Navigate(typeof(CommunityPage));
                    break;
                case "contacts":
                    ContentFrame.Navigate(typeof(ContactsPage));
                    break;
                case "discover":
                    ContentFrame.Navigate(typeof(DiscoverPage));
                    break;
                case "profile":
                    ContentFrame.Navigate(typeof(ProfilePage));
                    break;
            }
        }
    }
}
