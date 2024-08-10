using System;
using System.Collections.Generic;
using System.Threading.Channels;

namespace TestNS;

public sealed class WrapItems
{
    public readonly Channel<object> Items; 

    public WrapItems()
    {
        Items = Channel.CreateUnbounded<object>(new()
        {
            SingleWriter = true,
            SingleReader = false,
            AllowSynchronousContinuations = true
        });
    }

    public ChannelReader<object> Reader => Items.Reader;
    public ChannelWriter<object> Writer => Items.Writer;
}
