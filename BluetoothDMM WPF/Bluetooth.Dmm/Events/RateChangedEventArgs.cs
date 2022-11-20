using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HeartRateLE.Bluetooth.Events
{
    public class RateChangedEventArgs : EventArgs
    {
        public string BeatsPerMinute { get; set; }
        public string MyGattCData { get; set; }
        public string MyGattCDataSymbol { get; set; }
        public bool MyGattCDataMax { get; set; }
        public bool MyGattCDataMin { get; set; }
        public bool MyGattCDataTrue_RMS { get; set; }
        public bool MyGattCDataAutoRange { get; set; }
        public bool MyGattCDataDiode { get; set; }
        public bool MyGattCDataContinuity { get; set; }
        public bool MyGattCDataPeek { get; set; }
        public bool MyGattCDataInRush { get; set; }
        public bool MyGattCDataHold { get; set; }
        public string MyGattCDataACDC { get; set; }
        public bool? MyGattCDataBattery { get; set; }
        public bool MyGattCDataRel { get; set; }
        public bool MyGattCDataHV { get; set; }
        public int MyGattCDataType { get; set; }
    }
}
