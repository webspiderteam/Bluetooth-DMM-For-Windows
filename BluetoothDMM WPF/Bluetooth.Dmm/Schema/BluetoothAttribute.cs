using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace HeartRateLE.Bluetooth.Schema
{
    /// <summary>
    ///     Represents the display of an attribute - both characteristics and services.
    /// </summary>
    public class BluetoothAttribute
    {
        public GattCharacteristic characteristic;
        public GattDescriptor descriptor;

        public GattDeviceService service;

        public BluetoothAttribute(GattDeviceService service)
        {
            this.service = service;
            AttributeDisplayType = BluetoothAttributeType.Service;
        }

        public BluetoothAttribute(GattCharacteristic characteristic)
        {
            this.characteristic = characteristic;
            AttributeDisplayType = BluetoothAttributeType.Characteristic;
        }

        public string Name
        {
            get
            {
                switch (AttributeDisplayType)
                {
                    case BluetoothAttributeType.Service:
                        if (IsSigDefinedUuid(service.Uuid))
                        {
                            GattNativeServiceUuid serviceName;
                            if (Enum.TryParse(Utilities.ConvertUuidToShortId(service.Uuid).ToString(), out serviceName))
                            {
                                return serviceName.ToString();
                            }
                        }
                        else
                        {
                            return "Custom Service: " + service.Uuid;
                        }
                        break;
                    case BluetoothAttributeType.Characteristic:
                        if (IsSigDefinedUuid(characteristic.Uuid))
                        {
                            GattNativeCharacteristicUuid characteristicName;
                            if (Enum.TryParse(Utilities.ConvertUuidToShortId(characteristic.Uuid).ToString(),
                                out characteristicName))
                            {
                                return characteristicName.ToString();
                            }
                        }
                        else
                        {
                            if (!string.IsNullOrEmpty(characteristic.UserDescription))
                            {
                                return characteristic.UserDescription;
                            }

                            else
                            {
                                return "Custom Characteristic: " + characteristic.Uuid;
                            }
                        }
                        break;
                    default:
                        break;
                }
                return "Invalid";
            }
        }

        public BluetoothAttributeType AttributeDisplayType { get; }

        /// <summary>
        ///     The SIG has a standard base value for Assigned UUIDs. In order to determine if a UUID is SIG defined,
        ///     zero out the unique section and compare the base sections.
        /// </summary>
        /// <param name="uuid">The UUID to determine if SIG assigned</param>
        /// <returns></returns>
        private static bool IsSigDefinedUuid(Guid uuid)
        {
            var bluetoothBaseUuid = new Guid("00000000-0000-1000-8000-00805F9B34FB");

            var bytes = uuid.ToByteArray();
            // Zero out the first and second bytes
            // Note how each byte gets flipped in a section - 1234 becomes 34 12
            // Example Guid: 35918bc9-1234-40ea-9779-889d79b753f0
            //                   ^^^^
            // bytes output = C9 8B 91 35 34 12 EA 40 97 79 88 9D 79 B7 53 F0
            //                ^^ ^^
            bytes[0] = 0;
            bytes[1] = 0;
            var baseUuid = new Guid(bytes);
            return baseUuid == bluetoothBaseUuid;
        }
    }
}
