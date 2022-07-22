using Microsoft.Win32;
using System.Collections.Generic;
using System.Windows;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Settings : Window
    {
        private readonly Dictionary<string, string> DeviceListC;
        // The path to the key where Windows looks for startup applications
        RegistryKey rkApp = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

        public Settings(System.Collections.Generic.Dictionary<string, string> deviceListC)
        {
            InitializeComponent();
            checkBox3.IsChecked = Properties.Settings.Default.Ontop;
            checkBox2.IsChecked = Properties.Settings.Default.ChartOn;
            checkBox1.IsChecked = Properties.Settings.Default.ConnectOn;
            checkBox6.IsChecked = Properties.Settings.Default.Reconnect;
            checkBox5.IsChecked = Properties.Settings.Default.LogData;
            checkBox4.IsChecked = Properties.Settings.Default.Remember;
            checkBox7.IsChecked = Properties.Settings.Default.MinimizeTray;
            checkBox41.IsChecked = Properties.Settings.Default.ADisplay;
            checkBox9.IsChecked = Properties.Settings.Default.AskOnConnect;
            DeviceListC = deviceListC;
            if (rkApp.GetValue("BluetoothDMM") == null)
            {
                // The value doesn't exist, the application is not set to run at startup
                checkBox8.IsChecked = false;
            }
            else
            {
                // The value exists, the application is set to run at startup
                checkBox8.IsChecked = true;
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Properties.Settings.Default.Ontop = checkBox3.IsChecked == true;
            Properties.Settings.Default.ChartOn = checkBox2.IsChecked == true;
            Properties.Settings.Default.ConnectOn = checkBox1.IsChecked == true;
            Properties.Settings.Default.Reconnect = checkBox6.IsChecked == true;
            Properties.Settings.Default.LogData = checkBox5.IsChecked == true;
            //Properties.Settings.Default.DeviceID = ((MainWindow)Application.Current.MainWindow).SelectedDeviceId;
            Properties.Settings.Default.MinimizeTray = checkBox7.IsChecked == true;
            Properties.Settings.Default.Remember = checkBox4.IsChecked == true;
            Properties.Settings.Default.ADisplay = checkBox41.IsChecked == true;
            Properties.Settings.Default.AskOnConnect = checkBox9.IsChecked == true;
            Properties.Settings.Default.Save();
            //Properties.Settings.Default.WindowSize = new System.Drawing.Size(0,0);
            //Properties.Settings.Default.WindowPosition = new System.Drawing.Size(0, 0);
            if ((bool)checkBox8.IsChecked)
            {
                // Add the value in the registry so that the application runs at startup
                rkApp.SetValue("BluetoothDMM", System.Reflection.Assembly.GetExecutingAssembly().Location + " -m");
            }
            else
            {
                // Remove the value from the registry so that the application doesn't start
                rkApp.DeleteValue("BluetoothDMM", false);
            }
            DialogResult = true;

        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            var DeviceListEditor = new Devices(DeviceListC)
            {
                Topmost = this.Topmost
            };
            var result = DeviceListEditor.ShowDialog();
        }
    }
}
