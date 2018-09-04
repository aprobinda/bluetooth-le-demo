/* Using pieces of code from WindowsCommunityToolkit/Microsoft.Toolkit.Uwp.Connectivity/BluetoothLEHelper/
 * https://github.com/Microsoft/WindowsCommunityToolkit/tree/master/Microsoft.Toolkit.Uwp.Connectivity/BluetoothLEHelper
 * This is a simplified version of the BluetoothLEHelper.cs that uses only the Bluetooth LE Advertisement Watcher.
 */
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using BLEReader.Logging;
using Microsoft.Toolkit.Uwp.Connectivity;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.Core;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Windows.Foundation.Metadata;
using Windows.UI.Core;

namespace BLEReader
{
    /// <summary>
    /// Context for the entire app. This is where all app wide variables are stored
    /// </summary>
    public class BLEDeviceManager
    {
        /// <summary>
        /// Gets a value indicating whether the Bluetooth LE Helper is supported
        /// </summary>
        private static bool? _isBluetoothLESupported = null;

        /// <summary>
        /// Gets a value indicating whether the Bluetooth LE Helper is supported.
        /// </summary>
        public static bool IsBluetoothLESupported => (bool)(_isBluetoothLESupported ??
            (_isBluetoothLESupported = ApiInformation.IsApiContractPresent("Windows.Foundation.UniversalApiContract", 4)));


        /// <summary>
        /// Prevents a default instance of the <see cref="BLEDeviceManager" /> class from being created.
        /// </summary>
        static BLEDeviceManager()
        {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Init();
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }

        private static BluetoothAdapter _adapter;

        /// <summary>
        /// Initializes the app context.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        private static async Task Init()
        {
            _adapter = await BluetoothAdapter.GetDefaultAsync();
        }

        /// <summary>
        /// Gets a value indicating whether peripheral mode is supported by this device
        /// </summary>
        public bool IsPeripheralRoleSupported => _adapter.IsPeripheralRoleSupported;

        /// <summary>
        /// Gets a value indicating whether central role is supported by this device
        /// </summary>
        public bool IsCentralRoleSupported => _adapter.IsCentralRoleSupported;


        /// <summary>
        /// Gets the app context
        /// </summary>
        public static BLEDeviceManager Context { get; } = new BLEDeviceManager();


        /// <summary>
        /// Cache for advertised addresses
        /// Since trackedAddresses will be frequently checked, use HashSet because HashSet<T>.Contains(T) method is an O(1) operation 
        /// https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.hashset-1.contains?view=netframework-4.7.2#remarks
        /// </summary>
        private HashSet<ulong> trackedAddresses = new HashSet<ulong>();

        /// <summary>
        /// Reader/Writer lock for when we are updating the collection.
        /// </summary>
        private readonly ReaderWriterLockSlim _readerWriterLockSlim = new ReaderWriterLockSlim();

        /// <summary>
        /// Gets the list of the Bluetooth LE devices discovered by AdvertisementWatcher
        /// </summary>
        public ObservableCollection<BLEDevice> ActiveDevices { get; } = new ObservableCollection<BLEDevice>();


        /// <summary>
        /// Advertisement watcher used to find bluetooth devices.
        /// </summary>
        private BluetoothLEAdvertisementWatcher _advertisementWatcher;

        /// <summary>
        /// Starts AdvertisementWatcher enumeration of bluetooth device
        /// </summary>s
        public void StartEnumeration()
        {
            if (_advertisementWatcher?.Status == BluetoothLEAdvertisementWatcherStatus.Started)
            {
                return;
            }
            ActiveDevices.Clear();
            trackedAddresses.Clear();

            _advertisementWatcher = new BluetoothLEAdvertisementWatcher();
            _advertisementWatcher.Received += AdvertisementWatcher_Received;
            _advertisementWatcher.Start();
            Log.Instance.Debug("StartEnumeration()");
        }


        /// <summary>
        /// Gets a value indicating whether app is currently enumerating
        /// </summary>
        public bool IsEnumerating
        {
            get
            {
                if (_advertisementWatcher == null)
                {
                    _advertisementWatcher = new BluetoothLEAdvertisementWatcher();
                }
                return _advertisementWatcher.Status == BluetoothLEAdvertisementWatcherStatus.Started;
            }
        }


