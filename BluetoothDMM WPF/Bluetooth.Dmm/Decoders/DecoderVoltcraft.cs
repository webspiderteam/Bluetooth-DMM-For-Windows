using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BluetoothDLL.Bluetooth
{
    public static partial class Utilities
    {
        public static bool VoltcraftDecode(byte[] data)
        {
            /*
            
        public class BluetoothleGattAttributes
        {
            public static String BM35_BLE_SERVICE = "0000fff0-0000-1000-8000-00805f9b34fb";
            public static String BM35_BLE_SERVICE_INFO = "0000fff2-0000-1000-8000-00805f9b34fb";
            public static String BM35_BLE_SERVICE_NOTUSE = "0000fff5-0000-1000-8000-00805f9b34fb";
            public static String BM35_BLE_SERVICE_READ = "0000fff4-0000-1000-8000-00805f9b34fb";
            public static String BM35_BLE_SERVICE_SECURE = "0000fff1-0000-1000-8000-00805f9b34fb";
            public static String BM35_BLE_SERVICE_WRITE = "0000fff3-0000-1000-8000-00805f9b34fb";
            private static HashMap<String, String> attributes = new HashMap<>();
            public static String HEART_RATE_MEASUREMENT = "00002a37-0000-1000-8000-00805f9b34fb";
            public static String CLIENT_CHARACTERISTIC_CONFIG = "00002902-0000-1000-8000-00805f9b34fb";

            static {
                attributes.put("0000180d-0000-1000-8000-00805f9b34fb", "Heart Rate Service");
                attributes.put("0000180a-0000-1000-8000-00805f9b34fb", "Device Information Service");
                attributes.put(HEART_RATE_MEASUREMENT, "Heart Rate Measurement");
                attributes.put("00002a29-0000-1000-8000-00805f9b34fb", "Manufacturer Name String");
                attributes.put(BM35_BLE_SERVICE, "BM35 BLE Service");
                attributes.put(BM35_BLE_SERVICE_SECURE, "Secure");
                attributes.put(BM35_BLE_SERVICE_INFO, "Info");
                attributes.put(BM35_BLE_SERVICE_WRITE, "Write");
                attributes.put(BM35_BLE_SERVICE_READ, "Read");
                attributes.put(BM35_BLE_SERVICE_NOTUSE, "Not use");
            }
            * BLE GATT Data: 15 bytes

          * Bytes 1 (low) and 2 (high):
            D0 - D2:  Primary display: number of decimal places
            D3 - D5:  Primary display: unit prefix
            D6 - D10: Primary display: display mode
            D11:      0
            D12:      Secondary display active
            D13-D15:  0

          * Byte 3:   0xF0

          * Bytes 4 (low) and 5 (high): Primary display: counts (60000 max, then OL. Bluetooth will report up to 65535, then overflow to 0)

          * Byte 6:
            D0:       Primary display: > 65535 counts
            D1:       0
            D2:       0
            D3:       0
            D4:       Primary display: > 60000 counts (OL)
            D5:       0
            D6:       0
            D7:       Primary display: Negative value

          * Bytes 7 (low) and 8 (high):
            D0 - D2:  Secondary display: number of decimal places
            D3 - D5:  Secondary display: unit prefix
            D6 - D10: Secondary display: display mode
            D11:      1
            D12:      Secondary display active
            D13-D15:  0

          * Byte 9:   0xF0

          * Bytes 10 (low) and 11 (high): Secondary display: counts

          * Byte 12:
            D0:       Secondary display: OL (details unknown)
            D1:       0
            D2:       0
            D3:       0
            D4:       Secondary display: OL (details unknown)
            D5:       0
            D6:       0
            D7:       Secondary display: Negative value

          * Byte 13:
            D0:       HOLD
            D1:       REL
            D2:       AUTO
            D3:       LOW BATT
            D4:       MIN
            D5:       MAX
            D6:       0
            D7:       0

          * Byte 14:
            D0:       LoZ
            D1:       0
            D2:       0
            D3:       0
            D4:       Power Factor
            D5:       AC power measurement
            D6:       DC power measurement
            D7:       USB power measurement

          * Byte 15:
            D0:       0
            D1:       0
            D2:       0
            D3:       0
            D4:       0
            D5:       0
            D6:       0
            D7:       0

        --------------------------------------------------------------------------------
        Number of decimal places: 0-4, 6=UL, 7=OL

        Unit prefixes: 0 = p, 1 = n, 2 = µ, 3 = m, 4= , 5 = k, 6 = M, 7 = G

        Display mode: 0 = Voltage DC [V]
                      1 = Voltage AC [V]
                      2 = Current DC [A]
                      3 = Current AC [A]
                      4 = Resistance [Ω]
                      5 = Capacitance [F]
                      6 = Frequency [Hz]
                      7 = Duty Cycle [%]
                      8 = Temperature [°C]
                      9 = Temperature [°F]
                     10 = Diode
                     11 = Continuity
                     14 = Power [W]
                     15 = Power [VA]
                     16 = Power [PF]
                     18 = Energy [Ah]
                     19 = Time [hh:mm:ss]
                     20 = Energy [Wh]
                     21 = Voltage [V]
                     22 = Current [A]
            **Special Thansks to User 'FireBird3314' for helping reverse engineering this protocol.

            Interactive Commands
            Interactive commands are sent by writing a uint16_t number to UUID 0xfff3.

            0x0101 Select
            0x0002 Auto
            0x0102 Range
            0x0003 Backlight
            0x0103 Hold
            0x0004 Bluetooth Off
            0x0104 Relative
            0x0105 Hz/Duty
            0x0006 Normal
            0x0106 Min/Max
             */
            if (data.Length>14)
            {
                MyGattCDataHasSDisplay = true;
                MyGattCDataType = 9;
                string[] pre = { "p", "n", "µ", "m", "", "k", "M", "G" };
                int symbols = (data[1] << 8) | data[0];
                Debug.WriteLine(Convert.ToString(symbols, 2).PadLeft(16, '0'));
                int function = (symbols >> 6) & 0x1F;
                Debug.WriteLine(Convert.ToString(function, 2).PadLeft(4, '0'));
                int scale = (symbols >> 3) & 0x07;
                int point = symbols & 0x07;
                //Debug.WriteLine(scale);
                MyGattCData = "";
                if ((data[5] & 0x80) > 0) MyGattCData = "-";
                int measurement = data[4] << 8 | data[3];
                if (point == 6)
                    MyGattCData = " U.L ";
                else if (point == 7)
                    MyGattCData = " O.L ";
                else
                {
                    string tempData = (measurement).ToString("00000");
                    MyGattCData += tempData.Insert(tempData.Length - point, point > 0 ? "." : string.Empty);
                }
                if (point > 3)
                    Debug.WriteLine(point);
                string mode = Convert.ToString(data[13] << 8 | data[12], 2).PadLeft(16, '0');
                MyGattCDataHold = mode[0].Equals("1");
                MyGattCDataRel = mode[1].Equals("1");
                MyGattCDataACDC = ((new[] { 1, 3 }.Contains(function)) ? "AC" : String.Empty) +
                   ((new[] { 0, 2 }.Contains(function)) ? "DC" : String.Empty);//12 345678 12

                MyGattCDataSymbol = pre[scale] +
                                    ((function == 8) ? "°C" : String.Empty) +
                                    ((function == 9) ? "°F" : String.Empty) +
                                    ((new[] { 0, 1, 10, 21 }.Contains(function)) ? "V" : String.Empty) +
                                    ((function == 5) ? "F" : String.Empty) +
                                    ((function == 7) ? "%" : String.Empty) +
                                    ((new[] { 4, 11 }.Contains(function)) ? "Ω" : String.Empty) +
                                    ((function == 6) ? "Hz" : String.Empty) +
                                    ((new[] { 2, 3, 22 }.Contains(function)) ? "A" : String.Empty) +
                                    ((function == 14) ? "W" : String.Empty) +
                                    ((function == 15) ? "VA" : String.Empty) +
                                    ((function == 16) ? "PF" : String.Empty) +
                                    ((function == 18) ? "Ah" : String.Empty) +
                                    ((function == 19) ? "" : String.Empty) +//time
                                    ((function == 20) ? "Wh" : String.Empty);
                MyGattCDataMax = mode[5].Equals("1");
                MyGattCDataMin = mode[4].Equals("1");
                MyGattCDataTrue_RMS = false;// IsBitSet(data[0], 1);
                MyGattCDataAutoRange = mode[2].Equals("1");
                MyGattCDataDiode = function == 10;
                MyGattCDataContinuity = function == 11;
                MyGattCDataBattery = mode[3].Equals("1");
                MyGattCDataType = 6;
                if (function == 12)
                    MyGattCDataFunc = "hFE";
                else if (function == 13)
                {
                    MyGattCDataFunc = "NCV";
                    MyGattCData = measurement > 0 ? new string('-', measurement) : "EF";
                }
                if (((symbols >> 12) & 0x01) == 1)
                {
                    MyGattCDataSDisplay = "";
                    int ssymbols = (data[7] << 8) | data[6];
                    int sfunction = (ssymbols >> 6) & 0x1F;
                    int sscale = (ssymbols >> 3) & 0x07;
                    int spoint = ssymbols & 0x07;
                    MyGattCDataSDisplay = ((new[] { 1, 3 }.Contains(sfunction)) ? "AC " : String.Empty) +
                   ((new[] { 0, 2 }.Contains(sfunction)) ? "DC " : String.Empty);//12 345678 12
                    string tempSSymbol = pre[sscale] +
                                    ((sfunction == 8) ? "°C" : String.Empty) +
                                    ((sfunction == 9) ? "°F" : String.Empty) +
                                    ((new[] { 0, 1, 10, 21 }.Contains(sfunction)) ? "V" : String.Empty) +
                                    ((sfunction == 5) ? "F" : String.Empty) +
                                    ((sfunction == 7) ? "%" : String.Empty) +
                                    ((new[] { 4, 11 }.Contains(sfunction)) ? "Ω" : String.Empty) +
                                    ((sfunction == 6) ? "Hz" : String.Empty) +
                                    ((new[] { 2, 3, 22 }.Contains(sfunction)) ? "A" : String.Empty) +
                                    ((sfunction == 14) ? "W" : String.Empty) +
                                    ((sfunction == 15) ? "VA" : String.Empty) +
                                    ((sfunction == 16) ? "PF" : String.Empty) +
                                    ((sfunction == 18) ? "Ah" : String.Empty) +
                                    ((sfunction == 19) ? "" : String.Empty) +//time
                                    ((sfunction == 20) ? "Wh" : String.Empty);
                    if ((data[11] & 0x80) > 0) MyGattCDataSDisplay += "-";
                    int smeasurement = data[10] << 8 | data[9];
                    if (spoint == 6)
                        MyGattCDataSDisplay = " U.L ";
                    else if (spoint == 7)
                        MyGattCDataSDisplay = " O.L ";
                    else
                    {
                        if (sfunction != 19)
                        {
                            string tempData = (smeasurement).ToString("00000");
                            MyGattCDataSDisplay += tempData.Insert(tempData.Length - spoint, spoint > 0 ? "." : string.Empty);
                        }
                        else
                        {
                            //time display HH:MM:SS
                            TimeSpan time = TimeSpan.FromSeconds(smeasurement);
                            MyGattCDataSDisplay += time.ToString();
                        }
                    }
                    MyGattCDataSDisplay += " " + tempSSymbol;
                }
                else
                {
                    MyGattCDataSDisplay = "";
                }
                return true; 
            } else
                return false;
        }
    }
}