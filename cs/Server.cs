using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Text.Json;
using System.Security.Cryptography;
using System.Diagnostics;
using System.Collections.Generic;
using System.Linq;
using System.IO;

namespace PowerbankMonitor.Network
{
    public class PowerbankWebSocketServer
    {
        private readonly HttpListener _listener;
        private readonly int _port;
        private readonly CancellationTokenSource _cancellationTokenSource;
        private readonly ConcurrentDictionary<string, WebSocketConnection> _activeConnections;
        private readonly ILogger _logger;
        private readonly SecurityManager _securityManager;
        private readonly MetricsTracker _metricsTracker;
        private readonly MessageHandler _messageHandler;
        private readonly ConnectionPool _connectionPool;
        private readonly ConfigurationManager _configManager;
        private readonly List<IPAddress> _allowedIpAddresses;
        private readonly AuthenticationService _authService;
        private readonly MessageEncryptionService _encryptionService;
        private readonly DeviceRegistry _deviceRegistry;

        public PowerbankWebSocketServer(
            int port, 
            ILogger logger, 
            SecurityManager securityManager, 
            ConfigurationManager configManager,
            AuthenticationService authService,
            MessageEncryptionService encryptionService)
        {
            _port = port;
            _listener = new HttpListener();
            _listener.Prefixes.Add($"http://localhost:{port}/");
            _cancellationTokenSource = new CancellationTokenSource();
            _activeConnections = new ConcurrentDictionary<string, WebSocketConnection>();
            _logger = logger;
            _securityManager = securityManager;
            _configManager = configManager;
            _metricsTracker = new MetricsTracker();
            _connectionPool = new ConnectionPool(_configManager.MaxConnections);
            _authService = authService;
            _encryptionService = encryptionService;
            _deviceRegistry = new DeviceRegistry();
            _allowedIpAddresses = LoadAllowedIpAddresses();
        }

        private List<IPAddress> LoadAllowedIpAddresses()
        {
            return _configManager.GetAllowedIpAddresses()
                .Select(IPAddress.Parse)
                .ToList();
        }

        public async Task StartAsync()
        {
            try
            {
                _listener.Start();
                _logger.LogInfo($"WebSocket server starting on port {_port}");

                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    HttpListenerContext context = await _listener.GetContextAsync();
                    
                    if (!IsIpAllowed(context.Request.RemoteEndPoint.Address))
                    {
                        context.Response.StatusCode = 403;
                        context.Response.Close();
                        continue;
                    }

                    if (context.Request.IsWebSocketRequest)
                    {
                        await ProcessWebSocketRequest(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 400;
                        context.Response.Close();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Server startup failed: {ex.Message}");
            }
        }

        private bool IsIpAllowed(IPAddress clientIp)
        {
            return _allowedIpAddresses.Contains(clientIp) || 
                   _allowedIpAddresses.Count == 0;
        }

        private async Task ProcessWebSocketRequest(HttpListenerContext context)
        {
            HttpListenerWebSocketContext webSocketContext = null;
            try
            {
                if (!_connectionPool.TryAcquireConnection())
                {
                    context.Response.StatusCode = 503;
                    context.Response.Close();
                    return;
                }

                webSocketContext = await context.AcceptWebSocketAsync(subProtocol: null);
                WebSocket webSocket = webSocketContext.WebSocket;

                string connectionId = GenerateUniqueConnectionId();
                var connection = new WebSocketConnection(
                    connectionId, 
                    webSocket, 
                    _securityManager,
                    _encryptionService
                );

                if (_activeConnections.TryAdd(connectionId, connection))
                {
                    _logger.LogInfo($"New WebSocket connection established: {connectionId}");
                    _metricsTracker.IncrementTotalConnections();
                    await HandleWebSocketConnection(connection);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"WebSocket connection error: {ex.Message}");
                context.Response.StatusCode = 500;
                context.Response.Close();
            }
            finally
            {
                _connectionPool.ReleaseConnection();
            }
        }

        private async Task HandleWebSocketConnection(WebSocketConnection connection)
        {
            try
            {
                byte[] buffer = new byte[1024 * 4];
                while (connection.WebSocket.State == WebSocketState.Open)
                {
                    WebSocketReceiveResult result = await connection.WebSocket.ReceiveAsync(
                        new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                        await _messageHandler.ProcessMessageAsync(connection, message);
                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await connection.WebSocket.CloseAsync(
                            WebSocketCloseStatus.NormalClosure, 
                            string.Empty, 
                            CancellationToken.None);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"Connection handling error: {ex.Message}");
            }
            finally
            {
                _activeConnections.TryRemove(connection.ConnectionId, out _);
                _metricsTracker.DecrementTotalConnections();
            }
        }

        public async Task BroadcastMessageAsync(string message)
        {
            var tasks = _activeConnections.Values.Select(async connection => {
                try
                {
                    await connection.SendMessageAsync(message);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"Broadcast error: {ex.Message}");
                }
            });

            await Task.WhenAll(tasks);
        }

        private string GenerateUniqueConnectionId()
        {
            using (var rng = new RNGCryptoServiceProvider())
            {
                byte[] tokenData = new byte[16];
                rng.GetBytes(tokenData);
                return Convert.ToBase64String(tokenData);
            }
        }

        public void Stop()
        {
            _cancellationTokenSource.Cancel();
            _listener.Stop();
            _logger.LogInfo("WebSocket server stopped");
        }
    }

    public class ConnectionPool
    {
        private readonly SemaphoreSlim _connectionSemaphore;

        public ConnectionPool(int maxConnections)
        {
            _connectionSemaphore = new SemaphoreSlim(maxConnections);
        }

        public bool TryAcquireConnection()
        {
            return _connectionSemaphore.Wait(TimeSpan.FromSeconds(5));
        }

        public void ReleaseConnection()
        {
            _connectionSemaphore.Release();
        }
    }

    public class DeviceRegistry
    {
        private readonly ConcurrentDictionary<string, DeviceInfo> _registeredDevices;

        public DeviceRegistry()
        {
            _registeredDevices = new ConcurrentDictionary<string, DeviceInfo>();
        }

        public void RegisterDevice(DeviceInfo deviceInfo)
        {
            _registeredDevices[deviceInfo.DeviceId] = deviceInfo;
        }

        public DeviceInfo GetDeviceInfo(string deviceId)
        {
            return _registeredDevices.TryGetValue(deviceId, out var deviceInfo) 
                ? deviceInfo 
                : null;
        }
    }

    public class DeviceInfo
    {
        public string DeviceId { get; set; }
        public string DeviceType { get; set; }
        public DateTime LastConnectionTime { get; set; }
    }
}
