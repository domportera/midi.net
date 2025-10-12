using Commons.Music.Midi;

namespace LinnstrumentKeyboard;

public class MidiDevice : IMidiInput, IMidiOutput
{
    public readonly IMidiInput Input;
    public readonly IMidiOutput Output;

    private MidiStatus? _inputStatus;
    private byte? _nrpnLsb, _nrpnMsb;

    public MidiDevice(IMidiInput input, IMidiOutput output)
    {
        Input = input;
        Output = output;

        input.MessageReceived += OnMessageReceived;
    }

    private void OnMessageReceived(object? sender, MidiReceivedEventArgs e)
    {
        var dataSpan = new ReadOnlySpan<byte>(e.Data, e.Start, e.Length);

        if (!MidiParser.TryInterpret(ref _inputStatus, dataSpan, out var msg))
        {
            Console.WriteLine("Failed to parse MIDI message");
            return;
        }

        try
        {
            MidiReceived?.Invoke(this, msg.Value);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }

    public async Task CloseAsync()
    {
        try
        {
            await Input.CloseAsync();
            await Output.CloseAsync();
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync(e.ToString());
        }
    }

    public IMidiPortDetails Details => Input.Details;

    public MidiPortConnectionState Connection => Input.Connection;

    public void Dispose()
    {
        try
        {
            Input.Dispose();
            Output.Dispose();
        }
        catch (Exception e)
        {
            Console.Error.WriteLine(e);
        }
    }
    
    public void Send(byte[] mevent, int offset, int length, long timestamp) => Output.Send(mevent, offset, length, timestamp);

    public event EventHandler<MidiEvent>? MidiReceived; 

    public event EventHandler<MidiReceivedEventArgs>? MessageReceived
    {
        add => Input.MessageReceived += value;
        remove => Input.MessageReceived -= value;
    }
}