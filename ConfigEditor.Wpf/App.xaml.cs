using System.Windows;
using ConfigEditor.Core.Services;
using ConfigEditor.Infrastructure.Services;
using ConfigEditor.Wpf.ViewModels;
using Microsoft.Extensions.DependencyInjection;

namespace ConfigEditor.Wpf;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
	private ServiceProvider? _serviceProvider;

	protected override void OnStartup(StartupEventArgs e)
	{
		base.OnStartup(e);

		var services = new ServiceCollection();
		ConfigureServices(services);
		_serviceProvider = services.BuildServiceProvider();

		var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
		mainWindow.Show();
	}

	protected override void OnExit(ExitEventArgs e)
	{
		_serviceProvider?.Dispose();
		base.OnExit(e);
	}

	private static void ConfigureServices(IServiceCollection services)
	{
		services.AddSingleton<IConfigService, ConfigService>();
		services.AddSingleton<IXmlService, XmlService>();
		services.AddSingleton<IXmlBatchEditorService, XmlBatchEditorService>();
        services.AddTransient<MainViewModel>();
		services.AddTransient<MainWindow>();
	}
}

