using BluetoothDLL.Bluetooth.Events;
using BluetoothDLL.Bluetooth.Schema;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Security.RightsManagement;
using System.Threading.Tasks;
using System.Xml.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;
using Windows.Security.Cryptography;
using Windows.Storage.Streams;

namespace BluetoothDLL.Bluetooth
{
    public class GattMonitor
    {
        private BluetoothLEDevice _targetDevice = null;
        private List<BluetoothAttribute> _serviceCollection = new List<BluetoothAttribute>();

        public bool LogData { get; set; }
        public int DevType { get; set; }
        private GattDeviceUUIDs[] deviceUUIDs = { new GattDeviceUUIDs() { ServiceUUID = 0xFFF0, NotifyUUID = 0XFFF4, WriteUUID = 0XFFF3 },
                                                        new GattDeviceUUIDs() {ServiceUUID = 0xFFF0, NotifyUUID = 0XFFF4, WriteUUID = 0XFFF3},
                                                        new GattDeviceUUIDs() {ServiceUUID = 0xFFF0, NotifyUUID = 0XFFF4, WriteUUID = 0XFFF3},
                                                        new GattDeviceUUIDs() {ServiceUUID = 0xFFB0, NotifyUUID = 0XFFB2, WriteUUID = 0XFFB1},
                                                        new GattDeviceUUIDs() {ServiceUUID = 0xFE7D, NotifyUUID = 0X1E4D, WriteUUID = 0X6DAA, WriteUUID2 = 0x8841, UseSecondPart = true}
        };
        private static readonly byte[] SEQUENCE_GET_NAME = { 0xAB, 0xCD, 0x03, 0x5F, 0x01, 0xDA };
        private static readonly byte[] SEQUENCE_SEND_LAMP = { 0xAB, 0xCD, 0x03, 0x4B, 0x01, 0xC6 };
        private static readonly byte[] SEQUENCE_SEND_DATA2 = { 0xAB, 0xCD, 0x03, 0x5D, 0x01, 0xD8 };
        private static readonly byte[] SEQUENCE_SEND_DATA = { 0xAB, 0xCD, 0x03, 0x5E, 0x01, 0xD9 };

        private GattDeviceCommandDatas[] commandSpecial = new GattDeviceCommandDatas[] {
                    new GattDeviceCommandDatas() { CommandID = 1, CommandData = new byte[]{ 0xAB, 0xCD, 0x03, 0x5F, 0x01, 0xDA }, CommandDesc = "Get Device TypeName", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 2, CommandData = new byte[]{ 0xAB, 0xCD, 0x03, 0x5D, 0x01, 0xD8 }, CommandDesc = "Get Data", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 3, CommandData = new byte[]{ 0xAB, 0xCD, 0x03, 0x4B, 0x01, 0xC6 }, CommandDesc = "Get BackLight", IsAvailable = true}
        };

