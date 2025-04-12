using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BluetoothDLL.Bluetooth
{
    public static partial class Utilities
    {
        // Uni-T Decode Start...
        static string maxOLString = ".OL,O.L,OL.,OL";
        static string minOLString = "-.OL,-O.L,-OL.,-OL";
        static readonly string[] functionStrings = { "ACV", "ACmV", "DCV", "DCmV", "Hz", "%", "OHM", "CONT", "DIDOE", "CAP", "°C", "°F", "DCuA", "ACuA", "DCmA", "ACmA", "DCA", "ACA", "HFE", "Live", "NCV", "LozV", "ACA", "DCA", "LPF", "AC/DC", "LPF", "AC+DC", "LPFA", "AC+DC2", "INRUSH", "" };

        public class Measurement
        {
            private static readonly string[] _MODE = { "ACV", "ACmV", "DCV", "DCmV", "Hz", "%", "OHM", "CONT", "DIDOE", "CAP", "°C", "°F", "DCuA", "ACuA", "DCmA", "ACmA", "DCA", "ACA", "HFE", "Live", "NCV", "LozV", "ACA", "DCA", "LPF", "AC/DC", "LPF", "AC+DC", "LPF", "AC+DC2", "INRUSH" };

            private static readonly Dictionary<string, Dictionary<string, string>> _UNITS = new Dictionary<string, Dictionary<string, string>>
    {
        { "ACV", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "AC" } } },
        { "ACmV", new Dictionary<string, string> { { "0", "mV" },
            { "ACDC", "AC" } } },
        { "DCV", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "DC" } } },
        { "DCmV", new Dictionary<string, string> { { "0", "mV" },
            { "ACDC", "DC" } } },
        { "Hz", new Dictionary<string, string> { { "0", "Hz" }, { "1", "Hz" }, { "2", "kHz" }, { "3", "kHz" }, { "4", "kHz" }, { "5", "MHz" }, { "6", "MHz" }, { "7", "MHz" },
            { "ACDC", "" } } },
        { "%", new Dictionary<string, string> { { "0", "%" },
            { "ACDC", "" } } },
        { "OHM", new Dictionary<string, string> { { "0", "Ω" }, { "1", "kΩ" }, { "2", "kΩ" }, { "3", "kΩ" }, { "4", "MΩ" }, { "5", "MΩ" }, { "6", "MΩ" },
            { "ACDC", "" } } },
        { "CONT", new Dictionary<string, string> { { "0", "Ω" },
            { "ACDC", "" } } },
        { "DIDOE", new Dictionary<string, string> { { "0", "V" }, { "1", "V" },
            { "ACDC", "" } } },
        { "CAP", new Dictionary<string, string> { { "0", "nF" }, { "1", "nF" }, { "2", "uF" }, { "3", "uF" }, { "4", "uF" }, { "5", "mF" }, { "6", "mF" }, { "7", "mF" },
            { "ACDC", "" } } },
        { "°C", new Dictionary<string, string> { { "0", "°C" }, { "1", "°C" },
            { "ACDC", "" } } },
        { "°F", new Dictionary<string, string> { { "0", "°F" }, { "1", "°F" },
            { "ACDC", "" } } },
        { "DCuA", new Dictionary<string, string> { { "0", "uA" }, { "1", "uA" },
            { "ACDC", "DC" } } },
        { "ACuA", new Dictionary<string, string> { { "0", "uA" }, { "1", "uA" },
            { "ACDC", "AC" } } },
        { "DCmA", new Dictionary<string, string> { { "0", "mA" }, { "1", "mA" },
            { "ACDC", "DC" } } },
        { "ACmA", new Dictionary<string, string> { { "0", "mA" }, { "1", "mA" },
            { "ACDC", "AC" } } },
        { "DCA", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "DC" } } },
        { "ACA", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "AC" } } },
        { "HFE", new Dictionary<string, string> { { "0", "B" },
            { "ACDC", "" } } },
        { "Live", new Dictionary<string, string> { { "x","x" },
            { "ACDC", "AC" } } },
        { "NCV", new Dictionary<string, string> { { "0", "NCV" },
            { "ACDC", "" } } },
        { "LozV", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "ACA2", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "AC" } } },
        { "DCA2", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "DC" } } },
        { "LPF1", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "AC/DC", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "LPF2", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "AC+DC", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "" } } },
        { "LPF3", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "AC+DC2", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "" } } },
        { "INRUSH", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } }
    };

            private static readonly HashSet<string> _OVERLOAD = new HashSet<string> { ".OL", "O.L", "OL.", "OL", "-.OL", "-O.L", "-OL.", "-OL" };

            private static readonly HashSet<string> _NCV = new HashSet<string> { "EF", "-", "--", "---", "----", "-----" };

            private static readonly Dictionary<char, int> _EXPONENTS = new Dictionary<char, int>
    {
        { 'M', 6 },
        { 'k', 3 },
        { 'm', -3 },
        { 'u', -6 },
        { 'n', -9 }
    };

            private Dictionary<string, object> _data;

            public byte[] Binary => (byte[])_data["binary"];
            public string Mode => (string)_data["mode"];
            public string Range => (string)_data["range"];
            public string Display => (string)_data["display"];
            public bool Overload => (bool)_data["overload"];
            public decimal DisplayDecimal => (decimal)_data["display_decimal"];
            public string DisplayUnit => (string)_data["display_unit"];
            public string Unit => (string)_data["unit"];
            public decimal Value => (decimal)_data["decimal"];
            public int Progress => (int)_data["progres"];
            public bool IsMax => (bool)_data["max"];
            public bool IsMin => (bool)_data["min"];
            public bool IsHold => (bool)_data["hold"];
            public bool IsRel => (bool)_data["rel"];
            public bool IsAuto => (bool)_data["auto"];
            public bool HasBatteryWarning => (bool)_data["battery"];
            public bool HasHVWarning => (bool)_data["hvwarning"];
            public bool IsDC => (bool)_data["dc"];
            public bool IsMaxPeak => (bool)_data["peak_max"];
            public bool IsMinPeak => (bool)_data["peak_min"];
            public bool IsBarPol => (bool)_data["bar_pol"];

            public Measurement(byte[] b)
            {
                _data = new Dictionary<string, object>
                {
                    ["binary"] = b,
                    ["mode"] = _UNITS.ElementAt(b[0]).Key,
                    ["range"] = Encoding.ASCII.GetString(b, 1, 1),
                    ["display"] = Encoding.ASCII.GetString(b, 2, 7).Replace(" ", ""),
                    ["overload"] = _OVERLOAD.Contains(Encoding.ASCII.GetString(b, 2, 7).Replace(" ", ""))
                };

                if ((bool)_data["overload"])
                {
                    _data["display_decimal"] = decimal.MinValue;
                }
                else if (_NCV.Contains((string)_data["display"]))
                {
                    var switchDict = new Dictionary<string, int>
            {
                { "EF", 0 },
                { "-", 1 },
                { "--", 2 },
                { "---", 3 },
                { "----", 4 },
                { "-----", 5 }
            };
                    _data["display_decimal"] = switchDict[(string)_data["display"]];
                }
                else
                {
                    _data["display_decimal"] = decimal.Parse((string)_data["display"], CultureInfo.InvariantCulture.NumberFormat);
                }

                if (_UNITS.TryGetValue((string)_data["mode"], out var rangeUnits))
                {
                    rangeUnits.TryGetValue((string)_data["range"], out var displayUnit);
                    _data["display_unit"] = displayUnit;
                }

                _data["unit"] = _data["display_unit"];

                _data["decimal"] = _data["display_decimal"];
                if (_EXPONENTS.TryGetValue(((string)_data["unit"])[0], out var exponent) && !(bool)_data["overload"])
                {
                    _data["decimal"] = (decimal)_data["decimal"] * (decimal)Math.Pow(10, exponent);
                    _data["unit"] = ((string)_data["unit"]).Substring(1);
                }

                _data["progres"] = b[9] * 10 + b[10];
                _data["max"] = (b[11] & 8) > 0;
                _data["min"] = (b[11] & 4) > 0;
                _data["hold"] = (b[11] & 2) > 0;
                _data["rel"] = (b[11] & 1) > 0;
                _data["auto"] = (b[12] & 4) == 0;
                _data["battery"] = (b[12] & 2) > 0;
                _data["hvwarning"] = (b[12] & 1) > 0;
                _data["dc"] = (b[13] & 8) == 0;
                Console.WriteLine(b[13] + " " + (b[13] & 8));
                _data["peak_max"] = (b[13] & 4) > 0;
                _data["peak_min"] = (b[13] & 2) > 0;
                _data["bar_pol"] = (b[13] & 1) > 0;
            }


        }
        private static readonly Dictionary<string, Dictionary<string, string>> _UNITS = new Dictionary<string, Dictionary<string, string>>
    {
        { "ACV", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "AC" } } },
        { "ACmV", new Dictionary<string, string> { { "0", "mV" },
            { "ACDC", "AC" } } },
        { "DCV", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "DC" } } },
        { "DCmV", new Dictionary<string, string> { { "0", "mV" },
            { "ACDC", "DC" } } },
        { "Hz", new Dictionary<string, string> { { "0", "Hz" }, { "1", "Hz" }, { "2", "kHz" }, { "3", "kHz" }, { "4", "kHz" }, { "5", "MHz" }, { "6", "MHz" }, { "7", "MHz" },
            { "ACDC", "" } } },
        { "%", new Dictionary<string, string> { { "0", "%" },
            { "ACDC", "" } } },
        { "OHM", new Dictionary<string, string> { { "0", "Ω" }, { "1", "kΩ" }, { "2", "kΩ" }, { "3", "kΩ" }, { "4", "MΩ" }, { "5", "MΩ" }, { "6", "MΩ" },
            { "ACDC", "" } } },
        { "CONT", new Dictionary<string, string> { { "0", "Ω" },
            { "ACDC", "" } } },
        { "DIDOE", new Dictionary<string, string> { { "0", "V" }, { "1", "V" },
            { "ACDC", "" } } },
        { "CAP", new Dictionary<string, string> { { "0", "nF" }, { "1", "nF" }, { "2", "µF" }, { "3", "µF" }, { "4", "µF" }, { "5", "mF" }, { "6", "mF" }, { "7", "mF" },
            { "ACDC", "" } } },
        { "°C", new Dictionary<string, string> { { "0", "°C" }, { "1", "°C" },
            { "ACDC", "" } } },
        { "°F", new Dictionary<string, string> { { "0", "°F" }, { "1", "°F" },
            { "ACDC", "" } } },
        { "DCuA", new Dictionary<string, string> { { "0", "µA" }, { "1", "µA" },
            { "ACDC", "DC" } } },
        { "ACuA", new Dictionary<string, string> { { "0", "µA" }, { "1", "µA" },
            { "ACDC", "AC" } } },
        { "DCmA", new Dictionary<string, string> { { "0", "mA" }, { "1", "mA" },
            { "ACDC", "DC" } } },
        { "ACmA", new Dictionary<string, string> { { "0", "mA" }, { "1", "mA" },
            { "ACDC", "AC" } } },
        { "DCA", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "DC" } } },
        { "ACA", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "AC" } } },
        { "HFE", new Dictionary<string, string> { { "0", "B" },
            { "ACDC", "" } } },
        { "Live", new Dictionary<string, string> { { "x","x" },
            { "ACDC", "AC" } } },
        { "NCV", new Dictionary<string, string> { { "0", "NCV" },
            { "ACDC", "" } } },
        { "LozV", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "ACA2", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "AC" } } },
        { "DCA2", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "DC" } } },
        { "LPF1", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "AC/DC", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "LPF2", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "AC+DC", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "" } } },
        { "LPF3", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } },
        { "AC+DC2", new Dictionary<string, string> { { "1", "A" },
            { "ACDC", "" } } },
        { "INRUSH", new Dictionary<string, string> { { "0", "V" }, { "1", "V" }, { "2", "V" }, { "3", "V" },
            { "ACDC", "" } } }
    };

        public static bool Uni_tDecode(byte[] data)
        {
            if (data == null || data.Length <= 3)
            {
                return true;
            }
            if (data[0] != 0xAB || data[1] != 0xCD || data[2] != data.Length - 3)
            {
                return true;
            }
            if (data.Length == 19)
            {

                MyGattCDataTrue_RMS = false;
                MyGattCDataACDC = "";
                //var measurement = new Measurement(data);
                var function = functionStrings[(data[3] & 0x7F)];
                MyGattCData = Encoding.ASCII.GetString(data, 5, 7).Trim();
                if (_UNITS.TryGetValue(function, out var rangeUnits))
                {
                    rangeUnits.TryGetValue((data[4] - 48).ToString(), out var displayUnit);
                    MyGattCDataSymbol = displayUnit;
                }
                //loadProgress = (data[12] * 10) + data[13];
                MyGattCDataContinuity = "CONT".Equals(function);
                MyGattCDataDiode = "DIDOE".Equals(function);
                MyGattCDataMax = (data[14] & 8) != 0;
                MyGattCDataMin = (data[14] & 4) != 0;
                MyGattCDataHold = (data[14] & 2) != 0;
                MyGattCDataRel = (data[14] & 1) != 0;
                MyGattCDataAutoRange = (data[15] & 4) == 0;
                MyGattCDataBattery = (data[15] & 2) != 0;
                MyGattCDataHV = (data[15] & 1) != 0;
                //MyGattCDataACDC = (data[16] & 8) == 0 ? "DC" : "AC";
                if ("AC/DC".Equals(function))
                {
                    MyGattCDataACDC = (data[16] & 8) == 0 ? "DC" : "AC";
                }
                if (function.Contains("AC") || function.StartsWith("LPF") || "LozV".Equals(function) || "INRUSH".Equals(function))
                {
                    MyGattCDataACDC = "AC";
                    MyGattCDataTrue_RMS = true;
                }
                if (function.Contains("DC"))
                {
                    MyGattCDataACDC = "DC";
                }
                if ("NCV".Equals(function) || "HFE".Equals(function) || function.StartsWith("LPF"))
                {
                    MyGattCDataSymbol = function;
                }

                MyGattCDataPeek = (data[16] & 4) != 0;//max
                MyGattCDataPeek = (2 & data[16]) != 0;//min
                return true;
            }
            //if ((string)GattData[1] == "TypeRequest" && DevType == 4)
            //{
            //    await gattRemoteCommandTargetAttribute.characteristic.WriteValueAsync(commandSpecial[0].CommandData.AsBuffer());
            //}
            //else if ((string)GattData[1] == "DataRequest" && DevType == 4)
            //{
            //    await gattRemoteCommandTargetAttribute.characteristic.WriteValueAsync(commandSpecial[1].CommandData.AsBuffer());
            //}
            if (data.Length == 7)
            {
                if (data[0] == 0xAB && data[1] == 0xCD && data[3] == 0xFF && data[4] == 0x00)
                {
                    Debug.WriteLine("BleManager: Send/receive completed Length 7");
                    MyGattCData = "Error";
                    MyGattCDataSymbol = "DataRequest";
                    return true;
                }
                return false;
            }

            if (data.Length == 9)
            {
                if (data[0] == 0xAB && data[1] == 0xCD && data[3] == 0xAA && data[4] == 0xAA)
                {
                    //samplingManager.SetNG(true);
                    //RequestDeviceTypeAsync(curMac);
                    //sendTimer = new System.Threading.Timer(async _ =>
                    //{
                    //    await RequestDeviceTypeAsync(curMac);//SEQUENCE_GET_NAME
                    //}, null, 10, Timeout.Infinite);
                    MyGattCData = "Error";
                    MyGattCDataSymbol = "TypeRequest";
                    return true;
                }
                return false;
            }

            //for (int i = 3; i < data.Length - 2; i++)
            //{
            //    if (data[i] < 32 || data[i] > 126)
            //    {
            //        return false;
            //    }
            //}

            string typeName = Encoding.ASCII.GetString(data, 3, data.Length - 5);
            //new DeviceTypeBean(curMac, typeName).SaveToDB();

            //if (curConnectModel != null)
            //{
            //    curConnectModel.TypeName = typeName;
            //}
            //else
            //{
            //    curConnectModel = new TestDataModel();
            //    curConnectModel.TypeName = typeName;
            //}
            MyGattCData = "----";
            MyGattCDataSymbol = "V";
            Debug.WriteLine($"BleManager: Starting data reading from Uni-T");
            //StartReadTestValue(curMac);
            Debug.WriteLine($"deviceType: {typeName}");
            return false;
        }
    }
}