        /// <summary>
        /// Handler for advertisement received event
        /// </summary>
        /// <param name="sender">The Bluetooth LE Advertisement Watcher.</param>
        /// <param name="args">The advertisement.</param>
        private async void AdvertisementWatcher_Received(BluetoothLEAdvertisementWatcher sender, BluetoothLEAdvertisementReceivedEventArgs args)
        {
            Guid service;
            if (args.Advertisement.ServiceUuids.Count != 0)
            {
                service = args.Advertisement.ServiceUuids[0];
                // Filter based on device profile defined in MainPage.xaml.cs
                if (service.Equals(MainPage.FILTER_PROFILE))
                {
                    ulong address = args.BluetoothAddress;
                    String name = args.Advertisement.LocalName;
                    Log.Instance.Debug("AdvertisementWatcher_Received from {0}, profile: {1}",
                        args.Advertisement.LocalName, GattUuidsService.ConvertUuidToName(service));

                    if (trackedAddresses.Add(address))
                    {   // this address didn't exist in the "to be connected" list, so try to connect:
                        BluetoothLEDevice deviceFromAddress = await BluetoothLEDevice.FromBluetoothAddressAsync(address);
                        if (deviceFromAddress != null)
                        {
                            Log.Instance.Debug("1. ConnectionStatus after FromBluetoothAddressAsync: {0}", deviceFromAddress.ConnectionStatus);
                            //Log.Instance.Debug("deviceFromAddress Properties:");
                            //LogDeviceInfoProperties(deviceFromAddress);

                            #region Connect and get GattServices
                            GattDeviceServicesResult result = await deviceFromAddress.GetGattServicesAsync(BluetoothCacheMode.Uncached);
                            if (result.Status == GattCommunicationStatus.Success)
                            {// 2. At this stage, the device is connected and does not send advertisements anymore
                                Log.Instance.Debug("2. ConnectionStatus after GetGattServicesAsync: {0}", deviceFromAddress.ConnectionStatus);
                                var services = result.Services;
                                Log.Instance.Debug("Found {0} services for {1}:", services.Count, deviceFromAddress.Name);
                                foreach (var gattService in services)
                                {
                                    Log.Instance.Debug("\t {0}", GattUuidsService.ConvertUuidToName(gattService.Uuid));
                                }
                                await AddDeviceToList(deviceFromAddress);
                            }
                            else
                            {
                                Log.Instance.Debug("Device {0} is unreachable", deviceFromAddress.Name);
                            }
                            #endregion
                        }
                    }
                }
            }
        }

        private static void LogDeviceInfoProperties(BluetoothLEDevice deviceFromAddress)
        {
            foreach (var prop in deviceFromAddress.DeviceInformation.Properties)
            {
                Log.Instance.Debug("  k:{0} v:[{1}]", prop.Key, prop.Value);
            }
        }


        /// <summary>
        /// Adds the new device to the displayed list
        /// </summary>
        /// <param name="BluetoothLEDevice">The device to add</param>
        /// <returns>The task being used to add a device to a list</returns>
        private async Task AddDeviceToList(BluetoothLEDevice bleDevice)
        {
            //Log.Instance.Debug("BluetoothLEDevice.ConnectionStatus inside AddDeviceToList: {}", bleDevice.ConnectionStatus);
            // Make sure device name isn't blank or already present in the list.
            if (!string.IsNullOrEmpty(bleDevice?.Name))
            {
                var device = new BLEDevice(bleDevice);
                //Log.Instance.Debug("new BLEDevice.IsConnected: {}", device.IsConnected);
                await CoreApplication.MainView.CoreWindow.Dispatcher.RunAsync(
                    CoreDispatcherPriority.Normal,
                    () =>
                    {
                        if (_readerWriterLockSlim.TryEnterWriteLock(TimeSpan.FromSeconds(1)))
                        {
                            if (!ActiveDevices.Contains(device))
                            {
                                ActiveDevices.Add(device);
                            }
                            _readerWriterLockSlim.ExitWriteLock();
                        }
                    });
                Log.Instance.Debug("Added {0} to the list", bleDevice?.Name);
                return;
            }
        }


        /// <summary>
        /// Stops enumeration of bluetooth device
        /// </summary>
        public void StopEnumeration()
        {
            if (_advertisementWatcher != null)
            {
                _advertisementWatcher.Received -= AdvertisementWatcher_Received;
                _advertisementWatcher.Stop();
                _advertisementWatcher = null;
            }
            Log.Instance.Debug("StopEnumeration()");
        }
    }
}
