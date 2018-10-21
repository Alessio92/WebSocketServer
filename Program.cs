/*
 * Filename:  Program.cs
 * Author:    Alessio Carello
 * Last Update:    20/10/2018 12.09.14
 */

namespace WebSocketServer
{
    using System;
    using System.Collections.Generic;
    using System.Threading;

    /// <summary>
    /// Defines the <see cref="Program" />
    /// </summary>
    internal class Program
    {
        /// <summary>
        /// The Main
        /// </summary>
        /// <param name="args">The args<see cref="string[]"/></param>
        internal static void Main(string[] args)
        {
            m_registeredClient = new HashSet<Guid>();
            WebSocketServer.Istance.RegisterAsyncHandler("GetVuMeterValues", sendData);
            WebSocketServer.Istance.RegisterHandler("StopSendVuMeterValues", stopSendData);
            WebSocketServer.Istance.Start().Wait();
        }

        /// <summary>
        /// Defines the m_registeredClient
        /// </summary>
        private static HashSet<Guid> m_registeredClient;

        /// <summary>
        /// The stopSendData
        /// </summary>
        /// <param name="client">The client<see cref="WebSocketClient"/></param>
        /// <param name="arg">The arg<see cref="string"/></param>
        /// <returns>The <see cref="string"/></returns>
        private static string stopSendData(WebSocketClient client, string arg)
        {
            if (!m_registeredClient.Contains(client.Id))
            {
#if FULL_LOG
                Console.WriteLine($"Client {client.Id} has never registered");
#endif
                return string.Empty;
            }
            m_registeredClient.Remove(client.Id);
            return string.Empty;
        }

        /// <summary>
        /// The sendData
        /// </summary>
        /// <param name="client">The client<see cref="WebSocketClient"/></param>
        internal static void sendData(WebSocketClient client)
        {
            if (m_registeredClient.Contains(client.Id))
            {
#if FULL_LOG
                Console.WriteLine($"Client {client.Id} already registered");
#endif
                return;
            }

            var rng = new Random();
            m_registeredClient.Add(client.Id);
            while (m_registeredClient.Contains(client.Id))
            {
                if (!WebSocketServer.Istance.Write(client, rng.Next(1, 100).ToString())) { return; }
                Thread.Sleep(1000 / 15);
            }
        }
    }
}
