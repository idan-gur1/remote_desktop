using BenchmarkDotNet.Running;
using screenshot_testing.WindowsApi;
using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace screenshot_testing
{
    internal class Program
    {

        public const float defaultWindowsDPI = 96f;

        static void Main(string[] args)
        {
            BenchmarkRunner.Run<ScreenCaptureBenchmarks>();
        }
    }

}