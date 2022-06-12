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
using System.Windows.Navigation;
using System.Windows.Shapes;
using HeartRateLE.UI;
using System.Diagnostics;
using HeartRateLE.Bluetooth.Events;
using System.ComponentModel;


namespace HeartRateLE.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HeartRateLE.Bluetooth.HeartRateMonitor _heartRateMonitor;
        private string SelectedDeviceId { get; set; }
        private string SelectedDeviceName { get; set; }

        public MainWindow()
        {
            
            InitializeComponent();

            _heartRateMonitor = new HeartRateLE.Bluetooth.HeartRateMonitor();

            // we should always monitor the connection status
            _heartRateMonitor.ConnectionStatusChanged -= HrDeviceOnDeviceConnectionStatusChanged;
            _heartRateMonitor.ConnectionStatusChanged += HrDeviceOnDeviceConnectionStatusChanged;

            //// we can create value parser and listen for parsed values of given characteristic
            //HrParser.ConnectWithCharacteristic(HrDevice.HeartRate.HeartRateMeasurement);
            _heartRateMonitor.RateChanged -= HrParserOnValueChanged;
            _heartRateMonitor.RateChanged += HrParserOnValueChanged;
        }

        protected async override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_heartRateMonitor.IsConnected)
            {
                await _heartRateMonitor.DisconnectAsync();
            }
        }

        private async void HrParserOnValueChanged(object sender, RateChangedEventArgs arg)
        {
            await RunOnUiThread(() =>
            {
                d("Got new measurement: " + arg.MyGattCData);
                textBox.Text= arg.MyGattCData;
                //TxtHr.Text = String.Format("{0}", arg.MyGattCData);
                MyGattCData.Text = arg.MyGattCData;
                MyGattCDataSymbol.Text = arg.MyGattCDataSymbol;
                MyGattCDataACDC.Text = arg.MyGattCDataACDC;
                AutoRange.Visibility = Bool_to_Vis(arg.MyGattCDataAutoRange);
                True_RMS.Visibility = Bool_to_Vis(arg.MyGattCDataTrue_RMS);
                MyGattCDataMax.Visibility = Bool_to_Vis(arg.MyGattCDataMax);
                MyGattCDataMin.Visibility = Bool_to_Vis(arg.MyGattCDataMin);
                MyGattCDataPeek.Visibility = Bool_to_Vis(arg.MyGattCDataPeek);
                InRush.Visibility = Bool_to_Vis(arg.MyGattCDataInRush);
                MyGattCDataContinuity.Visibility = Bool_to_Vis(arg.MyGattCDataContinuity);
                MyGattCDataDiode.Visibility = Bool_to_Vis(arg.MyGattCDataDiode);
                
                if (arg.MyGattCDataHold)
                {
                    MyGattCData.Foreground = Brushes.Red; //new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(100, 255, 125, 35));
                }
                else
                {
                    MyGattCData.Foreground = Brushes.White;

                }
            });
        }

        private async void HrDeviceOnDeviceConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            d("Current connection status is: " + args.IsConnected);
            await RunOnUiThread(async () =>
            {
                bool connected = args.IsConnected;
                if (connected)
                {
                    var device = await _heartRateMonitor.GetDeviceInfoAsync();
                    TxtStatus.Text = SelectedDeviceName + ": connected";
                    TxtBattery.Text = String.Format("battery level: {0}%", device.BatteryPercent);
                    MyGattCDataBluetooth.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtStatus.Text = SelectedDeviceName + ": disconnected";
                    TxtBattery.Text = "battery level: --";
                    MyGattCDataBluetooth.Visibility = Visibility.Hidden;
                    //TxtHr.Text = "--";
                }

                //BtnReadInfo.IsEnabled = connected;
            });
        }

        private async void BtnStart_Click(object sender, RoutedEventArgs e)
        {
            d("Button START clicked.");
            //await _heartRateMonitor.EnableNotificationsAsync();
            d("Notification enabled");
        }

        private async void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            d("Button STOP clicked.");
            //await _heartRateMonitor.DisableNotificationsAsync();
            d("Notification disabled.");
            //TxtHr.Text = "--";
        }

        private async void BtnReadInfo_Click(object sender, RoutedEventArgs e)
        {
            var deviceInfo = await _heartRateMonitor.GetDeviceInfoAsync();

            d($" Manufacturer : {deviceInfo.Manufacturer}"); d("");
            d($"    Model : {deviceInfo.ModelNumber}"); d("");
            d($"      S/N : {deviceInfo.SerialNumber}"); d("");
            d($" Firmware : {deviceInfo.Firmware}"); d("");
            d($" Hardware : {deviceInfo.Hardware}"); d("");

            TxtBattery.Text = $"battery level: {deviceInfo.BatteryPercent}%";
        }

        [Conditional("DEBUG")]
        private void d(string txt)
        {
            Debug.WriteLine(txt);
        }
        private System.Windows.Visibility Bool_to_Vis(bool txt)
        {
            if (txt) { return Visibility.Visible; } else { return Visibility.Collapsed; }
        }

        private async Task RunOnUiThread(Action a)
        {
            await this.Dispatcher.InvokeAsync(() =>
           {
               a();
           });
        }

        private async void PairDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_heartRateMonitor.IsConnected)
            {
                SelectedDeviceId = string.Empty;
                SelectedDeviceName = string.Empty;

                await _heartRateMonitor.DisconnectAsync();
            }

            var devicePicker = new DevicePicker();
            var result = devicePicker.ShowDialog();
            if (result.Value)
            {
                SelectedDeviceId = devicePicker.SelectedDeviceId;
                SelectedDeviceName = devicePicker.SelectedDeviceName;

                var connectResult= await _heartRateMonitor.ConnectAsync(SelectedDeviceId);
                if (!connectResult.IsConnected)
                    MessageBox.Show(connectResult.ErrorMessage);
            }
        }
        private async void ReConnectDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            if (_heartRateMonitor.IsConnected)
            {
                SelectedDeviceId = string.Empty;
                SelectedDeviceName = string.Empty;

                await _heartRateMonitor.DisconnectAsync();
            }


            //SelectedDeviceId = devicePicker.SelectedDeviceId;
            //SelectedDeviceName = devicePicker.SelectedDeviceName;

            var connectResult = await _heartRateMonitor.ConnectAsync(SelectedDeviceId);
            if (!connectResult.IsConnected)
                MessageBox.Show(connectResult.ErrorMessage);
            
        }

    }
}
