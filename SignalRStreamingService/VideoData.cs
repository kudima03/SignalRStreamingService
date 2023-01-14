using System.Text.Json.Serialization;

namespace SignalRStreamingService
{
    public class VideoData
    {
        public int Index { get; }
        public byte[] Part { get; }

        [JsonConstructor]
        public VideoData(int index, byte[] part) => (Index, Part) = (index, part);
    }
}
