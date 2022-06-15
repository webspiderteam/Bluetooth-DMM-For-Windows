using HeartRateLE.Bluetooth.Schema;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;
using System.Linq;
using System.Diagnostics;
using System.Collections;
using System.IO;

namespace HeartRateLE.Bluetooth
{
    public static class Utilities
    {
        private static string MyGattCData;
        private static string MyGattCDataSymbol;
        private static bool MyGattCDataMax;
        private static bool MyGattCDataMin;
        private static bool MyGattCDataTrue_RMS;
        private static bool MyGattCDataAutoRange;
        private static bool MyGattCDataDiode;
        private static bool MyGattCDataContinuity;
        private static bool MyGattCDataPeek;
        private static bool MyGattCDataInRush;
        private static bool MyGattCDataHold;
        private static string MyGattCDataACDC;
        private static string oldMessage = "";
        private static int line=0;
        private static byte [] olddata = {1 ,2};


        public static async Task<string> ReadCharacteristicValueAsync(List<BluetoothAttribute> characteristics, string characteristicName)
        {
            var characteristic = characteristics.FirstOrDefault(a => a.Name == characteristicName)?.characteristic;
            if (characteristic == null)
                return "0";

            var readResult = await characteristic.ReadValueAsync();

            if (readResult.Status == GattCommunicationStatus.Success)
            {
                byte[] data;
                CryptographicBuffer.CopyToByteArray(readResult.Value, out data);

                if (characteristic.Uuid.Equals(GattCharacteristicUuids.BatteryLevel))
                {
                    try
                    {
                        // battery level is encoded as a percentage value in the first byte according to
                        // https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.battery_level.xml
                        return data[0].ToString();
                    }
                    catch (ArgumentException)
                    {
                        return "0";
                    }
                }
                else
                    return Encoding.UTF8.GetString(data);
            }
            else
            {
                return $"Read failed: {readResult.Status}";
            }
        }


        /// <summary>
        ///     Converts from standard 128bit UUID to the assigned 32bit UUIDs. Makes it easy to compare services
        ///     that devices expose to the standard list.
        /// </summary>
        /// <param name="uuid">UUID to convert to 32 bit</param>
        /// <returns></returns>
        public static ushort ConvertUuidToShortId(Guid uuid)
        {
            // Get the short Uuid
            var bytes = uuid.ToByteArray();
            var shortUuid = (ushort)(bytes[0] | (bytes[1] << 8));
            return shortUuid;
        }

        /// <summary>
        ///     Converts from a buffer to a properly sized byte array
        /// </summary>
        /// <param name="buffer"></param>
        /// <returns></returns>
        public static byte[] ReadBufferToBytes(IBuffer buffer)
        {
            var dataLength = buffer.Length;
            var data = new byte[dataLength];
            using (var reader = DataReader.FromBuffer(buffer))
            {
                reader.ReadBytes(data);
            }
            return data;
        }

        /// <summary>
        /// Process the raw data received from the device into application usable data,
        /// according the the Bluetooth Heart Rate Profile.
        /// https://www.bluetooth.com/specifications/gatt/viewer?attributeXmlFile=org.bluetooth.characteristic.heart_rate_measurement.xml&u=org.bluetooth.characteristic.heart_rate_measurement.xml
        /// This function throws an exception if the data cannot be parsed.
        /// </summary>
        /// <param name="data">Raw data received from the heart rate monitor.</param>
        /// <returns>The heart rate measurement value.</returns>
        
