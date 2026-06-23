using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace Majorsilence.Reporting.UI
{
    public partial class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                var window = new MainWindow();
                desktop.MainWindow = window;

                var args = desktop.Args;
                if (args?.Length > 0 && System.IO.File.Exists(args[0]))
                {
                    var fullPath = System.IO.Path.GetFullPath(args[0]);
                    window.Opened += async (_, _) => await window.OpenFileAsync(fullPath);
                }
            }

            base.OnFrameworkInitializationCompleted();
        }
    }
}