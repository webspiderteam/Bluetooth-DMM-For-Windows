using HeartRateLE.Bluetooth;
using HeartRateLE.Bluetooth.Events;
using HeartRateLE.Bluetooth.Schema;
using Microsoft.Win32;
using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using uPLibrary.Networking.M2Mqtt;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Providers;

namespace BluetoothDMM
{

    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon m_notifyIcon;
        private System.Windows.Forms.ContextMenu TrayContextMenu;
        private HeartRateLE.Bluetooth.HeartRateMonitor _heartRateMonitor;
        public string SelectedDeviceId { get; private set; }
        public string SelectedDeviceName { get; private set; }
        public static Dictionary<string, string> DeviceListC { get; set; }
        public static Dictionary<string, string> iDeviceListC;
        private string GattValue;
        private double doublevalue;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private bool DevicePickerActive = false;
        private readonly Plot plt;
        private Tooltip txt;
        public double[] gattData;
        private double sampleRate = 1;
        private SignalPlot signalPlot;
        private MarkerPlot HighlightedPoint;
        private double LastHighlightedIndex = -1;
        private int nextDataIndex;
        private int ZoomScale = 30;
        private string OldACDC;
        private string OldSymbol;
        private bool onLoad;
        private bool Draggable;
        private int Connected;
        private bool GotFirstData = false;
        private HeartDeviceWatcher deviceWatcher = new HeartRateLE.Bluetooth.HeartDeviceWatcher(DeviceSelector.BluetoothLe);

        public MainWindow()
        {
            if (Properties.Settings.Default.CallUpgrade)
            {
                Properties.Settings.Default.Upgrade();
                Properties.MQTT.Default.Upgrade();
                Properties.MQTT.Default.Save();
                Properties.Settings.Default.CallUpgrade = false;
                Properties.Settings.Default.Save();
            }
            InitializeComponent();
            if (Properties.Settings.Default.Lang == "")
                LocalizeDictionary.Instance.Culture = CultureInfo.CurrentCulture;
            else
            {
                LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo(Properties.Settings.Default.Lang);
                System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Properties.Settings.Default.Lang);
            }
                
