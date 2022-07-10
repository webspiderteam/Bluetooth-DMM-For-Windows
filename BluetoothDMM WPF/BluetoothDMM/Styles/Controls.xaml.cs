using System;
using System.Windows;
using System.Windows.Data;

namespace REghZy.Themes
{
    public partial class Controls
    {
        private void CloseWindow_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                try { CloseWind(Window.GetWindow((FrameworkElement)e.Source)); }
                catch { }
        }
        private void AutoMinimize_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                try { MaximizeRestore(Window.GetWindow((FrameworkElement)e.Source)); }
                catch { }
        }
        private void Minimize_Event(object sender, RoutedEventArgs e)
        {
            if (e.Source != null)
                try { MinimizeWind(Window.GetWindow((FrameworkElement)e.Source)); }
                catch { }
        }

        public void CloseWind(Window window) => window.Close();
        public void MaximizeRestore(Window window)
        {
            if (window.WindowState == WindowState.Maximized)
                window.WindowState = WindowState.Normal;
            else if (window.WindowState == WindowState.Normal)
                window.WindowState = WindowState.Maximized;
        }
        public void MinimizeWind(Window window) => window.WindowState = WindowState.Minimized;
    }

    class windowStyleConverter : IValueConverter
    {
        public string tmp;
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {

            tmp = value.ToString();
            if (value.ToString() == "None")
            {
                return Visibility.Collapsed;
            }
            else
            {
                return Visibility.Visible;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            //bool married = System.Convert.ToBoolean(value);
            if (value.ToString() == "None")
                return Visibility.Collapsed;
            else
                return Visibility.Visible;
        }
    }

}
