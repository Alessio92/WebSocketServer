/*
 * Filename:  WebSocketClient.cs
 * Author:    Alessio Carello
 * Last Update:    20/10/2018 21.41.38
 */

namespace WebSocketServer
{
    using global::WebSocketServer.EventArgs;
    using System;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="WebSocketClient" />
    /// </summary>
    public class WebSocketClient : IDisposable
    {
        /// <summary>
        /// Defines the m_client
        /// </summary>
        private TcpClient m_client;

        /// <summary>
        /// Defines the Stream
        /// </summary>
        public NetworkStream Stream;

        /// <summary>
        /// Gets the Id
        /// </summary>
        public Guid Id { get; private set; }

        /// <summary>
        /// Defines the Handshake
        /// </summary>
        public event HandshakeHandler Handshake;

        /// <summary>
        /// The HandshakeHandler
        /// </summary>
        /// <param name="sender">The sender<see cref="WebSocketClient"/></param>
        /// <param name="e">The e<see cref="WebSocketHandshakeEventArgs"/></param>
        public delegate void HandshakeHandler(WebSocketClient sender, WebSocketHandshakeEventArgs e);

        /// <summary>
        /// Defines the Message
        /// </summary>
        public event MessageHandler Message;

        /// <summary>
        /// The MessageHandler
        /// </summary>
        /// <param name="sender">The sender<see cref="WebSocketClient"/></param>
        /// <param name="e">The e<see cref="WebSocketDataEventArgs"/></param>
        public delegate void MessageHandler(WebSocketClient sender, WebSocketDataEventArgs e);

        /// <summary>
        /// Defines the OnStream
        /// </summary>
        public EventHandler OnStream;

        /// <summary>
        /// Gets the RemoteEndPoint
        /// </summary>
        public string RemoteEndPoint
        {
            get
            {
                return m_client.Client.RemoteEndPoint.ToString();
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketClient"/> class.
        /// </summary>
        /// <param name="client">The client<see cref="TcpClient"/></param>
        public WebSocketClient(TcpClient client)
        {
            Id = Guid.NewGuid();
            m_client = client;
        }

        /// <summary>
        /// Defines the m_hasClientAlreadyHandshake
        /// </summary>
        private bool m_hasClientAlreadyHandshake;

        /// <summary>
        /// The StartListen
        /// </summary>
        /// <returns>The <see cref="Task"/></returns>
        public async Task StartListen()
        {
            Stream = m_client.GetStream();
            while (m_client.Connected)
            {
                while (m_client.Available < 3 && m_client.Connected)
                {
                    /* wait for enough bytes to be available (if client is still connected)*/
                    Thread.Sleep(50);
                }

                byte[] bytes = new byte[m_client.Available];
                Stream.Read(bytes, 0, m_client.Available);

                while (m_client.Available > 0)
                {
                    var offset = bytes.Length;
                    var tmpBytes = new byte[bytes.Length + m_client.Available];

                    Array.Copy(bytes, 0, tmpBytes, 0, bytes.Length);
                    Stream.Read(tmpBytes, bytes.Length, m_client.Available);

                    bytes = tmpBytes;
                }

                //translate bytes of request to string
                if (!m_hasClientAlreadyHandshake)
                {
                    var data = Encoding.UTF8.GetString(bytes);
                    if (new Regex("^GET").IsMatch(data))
                    {
                        m_hasClientAlreadyHandshake = true;
                        Handshake?.Invoke(this, new WebSocketHandshakeEventArgs(data));
                    }
                }
                else
                {
                    Message?.Invoke(this, new WebSocketDataEventArgs(bytes));
                }
            }
        }

        /// <summary>
        /// The Dispose
        /// </summary>
        public void Dispose()
        {
            if (Stream != null) { Stream.Dispose(); }
            if (m_client != null) { m_client.Dispose(); }
        }
    }
}
