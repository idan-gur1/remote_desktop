using BenchmarkDotNet.Running;
using Host.Tests;
using RemoteDesktop.WindowsApi;
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

namespace RemoteDesktop
{
    internal class Program
    {

        static void Main(string[] args)
        {
            for (int i = 0; i < 50; i++)
            {
                Console.WriteLine(SecureRandomNumberGenerator.GenerateRandomInt(10000,1000000));
            }
        }
    }

}