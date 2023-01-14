using AForge.Video.DirectShow;
using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.ComponentModel;
using System.Configuration;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
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
        private HubConnection _connection;

        private Channel<byte[]> _channel;

        private VideoCaptureDevice _videoCaptureDevice;

        private CancellationTokenSource _screenCaptureCancellationSource = new CancellationTokenSource();

        private MemoryStream _stream = new MemoryStream();

        private bool _receiveEnabled = true;

        private void InitializeVideoCaptureDevice()
        {
            var videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice) ?? throw new Exception("No video devices found");
            _videoCaptureDevice = new VideoCaptureDevice(videoDevices[0].MonikerString);
        }

        private void InitializeConnection()
        {
            var hubUrl = ConfigurationManager.AppSettings.Get("HubUrl") ?? throw new Exception("Url cannot be nulll");
            _connection = new HubConnectionBuilder().WithUrl(hubUrl).Build();
            _connection.On<byte[]>("video-data", NewFrameReceived);
        }

        private void InitializeChannel()
        {
            _channel = Channel.CreateUnbounded<byte[]>();
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeVideoCaptureDevice();
            InitializeChannel();
            InitializeConnection();
        }

        private void NewFrameReceived(byte[] frame)
        {
            Dispatcher.Invoke(new Action(async () =>
            {
                ImageBox.Source = ToBitmapSource(await ToBitmap(frame));
            }));
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

        public async Task<Bitmap> ToBitmap(byte[] arr)
        {
            await _stream.WriteAsync(arr, 0, arr.Length);
            var bmp = new Bitmap(_stream);
            _stream.Position = 0;
            _stream.SetLength(0);
            return bmp;
        }

        private async void VideoCaptureDevice_NewFrame(object sender, AForge.Video.NewFrameEventArgs eventArgs)
        {
            byte[]? bytes = TypeDescriptor.GetConverter(eventArgs.Frame).ConvertTo(eventArgs.Frame, typeof(byte[])) as byte[];
            await _channel.Writer.WriteAsync(bytes);
        }

        private async void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            await _connection.StartAsync();
        }

        private async void StreamButton_Click(object sender, RoutedEventArgs e)
        {
            _videoCaptureDevice.NewFrame += VideoCaptureDevice_NewFrame;
            await _connection.SendAsync("Stream", _channel.Reader);
            _videoCaptureDevice.Start();
        }

        private void StopStreamButton_Click(object sender, RoutedEventArgs e)
        {
            _videoCaptureDevice.SignalToStop();
        }

        private async void ScreenCapture_Click(object sender, RoutedEventArgs e)
        {
            await _connection.SendAsync("Stream", _channel.Reader);
            await Task.Run(async () =>
            {
                var bmp = new Bitmap(1920, 1080);
                var gr = Graphics.FromImage(bmp);
                while (!_screenCaptureCancellationSource.IsCancellationRequested)
                {
                    await Task.Delay(35);
                    gr.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(1920, 1080));
                    await _channel.Writer.WriteAsync(TypeDescriptor.GetConverter(bmp).ConvertTo(bmp, typeof(byte[])) as byte[]);
                }
            }, _screenCaptureCancellationSource.Token);
        }

        private async void StopScreenCapture_Click(object sender, RoutedEventArgs e)
        {
            _screenCaptureCancellationSource.Cancel();
            await Task.Delay(70);
            _screenCaptureCancellationSource = new CancellationTokenSource();
        }

        private void SwitchView_Click(object sender, RoutedEventArgs e)
        {
            if (_receiveEnabled)
            {
                _connection.Remove("video-data");
                _receiveEnabled = false;
            }
            else
            {
                _connection.On<byte[]>("video-data", NewFrameReceived);
                _receiveEnabled = true;
            }
        }

        private async void Window_Closing(object sender, CancelEventArgs e)
        {
            _videoCaptureDevice.SignalToStop();
            _channel.Writer.Complete();
            await _connection.DisposeAsync();
        }
    }
}