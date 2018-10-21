/*
 * Filename:  WebSocketHandshakeEventArgs.cs
 * Author:    Alessio Carello
 * Last Update:    20/10/2018 21.42.08
 */

namespace WebSocketServer.EventArgs
{
    /// <summary>
    /// Defines the <see cref="WebSocketHandshakeEventArgs" />
    /// </summary>
    public class WebSocketHandshakeEventArgs
    {
        /// <summary>
        /// Gets the Data
        /// </summary>
        public string Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketHandshakeEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data<see cref="string"/></param>
        public WebSocketHandshakeEventArgs(string data)
        {
            Data = data;
        }
    }
}
