using System.Globalization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Threading;

namespace DoMping;

public partial class App : Application
{
	private void Application_Startup(object sender, StartupEventArgs e)
	{
		RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;
		FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement), new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
		DispatcherUnhandledException += App_DispatcherUnhandledException;
		TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
		// Last chance: cannot keep the process alive, but at least record why it died.
		System.AppDomain.CurrentDomain.UnhandledException += (s, args) => DoMping.Classes.Util.WriteErrorLog("Fatal unhandled exception", args.ExceptionObject as System.Exception ?? new System.Exception(args.ExceptionObject?.ToString()));
	}

	private void App_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
	{
		System.Diagnostics.Trace.WriteLine("Unhandled probe/UI exception (suppressed to keep the app running): " + e.Exception);
		DoMping.Classes.Util.WriteErrorLog("Unhandled UI exception (suppressed)", e.Exception);
		e.Handled = true;
	}

	private void TaskScheduler_UnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
	{
		System.Diagnostics.Trace.WriteLine("Unobserved background task exception (suppressed): " + e.Exception);
		DoMping.Classes.Util.WriteErrorLog("Unobserved background task exception (suppressed)", e.Exception);
		e.SetObserved();
	}
}
