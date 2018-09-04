/* Using pieces of code from WindowsCommunityToolkit/Microsoft.Toolkit.Uwp.Connectivity/BluetoothLEHelper/
 * https://github.com/Microsoft/WindowsCommunityToolkit/tree/master/Microsoft.Toolkit.Uwp.Connectivity/BluetoothLEHelper
 */
using System;
using System.ComponentModel;
using Windows.UI.Core;

using Windows.Devices.Bluetooth;
using BLEReader.Logging;
using Windows.ApplicationModel.Core;

namespace BLEReader
{
    /// <summary>
    ///     Display class used to represent a BluetoothLEDevice in the Device list
    /// </summary>
    public class BLEDevice : INotifyPropertyChanged
    {
        public BLEDevice(BluetoothLEDevice bluetoothLeDevice)
        {
            BluetoothLEDevice = bluetoothLeDevice;
            IsConnected = BluetoothLEDevice.ConnectionStatus == BluetoothConnectionStatus.Connected;
            //Log.Instance.Debug("BLEDevice constructor. ConnectionStatus {0}", _bluetoothLeDevice.ConnectionStatus);

            BluetoothLEDevice.ConnectionStatusChanged += BluetoothLEDevice_ConnectionStatusChanged;
        }

        private BluetoothLEDevice _bluetoothLeDevice;
        /// <summary>
        /// Gets the bluetooth device this class wraps
        /// </summary>
        /// <value>The bluetooth le device.</value>
        public BluetoothLEDevice BluetoothLEDevice
        {
            get
            {
                return _bluetoothLeDevice;
            }

            private set
            {
                _bluetoothLeDevice = value;
                OnPropertyChanged();
            }
        }
        public string Name => _bluetoothLeDevice.Name;

        private bool _isConnected;
        /// <summary>
        /// Gets a value indicating whether this device is connected
        /// </summary>
        /// <value><c>true</c> if this instance is connected; otherwise, <c>false</c>.</value>
        public bool IsConnected
        {
            get
            {
                return _isConnected;
            }

            private set
            {
                if (_isConnected != value)
                {
                    _isConnected = value;
                    OnPropertyChanged();
                }
            }
        }


        /// <summary>
        /// Executes when the connection state changes
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="args">The arguments.</param>
        private async void BluetoothLEDevice_ConnectionStatusChanged(BluetoothLEDevice sender, object args)
        {
            await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                CoreDispatcherPriority.Normal,
                () =>
                {
                    IsConnected = (BluetoothLEDevice.ConnectionStatus == BluetoothConnectionStatus.Connected);
                });
        }


        /// <summary>
        /// Event to notify when this object has changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;


        protected void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string propertyName = null)
        {
            //Log.Instance.Debug("OnPropertyChanged {0}", propertyName);
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

    }
}
