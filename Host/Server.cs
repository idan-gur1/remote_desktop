using RemoteDesktop;
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
            udpClient = new UdpClient();

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

            while (true)
            {
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

    // receiving side

    /*
     using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class Server
{
    private static UdpClient udpServer;
    private static int listenPort = 1234; // Replace with the port number the server listens on.
    private static Dictionary<int, List<byte[]>> receivedBitmaps = new Dictionary<int, List<byte[]>>();

    public static void Main()
    {
        // Initialize the UDP server.
        udpServer = new UdpClient(listenPort);

        while (true)
        {
            ReceiveBitmapFragments();
        }
    }

    private static void ReceiveBitmapFragments()
    {
        int maxChunkSize = 4096; // Set the maximum chunk size (adjust to match the client).

        while (true)
        {
            byte[] receivedData = udpServer.Receive(ref remoteEP);

            // Extract the bitmap identifier and the fragment identifier from the packet data.
            int bitmapId = BitConverter.ToInt32(receivedData, 0);
            int fragmentId = BitConverter.ToInt32(receivedData, sizeof(int));

            // Remove the identifiers from the packet data, leaving only the fragment data.
            byte[] fragmentData = new byte[receivedData.Length - sizeof(int) - sizeof(int)];
            Buffer.BlockCopy(receivedData, sizeof(int) + sizeof(int), fragmentData, 0, fragmentData.Length);

            if (!receivedBitmaps.ContainsKey(bitmapId))
            {
                receivedBitmaps[bitmapId] = new List<byte[]>();
            }

            // Add the fragment data to the list of received fragments for this bitmap.
            receivedBitmaps[bitmapId].Add(fragmentData);

            if (fragmentData.Length < maxChunkSize)
            {
                // Last fragment received for this bitmap, reassemble the bitmap.
                Bitmap receivedBitmap = ReassembleBitmap(receivedBitmaps[bitmapId]);

                // Process the receivedBitmap as needed.

                // Optionally, release the resources of the receivedBitmap.
                receivedBitmap.Dispose();

                // Remove the reassembled bitmap fragments from the dictionary.
                receivedBitmaps.Remove(bitmapId);

                // Exit the loop to process the next UDP packet.
                break;
            }
        }
    }

    private static Bitmap ReassembleBitmap(List<byte[]> fragments)
    {
        using (MemoryStream stream = new MemoryStream())
        {
            foreach (byte[] fragment in fragments)
            {
                stream.Write(fragment, 0, fragment.Length);
            }

            // Reset the stream position for reading.
            stream.Position = 0;

            // Create the Bitmap from the reassembled byte array.
            Bitmap bitmap = new Bitmap(stream);

            return bitmap;
        }
    }
}
     */

    // sending side

    /*
     using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class Program
{
    private static UdpClient udpClient;
    private static IPAddress serverIP = IPAddress.Parse("SERVER_IP_ADDRESS"); // Replace with the server's IP address.
    private static int serverPort = 1234; // Replace with the server's port number.
    private static int bitmapId = 0; // Initialize the bitmap identifier.

    public static void Main()
    {
        // Initialize the UDP client.
        udpClient = new UdpClient();

        // Example loop for sending Bitmaps at a certain frame rate.
        int desiredFPS = 30; // Set the desired frame rate (e.g., 30 FPS).
        int interval = 1000 / desiredFPS; // Calculate the time interval in milliseconds between frames.

        while (true)
        {
            MemoryStream bitmapStream = CaptureScreenToMemoryStream(); // Replace this with your MemoryStream capture logic.
            SendMemoryStreamInFragments(bitmapStream);

            // Increment the bitmap identifier for the next bitmap.
            bitmapId++;

            // Optionally, release the resources of the MemoryStream.
            bitmapStream.Dispose();

            // Pause the loop to achieve the desired frame rate.
            Thread.Sleep(interval);
        }
    }

    // Replace this with your MemoryStream capture logic.
    private static MemoryStream CaptureScreenToMemoryStream()
    {
        // Implement screen capture logic here.
        // This function should return the MemoryStream representation of the captured screen frame.
        throw new NotImplementedException();
    }

    private static void SendMemoryStreamInFragments(MemoryStream stream)
    {
        int maxChunkSize = 4096; // Set the maximum chunk size (adjust as needed).
        int totalChunks = (int)Math.Ceiling((double)stream.Length / maxChunkSize);
        int fragmentId = 0; // Initialize the fragment identifier.

        byte[] buffer = new byte[maxChunkSize];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, maxChunkSize)) > 0)
        {
            byte[] chunkData = new byte[bytesRead];
            Array.Copy(buffer, chunkData, bytesRead);

            // Create a packet with the chunk data, the bitmap identifier, and the fragment identifier.
            byte[] packetData = new byte[bytesRead + sizeof(int) + sizeof(int)];
            Buffer.BlockCopy(chunkData, 0, packetData, sizeof(int) + sizeof(int), bytesRead);
            Buffer.BlockCopy(BitConverter.GetBytes(bitmapId), 0, packetData, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(fragmentId), 0, packetData, sizeof(int), sizeof(int));

            // Send the packet as a UDP packet to the server.
            udpClient.Send(packetData, packetData.Length, new IPEndPoint(serverIP, serverPort));

            fragmentId++;
        }
    }
}
     */


    //sending 2.0

    /*
     using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;

public class Program
{
    private static UdpClient udpClient;
    private static IPAddress serverIP = IPAddress.Parse("SERVER_IP_ADDRESS"); // Replace with the server's IP address.
    private static int serverPort = 1234; // Replace with the server's port number.
    private static int bitmapId = 0; // Initialize the bitmap identifier.

    public static void Main()
    {
        // Initialize the UDP client.
        udpClient = new UdpClient();

        // Example loop for sending Bitmaps at a certain frame rate.
        int desiredFPS = 30; // Set the desired frame rate (e.g., 30 FPS).
        int interval = 1000 / desiredFPS; // Calculate the time interval in milliseconds between frames.

        while (true)
        {
            MemoryStream bitmapStream = CaptureScreenToMemoryStream(); // Replace this with your MemoryStream capture logic.
            SendMemoryStreamInFragments(bitmapStream);

            // Increment the bitmap identifier for the next bitmap.
            bitmapId++;

            // Optionally, release the resources of the MemoryStream.
            bitmapStream.Dispose();

            // Pause the loop to achieve the desired frame rate.
            Thread.Sleep(interval);
        }
    }

    // Replace this with your MemoryStream capture logic.
    private static MemoryStream CaptureScreenToMemoryStream()
    {
        // Implement screen capture logic here.
        // This function should return the MemoryStream representation of the captured screen frame.
        throw new NotImplementedException();
    }

    private static void SendMemoryStreamInFragments(MemoryStream stream)
    {
        int maxChunkSize = 4096; // Set the maximum chunk size (adjust as needed).
        int totalChunks = (int)Math.Ceiling((double)stream.Length / maxChunkSize);
        int fragmentId = 0; // Initialize the fragment identifier.

        byte[] buffer = new byte[maxChunkSize];
        int bytesRead;

        while ((bytesRead = stream.Read(buffer, 0, maxChunkSize)) > 0)
        {
            byte[] chunkData = new byte[bytesRead];
            Array.Copy(buffer, chunkData, bytesRead);

            // Create a packet with the chunk data, the bitmap identifier, the fragment identifier, and the "last fragment" flag.
            bool isLastFragment = bytesRead < maxChunkSize;
            byte[] packetData = new byte[bytesRead + sizeof(int) + sizeof(int) + sizeof(bool)];
            Buffer.BlockCopy(chunkData, 0, packetData, sizeof(int) + sizeof(int) + sizeof(bool), bytesRead);
            Buffer.BlockCopy(BitConverter.GetBytes(bitmapId), 0, packetData, 0, sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(fragmentId), 0, packetData, sizeof(int), sizeof(int));
            Buffer.BlockCopy(BitConverter.GetBytes(isLastFragment), 0, packetData, sizeof(int) + sizeof(int), sizeof(bool));

            // Send the packet as a UDP packet to the server.
            udpClient.Send(packetData, packetData.Length, new IPEndPoint(serverIP, serverPort));

            fragmentId++;
        }
    }
}

     */
}
