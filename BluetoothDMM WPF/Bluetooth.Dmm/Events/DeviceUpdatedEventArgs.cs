﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BluetoothDLL.Bluetooth.Events
{
    public class DeviceUpdatedEventArgs
    {
        public Schema.WatcherDevice Device { get; set; }
    }
}
