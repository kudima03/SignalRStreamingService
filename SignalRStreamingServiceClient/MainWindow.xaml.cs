using AForge.Video.DirectShow;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace SignalRStreamingServiceClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        HubConnection connection;

        private event EventHandler<NewFrameArgs> NewFrame;

        private Channel<byte[]> channel;

        BitmapSource src;

        bool a = true;

        public MainWindow()
        {
            InitializeComponent();
            connection = new HubConnectionBuilder().WithUrl("http://localhost:5000/stream").Build();
            NewFrame += MainWindow_NewFrame;
            ImageBox.Source = src;
            connection.On<byte[]>("video-data", (item) =>
            {
                NewFrame(this, new NewFrameArgs(item));
            });
            connection.StartAsync().Wait();
            channel = Channel.CreateUnbounded<byte[]>();
        }

        public BitmapSource ToBitmapSource(Bitmap bmp)
        {
            var rect = new Rectangle(0, 0, bmp.Width, bmp.Height);
            var bmpData = bmp.LockBits(rect, System.Drawing.Imaging.ImageLockMode.ReadWrite, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
            int bufferSize = bmpData.Stride * bmp.Height;
            var bms = new WriteableBitmap(bmp.Width, bmp.Height, bmp.HorizontalResolution, bmp.VerticalResolution, PixelFormats.Bgr32, null);
            bms.WritePixels(new Int32Rect(0, 0, bmp.Width, bmp.Height), bmpData.Scan0, bufferSize, bmpData.Stride);
            bmp.UnlockBits(bmpData);
            return bms;
        }

        public Bitmap ToBitmap(byte[] arr)
        {
            using (var ms = new MemoryStream(arr))
            {
                return new Bitmap(ms);
            }
        }

        private void MainWindow_NewFrame(object? sender, NewFrameArgs e)
        {
            var a = ToBitmap(e.Frame);
            a.Save("test.jpeg");
            //ImageBox.Source = ToBitmapSource();
        }


        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            FilterInfoCollection devices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            var videoCaptureDevice = new VideoCaptureDevice(devices[0].MonikerString);
            videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
            await connection.SendAsync("Stream", channel.Reader);
            videoCaptureDevice.Start();
           
        }

        private async void VideoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            var a = new Bitmap(eventArgs.Frame, 50, 50);
            byte[]? bytes = TypeDescriptor.GetConverter(a).ConvertTo(a, typeof(byte[])) as byte[];
            await channel.Writer.WriteAsync(bytes);
        }
    }
}
