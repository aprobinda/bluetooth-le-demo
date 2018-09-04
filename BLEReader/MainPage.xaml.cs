using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;
using BLEReader.Logging;
using Microsoft.Toolkit.Uwp.Connectivity;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


// The Blank Page item template is documented at https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x409

namespace BLEReader
{
    /// <summary>
    /// An empty page that can be used on its own or navigated to within a Frame.
    /// </summary>
    public sealed partial class MainPage : Page
    {
        // Get a local copy of the context for easier reading
        BLEDeviceManager DeviceManager = BLEDeviceManager.Context;

        public static Guid FILTER_PROFILE => GattServiceUuids.BloodPressure;

        public MainPage()
        {
            this.InitializeComponent();
            Log.Instance.Debug("App has started");

            // check if BluetoothLE APIs are available
            if (BLEDeviceManager.IsBluetoothLESupported)
            {
                //DeviceManager.StartEnumeration();
                EnumerateButton_Click();
            }
        }


        private void EnumerateButton_Click()
        {
            if (DeviceManager.IsEnumerating)
            {
                DeviceManager.StopEnumeration();
                EnumerateButton.Content = "Start enumerating";
            }
            else
            {
                DeviceManager.StartEnumeration();
                EnumerateButton.Content = "Stop enumerating";
            }
        }
    }
}
