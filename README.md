# bluetooth-le-demo
A quick demo that show how to discover nearby Bluetooth Low Energy devices using [Windows Runtime API](https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth) that allows UWP app and desktop apps to interact with Bluetooth devices.

Specifically, the demo shows that [FromBluetoothAddressAsync method](
https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice.frombluetoothaddressasync) **will not initiate a connection**, as one would expect after reading the documentation.

Use [GetGattServicesAsync](https://docs.microsoft.com/en-us/uwp/api/windows.devices.bluetooth.bluetoothledevice.getgattservicesasync) method instead.
