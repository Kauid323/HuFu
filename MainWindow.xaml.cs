using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using HuFu.Pages;
using HuFu.Services;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using WinRT.Interop;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace HuFu
{
    /// <summary>
    /// An empty window that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainWindow : Window
    {
        private AppWindow? _appWindow;

        public MainWindow()
        {
            InitializeComponent();
            InitializeCustomTitleBar();

            if (SessionStore.IsLoggedIn)
            {
                RootFrame.Navigate(typeof(ShellPage));
            }
            else
            {
                RootFrame.Navigate(typeof(LoginPage));
            }
        }

        private void InitializeCustomTitleBar()
        {
            IntPtr hWnd = WindowNative.GetWindowHandle(this);
            WindowId windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
            _appWindow = AppWindow.GetFromWindowId(windowId);

            if (_appWindow != null && AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = _appWindow.TitleBar;
                titleBar.ExtendsContentIntoTitleBar = true;
                
                // Set button colors to transparent
                UpdateTitleBarButtons();

                // Set drag region
                AppTitleBar.Loaded += (s, e) => UpdateDragRegion();
                AppTitleBar.SizeChanged += (s, e) => UpdateDragRegion();

                // Listen for theme changes to update button colors
                if (Content is FrameworkElement rootElement)
                {
                    rootElement.ActualThemeChanged += (s, e) => UpdateTitleBarButtons();
                }

                this.Activated += MainWindow_Activated;
            }
        }

        private void UpdateDragRegion()
        {
            if (_appWindow == null || !AppWindowTitleBar.IsCustomizationSupported()) return;

            double scaleAdjustment = AppTitleBar.XamlRoot.RasterizationScale;
            var dragRects = new List<Windows.Graphics.RectInt32>();
            
            Windows.Graphics.RectInt32 dragRect;
            dragRect.X = 0;
            dragRect.Y = 0;
            dragRect.Width = (int)(AppTitleBar.ActualWidth * scaleAdjustment);
            dragRect.Height = (int)(AppTitleBar.ActualHeight * scaleAdjustment);
            
            dragRects.Add(dragRect);
            _appWindow.TitleBar.SetDragRectangles(dragRects.ToArray());
        }

        private void UpdateTitleBarButtons()
        {
            if (_appWindow == null || !AppWindowTitleBar.IsCustomizationSupported()) return;

            var titleBar = _appWindow.TitleBar;
            titleBar.ButtonBackgroundColor = Colors.Transparent;
            titleBar.ButtonInactiveBackgroundColor = Colors.Transparent;

            // Follow system theme
            bool isDark = Content is FrameworkElement root && root.ActualTheme == ElementTheme.Dark;
            titleBar.ButtonForegroundColor = isDark ? Colors.White : Colors.Black;
            titleBar.ButtonHoverBackgroundColor = isDark ? ColorHelper.FromArgb(40, 255, 255, 255) : ColorHelper.FromArgb(20, 0, 0, 0);
            titleBar.ButtonPressedBackgroundColor = isDark ? ColorHelper.FromArgb(60, 255, 255, 255) : ColorHelper.FromArgb(40, 0, 0, 0);
        }

        private void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_appWindow != null && AppWindowTitleBar.IsCustomizationSupported())
            {
                var titleBar = _appWindow.TitleBar;
                if (args.WindowActivationState == WindowActivationState.Deactivated)
                {
                    titleBar.ButtonForegroundColor = Colors.Gray;
                }
                else
                {
                    UpdateTitleBarButtons();
                }
            }
        }
    }
}
