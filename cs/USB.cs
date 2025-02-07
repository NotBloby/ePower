using System;
using System.Management;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;

namespace ePower.Hardware
{
    public class UsbDetector
    {
        private const int USB_DEVICE_CLASS_BATTERY = 0x01;
        private readonly ILogger _logger;
        private ManagementEventWatcher _usbWatcher;

        public event EventHandler<UsbDeviceEventArgs> DeviceConnected;
        public event EventHandler<UsbDeviceEventArgs> DeviceDisconnected;

        public UsbDetector(ILogger logger)
        {
            _logger = logger;
            InitializeUsbWatcher();
        }

        private void InitializeUsbWatcher()
        {
            _usbWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3")
            );
            _usbWatcher.EventArrived += OnUsbDeviceChanged;
            _usbWatcher.Start();
        }

        private void OnUsbDeviceChanged(object sender, EventArrivedEventArgs e)
        {
            try
            {
                string devicePath = e.NewEvent.Properties["TargetInstance"].Value.ToString();
                int eventType = Convert.ToInt32(e.NewEvent.Properties["EventType"].Value);

                UsbDevice device = ParseDeviceDetails(devicePath);
                if (device != null)
                {
                    if (eventType == 2)
                        DeviceConnected?.Invoke(this, new UsbDeviceEventArgs(device));
                    else if (eventType == 3)
                        DeviceDisconnected?.Invoke(this, new UsbDeviceEventArgs(device));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"USB device change error: {ex.Message}");
            }
        }

        public List<UsbDevice> GetConnectedPowerbanks()
        {
            List<UsbDevice> powerbanks = new List<UsbDevice>();

            using (var searcher = new ManagementObjectSearcher(
                "SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Battery'"))
            {
                foreach (ManagementObject device in searcher.Get())
                {
                    UsbDevice usbDevice = CreateUsbDeviceFromManagementObject(device);
                    if (usbDevice != null)
                        powerbanks.Add(usbDevice);
                }
            }

            return powerbanks;
        }

        private UsbDevice ParseDeviceDetails(string deviceInfo)
        {
            try
            {
                var deviceMatch = Regex.Match(deviceInfo, @"DeviceID\s*=\s*""(.+?)""");
                if (deviceMatch.Success)
                {
                    string deviceId = deviceMatch.Groups[1].Value;
                    return new UsbDevice
                    {
                        DeviceId = deviceId,
                        ConnectionTimestamp = DateTime.UtcNow
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Device parsing error: {ex.Message}");
            }

            return null;
        }

        private UsbDevice CreateUsbDeviceFromManagementObject(ManagementObject device)
        {
            try
            {
                return new UsbDevice
                {
                    DeviceId = device["DeviceID"]?.ToString(),
                    Name = device["Name"]?.ToString(),
                    Manufacturer = device["Manufacturer"]?.ToString(),
                    ConnectionTimestamp = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError($"Device creation error: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            _usbWatcher?.Stop();
            _usbWatcher?.Dispose();
        }
    }

    public class UsbDevice
    {
        public string DeviceId { get; set; }
        public string Name { get; set; }
        public string Manufacturer { get; set; }
        public DateTime ConnectionTimestamp { get; set; }
        public bool IsPowerbank => 
            !string.IsNullOrEmpty(Manufacturer) && 
            Regex.IsMatch(Manufacturer, @"(Power|Battery|Charge)", RegexOptions.IgnoreCase);
    }

    public class UsbDeviceEventArgs : EventArgs
    {
        public UsbDevice Device { get; }

        public UsbDeviceEventArgs(UsbDevice device)
        {
            Device = device;
        }
    }
}
