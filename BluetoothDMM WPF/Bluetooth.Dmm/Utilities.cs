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
        private static bool MyGattCDataBattery;
        private static bool MyGattCDataPeek;
        private static bool MyGattCDataInRush;
        private static bool MyGattCDataHold;
        private static bool MyGattCDataHV;
        private static bool MyGattCDataRel;
        private static string MyGattCDataACDC;
        private static string oldMessage = "";
        private static int line = 0;
        private static byte[] olddata = { 1, 2 };


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


        private static byte[] TestData(int dev_type, int freq)
        {
            var logdata = new string[] { " " };
            if (dev_type == 2)
            {
                //10 Byte ID 2 Log data
                logdata = new string[]
                {
                    "27, 132, 113, 241, 47, 110, 207, 254, 108, 170",
                    "27, 132, 113, 241, 47, 110, 207, 246, 108, 170",
                    "27, 132, 113, 241, 47, 110, 207, 254, 108, 170",
                    "27, 132, 113, 241, 47, 110, 175, 251, 108, 170",
                    "27, 132, 113, 241, 47, 110, 207, 246, 108, 170",
                    "27, 132, 113, 241, 47, 110, 239, 246, 108, 170",
                    "27, 132, 113, 241, 47, 110, 207, 246, 108, 170",
                    "27, 132, 113, 241, 47, 110, 239, 246, 108, 170",
                    "27, 132, 113, 241, 47, 110, 175, 251, 108, 170",
                    "27, 132, 113, 241, 47, 110, 239, 246, 108, 170",
                    "27, 132, 113, 241, 47, 110, 175, 251, 108, 170",
                    "27, 132, 113, 241, 47, 110, 207, 254, 108, 170",
                    "27, 132, 113, 241, 47, 110, 239, 254, 108, 170",
                    "27, 132, 113, 241, 47, 78, 173, 254, 108, 170",
                    "27, 132, 113, 241, 47, 78, 109, 255, 108, 170",
                    "27, 132, 113, 81, 72, 46, 41, 251, 108, 170",
                    "27, 132, 113, 81, 162, 209, 50, 241, 108, 170",
                    "27, 132, 113, 181, 140, 162, 23, 246, 102, 170",
                    "27, 132, 113, 181, 140, 162, 23, 246, 102, 170",
                    "27, 132, 113, 93, 82, 170, 51, 241, 68, 170",
                    "27, 132, 113, 181, 89, 42, 217, 250, 119, 170",
                    "27, 132, 113, 181, 89, 42, 185, 251, 119, 170",
                    "27, 132, 113, 181, 89, 42, 217, 246, 119, 170",
                    "27, 132, 113, 181, 89, 42, 249, 246, 119, 170",
                    "27, 132, 113, 181, 89, 42, 121, 255, 119, 170",
                    "27, 132, 113, 181, 89, 42, 153, 252, 119, 170",
                    "27, 132, 113, 181, 89, 42, 249, 246, 119, 170",
                    "27, 132, 113, 181, 89, 42, 217, 246, 119, 170",
                    "27, 132, 113, 181, 89, 42, 185, 251, 119, 170",
                    "27, 132, 113, 85, 162, 209, 50, 241, 246, 170",
                    "27, 132, 113, 85, 178, 193, 50, 241, 119, 170",
                    "27, 132, 113, 181, 89, 42, 217, 246, 119, 170",
                    "27, 132, 113, 85, 162, 209, 50, 241, 246, 170",
                    "27, 132, 113, 85, 178, 193, 50, 241, 119, 170",
                    "27, 132, 113, 181, 89, 42, 217, 254, 119, 170",
                    "27, 132, 113, 181, 89, 42, 185, 251, 119, 170",
                    "27, 132, 113, 181, 89, 42, 217, 254, 119, 170",
                    "27, 132, 113, 181, 89, 42, 249, 254, 119, 170",
                    "27, 132, 113, 181, 89, 42, 217, 250, 102, 138",
                    "27, 132, 113, 85, 162, 97, 127, 255, 102, 42",
                    "27, 132, 113, 85, 162, 193, 210, 250, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 246, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 184, 251, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 216, 254, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 65, 248, 254, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 33, 61, 251, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 33, 61, 251, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 33, 61, 251, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 33, 61, 251, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 33, 61, 251, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 33, 61, 251, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 33, 61, 251, 102, 234",
                    "27, 132, 113, 85, 162, 33, 221, 250, 102, 234",
                    "27, 132, 113, 85, 162, 33, 61, 251, 102, 234",
                    "27, 132, 113, 93, 82, 170, 51, 241, 68, 170",
                    "27, 132, 113, 181, 89, 42, 217, 250, 119, 170",
                    "27, 132, 113, 181, 89, 106, 191, 254, 119, 170",
                    "27, 132, 113, 181, 89, 106, 159, 252, 119, 170",
                    "27, 132, 113, 181, 89, 106, 63, 251, 119, 170",
                    "27, 132, 113, 181, 89, 42, 217, 250, 102, 138",
                    "27, 132, 113, 85, 162, 193, 210, 250, 102, 42",
                    "27, 132, 113, 85, 162, 97, 191, 251, 102, 42"
                };
            }
            else if (dev_type == 1)
            {
                //10 Byte ID 1 Log data
                logdata = new string[]//ST207
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
            }
            if (line > logdata.Length) { line = 0; }
            byte[] data = Array.ConvertAll(logdata[Convert.ToInt16(line / freq)].Split(','), byte.Parse);
            line++;
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

        public static ArrayList ParseHeartRateValue(byte[] data,bool LogData)
        {

            var TestDevice = 0;
            if (TestDevice > 0)
            {
                data = TestData(TestDevice, 20);
            }

            if (data.Length>9)
            {
                try
                {
                    var newValue = "";
                    var datashift = new byte[] { 65, 33, 115, 85, 256-94, 256-63, 50, 113, 102, 256-86, 59, 256-48, 256-30, 256-88, 51, 20, 32, 26, 256-86, 256-69 };
                    var tmp = "";
                    bool[] subValue = { };
                    bool[] msubValue = { };
                    int i = 0;
                    foreach (var binaryval in data)
                    {
                        tmp = new string(Convert.ToString(binaryval ^ datashift[i], 2).PadLeft(8, '0').ToArray());
                        newValue += tmp;
                        i++;
                    }

                    if (oldMessage != newValue || oldMessage == "")
                    {
                        string[] pre_digits = new string[] { "-", ".", ".", "."};
                        string[] digits = new string[] { "", "", "", "" };
                        int dev_type = data[2] ^ datashift[2];
                        if (dev_type != 4)
                        {
                            for (int n = 0; n < 4; n++)
                            {
                                int fi = (n + 3) * 8;
                                string first = newValue.Substring(fi, 3);
                                int si = ((n + 4) * 8) + 4;
                                string second = newValue.Substring(si, 4);
                                digits[n] = (newValue.Substring(((n + 3) * 8) + 3, 1).Equals("1") ? pre_digits[n] : string.Empty) + Parsedigit(first + second, dev_type);
                            }
                        } else
                        {
                            pre_digits = new string[] { ".", ".", (newValue.Substring((14 * 8) + 3, 1).Equals("1") ? ":" : "."), ".", "" };

                            for (int n = 14; n > 9; n--)
                            {
                                
                                int fi = n * 8;
                                string first = newValue.Substring(fi, 3);
                                int si = (n * 8) + 4;
                                string second = newValue.Substring(si, 4);
                                digits[14-n] = (newValue.Substring((n * 8) + 3, 1).Equals("1") ? pre_digits[n-10] : string.Empty) + Parsedigit(first + second, dev_type);
                            }
                        }
                        MyGattCData = string.Join("", digits);
                        Debug.WriteLine(String.Format("NewVal {0} at {1}", newValue, DateTime.Now.ToString()));
                        if (LogData)
                        {
                            File.AppendAllText("log.txt", "{" + string.Join(", ", data) + "}" + System.Environment.NewLine);
                            
                            //File.AppendAllText("logbinary.txt", newValue + System.Environment.NewLine);
                        }
                        oldMessage = newValue;

                        if (data.Count() == 11)
                        {
                            MyGattCDataHold = newValue.Substring(59, 1).Equals("1");
                            MyGattCDataRel = newValue.Substring(30, 1).Equals("1");
                            MyGattCDataACDC = (newValue.Substring(68, 1).Equals("1") ? "AC" : String.Empty) +
                               (newValue.Substring(73, 1).Equals("1") ? "DC" : String.Empty);

                            MyGattCDataSymbol = (newValue.Substring(57, 1).Equals("1") ? "°C" : String.Empty) +
                                                                          (newValue.Substring(58, 1).Equals("1") ? "°F" : String.Empty) +
                                                                          (newValue.Substring(74, 1).Equals("1") ? "m" : String.Empty) +
                                                                          (newValue.Substring(75, 1).Equals("1") ? "V" : String.Empty) +
                                                                          (newValue.Substring(64, 1).Equals("1") ? "n" : String.Empty) +
                                                                          (newValue.Substring(65, 1).Equals("1") ? "m" : String.Empty) +
                                                                          (newValue.Substring(66, 1).Equals("1") ? "µ" : String.Empty) +
                                                                          (newValue.Substring(67, 1).Equals("1") ? "F" : String.Empty) +
                                                                          (newValue.Substring(69, 1).Equals("1") ? "%" : String.Empty) +
                                                                          (newValue.Substring(76, 1).Equals("1") ? "M" : String.Empty) +
                                                                          (newValue.Substring(77, 1).Equals("1") ? "k" : String.Empty) +
                                                                          (newValue.Substring(78, 1).Equals("1") ? "Ω" : String.Empty) +
                                                                          (newValue.Substring(79, 1).Equals("1") ? "Hz" : String.Empty) +
                                                                          (newValue.Substring(85, 1).Equals("1") ? "µ" : String.Empty) +
                                                                          (newValue.Substring(84, 1).Equals("1") ? "m" : String.Empty) +
                                                                          (newValue.Substring(72, 1).Equals("1") ? "A" : String.Empty);
                            MyGattCDataMax = newValue.Substring(71, 1).Equals("1");
                            MyGattCDataMin = newValue.Substring(70, 1).Equals("1");
                            MyGattCDataTrue_RMS = newValue.Substring(68, 1).Equals("1");
                            MyGattCDataAutoRange = newValue.Substring(87, 1).Equals("1");
                            MyGattCDataDiode = newValue.Substring(56, 1).Equals("1");
                            MyGattCDataContinuity = newValue.Substring(28, 1).Equals("1");
                            MyGattCDataBattery = newValue.Substring(31, 1).Equals("1");

                        }
                        else
                        {
                            if (data.Count() == 10)
                            {

                                if (dev_type == 2)
                                {
                                    subValue = (newValue.Substring(28, 4) +
                                                        newValue.Substring(56, 4) +
                                                        newValue.Substring(68, 4) +
                                                        newValue.Substring(64, 4) +
                                                        newValue.Substring(76, 4) +
                                                        newValue.Substring(72, 4)).Select(c => c == '1').ToArray();
                                    msubValue = ("0000" + "0000").Select(c => c == '1').ToArray();
                                }
                                else if (dev_type == 1)
                                {
                                    subValue = (newValue.Substring(28, 4) +
                                                        newValue.Substring(72, 4) +
                                                        newValue.Substring(56, 4) +
                                                        newValue.Substring(68, 4) +
                                                        newValue.Substring(64, 4) +
                                                        newValue.Substring(76, 4)).Select(c => c == '1').ToArray();
                                    msubValue = (newValue.Substring(72, 4) + "0000").Select(c => c == '1').ToArray();
                                }
                                MyGattCDataHold = subValue[2];
                                MyGattCDataHV = Convert.ToBoolean(Convert.ToInt16(subValue[1]) ^ Convert.ToInt16(newValue.Substring(23, 1)));
                                MyGattCDataACDC =  (subValue[8] ? "AC" : String.Empty) +
                                                    (subValue[9] ? "DC" : String.Empty);

                                MyGattCDataSymbol = (subValue[20] ? "°C" : String.Empty) +
                                                      (subValue[21] ? "°F" : String.Empty) +
                                                      (subValue[16] ? "M" : String.Empty) +
                                                      (subValue[18] ? "k" : String.Empty) +
                                                      (subValue[19] ? "Ω" : String.Empty) +
                                                      ((subValue[23] && dev_type != 1) ? "%" : String.Empty) +
                                                      (subValue[22] ? "Hz" : String.Empty) +
                                                      (subValue[11] ? "n" : String.Empty) +
                                                      (subValue[12] ? "µ" : String.Empty) +
                                                      (subValue[17] ? "m" : String.Empty) +
                                                      (subValue[10] ? "V" : String.Empty) +
                                                      (subValue[15] ? "F" : String.Empty) +
                                                      (subValue[13] ? "A" : String.Empty);
                                MyGattCDataTrue_RMS = subValue[8];
                                MyGattCDataDiode = subValue[14];
                                MyGattCDataContinuity = subValue[0];
                                MyGattCDataBattery = subValue[3];
                                MyGattCDataPeek = msubValue[1];
                                MyGattCDataInRush = msubValue[3];
                            }
                            else if (data.Count() > 18 && dev_type == 4)
                            {
                                subValue = (newValue.Substring(36, 4) +
                                                    newValue.Substring(32, 4) +
                                                    newValue.Substring(44, 4) +
                                                    newValue.Substring(40, 4) +
                                                    newValue.Substring(100, 4) +
                                                    newValue.Substring(96, 4) +
                                                    newValue.Substring(120, 4) +
                                                    newValue.Substring(132, 4) +
                                                    newValue.Substring(136, 4)).Select(c => c == '1').ToArray();
                                msubValue = ("0000" + "0000").Select(c => c == '1').ToArray();
                                MyGattCDataHold = subValue[2];

                                MyGattCDataRel = !subValue[8];
                                MyGattCDataACDC = (subValue[21] ? "AC" : String.Empty) +
                                                    (subValue[18] ? "DC" : String.Empty);


                                MyGattCDataSymbol = (subValue[27] ? "M" : String.Empty) +
                                                      (subValue[26] ? "k" : String.Empty) +
                                                      (subValue[25] ? "Ω" : String.Empty) +
                                                      (subValue[23] ? "%" : String.Empty) +
                                                      (subValue[24] ? "Hz" : String.Empty) +
                                                      (subValue[28] ? "n" : String.Empty) +
                                                      (subValue[12] ? "µ1" : String.Empty) +
                                                      (subValue[30] ? "µ2" : String.Empty) +
                                                      (subValue[29] ? "m" : String.Empty) +
                                                      (subValue[15] ? "V" : String.Empty) +
                                                      (subValue[31] ? "F" : String.Empty) +
                                                      (subValue[35] ? "A" : String.Empty);
                                MyGattCDataTrue_RMS = subValue[21];
                                MyGattCDataDiode = subValue[5];
                                MyGattCDataContinuity = subValue[6];
                                MyGattCDataPeek = subValue[9];
                                MyGattCDataMax = subValue[10];
                                MyGattCDataMin = subValue[11];
                                MyGattCDataBattery = subValue[3];
                            }
                        }
                    }
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
                                          MyGattCDataACDC,
                                          MyGattCDataBattery,
                                          MyGattCDataHV,
                                          MyGattCDataRel };
            }
            else
            {
                return new ArrayList() { data[1] };
            }
        }

        private static string Parsedigit(string digitraw, int dev_type)
        {

            switch (digitraw)
            {
                ///  aaa --> ebagfdc (flipped)
                ///  b c
                ///  ddd
                ///  e f 
                ///  ggg ----> abecdfg (rightorder)

                case "0000000": return " ";
                case "1111110": return "A";
                case "0010011": return "U";
                case "0110101": return "T";
                case "0010111": return "O";
                case "1110101": return "E";
                case "1110100": return "F";
                case "0110001": return "L";
                case "0000100": return "-";
                case "1111011": return "0";
                case "0001010": return "1";
                case "1011101": return "2";
                case "1001111": return "3";
                case "0101110": return "4";
                case "1100111": return "5";
                case "1110111": return "6";
                case "1001010": return "7";
                case "1111111": return "8";
                case "1101111": return "9";
                case "0001100": return "1";//p66 firstdigit
                case "1000000": return "-";//P66
                default: return "?";
            }
        }
    }
}
