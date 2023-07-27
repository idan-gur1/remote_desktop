using BenchmarkDotNet.Running;
using RemoteDesktop.WindowsApi;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace RemoteDesktop
{
    internal class Program
    {

        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ScreenCaptureBenchmarks>();
        }
    }

}