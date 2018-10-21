/*
 * Filename:  WebSocketDataEventArgs.cs
 * Author:    Alessio Carello
 * Last Update:    20/10/2018 21.41.59
 */

namespace WebSocketServer.EventArgs
{
    /// <summary>
    /// Defines the <see cref="WebSocketDataEventArgs" />
    /// </summary>
    public class WebSocketDataEventArgs
    {
        /// <summary>
        /// Gets the Data
        /// </summary>
        public byte[] Data { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="WebSocketDataEventArgs"/> class.
        /// </summary>
        /// <param name="data">The data<see cref="byte[]"/></param>
        public WebSocketDataEventArgs(byte[] data)
        {
            Data = data;
        }
    }
}
