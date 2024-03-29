using System;
using System.Collections.Concurrent;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace ZenTools.Postman
{
    /// <summary>
    /// Represents a TCP server that can accept connections from multiple clients, send messages, and broadcast messages to all connected clients.
    /// </summary>
    public class NetworkServer
    {
        /// <summary>
        /// The IP address on which the server listens for incoming connections.
        /// </summary>
        public string IPAddress { get; private set; }

        /// <summary>
        /// The port number on which the server listens for incoming connections.
        /// </summary>
        public int PortNumber { get; private set; }

        /// <summary>
        /// Status message indicating the server's current connection status.
        /// </summary>
        public string ServerStatus { get; private set; }

        /// <summary>
        /// Event triggered when the server starts or stops, indicating the server's running status.
        /// </summary>
        public event EventHandler<bool> OnServerStatusChanged;

        // Default IP address and port number
        private string defaultIPAddress = "127.0.0.1";
        private int defaultPortNumber = 8888;

        // Indicates whether the server is running
        private bool _isRunning = false;

        // Listener for incoming connections
        private TcpListener _listener;

        // Dictionary of connected clients
        private ConcurrentDictionary<Guid, TcpClient> _clients = new ConcurrentDictionary<Guid, TcpClient>();

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkServer"/> class with default settings.
        /// </summary>
        public NetworkServer()
        {
            IPAddress = defaultIPAddress; // set to default IP address
            PortNumber = defaultPortNumber; // set to default port number

            Debug.Log($"Server configured with default IP address ({IPAddress}) and port number ({PortNumber}).");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="NetworkServer"/> class with specified IP address and port number.
        /// </summary>
        /// <param name="ipAddress">The IP address the server will listen on.</param>
        /// <param name="portNumber">The port number the server will listen on.</param>
        public NetworkServer(string ipAddress, int portNumber)
        {
            if (ValidateIPAddress(ipAddress))
            {
                IPAddress = ipAddress;
            }
            else
            {
                Debug.LogError("The server IP address is not configured correctly. Setting default configuration.");
                IPAddress = defaultIPAddress;
            }

            if (ValidatePortNumber(portNumber))
            {
                PortNumber = portNumber;
            }
            else
            {
                Debug.LogError("The server port number is not configured correctly. Setting default configuration.");
                PortNumber = defaultPortNumber;
            }

            Debug.Log($"Server configured with IP address {IPAddress} and port number {PortNumber}.");
        }

        /// <summary>
        /// Validates the provided IP address.
        /// </summary>
        /// <param name="ipAddress">The IP address to validate.</param>
        /// <returns>True if the IP address is valid; otherwise, false.</returns>
        private bool ValidateIPAddress(string ipAddress)
        {
            // Validate the IP address
            if (!string.IsNullOrEmpty(ipAddress))
            {
                var match = Regex.Match(ipAddress, @"\b(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})\b");

                if (match.Success)
                {
                    return true;
                }
                else
                {
                    Debug.LogErrorFormat("{0} is not a valid IP Address", ipAddress);
                    return false;
                }
            }
            else
            {
                Debug.LogError("The supplied IP address is null or empty.");
                return false;
            }
        }

        /// <summary>
        /// Validates the provided port number.
        /// </summary>
        /// <param name="portNumber">The port number to validate.</param>
        /// <returns>True if the port number is valid; otherwise, false.</returns>
        private bool ValidatePortNumber(int portNumber)
        {
            // Validate the port number
            if (portNumber >= 0 && portNumber <= 65535)
            {
                return true;
            }
            else
            {
                Debug.LogErrorFormat("The server port number {0} is not a valid value.", portNumber);
                return false;
            }
        }

        /// <summary>
        /// Starts the server asynchronously and begins listening for incoming connections.
        /// </summary>
        public async void StartServerAsync()
        {
            try 
            {
                _listener = new TcpListener(System.Net.IPAddress.Parse(IPAddress), PortNumber);
                _listener.Start();

                UpdateServerStatus(true);

                while (_isRunning)
                {
                    var client = await _listener.AcceptTcpClientAsync();
                    var clientID = Guid.NewGuid();
                    var ipAddress = client.Client.RemoteEndPoint.ToString();
                    _clients.TryAdd(clientID, client);
                    Debug.Log($"Client {clientID} connected at {ipAddress}.");
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Error starting server: " + e.Message);
                _isRunning = false;
            }
        }

        /// <summary>
        /// Stops the server and disconnects all currently connected clients.
        /// </summary>
        public void StopServer()
        {
            DisconnectAllClients();
            ShutdownServer();
        }

        /// <summary>
        /// Updates the server's running status and logs the current state.
        /// </summary>
        /// <param name="isRunning">Indicates whether the server is running.</param>
        private void UpdateServerStatus(bool isRunning)
        {
            _isRunning = isRunning;
            ServerStatus = isRunning ? "Server started." : "Server stopped.";
            Debug.Log(ServerStatus);
            OnServerStatusChanged?.Invoke(this, _isRunning);
        }

        /// <summary>
        /// Sends a message asynchronously to a specific client identified by their unique identifier.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client to send the message to.</param>
        /// <param name="message">The message to send.</param>
        public async void SendMessageAsync(Guid clientId, string message)
        {
            if (!_clients.TryGetValue(clientId, out TcpClient client) || !client.Connected)
            {
                Debug.LogWarning($"Client not found or not connected: {clientId}");
                // Attempt to close if the client object is still in the dictionary but not connected
                client?.Close();
                _clients.TryRemove(clientId, out _); // Remove the client from the dictionary
                return;
            }

            try
            {
                var stream = client.GetStream();
                byte[] data = Encoding.ASCII.GetBytes(message);
                await stream.WriteAsync(data, 0, data.Length);
                Debug.Log($"Sent message to client {clientId}: {message}");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending message to client {clientId}: {e.Message}");
                DisconnectClient(clientId);
            }
        }

        /// <summary>
        /// Broadcasts a message to all connected clients.
        /// </summary>
        /// <param name="message">The message to broadcast.</param>
        public void BroadcastMessageAsync(string message)
        {
            foreach (var client in _clients)
            {
                SendMessageAsync(client.Key, message);
            }

            Debug.Log($"Broadcasted message to all clients: {message}");
        }

        /// <summary>
        /// Disconnects all clients currently connected to the server.
        /// </summary>
        private void DisconnectAllClients()
        {
            Debug.Log("Disconnecting clients...");
        
            foreach (var client in _clients)
            {
                DisconnectClient(client.Key);
            }
        
            Debug.Log("All clients disconnected.");
        }

        /// <summary>
        /// Disconnects a specific client identified by their unique identifier.
        /// </summary>
        /// <param name="clientId">The unique identifier of the client to disconnect.</param>
        private void DisconnectClient(Guid clientId)
        {
            if (_clients.TryGetValue(clientId, out var client))
            {
                client.Close();
                _clients.TryRemove(clientId, out _);
                Debug.Log($"Client disconnected: {clientId}");
            }
            else
            {
                Debug.LogWarning($"Client not found: {clientId}");
            }
        }

        /// <summary>
        /// Shuts down the server and releases all resources.
        /// </summary>
        private void ShutdownServer()
        {
            if (_listener != null)
            {
                _listener.Stop();
                _listener = null;
            }

            UpdateServerStatus(false);
        }
    }
}
