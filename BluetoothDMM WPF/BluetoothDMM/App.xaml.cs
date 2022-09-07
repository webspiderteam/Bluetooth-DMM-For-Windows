using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private const int MINIMUM_SPLASH_TIME = 3000; // Miliseconds
        private const int SPLASH_FADE_TIME = 1000;     // Miliseconds
        private string time;

        protected override void OnStartup(StartupEventArgs e)
        {
            Application.Current.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(AppDispatcherUnhandledException);
            //AppDomain.UnhandledException += AppDomain_UnhandledException;
            this.Dispatcher.UnhandledException += AppDispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
            time = DateTime.Now.ToString("dd-MM-yyyy_HH_mm_ss");
            //AppDomain.CurrentDomain.FirstChanceException += CurrentDomain_FirstChanceException;
            // Step 1 - Load the splash screen
            SplashScreen splash = new SplashScreen("Assets/SplashScreen.scale-100.png");
            splash.Show(false, true);
            // Step 2 - Start a stop watch
            Stopwatch timer = new Stopwatch();
            timer.Start();

            // Step 3 - Load your windows but don't show it yet
            base.OnStartup(e);
            MainWindow main = new MainWindow();

            // Step 4 - Make sure that the splash screen lasts at least two seconds
            timer.Stop();
            int remainingTimeToShowSplash = MINIMUM_SPLASH_TIME - (int)timer.ElapsedMilliseconds;
            if (remainingTimeToShowSplash > 0)
                Thread.Sleep(remainingTimeToShowSplash);

            // Step 5 - show the page
            splash.Close(TimeSpan.FromMilliseconds(SPLASH_FADE_TIME));
            main.Show();
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            
            Exception ex = e.ExceptionObject as Exception;
#if DEBUG// In debug mode do not custom-handle the exception, let Visual Studio handle it
            //ex.Handled = false;
#else
            //ex. Handled = true;
            //e.Exception.StackTrace
            string errorMessage = string.Format("An application error occurred.\n" +
                "Please check whether your data is correct and repeat the action. " +
                "If this error occurs again there seems to be a more serious malfunction in the application, and you better close it.\n\n" +
                "Error: {0}\n\nDo you want to continue?\n" +
                "(if you click Yes you will continue with your work, if you click No the application will close)",
            ex.Message + (ex.InnerException != null ? "\n" +
            ex.InnerException.Message : null));
            
            File.AppendAllText("Debuglog_" + time + ".txt", "error catch Error: " + ex.ToString() + System.Environment.NewLine);
            File.AppendAllText("Debuglog_" + time + ".txt", errorMessage + System.Environment.NewLine);
            if (MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error) == MessageBoxResult.No)
                if (MessageBox.Show("WARNING: The application will close. Any changes will not be saved!\nDo you really want to close it?", "Close the application!", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
#endif
        }

        void AppDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
#if DEBUG// In debug mode do not custom-handle the exception, let Visual Studio handle it
            e.Handled = false;
#else
            ShowUnhandledException(e);
#endif

        }

        void ShowUnhandledException(DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            //e.Exception.StackTrace
            string errorMessage = string.Format("An application error occurred.\n" +
                "Please check whether your data is correct and repeat the action. " +
                "If this error occurs again there seems to be a more serious malfunction in the application, and you better close it.\n\n" +
                "Error: {0}\n\nDo you want to continue?\n" +
                "(if you click Yes you will continue with your work, if you click No the application will close)",
            e.Exception.Message + (e.Exception.InnerException != null ? "\n" +
            e.Exception.InnerException.Message : null));
            string time = DateTime.Now.ToString("dd-MM-yyyy_HH_mm_ss");
            File.AppendAllText("crashlog_" + time + ".txt", "error catch Error: " + e.ToString() + System.Environment.NewLine);
            File.AppendAllText("crashlog_" + time + ".txt", errorMessage + System.Environment.NewLine);
            if (MessageBox.Show(errorMessage, "Application Error", MessageBoxButton.YesNoCancel, MessageBoxImage.Error) == MessageBoxResult.No)
                if (MessageBox.Show("WARNING: The application will close. Any changes will not be saved!\nDo you really want to close it?", "Close the application!", MessageBoxButton.YesNoCancel, MessageBoxImage.Warning) == MessageBoxResult.Yes)
                {
                    Application.Current.Shutdown();
                }
        }
        public ResourceDictionary ThemeDictionary
        {
            // You could probably get it via its name with some query logic as well.
            get { return Resources.MergedDictionaries[0]; }
        }

        public void ChangeTheme(Uri uri)
        {
            ThemeDictionary.MergedDictionaries.Clear();
            ThemeDictionary.MergedDictionaries.Add(new ResourceDictionary() { Source = uri });
        }
    }
}
