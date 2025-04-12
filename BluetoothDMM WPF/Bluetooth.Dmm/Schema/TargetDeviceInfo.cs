using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothDLL.Bluetooth.Schema
{
    public class TargetDeviceInfo
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public string Firmware { get; set; }
        public string Hardware { get; set; }
        public string Manufacturer { get; set; }
        public string SerialNumber { get; set; }
        public string ModelNumber { get; set; }
        public int BatteryPercent { get; set; }
    }
    public class GattDeviceUUIDs
    {
        public ushort ServiceUUID { get; set; }
        public ushort NotifyUUID { get; set; }
        public ushort WriteUUID { get; set; }
        public ushort WriteUUID2 { get; set; } = 0;
        public bool UseSecondPart { get; set; } = false;

    }
    public class GattDeviceCommandDatas
    {
        public ushort CommandID { get; set; }
        public byte[] CommandData { get; set; } = null;
        public bool IsAvailable { get; set; } = false;
        public string CommandDesc { get; set; }

    }
}