        private List<GattDeviceCommandDatas[]> commandDatas = new List<GattDeviceCommandDatas[]> {
            new GattDeviceCommandDatas[] {
                    new GattDeviceCommandDatas() { CommandID = 1, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XED, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X99 }, CommandDesc = "Auto Range", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 2, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE3, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X9B }, CommandDesc = "C degree", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 3, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE2, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X98 }, CommandDesc = "F degree", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 4, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE5, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X81 }, CommandDesc = "Capacitance", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 5, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE4, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X86 }, CommandDesc = "Diode", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 6, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE7, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X87 }, CommandDesc = "NCV", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 7, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE6, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X84 }, CommandDesc = "Hz", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 8, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE1, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X85 }, CommandDesc = "Hold", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 9, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0X84, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0XE6 }, CommandDesc = "Min/Max", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 10, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0X93, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0XEB }, CommandDesc = "mV", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 11, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XEB, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X93 }, CommandDesc = "OHM", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 12, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0X9C, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0XEE }, CommandDesc = "A/mA", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 13, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0X91, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X95 }, CommandDesc = "AC/DC", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 14, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE0, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X9A }, CommandDesc = "ZERO", IsAvailable = true}
            },
            new GattDeviceCommandDatas[] {
                    new GattDeviceCommandDatas() { CommandID = 1, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XED, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X99 }, CommandDesc = "Auto Range", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 2, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE3, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X9B }, CommandDesc = "C degree", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 3, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE2, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X98 }, CommandDesc = "F degree", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 4, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE5, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X81 }, CommandDesc = "Capacitance", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 5, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE4, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X86 }, CommandDesc = "Diode", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 6, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE7, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X87 }, CommandDesc = "NCV", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 7, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE6, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X84 }, CommandDesc = "Hz", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 8, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE1, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X85 }, CommandDesc = "Hold", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 9, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0X84, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0XE6 }, CommandDesc = "Min/Max", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 10, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0X93, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0XEB }, CommandDesc = "mV", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 11, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XEB, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X93 }, CommandDesc = "OHM", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 12, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0X9C, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0XEE }, CommandDesc = "A/mA", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 13, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0X91, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X95 }, CommandDesc = "AC/DC", IsAvailable = true},
                    new GattDeviceCommandDatas() { CommandID = 14, CommandData = new byte[]{ 0XEA, 0XEC, 0X70, 0XE0, 0XA2, 0XC1, 0X32, 0X71, 0X64, 0X9A }, CommandDesc = "ZERO", IsAvailable = true}
            },
            new GattDeviceCommandDatas[] { },
            new GattDeviceCommandDatas[] { }
        };

        private BluetoothAttribute gattMeasurementAttribute;
        private bool _useNewWriteUuid = true;
        private BluetoothAttribute gattRemoteCommandTargetAttribute;
        private BluetoothAttribute _gattAttribute;
        private GattCharacteristic _gattMeasurementCharacteristic;
        private GattCharacteristic _gattRemoteCommandTargetCharacteristic;
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
        public event EventHandler<Events.DataChangedEventArgs> RateChanged;
        /// <summary>
        /// Raises the <see cref="E:ValueChanged" /> event.
        /// </summary>
        /// <param name="e">The <see cref="Events.DataChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnRateChanged(Events.DataChangedEventArgs e)
        {
            RateChanged?.Invoke(this, e);
        }

        public async Task<ConnectionResult> ConnectAsync(string deviceId, int devType)
        {
            DevType = devType;
            if (_targetDevice != null)
            {
                await DisconnectAsync();
            }
            _targetDevice = await BluetoothLEDevice.FromIdAsync(deviceId);
            if (_targetDevice == null)
            {
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = "Could not find specified device"
                };
            }

            //if (_targetDevice.DeviceInformation.Pairing.IsPaired)
            //{
            //    _targetDevice = null;
            //    return new Schema.ConnectionResult()
            //    {
            //        IsConnected = false,
            //        ErrorMessage = "Device is not paired"
            //    };
            //}

            // we should always monitor the connection status
            _targetDevice.ConnectionStatusChanged -= DeviceConnectionStatusChanged;
            _targetDevice.ConnectionStatusChanged += DeviceConnectionStatusChanged;

            var isReachable = await GetDeviceServicesAsync();
            if (!isReachable)
            {
                //Debug.WriteLine("2nd");
                _targetDevice.Dispose();
                _targetDevice = null;
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = "Device is unreachable or has no DMM gatt_attrib(i.e. out of range or shutoff).\n If error continues Check your Bluetooth settings on Windows and Try Disable and Enable Bluetooth."
                };
            }

            CharacteristicResult characteristicResult;
            characteristicResult = await SetupGattCharacteristic();
            if (!characteristicResult.IsSuccess)
            {
                //Debug.WriteLine("3rd");
                if (_targetDevice != null)
                    _targetDevice.Dispose();
                _targetDevice = null;
                return new Schema.ConnectionResult()
                {
                    IsConnected = false,
                    ErrorMessage = characteristicResult.Message
                };
            }

            // we could force propagation of event with connection status change, to run the callback for initial status
            //DeviceConnectionStatusChanged(_targetDevice, null);
            if (DevType == 4)
            {
                if (_useNewWriteUuid)
                    await gattRemoteCommandTargetAttribute.characteristic.WriteValueAsync(commandSpecial[0].CommandData.AsBuffer(), GattWriteOption.WriteWithoutResponse);
                else
                    await gattRemoteCommandTargetAttribute.characteristic.WriteValueAsync(commandSpecial[0].CommandData.AsBuffer(), GattWriteOption.WriteWithResponse);
            }
            return new Schema.ConnectionResult()
            {
                IsConnected = _targetDevice.ConnectionStatus == BluetoothConnectionStatus.Connected,
                Name = _targetDevice.Name
            };
        }


        private async Task<List<BluetoothAttribute>> GetServiceCharacteristicsAsync(BluetoothAttribute gatt_attrib)
        {
            IReadOnlyList<GattCharacteristic> characteristics = null;
            try
            {
                // Ensure we have access to the device.
                var accessStatus = await gatt_attrib.service.RequestAccessAsync();
                if (accessStatus == DeviceAccessStatus.Allowed)
                {
                    // BT_Code: Get all the child characteristics of a gatt_attrib. Use the cache mode to specify uncached characterstics only 
                    // and the new Async functions to get the characteristics of unpaired devices as well. 
                    var result = await gatt_attrib.service.GetCharacteristicsAsync(BluetoothCacheMode.Uncached);
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

        private async Task<CharacteristicResult> SetupGattCharacteristic()
        {
            //if (!deviceUUIDs[DevType].UseSecondPart)
            _gattAttribute = _serviceCollection.Where(a => a.Name == deviceUUIDs[DevType].ServiceUUID.ToString()).FirstOrDefault();
            //else
            //    _gattAttribute = _serviceCollection.Where(a => a.Name.Substring(9, 4).ToUpper() == deviceUUIDs[DevType].ServiceUUID.ToString()).FirstOrDefault();
            if (_gattAttribute == null)
            {
                await DisconnectAsync();
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Cannot find DMM gatt_attrib"
                };
            }

            var characteristics = await GetServiceCharacteristicsAsync(_gattAttribute);
            gattMeasurementAttribute = characteristics.Where(a => a.Name == deviceUUIDs[DevType].NotifyUUID.ToString()).FirstOrDefault();
            gattRemoteCommandTargetAttribute = characteristics.Where(a => a.Name == deviceUUIDs[DevType].WriteUUID.ToString()).FirstOrDefault();
            if (gattRemoteCommandTargetAttribute == null && deviceUUIDs[DevType].WriteUUID2 != 0)
            {
                _useNewWriteUuid = false;
                gattRemoteCommandTargetAttribute = characteristics.Where(a => a.Name == deviceUUIDs[DevType].WriteUUID2.ToString()).FirstOrDefault();
            }
            if (gattMeasurementAttribute == null)
            {
                await DisconnectAsync();
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = "Cannot find characteristic"
                };
            }
            _gattMeasurementCharacteristic = gattMeasurementAttribute.characteristic;

            //if (gattRemoteCommandTargetAttribute == null)
            //{
            //    await DisconnectAsync();
            //    return new CharacteristicResult()
            //    {
            //        IsSuccess = false,
            //        Message = "Cannot find characteristic"
            //    };
            //}
            //_gattRemoteCommandTargetCharacteristic = gattRemoteCommandTargetAttribute.characteristic;

            // Get all the child descriptors of a characteristics. Use the cache mode to specify uncached descriptors only 
            // and the new Async functions to get the descriptors of unpaired devices as well. 
            var result = await _gattMeasurementCharacteristic.GetDescriptorsAsync(BluetoothCacheMode.Uncached);
            if (result.Status != GattCommunicationStatus.Success)
            {
                await DisconnectAsync();
                return new CharacteristicResult()
                {
                    IsSuccess = false,
                    Message = result.Status.ToString()
                };
            }

            if (_gattMeasurementCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.Notify))
            {
                var status = await _gattMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorAsync(GattClientCharacteristicConfigurationDescriptorValue.Notify);
                if (status == GattCommunicationStatus.Success)
                    _gattMeasurementCharacteristic.ValueChanged += GattValueChanged;

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
            GattDeviceServicesResult result = await _targetDevice.GetGattServicesAsync(BluetoothCacheMode.Uncached);

            if (result.Status == GattCommunicationStatus.Success)
            {
                _serviceCollection.Clear();
                _serviceCollection.AddRange(result.Services.Select(a => new BluetoothAttribute(a)));
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
                return true;
            }
            else
            {
                return false;
            }
        }

        /// <summary>
        /// Disconnects the current BLE device.
        /// </summary>
        /// <returns></returns>
        public Task DisconnectAsync()
        {
            if (_targetDevice != null)
            {
                if (_gattMeasurementCharacteristic != null)
                {
                    try
                    {
                        //NOTE: might want to do something here if the result is not successful
                        //Debug.WriteLine("1st a");
                        _gattMeasurementCharacteristic.ValueChanged -= GattValueChanged;
                        //var result = await _gattMeasurementCharacteristic.WriteClientCharacteristicConfigurationDescriptorWithResultAsync(GattClientCharacteristicConfigurationDescriptorValue.None);
                        if (_gattMeasurementCharacteristic.Service != null)
                            _gattMeasurementCharacteristic.Service.Dispose();
                        _gattMeasurementCharacteristic = null;
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine("exr" + ex);
                    }
                }

                if (gattMeasurementAttribute != null)
                {
                    //Debug.WriteLine("1st b");
                    if (gattMeasurementAttribute.service != null)
                        gattMeasurementAttribute.service.Dispose();
                    gattMeasurementAttribute = null;
                }

                if (_gattAttribute != null)
                {
                    //Debug.WriteLine("1st c");
                    if (_gattAttribute.service != null)
                        _gattAttribute.service.Dispose();
                    _gattAttribute = null;
                }

                _serviceCollection = new List<BluetoothAttribute>();
                //Debug.WriteLine("4th");
                _targetDevice.ConnectionStatusChanged -= DeviceConnectionStatusChanged;
                _targetDevice.Dispose();
                _targetDevice = null;

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

        private async void GattValueChanged(GattCharacteristic sender, GattValueChangedEventArgs e)
        {
#if DEBUG
            threatTime = new Stopwatch();
            threatTime.Start();
#endif
            CryptographicBuffer.CopyToByteArray(e.CharacteristicValue, out byte[] data);
            //if (false)
            //{
            //    var dateValue = DateTime.Today.ToString("yyyy-MM-dd");

            //    File.AppendAllText("log-" + dateValue + ".txt", "{" + string.Join(", ", data) + "}" + System.Environment.NewLine);

            //    //File.AppendAllText("logbinary.txt", newValue + System.Environment.NewLine);
            //}
            if (!Enumerable.SequenceEqual(data, olddata))
            {

                var GattData = Utilities.ParseGattValue(data, LogData, DevType);
                if ((string)GattData[0] != "Error")
                {
                    var args = new Events.DataChangedEventArgs()
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
                else
                {
                    if ((string)GattData[1] == "TypeRequest" && DevType == 4)
                    {
                        if (_useNewWriteUuid)
                            await gattRemoteCommandTargetAttribute.characteristic.WriteValueAsync(commandSpecial[0].CommandData.AsBuffer(),GattWriteOption.WriteWithoutResponse);
                        else
                            await gattRemoteCommandTargetAttribute.characteristic.WriteValueAsync(commandSpecial[0].CommandData.AsBuffer(),GattWriteOption.WriteWithResponse);
                    }
                    else if ((string)GattData[1] == "DataRequest" && DevType == 4)
                    {
                        if (_useNewWriteUuid)
                            await gattRemoteCommandTargetAttribute.characteristic.WriteValueAsync(commandSpecial[1].CommandData.AsBuffer(), GattWriteOption.WriteWithoutResponse);
                        else
                            await gattRemoteCommandTargetAttribute.characteristic.WriteValueAsync(commandSpecial[1].CommandData.AsBuffer(), GattWriteOption.WriteWithResponse);
                    }
                }
            }
#if DEBUG
            threatTime.Stop();
            if (threatTime.ElapsedMilliseconds > 0)
            {
                maxTime = (int)threatTime.ElapsedMilliseconds > maxTime ? (int)threatTime.ElapsedMilliseconds : maxTime;
                //Console.WriteLine("Elapsed={0} ms Max={1}", threatTime.ElapsedMilliseconds, maxTime);
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
            get { return _targetDevice != null && _targetDevice.ConnectionStatus == BluetoothConnectionStatus.Connected; }
        }



        /// <summary>
        /// Gets the device information for the current BLE device.
        /// </summary>
        /// <returns></returns>
        public async Task<Schema.TargetDeviceInfo> GetDeviceInfoAsync()
        {
            if (_targetDevice != null && _targetDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                var deviceInfoService = _serviceCollection.Where(a => a.Name == "DeviceInformation").FirstOrDefault();
                var deviceInfocharacteristics = await GetServiceCharacteristicsAsync(deviceInfoService);

                var batteryService = _serviceCollection.Where(a => a.Name == "Battery").FirstOrDefault();
                var batteryCharacteristics = await GetServiceCharacteristicsAsync(batteryService);
                //byte battery = await _batteryParser.ReadAsync();

                return new Schema.TargetDeviceInfo()
                {
                    DeviceId = _targetDevice.DeviceId,
                    Name = _targetDevice.Name,
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
                return new Schema.TargetDeviceInfo();
            }
        }

        public async Task<CharacteristicResult> WriteGattCommandAsync(byte[] Data)
        {
            if (_targetDevice != null && _targetDevice.ConnectionStatus == BluetoothConnectionStatus.Connected)
            {
                if (_gattRemoteCommandTargetCharacteristic.CharacteristicProperties.HasFlag(GattCharacteristicProperties.WriteWithoutResponse))
                {
                    IBuffer buffUTF8 = CryptographicBuffer.CreateFromByteArray(Data);
                    var status = await _gattRemoteCommandTargetCharacteristic.WriteValueAsync(buffUTF8);

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