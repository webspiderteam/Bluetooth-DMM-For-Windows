using ScottPlot;
using ScottPlot.Plottable;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class DataViewer : Window
    {
        public string SelectedDeviceId { get; private set; }

        public string SelectedDeviceName { get; private set; }
        private System.Windows.Threading.DispatcherTimer dispatcherTimer = new System.Windows.Threading.DispatcherTimer();
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

        public DataViewer(double[] ImportedData, byte[] vLinesS, int[] VLinesX)
        {

            InitializeComponent();

            this.Topmost = Properties.Settings.Default.Ontop;
            System.Threading.Thread.CurrentThread.CurrentCulture = new CultureInfo("en-US");

            plt = wpfPlot1.Plot;
            plt.Layout(10, 0, 0, 50, 0);
            plt.Style(ScottPlot.Style.Black);
            plt.Style(figureBackground: System.Drawing.Color.Transparent);
            plt.SetAxisLimits(0, 30);
            plt.XAxis.MinimumTickSpacing(5);
            plt.YAxis.MinimumTickSpacing(0.0001);
            plt.Margins(0.1, 0.2);
            gattData = ImportedData;
            nextDataIndex = ImportedData.Length;
            signalPlot = plt.AddSignal(gattData, sampleRate, color: System.Drawing.Color.White);
            signalPlot.YAxisIndex = 0;
            signalPlot.LineWidth = 2;
            signalPlot.MarkerSize = 3;

            //signalPlot.IsVisible = false;
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
            for (int sx = 0; sx < vLinesS.Length; sx++)
            {
                var vline = plt.AddVerticalLine(VLinesX[sx]);
                vline.YAxisIndex = 1;
                vline.LineWidth = 2;
                vline.Color = System.Drawing.Color.FromArgb(120, System.Drawing.Color.Yellow);
                var axes = plt.GetAxisLimits(0, 1);

                int yHigh = (int)axes.YMax;
                var txtvline1 = plt.AddText(ByteToSymbol(vLinesS[sx]), VLinesX[sx], yHigh);
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
            wpfPlot1.Plot.YAxis2.LockLimits(true);
            wpfPlot1.Configuration.LockVerticalAxis = true;
            fitChart(0, 30);
            wpfPlot1.Refresh();


            DataContext = this;

        }

        private static string ByteToSymbol(byte Value)
        {

            string s1;
            string s2;
            int ACDC = Value % 10;
            int Symbol = Value - ACDC;

            switch (ACDC)
            {
                case 3: s1 = "Δ"; break;
                case 2: s1 = "AC"; break;
                case 1: s1 = "DC"; break;
                default: s1 = ""; break;
            }

            switch (Symbol)
            {
                case 10: s2 = "µA"; break;
                case 20: s2 = "mA"; break;
                case 30: s2 = "A"; break;
                case 40: s2 = "mV"; break;
                case 50: s2 = "V"; break;
                case 60: s2 = "nF"; break;
                case 70: s2 = "µF"; break;
                case 80: s2 = "F"; break;
                case 90: s2 = "mF"; break;
                case 100: s2 = "MΩ"; break;
                case 110: s2 = "kΩ"; break;
                case 120: s2 = "Ω"; break;
                case 130: s2 = "KHz"; break;
                case 140: s2 = "MHz"; break;
                case 150: s2 = "Hz"; break;
                case 160: s2 = "°C"; break;
                case 170: s2 = "°F"; break;
                case 180: s2 = "%"; break;
                default: s2 = "NCV"; break;
            }
            return (s1 == "" ? string.Empty : "  " + s1 + " * \n") + "  " + s2 + " *  ";
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName = null)
        {
            if (PropertyChanged != null) PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
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
                            txt.Label = $" {st[0]} {pointY} {st[1]} \n at {pointX} ";
                        }
                        LastX = (int)((ScottPlot.Plottable.Text)plotT).X;
                    }
                }
                LastHighlightedIndex = pointIndex;

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

        private static string[] SymbolToText(string Value)
        {
            Value = Value.Replace('*', ' ');
            var result = Value.Split(new string[] { "\n", " " }, StringSplitOptions.RemoveEmptyEntries);

            //var result = Value.Split(new string[] { ";" } , StringSplitOptions.RemoveEmptyEntries);
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
        private void wpfPlot1_MouseUp(object sender, MouseButtonEventArgs e)
        {
            // determine the axis where we are now
            var axes = plt.GetAxisLimits();
            int xLow = (int)(axes.XMin + ZoomScale * 0.02);
            int xHigh = (int)(axes.XMax - ZoomScale * 0.02);
            if (e.ChangedButton != MouseButton.Left)
            {
                ZoomScale = xHigh - xLow;
            }

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
                double yLow = gattData[0];
                double yHigh = gattData[0];
                int DataLow = xLow;
                int DataHigh = xHigh;
                int lastDataIndex = (int)plt.GetDataLimits().XMax;
                if (DataHigh > lastDataIndex)
                {
                    DataHigh = lastDataIndex;
                    DataLow = lastDataIndex - ZoomScale;

                }
                if (DataLow < 0 || lastDataIndex == 0 || lastDataIndex < ZoomScale)
                {
                    DataLow = 0;
                    DataHigh = ZoomScale + 1;
                    xLow = 0;
                }


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

        private void addVLine()
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
        private void wpfPlot1_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            var axes = plt.GetAxisLimits();
            int xLow = (int)axes.XMin;
            int xHigh = (int)axes.XMax;
            ZoomScale = xHigh - xLow - 1;
            //set the Y axis limits to the high and low of the range
            fitChart(xLow, xHigh);
            wpfPlot1.Refresh();
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


        private void window_Activated(object sender, EventArgs e)
        {

        }

    }
}
