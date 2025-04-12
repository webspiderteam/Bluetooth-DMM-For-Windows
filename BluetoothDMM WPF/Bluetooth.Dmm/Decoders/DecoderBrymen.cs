using System.Collections.Generic;

namespace BluetoothDLL.Bluetooth
{
    public static partial class Utilities
    {


        /*
         Device Reading Packet (32 bytes)
        Description Index Value Remark
        HeadByte0 [0] 0xFF HEAD
        HeadByte1 [1] 0x02 SOH
        Packet Length [2] 0x20
        Packet Type [3] 0x05 0x04: Device Information; 0x05: Device Reading
        Logging Data set ID1 [4] 0x01
        0x000001 Logging Data set ID2 [5] 0x00 for BM78xBT
        Logging Data set ID3 [6] 0x00
        Device Reading PK ID [7] 0x01 0x01 only for BM78xBT (Single Display Device)
        Device RTC Time
        [8]
        See Device RTC Time [8] ~ [13] Format
        [9]
        [10]
        [11]
        [12]
        [13]
        Device Status Flag0 [14]
        Device Status Flag1 [15] See [Status Flag0 ~ Flag2]
        Device Status Flag2 [16]
        Device Type [17] 0x01 0: Sensor; 1: Meter
        Main-Function ID [18] See Function ID Table
        Reserved [19] 0x00
        Sub-Function ID [20] See Function ID Table
        Device Reading0 [21] Device Reading [2] ~ [0]: Signed bytes
        e.g.: 0x008000 = 32768
        0xFF8000 = -32768
        Device Reading1 [22]
        Device Reading2 [23]
        Reading Decimal Point [24] See Decimal Point [24]
        Metrics Prefix [25] -9=”n”; -6=”μ”; -3=”m”; 0=” ”; 3=”k”, 6=”M”, 9=”G”
        Function Unit [26] See Function Unit [26]
        Display Digit Number [27] 3:XXX; 4:”XXXX”; 5:”XXXXX”; 6:”XXXXXX”
        Checksum0 [28] CRC calculation 1) of Index [2]~[27] bytes: To use the CRC-16
        Checksum1 [29] reverse algorithm based on the polynomial x16+x15+x2+1 (0x8005)
        EndByte0 [30] 0xFF HEAD
        EndByte1 [31] 0x03 ETX
        1)CRC calculation:
        unsigned int crc_chk(unsigned char* data, unsigned char length)
        {
        int j;
        unsigned int reg_crc=0xFFFF;
        while(length--)
        {
        10
        reg_crc ^= *data++;
        for(j=0;j<8;j++)
        {
        if(reg_crc & 0x01) // LSB(b0)=1
        reg_crc=(reg_crc>>1) ^ 0xA001;// 0x8005 reverse
        else
        reg_crc=reg_crc >>1;
        }
        }
        return reg_crc;
        }-9=”n”; -6=”μ”; -3=”m”; 0=” ”; 3=”k”, 6=”M”, 9=”G”
         */
        private static readonly Dictionary<int, char> _Prefix = new Dictionary<int, char>
        {
            { 6,'M' },
            { 3, 'k' },
            { -3, 'm' },
            { -6, 'μ' },
            { -9, 'n' }
        };

        private static readonly Dictionary<byte, string> _Units = new Dictionary<byte, string>
        {
            { 0x02, "V"},
            { 0x03, "A" },
            { 0x04, "Ω"},
            { 0x05, "℧" },
            { 0x06, "F" },
            { 0x08, "Hz" },
            { 0x0A, "%"},
            { 0x14, "°C"},
            { 0x15, "°F" },
            { 0x4F, "mA"}
        };
        public static bool brymenDecode(byte[] data)
        {
            //Check crc
            //int j;
            //uint reg_crc = 0xFFFF;
            //while (length--)
            //{
            //    reg_crc ^= *data++;
            //    for (j = 0; j < 8; j++)
            //    {
            //        if (reg_crc & 0x01) // LSB(b0)=1
            //            reg_crc = (reg_crc >> 1) ^ 0xA001;// 0x8005 reverse
            //        else
            //            reg_crc = reg_crc >> 1;
            //
            //  }
            //}
            if (data[3] == 0x05) // 0x05 Device reading, 0x04 Device info 0b0000_1111
            {
                MyGattCData = (((data[22] << 8) + data[21]) * (data[23] == 0xFF ? -1 : 1)).ToString().Insert(data[27], data[27] > 0 ? "." : "");
                MyGattCData = ((data[15] & 0b0100_0000) == 1 ? "-" : "") + MyGattCData;
                MyGattCData = ((data[15] & 0b0010_0000) == 1 ? "OL" : MyGattCData);
                _Prefix.TryGetValue(data[25], out var exponent);
                MyGattCDataSymbol = exponent.ToString() + _Units[data[26]];
                MyGattCDataRel = (data[14] & 0b0100_0000) == 1;
                MyGattCDataHold = (data[14] & 0b0010_0000) == 1;
                MyGattCDataAutoRange = (data[14] & 0b0001_0000) == 1;
                if ((data[14] & 0b0000_0100) == 1)
                {
                    switch (data[21])
                    {
                        case 0x01:
                            MyGattCData = "AUTO";
                            break;
                        case 0x02:
                            MyGattCData = "InEr";
                            break;
                        case 0x03:
                            MyGattCData = "-";
                            break;
                        case 0x04:
                            MyGattCData = "--";
                            break;
                        case 0x05:
                            MyGattCData = "---";
                            break;
                        case 0x06:
                            MyGattCData = "----";
                            break;
                        case 0x07:
                            MyGattCData = "-----";
                            break;
                        case 0x0A:
                            MyGattCData = "EF-H";
                            break;
                        case 0x0B:
                            MyGattCData = "EF-L";
                            break;
                        default:
                            break;
                    }
                }
                MyGattCDataMax = (data[14] & 0b0000_1000) == 1;
                MyGattCDataMin = (data[14] & 0b0000_0100) == 1;
                //MyGattCDataRel = (data[14] & 0b0100_0000) == 1;
            }
            return true;
        }
    }
}