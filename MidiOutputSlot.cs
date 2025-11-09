using System.Diagnostics.CodeAnalysis;
using Commons.Music.Midi;

namespace Midi.Net;

public sealed class MidiOutputSlot : MidiSlot, IMidiOutput
{
    public MidiOutputSlot(DeviceHandler.DeviceSearchTerm term, IMidiAccess2 access) : base(access, term)
    {
    }

    public void Send(byte[] mevent, int offset, int length, long timestamp)
    {
        if (AllowSend)
        {
            try
            {
                Output.Send(mevent, offset, length, timestamp);
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        }
    }

    protected override void OnConnectionStateChanged(IMidiPort? port)
    {
        Output = port as IMidiOutput;
        AllowSend = Output != null;   
        
    }

    protected override Task<Result<IMidiPort>> BeginConnectTask(DeviceHandler.DeviceSearchTerm searchTerm)
    {
        return DeviceHandler.TryOpenOutput(searchTerm).Cast<IMidiOutput, IMidiPort>();
    }

    protected override void OnDispose(bool disposing)
    {
    }

    private IMidiOutput? Output { get; set; }


    [MemberNotNullWhen(true, nameof(Output))]
    private bool AllowSend { get; set; }
}