// <copyright file="DeviceServicesPage.xaml.cs" company="Microsoft Corporation">
// Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------------------------
using BluetoothLEExplorer.Models;
using System;
using Windows.Foundation;
using Windows.UI;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using System.Diagnostics;

namespace BluetoothLEExplorer.Views
{
    /// <summary>
    /// Device Service page
    /// </summary>
    public sealed partial class DeviceServicesPage : Page
    {
        private WindowSizeChangedEventHandler onResizeHandler;
        Windows.Storage.ApplicationDataContainer localSettings =
    Windows.Storage.ApplicationData.Current.LocalSettings;
        Windows.Storage.StorageFolder localFolder =
            Windows.Storage.ApplicationData.Current.LocalFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="DeviceServicesPage" /> class.
        /// </summary>
        public DeviceServicesPage()
        {
            var viewtb = ApplicationView.GetForCurrentView();

            // active
            viewtb.TitleBar.BackgroundColor = Colors.Transparent;
            viewtb.TitleBar.ForegroundColor = Colors.Transparent;

            // inactive  
            viewtb.TitleBar.InactiveBackgroundColor = Colors.Transparent;
            viewtb.TitleBar.InactiveForegroundColor = Colors.Transparent;

            // button
            viewtb.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            viewtb.TitleBar.ButtonForegroundColor = Colors.Black;

            viewtb.TitleBar.ButtonHoverBackgroundColor = Colors.Blue;
            viewtb.TitleBar.ButtonHoverForegroundColor = Colors.White;

            viewtb.TitleBar.ButtonPressedBackgroundColor = Colors.Blue;
            viewtb.TitleBar.ButtonPressedForegroundColor = Colors.White;
            
            viewtb.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            viewtb.TitleBar.ButtonInactiveForegroundColor = Colors.Black;
            
            var coreTitleBar = Windows.ApplicationModel.Core.CoreApplication.GetCurrentView().TitleBar;
            coreTitleBar.ExtendViewIntoTitleBar = true;
            InitializeComponent();

            HeightOfWin = Convert.ToDouble(localSettings.Values["Height"]);
            WidthOfWin = Convert.ToDouble(localSettings.Values["Width"]);
            Debug.WriteLine(HeightOfWin.ToString() + "x" + WidthOfWin.ToString());
            viewtb.SetPreferredMinSize(new Size { Width = 220, Height = 60 }); //536x694
            if (HeightOfWin !=0 && WidthOfWin !=0)
            {
                viewtb.TryResizeView(new Size { Width = WidthOfWin, Height = HeightOfWin });
            }
            else
            {
                viewtb.TryResizeView(new Size { Width = 300, Height = 80});
            }
            
            NavigationCacheMode = NavigationCacheMode.Disabled;
            //OnResize();
            //Window.Current.SizeChanged += onResizeHandler = new WindowSizeChangedEventHandler((o, e) => OnResize(o, e));
            
        }
        private double HeightOfWin;
        private double WidthOfWin;
        /// <summary>
        /// Respond to window resizing
        /// </summary>

        #region Handling when page size changes - Page_SizeChanged(sender, e) 

        private void Page_SizeChanged(object sender, SizeChangedEventArgs e) 
        { 
            
            HeightOfWin=e.NewSize.Height; 
            WidthOfWin=e.NewSize.Width;
            localSettings.Values["Height"] = HeightOfWin;
            localSettings.Values["Width"] = WidthOfWin;
        }
        #endregion

        private void Page_Unloaded(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            Debug.WriteLine("UnLoaded");
        }

        /// <summary>
        /// Updates the view model with the just selected characteristic
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CharacteristicsListView_ItemClick(object sender, ItemClickEventArgs e)
        {
            ViewModel.SelectedCharacteristic = (ObservableGattCharacteristics)e.ClickedItem;
        }

            /// <summary>
            /// Updates the view model with the just selected service
            /// </summary>
            /// <param name="sender"></param>
            /// <param name="e"></param>
            private void ServicesListView_ItemClick(object sender, ItemClickEventArgs e)
            {
                ViewModel.SelectedService = (ObservableGattDeviceService)e.ClickedItem;
            }
       
    }
    public class ActiveWhenFalseConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }
        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            return !(bool)value;
        }
    }
    public class BackgroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool && (bool)value == false)
            {
                return new SolidColorBrush(Colors.Red);
            }
            else
            {
                return new SolidColorBrush(Colors.Transparent);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }

    public class ForegroundColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            if (value is bool && (bool)value == false)
            {
                return new SolidColorBrush(Colors.White);
            }
            else
            {
                return new SolidColorBrush(Colors.Red);
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}
