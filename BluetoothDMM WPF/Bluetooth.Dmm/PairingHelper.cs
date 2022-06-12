using System;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;

namespace HeartRateLE.Bluetooth
{
    public class PairingHelper
    {
        public static async Task<Schema.PairingResult> PairDeviceAsync(string deviceId)
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceId);

            if (device != null)
            {
                device.DeviceInformation.Pairing.Custom.PairingRequested += Custom_PairingRequested;
                var result = await device.DeviceInformation.Pairing.Custom.PairAsync(DevicePairingKinds.ConfirmOnly);
                //var result = await device.DeviceInformation.Pairing.PairAsync();

                return new Schema.PairingResult()
                {
                    Status = result.Status.ToString()
                };
            }
            else
            {
                return new Schema.PairingResult()
                {
                    Status = string.Format("Device Id:{0} not found", deviceId)
                };
            }
        }

        private static void Custom_PairingRequested(DeviceInformationCustomPairing sender, DevicePairingRequestedEventArgs args)
        {
            args.Accept();
            //throw new NotImplementedException();
        }

        public static async Task<Schema.PairingResult> UnpairDeviceAsync(string deviceId)
        {
            var device = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (device != null)
            {
                var result = await device.DeviceInformation.Pairing.UnpairAsync();
                return new Schema.PairingResult()
                {
                    Status = result.Status.ToString()
                };
            }
            else
            {
                return new Schema.PairingResult()
                {
                    Status = string.Format("Device Id:{0} not found", deviceId)
                };
            }
        }

    }
}
