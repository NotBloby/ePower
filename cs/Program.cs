using System;
using System.Collections.Generic;
using System.Management;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.IO;
using WebSocketSharp.Server;

namespace MonitorW
{
    public class Powerbank
    {
        private static readonly List<PowerbankDevice> _connectedDevices = new List<PowerbankDevice>();
        private static WebSocketServer _webSocketServer;
        private static CancellationTokenSource _cancellationTokenSource;
        private readonly ILogger _logger;
        private readonly ConfigurationManager _configManager;

        public Powerbank(ILogger logger, ConfigurationManager configManager)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _configManager = configManager ?? throw new ArgumentNullException(nameof(configManager));
        }

        private void USB()
        {
            var usbWatcher = new ManagementEventWatcher(
                new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3")   
            );

            usbWatcher.EventArrived += (sender, e) => {
                DetectAndProcessPowerbanks();
            };

            usbWatcher.start();
        }

        private void DetectAndProcessPowerbanks()
        {
            using (var searcher = new ManagementObjectSearch("SELECT * FROM Win32_PnPEntity WHERE PNPClass = 'Battery'"))
            {
                foreach (ManagementObject device in searcher.Get())
                {
                    var powerbank = new PowerbankDevice(device);
                    if (!_connectedDevices.Contains(powerbank))
                    {
                        _connectedDevices.Add(powerbank);
                        InitializePowerbank(powerbank);
                    }
                }
            }
        }

        private void InitializePowerbank(PowerbankDevice powerbank)
        {
            Task.Run(() => {
                while (powerbank.IsConnected)
                {
                    Update(powerbank);
                    Thread.Sleep(_configManager.MonitoringInterval);
                }
            });
        }

        private void Update(Powerbank powerbank)
        {
            try
            {
                powerbank.RefreshStatus();
                Broadcast(powerbank);
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error updating powerbank: {ex.Message}");
            }
        }

        private void Broadcast(PowerbankDevice powerbank)
        {
            var statusMessage = JsonSerializer.Serialize(new {
                DeviceId = powerbank.DeviceId,
                BatteryLevel = powerbank.BatteryPercentage,
                ChargingStatus = powerbank.ChargingStatus,
                Timestamp = DateTime.UtcNow
            });

            _webSocketServer.Broadcast(statusMessage);
        }

        public void Start()
        {
            try
            {
                InitializeWebSocketServer();
                StartDevice();
                _logger.LogInfo("PowerbankMonitor started successfully");
            }
            catch(Exception ex)
            {
                _logger.LogCritical($"Startup failed: {ex.Message}");
                throw;
            }
        }

           private void InitializeWebSocketServer()
        {
            _webSocketServer = new WebSocketServer(_configManager.WebSocketPort);
            _webSocketServer.Start();
            _logger.LogInfo($"WebSocket server running on port {_configManager.WebSocketPort}");
        }

        public static void Main(string[] args)
        {
            var logger = new FileLogger("powerbank_monitor.log");
            var configManager = new ConfigurationManager("config.json");

            var app = new PowerbankMonitorApp(logger, configManager);
            app.Start();

            Console.WriteLine("PowerbankMonitor is running. Press Ctrl+C to exit.");
            Console.ReadLine();
        }
    }

    public class PowerbankDevice { }
    public class ConfigurationManager { }
    public interface ILogger { }
    public class FileLogger : ILogger { }
}
