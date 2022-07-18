using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for Devices.xaml
    /// </summary>
    public partial class Devices : Window
    {
        public Dictionary<string, string> DeviceListC { get; set; }

        public Devices(Dictionary<string, string> deviceListC)
        {
            InitializeComponent();

            //var mainWindow = Application.Current.MainWindow;
            //this.DataContext = mainWindow.DataContext;
            DeviceListC = deviceListC;
            
            this.DataContext = this;
        }

        private void button1_Click(object sender, RoutedEventArgs e)
        {
            DeviceListC.Remove(((KeyValuePair<string, string>)listBox.SelectedValue).Key);
            listBox.SelectedIndex = -1;
            listBox.Items.Refresh();
            //Debug.WriteLine(((System.Collections.Generic.KeyValuePair<string, string>)listBox.SelectedValue).Key);
            
        }

        private void button2_Click(object sender, RoutedEventArgs e)
        {
            DeviceListC.Clear();
            listBox.SelectedIndex = -1;
            listBox.Items.Refresh();
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            var a = ((System.Collections.Generic.KeyValuePair<string, string>)listBox.SelectedValue).Value;
            EditBox.Text = a;
            Renamer.IsOpen = true;
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (EditBox.Text.Length > 0)
            {
                DeviceListC[((KeyValuePair<string, string>)listBox.SelectedValue).Key]=EditBox.Text;
               
                listBox.Items.Refresh();
                Renamer.IsOpen = false;
            }
        }

        private void button3_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.ConnectOn)
            {
                Properties.Settings.Default.DeviceID.Clear();
                foreach (KeyValuePair<string, string> items in DeviceListC)
                {
                    Properties.Settings.Default.DeviceID.Add(items.Key + "\n" + items.Value);
                }
                Properties.Settings.Default.Save();
            }
            DialogResult = true;
        }
    }
    public class IDtoMac_Converter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value.ToString().Substring(value.ToString().Length - 17, 17).ToUpper();
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return " ";
        } 
    }
}
