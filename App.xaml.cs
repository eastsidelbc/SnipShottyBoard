using System.Configuration;
using System.Data;
using System.Windows;
using System;

namespace SnipShottyBoard;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        // Add global exception handling
        AppDomain.CurrentDomain.UnhandledException += (sender, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            MessageBox.Show($"Unhandled Exception:\n{ex?.Message}\n\nStack Trace:\n{ex?.StackTrace}", 
                           "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
        };

        this.DispatcherUnhandledException += (sender, args) =>
        {
            MessageBox.Show($"Dispatcher Exception:\n{args.Exception.Message}\n\nStack Trace:\n{args.Exception.StackTrace}", 
                           "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        try
        {
            base.OnStartup(e);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Startup Exception:\n{ex.Message}\n\nStack Trace:\n{ex.StackTrace}", 
                           "Application Startup Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }
}

