using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for Window1.xaml
    /// </summary>
    public partial class Settings : Window
    {
        public Settings()
        {
            InitializeComponent();
            checkBox3.IsChecked = Properties.Settings.Default.Ontop;
            checkBox2.IsChecked = Properties.Settings.Default.ChartOn;
            checkBox1.IsChecked = Properties.Settings.Default.ConnectOn;
            checkBox6.IsChecked = Properties.Settings.Default.Reconnect;
            checkBox5.IsChecked = Properties.Settings.Default.LogData;
            checkBox4.IsChecked = Properties.Settings.Default.Remember;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {

            Properties.Settings.Default.Ontop = checkBox3.IsChecked == true;
            Properties.Settings.Default.ChartOn = checkBox2.IsChecked == true;
            Properties.Settings.Default.ConnectOn = checkBox1.IsChecked == true;
            Properties.Settings.Default.Reconnect = checkBox6.IsChecked == true;
            Properties.Settings.Default.LogData = checkBox5.IsChecked == true;
            Properties.Settings.Default.DeviceID = ((MainWindow)Application.Current.MainWindow).SelectedDeviceId;
            Properties.Settings.Default.DeviceName = ((MainWindow)Application.Current.MainWindow).SelectedDeviceName;
            Properties.Settings.Default.Remember = checkBox4.IsChecked == true;
            Properties.Settings.Default.Save();
            //Properties.Settings.Default.WindowSize = new System.Drawing.Size(0,0);
            //Properties.Settings.Default.WindowPosition = new System.Drawing.Size(0, 0);
            Close();

        }
    }
}
