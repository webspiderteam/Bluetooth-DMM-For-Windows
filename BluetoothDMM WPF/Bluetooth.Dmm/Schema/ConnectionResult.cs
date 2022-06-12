using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartRateLE.Bluetooth.Schema
{
    public class ConnectionResult
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public bool IsConnected { get; set; }
        public bool HasError { get; set; }
        public string ErrorMessage { get; set; }
    }
}
