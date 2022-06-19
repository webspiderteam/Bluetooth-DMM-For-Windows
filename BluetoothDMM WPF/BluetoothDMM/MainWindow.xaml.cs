using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using HeartRateLE.Bluetooth.Events;
using System.ComponentModel;
using System.Globalization;
using LiveCharts;
using LiveCharts.Wpf;
using System.Linq;
using System.Windows.Input;
using System.Windows.Controls;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HeartRateLE.Bluetooth.HeartRateMonitor _heartRateMonitor;
        private string SelectedDeviceId { get; set; }
        private string SelectedDeviceName { get; set; }
        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }
        private string GattValue;
        private ZoomingOptions _zoomingMode;
        private double doublevalue;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private int tickcount;
        private bool DevicePickerActive = false;

        public MainWindow()
        {
            
            InitializeComponent();
            Tg_Btn.IsChecked = true;
            LV.SelectionChanged += LstOnSelectionChanced;
            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Series 1",
                    Values = new ChartValues<double> { 0 }
                },

            };
            ZoomingMode = ZoomingOptions.X;
            Labels = new[] { "0","", "", "", "", "", "", "", "", "", "5", "", "", "", "", "", "", "", "", "", "10"};
            //YFormatter = value => value.ToString("C");

            //modifying the series collection will animate and update the chart

            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);
            //modifying any series values will also animate and update the chart
            //Labels = Labels.Concat(new[] { "2" }).ToArray(); 
            //SeriesCollection[0].Values.Add(5d);
            //Labels = Labels.Concat(new[] { "3" }).ToArray();
            //SeriesCollection[0].Values.Add(5d);
            
            DataContext = this;
            _heartRateMonitor = new HeartRateLE.Bluetooth.HeartRateMonitor();

            // we should always monitor the connection status
            _heartRateMonitor.ConnectionStatusChanged -= HrDeviceOnDeviceConnectionStatusChanged;
            _heartRateMonitor.ConnectionStatusChanged += HrDeviceOnDeviceConnectionStatusChanged;

            //// we can create value parser and listen for parsed values of given characteristic
            //HrParser.ConnectWithCharacteristic(HrDevice.HeartRate.HeartRateMeasurement);
            _heartRateMonitor.RateChanged -= HrParserOnValueChanged;
            _heartRateMonitor.RateChanged += HrParserOnValueChanged;
            ChartView.MouseLeave -= ChartMouseLeave;
            ChartView.MouseLeave += ChartMouseLeave;
        }

        private void ChartMouseLeave(object sender, MouseEventArgs e)
        {
            //ChartView.ScrollMode = ScrollMode.X;
            //ChartView.mi  X.MinValue = double.NaN;
            //X.MaxValue = double.NaN;
        }

        private void ListViewItem_MouseEnter(object sender, MouseEventArgs e)
        {
            // Set tooltip visibility

            if (Tg_Btn.IsChecked == true)
            {
                tt_home.Visibility = Visibility.Collapsed;
                tt_contacts.Visibility = Visibility.Collapsed;
                tt_messages.Visibility = Visibility.Collapsed;

                
            }
            else
            {
                tt_home.Visibility = Visibility.Visible;
                tt_contacts.Visibility = Visibility.Visible;
                tt_messages.Visibility = Visibility.Visible;

                
            }
        }

        private void Tg_Btn_Unchecked(object sender, RoutedEventArgs e)
        {
            //img_bg.Opacity = 1;
        }

        private void Tg_Btn_Checked(object sender, RoutedEventArgs e)
        {
            //img_bg.Opacity = 0.3;
        }

        private void BG_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Tg_Btn.IsChecked = false;
        }

        private void CloseBtn_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
        protected async override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_heartRateMonitor.IsConnected)
            {
                await _heartRateMonitor.DisconnectAsync();
            }
        }
        public ZoomingOptions ZoomingMode
        {
            get { return _zoomingMode; }
            set
            {
                _zoomingMode = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        private async void HrParserOnValueChanged(object sender, RateChangedEventArgs arg)
        {
            await RunOnUiThread(() =>
            {
                d("Got new measurement: " + arg.MyGattCData);
                textBox.Text= arg.MyGattCData;
                //TxtHr.Text = String.Format("{0}", arg.MyGattCData);
                if (arg.MyGattCData.Length > 6) { MyGattCData.FontWeight = FontWeights.SemiBold; } else { MyGattCData.FontWeight = FontWeights.Bold; }
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
                GattValue = arg.MyGattCData;
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
        private void dispatcherTimer_Tick(object sender, EventArgs e)
        {
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000);
            try
            {
                float value = float.Parse(GattValue, CultureInfo.InvariantCulture.NumberFormat);
                doublevalue = Convert.ToDouble(value);
            }
            catch 
            {
                doublevalue = 0;
            }
            tickcount++;
            if (tickcount % 5 == 0)
            {
                Labels = Labels.Concat(new[] { (tickcount + 10).ToString() }).ToArray();
            } else { Labels = Labels.Concat(new[] { "" }).ToArray(); }
            
            SeriesCollection[0].Values.Add(doublevalue);
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
                    Is_Connected.IsChecked = true;
                }
                else
                {
                    TxtStatus.Text = SelectedDeviceName + ": disconnected";
                    TxtBattery.Text = "battery level: --";
                    //MyGattCDataBluetooth.Visibility = Visibility.Hidden;
                    dispatcherTimer.Stop();
                    //TxtHr.Text = "--";
                    Is_Connected.IsChecked = false;
                    if (_heartRateMonitor.IsConnected)
                    {
                        //SelectedDeviceId = string.Empty;
                        //SelectedDeviceName = string.Empty;

                        await _heartRateMonitor.DisconnectAsync();
                    }


                    //SelectedDeviceId = devicePicker.SelectedDeviceId;
                    //SelectedDeviceName = devicePicker.SelectedDeviceName;
                    while (!DevicePickerActive)
                    {
                        try
                        {
                            var connectResult = await _heartRateMonitor.ConnectAsync(SelectedDeviceId);
                            if (connectResult.IsConnected)
                                break;
                        }
                        catch
                        {
                            continue;
                        }
                    }
                }
                
                //BtnReadInfo.IsEnabled = connected;
            });
            
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

 
        private async void LstOnSelectionChanced(object sender, SelectionChangedEventArgs e)
        {
            var Sender = ((System.Windows.FrameworkElement)((System.Windows.Controls.Primitives.Selector)sender).SelectedItem);
            if (LV.SelectedIndex != -1)
            {
                if (Sender.Name == "ConnectTo")
                {
                    /*if (_heartRateMonitor.IsConnected)
                    {
                        SelectedDeviceId = string.Empty;
                        SelectedDeviceName = string.Empty;

                        await _heartRateMonitor.DisconnectAsync();
                    }*/

                    var devicePicker = new DevicePicker();
                    DevicePickerActive = true;
                    var result = devicePicker.ShowDialog();
                    if (result.Value)
                    {
                        SelectedDeviceId = devicePicker.SelectedDeviceId;
                        SelectedDeviceName = devicePicker.SelectedDeviceName;

                        var connectResult = await _heartRateMonitor.ConnectAsync(SelectedDeviceId);
                        if (!connectResult.IsConnected)
                            MessageBox.Show(connectResult.ErrorMessage);
                        SeriesCollection[0].Values.Clear();
                        Labels = new[] { "0", "", "", "", "", "", "", "", "", "", "5", "", "", "", "", "", "", "", "", "", "10" };
                    }
                    DevicePickerActive = false;
                }
                else if (Sender.Name == "Chart")
                {
                    _OnChart();
                }
                else if (Sender.Name == "OnTop")
                {
                    _OnTop();
                }
                LV.SelectedIndex = -1;
                Tg_Btn.IsChecked = false;
            }
        }
        private void _OnChart()
        {
            if (TopStackPanel.Visibility==Visibility.Visible)
            {
                dispatcherTimer.Stop();
                tickcount = 0;
                SeriesCollection[0].Values.Clear();
                Labels = new[] { "0", "", "", "", "", "", "", "", "", "", "5", "", "", "", "", "", "", "", "", "", "10" };
                
                TopStackPanel.Visibility=Visibility.Collapsed;
                ChartON.Visibility = Visibility.Hidden;
                this.Height = this.Height - 164;
            }
            else
            {
                dispatcherTimer.Start();
                TopStackPanel.Visibility = Visibility.Visible;
                ChartON.Visibility = Visibility.Visible;
                this.Height = this.Height + 164;
            }
        }
        private void _OnTop()
        {
            if (this.Topmost)
            {
                this.Topmost = false;
                OnTonON.Visibility = Visibility.Hidden;
            }
            else
            {
                this.Topmost = true;
                OnTonON.Visibility = Visibility.Visible;
            }
        }

        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