            var x = WPFLocalizeExtension.Engine.LocalizeDictionary.Instance.MergedAvailableCultures;
            (LocalizeDictionary.Instance.DefaultProvider as ResxLocalizationProvider).SearchCultures =
                    new List<System.Globalization.CultureInfo>()
                    {
                                    System.Globalization.CultureInfo.GetCultureInfo("ar"),
                                    System.Globalization.CultureInfo.GetCultureInfo("de"),
                                    System.Globalization.CultureInfo.GetCultureInfo("el"),
                                    System.Globalization.CultureInfo.GetCultureInfo("es"),
                                    System.Globalization.CultureInfo.GetCultureInfo("fr"),
                                    System.Globalization.CultureInfo.GetCultureInfo("it"),
                                    System.Globalization.CultureInfo.GetCultureInfo("ja"),
                                    System.Globalization.CultureInfo.GetCultureInfo("nl"),
                                    System.Globalization.CultureInfo.GetCultureInfo("en"),
                                    System.Globalization.CultureInfo.GetCultureInfo("pl"),
                                    System.Globalization.CultureInfo.GetCultureInfo("ru"),
                                    System.Globalization.CultureInfo.GetCultureInfo("tr"),
                                    System.Globalization.CultureInfo.GetCultureInfo("zh-CN"),
                                    System.Globalization.CultureInfo.GetCultureInfo("zh-TW")
                    };
            version.Text = "V " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0,4);
            if (Properties.Settings.Default.Remember)
            {
                (Width, Height) = (Properties.Settings.Default.WindowSize.Width, Properties.Settings.Default.WindowSize.Height);
                (Top, Left) = (Properties.Settings.Default.WindowPosition.X, Properties.Settings.Default.WindowPosition.Y);
            }
            this.Topmost = Properties.Settings.Default.Ontop;
            OnTopON.Visibility = (Properties.Settings.Default.Ontop ? Visibility.Visible : Visibility.Hidden);
            //System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            LV.SelectionChanged += LstOnSelectionChanced;
            //Chart Startup initialization
            plt = wpfPlot1.Plot;
            plt.Layout(10, 0, 0, 50, 0);
            plt.Style(ScottPlot.Style.Black);
            plt.Style(figureBackground: System.Drawing.Color.Transparent);
            plt.SetAxisLimits(0, 30);
            plt.XAxis.MinimumTickSpacing(5);
            plt.YAxis.MinimumTickSpacing(0.0001);
            plt.Margins(0.1, 0.2);
            ChartSetup();
            wpfPlot1.Plot.YAxis2.LockLimits(true);
            wpfPlot1.Configuration.LockVerticalAxis = true;
            wpfPlot1.Refresh();

            CustomDialog.PreviewMouseMove += new MouseEventHandler(CustomDialog_MouseMove);
            AboutDialog.PreviewMouseMove += new MouseEventHandler(CustomDialog_MouseMove);
            dispatcherTimer.Tick += new EventHandler(DispatcherTimer_Tick);

            DataContext = this;
            _heartRateMonitor = new HeartRateLE.Bluetooth.HeartRateMonitor
            {
                LogData = Properties.Settings.Default.LogData
            };
            // we should always monitor the connection status
            _heartRateMonitor.ConnectionStatusChanged -= HrDeviceOnDeviceConnectionStatusChanged;
            _heartRateMonitor.ConnectionStatusChanged += HrDeviceOnDeviceConnectionStatusChanged;

            //// we can create value parser and listen for parsed values of given characteristic
            //HrParser.ConnectWithCharacteristic(HrDevice.HeartRate.HeartRateMeasurement);
            _heartRateMonitor.RateChanged -= HrParserOnValueChanged;
            _heartRateMonitor.RateChanged += HrParserOnValueChanged;
            if (Properties.Settings.Default.ADisplay)
            {
                aDisplay.Visibility = Visibility.Collapsed;
                _ADisplay();
            }
            Connected = 1;
            //Properties.Settings.Default.DeviceID.Clear();
            //Properties.Settings.Default.Save();
            DeviceListC = new Dictionary<string, string>();
            if (Properties.Settings.Default.ConnectOn)
            {
                Tg_Btn.IsChecked = true;
                Tg_Btn.IsChecked = false;
                Connected = 0;
                DeviceListC = ConvertToDict(Properties.Settings.Default.DeviceID);
                //Reconnect();
            }
            else { Tg_Btn.IsChecked = true; }

            onLoad = true;
            m_notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                BalloonTipText = "Bluetooth DMM has been minimised to tray. Click the tray icon to show.",
                BalloonTipTitle = "Minimized to Tray",
                Text = "Bluetooth DMM",
                Icon = new System.Drawing.Icon(Application.GetResourceStream(new Uri("pack://application:,,,/BluetoothDMM;component/Assets/Logo.ico")).Stream),
                Visible = true
            };
            TrayContextMenu = new System.Windows.Forms.ContextMenu();
            TrayContextMenu.MenuItems.Add("&Show App", NotifyIcon_Click);
            TrayContextMenu.MenuItems.Add("E&xit",Exit_Click);
            m_notifyIcon.ContextMenu = TrayContextMenu;
            m_notifyIcon.Click += new EventHandler(NotifyIcon_Click);
            get_MQTT_Settings();
            if (MQTTSetup.MQTTEnabled)
                Task.Run(() =>
                {
                    try
                    {
                        mqttClient = new MqttClient(MQTTSetup.BrokerAddress, MQTTSetup.BrokerPort, MQTTSetup.isEncrypted, null, null, MQTTSetup.isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                        mqttClient.shouldReconnect = true;
                        if (MQTTSetup.UseLogin)
                            mqttClient.Connect(MQTTSetup.ClientId, MQTTSetup.Username, MQTTSetup.Password);
                        else
                            mqttClient.Connect(MQTTSetup.ClientId);
                    }
                    catch (Exception ex)
                    {
                    //MQTT Connection Error
                    d("MQTT Connection Error");
                    }
                });

        }

        private void get_MQTT_Settings()
        {
            MQTTSetup.MQTTEnabled = Properties.MQTT.Default.MQTTEnabled;
            MQTTSetup.addMac = Properties.MQTT.Default.addMac;
            MQTTSetup.BrokerAddress = Properties.MQTT.Default.BrokerAddress;
            MQTTSetup.BrokerPort = Properties.MQTT.Default.BrokerPort;
            MQTTSetup.ClientId = Properties.MQTT.Default.ClientId;
            MQTTSetup.isEncrypted = Properties.MQTT.Default.isEncrypted;
            MQTTSetup.Password = Properties.MQTT.Default.Password;
            MQTTSetup.Topic = Properties.MQTT.Default.Topic;
            MQTTSetup.UseLogin = Properties.MQTT.Default.UseLogin;
            MQTTSetup.Username = Properties.MQTT.Default.Username;
        }

        private void ChartSetup()
        {
            gattData = new double[1_000_000];
            signalPlot = plt.AddSignal(gattData, sampleRate, color: System.Drawing.Color.White);
            signalPlot.YAxisIndex = 0;
            signalPlot.LineWidth = 2;
            signalPlot.MarkerSize = 3;

            signalPlot.IsVisible = false;
            HighlightedPoint = plt.AddPoint(0, 0);
            HighlightedPoint.Color = System.Drawing.Color.Yellow;
            HighlightedPoint.MarkerSize = 10;
            HighlightedPoint.MarkerShape = ScottPlot.MarkerShape.openCircle;
            HighlightedPoint.IsVisible = false;

            txt = plt.AddTooltip("Data", 0, 0);
            txt.Font.Color = System.Drawing.Color.White;
            txt.FillColor = System.Drawing.Color.FromArgb(190, 107, 126, 243);
            txt.BorderWidth = 1;
            txt.BorderColor = System.Drawing.Color.White;
            txt.Font.Bold = true;
            txt.Font.Size = 12;
            txt.IsVisible = false;
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Close();
        }

        private WindowState m_storedWindowState = WindowState.Normal;
        private Point Startpoint;
        private string ResultDialog;
        private MqttClient mqttClient;

        private void OnStateChanged(object sender, EventArgs args)
        {
            if (Properties.Settings.Default.MinimizeTray)
                if (WindowState == WindowState.Minimized)
                {
                    MyPopup.IsOpen = false;
                    Tg_Btn.IsChecked = false;
                    Hide();
                    if (m_notifyIcon != null && !onLoad)
                        m_notifyIcon.ShowBalloonTip(2000);
                }
                else
                    m_storedWindowState = WindowState;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args)
        {
            CheckTrayIcon();
        }

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            Show();
            WindowState = m_storedWindowState;
        }

        private void CheckTrayIcon()
        {
            ShowTrayIcon(!IsVisible);
        }

        private void ShowTrayIcon(bool show)
        {
            if (m_notifyIcon != null)
                m_notifyIcon.Visible = show;
        }

        private Dictionary<string, string> ConvertToDict(StringCollection deviceID)
        {
            Dictionary<string, string> Temp = new Dictionary<string, string>();
            if (deviceID == null)
                return Temp;
            foreach (string Device in deviceID)
            {
                var Strings=Device.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                Temp.Add(Strings[0], Strings[1]);
            }
            return Temp;
        }

        private void Tg_Btn_Unchecked(object sender, RoutedEventArgs e)
        {
            MyPopup.IsOpen = false;
            Tg_Btn.IsChecked = false;

        }

        private void Tg_Btn_Checked(object sender, RoutedEventArgs e)
        {
            MyPopup.IsOpen = false;
            MyPopup.PlacementTarget = sender as UIElement;
            MyPopup.Placement = PlacementMode.Relative;
            MyPopup.HorizontalOffset = -20;
            MyPopup.VerticalOffset = -35;
            MyPopup.AllowsTransparency = true;
            MyPopup.PopupAnimation = PopupAnimation.Fade;
            MyPopup.IsOpen = true;
        }

        private void BG_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            MyPopup.IsOpen = false;
            Tg_Btn.IsChecked = false;
        }

        protected async override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);

            if (_heartRateMonitor.IsConnected)
            {
                await _heartRateMonitor.DisconnectAsync();
            }
            if (Properties.Settings.Default.Remember)
            {
                Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)ActualWidth, (int)ActualHeight);
                Properties.Settings.Default.WindowPosition = new System.Drawing.Point((int)Top, (int)Left);
                Properties.Settings.Default.Save();
            }
            if (mqttClient!=null && mqttClient.IsConnected)
                mqttClient.Disconnect();
            m_notifyIcon.Dispose();
            m_notifyIcon = null;
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
                GotFirstData = true;
                //textBox.Text = arg.MyGattCData;
                MyGattCData.FontWeight = arg.MyGattCData.Length > 6 ? FontWeights.SemiBold : FontWeights.Bold;
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
                Battery.IsChecked = arg.MyGattCDataBattery;
                MyGattCDataHV.Visibility = Bool_to_Vis(arg.MyGattCDataHV);
                MyGattCDataRel.Visibility = Bool_to_Vis(arg.MyGattCDataRel);
                GattValue = arg.MyGattCData;
                try { doublevalue = Convert.ToDouble(GattValue,CultureInfo.InvariantCulture); }
                catch { }//doublevalue = 0;}
                if (GattValue != null)
                {
                    var result = Math.Abs(doublevalue).ToString().Split(new string[] { ".", " " }, StringSplitOptions.RemoveEmptyEntries);
                    double maxvalue = 6 * Math.Pow(10, result[0].Length - 1);
                    double currentValue = Math.Abs(doublevalue);
                    if (maxvalue < currentValue )
                        maxvalue *= 10;
                    Meter.Value = currentValue * 100 / maxvalue;
                }
                MyGattCData.Foreground = arg.MyGattCDataHold ? Brushes.Red : Brushes.White;
            });
        }
        
        private void DispatcherTimer_Tick(object sender, EventArgs e)
        {
            dispatcherTimer.Interval = TimeSpan.FromMilliseconds(1000);
            bool exCondition = MyGattCDataSymbol.Text == "" && MyGattCDataACDC.Text == "";
            if (Is_Connected.IsChecked == true && GotFirstData )
            {
                if (!exCondition)
                {
                    signalPlot.IsVisible = true;
                    if (MyGattCDataSymbol.Text != OldSymbol || MyGattCDataACDC.Text != OldACDC)
                    {
                        if (nextDataIndex > 1)
                            AddVLine();
                        OldACDC = MyGattCDataACDC.Text;
                        OldSymbol = MyGattCDataSymbol.Text;
                    }
                    gattData[nextDataIndex] = doublevalue;
                    signalPlot.MaxRenderIndex = nextDataIndex;
                    if (!wpfPlot1.IsMouseOver)
                    {
                        txt.IsVisible = false;
                        FitChart(nextDataIndex - ZoomScale, nextDataIndex);
                    }
                    wpfPlot1.Refresh();
                    nextDataIndex += 1;
                }
                if (mqttClient != null && mqttClient.IsConnected)
                {
                    string Mac = "";
                    if (MQTTSetup.addMac)
                        Mac = "/" + SelectedDeviceId.Substring(SelectedDeviceId.Length - 17, 17).ToUpper().Replace(":", "_");
                    mqttClient.Publish($"{MQTTSetup.ClientId}{Mac}/{MQTTSetup.Topic}", System.Text.Encoding.UTF8.GetBytes(
                        $"{{ Time:\"{DateTime.Now}\"," +
                        $"Device:\"{SelectedDeviceName}\" ," +
                        $"Value:\"{MyGattCData.Text}\", " +
                        $"Range:\"{MyGattCDataSymbol.Text}\", " +
                        $"Current:\"{MyGattCDataACDC.Text} "));
                }
            }
        }
        
        private async void HrDeviceOnDeviceConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            d("Current connection status is: " + args.IsConnected);
            await RunOnUiThread(async () =>
            {
                bool connected = args.IsConnected;
                if (connected)
                {
                    //var device = await _heartRateMonitor.GetDeviceInfoAsync();
                    TxtStatus.Text = SelectedDeviceName + " : " + Properties.Resources.Connected;
                    //TxtBattery.Text = String.Format("battery level: {0}%", device.BatteryPercent);
                    MyGattCDataBluetooth.Visibility = Visibility.Visible;
                    Is_Connected.IsChecked = true;
                    dispatcherTimer.Start();
                    Connected = 1;
                    if (mqttClient != null && mqttClient.IsConnected)
                    {
                        mqttClient.Publish($"{MQTTSetup.ClientId}/{MQTTSetup.Topic}", System.Text.Encoding.UTF8.GetBytes(
                            $"Status: \"Connected\", " +
                            $"MAC: \"{SelectedDeviceId.Substring(SelectedDeviceId.Length - 17, 17).ToUpper().Replace(":", "_")}\", " +
                            $"UseMAC: \"{MQTTSetup.addMac.ToString()}\""));
                    }
                }
                else
                {
                    TxtStatus.Text = SelectedDeviceName + " : " + Properties.Resources.Disconnected;
                    TxtBattery.Text = "battery level: --";
                    dispatcherTimer.Stop();
                    Is_Connected.IsChecked = false;
                    if (Properties.Settings.Default.Reconnect)
                        Connected = 0;
                    if (mqttClient != null && mqttClient.IsConnected)
                    {
                        mqttClient.Publish($"{MQTTSetup.ClientId}/{MQTTSetup.Topic}", System.Text.Encoding.UTF8.GetBytes(
                            $"Status: \"Disconnected\", " +
                            $"MAC: \"{SelectedDeviceId.Substring(SelectedDeviceId.Length - 17, 17).ToUpper().Replace(":", "_")}\""));
                    }
                }
            });
        }

        private async Task SearchDevices()
        {
            iDeviceListC = new Dictionary<string, string>();
            try
            {
                deviceWatcher.DeviceAdded += async (watcher, args) =>
                {
                    
                    if (Connected==0 && !DevicePickerActive)
                    {
                        if (DeviceListC.ContainsKey(args.Device.Id))
                            iDeviceListC.Remove(args.Device.Id);
                        //Debug.WriteLine($"Added {args.Device.Name} Connected: {args.Device.Id} IsConnectable: {(args.Device.Properties.ContainsKey("System.Devices.Aep.Bluetooth.Le.IsConnectable") ? (bool)args.Device.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"] : false)}");
                        if (DeviceListC.ContainsKey(args.Device.Id)  && (args.Device.Properties.ContainsKey("System.Devices.Aep.Bluetooth.Le.IsConnectable") && (bool)args.Device.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"]))
                        {
                            SelectedDeviceId = args.Device.Id;
                            SelectedDeviceName = DeviceListC[args.Device.Id];
                            //Debug.WriteLine($"Added {args.Device.Name} Connected : {args.Device.IsConnected}" + "IsConnectable");
                            Connected = 2;
                            string result;
                            if ((bool)Properties.Settings.Default.AskOnConnect)
                                result = await ShowCustomDialog(SelectedDeviceName);
                            else
                                result = "Button1";
                            if (result == "Button1")
                            {
                                Debug.WriteLine("Connecting  From Updated Event");
                                var connectResult = await _heartRateMonitor.ConnectAsync(SelectedDeviceId);
                                if (connectResult.IsConnected)
                                    Debug.WriteLine("Connected  From Updated Event");
                            }
                            else
                            {
                                iDeviceListC.Add(args.Device.Id, DeviceListC[args.Device.Id]);
                                Connected = 0;
                            }
                        }
                    }
                };
                deviceWatcher.DeviceUpdated += async (watcher, args) =>
                {
                    if (Connected==0 && !DevicePickerActive)
                    {
                        bool IsConnectable = args.Device.Properties.ContainsKey("System.Devices.Aep.Bluetooth.Le.IsConnectable") && (bool)args.Device.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"];
                        //Debug.WriteLine($"Updated {args.Device.Id} IsConnectable : {IsConnectable}");
                        if (!iDeviceListC.ContainsKey(args.Device.Id) && DeviceListC.ContainsKey(args.Device.Id) && IsConnectable)
                        {
                            SelectedDeviceId = args.Device.Id;
                            SelectedDeviceName = DeviceListC[args.Device.Id];
                            //Debug.WriteLine($"Added {args.Device.Name} Connected : {args.Device.IsConnected}" + "IsConnectable");
                            Connected = 2;
                            string result;
                            if ((bool)Properties.Settings.Default.AskOnConnect)
                                result = await ShowCustomDialog(SelectedDeviceName);
                            else
                                result = "Button1";
                            if (result == "Button1")
                            {
                                Debug.WriteLine("Connecting From Updated Event");
                                try
                                {
                                    var connectResult = await _heartRateMonitor.ConnectAsync(SelectedDeviceId);
                                    if (connectResult.IsConnected)
                                        Debug.WriteLine("Connected From Updated Event");
                                }
                                catch (Exception ex)
                                {
                                    MessageBox.Show(ex.Message);
                                }

                            }
                            else
                            {
                                iDeviceListC.Add(args.Device.Id, DeviceListC[args.Device.Id]);
                                Connected = 0;
                            }
                        }
                    }
                };
                deviceWatcher.DeviceRemoved += (watcher, args) => { };// Debug.WriteLine($"Removed {args.Device.Id}"); };
                deviceWatcher.DeviceEnumerationCompleted += (watcher, args) => { Debug.WriteLine("No more devices found"); };
                deviceWatcher.DeviceEnumerationStopped += (watcher, args) => { Debug.WriteLine("Device Enumeration Stopped"); };
                deviceWatcher.Start();
            }
            catch (ArgumentException ex) { Debug.WriteLine("MainSearch" + ex.Message); }
            Debug.WriteLine("Device Enumeration Stopped?");
        }

        private async Task<string> ShowCustomDialog(string DeviceName)
        {
            ResultDialog = "0";
            await RunOnUiThread(async () =>
            {
                CustomDialogHeader.Text = DeviceName;
                CustomDialog.IsOpen = true;
            });

            while (ResultDialog == "0")
                continue;
            return ResultDialog;
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
                    var devicePicker = new DevicePicker(DeviceListC);
                    DevicePickerActive = true;
                    var result = devicePicker.ShowDialog();
                    if (result.Value)
                    {
                        SelectedDeviceId = devicePicker.SelectedDeviceId;
                        SelectedDeviceName = !DeviceListC.ContainsKey(SelectedDeviceId) ? devicePicker.SelectedDeviceName : DeviceListC[key: SelectedDeviceId];
                        Connected = 2;
                        var connectResult = await _heartRateMonitor.ConnectAsync(SelectedDeviceId);
                        if (connectResult.IsConnected)
                        {
                            if (!DeviceListC.ContainsKey(SelectedDeviceId))
                            {
                                if (Properties.Settings.Default.ConnectOn)
                                {
                                    if (Properties.Settings.Default.DeviceID == null)
                                        Properties.Settings.Default.DeviceID = new StringCollection();
                                    Properties.Settings.Default.DeviceID.Add(devicePicker.SelectedDeviceId + "\n" + devicePicker.SelectedDeviceName);
                                    Properties.Settings.Default.Save();
                                }
                                DeviceListC.Add(devicePicker.SelectedDeviceId, devicePicker.SelectedDeviceName);
                            }
                            if (DeviceListC.ContainsKey(SelectedDeviceId))
                                iDeviceListC.Remove(SelectedDeviceId);
                        }
                        else
                            MessageBox.Show(connectResult.ErrorMessage);

                    }
                    DevicePickerActive = false;
                }
                else if (Sender.Name == "Chart")
                    _OnChart();
                else if (Sender.Name == "OnTop")
                    _OnTop();
                else if (Sender.Name == "ADisplay")
                    _ADisplay();
                else if (Sender.Name == "About")
                    AboutDialog.IsOpen=true;
                LV.SelectedIndex = -1;
                Tg_Btn.IsChecked = false;
            }
        }
        private void _OnChart()
        {
            if (TopStackPanel.Visibility == Visibility.Visible)
            {
                dispatcherTimer.Stop();
                TopStackPanel.Visibility = Visibility.Collapsed;
                ChartON.Visibility = Visibility.Hidden;
                Height = grid.RowDefinitions[0].ActualHeight;
            }
            else
            {
                dispatcherTimer.Start();
                TopStackPanel.Visibility = Visibility.Visible;
                ChartON.Visibility = Visibility.Visible;
                this.Height = 160 + grid.RowDefinitions[0].ActualHeight + 20;
            }
        }
        
        private void _OnTop()
        {
            if (this.Topmost)
            {
                this.Topmost = false;
                OnTopON.Visibility = Visibility.Hidden;
            }
            else
            {
                this.Topmost = true;
                OnTopON.Visibility = Visibility.Visible;
            }
        }
        
        private void _ADisplay()
        {
            if (aDisplay.Visibility==Visibility.Visible)
            {
                TxtStatus.Margin = new Thickness(0, 10, 0, 0);
                Display.Margin = new Thickness(0, 5, 0, 0);
                aDisplay.Visibility = Visibility.Collapsed;
                ADisplayON.Visibility = Visibility.Hidden;
            }
            else
            {
                
                TxtStatus.Margin= new Thickness(0, -70, 0, 0);
                Display.Margin= new Thickness(0, 45, 0, 0);
                aDisplay.Visibility = Visibility.Visible;
                ADisplayON.Visibility = Visibility.Visible;
            }
        }
        
        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {
            var Settings = new Settings(DeviceListC)
            {
                Topmost = this.Topmost
            };
            var result = Settings.ShowDialog();
            if ((bool)result)
            {
                if (Properties.Settings.Default.Lang == "")
                    LocalizeDictionary.Instance.Culture = CultureInfo.CurrentCulture;
                else
                {
                    LocalizeDictionary.Instance.Culture = new System.Globalization.CultureInfo(Properties.Settings.Default.Lang);
                    System.Threading.Thread.CurrentThread.CurrentUICulture = new System.Globalization.CultureInfo(Properties.Settings.Default.Lang);
                }
                if (Properties.Settings.Default.Remember)
                {
                    Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)ActualWidth, (int)ActualHeight);
                    Properties.Settings.Default.WindowPosition = new System.Drawing.Point((int)Top, (int)Left);
                    Properties.Settings.Default.Save();
                }

                if (Properties.Settings.Default.ADisplay)
                {
                    aDisplay.Visibility = Visibility.Collapsed;
                    _ADisplay();
                }
                if (Properties.Settings.Default.Reconnect && SelectedDeviceId != null && Is_Connected.IsChecked == false)
                    Connected = 0;
                if (Properties.Settings.Default.ConnectOn)
                {
                    if (DeviceListC == null)
                        DeviceListC = new Dictionary<string, string>();
                    DeviceListC = ConvertToDict(Properties.Settings.Default.DeviceID);
                    if (SelectedDeviceId != null && DeviceListC.ContainsKey(SelectedDeviceId))
                    {
                        SelectedDeviceName = DeviceListC[SelectedDeviceId];
                        if (Connected == 1)
                            TxtStatus.Text = SelectedDeviceName + " : " + Properties.Resources.Connected;
                        else
                            TxtStatus.Text = SelectedDeviceName + " : " + Properties.Resources.Disconnected;
                    }
                    else
                        TxtStatus.Text = Properties.Resources.NotConnected;
                }
                if (Properties.MQTT.Default.MQTTEnabled)
                {
                    get_MQTT_Settings();
                    if (mqttClient != null && mqttClient.IsConnected)
                        mqttClient.Disconnect();
                    Task.Run(() =>
                    {
                        try
                        {
                            mqttClient = new MqttClient(MQTTSetup.BrokerAddress, MQTTSetup.BrokerPort, MQTTSetup.isEncrypted, null, null, MQTTSetup.isEncrypted ? MqttSslProtocols.SSLv3 : MqttSslProtocols.None);
                            mqttClient.shouldReconnect = true;
                            if (MQTTSetup.UseLogin)
                                mqttClient.Connect(MQTTSetup.ClientId, MQTTSetup.Username, MQTTSetup.Password);
                            else
                                mqttClient.Connect(MQTTSetup.ClientId);
                            if (mqttClient != null && mqttClient.IsConnected)
                            {
                                mqttClient.Publish($"{MQTTSetup.ClientId}/{MQTTSetup.Topic}", System.Text.Encoding.UTF8.GetBytes(
                                    $"Status: \"Connected\", " +
                                    $"MAC: \"{SelectedDeviceId.Substring(SelectedDeviceId.Length - 17, 17).ToUpper().Replace(":", "_")}\", " +
                                    $"UseMAC: \"{MQTTSetup.addMac.ToString()}\""));
                            }
                        }
                        catch (Exception ex)
                        {
                            //MQTT Connection Error
                            d("MQTT Connection Error");
                            MessageBox.Show(ex.Message);
                        }
                    });
                }
                _heartRateMonitor.LogData = Properties.Settings.Default.LogData;
            }
        }

        private void WpfPlot1_MouseMove(object sender, MouseEventArgs e)
        {
            Draggable = false;
            // determine point nearest the cursor
            (double mouseCoordX, _) = wpfPlot1.GetMouseCoordinates();
            (double pointX, double pointY, int pointIndex) = signalPlot.GetPointNearestX(mouseCoordX);

            // render if the highlighted point chnaged

            if (LastHighlightedIndex != pointIndex && pointIndex <= nextDataIndex)
            {
                // place the highlight over the point of interest
                HighlightedPoint.X = pointX;
                HighlightedPoint.Y = pointY;
                HighlightedPoint.IsVisible = true;
                var plottablelist = plt.GetPlottables();
                int LastX = 0;
                foreach (var plotT in plottablelist)
                {
                    var name = plotT.GetType().Name;
                    if (plotT.GetType().Name == "Text")
                    {
                        if (pointIndex <= (int)((ScottPlot.Plottable.Text)plotT).X && pointIndex > LastX)
                        {
                            string[] st = SymbolToText(((ScottPlot.Plottable.Text)plotT).Label);
                            txt.Label = $" {st[0]} {pointY}{st[1]} \n at {pointX} ";
                        }
                        LastX = (int)((ScottPlot.Plottable.Text)plotT).X;
                    }
                }
                if (LastX < pointIndex)
                    txt.Label = $" {MyGattCDataACDC.Text} {pointY}{MyGattCDataSymbol.Text} \n at {pointX} ";

                LastHighlightedIndex = pointIndex;

                txt.X = pointX;
                txt.Y = pointY;
                txt.IsVisible = true;

                wpfPlot1.Render();
            }

            // update the GUI to describe the highlighted point
            //double mouseX = e.GetPosition(this).X;
            //double mouseY = e.GetPosition(this).Y;
            //label1.Content = $"Closest point to ({mouseX:N2}, {mouseY:N2}) " +
            //   $"is index {pointIndex} ({pointX:N2}, {pointY:N2})";
        }
        
        private static string[] SymbolToText(string Value)
        {
            Value = Value.Replace('*', ' ');
            var result = Value.Split(new string[] { "\n", " " }, StringSplitOptions.RemoveEmptyEntries);
            string s1;
            string s2;
            if (result.Length == 2)
            {
                s1 = result[0];
                s2 = result[1];
            }
            else if (result.Length==1)
            {
                s1 = " ";
                s2 = result[0];
            } else { s1 = s2 = " "; }

            return new string[] { s1, s2 };
        }

        private void WpfPlot1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // determine the axis where we are now
            var axes = plt.GetAxisLimits();
            int xLow = (int)(axes.XMin + ZoomScale * 0.02);
            int xHigh = (int)(axes.XMax - ZoomScale * 0.02);
            if (e.ChangedButton != MouseButton.Left)
                ZoomScale = xHigh - xLow;
            TxtBattery.Text = ZoomScale.ToString();
            //set the Y axis limits to the high and low of the range
            FitChart(xLow, xHigh);
            wpfPlot1.Refresh();
        }

        private void FitChart(int xLow, int xHigh)
        {

            if (ZoomScale < 0)
            {
                plt.AxisAutoX();
                plt.AxisAutoY(null, 0);
            }
            else
            {
                double yLow = gattData[0];
                double yHigh = gattData[0];
                int DataLow = xLow;
                int DataHigh = xHigh;
                int lastDataIndex = (int)plt.GetDataLimits().XMax;
                if (DataLow < 0 || lastDataIndex == 0 || lastDataIndex < ZoomScale)
                {
                    DataLow = 0;
                    DataHigh = ZoomScale + 1;
                    xLow = 0;
                }
                if (DataHigh > lastDataIndex) { DataHigh = lastDataIndex; }

                if (lastDataIndex > 0)
                {
                    yLow = gattData.Skip(DataLow).Take(DataHigh - DataLow + 1).Min();
                    yHigh = gattData.Skip(DataLow).Take(DataHigh - DataLow + 1).Max();
                }
                //set the Y axis limits to the high and low of the range
                //plt.SetAxisLimitsX(nextDataIndex - 30, nextDataIndex - 0.5);
                plt.SetAxisLimits(xLow - ZoomScale * 0.02, xLow + ZoomScale + ZoomScale * 0.02, yLow - (yHigh - yLow) * plt.Margins().y, yHigh + (yHigh - yLow) * plt.Margins().y);
            }
        }

        private void AddVLine()
        {
            var vline = plt.AddVerticalLine(nextDataIndex - 1);
            vline.YAxisIndex = 1;
            vline.LineWidth = 2;
            var axes = plt.GetAxisLimits(0, 1);

            int yHigh = (int)axes.YMax;
            var txtvline1 = plt.AddText((OldACDC == "" ? string.Empty : "  " + OldACDC + " * \n") + "  " + OldSymbol + " *  ", nextDataIndex - 1, yHigh);
            txtvline1.YAxisIndex = 1;
            txtvline1.BorderColor = System.Drawing.Color.White;   // controls whether anything can be dragged
            txtvline1.BorderSize = 1; // controls whether points can be dragged horizontally 
            txtvline1.PixelOffsetX = -50;
            txtvline1.PixelOffsetY = -5;
            txtvline1.BackgroundFill = true;
            txtvline1.BackgroundColor = System.Drawing.Color.FromArgb(190, 107, 126, 243);
            txtvline1.FontBold = true;
            txtvline1.FontSize = 12;// controls whether points can be dragged vertically
            txtvline1.Font.Color = System.Drawing.Color.White;

        }
        
        private void WpfPlot1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var axes = plt.GetAxisLimits();
            int xLow = (int)axes.XMin;
            int xHigh = (int)axes.XMax;
            ZoomScale = xHigh - xLow - 1;
            //set the Y axis limits to the high and low of the range
            FitChart(xLow, xHigh);
            wpfPlot1.Refresh();
        }

        private void MyPopup_Closed(object sender, EventArgs e)
        {
            Tg_Btn.IsChecked = false;
        }

        private void ChartSettings_Click(object sender, RoutedEventArgs e)
        {
            if (SettingPopup.IsOpen == false)
            {
                SettingPopup.PlacementTarget = sender as UIElement;
                SettingPopup.IsOpen = true;
            }
            else { SettingPopup.IsOpen = false; }

        }

        private void Chart_Reset_Click(object sender, RoutedEventArgs e)
        {
            ZoomScale = 30;
            FitChart(nextDataIndex - ZoomScale, nextDataIndex + 1);
            wpfPlot1.Refresh();
            SettingPopup.IsOpen = false;
        }

        private void Chart_Fit_Click(object sender, RoutedEventArgs e)
        {
            ZoomScale = -1;
            FitChart(0, 0);
            wpfPlot1.Refresh();
            SettingPopup.IsOpen = false;
        }

        private void Chart_Import_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Filter = "DMM Chart Data file (*.dcd)|*.dcd"
            };
            if (openFileDialog.ShowDialog() == true)
            {
                var newplt = new ScottPlot.Plot(600, 400);
                byte[] bytes = File.ReadAllBytes(openFileDialog.FileName);
                int DataLen = BitConverter.ToInt32(bytes, bytes.Length - sizeof(int)) + 1;
                byte[] VLines = new byte[(bytes.Length - DataLen * sizeof(double))];
                Array.Copy(bytes, (DataLen * sizeof(double)), VLines, 0, (bytes.Length - DataLen * sizeof(double)));
                double[] ImportedData = Enumerable.Range(0, DataLen).Select(offset => BitConverter.ToDouble(bytes, offset * sizeof(double))).ToArray();

                byte[] vLinesS = Enumerable.Range(0, VLines.Length / 5).Select(offset => VLines[offset * 5]).ToArray();
                int[] vLinesX = Enumerable.Range(0, VLines.Length / 5)
                                        .Select(offset => BitConverter.ToInt32(VLines, (offset * 5) + 1)).ToArray();

                var dataWindow = new DataViewer(ImportedData, vLinesS, vLinesX);
                dataWindow.Show();
            }
            SettingPopup.IsOpen = false;
            dispatcherTimer.Start();
        }

        private static byte SymbolToByte(string Value)
        {
            Value = Value.Replace('*', ' ');
            var result = Value.Split(new string[] { "\n", " " }, StringSplitOptions.RemoveEmptyEntries);

            //var result = Value.Split(new string[] { ";" } , StringSplitOptions.RemoveEmptyEntries);
            string s1;
            string s2;
            int final = 0;
            if (result.Length == 2)
            {
                s1 = result[0];
                s2 = result[1];
            }
            else if (result.Length==1)
            {
                s1 = " ";
                s2 = result[0];
            } else { s1 = s2 = " "; }

            switch (s1)
            {
                case "Δ": final = 3; break;
                case "AC": final = 2; break;
                case "DC": final = 1; break;
                default: final = 0; break;
            }

            switch (s2)
            {
                case "µA": final += 10; break;
                case "mA": final += 20; break;
                case "A": final += 30; break;
                case "mV": final += 40; break;
                case "V": final += 50; break;
                case "nF": final += 60; break;
                case "µF": final += 70; break;
                case "F": final += 80; break;
                case "mF": final += 90; break;
                case "MΩ": final += 100; break;
                case "kΩ": final += 110; break;
                case "Ω": final += 120; break;
                case "KHz": final += 130; break;
                case "MHz": final += 140; break;
                case "Hz": final += 150; break;
                case "°C": final += 160; break;
                case "°F": final += 170; break;
                case "%": final += 180; break;
                default: final = 9; break;
            }
            return (byte)final;
        }

        private void Chart_Export_Click(object sender, RoutedEventArgs e)
        {
            dispatcherTimer.Stop();
            //File.AppendAllText("debug.txt", "start export" + System.Environment.NewLine);
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "DMM Chart Data file (*.dcd)|*.dcd|CSV file (*.csv)|*.csv"
            };
            var result = saveFileDialog.ShowDialog();
            var extension = Path.GetExtension(saveFileDialog.FileName);
            if (result == true && nextDataIndex > 0)
            {
                if (extension == ".dcd")
                {
                    double[] ChartData = gattData.Take((int)plt.GetDataLimits().XMax + 1).ToArray();
                    byte[] Chartbytes = ChartData.SelectMany(value => BitConverter.GetBytes(value)).ToArray();
                    var plottablelist = plt.GetPlottables();
                    System.Collections.Generic.List<byte> VlineList = new System.Collections.Generic.List<byte>();
                    foreach (var plotT in plottablelist)
                    {
                        var name = plotT.GetType().Name;
                        if (plotT.GetType().Name == "Text")
                        {
                            byte sb = SymbolToByte(((ScottPlot.Plottable.Text)plotT).Label);
                            VlineList.Add(sb);
                            byte[] pb = BitConverter.GetBytes((int)((ScottPlot.Plottable.Text)plotT).X);
                            foreach (var pba in pb)
                                VlineList.Add(pba);
                        }
                    }
                    byte sbe = SymbolToByte((MyGattCDataACDC.Text == "" ? string.Empty : "  " + MyGattCDataACDC.Text + " * \n") + "  " + MyGattCDataSymbol.Text + " *  ");
                    VlineList.Add(sbe);
                    byte[] pbe = BitConverter.GetBytes((int)plt.GetDataLimits().XMax);
                    foreach (var pba in pbe)
                        VlineList.Add(pba);

                    byte[] Vlines = VlineList.ToArray();
                    byte[] Final = Chartbytes.Concat(Vlines).ToArray();
                    File.WriteAllBytes(saveFileDialog.FileName, Final);
                }
                else if (extension == ".csv")
                {
                    try
                    {
                        double[] ChartData = gattData.Take((int)plt.GetDataLimits().XMax + 1).ToArray();
                        string[] createText = new string[(int)plt.GetDataLimits().XMax + 2];
                        createText[0] = "Time,CurrentType,Value,Symbol";
                        var plottablelist = plt.GetPlottables();
                        Dictionary<int, string> keyValuePairs = new Dictionary<int, string>();
                        foreach (var plotT in plottablelist)
                        {
                            var name = plotT.GetType().Name;
                            if (plotT.GetType().Name == "Text")
                            {
                                string sb = ((ScottPlot.Plottable.Text)plotT).Label;
                                int pb = (int)((ScottPlot.Plottable.Text)plotT).X;
                                keyValuePairs.Add(pb, sb);
                            }
                        }
                        string sbe = (MyGattCDataACDC.Text == "" ? string.Empty : "  " + MyGattCDataACDC.Text + " * \n") + "  " + MyGattCDataSymbol.Text + " *  ";
                        int pbe = (int)plt.GetDataLimits().XMax;
                        keyValuePairs.Add(pbe, sbe);
                        int pairs_i = 0;
                        foreach (var (value, index) in ChartData.Select((v, i) => (v, i)))
                        {
                            string Value = keyValuePairs.ElementAt(pairs_i).Value.Replace('*', ' ');
                            var resultval = Value.Split(new string[] { "\n", " " }, StringSplitOptions.RemoveEmptyEntries);
                            string s1;
                            string s2;
                            if (resultval.Length == 2)
                            {
                                s1 = resultval[0];
                                s2 = resultval[1];
                            }
                            else if (resultval.Length==1)
                            {
                                s1 = " ";
                                s2 = resultval[0];
                            }
                            else { s1 = s2 = " "; }
                            if (index == keyValuePairs.ElementAt(pairs_i).Key)
                                pairs_i++;
                            createText[index + 1] = $"{index},{s1},{value},{s2}";
                        }
                        File.WriteAllLines(saveFileDialog.FileName, createText, System.Text.Encoding.UTF8);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message.ToString());
                        File.AppendAllText("debug.txt", "error catch Error: " + ex.ToString() + System.Environment.NewLine);
                    }
                    
                }
            }
            SettingPopup.IsOpen = false;
            dispatcherTimer.Start();
        }

        private void Window_MouseDown(Object sender, MouseButtonEventArgs e)
        {
            if (Draggable)
                this.DragMove();
        }
        
        private void HandleLinkClick(object sender, RoutedEventArgs e)
        {
            Hyperlink hl = (Hyperlink)sender;
            string navigateUri = hl.NavigateUri.ToString();
            Process.Start(new ProcessStartInfo(navigateUri));
            e.Handled = true;
        }
        
        private async void Window_Activated(object sender, EventArgs e)
        {
            if (onLoad)
            {
                await SearchDevices();
                if (Properties.Settings.Default.ChartOn )
                {
                    
                    TopStackPanel.Visibility = Visibility.Visible;
                    ChartON.Visibility = Visibility.Visible;
                    if ((grid.RowDefinitions[0].ActualHeight + 90 > this.ActualHeight))
                        this.Height = grid.RowDefinitions[0].ActualHeight + 180;
                }
                if (Properties.Settings.Default.MinimizeTray)
                {
                    string[] arguments = Environment.GetCommandLineArgs();
                    if (arguments.Length > 1 && arguments[1] == "-m")
                        WindowState = WindowState.Minimized;
                }
                //Console.WriteLine("xxxGetCommandLineArgs: {0}", string.Join(", ", arguments));
                onLoad = false;
            }
        }

        private void WpfPlot1_MouseLeave(object sender, MouseEventArgs e)
        {
            Draggable = true;
        }

        private void CustomDialog_MouseMove(object sender, MouseEventArgs e)
        {
            Draggable = false;
            var uiElement = (Popup)sender;
            bool controlsnotover = !Button1.IsMouseOver && !Button1.IsMouseOver && !Button1.IsMouseOver && !DontAsk.IsMouseOver;
            if (e.LeftButton == MouseButtonState.Pressed && controlsnotover)
            {
                Point relative = e.GetPosition(null);
                uiElement.PlacementRectangle = new Rect(relative.X + uiElement.PlacementRectangle.X - Startpoint.X, relative.Y + uiElement.PlacementRectangle.Y - Startpoint.Y, uiElement.Width, uiElement.Height);
            }
        }

        private void CustomDialog_MouseLeave(object sender, MouseEventArgs e)
        {
            bool controlsnotover = !Button1.IsMouseOver && !Button1.IsMouseOver && !Button1.IsMouseOver && !DontAsk.IsMouseOver;
            var uiElement = (Popup)sender;
            if (e.LeftButton == MouseButtonState.Pressed && controlsnotover)
            {
                Point relative = e.GetPosition(null);
                uiElement.PlacementRectangle = new Rect(relative.X + uiElement.PlacementRectangle.X - Startpoint.X, relative.Y + uiElement.PlacementRectangle.Y - Startpoint.Y, uiElement.Width, uiElement.Height);
            }
            else
                Draggable = true;
        }

        private void CustomDialog_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Startpoint = e.GetPosition(null);
            if(!Button1.IsMouseOver && !Button2.IsMouseOver && !Button3.IsMouseOver && !DontAsk.IsMouseOver)
                DialogBorder.Opacity = 0.6;
        }

        private void CustomDialog_Opened(object sender, EventArgs e)
        {
            var uiElement = (Popup)sender;
            DialogBorder.Opacity = 1;
            DontAsk.IsChecked = !Properties.Settings.Default.AskOnConnect;
            uiElement.PlacementRectangle = new Rect((SystemParameters.FullPrimaryScreenWidth - uiElement.Width) / 2,
         (SystemParameters.FullPrimaryScreenHeight - uiElement.Height) / 2, uiElement.Width, uiElement.Height);
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ResultDialog = ((System.Windows.FrameworkElement)sender).Name;
            CustomDialog.IsOpen = false;
            if (this.WindowState == WindowState.Minimized && ResultDialog=="Button1")
                NotifyIcon_Click(null, null);
            Properties.Settings.Default.AskOnConnect = (bool)!DontAsk.IsChecked;
        }

        private void CustomDialog_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            DialogBorder.Opacity = 1;
        }

        private void Ok_Click(object sender, RoutedEventArgs e)
        {
            AboutDialog.IsOpen = false;
        }

        private void DontAsk_Unchecked(object sender, RoutedEventArgs e)
        {
            Button3.IsEnabled = (bool)!DontAsk.IsChecked;
        }

        private void ChartClear_Click(object sender, RoutedEventArgs e)
        {
            plt.Clear();
            nextDataIndex = 0;
            ChartSetup();
            wpfPlot1.Plot.YAxis2.LockLimits(true);
            wpfPlot1.Configuration.LockVerticalAxis = true;
            wpfPlot1.Refresh();
            SettingPopup.IsOpen = false;
        }
    }
}
