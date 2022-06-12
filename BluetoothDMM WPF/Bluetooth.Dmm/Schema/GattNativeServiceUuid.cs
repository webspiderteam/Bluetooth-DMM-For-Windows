namespace HeartRateLE.Bluetooth.Schema
{
    /// <summary>
    ///     This enum assists in finding a string representation of a BT SIG assigned value for Service UUIDS
    ///     Reference: https://developer.bluetooth.org/gatt/services/Pages/ServicesHome.aspx
    /// </summary>
    public enum GattNativeServiceUuid : ushort
    {
        None = 0,
        AlertNotification = 0x1811,
        Battery = 0x180F,
        BloodPressure = 0x1810,
        CurrentTimeService = 0x1805,
        CyclingSpeedandCadence = 0x1816,
        DeviceInformation = 0x180A,
        GenericAccess = 0x1800,
        GenericAttribute = 0x1801,
        Glucose = 0x1808,
        HealthThermometer = 0x1809,
        HeartRate = 0x180D,
        HumanInterfaceDevice = 0x1812,
        ImmediateAlert = 0x1802,
        LinkLoss = 0x1803,
        NextDSTChange = 0x1807,
        PhoneAlertStatus = 0x180E,
        ReferenceTimeUpdateService = 0x1806,
        RunningSpeedandCadence = 0x1814,
        ScanParameters = 0x1813,
        TxPower = 0x1804,
        SimpleKeyService = 0xFFE0
    }
}
