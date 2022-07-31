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
    /// Interaction logic for MQTT_Settings.xaml
    /// </summary>
    public partial class MQTT_Settings : Window
    {
        public MQTT_Settings()
        {
            InitializeComponent();
            DataContext = this;
            txtPasword.Password = Properties.MQTT.Default.Password;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            Properties.MQTT.Default.Password = txtPasword.Password;
            Properties.MQTT.Default.Save();
            DialogResult = true;
        }
    }
}
