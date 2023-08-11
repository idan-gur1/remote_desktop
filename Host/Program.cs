using BenchmarkDotNet.Running;
using Host.Tests;
using Host.WindowsApi;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using Utilities;

namespace Host
{
    class Program
    {

        public static void Main(string[] args)
        {
            Server s = new Server(0, 15);

            s.Start();

            Console.WriteLine("Started");

            Thread.Sleep(3000);

            s.Stop();

            Console.WriteLine("Stopped");
        }
    }

}