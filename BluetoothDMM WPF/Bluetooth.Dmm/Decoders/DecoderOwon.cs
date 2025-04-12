using System;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace BluetoothDLL.Bluetooth
{
    public static partial class Utilities
    {

        public static bool b35tDecodeOld(byte[] data)
        {
            /*
             
            1:  + or - (dec 43 or 45)
                BYTE 0
              #define REGPLUSMINUS    0x00
              #define FLAGPLUS        B00101011
              #define FLAGMINUS       B00101101

            2:  Value 0000-9999 in dec
                BYTE 1-4
              #define REGDIG1         0x01
              #define REGDIG2         0x02
              #define REGDIG3         0x03
              #define REGDIG4         0x04

            3:  Just space (dec 32)
                BYTE 5

            4:  Decimal point position
                - dec 48 no point
                - dec 49 after the first number
                - dec 50 after the second number
                - dec 52 after the third number

                BYTE 6
              #define REGPOINT        0x06
              #define FLAGPOINT0      B00110000
              #define FLAGPOINT1      B00110001
              #define FLAGPOINT2      B00110010
              #define FLAGPOINT3      B00110100

            5:  AC or DC and Auto mode
                - dec 49 DC Auto mode / 51 HOLD
                - dec 41 AC Auto mode / 43 HOLD
                - dec 17 DC Manual mode / 19 HOLD
                - dec 09 AC Manual mode / 11 HOLD

                BYTE 7
              #define REGMODE         0x07
              #define FLAGMODENONE    B00000000   //none
              #define FLAGMODEMAN     B00000001   //manual
              #define FLAGMODEHOLD    B00000010   //hold
              #define FLAGMODEREL     B00000100   //relative  
              #define FLAGMODEAC      B00001000   //ac
              #define FLAGMODEDC      B00010000   //dc
              #define FLAGMODEAUTO    B00100000   //auto
              #define FLAGMODECHECK   B11111111   //mask for check others

            6:  MIN MAX
                - dec  0 MIN MAX off
                - dec 16 MIN
                - dec 32 MAX
                BYTE 8
              #define REGMINMAX       0x08
              #define FLAGMINMAXNONE  B00000000
              #define FLAGBATT        B00000100
              #define FLAGMIN         B00010000
              #define FLAGMAX         B00100000

            7:  Units
                - dec   2   0    % Duty
                - dec   0   1 Fahrenheit
                - dec   0   2 Grad
                - dec   0   4   nF
                - dec   0   8   Hz
                - dec   0  16  hFE
                - dec   0  32  Ohm
                - dec  32  32 kOhm
                - dec  16  32 MOhm
                - dec 128  64   uA
                - dec  64  64   mA
                - dec   0  64    A
                - dec  64 128   mV
                - dec   0 128    V
                BYTES 9 & 10
              #define REGSCALE        0x09
              #define FLAGSCALEDUTY   B00000010
              #define FLAGSCALEDIODE  B00000100
              #define FLAGSCALEBUZZ   B00001000
              #define FLAGSCALEMEGA   B00010000  
              #define FLAGSCALEKILO   B00100000
              #define FLAGSCALEMILI   B01000000
              #define FLAGSCALEMICRO  B10000000
  
              #define REGUNIT         0x0a
              #define FLAGUNITNONE    B00000000
              #define FLAGUNITFAHR    B00000001
              #define FLAGUNITGRAD    B00000010
              #define FLAGUNITNF      B00000100
              #define FLAGUNITHZ      B00001000
              #define FLAGUNITHFE     B00010000
              #define FLAGUNITOHM     B00100000
              #define FLAGUNITAMP     B01000000
              #define FLAGUNITVOLT    B10000000

            8:  ???

            9:  CR + LF
             */
            MyGattCData = "";
            if (data[0] == 45) MyGattCData = "-";
            int point = Convert.ToString(data[6] & 0x07, 2).PadLeft(4, '0').IndexOf('1');
            string tempData = Encoding.ASCII.GetString(data, 1, 4);
            MyGattCData += tempData.Insert(tempData.Length - point, ".");
            MyGattCDataHold = IsBitSet(data[7], 1);
            MyGattCDataRel = IsBitSet(data[7], 2);
            MyGattCDataACDC = (IsBitSet(data[7], 3) ? "AC" : String.Empty) +
               (IsBitSet(data[7], 4) ? "DC" : String.Empty);
            MyGattCDataFunc = IsBitSet(data[10], 1) ? "hFE" : string.Empty;
            MyGattCDataSymbol = (IsBitSet(data[10], 1) ? "°C" : String.Empty) +
                                (IsBitSet(data[10], 0) ? "°F" : String.Empty) +
                                (IsBitSet(data[10], 2) && data[9] == 0 ? "n" : String.Empty) +
                                (IsBitSet(data[9], 6) ? "m" : String.Empty) +
                                (IsBitSet(data[9], 7) ? "µ" : String.Empty) +
                                (IsBitSet(data[9], 4) ? "M" : String.Empty) +
                                (IsBitSet(data[9], 5) ? "k" : String.Empty) +
                                (IsBitSet(data[10], 7) ? "V" : String.Empty) +
                                (IsBitSet(data[10], 2) ? "F" : String.Empty) +
                                (IsBitSet(data[9], 1) ? "%" : String.Empty) +
                                (IsBitSet(data[10], 5) ? "Ω" : String.Empty) +
                                (IsBitSet(data[10], 3) ? "Hz" : String.Empty) +
                                (IsBitSet(data[10], 6) ? "A" : String.Empty);
            MyGattCDataMax = IsBitSet(data[8], 5);
            MyGattCDataMin = IsBitSet(data[8], 4);
            MyGattCDataTrue_RMS = false;// IsBitSet(data[7], 1);
            MyGattCDataAutoRange = IsBitSet(data[7], 5);
            MyGattCDataDiode = IsBitSet(data[9], 2);
            MyGattCDataContinuity = IsBitSet(data[9], 3);
            MyGattCDataBattery = IsBitSet(data[8], 3);
            return true;
        }
        public static bool owonPlusTypeDecode(byte[] data)
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
            }            private final UUID SPP_UUID = UUID.fromString("00001101-0000-1000-8000-00805F9B34FB");            byte 2-3 -->
            VBAR(13),
            LPF1(12),
            LPF0(11),
            UL(10),
            PMAX(9),
            PMIN(8),
            RMR(7),
            OL(6),
            MAX(5),
            MIN(4),
            Bat(3),
            AUTO(2),
            REL(1),
            HOLD(0);            --| TrueRMS? 
            #define MODE_DC_VOLTS     0b00000000 0
            #define MODE_AC_VOLTS     0b00000001 1
            #define MODE_DC_AMPS      0b00000010 2
            #define MODE_AC_AMPS      0b00000011 3
            #define MODE_OHMS         0b00000100 4
            #define MODE_CAPACITANCE  0b00000101 5
            #define MODE_FREQUENCY    0b00000110 6
            #define MODE_PERCENT      0b00000111 7
            #define MODE_TEMP_C       0b00001000 8
            #define MODE_TEMP_F       0b00001001 9
            #define MODE_DIODE        0b00001010 10
            #define MODE_CONTINUITY   0b00001011 11
            #define MODE_HFE          0b00001100 12
            #define MODE_NCV          0b00001101 13

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
            string[] pre = { "p", "n", "µ", "m", "", "k", "M", "G" };
            int symbols = (data[1] << 8) | data[0];
            Debug.WriteLine(Convert.ToString(symbols, 2).PadLeft(16, '0'));
            int function = (symbols >> 6) & 0x0f;
            Debug.WriteLine(Convert.ToString(function, 2).PadLeft(4, '0'));
            int scale = (symbols >> 3) & 0x07;
            int point = symbols & 0x07;
            //Debug.WriteLine(scale);
            MyGattCData = "";
            if (data[0] == 45) MyGattCData = "-";
            int measurement = data[5] << 8 | data[4];
            if (point == 6)
                MyGattCData = " U.L ";
            else if (point == 7)
                MyGattCData = " O.L ";
            else
            {
                string tempData = ((measurement == (measurement & 0x7fff)) ? measurement : -1 * (measurement & 0x7fff)).ToString("0000");
                MyGattCData += tempData.Insert(tempData.Length - point, point > 0 ? "." : string.Empty);
            }
            if (point > 3)
                Debug.WriteLine(point);
            string mode = Convert.ToString(data[3] << 8 | data[2], 2).PadLeft(16, '0');
            MyGattCDataHold = mode[0].Equals("1");
            MyGattCDataRel = mode[1].Equals("1");
            MyGattCDataACDC = ((new[] { 1, 3 }.Contains(function)) ? "AC" : String.Empty) +
               ((new[] { 0, 2 }.Contains(function)) ? "DC" : String.Empty);//12 345678 12

            MyGattCDataSymbol = pre[scale] +
                                ((function == 8) ? "°C" : String.Empty) +
                                ((function == 9) ? "°F" : String.Empty) +
                                ((new[] { 0, 1, 10 }.Contains(function)) ? "V" : String.Empty) +
                                ((function == 5) ? "F" : String.Empty) +
                                ((function == 7) ? "%" : String.Empty) +
                                ((new[] { 4, 11 }.Contains(function)) ? "Ω" : String.Empty) +
                                ((function == 6) ? "Hz" : String.Empty) +
                                ((new[] { 2, 3 }.Contains(function)) ? "A" : String.Empty);
            MyGattCDataMax = mode[5].Equals("1");
            MyGattCDataMin = mode[4].Equals("1");
            MyGattCDataTrue_RMS = false;// IsBitSet(data[0], 1);
            MyGattCDataAutoRange = mode[2].Equals("1");
            MyGattCDataDiode = function == 10;
            MyGattCDataContinuity = function == 11;
            MyGattCDataBattery = mode[3].Equals("1");
            if (function == 12)
                MyGattCDataFunc = "hFE";
            else if (function == 13)
            {
                MyGattCDataFunc = "NCV";
                MyGattCData = measurement > 0 ? new string('-', measurement) : "EF";
            }
            return true;
        }
    }
}