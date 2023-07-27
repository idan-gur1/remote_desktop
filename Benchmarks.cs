using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Order;
using BenchmarkDotNet.Running;
using System;
using System.Collections.Generic;
using System.DirectoryServices.ActiveDirectory;
using System.Drawing;
using System.Text;

namespace RemoteDesktop
{
    [MemoryDiagnoser]
    [Orderer(SummaryOrderPolicy.FastestToSlowest)]
    [RankColumn]
    public class ScreenCaptureBenchmarks
    {
        private static readonly ScreenCapture First = new ScreenCapture(0);

        [Benchmark]
        public void CaptureScreenApi() // memory Allocated - 43 B
        {
            using (Bitmap firstScreen = First.CaptureScreenApi())
            {
                //firstScreen.Save("screenshotApi.png");
            }
        }
        // inconsistent speed
        [Benchmark]
        public void CaptureScreenGraphics() // memory Allocated - 460 B
        {
            using (Bitmap firstScreen = First.CaptureScreenGraphics())
            {
                //firstScreen.Save("screenshotApi.png");
            }
        }
    }
}
