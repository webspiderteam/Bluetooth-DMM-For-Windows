using System;
using System.Collections.Generic;
using System.Diagnostics;
namespace BluetoothDLL.Bluetooth
{
    public static partial class Utilities
    {
        private static Dictionary<string, string> aiCareNumbers = new Dictionary<string, string>(){
                {"1111101" , "0"},
                {"0000101" , "1"},
                {"1011011" , "2"},
                {"0011111" , "3"},
                {"0100111" , "4"},
                {"0111110" , "5"},
                {"1111110" , "6"},
                {"0010101" , "7"},
                {"1111111" , "8"},
                {"0111111" , "9"},
                {"1101000" , "L"},
                {"0000000" , ""}
            };

        //OW18e decoding end

        /* AICARE AP-570C-APP Bluetooth Clamp Meter
            public static final String SERVICE_UUID_STR = "0000ffb0-0000-1000-8000-00805f9b34fb";
            private static final UUID AICARE_SERVICE_UUID = UUID.fromString(SERVICE_UUID_STR);
            private static final UUID AICARE_NOTIFY_CHARACTERISTIC_UUID = UUID.fromString("0000ffb2-0000-1000-8000-00805f9b34fb");
            private static final UUID AICARE_WRITE_CHARACTERISTIC_UUID = UUID.fromString("0000ffb1-0000-1000-8000-00805f9b34fb");
            private static final UUID DESCR_TWO = UUID.fromString("00002902-0000-1000-8000-00805f9b34fb");
        14 byte data len 
        test data (not original) {0xb0001_0111, 0xb0010_0000, 0xb0011_0101, 0xb0100_1101, 0xb0101_1011, 0xb0110_0001, 0xb0111_1111, 
                                  0xb1000_0010, 0xb1001_0111, 0xb1010_0000, 0xb1011_0000, 0xb1100_0000, 0xb1101_0000, 0xb1110_0000 };  
        data byte binary -> xxxx yyyy -> xxxx byte order no, yyyy values with byte ordered.
        digits order ABCD
        Values={"AC", "DC", "AUTO", "BT", 
                "MINUS", "A5", "A6", "A1", 
                "A4", "A3", "A7", "A2", 
                "P1", "B5", "B6", "B1", 
                "B4", "B3", "B7", "B2", 
                "P2", "C5", "C6", "C1", 
                "C4", "C3", "C7", "C2", 
                "P3", "D5", "D6", "D1", 
                "D4", "D3", "D7", "D2", 
                "µ", "n", "K", "DIODE", 
                "m", "%", "M", "BUZZ", 
                "F", "OHM", "REL", "HOLD", 
                "A", "V", "Hz", "BATTERY", 
                "", "C", "", "" }
        digits segments
                       //  111
                       // 6   2
                       // 6   2
                       //  777
                       // 5   3
                       // 5   3
                       //  444
        new Dictionary<string, string>(){{"1111101" , "0"},
        {"0000101" , "1"},
        {"1011011" , "2"},
        {"0011111" , "3"},
        {"0100111" , "4"},
        {"0111110" , "5"},
        {"1111110" , "6"},
        {"0010101" , "7"},
        {"1111111" , "8"},
        {"0111111" , "9"},
        {"1101000" , "L"},
        {"0000000" , ""}};
        */

        public static bool aiCareDecode(byte[] data)
        {
            string[] valuesArray = new string[data.Length];

            foreach (var item in data)
            {
                int itemno = ((item & 0xf0) >> 4) - 1;
                valuesArray[itemno] = Convert.ToString(item & 0x0f, 2).PadLeft(4, '0');
            }
            string values = string.Join("", valuesArray);
            Debug.WriteLine(values);
            MyGattCDataACDC = (values[0] == '1' ? "AC" : string.Empty) + (values[1] == '1' ? "DC" : string.Empty);
            MyGattCDataAutoRange = values[2] == '1';
            MyGattCDataSymbol = (values[36] == '1' ? "µ" : string.Empty) +
                                (values[37] == '1' ? "n" : string.Empty) +
                                (values[38] == '1' ? "k" : string.Empty) +
                                (values[40] == '1' ? "m" : string.Empty) +
                                (values[42] == '1' ? "M" : string.Empty) +
                                (values[41] == '1' ? "%" : string.Empty) +
                                (values[44] == '1' ? "F" : string.Empty) +
                                (values[45] == '1' ? "Ω" : string.Empty) +
                                (values[48] == '1' ? "A" : string.Empty) +
                                (values[49] == '1' ? "V" : string.Empty) +
                                (values[50] == '1' ? "Hz" : string.Empty) +
                                (values[53] == '1' ? "°C" : string.Empty);
            MyGattCDataContinuity = values[43] == '1';
            MyGattCDataDiode = values[39] == '1';
            MyGattCDataHold = values[47] == '1';
            MyGattCDataRel = values[46] == '1';
            MyGattCDataBattery = values[51] == '1';
            MyGattCData = (values[4] == '1' ? "-" : string.Empty) + aiCareNumbers[values.Substring(5, 7)] +
                          (values[12] == '1' ? "." : string.Empty) + aiCareNumbers[values.Substring(13, 7)] +
                          (values[20] == '1' ? "." : string.Empty) + aiCareNumbers[values.Substring(21, 7)] +
                          (values[28] == '1' ? "." : string.Empty) + aiCareNumbers[values.Substring(29, 7)];
            return true;
        }
    }
}