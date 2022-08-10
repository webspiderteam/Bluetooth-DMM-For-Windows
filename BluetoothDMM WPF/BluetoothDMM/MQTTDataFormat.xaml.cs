using System;
using System.Linq;
using System.Windows;
using static BluetoothDMM.GattData;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for MQTTDataFormat.xaml
    /// </summary>
    public partial class MQTTDataFormat : Window
    {
        SelectedDataList selectedDataList = null;
        public MQTTDataFormat()
        {
            InitializeComponent();
            DataContext = this;

            selectedDataList = listBox1.ItemsSource as SelectedDataList;
            DataList DataListItems = listBox.ItemsSource as DataList;
            foreach (var item in Properties.MQTT.Default.SelectedDataList)
            {
                string[] row = item.Split(new string[] { "\n"}, StringSplitOptions.RemoveEmptyEntries);
                var xb = DataListItems.Single(x => x.Key == row[0]);
                //var selected = listBox.FindName(row[0]) as MQTT_DataFormat;
                selectedDataList.Add(xb);
            }
            selectedDataList.CollectionChanged -= SelectedDataList_CollectionChanged;
            selectedDataList.CollectionChanged += SelectedDataList_CollectionChanged;
            SelectedDataList_CollectionChanged(null, null);
            chkCleanWhitespace.IsChecked = Properties.MQTT.Default.CleanWhitespace;
            chkUseComa.IsChecked= Properties.MQTT.Default.UseComa;
        }

        private void SelectedDataList_CollectionChanged(object sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            string sample = "{";
            bool first = true;
            foreach (var item in selectedDataList)
            {
                sample += (first ? "" : ", ") + (Properties.MQTT.Default.UseComa? item.KeyPreview.Replace('.',','):item.KeyPreview);
                first = false;
            }
            sample += "}";
            txtSampleOutput.Text = sample;
        }

        private void btnAdd_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItem!=null)
            {
                var selected = listBox.SelectedItem as MQTT_DataFormat;
                selectedDataList.Add(selected);
            }
        }

        private void btnUp_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = listBox1.SelectedIndex;

            if (selectedIndex > 0)
            {
                var itemToMoveUp = selectedDataList[selectedIndex];
                selectedDataList.RemoveAt(selectedIndex);
                selectedDataList.Insert(selectedIndex - 1, itemToMoveUp);
                listBox1.SelectedIndex = selectedIndex - 1;
            }
        }

        private void btnDown_Click(object sender, RoutedEventArgs e)
        {
            var selectedIndex = listBox1.SelectedIndex;
            if (selectedIndex + 1 < selectedDataList.Count)
            {
                var itemToMoveDown = selectedDataList[selectedIndex];
                selectedDataList.RemoveAt(selectedIndex);
                selectedDataList.Insert(selectedIndex + 1, itemToMoveDown);
                listBox1.SelectedIndex = selectedIndex + 1;
            }
        }

        private void btnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (listBox1.SelectedItem!=null)
            {
                var selected = listBox1.SelectedItem as MQTT_DataFormat;
                selectedDataList.Remove(selected);
            }
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            Properties.MQTT.Default.SelectedDataList.Clear();
            foreach (var item in selectedDataList)
            {
                Properties.MQTT.Default.SelectedDataList.Add(item.Key + "\n" + item.Value);
            }
            Properties.MQTT.Default.CleanWhitespace = (bool)chkCleanWhitespace.IsChecked;
            Properties.MQTT.Default.UseComa = (bool)chkUseComa.IsChecked;
            Properties.MQTT.Default.Save();
            DialogResult = true;
        }

        private void chkUseComa_Unchecked(object sender, RoutedEventArgs e)
        {
            Properties.MQTT.Default.UseComa = (bool)chkUseComa.IsChecked;
            SelectedDataList_CollectionChanged(null, null);
        }
    }
}
