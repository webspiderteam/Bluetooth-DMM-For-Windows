using HeartRateLE.Bluetooth.Events;
using HeartRateLE.Bluetooth.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace HeartRateLE.Bluetooth
{
    public class HeartRateMonitor
    {
        private BluetoothLEDevice _heartRateDevice = null;
        private List<BluetoothAttribute> _serviceCollection = new List<BluetoothAttribute>();

        public bool LogData { get; set; }
        public int DevType { get; set; }

        private BluetoothAttribute _heartRateMeasurementAttribute;
        private BluetoothAttribute _heartRateAttribute;
        private GattCharacteristic _heartRateMeasurementCharacteristic;
        private byte[] olddata = { 0 };
        private Stopwatch threatTime;
        private int maxTime;

        /// <summary>
        /// Occurs when [connection status changed].
        /// </summary>
        public event EventHandler<Events.ConnectionStatusChangedEventArgs> ConnectionStatusChanged;
        /// <summary>
        /// Raises the <see cref="E:ConnectionStatusChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="Events.ConnectionStatusChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnConnectionStatusChanged(Events.ConnectionStatusChangedEventArgs e)
        {
            ConnectionStatusChanged?.Invoke(this, e);
        }

        /// <summary>
        /// Occurs when [value changed].
        /// </summary>
        public event EventHandler<Events.RateChangedEventArgs> RateChanged;
        /// <summary>
        /// Raises the <see cref="E:ValueChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="Events.RateChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnRateChanged(Events.RateChangedEventArgs e)
        {
            RateChanged?.Invoke(this, e);
        }

        public async Task<ConnectionResult> ConnectAsync(string deviceId, int devType)
        {
            if (_heartRateDevice != null)
            {
                await DisconnectAsync();
            }
            _heartRateDevice = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (_heartRateDevice == null)
            {
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = "Could not find specified device"
                };
            }

            //if (_heartRateDevice.DeviceInformation.Pairing.IsPaired)
            //{
            //    _heartRateDevice = null;
            //    return new Schema.ConnectionResult()
            //    {
            //        IsConnected = false,
            //        ErrorMessage = "Device is not paired"
            //    };
            //}

            // we should always monitor the connection status
            _heartRateDevice.ConnectionStatusChanged -= DeviceConnectionStatusChanged;
            _heartRateDevice.ConnectionStatusChanged += DeviceConnectionStatusChanged;

            var isReachable = await GetDeviceServicesAsync();
            if (!isReachable)
            {
                //Debug.WriteLine("2nd");
                _heartRateDevice.Dispose();
                _heartRateDevice = null;
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = "Device is unreachable or has no DMM service(i.e. out of range or shutoff).\n If error continues Check your Bluetooth settings on Windows and Try Disable and Enable Bluetooth."
                };
            }

            CharacteristicResult characteristicResult;
            characteristicResult = await SetupHeartRateCharacteristic();
            if (!characteristicResult.IsSuccess)
            {
                //Debug.WriteLine("3rd");
                if (_heartRateDevice!=null)
                    _heartRateDevice.Dispose();
                _heartRateDevice = null;
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = characteristicResult.Message
                };
            }

            // we could force propagation of event with connection status change, to run the callback for initial status
            //DeviceConnectionStatusChanged(_heartRateDevice, null);
            DevType = devType;
            return new Schema.ConnectionResult()
            {
                IsConnected = _heartRateDevice.ConnectionStatus == BluetoothConnectionStatus.Connected,
                Name = _heartRateDevice.Name
            };
        }


        private async Task<List<BluetoothAttribute>> GetServiceCharacteristicsAsync(BluetoothAttribute service)
        {
            IReadOnlyList<GattCharacteristic> characteristics = null;
            try
            {
                // Ensure we have access to the device.
                var accessStatus = await service.service.RequestAccessAsync();
                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a service. Use the cache mode to specify uncached characterstics only 
                    // and the new Async functions to get the characteristics of unpaired devices as well. 
                    var result = await service.service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
                    if (result.Status == GattCommunicationStatus.Success)
                    {
                        characteristics = result.Characteristics;
                    }
                    else
                    {
                        characteristics = new List<GattCharacteristic>();
                    }
                }
                else
                {
                    // Not granted access
                    // On error, act as if there are no characteristics.
                    characteristics = new List<GattCharacteristic>();
                }
            }
            catch (Exception ex)
            {
                characteristics = new List<GattCharacteristic>();
                Debug.WriteLine("exr2" + ex);
            }

            var characteristicCollection = new List<BluetoothAttribute>();
            characteristicCollection.AddRange(characteristics.Select(a => new BluetoothAttribute(a)));
            return characteristicCollection;
        }

        private async Task<CharacteristicResult> SetupHeartRateCharacteristic()
        {
            
            foreach (var item in _serviceCollection)
            {
                File.AppendAllText("uuids.txt", "Service: " + item.service.Uuid.ToString() + System.Environment.NewLine);
                var characteristicList = await GetServiceCharacteristicsAsync(item);
                File.AppendAllText("uuids.txt", "    Characteristics" + System.Environment.NewLine);
                foreach (var item1 in characteristicList)
                {
                    File.AppendAllText("uuids.txt", "    --->" + item1.characteristic.Uuid + "  Handle: " + item1.characteristic.AttributeHandle.ToString() + "  Props: " + item1.characteristic.CharacteristicProperties.ToString() + System.Environment.NewLine);
                }
            }
            _heartRateAttribute = _serviceCollection.Where(a => a.Name == "65520").FirstOrDefault();
            if (_heartRateAttribute == null)
            {
                await DisconnectAsync();
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Cannot find DMM service"
                };
            }

            var characteristics = await GetServiceCharacteristicsAsync(_heartRateAttribute);
            _heartRateMeasurementAttribute = characteristics.Where(a => a.Name == "65524").FirstOrDefault();
            if (_heartRateMeasurementAttribute == null)
            {
                await DisconnectAsync();
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Cannot find characteristic"
                };
            }
            _heartRateMeasurementCharacteristic = _heartRateMeasurementAttribute.characteristic;


            // Get all the child descriptors of a characteristics. Use the cache mode to specify uncached descriptors only 
            // and the new Async functions to get the descriptors of unpaired devices as well. 
            var result = await _heartRateMeasurementCharacteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
            {
                await DisconnectAsync();
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = result.Status.ToString()
                };
            }

            if (_heartRateMeasurementCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                var status = await _heartRateMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                    _heartRateMeasurementCharacteristic.ValueChanged += HeartRateValueChanged;

                return new CharacteristicResult()
                {
                    IsSuccess = status == GattCommunicationStatus.Success,
                    Message = status.ToString()
                };
            }
            else
            {
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Characteristic does not support notify"
                };
            }
        }

        private async Task<bool> GetDeviceServicesAsync()
        {
            // Note: BluetoothLEDevice.GattServices property will return an empty list for unpaired devices. For all uses we recommend using the GetGattServicesAsync method.
            // BT_Code: GetGattServicesAsync returns a list of all the supported services of the device (even if it's not paired to the system).
            // If the services supported by the device are expected to change during BT usage, subscribe to the GattServicesChanged event.
            GattDeviceServicesResult result = await _heartRateDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);
            
            if (result.Status == GattCommunicationStatus.Success)
            {
                _serviceCollection.Clear();
                _serviceCollection.AddRange(result.Services.Select(a => new BluetoothAttribute(a)));
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnects the current BLE heart rate device.
        /// </summary>
        /// <returns></returns>
        public Task DisconnectAsync()
        {
            if (_heartRateDevice != null)
            {
                if (_heartRateMeasurementCharacteristic != null)
                {
                    try
                    {
                        //NOTE: might want to do something here if the result is not successful
                        //Debug.WriteLine("1st a");
                        _heartRateMeasurementCharacteristic.ValueChanged -= HeartRateValueChanged;
                        //var result = await _heartRateMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                        if (_heartRateMeasurementCharacteristic.Service != null)
                            _heartRateMeasurementCharacteristic.Service.Dispose();
                        _heartRateMeasurementCharacteristic = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("exr" + ex);
                    }    
                }

                if (_heartRateMeasurementAttribute != null)
                {
                    //Debug.WriteLine("1st b");
                    if (_heartRateMeasurementAttribute.service != null)
                        _heartRateMeasurementAttribute.service.Dispose();
                    _heartRateMeasurementAttribute = null;
                }

                if (_heartRateAttribute != null)
                {
                    //Debug.WriteLine("1st c");
                    if (_heartRateAttribute.service != null)
                        _heartRateAttribute.service.Dispose();
                    _heartRateAttribute = null;
                }

                _serviceCollection = new List<BluetoothAttribute>();
                //Debug.WriteLine("4th");
                _heartRateDevice.ConnectionStatusChanged -= DeviceConnectionStatusChanged;
                _heartRateDevice.Dispose();
                _heartRateDevice = null;

                //DeviceConnectionStatusChanged(null, null);
            }
            return Task.CompletedTask;
        }

        private void DeviceConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            var result = new ConnectionStatusChangedEventArgs()
            {
                IsConnected = sender != null && (sender.ConnectionStatus == BluetoothConnectionStatus.Connected)
            };

            OnConnectionStatusChanged(result);
        }

        private void HeartRateValueChanged(GattCharacteristic sender, GattValueChangedEventArgs e)
        {
#if DEBUG
            threatTime = new Stopwatch();
            threatTime.Start();
#endif
            CryptographicBuffer.CopyToByteArray(e.CharacteristicValue, out byte[] data);
            if (!Enumerable.SequenceEqual(data, olddata))
            {
                
                var GattData = Utilities.ParseHeartRateValue(data, LogData, DevType);
                if ((string)GattData[0] != "Error")
                {
                    var args = new Events.RateChangedEventArgs()
                    {

                        MyGattCData = (string)GattData[0],
                        MyGattCDataSymbol = (string)GattData[1],
                        MyGattCDataMax = (bool)GattData[2],
                        MyGattCDataMin = (bool)GattData[3],
                        MyGattCDataTrue_RMS = (bool)GattData[4],
                        MyGattCDataAutoRange = (bool)GattData[5],
                        MyGattCDataDiode = (bool)GattData[6],
                        MyGattCDataContinuity = (bool)GattData[7],
                        MyGattCDataHold = (bool)GattData[8],
                        MyGattCDataInRush = (bool)GattData[9],
                        MyGattCDataPeek = (bool)GattData[10],
                        MyGattCDataACDC = (string)GattData[11],
                        MyGattCDataBattery = (bool?)GattData[12],
                        MyGattCDataHV = (bool)GattData[13],
                        MyGattCDataRel = (bool)GattData[14],
                        MyGattCDataType = (int)GattData[15],
                        MyGattCDataFunc = (string)GattData[16]
                    };
                    olddata = data;
                    OnRateChanged(args);

                }
            }
#if DEBUG
            threatTime.Stop();
            if (threatTime.ElapsedMilliseconds > 0)
            {
                maxTime = (int)threatTime.ElapsedMilliseconds > maxTime ? (int)threatTime.ElapsedMilliseconds : maxTime;
                Console.WriteLine("Elapsed={0} ms Max={1}", threatTime.ElapsedMilliseconds, maxTime);
            }
#endif
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connected.
        /// </summary>
        /// <value>
        ///   <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected
        {
            get { return _heartRateDevice != null && _heartRateDevice.ConnectionStatus == BluetoothConnectionStatus.Connected; }
        }



        /// <summary>
        /// Gets the device information for the current BLE heart rate device.
        /// </summary>
        /// <returns></returns>
        public async Task<Schema.HeartRateDeviceInfo> GetDeviceInfoAsync()
        {
            if (_heartRateDevice != null && _heartRateDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                var deviceInfoService = _serviceCollection.Where(a => a.Name == "DeviceInformation").FirstOrDefault();
                var deviceInfocharacteristics = await GetServiceCharacteristicsAsync(deviceInfoService);

                var batteryService = _serviceCollection.Where(a => a.Name == "Battery").FirstOrDefault();
                var batteryCharacteristics = await GetServiceCharacteristicsAsync(batteryService);
                //byte battery = await _batteryParser.ReadAsync();

                return new Schema.HeartRateDeviceInfo()
                {
                    DeviceId = _heartRateDevice.DeviceId,
                    Name = _heartRateDevice.Name,
                    Firmware = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "FirmwareRevisionString"),
                    Hardware = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "HardwareRevisionString"),
                    Manufacturer = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "ManufacturerNameString"),
                    SerialNumber = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "SerialNumberString"),
                    ModelNumber = await Utilities.ReadCharacteristicValueAsync(deviceInfocharacteristics, "ModelNumberString"),
                    BatteryPercent = Convert.ToInt32(await Utilities.ReadCharacteristicValueAsync(batteryCharacteristics, "BatteryLevel"))
                };
            }
            else
            {
                return new Schema.HeartRateDeviceInfo();
            }
        }

        public async Task<CharacteristicResult> WriteGattCommandAsync(byte[] Data)
        {
            if (_heartRateDevice != null && _heartRateDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                if (_heartRateMeasurementCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                {
                    IBuffer buffUTF8 = CryptographicBuffer.CreateFromByteArray(Data);
                    var status = await _heartRateMeasurementCharacteristic.WriteValueAsync(buffUTF8);

                    return new CharacteristicResult()
                    {
                        IsSuccess = status == GattCommunicationStatus.Success,
                        Message = status.ToString()
                    };
                }
                else
                {
                    return new CharacteristicResult()
                    {
                        IsSuccess = false,
                        Message = "Characteristic does not support Write"
                    };
                }
            }
            else
            {
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Not Connected to Device"
                };
            }

        }
    }
    
}