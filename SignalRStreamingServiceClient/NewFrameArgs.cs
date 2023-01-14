using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SignalRStreamingServiceClient
{
    internal class NewFrameArgs : EventArgs
    {
        public byte[] Frame { get; set; }

        public NewFrameArgs(byte[] frame)
        {
            Frame = frame;
        }
    }
}
