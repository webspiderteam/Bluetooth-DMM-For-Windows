using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Enumeration;

namespace HeartRateLE.Bluetooth.Schema
{
    public class DeviceSelectorInfo
    {
        public DeviceSelectorInfo()
        {
            Kind = DeviceInformationKind.Unknown;
            DeviceClassSelector = DeviceClass.All;
        }

        public string DisplayName
        {
            get;
            set;
        }

        public DeviceClass DeviceClassSelector
        {
            get;
            set;
        }

        public DeviceInformationKind Kind
        {
            get;
            set;
        }

        public string Selector
        {
            get;
            set;
        }
    }
}
