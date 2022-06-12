using HeartRateLE.Bluetooth.Schema;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Devices.Enumeration;

namespace HeartRateLE.Bluetooth
{
    public class HeartDeviceWatcher
    {
        // Additional properties we would like about the device.
        // Property strings are documented here https://msdn.microsoft.com/en-us/library/windows/desktop/ff521659(v=vs.85).aspx
        string[] additionalProperties =
            {
                "System.Devices.Aep.CanPair",
                "System.Devices.Aep.IsConnected",
                "System.Devices.Aep.IsPresent",
                "System.Devices.Aep.IsPaired"
            };

        private static readonly string[] RequiredServices = new string[] { "180D", "180A", "180F" };
        private DeviceWatcher _deviceWatcher;
        private List<string> _filters;

        public event EventHandler<Events.DeviceAddedEventArgs> DeviceAdded;
        protected virtual void OnDeviceAdded(Events.DeviceAddedEventArgs e)
        {
            DeviceAdded?.Invoke(this, e);
        }

        public event EventHandler<Events.DeviceUpdatedEventArgs> DeviceUpdated;
        protected virtual void OnDeviceUpdated(Events.DeviceUpdatedEventArgs e)
        {
            DeviceUpdated?.Invoke(this, e);
        }

        public event EventHandler<Events.DeviceRemovedEventArgs> DeviceRemoved;
        protected virtual void OnDeviceRemoved(Events.DeviceRemovedEventArgs e)
        {
            DeviceRemoved?.Invoke(this, e);
        }

        public event EventHandler<object> DeviceEnumerationCompleted;
        protected virtual void OnDeviceEnumerationCompleted(object obj)
        {
            DeviceEnumerationCompleted?.Invoke(this, obj);
        }

        public event EventHandler<object> DeviceEnumerationStopped;
        protected virtual void OnDeviceEnumerationStopped(object obj)
        {
            DeviceEnumerationStopped?.Invoke(this, obj);
        }

        public HeartDeviceWatcher(Schema.DeviceSelector deviceSelector)
        {
            _deviceWatcher = DeviceInformation.CreateWatcher(
                        GetSelector(deviceSelector),
                        additionalProperties,
                        DeviceInformationKind.AssociationEndpoint);

            _deviceWatcher.Added += Added;
            _deviceWatcher.Updated += Updated;
            _deviceWatcher.Removed += Removed;
            _deviceWatcher.EnumerationCompleted += EnumerationCompleted;
            _deviceWatcher.Stopped += Stopped;
        }

        public HeartDeviceWatcher(Schema.DeviceSelector deviceSelector, List<string> filters) : this(deviceSelector)
        {
            _filters = filters;
        }

        private string GetSelector(Schema.DeviceSelector deviceSelector)
        {
            switch (deviceSelector)
            {
                case Schema.DeviceSelector.BluetoothLePairedOnly:
                    return BluetoothLEDevice.GetDeviceSelectorFromPairingState(true);
                case Schema.DeviceSelector.BluetoothLeUnpairedOnly:
                    return BluetoothLEDevice.GetDeviceSelectorFromPairingState(false);
                default:
                    return "(System.Devices.Aep.ProtocolId:=\"{bb7bb05e-5972-42b5-94fc-76eaa7084d49}\")";
            }
        }

        private void Stopped(DeviceWatcher watcher, object obj)
        {
            OnDeviceEnumerationStopped(obj);
        }

        private void EnumerationCompleted(DeviceWatcher sender, object obj)
        {
            // Protect against race condition if the task runs after the app stopped the deviceWatcher.
            if (sender == _deviceWatcher)
                OnDeviceEnumerationCompleted(obj);
        }

        private async Task<bool> IsDeviceCompatible(string deviceId)
        {
            var compatibleDevice = true;
            try
            {
                //if filters were passed, check if the device name contains one of the names in the list
                if (_filters != null)
                {
                    using (var device = await BluetoothLEDevice.FromIdAsync(deviceId))
                    {
                        compatibleDevice = _filters.Any(a => device.Name.CaseInsensitiveContains(a));
                    }                            
                }
            }
            catch
            {
                compatibleDevice = false;
            }

            return compatibleDevice;
        }

        private async void Added(DeviceWatcher sender, DeviceInformation deviceInformation)
        {
            if (await IsDeviceCompatible(deviceInformation.Id))
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == _deviceWatcher)
                {
                    var args = new Events.DeviceAddedEventArgs()
                    {
                        Device = new Schema.WatcherDevice()
                        {
                            Id = deviceInformation.Id,
                            IsDefault = deviceInformation.IsDefault,
                            IsEnabled = deviceInformation.IsEnabled,
                            Name = (deviceInformation.Name=="" ? "Unknown Device " : deviceInformation.Name),
                            MacAdr = deviceInformation.Id.Substring(deviceInformation.Id.Length-17, 17).ToUpper(),
                            IsPaired = deviceInformation.Pairing.IsPaired,
                            Kind = deviceInformation.Kind.ToString(),
                            Properties = deviceInformation.Properties.ToDictionary(pair => pair.Key, pair => pair.Value)
                        }
                    };

                    OnDeviceAdded(args);
                }
            }
        }

        private async void Updated(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
            if (await IsDeviceCompatible(deviceInformationUpdate.Id))
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == _deviceWatcher)
                {
                    var args = new Events.DeviceUpdatedEventArgs()
                    {
                        Device = new Schema.WatcherDevice()
                        {
                            Id = deviceInformationUpdate.Id,
                            Kind = deviceInformationUpdate.Kind.ToString(),
                            Properties = deviceInformationUpdate.Properties.ToDictionary(pair => pair.Key, pair => pair.Value)
                        }
                    };

                    OnDeviceUpdated(args);
                }
            }
        }

        private async void Removed(DeviceWatcher sender, DeviceInformationUpdate deviceInformationUpdate)
        {
            if (await IsDeviceCompatible(deviceInformationUpdate.Id))
            {
                // Protect against race condition if the task runs after the app stopped the deviceWatcher.
                if (sender == _deviceWatcher)
                {
                    var args = new Events.DeviceRemovedEventArgs()
                    {
                        Device = new Schema.WatcherDevice()
                        {
                            Id = deviceInformationUpdate.Id,
                            Kind = deviceInformationUpdate.Kind.ToString(),
                            Properties = deviceInformationUpdate.Properties.ToDictionary(pair => pair.Key, pair => pair.Value)
                        }
                    };

                    OnDeviceRemoved(args);
                }
            }
        }

        public void Start()
        {
            _deviceWatcher.Start();
        }

        public void Stop()
        {
            if (_deviceWatcher != null)
            {
                // Unregister the event handlers.
                _deviceWatcher.Added -= Added;
                _deviceWatcher.Updated -= Updated;
                _deviceWatcher.Removed -= Removed;
                _deviceWatcher.EnumerationCompleted -= EnumerationCompleted;
                _deviceWatcher.Stopped -= Stopped;

                // Stop the watcher.
                _deviceWatcher.Stop();
                _deviceWatcher = null;
            }
        }
    }
}

