using ScottPlot;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media.Imaging;

namespace BluetoothDMM
{
    public class DeviceProps
    {
        public string Name { get; set; }
        public int Type { get; set; }
        public override string ToString()
        {
            return Name;
        }
    }
    public class Updates
    {
        public static IReadOnlyList<Octokit.Release> Releases { get; set; }
    }

    public class Boolean2VisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // to invert use ConverterParameter = '1'
            bool boolValue = !(bool)value;

            int param = parameter != null ? int.Parse(parameter.ToString(), NumberStyles.AllowLeadingSign) : 0;
            if (param < 0)
                boolValue = !boolValue;

            if (Math.Abs(param) == 1)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            else
                return boolValue ? Visibility.Visible : Visibility.Hidden;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    }

    public sealed class CountryIdToFlagImageSourceConverter : IValueConverter
    {

        public object Convert(object value, System.Type targetType, object parameter, CultureInfo culture)
        {

            var countryId = value as string;

            if (countryId == null)
                return null;

            try
            {
                var path = $"/Assets/Flags/{countryId.Substring(countryId.Length - 2).ToLower()}.png";
                var uri = new Uri(path, UriKind.Relative);
                var resourceStream = Application.GetResourceStream(uri);
                if (resourceStream == null)
                    return null;

                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.StreamSource = resourceStream.Stream;
                bitmap.EndInit();
                return bitmap;
            }
            catch
            {
                return null;
            }
        }

        public object ConvertBack(object value, System.Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
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
