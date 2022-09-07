﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace BluetoothDMM
{
    /// <summary>
    /// Interaction logic for Devices.xaml
    /// </summary>
    public partial class Devices : Window
    {
        public Dictionary<string, string> DeviceListC { get; set; }

        public Devices(Dictionary<string, string> deviceListC)
        {
            InitializeComponent();

            //var mainWindow = Application.Current.MainWindow;
            //this.DataContext = mainWindow.DataContext;
            DeviceListC = deviceListC;
            
            this.DataContext = this;
        }

        private void Button1_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItems.Count > 0)
            {
                DeviceListC.Remove(((KeyValuePair<string, string>)listBox.SelectedValue).Key);
                listBox.SelectedIndex = -1;
                listBox.Items.Refresh();
                //Debug.WriteLine(((System.Collections.Generic.KeyValuePair<string, string>)listBox.SelectedValue).Key);
            }
        }

        private void Button2_Click(object sender, RoutedEventArgs e)
        {
            DeviceListC.Clear();
            listBox.SelectedIndex = -1;
            listBox.Items.Refresh();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (listBox.SelectedItems.Count > 0)
            {
                var a = ((KeyValuePair<string, string>)listBox.SelectedValue).Value;
                EditBox.Text = a;
                Key.Text = ((KeyValuePair<string, string>)listBox.SelectedValue).Key;
                Renamer.IsOpen = true;
            }
        }

        private void Button_Click_1(object sender, RoutedEventArgs e)
        {
            if (EditBox.Text.Length > 0)
            {
                DeviceListC[Key.Text]=EditBox.Text;
                Key.Text = "";
                listBox.Items.Refresh();
                Renamer.IsOpen = false;
            }
        }

        private void Button3_Click(object sender, RoutedEventArgs e)
        {
            if (Properties.Settings.Default.ConnectOn)
            {
                Properties.Settings.Default.DeviceID.Clear();
                foreach (KeyValuePair<string, string> items in DeviceListC)
                {
                    Properties.Settings.Default.DeviceID.Add(items.Key + "\n" + items.Value);
                }
                Properties.Settings.Default.Save();
            }
            DialogResult = true;
        }
    }

}
