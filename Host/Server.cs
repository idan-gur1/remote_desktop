using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Utilities;

namespace Host
{
    class Server
    {
        private const int maxChunkSize = 4096;
        private const float FrameRateBuffer = 1.4f;

        public int AuthCode { get; private set; }

        private readonly int interval;

        private ScreenCapture screenCapture;
        private UdpClient udpClient;
        private IPEndPoint clientEndPoint;
        private Thread serverThread;
        private int bitmapId;
        private bool isRunning;

        public Server(int screen, int desiredFPS)
        {
            clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
            screenCapture = new ScreenCapture(screen);
            interval = 1000 / (int)(desiredFPS * FrameRateBuffer);
        }

        public void Start()
        {
            if (!isRunning)
            {
                AuthCode = SecureRandomNumberGenerator.GenerateRandomInt(10000, 1000000);
                udpClient = new UdpClient(55555);

                isRunning = true;
                serverThread = new Thread(SendScreenCapture);
                serverThread.Start();
            }
        }

        public void Stop()
        {
            isRunning = false;
            if (serverThread != null && serverThread.IsAlive)
            {
                serverThread.Join();
            }
        }

        private void SendScreenCapture()
        {
            HandleClientAuth();

            Stopwatch clock = Stopwatch.StartNew();

            while (isRunning)
            {
                clock.Restart();

                using (MemoryStream bitmapStream = screenCapture.CaptureScreenToMemoryStream())
                    SendMemoryStreamInFragments(bitmapStream);

                bitmapId++;

                // frame rate limit
                Thread.Sleep(Math.Max((int)(interval - clock.ElapsedMilliseconds), 0));
            }

            clock.Stop();
            udpClient.Close();
            udpClient.Dispose();
        }

        private void HandleClientAuth()
        {

            int code = -1;

            while (isRunning)
            {

                if (!udpClient.Client.Poll(500000, SelectMode.SelectRead)) continue;

                byte[] data = udpClient.Receive(ref clientEndPoint);
                try
                {
                    code = BitConverter.ToInt32(data, 0);
                }
                catch
                {
                    udpClient.Send(MsgCodes.WrongAuth, MsgCodes.WrongAuth.Length, clientEndPoint);
                    continue;
                }

                if (code == AuthCode)
                {
                    udpClient.Send(MsgCodes.AuthOk, MsgCodes.AuthOk.Length, clientEndPoint);
                    break;
                }
                udpClient.Send(MsgCodes.WrongAuth, MsgCodes.WrongAuth.Length, clientEndPoint);
            }


        }

        private void SendMemoryStreamInFragments(MemoryStream stream)
        {
            int fragmentId = 0;

            byte[] buffer = new byte[maxChunkSize];
            int bytesRead;

            while ((bytesRead = stream.Read(buffer, 0, maxChunkSize)) > 0)
            {
                byte[] chunkData = new byte[bytesRead];
                Array.Copy(buffer, chunkData, bytesRead);

                bool isLastFragment = bytesRead < maxChunkSize;
                byte[] packetData = new byte[bytesRead + sizeof(int) + sizeof(int) + sizeof(bool)];
                Buffer.BlockCopy(chunkData, 0, packetData, sizeof(int) + sizeof(int) + sizeof(bool), bytesRead);
                Buffer.BlockCopy(BitConverter.GetBytes(bitmapId), 0, packetData, 0, sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(fragmentId), 0, packetData, sizeof(int), sizeof(int));
                Buffer.BlockCopy(BitConverter.GetBytes(isLastFragment), 0, packetData, sizeof(int) + sizeof(int), sizeof(bool));

                udpClient.Send(packetData, packetData.Length, clientEndPoint);

                fragmentId++;
            }
        }
    }

    
}
