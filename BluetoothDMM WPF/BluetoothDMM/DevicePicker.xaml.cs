using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using HeartRateLE.Bluetooth.Schema;
using HeartRateLE.Bluetooth;

namespace HeartRateLE.UI
{
    /// <summary>
    /// Interaction logic for DeviceWatcher.xaml
    /// </summary>
    public partial class DevicePicker : Window
    {
        public ObservableCollection<WatcherDevice> UnpairedCollection
        {
            get;
            private set;
        }

        public ObservableCollection<WatcherDevice> PairedCollection
        {
            get;
            private set;
        }

        public string SelectedDeviceId { get; set; }
        public string SelectedDeviceName { get; set; }

        private HeartRateLE.Bluetooth.HeartDeviceWatcher _unpairedWatcher;
        private HeartRateLE.Bluetooth.HeartDeviceWatcher _pairedWatcher;

        public DevicePicker()
        {
            InitializeComponent();

            UnpairedCollection = new ObservableCollection<WatcherDevice>();
            PairedCollection = new ObservableCollection<WatcherDevice>();
            this.DataContext = this;

            _unpairedWatcher = new HeartRateLE.Bluetooth.HeartDeviceWatcher(DeviceSelector.BluetoothLeUnpairedOnly);
            _unpairedWatcher.DeviceAdded += OnDeviceAdded;
            _unpairedWatcher.DeviceRemoved += OnDeviceRemoved;
            _unpairedWatcher.Start();

            _pairedWatcher = new HeartRateLE.Bluetooth.HeartDeviceWatcher(DeviceSelector.BluetoothLePairedOnly);
            _pairedWatcher.DeviceAdded += OnPaired_DeviceAdded;
            _pairedWatcher.DeviceRemoved += OnPaired_DeviceRemoved;

            _pairedWatcher.Start();
            SelectedDeviceId = string.Empty;
            SelectedDeviceName = string.Empty;
        }

        private async void OnPaired_DeviceRemoved(object sender, HeartRateLE.Bluetooth.Events.DeviceRemovedEventArgs e)
        {
            await RunOnUiThread(() =>
            {
                var foundItem = PairedCollection.FirstOrDefault(a => a.Id == e.Device.Id);
                if (foundItem != null)
                    PairedCollection.Remove(foundItem);
                Debug.WriteLine("Paired device Removed: " + e.Device.Id);
            });
        }

        private async void OnPaired_DeviceAdded(object sender, HeartRateLE.Bluetooth.Events.DeviceAddedEventArgs e)
        {
            await RunOnUiThread(() =>
            {
                PairedCollection.Add(e.Device);
                Debug.WriteLine("Paired Device Added: " + e.Device.Id);
            });
        }

        private async void OnDeviceRemoved(object sender, HeartRateLE.Bluetooth.Events.DeviceRemovedEventArgs e)
        {
            await RunOnUiThread(() =>
            {
                var foundItem = UnpairedCollection.FirstOrDefault(a => a.Id == e.Device.Id);
                if (foundItem != null)
                    UnpairedCollection.Remove(foundItem);
                Debug.WriteLine("Unpaired Device Removed: " + e.Device.Id);
            });
        }

        private async void OnDeviceAdded(object sender, HeartRateLE.Bluetooth.Events.DeviceAddedEventArgs e)
        {
            await RunOnUiThread(() =>
            {
                UnpairedCollection.Add(e.Device);
                Debug.WriteLine("Unpaired Device Added: " + e.Device.Id);
            });
        }

        protected override void OnClosing(CancelEventArgs e)
        {
            base.OnClosing(e);
            _unpairedWatcher.Stop();
            _pairedWatcher.Stop();
        }

        private async Task RunOnUiThread(Action a)
        {
            await this.Dispatcher.InvokeAsync(() =>
            {
                a();
            });
        }

        private async void PairDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (WatcherDevice)unpairedListView.SelectedItem;
            if (selectedItem != null)
            {
                var result = await PairingHelper.PairDeviceAsync(selectedItem.Id);
                MessageBox.Show(result.Status);
            }
            else
            {
                MessageBox.Show("Must select an unpaired device");
            }
        }

        private async void UnpairDeviceButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (WatcherDevice)pairedListView.SelectedItem;
            if (selectedItem != null)
            {
                var result = await PairingHelper.UnpairDeviceAsync(selectedItem.Id);
                MessageBox.Show(result.Status);
            }
            else
            {
                MessageBox.Show("Must select an paired device");
            }
        }

        private void OkButton_Click(object sender, RoutedEventArgs e)
        {
            var selectedItem = (WatcherDevice)unpairedListView.SelectedItem;
            //var selectedItem = (WatcherDevice)pairedListView.SelectedItem;
            if (selectedItem != null)
            {
                SelectedDeviceId = selectedItem.Id;
                SelectedDeviceName = selectedItem.Name;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("Must select an paired device");
            }
        }
    }
}
