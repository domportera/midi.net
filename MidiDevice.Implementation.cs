using Commons.Music.Midi;

namespace Midi.Net;

public partial class MidiDevice
{

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

    #region managed-midi interface implementation

    IMidiPortDetails IMidiPort.Details => Input.Details;
    MidiPortConnectionState IMidiPort.Connection => Input.Connection;

    void IMidiOutput.Send(byte[] mevent, int offset, int length, long timestamp) =>
        Output.Send(mevent, offset, length, timestamp);

    event EventHandler<MidiReceivedEventArgs>? IMidiInput.MessageReceived
    {
        add => Input.MessageReceived += value;
        remove => Input.MessageReceived -= value;
    }

    #endregion

    private void Dispose(bool disposing)
    {
        try
        {
            Input.MessageReceived -= OnMessageReceived;

            if (OperatingSystem.IsWindows())
            {
                Input.Dispose();
                Output.Dispose();
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // On Unix-based systems, only dispose input
                Input.Dispose();
            }
            else
            {
                // For unsupported platforms, dispose both to be safe
                Output.Dispose();
                Input.Dispose();
            }
        }
        catch (Exception e)
        {
            Console.Error.WriteLine($"Error during device disposal: {e.Message}");
        }

        if (disposing)
        {
            IsDisposed = true;
        }
    }

    ~MidiDevice()
    {
        Dispose(false);
    }
}