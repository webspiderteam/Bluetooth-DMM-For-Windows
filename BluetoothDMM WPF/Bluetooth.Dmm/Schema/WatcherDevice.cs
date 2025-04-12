using System.Collections.Generic;
using Windows.Devices.Enumeration;

namespace BluetoothDLL.Bluetooth.Schema
{
    public class WatcherDevice
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public string MacAdr { get; set; }
        public System.IO.Stream GlyphImage { get; set;}
        public bool IsPaired { get; set; }
        public string Kind { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public DeviceThumbnail GlyphBitmapImage { get; internal set; }
    }
}
