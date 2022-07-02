using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Diagnostics;
using HeartRateLE.Bluetooth.Events;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Input;
using System.Windows.Controls;
using ScottPlot;
using ScottPlot.Plottable;
using System.Windows.Documents;
using System.Windows.Controls.Primitives;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HeartRateLE.Bluetooth.HeartRateMonitor _heartRateMonitor;
        public string SelectedDeviceId { get; private set; }

        public string SelectedDeviceName { get; private set; }
        private string GattValue;
        private double doublevalue;
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
        private bool DevicePickerActive = false;
        private readonly Plot plt;
        private readonly Tooltip txt;
        public double[] gattData = new double[1_000_000];
        private double sampleRate = 1;
        private readonly SignalPlot signalPlot;
        private readonly MarkerPlot HighlightedPoint;
        private double LastHighlightedIndex = -1;
        private int nextDataIndex;
        private int ZoomScale = 30;
        private string OldACDC;
        private string OldSymbol;

        public MainWindow()
        {

            InitializeComponent();
            if (Properties.Settings.Default.Remember)
            {
                (Width, Height) = (Properties.Settings.Default.WindowSize.Width, Properties.Settings.Default.WindowSize.Height);
                (Top, Left) = (Properties.Settings.Default.WindowPosition.X, Properties.Settings.Default.WindowPosition.Y);
            }
            this.Topmost = Properties.Settings.Default.Ontop;
            OnTopON.Visibility = (Properties.Settings.Default.Ontop ? Visibility.Visible : Visibility.Hidden);
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");
            LV.SelectionChanged += LstOnSelectionChanced;
            plt = wpfPlot1.Plot;
            plt.Layout(10, 0, 0, 50, 0);
            plt.Style(ScottPlot.Style.Black);
            plt.Style(figureBackground: System.Drawing.Color.Transparent);
            plt.SetAxisLimits(0, 30);
            plt.XAxis.MinimumTickSpacing(5);
            plt.YAxis.MinimumTickSpacing(0.0001);
            plt.Margins(0.1, 0.2);

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




            wpfPlot1.Plot.YAxis2.LockLimits(true);
            wpfPlot1.Configuration.LockVerticalAxis = true;
            wpfPlot1.Refresh();


            dispatcherTimer.Tick += new EventHandler(dispatcherTimer_Tick);

            DataContext = this;
            _heartRateMonitor = new HeartRateLE.Bluetooth.HeartRateMonitor();
            _heartRateMonitor.LogData= Properties.Settings.Default.LogData;
            // we should always monitor the connection status
            _heartRateMonitor.ConnectionStatusChanged -= HrDeviceOnDeviceConnectionStatusChanged;
            _heartRateMonitor.ConnectionStatusChanged += HrDeviceOnDeviceConnectionStatusChanged;

            //// we can create value parser and listen for parsed values of given characteristic
            //HrParser.ConnectWithCharacteristic(HrDevice.HeartRate.HeartRateMeasurement);
            _heartRateMonitor.RateChanged -= HrParserOnValueChanged;
            _heartRateMonitor.RateChanged += HrParserOnValueChanged;
            if (Properties.Settings.Default.ConnectOn)
            {
                Tg_Btn.IsChecked = true;
                Tg_Btn.IsChecked = false;
                SelectedDeviceId = Properties.Settings.Default.DeviceID;
                SelectedDeviceName = Properties.Settings.Default.DeviceName;
                Reconnect();
            }
            else { Tg_Btn.IsChecked = true; }
            if (Properties.Settings.Default.ChartOn)
            {
                dispatcherTimer.Start();
                TopStackPanel.Visibility = Visibility.Visible;
                ChartON.Visibility = Visibility.Visible;
            }
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
            MyPopup.HorizontalOffset =-20;
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
            if (Properties.Settings.Default.Remember)
            {
                Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)ActualWidth, (int)ActualHeight);
                Properties.Settings.Default.WindowPosition = new System.Drawing.Point((int)Top, (int)Left);
                Properties.Settings.Default.Save();
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
                //textBox.Text = arg.MyGattCData;
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
                Battery.IsChecked = arg.MyGattCDataBattery;
                GattValue = arg.MyGattCData;
                if (arg.MyGattCDataHold)
                {
                    MyGattCData.Foreground = Brushes.Red; 
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
            if (Is_Connected.IsChecked == true)
            {
                signalPlot.IsVisible = true;
                try
                {

                    doublevalue = Convert.ToDouble(GattValue);
                }
                catch
                {
                    doublevalue = 0;
                }
                if (MyGattCDataSymbol.Text != OldSymbol || MyGattCDataACDC.Text != OldACDC)
                {
                    if (nextDataIndex > 1)
                    {
                        addVLine();
                    }
                    OldACDC = MyGattCDataACDC.Text;
                    OldSymbol = MyGattCDataSymbol.Text;
                }
                gattData[nextDataIndex] = doublevalue;
                signalPlot.MaxRenderIndex = nextDataIndex;
                if (!wpfPlot1.IsMouseOver)
                {
                    txt.IsVisible = false;
                    if (nextDataIndex > ZoomScale)
                    {
                        fitChart(nextDataIndex - ZoomScale, nextDataIndex + 1);
                    }
                    else 
                    {
                        fitChart(0, ZoomScale + 1);
                    }
                }
                wpfPlot1.Refresh();
                nextDataIndex += 1;
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
                    var device = await _heartRateMonitor.GetDeviceInfoAsync();
                    TxtStatus.Text = SelectedDeviceName + ": connected";
                    TxtBattery.Text = String.Format("battery level: {0}%", device.BatteryPercent);
                    MyGattCDataBluetooth.Visibility = Visibility.Visible;
                    Is_Connected.IsChecked = true;
                    dispatcherTimer.Start();
                    if (Properties.Settings.Default.ConnectOn)
                    {
                        Properties.Settings.Default.DeviceID = SelectedDeviceId;
                        Properties.Settings.Default.DeviceName = SelectedDeviceName;
                        Properties.Settings.Default.Save();
                    }
                }
                else
                {
                    TxtStatus.Text = SelectedDeviceName + ": disconnected";
                    TxtBattery.Text = "battery level: --";
                    dispatcherTimer.Stop();
                    Is_Connected.IsChecked = false;
                    if (Properties.Settings.Default.Reconnect)
                    {
                        Reconnect();
                    }
                }
            });
        }

        private async void Reconnect()
        {
            if (_heartRateMonitor.IsConnected)
            {
                await _heartRateMonitor.DisconnectAsync();
            }

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
            if (TopStackPanel.Visibility == Visibility.Visible)
            {
                dispatcherTimer.Stop();

                TopStackPanel.Visibility = Visibility.Collapsed;
                ChartON.Visibility = Visibility.Hidden;
                Height = Display.ActualHeight;
            }
            else
            {
                dispatcherTimer.Start();
                TopStackPanel.Visibility = Visibility.Visible;
                ChartON.Visibility = Visibility.Visible;
                this.Height = 160 + Display.ActualHeight + 20;
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

        private void SettingBtn_Click(object sender, RoutedEventArgs e)
        {
            var Settings = new Settings();
            var result = Settings.ShowDialog();
            if (Properties.Settings.Default.Remember)
            {
                Properties.Settings.Default.WindowSize = new System.Drawing.Size((int)ActualWidth, (int)ActualHeight);
                Properties.Settings.Default.WindowPosition = new System.Drawing.Point((int)Top, (int)Left);
                Properties.Settings.Default.Save();
            }
            if (Properties.Settings.Default.ConnectOn)
            {
                Properties.Settings.Default.DeviceID = SelectedDeviceId;
                Properties.Settings.Default.DeviceName = SelectedDeviceName;
                Properties.Settings.Default.Save();
            }
            _heartRateMonitor.LogData = Properties.Settings.Default.LogData;
        }

        private void wpfPlot1_MouseMove(object sender, MouseEventArgs e)
        {
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

                LastHighlightedIndex = pointIndex;
                txt.Label = $" Value: {pointY} \n at {pointX} ";
                txt.X = pointX;
                txt.Y = pointY;
                txt.IsVisible = true;

                wpfPlot1.Render();
            }

            // update the GUI to describe the highlighted point
            double mouseX = e.GetPosition(this).X;
            double mouseY = e.GetPosition(this).Y;
            //label1.Content = $"Closest point to ({mouseX:N2}, {mouseY:N2}) " +
            //   $"is index {pointIndex} ({pointX:N2}, {pointY:N2})";
        }

        private void wpfPlot1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // determine the axis where we are now
            var axes = plt.GetAxisLimits();
            int xLow = (int)axes.XMin;
            int xHigh = (int)axes.XMax;
            ZoomScale = xHigh - xLow;
            //set the Y axis limits to the high and low of the range
            fitChart(xLow, xHigh);
            wpfPlot1.Refresh();
        }

        private void fitChart(int xLow, int xHigh)
        {

            if (ZoomScale < 0)
            {

                plt.AxisAutoX();
                plt.AxisAutoY(null, 0);
            }
            else
            {
                if (xHigh > nextDataIndex) { xHigh = nextDataIndex; }
                if (xLow < 0 || nextDataIndex == 0) { xLow = 0; }
                double yLow = gattData.Skip(xLow).Take(xHigh - xLow + 1).Min();
                double yHigh = gattData.Skip(xLow).Take(xHigh - xLow + 1).Max();

                //set the Y axis limits to the high and low of the range
                //plt.SetAxisLimitsX(nextDataIndex - 30, nextDataIndex - 0.5);
                plt.SetAxisLimits(xLow - ZoomScale* 0.02, xLow + ZoomScale + ZoomScale * 0.02, yLow - (yHigh - yLow) * plt.Margins().y, yHigh + (yHigh - yLow) * plt.Margins().y);
            }
        }

        private void addVLine()
        {
            var vline = plt.AddVerticalLine(nextDataIndex-1);
            vline.YAxisIndex = 1;
            vline.LineWidth = 2;
            var axes = plt.GetAxisLimits(0,1);

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
        private void wpfPlot1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var axes = plt.GetAxisLimits();
            int xLow = (int)axes.XMin;
            int xHigh = (int)axes.XMax;
            ZoomScale = xHigh - xLow;
            //set the Y axis limits to the high and low of the range
            fitChart(xLow, xHigh);
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
            } else { SettingPopup.IsOpen = false; }

        }

        private void Chart_Reset_Click(object sender, RoutedEventArgs e)
        {
            ZoomScale = 30;
            fitChart(nextDataIndex - ZoomScale, nextDataIndex + 1);
            wpfPlot1.Refresh();
            SettingPopup.IsOpen = false;
        }

        private void Chart_Fit_Click(object sender, RoutedEventArgs e)
        {
            ZoomScale = -1;
            fitChart(0, 0);
            wpfPlot1.Refresh();
            SettingPopup.IsOpen = false;
        }

        private void Chart_Import_Click(object sender, RoutedEventArgs e)
        {

            SettingPopup.IsOpen = false;
        }

        private void Chart_Export_Click(object sender, RoutedEventArgs e)
        {

            SettingPopup.IsOpen = false;
        }
    }
}
