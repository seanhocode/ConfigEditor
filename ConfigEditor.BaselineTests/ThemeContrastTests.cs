using ConfigEditor.Infrastructure.Services;
using ConfigEditor.Wpf;
using ConfigEditor.Wpf.ViewModels;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace ConfigEditor.BaselineTests;

public class ThemeContrastTests
{
    [Fact]
    public void MainWindow_UsesReadableThemeColors()
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                var app = new App();
                app.InitializeComponent();
                var xml = new XmlService();
                var window = new MainWindow(new MainViewModel(
                    new ConfigService(), xml, new XmlBatchEditorService(xml)));
                window.Show();

                AssertContrast(window.Foreground, window.Background);
                AssertContrast(
                    ((TextBlock)FindDescendant<TextBlock>(window, x => x.Text == "掃描根目錄")).Foreground,
                    ((Border)window.Content).Background,
                    window.Background);
                FindDescendant<Expander>(window, _ => true).IsExpanded = true;
                FindDescendant<TabControl>(window, _ => true).SelectedIndex = 1;
                window.UpdateLayout();
                foreach (var type in new[] { typeof(TreeView), typeof(ListBox), typeof(DataGrid), typeof(TabControl), typeof(TabItem), typeof(TextBox), typeof(ComboBox) })
                {
                    var control = TryFindDescendant<Control>(window, x => x.GetType() == type)
                        ?? throw new InvalidOperationException($"Missing {type.Name}.");
                    if (control.Background is not null)
                    {
                        AssertContrast(control.Foreground, control.Background,
                            ((Border)window.Content).Background, window.Background);
                    }
                }
                FindDescendant<TabControl>(window, _ => true).SelectedIndex = 0;
                window.UpdateLayout();
                var xmlTree = FindDescendant<TreeView>(window, x => x.Name == "XmlTree");
                AssertContrast(xmlTree.Foreground, xmlTree.Background,
                    ((Border)window.Content).Background, window.Background);

                window.Close();
                app.Shutdown();
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        Assert.True(thread.Join(TimeSpan.FromSeconds(15)), "WPF contrast check timed out.");
        if (error is not null)
        {
            throw error;
        }
    }

    private static T FindDescendant<T>(DependencyObject parent, Func<T, bool> match) where T : DependencyObject
        => TryFindDescendant(parent, match) ?? throw new InvalidOperationException($"Missing {typeof(T).Name}.");

    private static T? TryFindDescendant<T>(DependencyObject parent, Func<T, bool> match) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T candidate && match(candidate))
            {
                return candidate;
            }

            var found = TryFindDescendant(child, match);
            if (found is not null)
            {
                return found;
            }
        }

        return null;
    }

    private static void AssertContrast(Brush foreground, Brush background,
        Brush? underneath = null, Brush? windowBackground = null)
    {
        var front = Assert.IsType<SolidColorBrush>(foreground).Color;
        var back = Assert.IsType<SolidColorBrush>(background).Color;
        if (windowBackground is not null)
        {
            var bottom = Assert.IsType<SolidColorBrush>(windowBackground).Color;
            var layer = Assert.IsType<SolidColorBrush>(underneath).Color;
            underneath = new SolidColorBrush(Composite(layer, bottom));
        }
        if (underneath is not null)
        {
            var baseColor = Assert.IsType<SolidColorBrush>(underneath).Color;
            back = Composite(back, baseColor);
        }
        static double Luminance(Color c) =>
            (0.2126 * c.R + 0.7152 * c.G + 0.0722 * c.B) / 255;
        Assert.True(Math.Abs(Luminance(front) - Luminance(back)) >= 0.35,
            $"Insufficient contrast: foreground {front}, background {back}.");
    }

    private static Color Composite(Color top, Color bottom)
    {
        var opacity = top.A / 255.0;
        return Color.FromRgb(
            (byte)(top.R * opacity + bottom.R * (1 - opacity)),
            (byte)(top.G * opacity + bottom.G * (1 - opacity)),
            (byte)(top.B * opacity + bottom.B * (1 - opacity)));
    }
}
