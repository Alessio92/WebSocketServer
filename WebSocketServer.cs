/*
 * Filename:  WebSocketServer.cs
 * Author:    Alessio Carello
 * Last Update:    20/10/2018 12.09.37
 */

namespace WebSocketServer
{
    using global::WebSocketServer.EventArgs;
    using System;
    using System.Collections.Generic;
    using System.Net;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;

    /// <summary>
    /// Defines the <see cref="WebSocketServer" />
    /// </summary>
    public class WebSocketServer
    {
        /// <summary>
        /// Defines the m_callbacks
        /// </summary>
        private Dictionary<string, Func<WebSocketClient, string, string>> m_callbacks;

        /// <summary>
        /// Defines the m_asyncCallbacks
        /// </summary>
        private Dictionary<string, Action<WebSocketClient>> m_asyncCallbacks;

        /// <summary>
        /// Defines the m_connectedClients
        /// </summary>
        private List<WebSocketClient> m_connectedClients;

        /// <summary>
        /// Gets or sets the Port
        /// </summary>
        public int Port { get; set; }

        /// <summary>
        /// Defines the m_istance
        /// </summary>
        private static WebSocketServer m_istance;

        /// <summary>
        /// Gets the Istance
        /// </summary>
        public static WebSocketServer Istance
        {
            get
            {
                return m_istance ?? (m_istance = new WebSocketServer());
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketServer"/> class.
        /// </summary>
        public WebSocketServer()
        {
            m_connectedClients = new List<WebSocketClient>(30);
            m_asyncCallbacks = new Dictionary<string, Action<WebSocketClient>>();
            m_callbacks = new Dictionary<string, Func<WebSocketClient, string, string>>();
        }

        /// <summary>
        /// The RegisterHandler
        /// </summary>
        /// <param name="request">The request<see cref="string"/></param>
        /// <param name="callback">The callback<see cref="Func{string, string}"/></param>
        public void RegisterHandler(string request, Func<WebSocketClient, string, string> callback)
        {
            m_callbacks[request] = callback;
        }

        /// <summary>
        /// The RegisterAsyncHandler
        /// </summary>
        /// <param name="request">The request<see cref="string"/></param>
        /// <param name="callback">The callback<see cref="Func{NetworkStream, string}"/></param>
        public void RegisterAsyncHandler(string request, Action<WebSocketClient> callback)
        {
            m_asyncCallbacks[request] = callback;
        }

        /// <summary>
        /// Defines the started
        /// </summary>
        private bool started;

        /// <summary>
        /// The Start
        /// </summary>
        /// <param name="port">The port<see cref="int"/></param>
        /// <returns>The <see cref="Task"/></returns>
        public async Task Start(int port = 8990)
        {
            Port = port;
            if (started)
            {
                Console.WriteLine("WebSocket server already started");
                return;
            }

            started = true;
            TcpListener listener = new TcpListener(IPAddress.Any, port);
            listener.Start();
            Console.WriteLine("WebSocket server started");

            while (true)
            {
                Console.WriteLine("Waiting for new connection..");
                var client = new WebSocketClient(await listener.AcceptTcpClientAsync());

                m_connectedClients.Add(client);

                Console.WriteLine($"New client from {client.RemoteEndPoint}");
                Console.WriteLine($"Actually there are {m_connectedClients.Count} connected");

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Factory.StartNew(() =>
                {
                    client.Handshake += onClientHandshake;
                    client.Message += onClientMessage;

                    client.StartListen().ContinueWith((o) =>
                    {
                        m_connectedClients.Remove(client);
                        client.Dispose();
                        Console.WriteLine($"Client removed, now there are {m_connectedClients.Count} connected");
                    });
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
        }

        /// <summary>
        /// The onClientHandshake
        /// </summary>
        /// <param name="sender">The sender<see cref="WebSocketClient"/></param>
        /// <param name="e">The e<see cref="WebSocketHandshakeEventArgs"/></param>
        private void onClientHandshake(WebSocketClient sender, WebSocketHandshakeEventArgs e)
        {
            var handshake = calculateHandshake(e.Data);
            sender.Stream.Write(handshake, 0, handshake.Length);
            sender.Handshake -= onClientHandshake;
        }

        /// <summary>
        /// The onClientMessage
        /// </summary>
        /// <param name="sender">The sender<see cref="WebSocketClient"/></param>
        /// <param name="e">The e<see cref="WebSocketDataEventArgs"/></param>
        private void onClientMessage(WebSocketClient sender, WebSocketDataEventArgs e)
        {

            /*
             * Parse message as described in
             * https://developer.mozilla.org/en-US/docs/Web/API/WebSockets_API/Writing_WebSocket_server#collapse-3
             */

            var byte1 = e.Data[0]; // should be 129 for text

            var received = getBytesBit(byte1);
            var opCode = convertBitArrayToNumber(new[] { received[4], received[5], received[6], received[7] });
#if FULL_LOG
            Console.WriteLine($"{received[0]} - FIN");
            Console.WriteLine($"{received[1]} - RSV1");
            Console.WriteLine($"{received[2]} - RSV2");
            Console.WriteLine($"{received[3]} - RSV3");
            Console.WriteLine($"{opCode} - Opcode");
#endif
            try
            {
                switch (opCode)
                {
                    case 0x0:
                        Console.WriteLine($"Received unmanaged `binary` from client {sender.RemoteEndPoint}");
                        break;// denotes a continuation frame
                    case 0x1: // denotes a text frame
                        Console.WriteLine($"Received `text` from client {sender.RemoteEndPoint}");
                        manageTextReceived(sender, e.Data);
                        break;
                    case 0x2: break; // reserved for further non-control frames
                    case 0x3: break;
                    case 0x4: break;
                    case 0x5: break;
                    case 0x6: break;
                    case 0x7: break;
                    case 0x8:
                        Console.WriteLine($"Received `close connection` from client {sender.RemoteEndPoint}");
                        sender.Dispose();
                        break;// denotes a connection close
                    case 0x9:
                        Console.WriteLine($"Received `ping` from client {sender.RemoteEndPoint}, send `pong`");
                        SendPong(sender);
                        break;// denotes a ping
                    case 0xA:
                        Console.WriteLine($"Received `pong` from client {sender.RemoteEndPoint}, it's alive");
                        break;// denotes a pong
                    case 0xB: break; //reserved for further control frames
                    case 0xC: break;
                    case 0xD: break;
                    case 0xE: break;
                    case 0xF: break;
                    default:
                        Console.WriteLine($"Received unknown opcode `{opCode}`. Close connection.");
                        sender.Dispose();
                        break;
                }
            }
            catch (Exception)
            {
                sender.Dispose();
            }
        }

        /// <summary>
        /// The writeDataInStream
        /// </summary>
        /// <param name="stream">The stream<see cref="NetworkStream"/></param>
        /// <param name="data">The data<see cref="byte[]"/></param>
        /// <returns>The <see cref="bool"/></returns>
        private bool writeDataInStream(NetworkStream stream, byte[] data)
        {
            if (!stream.CanWrite)
            {
                return false;
            }
            try
            {
                stream.Write(data, 0, data.Length);
            }
            catch (Exception e)
            {
                return false;
            }
            return true;
        }

        /// <summary>
        /// The SendPing
        /// </summary>
        /// <param name="client">The client<see cref="WebSocketClient"/></param>
        /// <returns>The <see cref="bool"/></returns>
        public bool SendPing(WebSocketClient client)
        {
            try
            {
                byte[] send = new byte[1];
                send[0] = 0x80 + 0x9; // last frame, ping

                return writeDataInStream(client.Stream, send);
            }
            catch (Exception e) { return false; }
        }

        /// <summary>
        /// The SendPong
        /// </summary>
        /// <param name="client">The client<see cref="WebSocketClient"/></param>
        /// <returns>The <see cref="bool"/></returns>
        public bool SendPong(WebSocketClient client)
        {
            try
            {
                byte[] send = new byte[1];
                send[0] = 0x80 + 0xA; // last frame, pong

                return writeDataInStream(client.Stream, send);
            }
            catch (Exception e) { return false; }
        }

        /// <summary>
        /// The Write
        /// </summary>
        /// <param name="client">The client<see cref="WebSocketClient"/></param>
        /// <param name="message">The message<see cref="string"/></param>
        /// <returns>The <see cref="bool"/></returns>
        public bool Write(WebSocketClient client, string message)
        {
            try
            {
                var nextValue = Encoding.UTF8.GetBytes(message);
                byte[] send = new byte[2 + nextValue.Length];
                send[0] = 0x80 + 0x1; // last frame, text
                send[1] = BitConverter.GetBytes(nextValue.Length)[0]; // not masked
                for (var i = 0; i < nextValue.Length; i++)
                {
                    send[2 + i] = nextValue[i];
                }

                return writeDataInStream(client.Stream, send);
            }
            catch (Exception e) { return false; }
        }

        /// <summary>
        /// The calculateHandshake
        /// </summary>
        /// <param name="data">The data<see cref="string"/></param>
        /// <returns>The <see cref="Task"/></returns>
        private byte[] calculateHandshake(string data)
        {
            const string eol = "\r\n"; // HTTP/1.1 defines the sequence CR LF as the end-of-line marker

            Byte[] response = Encoding.UTF8.GetBytes("HTTP/1.1 101 Switching Protocols" + eol
                + "Connection: Upgrade" + eol
                + "Upgrade: websocket" + eol
                + "Sec-WebSocket-Accept: " + Convert.ToBase64String(
                    System.Security.Cryptography.SHA1.Create().ComputeHash(
                        Encoding.UTF8.GetBytes(new Regex("Sec-WebSocket-Key: (.*)").Match(data).Groups[1].Value.Trim() + "258EAFA5-E914-47DA-95CA-C5AB0DC85B11"
                        )
                    )
                ) + eol
                + eol);
            return response;
        }

        /// <summary>
        /// Defines the SOCKET_TEXT_MESSAGE_BYTE_VALUE
        /// </summary>
        private const short SOCKET_TEXT_MESSAGE_BYTE_VALUE = 129;

        /// <summary>
        /// The getBytesBit
        /// </summary>
        /// <param name="b">The b<see cref="byte"/></param>
        /// <returns>The <see cref="bool[]"/></returns>
        private bool[] getBytesBit(byte b)
        {
            var result = new bool[]
                {
                    ((b >> 7) & 1) != 0,
                    ((b >> 6) & 1) != 0,
                    ((b >> 5) & 1) != 0,
                    ((b >> 4) & 1) != 0,
                    ((b >> 3) & 1) != 0,
                    ((b >> 2) & 1) != 0,
                    ((b >> 1) & 1) != 0,
                    ((b >> 0) & 1) != 0
                };
            return result;
        }

        /// <summary>
        /// The convertBitArrayToNumber
        /// </summary>
        /// <param name="data">The data<see cref="bool[]"/></param>
        /// <param name="littleEndian">The littleEndian<see cref="bool"/></param>
        /// <returns>The <see cref="int"/></returns>
        unsafe private int convertBitArrayToNumber(bool[] data, bool littleEndian = true)
        {
            var result = 0;
            for (var i = 0; i < data.Length; i++)
            {
                var shift = littleEndian ? (data.Length - i - 1) : i;
                var bit = data[i];
                result += *(byte*)(&bit) << shift;
            }
            return result;
        }

        /// <summary>
        /// The parseMessage
        /// </summary>
        /// <param name="data">The data<see cref="byte[]"/></param>
        /// <returns>The <see cref="string"/></returns>
        public string ParseMessage(byte[] data)
        {
            var msg = string.Empty;

            var byte2 = data[1];

            var indexStartMask = 2;

            /* Because the first bit is always 1 for client-to-server messages,
             * you can subtract 128 from this byte to get rid of the MASK bit.
             * */
            ulong msgCount = (ulong)(byte2 - 128);

            /* Payload Length: If this value is between 0 and 125,
             * then it is the length of message.
             * If it is 126, the following 2 bytes(16 - bit unsigned integer) are the length.
             * If it is 127, the following 8 bytes(64 - bit unsigned integer) are the length.*/
            if (msgCount <= 125) { }
            else if (msgCount == 126)
            {
                msgCount = BitConverter.ToUInt16(new[] { data[3], data[2] }, 0);
                indexStartMask += 2;
            }
            else if (msgCount == 127)
            {
                msgCount = BitConverter.ToUInt64(new[] {
                    data[9], data[8],
                    data[7], data[6],
                    data[5], data[4],
                    data[3], data[2] }, 0);
                indexStartMask += 8;
                if ((msgCount + 14L) != (ulong)data.Length)
                {
                    Console.WriteLine($"Received lenght `{msgCount}` mismatch with real length");
                    throw new Exception("Wrong packet received");
                }
            }

            var mask = new byte[] {
                data[indexStartMask],
                data[indexStartMask+1],
                data[indexStartMask+2],
                data[indexStartMask+3],
            };

            return decodeByteArrayWithMask(data, mask, (ulong)indexStartMask + 4L, msgCount);
        }

        /// <summary>
        /// The manageTextReceived
        /// </summary>
        /// <param name="client">The client<see cref="WebSocketClient"/></param>
        /// <param name="data">The data<see cref="byte[]"/></param>
        private void manageTextReceived(WebSocketClient client, byte[] data)
        {
            var sub = ParseMessage(data);
#if FULL_LOG
            Console.WriteLine($"Received `{sub}` from client `{client.RemoteEndPoint}`");
#endif

            if (m_callbacks.ContainsKey(sub))
            {
                m_callbacks[sub].Invoke(client, sub);
            }
            else if (m_asyncCallbacks.ContainsKey(sub))
            {
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
                Task.Factory.StartNew(() =>
                {
                    m_asyncCallbacks[sub].Invoke(client);
                });
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            }
            else
            {
                Console.WriteLine($"Command `{sub.Substring(0, 10)}`.. not found");
            }
        }

        /// <summary>
        /// The decodeByteArrayWithMask
        /// </summary>
        /// <param name="encoded">The encoded<see cref="byte[]"/></param>
        /// <param name="mask">The mask<see cref="byte[]"/></param>
        /// <param name="startFrom">The startFrom<see cref="int"/></param>
        /// <param name="msgLength">The msgLength<see cref="int"/></param>
        /// <returns>The <see cref="string"/></returns>
        private string decodeByteArrayWithMask(byte[] encoded, byte[] mask, ulong startFrom = 0, ulong msgLength = 0)
        {
            if (msgLength == 0) { msgLength = (ulong)encoded.Length; }

            var decoded = new byte[msgLength];

            for (ulong i = 0; i < msgLength; i++)
            {
                decoded[i] = (byte)(encoded[startFrom + i] ^ mask[i % 4]);
            }

            return Encoding.UTF8.GetString(decoded);
        }
    }
}
