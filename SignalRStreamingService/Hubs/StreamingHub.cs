using Microsoft.AspNetCore.SignalR;
using System.Drawing;
using System.IO;
using System.Threading.Channels;

namespace SignalRStreamingService.Hubs
{
    public class StreamingHub : Hub
    {

        private readonly IWebHostEnvironment _webEnv;

        public StreamingHub(IWebHostEnvironment webEnv)
        {
            _webEnv = webEnv;
        }

        public async Task Stream(ChannelReader<byte[]> videoData)
        {
            while (await videoData.WaitToReadAsync())
            {
                while (videoData.TryRead(out var item))
                {
                    await Clients.All.SendAsync("video-data", item);
                }
            }
        }
    }
}
