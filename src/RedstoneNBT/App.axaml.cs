using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using RedstoneNBT.ViewModels;
using RedstoneNBT.Views;
using System;
using System.IO;

namespace RedstoneNBT;

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
            var mainVm = new MainViewModel();
            desktop.MainWindow = new MainWindow
            {
                DataContext = mainVm,
            };

            if (desktop.Args != null && desktop.Args.Length > 0)
            {
                foreach (var rawArg in desktop.Args)
                {
                    var cleanPath = NormalizePath(rawArg);
                    if (string.IsNullOrWhiteSpace(cleanPath)) continue;

                    if (File.Exists(cleanPath))
                    {
                        mainVm.OpenFile(cleanPath);
                    }
                    else if (Directory.Exists(cleanPath))
                    {
                        mainVm.OpenFolder(cleanPath);
                    }
                }
            }
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static string NormalizePath(string raw)
    {
        if (string.IsNullOrWhiteSpace(raw)) return string.Empty;

        var path = raw.Trim('"', '\'');

        if (path.StartsWith("file://", StringComparison.OrdinalIgnoreCase))
        {
            if (Uri.TryCreate(path, UriKind.Absolute, out var uri) && uri.IsFile)
            {
                path = uri.LocalPath;
            }
        }

        try
        {
            return Path.GetFullPath(path);
        }
        catch
        {
            return path;
        }
    }
}