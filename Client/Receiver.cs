using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace Client
{
    public class ScreenSharingReceiver
    {
        private UdpClient udpServer;
        private IPEndPoint ServerEndPoint;
        private Thread serverThread;
        private int listenPort;
        private bool isRunning;

        private Dictionary<int, Dictionary<int, byte[]>> receivedBitmaps = new Dictionary<int, Dictionary<int, byte[]>>();

        public ScreenSharingReceiver(int listenPort)
        {
            ServerEndPoint = new IPEndPoint(IPAddress.Any, 0);
            this.listenPort = listenPort;
        }

        public void Start()
        {
            if (!isRunning)
            {
                isRunning = true;
                serverThread = new Thread(ReceiveBitmapFragments);
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

        private void ReceiveBitmapFragments()
        {
            udpServer = new UdpClient(listenPort);

            while (isRunning)
            {
                byte[] receivedData = udpServer.Receive(ref ServerEndPoint);

                int bitmapId = BitConverter.ToInt32(receivedData, 0);
                int fragmentId = BitConverter.ToInt32(receivedData, sizeof(int));
                bool isLastFragment = BitConverter.ToBoolean(receivedData, sizeof(int) + sizeof(int));

                byte[] fragmentData = new byte[receivedData.Length - sizeof(int) - sizeof(int) - sizeof(bool)];
                Buffer.BlockCopy(receivedData, sizeof(int) + sizeof(int) + sizeof(bool), fragmentData, 0, fragmentData.Length);

                if (!receivedBitmaps.ContainsKey(bitmapId))
                {
                    receivedBitmaps[bitmapId] = new Dictionary<int, byte[]>();
                }

                receivedBitmaps[bitmapId][fragmentId] = fragmentData;

                if (isLastFragment)
                {
                    Dictionary<int, byte[]> fragments = receivedBitmaps[bitmapId];

                    List<byte[]> orderedFragments = new List<byte[]>(fragments.Count);
                    for (int i = 0; i < fragments.Count; i++)
                    {
                        if (fragments.ContainsKey(i))
                        {
                            orderedFragments.Add(fragments[i]);
                        }
                        else
                        {
                            // missing fragment
                        }
                    }

                    Bitmap receivedBitmap = ReassembleBitmap(orderedFragments);

                    receivedBitmap.Dispose();

                    receivedBitmaps.Remove(bitmapId);
                }
            }

            udpServer.Close();
        }

        private Bitmap ReassembleBitmap(List<byte[]> fragments)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                foreach (byte[] fragment in fragments)
                {
                    stream.Write(fragment, 0, fragment.Length);
                }

                stream.Position = 0;

                Bitmap bitmap = new Bitmap(stream);

                return bitmap;
            }
        }
    }
}
