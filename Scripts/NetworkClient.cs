using System;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ZenTools.Postman
{
    /// <summary>
    /// Manages a TCP network connection to a server, allowing for sending and receiving messages.
    /// </summary>
    public class NetworkClient
    {
        /// <summary>
        /// Indicates whether the client is currently connected to the server.
        /// </summary>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Provides a human-readable string indicating the current connection status.
        /// </summary>
        public string ConnectionStatus { get; private set; }

        /// <summary>
        /// Event triggered when the connection status changes.
        /// </summary>
        public event EventHandler<bool> ConnectionStatusChanged;

        /// <summary>
        /// Event triggered when a message is received from the server.
        /// </summary>
        public event EventHandler<string> MessageReceived;
    
        private TcpClient _client;

        /// <summary>
        /// Asynchronously establishes a connection to a server at the specified IP address and port.
        /// </summary>
        /// <param name="serverIPAddress">The IP address of the server to connect to.</param>
        /// <param name="serverPort">The port number of the server to connect to.</param>
        /// <returns>A task that represents the asynchronous operation of connecting to the server.</returns>
        public async Task ConnectAsync(string serverIPAddress, int serverPort)
        {
            try
            {
                _client = new TcpClient();
                await _client.ConnectAsync(serverIPAddress, serverPort);
                IsConnected = true;
                UpdateConnectionStatus(IsConnected);

                // Start listening for messages from the server
                await ReceiveMessagesAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to connect to server: {ex.Message}");
                IsConnected = false;
            }
        }

        /// <summary>
        /// Asynchronously disconnects from the server and updates the connection status.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation of disconnecting from the server.</returns>
        public void DisconnectClient()
        {
            if (_client != null && IsConnected)
            {
                _client.Close();
                IsConnected = false;
                UpdateConnectionStatus(IsConnected);
            }
        }

        /// <summary>
        /// Updates the internal connection status and triggers the ConnectionStatusChanged event.
        /// </summary>
        /// <param name="isConnected">Indicates the current connection state.</param>
        private void UpdateConnectionStatus(bool isConnected)
        {
            IsConnected = isConnected;
            ConnectionStatus = isConnected ? "Connected to server." : "Disconnected from server.";
            Debug.Log(ConnectionStatus);
            ConnectionStatusChanged?.Invoke(this, IsConnected);
        }

        /// <summary>
        /// Listens for messages from the server and triggers the MessageReceived event upon receiving a message.
        /// </summary>
        /// <returns>A task that represents the asynchronous operation of receiving messages from the server.</returns>
        private async Task ReceiveMessagesAsync()
        {
            var stream = _client.GetStream();
            byte[] buffer = new byte[1024];

            try
            {
                while (IsConnected)
                {
                    int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        Debug.LogWarning("Server has closed the connection.");
                        break;
                    }
                    string receivedMessage = Encoding.ASCII.GetString(buffer, 0, bytesRead);
                    Debug.Log($"Received message from server: {receivedMessage}");
                
                    // Process the received message here
                    MessageReceived?.Invoke(this, receivedMessage);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error receiving message from server: {e.Message}");
            }
            finally
            {
                // Ensure the connection is properly closed upon losing connection
                DisconnectClient();
            }
        }
    }
}