        public static ArrayList ParseHeartRateValue(byte[] data)
        {
            // Heart Rate profile defined flag values
            const byte heartRateValueFormat = 0x01;
            
            byte flags = data[0];
            bool isHeartRateValueSizeLong = ((flags & heartRateValueFormat) != 0);

            if (isHeartRateValueSizeLong)
            {
                //TODO: Convert data from dmm 1b 84 70 b1 8c a2 17 76 66 aa 3b
                try                         //1b 84 71 55 a2 61 df fe 66 2a
                {
                    var newValue = "";
                    var logdata = new string[]
                    {
                        "27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 89, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 177, 89, 42, 217, 106, 103, 42",
"27, 132, 114, 177, 89, 202, 216, 106, 103, 42",
"27, 132, 114, 81, 162, 97, 255, 118, 102, 34",
"27, 132, 114, 81, 162, 193, 210, 122, 102, 46",
"27, 132, 114, 81, 162, 65, 184, 123, 102, 46",
"27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 177, 89, 42, 217, 122, 102, 40",
"27, 132, 114, 145, 69, 58, 189, 123, 102, 40",
"27, 132, 114, 17, 108, 126, 223, 126, 102, 40",
"27, 132, 114, 17, 108, 62, 221, 122, 102, 40",
"27, 132, 114, 145, 69, 58, 249, 126, 102, 40",
"27, 132, 114, 145, 69, 58, 185, 123, 102, 40",
"27, 132, 114, 145, 69, 58, 217, 118, 102, 40",
"27, 132, 114, 17, 76, 62, 189, 123, 102, 40",
"27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 83, 178, 193, 50, 17, 102, 106",
"27, 132, 114, 179, 89, 42, 217, 26, 102, 106",
"27, 132, 114, 177, 89, 202, 56, 219, 102, 42",
"27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 83, 178, 193, 50, 17, 102, 106",
"27, 132, 114, 179, 89, 42, 217, 26, 102, 106",
"27, 132, 114, 177, 89, 202, 56, 219, 102, 42",
"27, 132, 114, 179, 89, 202, 56, 219, 102, 42",
"27, 132, 114, 179, 89, 42, 217, 26, 102, 106",
"27, 132, 114, 177, 89, 106, 63, 27, 102, 42",
"27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 83, 178, 193, 50, 17, 102, 106",
"27, 132, 114, 179, 89, 42, 217, 26, 102, 106",
"27, 132, 114, 179, 89, 202, 56, 219, 102, 42",
"27, 132, 114, 179, 89, 42, 217, 26, 102, 106",
"27, 132, 114, 177, 89, 42, 217, 122, 102, 40",
"27, 132, 114, 179, 89, 42, 217, 122, 102, 40",
"27, 132, 114, 177, 89, 42, 217, 122, 102, 40",
"27, 132, 114, 179, 89, 42, 217, 122, 102, 40",
"27, 132, 114, 177, 89, 42, 217, 122, 102, 40",
"27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 89, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 91, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 89, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 91, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 89, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 91, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 89, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 91, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 89, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 177, 89, 42, 217, 106, 103, 42",
"27, 132, 114, 177, 89, 202, 120, 111, 103, 42",
"27, 132, 114, 177, 89, 202, 184, 110, 103, 42",
"27, 132, 114, 177, 89, 202, 184, 110, 103, 43",
"27, 132, 114, 177, 89, 202, 56, 107, 103, 43",
"27, 132, 114, 177, 89, 202, 216, 106, 103, 43",
"27, 132, 114, 177, 89, 42, 217, 102, 103, 43",
"27, 132, 114, 177, 89, 42, 121, 111, 103, 43",
"27, 132, 114, 177, 89, 42, 185, 110, 103, 43",
"27, 132, 114, 177, 89, 42, 57, 107, 103, 43",
"27, 132, 114, 177, 89, 42, 217, 106, 103, 43",
"27, 132, 114, 177, 89, 42, 57, 107, 103, 42",
"27, 132, 114, 177, 89, 42, 185, 110, 103, 42",
"27, 132, 114, 177, 89, 42, 121, 111, 103, 42",
"27, 132, 114, 177, 89, 42, 217, 110, 103, 42",
"27, 132, 114, 177, 89, 42, 217, 110, 103, 43",
"27, 132, 114, 177, 89, 42, 249, 110, 103, 42",
"27, 132, 114, 177, 89, 202, 56, 107, 103, 43",
"27, 132, 114, 177, 89, 42, 217, 110, 103, 43",
"27, 132, 114, 177, 89, 42, 217, 102, 103, 42",
"27, 132, 114, 177, 89, 42, 217, 110, 103, 42",
"27, 132, 114, 177, 89, 42, 249, 110, 103, 42",
"27, 132, 114, 177, 89, 202, 56, 107, 103, 43",
"27, 132, 114, 177, 89, 42, 249, 110, 103, 43",
"27, 132, 114, 177, 89, 42, 217, 110, 103, 43",
"27, 132, 114, 177, 89, 42, 217, 110, 103, 42",
"27, 132, 114, 177, 89, 42, 249, 110, 103, 42",
"27, 132, 114, 177, 89, 202, 56, 107, 103, 42",
"27, 132, 114, 177, 89, 202, 184, 110, 103, 42",
"27, 132, 114, 177, 89, 202, 120, 111, 103, 42",
"27, 132, 114, 81, 162, 193, 210, 122, 102, 34",
"27, 132, 114, 81, 162, 97, 255, 118, 102, 34",
"27, 132, 114, 83, 162, 97, 255, 118, 102, 34",
"27, 132, 114, 81, 162, 97, 255, 118, 102, 34",
"27, 132, 114, 83, 162, 97, 255, 118, 102, 34",
"27, 132, 114, 81, 162, 97, 255, 118, 102, 34",
"27, 132, 114, 81, 162, 193, 210, 122, 102, 46",
"27, 132, 114, 81, 162, 65, 184, 123, 102, 46",
"27, 132, 114, 81, 162, 65, 216, 126, 102, 46",
"27, 132, 114, 83, 162, 65, 216, 126, 102, 46",
"27, 132, 114, 81, 162, 65, 216, 126, 102, 46",
"27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 81, 66, 36, 54, 113, 102, 42",
"27, 132, 114, 81, 166, 197, 54, 117, 102, 42",
"27, 132, 114, 81, 166, 197, 54, 113, 102, 42",
"27, 132, 114, 81, 166, 197, 54, 117, 102, 42",
"27, 132, 114, 81, 66, 36, 54, 113, 102, 42",
"27, 132, 114, 81, 166, 197, 54, 113, 102, 42",
"27, 132, 114, 81, 166, 197, 54, 117, 102, 42",
"27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 177, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 179, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 81, 166, 197, 54, 245, 98, 58",
"27, 132, 114, 177, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 179, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 81, 166, 197, 54, 245, 98, 58",
"27, 132, 114, 177, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 179, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 81, 166, 197, 54, 245, 98, 58",
"27, 132, 114, 177, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 179, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 81, 166, 197, 54, 245, 98, 58",
"27, 132, 114, 177, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 179, 89, 42, 217, 250, 98, 42",
"27, 132, 114, 81, 166, 197, 54, 245, 98, 58",
"27, 132, 114, 177, 140, 162, 23, 118, 102, 42",
"27, 132, 114, 83, 178, 193, 50, 17, 102, 106",
"27, 132, 114, 179, 89, 42, 217, 26, 102, 106",
"27, 132, 114, 177, 89, 106, 223, 218, 102, 42",
"27, 132, 114, 179, 89, 106, 223, 218, 102, 42",
"27, 132, 114, 179, 89, 42, 217, 26, 102, 106",
"27, 132, 114, 177, 89, 42, 217, 122, 102, 40",
"27, 132, 114, 89, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 91, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 89, 82, 170, 51, 81, 100, 42",
"27, 132, 114, 177, 89, 42, 217, 106, 103, 42",
"27, 132, 114, 177, 89, 202, 184, 107, 103, 42"

                    };

                        //data = Array.ConvertAll(logdata[Convert.ToInt16(line/20)].Split(','), byte.Parse);
                        //line++;

                    //data = new byte[] { 27, 132, 114, 177, 140, 162, 23, 118, 102, 42 };
                    var datashift = new byte[] { 65, 33, 115, 85, 162, 193, 50, 113, 102, 170, 59, 208, 226, 168, 51, 20, 32, 26, 170, 187 };
                    //var datashift = new byte[] { 65, 33, 115, 85, 162, 193, 50, 241, 102, 170, 59, 208, 226, 168, 51, 20, 32, 26, 170, 187 };
                    var tmp = "";
                    int i = 0;
                    foreach (var binaryval in data) {
                        tmp =  new string(Convert.ToString(binaryval ^ datashift[i], 2).PadLeft(8, '0').Reverse().ToArray());// Convert.ToString(binaryval ^ datashift[i], 2).PadLeft(8, '0');
                        newValue += tmp;
                        i++;
                        
                    }
                    
                    //GattSampleContext.Context.MyGattCData = binaryvalstr; {27, 132, 112, 177, 140, 162, 23, 118, 102, 170, 59}
                    

                    if (oldMessage != newValue || oldMessage == "")
                    {
                        string[] pre_digits = new string[] { "-", ".", ".", "." };
                        string[] digits = new string[] { "", "", "", "" };
                        for (int n = 0; n < 4; n++)
                        {
                            digits[n] = (newValue.Substring(((n + 3) * 8) + 4, 1).Equals("1") ? pre_digits[n] : string.Empty) + Parsedigit(newValue.Substring(((n + 3) * 8) + 5, 7));
                        }
                        MyGattCData = string.Join("",digits);
                        Debug.WriteLine(String.Format("NewVal {0} at {1}", newValue, DateTime.Now.ToString()));
                        File.AppendAllText("log.txt", "{" + string.Join(", ", data) + "}" + System.Environment.NewLine);
                        //File.AppendAllText("logbinary.txt", newValue + System.Environment.NewLine);
                        oldMessage = newValue;
                        if (data.Count() == 11){
                            MyGattCDataHold = newValue.Substring(60, 1).Equals("1");
                            MyGattCDataACDC = (newValue.Substring(25, 1).Equals("1") ? "Δ " : String.Empty) +
                               (newValue.Substring(67, 1).Equals("1") ? "AC " : String.Empty) +
                               (newValue.Substring(78, 1).Equals("1") ? "DC " : String.Empty);


                            MyGattCDataSymbol = (newValue.Substring(62, 1).Equals("1") ? "°C" : String.Empty) +
                              (newValue.Substring(61, 1).Equals("1") ? "°F" : String.Empty) +
                              (newValue.Substring(77, 1).Equals("1") ? "m" : String.Empty) +
                              (newValue.Substring(76, 1).Equals("1") ? "V" : String.Empty) +
                              (newValue.Substring(71, 1).Equals("1") ? "n" : String.Empty) +
                              (newValue.Substring(70, 1).Equals("1") ? "m" : String.Empty) +
                              (newValue.Substring(69, 1).Equals("1") ? "µ" : String.Empty) +
                              (newValue.Substring(68, 1).Equals("1") ? "F" : String.Empty) +
                              (newValue.Substring(66, 1).Equals("1") ? "%" : String.Empty) +
                              (newValue.Substring(75, 1).Equals("1") ? "M" : String.Empty) +
                              (newValue.Substring(74, 1).Equals("1") ? "k" : String.Empty) +
                              (newValue.Substring(73, 1).Equals("1") ? "Ω" : String.Empty) +
                              (newValue.Substring(72, 1).Equals("1") ? "Hz" : String.Empty) +
                              (newValue.Substring(82, 1).Equals("1") ? "µ" : String.Empty) +
                              (newValue.Substring(83, 1).Equals("1") ? "m" : String.Empty) +
                              (newValue.Substring(79, 1).Equals("1") ? "A" : String.Empty);
                            MyGattCDataMax = newValue.Substring(64, 1).Equals("1");
                            MyGattCDataMin = newValue.Substring(65, 1).Equals("1");
                            MyGattCDataTrue_RMS = newValue.Substring(67, 1).Equals("1");
                            MyGattCDataAutoRange = newValue.Substring(80, 1).Equals("1");
                            MyGattCDataDiode = newValue.Substring(63, 1).Equals("1");
                            MyGattCDataContinuity = newValue.Substring(27, 1).Equals("1");

                        } else if (data.Count() == 10)
                        {
                            var dev_type = data[2] ^ datashift[2];
                            if (dev_type == 3)
                            {
                                MyGattCDataHold = newValue.Substring(60, 1).Equals("1");
                                MyGattCDataACDC = (newValue.Substring(26, 1).Equals("1") ? "Ϟ " : String.Empty) +
                                   (newValue.Substring(67, 1).Equals("1") ? "AC " : String.Empty) +
                                   (newValue.Substring(66, 1).Equals("1") ? "DC " : String.Empty);


                                MyGattCDataSymbol = (newValue.Substring(79, 1).Equals("1") ? "°C" : String.Empty) +
                                  (newValue.Substring(78, 1).Equals("1") ? "°F" : String.Empty) +
                                  (newValue.Substring(75, 1).Equals("1") ? "M" : String.Empty) +
                                  (newValue.Substring(73, 1).Equals("1") ? "k" : String.Empty) +
                                  (newValue.Substring(72, 1).Equals("1") ? "Ω" : String.Empty) +
                                  (newValue.Substring(66, 1).Equals("1") ? "%" : String.Empty) +
                                  (newValue.Substring(72, 1).Equals("1") ? "Hz" : String.Empty) +
                                  (newValue.Substring(64, 1).Equals("1") ? "n" : String.Empty) +
                                  (newValue.Substring(71, 1).Equals("1") ? "µ" : String.Empty) +
                                  (newValue.Substring(74, 1).Equals("1") ? "m" : String.Empty) +
                                  (newValue.Substring(65, 1).Equals("1") ? "V" : String.Empty) +
                                  (newValue.Substring(68, 1).Equals("1") ? "F" : String.Empty) +
                                  (newValue.Substring(70, 1).Equals("1") ? "A" : String.Empty);
                                MyGattCDataTrue_RMS = newValue.Substring(67, 1).Equals("1");
                                MyGattCDataDiode = newValue.Substring(69, 1).Equals("1");
                                MyGattCDataContinuity = newValue.Substring(27, 1).Equals("1");
                            }else if (dev_type == 1)
                            {
                                MyGattCDataHold = newValue.Substring(25, 1).Equals("1");
                                MyGattCDataACDC = (newValue.Substring(26, 1).Equals("0") ? "Ϟ " : String.Empty) +
                                   (newValue.Substring(63, 1).Equals("1") ? "AC " : String.Empty) +
                                   (newValue.Substring(62, 1).Equals("1") ? "DC " : String.Empty);


                                MyGattCDataSymbol = (newValue.Substring(75, 1).Equals("1") ? "°C" : String.Empty) +
                                  (newValue.Substring(74, 1).Equals("1") ? "°F" : String.Empty) +
                                  (newValue.Substring(71, 1).Equals("1") ? "M" : String.Empty) +
                                  (newValue.Substring(69, 1).Equals("1") ? "k" : String.Empty) +
                                  (newValue.Substring(68, 1).Equals("1") ? "Ω" : String.Empty) +
                                  //(newValue.Substring(66, 1).Equals("1") ? "%" : String.Empty) +
                                  (newValue.Substring(73, 1).Equals("1") ? "Hz" : String.Empty) +
                                  (newValue.Substring(60, 1).Equals("1") ? "n" : String.Empty) +
                                  (newValue.Substring(67, 1).Equals("1") ? "µ" : String.Empty) +
                                  (newValue.Substring(70, 1).Equals("1") ? "m" : String.Empty) +
                                  (newValue.Substring(61, 1).Equals("1") ? "V" : String.Empty) +
                                  (newValue.Substring(64, 1).Equals("1") ? "F" : String.Empty) +
                                  (newValue.Substring(66, 1).Equals("1") ? "A" : String.Empty);
                                MyGattCDataTrue_RMS = newValue.Substring(63, 1).Equals("1");
                                MyGattCDataDiode = newValue.Substring(65, 1).Equals("1");
                                MyGattCDataContinuity = newValue.Substring(27, 1).Equals("1");
                                MyGattCDataPeek= newValue.Substring(78, 1).Equals("1");
                                MyGattCDataInRush = newValue.Substring(76, 1).Equals("1");
                            }
                        }
                    }
                    //return "Unknown format: " + binaryvalstr;
                    
                }
                
                catch (ArgumentException)
                {
                    MyGattCData = "Error binary value";
                }
                return new ArrayList() { MyGattCData,
                                          MyGattCDataSymbol,
                                          MyGattCDataMax,
                                          MyGattCDataMin,
                                          MyGattCDataTrue_RMS,
                                          MyGattCDataAutoRange,
                                          MyGattCDataDiode,
                                          MyGattCDataContinuity,
                                          MyGattCDataHold,
                                          MyGattCDataInRush,
                                          MyGattCDataPeek,
                                          MyGattCDataACDC };
            }
            else
            {
                return new ArrayList() { data[1] };
            }
        }

        private static string Parsedigit(string digitraw)
        {

            switch (digitraw)
            {
                ///  aaa --> ebagfdc
                ///  b c
                ///  ddd
                ///  e f
                ///  ggg
                
                case "0000000": return " ";
                case "1110111": return "A";
                case "1001100": return "U";
                case "1101010": return "T";
                case "1001110": return "O";
                case "1111010": return "E";
                case "1110010": return "F";
                case "1101000": return "L";
                case "0000010": return "-";
                case "1111101": return "0";
                case "0000101": return "1";
                case "1011011": return "2";
                case "0011111": return "3";
                case "0100111": return "4";
                case "0111110": return "5";
                case "1111110": return "6";
                case "0010101": return "7";
                case "1111111": return "8";
                case "0111111": return "9";
                default: return "?";
            }
        }


    }
}
