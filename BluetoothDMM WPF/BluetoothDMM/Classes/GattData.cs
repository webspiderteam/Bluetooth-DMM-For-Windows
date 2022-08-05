using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothDMM
{
    class GattData
    {

    }
    public class MQTT_DataFormat
    {
        public string Key { get; set; }
        public string KeyPreview { get; set; }
        public string Value { get; set; }
    }

    public class DataList
        : ObservableCollection<MQTT_DataFormat>
    {
        public DataList()
            : base()
        {
            Add(new MQTT_DataFormat { Key = "Time", KeyPreview = "\"Time\":\"01.01.2022 12:12:00\"", Value = "\"Time\":\"" });
            Add(new MQTT_DataFormat { Key = "Value(string)", KeyPreview = "\"ValueS\":\"-10.00\"", Value = "\"ValueS\":\"" });
            Add(new MQTT_DataFormat { Key = "Value(float)", KeyPreview = "\"ValueF\":-10.00", Value = "\"ValueF\":" });
            Add(new MQTT_DataFormat { Key = "Range", KeyPreview = "\"Range\":\"mV\"", Value = "\"Range\":\"" });
            Add(new MQTT_DataFormat { Key = "Current", KeyPreview = "\"Current\":\"DC\"", Value = "\"Current\":\"" });
            Add(new MQTT_DataFormat { Key = "Device", KeyPreview = "\"Device\":\"Bluetooth DMM\"", Value = "\"Device\":\"" });
            Add(new MQTT_DataFormat { Key = "AutoRange(Boolean)", KeyPreview = "\"AutoRange\":\"True\"", Value = "\"AutoRange\":\"" });
            Add(new MQTT_DataFormat { Key = "TrueRMS(Boolean)", KeyPreview = "\"TrueRMS\":\"True\"", Value = "\"TrueRMS\":\"" });
            Add(new MQTT_DataFormat { Key = "Max(Boolean)", KeyPreview = "\"Max\":\"True\"", Value = "\"Max\":\"" });
            Add(new MQTT_DataFormat { Key = "Min(Boolean)", KeyPreview = "\"Min\":\"True\"", Value = "\"Min\":\"" });
            Add(new MQTT_DataFormat { Key = "Peek(Boolean)", KeyPreview = "\"Peek\":\"True\"", Value = "\"Peek\":\"" });
            Add(new MQTT_DataFormat { Key = "InRush(Boolean)", KeyPreview = "\"InRush\":\"True\"", Value = "\"InRush\":\"" });
            Add(new MQTT_DataFormat { Key = "Buzz(Boolean)", KeyPreview = "\"Buzz\":\"True\"", Value = "\"Buzz\":\"" });
            Add(new MQTT_DataFormat { Key = "Diode(Boolean)", Value = "\"Diode\":\"True\"", KeyPreview = "\"Diode\":\"" });
            Add(new MQTT_DataFormat { Key = "Battery(Boolean)", KeyPreview = "\"Batt\":\"True\"", Value = "\"Batt\":\"" });
            Add(new MQTT_DataFormat { Key = "HV(Boolean)", KeyPreview = "\"HV\":\"True\"", Value = "\"HV\":\"" });
            Add(new MQTT_DataFormat { Key = "Rel(Boolean)", KeyPreview = "\"Rel\":\"True\"", Value = "\"Rel\":\"" });
            Add(new MQTT_DataFormat { Key = "All Booleans", KeyPreview = "\"Symbols\":[\"TRUERMS\",\"PEEK\",\"AUTORANGE\"]", Value = "\"Symbols\":" });

            
        }
    }
    public class SelectedDataList
    : ObservableCollection<MQTT_DataFormat>
    {
        public SelectedDataList()
            : base()
        {

        }
    }

}
