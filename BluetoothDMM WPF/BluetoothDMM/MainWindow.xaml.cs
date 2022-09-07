using HeartRateLE.Bluetooth;
using HeartRateLE.Bluetooth.Events;
using HeartRateLE.Bluetooth.Schema;
using Microsoft.Win32;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Protocol;
using Octokit;
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
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using WPFLocalizeExtension.Engine;
using WPFLocalizeExtension.Providers;
using Application = System.Windows.Application;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon m_notifyIcon;
        private HeartRateLE.Bluetooth.HeartRateMonitor _heartRateMonitor;
        public string SelectedDeviceId { get; private set; }
        public string SelectedDeviceName { get; private set; }
        public static Dictionary<string, string> DeviceListC { get; set; }
        public static Dictionary<string, string> iDeviceListC;
        private string GattValue;
        private double doublevalue;
        private string ValueF;
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
            if (Properties.Settings.Default.CheckUpdates)
            {
                Task.Run(async () =>
                {
                    try
                    {
                        GitHubClient client = new GitHubClient(new ProductHeaderValue("BluetoothDMMForWindows"));
                        
                        IReadOnlyList<Release> releases = await client.Repository.Release.GetAll("webspiderteam", "Bluetooth-DMM-For-Windows");
                        Updates.Releases = releases;
                        var _prefix = "WPFRelease";
                        //Setup the versions
                        var tagname = releases[0].TagName;
                        if (_prefix != "" && tagname.StartsWith(_prefix))
                            tagname = tagname.Replace(_prefix, "");
                        Version latestGitHubVersion = new Version(tagname);
                        Version localVersion = new Version(System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString().Substring(0, 4)); //Local version. 
                        //Version localVersion = new Version("1.18.0");
                        //Compare the Versions
                        int versionComparison = localVersion.CompareTo(latestGitHubVersion);
                        if (versionComparison < 0)
                            await RunOnUiThread(() =>
                            {
                                TxtUpdate.IsEnabled = true;
                            });
                    }
                    catch (Exception ex)
                    {
                        d("update" + ex.Message);
                    }
                });
            }
            if (Properties.Settings.Default.Lang == "")
                LocalizeDictionary.Instance.Culture = CultureInfo.CurrentCulture;
            else
            {
                LocalizeDictionary.Instance.Culture = new CultureInfo(Properties.Settings.Default.Lang);
                System.Threading.Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Lang);
            }
                
            var x = WPFLocalizeExtension.Engine.LocalizeDictionary.Instance.MergedAvailableCultures;
            (LocalizeDictionary.Instance.DefaultProvider as ResxLocalizationProvider).SearchCultures =
                    new List<CultureInfo>()
                    {
                                    CultureInfo.GetCultureInfo("ar"),
                                    CultureInfo.GetCultureInfo("de"),
                                    CultureInfo.GetCultureInfo("el"),
                                    CultureInfo.GetCultureInfo("es"),
                                    CultureInfo.GetCultureInfo("fr"),
                                    CultureInfo.GetCultureInfo("it"),
                                    CultureInfo.GetCultureInfo("ja"),
                                    CultureInfo.GetCultureInfo("nl"),
                                    CultureInfo.GetCultureInfo("en"),
                                    CultureInfo.GetCultureInfo("pl"),
                                    CultureInfo.GetCultureInfo("ru"),
                                    CultureInfo.GetCultureInfo("tr"),
                                    CultureInfo.GetCultureInfo("zh-CN"),
                                    CultureInfo.GetCultureInfo("zh-TW")
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
            LVView.SelectionChanged += LstOnSelectionChanced;
            //Chart Startup initialization
            plt = wpfPlot1.Plot;
            plt.Layout(10, 0, 0, 50, 0);
            plt.Style(ScottPlot.Style.Black);
            plt.Style(figureBackground: System.Drawing.Color.Transparent);
            plt.SetAxisLimits(0, 30);
            //plt.XAxis.MinimumTickSpacing(5);
            plt.YAxis.MinimumTickSpacing(0.0001);
            plt.Margins(0.1, 0.2);
            ChartSetup();
            
            wpfPlot1.Plot.YAxis2.LockLimits(true);
            wpfPlot1.Configuration.LockVerticalAxis = true;
            wpfPlot1.Refresh();
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
                DoADisplay();
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
                Visible = true,
                ContextMenu = new System.Windows.Forms.ContextMenu() //TrayContextMenu;
            };
            m_notifyIcon.Click += new EventHandler(NotifyIcon_Click);
            Get_MQTT_Settings();
            if (MQTTSetup.MQTTEnabled)
            {
                Task task = Connect();
                Get_DataList();
            }
        }

        async Task Connect()
        {
            //var server = "test.mosquitto.org";
            //server = "broker.hivemq.com";-
            var tlsoption = new MqttClientOptionsBuilderTlsParameters();
            tlsoption.SslProtocol = Properties.MQTT.Default.isEncrypted ? System.Security.Authentication.SslProtocols.Ssl3 : System.Security.Authentication.SslProtocols.None;
            var mqttFactory = new MqttFactory();
            client = mqttFactory.CreateMqttClient();
            var t_options = new MqttClientOptionsBuilder()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(MQTTSetup.BrokerAddress, MQTTSetup.BrokerPort)
                .WithTls(tlsoption)
                .WithCleanSession();
                //.Build();
            if (MQTTSetup.UseLogin)
                t_options = t_options.WithCredentials(MQTTSetup.Username, MQTTSetup.Password);
            options = t_options.Build();
            client.ConnectedAsync += Client_ConnectedAsync;
            client.DisconnectedAsync += Client_DisconnectedAsync;
            //client.ApplicationMessageReceivedAsync += Client_ApplicationMessageReceivedAsync;
            try
            {
                await client.ConnectAsync(options, CancellationToken.None);
            }
            catch (Exception ex)
            {
                MessageBox.Show("MQTT Connection error;\n" + ex.Message +"\nPlease check your MQTT settings!");
                d("Connection error with " + ex.Message);
            }
        }

        private Task Client_DisconnectedAsync(MqttClientDisconnectedEventArgs arg)
        {
            Task.Delay(250);
            if (ReconnectMqtt)
                client.ConnectAsync(options);
            d("Mqtt disconnected");
            //dispatcherTimer.Start();
            return Task.CompletedTask;
        }
        private Task Client_ConnectedAsync(MqttClientConnectedEventArgs arg)
        {
            ReconnectMqtt = true;
            d("Mqtt Connected");
            if (Connected == 1)
            {
                Task task = Publish($"{MQTTSetup.ClientId}/{MQTTSetup.Topic}", $"{{ \"Status\": \"Connected\", " +
                    $"\"MAC\": \"{SelectedDeviceId.Substring(SelectedDeviceId.Length - 17, 17).ToUpper().Replace(":", "_")}\", " +
                    $"\"UseMAC\": \"{MQTTSetup.addMac}\"}}");
            }
            //Subscribe(topic); For posterity
            return Task.CompletedTask;
        }

        void Subscribe(string stopic)
        {
            var topicFilter = new MqttTopicFilterBuilder()
                    .WithTopic(stopic)
                    .Build();
            client.SubscribeAsync(topicFilter);
        }
        async Task Publish(string topic,string msg)
        {
            var message = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(msg)
                .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            if (client.IsConnected)
            {
                await client.PublishAsync(message);
            }
        }

        private void Get_DataList()
        {
            SelectedDatas = new Dictionary<string, string>(Properties.MQTT.Default.SelectedDataList.Count);
            foreach (var item in Properties.MQTT.Default.SelectedDataList)
            {
                string[] row = item.Split(new string[] { "\n" }, StringSplitOptions.RemoveEmptyEntries);
                SelectedDatas.Add(row[0], row[1]);

            }
        }

        private void Get_MQTT_Settings()
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

        static string CustomTickFormatter(double position)
        {
            TimeSpan result = TimeSpan.FromSeconds(position);
            if (position < 3600)
                return result.ToString("m':'ss");
            else if (position<3600*24)
                return result.ToString("h':'m':'ss");
            else
                return result.ToString("d'-'h':'m':'ss");
        }

        private void ChartSetup()
        {
            gattData = new double[1_000_000];
            signalPlot = plt.AddSignal(gattData, sampleRate, color: System.Drawing.Color.White);
            plt.XAxis.TickLabelFormat(CustomTickFormatter);
            signalPlot.YAxisIndex = 0;
            signalPlot.LineWidth = 1.5;
            signalPlot.MarkerSize = 2;
            
            signalPlot.IsVisible = false;
            HighlightedPoint = plt.AddPoint(0, 0);
            HighlightedPoint.Color = System.Drawing.Color.Yellow;
            HighlightedPoint.MarkerSize = 10;
            HighlightedPoint.MarkerShape = ScottPlot.MarkerShape.openCircle;
            HighlightedPoint.IsVisible = false;

            txt = plt.AddTooltip("No Data", 0, 0);
            txt.Font.Color = System.Drawing.Color.White;
            txt.FillColor = System.Drawing.Color.FromArgb(190, 107, 126, 243);
            txt.BorderWidth = 1;
            txt.BorderColor = System.Drawing.Color.White;
            txt.Font.Bold = true;
            txt.Font.Size = 12;
            txt.IsVisible = false;
        }

        private WindowState m_storedWindowState = WindowState.Normal;
        private string ResultDialog;
        private Dictionary<string, string> SelectedDatas;
        private bool trick;
        private UIElement mTitlebar;
        private bool wStateChanged;
        IMqttClient client;
        MqttClientOptions options;
        private bool ReconnectMqtt;

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
            wStateChanged = true;
        }

        private void OnIsVisibleChanged(object sender, DependencyPropertyChangedEventArgs args) => CheckTrayIcon();

        private void NotifyIcon_Click(object sender, EventArgs e)
        {
            if (e is System.Windows.Forms.MouseEventArgs mouseArgs)
            {
                if (mouseArgs.Button == System.Windows.Forms.MouseButtons.Left)
                    Show_Click(null, null);
                else if (mouseArgs.Button == System.Windows.Forms.MouseButtons.Right)
                {
                    ContextMenu menu = (ContextMenu)aDisplay.FindResource("cMenu");//This sentence is to find resources (you can write the menu style there)
                    menu.IsOpen = true;
                }
            }
        }
        private void Show_Click(object sender, RoutedEventArgs e)
        {
            Show();
            WindowState = m_storedWindowState;
        }

        private void Quit_Click(object sender, RoutedEventArgs e) => Close();
        private void CheckTrayIcon() => ShowTrayIcon(!IsVisible);

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
            if (client != null && client.IsConnected)
            {
                ReconnectMqtt = false;
                await client.DisconnectAsync();
            }
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
                try { 
                    doublevalue = Convert.ToDouble(GattValue,CultureInfo.InvariantCulture);
                    ValueF = Convert.ToString(doublevalue, CultureInfo.InvariantCulture);
                }
                catch { ValueF = "null"; }//doublevalue = 0;}
                if (GattValue != null)
                {
                    var result = Convert.ToString(Math.Abs(doublevalue), CultureInfo.InvariantCulture).Split(new string[] { ".", " " }, StringSplitOptions.RemoveEmptyEntries);
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
                if (client != null && client.IsConnected)
                {
                    string Mac = "";
                    if (MQTTSetup.addMac)
                        Mac = "/" + SelectedDeviceId.Substring(SelectedDeviceId.Length - 17, 17).ToUpper().Replace(":", "_");
                    Task task = Publish($"{MQTTSetup.ClientId}{Mac}/{MQTTSetup.Topic}", Create_MQTTString());
                }
            }
            if (GotFirstData)
                TxtStatus.IsEnabled = false;
        }

        private string Create_MQTTString()
        {
            string[] Strings = new string[SelectedDatas.Count];
            string ValueS = Properties.MQTT.Default.UseComa ? MyGattCData.Text.Replace('.', ',') : MyGattCData.Text;
            if (Properties.MQTT.Default.CleanWhitespace)
                ValueS = ValueS.Trim();
            var oValue = doublevalue;
            var BaseSym = MyGattCDataSymbol.Text;
            if (MyGattCDataSymbol.Text.Length > 1 && !(MyGattCDataSymbol.Text == "Hz" || MyGattCDataSymbol.Text == "°C" || MyGattCDataSymbol.Text == "°F"))
            {
                var pre = MyGattCDataSymbol.Text.Substring(0, 1);
                switch (pre)
                {
                    case "n": oValue /= 1000000000; break;
                    case "µ": oValue /= 1000000; break;
                    case "m": oValue /= 1000; break;
                    case "k": oValue *= 1000; break;
                    case "M": oValue *= 1000000; break;
                }
                BaseSym = BaseSym.Substring(1);
            }
            foreach (var (item,index) in SelectedDatas.Select((n, i) => (n, i)))
            {
                switch (item.Key)
                {
                    case "Time": Strings[index]=item.Value + Convert.ToString(DateTime.Now, CultureInfo.InvariantCulture) + "\"";break;
                    case "Device": Strings[index]=item.Value + SelectedDeviceName + "\""; break;
                    case "Value(string)": Strings[index]=item.Value + ValueS + "\""; break;
                    case "Value(float)": Strings[index]=item.Value + Convert.ToString(ValueF, CultureInfo.InvariantCulture); break;
                    case "BaseValue(double)": Strings[index] = item.Value + (ValueF == "null" ? "null" :Convert.ToString(oValue, CultureInfo.InvariantCulture)); break;
                    case "BaseSym": Strings[index] = item.Value + BaseSym + "\""; break;
                    case "Range": Strings[index]=item.Value + MyGattCDataSymbol.Text + "\""; break;
                    case "Current": Strings[index]=item.Value + MyGattCDataACDC.Text + "\""; break;
                    case "AutoRange(Boolean)": Strings[index]=item.Value + AutoRange.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "TrueRMS(Boolean)": Strings[index]=item.Value + True_RMS.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "Max(Boolean)": Strings[index]=item.Value + MyGattCDataMax.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "Min(Boolean)": Strings[index]=item.Value + MyGattCDataMin.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "Peek(Boolean)": Strings[index]=item.Value + MyGattCDataPeek.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "InRush(Boolean)": Strings[index]=item.Value + InRush.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "Buzz(Boolean)": Strings[index]=item.Value + MyGattCDataContinuity.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "Diode(Boolean)": Strings[index]=item.Value + MyGattCDataDiode.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "Battery(Boolean)": Strings[index]=item.Value + MyGattCDataBattery.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "HV(Boolean)": Strings[index]=item.Value + MyGattCDataHV.Visibility.Equals(Visibility.Visible) + "\""; break;
                    case "Rel(Boolean)": Strings[index]=item.Value + MyGattCDataRel + "\""; break;
                    case "All Booleans":
                        string allString = "[ " + (AutoRange.Visibility.Equals(Visibility.Visible) ? "\"AUTORANGE\"," : string.Empty) +
                            (True_RMS.Visibility.Equals(Visibility.Visible) ? "\"TRUERMS\"," : string.Empty) +
                            (MyGattCDataMax.Visibility.Equals(Visibility.Visible) ? "\"MAX\"," : string.Empty) +
                            (MyGattCDataMin.Visibility.Equals(Visibility.Visible) ? "\"MIN\"," : string.Empty) +
                            (MyGattCDataPeek.Visibility.Equals(Visibility.Visible) ? "\"PEEK\"," : string.Empty) +
                            (InRush.Visibility.Equals(Visibility.Visible) ? "\"INRUSH\"," : string.Empty) +
                            (MyGattCDataContinuity.Visibility.Equals(Visibility.Visible) ? "\"BUZZ\"," : string.Empty) +
                            (MyGattCDataDiode.Visibility.Equals(Visibility.Visible) ? "\"DIODE\"," : string.Empty) +
                            ((bool)Battery.IsChecked ? "\"BATT\"," : string.Empty) +
                            (MyGattCDataHV.Visibility.Equals(Visibility.Visible) ? "\"HV\"," : string.Empty) +
                            (MyGattCDataRel.Visibility.Equals(Visibility.Visible) ? "\"REL\"," : string.Empty);
                        Strings[index]= item.Value + allString.Substring(0,allString.Length-1) + " ]"; 
                        break;
                }
            }
            return "{ " + string.Join(", ", Strings) + " }";
        }

        private async void HrDeviceOnDeviceConnectionStatusChanged(object sender, ConnectionStatusChangedEventArgs args)
        {
            d("Current connection status is: " + args.IsConnected);
            
            await RunOnUiThread(() =>
            {
                if (TxtStatus.IsEnabled)
                    TxtStatus.IsEnabled = false;
                TxtStatus.IsEnabled = true;
                bool connected = args.IsConnected;
                if (connected)
                {
                    //var device = await _heartRateMonitor.GetDeviceInfoAsync();
                    textDevicename.Text = SelectedDeviceName + " : ";
                    new WPFLocalizeExtension.Extensions.LocExtension("Connected").SetBinding(textStatus, Run.TextProperty);
                    //TxtBattery.Text = String.Format("battery level: {0}%", device.BatteryPercent);
                    MyGattCDataBluetooth.Visibility = Visibility.Visible;
                    dispatcherTimer.Start();
                    Connected = 1;
                    Is_Connected.IsChecked = true;
                    TxtUpdate.IsEnabled = false;
                    if (client != null && client.IsConnected)
                    {
                        Task task = Publish($"{MQTTSetup.ClientId}/{MQTTSetup.Topic}",
                                            $"{{\"Status\": \"Connected\", " +
                                            $"\"MAC\": \"{SelectedDeviceId.Substring(SelectedDeviceId.Length - 17, 17).ToUpper().Replace(":", "_")}\", " +
                                            $"\"UseMAC\": \"{MQTTSetup.addMac}\" }}");
                    }
                }
                else
                {
                    new WPFLocalizeExtension.Extensions.LocExtension("Disconnected").SetBinding(textStatus, Run.TextProperty);
                    TxtStatus.IsEnabled=true;
                    TxtBattery.Text = "battery level: --";
                    dispatcherTimer.Stop();
                    Is_Connected.IsChecked = false;
                    if (Properties.Settings.Default.Reconnect)
                        Connected = 0;
                    if (client != null && client.IsConnected)
                    {
                        Task task = Publish($"{MQTTSetup.ClientId}/{MQTTSetup.Topic}",
                            $"{{\"Status\": \"Disconnected\", " +
                            $"\"MAC\": \"{SelectedDeviceId.Substring(SelectedDeviceId.Length - 17, 17).ToUpper().Replace(":", "_")}\"}}");
                    }
                }
            });
        }

        private Task SearchDevices()
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
                        if (DeviceListC.ContainsKey(args.Device.Id)  && args.Device.Properties.ContainsKey("System.Devices.Aep.Bluetooth.Le.IsConnectable") && (bool)args.Device.Properties["System.Devices.Aep.Bluetooth.Le.IsConnectable"])
                        {
                            SelectedDeviceId = args.Device.Id;
                            SelectedDeviceName = DeviceListC[args.Device.Id];
                            //Debug.WriteLine($"Added {args.Device.Name} Connected : {args.Device.IsConnected}" + "IsConnectable");
                            Connected = 2;
                            string result;
                            if (Properties.Settings.Default.AskOnConnect)
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
                            if (Properties.Settings.Default.AskOnConnect)
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
            return Task.CompletedTask;
        }

        private async Task<string> ShowCustomDialog(string DeviceName)
        {
            ResultDialog = "0";
            await RunOnUiThread(() =>
            {
                CustomDialogHeader.Text = DeviceName;
                CustomDialog.IsOpen = true;
            });

            while (ResultDialog == "0")
            {
                await Task.Delay(25);
                continue;
            }
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
            if (txt) { return Visibility.Visible; } else { return Visibility.Hidden; }
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
            var SItem = ((System.Windows.FrameworkElement)((System.Windows.Controls.Primitives.Selector)sender).SelectedItem);
            var LVs = sender as ListView;
            if (LVs.SelectedIndex != -1)
            {
                if (SItem.Name == "ConnectTo")
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
                else if (SItem.Name == "Chart")
                    DoOnChart();
                else if (SItem.Name == "OnTop")
                    DoOnTop();
                else if (SItem.Name == "ADisplay")
                    DoADisplay();
                else if (SItem.Name == "Settings")
                {
                    if (trick)
                    {
                        Properties.Settings.Default.SVGIcon = !Properties.Settings.Default.SVGIcon;
                        Properties.Settings.Default.Save();
                    }
                    trick = false;
                    SettingBtn_Click(null, null);
                }
                else if (SItem.Name == "About")
                    AboutDialog.IsOpen = true;
                LVs.SelectedIndex = -1;
                Tg_Btn.IsChecked = false;
            }
        }
        private void DoOnChart()
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
        
        private void DoOnTop()
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
        
        private void DoADisplay()
        {
            if (aDisplay.Visibility==Visibility.Visible)
            {
                aDisplay.Visibility = Visibility.Collapsed;
                ADisplayON.Visibility = Visibility.Hidden;
            }
            else
            {
                aDisplay.Visibility = Visibility.Visible;
                ADisplayON.Visibility = Visibility.Visible;
            }
        }
        
        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {
            var Settings = new SettingsNew(DeviceListC)//Settings(DeviceListC)
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
                    LocalizeDictionary.Instance.Culture = new CultureInfo(Properties.Settings.Default.Lang);
                    Thread.CurrentThread.CurrentUICulture = new CultureInfo(Properties.Settings.Default.Lang);
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
                    DoADisplay();
                }
                if (Properties.Settings.Default.ChartOn && TopStackPanel.Visibility != Visibility.Visible)
                    DoOnChart();
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
                        textStatus.Text = SelectedDeviceName + " : ";
                    }
                }
                if (Properties.MQTT.Default.MQTTEnabled)
                {
                    Get_MQTT_Settings();
                    Get_DataList();
                    if (client != null && client.IsConnected)
                    {
                        ReconnectMqtt = false;
                        client.DisconnectAsync();
                    }
                    Task.Run(() =>
                    {
                        try
                        {
                            Task task = Connect();
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
                            txt.Label = $" {st[0]} {pointY}{st[1]} \n at {CustomTickFormatter(pointX)} "; 
                        }
                        LastX = (int)((ScottPlot.Plottable.Text)plotT).X;
                    }
                }
                if (LastX < pointIndex)
                    txt.Label = $" {MyGattCDataACDC.Text} {pointY}{MyGattCDataSymbol.Text} \n at {CustomTickFormatter(pointX)} ";

                LastHighlightedIndex = pointIndex;

                txt.X = pointX;
                txt.Y = pointY;
                txt.IsVisible = true;

                wpfPlot1.Render();
            }
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

        private void MyPopup_Closed(object sender, EventArgs e) => Tg_Btn.IsChecked = false;

        private void ChartSettings_Click(object sender, RoutedEventArgs e)
        {

                SettingPopup.PlacementTarget = sender as UIElement;
                SettingPopup.IsOpen = !SettingPopup.IsOpen;
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
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            if (Properties.Settings.Default.CSVFirst)
                saveFileDialog.Filter = "CSV file (*.csv)|*.csv|DMM Chart Data file (*.dcd)|*.dcd";
            else
                saveFileDialog.Filter = "DMM Chart Data file (*.dcd)|*.dcd|CSV file (*.csv)|*.csv";

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
                    string acdc;
                    string sym;
                    if (MyGattCDataACDC.Text == "" && MyGattCDataSymbol.Text == "")
                    {
                        sym = OldSymbol;
                        acdc = OldACDC + " * \n";
                    }
                    else
                    {
                        acdc = MyGattCDataACDC.Text == "" ? string.Empty : ("  " + MyGattCDataACDC.Text + " * \n");
                        sym = MyGattCDataSymbol.Text;
                    }
                    byte sbe = SymbolToByte(acdc + "  " + sym + " *  ");
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
                        createText[0] = "Time,CurrentType,sValue,Symbol,BaseVal,BaseSym";
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
                        string acdc;
                        string sym;
                        if(MyGattCDataACDC.Text=="" && MyGattCDataSymbol.Text=="")
                        {
                            sym = OldSymbol;
                            acdc = OldACDC + " * \n";
                        }
                        else
                        {
                            acdc = MyGattCDataACDC.Text == "" ? string.Empty : ("  " + MyGattCDataACDC.Text + " * \n");
                            sym = MyGattCDataSymbol.Text;
                        }
                        string sbe = acdc + "  " + sym + " *  ";
                        int pbe = (int)plt.GetDataLimits().XMax;
                        keyValuePairs.Add(pbe, sbe);
                        int pairs_i = 0;
                        foreach (var (value, index) in ChartData.Select((v, i) => (v, i)))
                        {
                            var oValue = value;
                            string sValue = keyValuePairs.ElementAt(pairs_i).Value.Replace('*', ' ');
                            var resultval = sValue.Split(new string[] { "\n", " " }, StringSplitOptions.RemoveEmptyEntries);
                            string s1;
                            string s2;
                            string s3;
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
                            s3 = s2;

                            if (s2.Length > 1 && !(s2 == "Hz" || s2 == "°C" || s2 == "°F"))
                            {
                                var pre = s2.Substring(0, 1);
                                switch (pre)
                                {
                                    case "n": oValue /= 1000000000; break;
                                    case "µ": oValue /= 1000000; break;
                                    case "m": oValue /= 1000; break;
                                    case "k": oValue *= 1000; break;
                                    case "M": oValue *= 1000000; break;
                                }
                                s3 = s3.Substring(1);
                            }
                            if (index == keyValuePairs.ElementAt(pairs_i).Key)
                                pairs_i++;
                            createText[index + 1] = $"{index},{s1},{Convert.ToString(value, CultureInfo.InvariantCulture)},{s2},{Convert.ToString(oValue, CultureInfo.InvariantCulture)},{s3}";
                        }
                        File.WriteAllLines(saveFileDialog.FileName, createText, System.Text.Encoding.Default);
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
            if (Draggable && e.LeftButton==MouseButtonState.Pressed)
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
                mTitlebar = REghZy.Themes.Controls.Titlebarr;
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
                onLoad = false;
            }
        }

        private void WpfPlot1_MouseLeave(object sender, MouseEventArgs e) => Draggable = true;

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
                Show_Click(null, null);
            Properties.Settings.Default.AskOnConnect = (bool)!DontAsk.IsChecked;
            Properties.Settings.Default.Save();
        }

        private void Ok_Click(object sender, RoutedEventArgs e) => AboutDialog.IsOpen = false;

        private void DontAsk_Unchecked(object sender, RoutedEventArgs e) => Button3.IsEnabled = (bool)!DontAsk.IsChecked;

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

        private void ViewMenu_MouseMove(object sender, MouseEventArgs e)
        {
            MyPopupViewSub.IsOpen = true;
            MyPopup.StaysOpen = true;
        }

        private void MyPopupViewSub_MouseLeave(object sender, MouseEventArgs e)
        {
            if (!(ViewMenu.IsMouseOver || MyPopupViewSub.IsMouseOver))
            {
                MyPopupViewSub.IsOpen = false;
                MyPopup.StaysOpen = false;
            }
        }

        private void Settings_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl)
                trick = (e.KeyStates==KeyStates.Down);
        }

        private void Display_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.RoutedEvent.Name == "MouseEnter")
            {
                Draggable = true;
                mTitlebar.Visibility = Visibility.Visible;
                Tg_Btn.Visibility = Visibility.Visible;
                TxtStatus.Visibility = Visibility.Visible;
                TxtStatus.IsEnabled = true;
            }
            else
            {
                mTitlebar.Visibility = Visibility.Hidden;
                Tg_Btn.Visibility = Visibility.Hidden;
                TxtStatus.IsEnabled = false;
                //TxtStatus.Opacity = 0.0;
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            
            if (!wStateChanged && Height < Display.ActualHeight)
                Height = Display.ActualHeight;
            wStateChanged = false;
        }

        private void PopupDialog_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var popup = sender as Popup;
            var enumm = ((Canvas)popup.Child).Children.GetEnumerator();
            enumm.MoveNext();
            while (enumm.Current.ToString() != "System.Windows.Controls.Primitives.Thumb")
                enumm.MoveNext();
            var tmb = (enumm.Current) as Thumb;
            ((Canvas)popup.Child).Opacity = 0.6;
            tmb.RaiseEvent(e);
        }

        private void Thumb_DragDelta(object sender, DragDeltaEventArgs e)
        {
            var tmb = sender as Thumb;
            var popup = ((System.Windows.FrameworkElement)tmb.Parent).Parent as Popup;
            popup.HorizontalOffset += e.HorizontalChange;
            popup.VerticalOffset += e.VerticalChange;
        }

        private void PopupDialog_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            var popup = sender as Popup;
            ((Canvas)popup.Child).Opacity = 1;
        }

        private void Window_MouseMove(object sender, MouseEventArgs e)
        {
            if(mTitlebar.Visibility != Visibility.Visible)
            {
                Draggable = true;
                mTitlebar.Visibility = Visibility.Visible;
                Tg_Btn.Visibility = Visibility.Visible;
                TxtStatus.Visibility = Visibility.Visible;
                TxtStatus.IsEnabled = true;
            }
        }
    }
}