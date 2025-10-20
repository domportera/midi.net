using Commons.Music.Midi;

namespace Midi.Net;

public partial class MidiDevice
{
    private void OnMessageReceived(object? sender, MidiReceivedEventArgs e)
    {
        var dataSpan = new ReadOnlySpan<byte>(e.Data, e.Start, e.Length);

        if (!MidiParser.TryInterpret(ref _inputStatus, dataSpan, out var msg, out var message))
        {
            _ = Console.Error.WriteLineAsync($"Failed to parse MIDI message: {message}");
            return;
        }

        try
        {
            MidiReceived?.Invoke(this, msg.Value);
        }
        catch (Exception ex)
        {
            _ = Console.Error.WriteLineAsync(ex.ToString());
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

    private async Task Dispose(bool disposing)
    {
        if (disposing)
        {
            IsDisposed = true;
        }

        await _cancellationTokenSource.CancelAsync();
        _midiSendEvent.Dispose();

        try
        {
            Input.MessageReceived -= OnMessageReceived;

            if (OperatingSystem.IsWindows())
            {
                await Input.CloseAsync();
                await Output.CloseAsync();
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                // On Unix-based systems, only dispose input
                await Input.CloseAsync();
                await Output.CloseAsync();
            }
            else
            {
                // For unsupported platforms, dispose both to be safe
                await Input.CloseAsync();
                await Output.CloseAsync();
            }
        }
        catch (Exception e)
        {
            await Console.Error.WriteLineAsync($"Error during device disposal: {e.Message}");
        }
    }

    ~MidiDevice()
    {
        _ = Dispose(false);
    }
}