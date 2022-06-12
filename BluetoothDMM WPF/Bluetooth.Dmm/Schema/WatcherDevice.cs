using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartRateLE.Bluetooth.Schema
{
    public class WatcherDevice
    {
        public string Id { get; set; }
        public bool IsEnabled { get; set; }
        public bool IsDefault { get; set; }
        public string Name { get; set; }
        public string MacAdr { get; set; }
        public bool IsPaired { get; set; }
        public bool IsConnected { get; set; }
        public string Kind { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public Bitmap DeviceThumbnail { get; set; }
    }
}
