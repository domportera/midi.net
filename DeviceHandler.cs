using Commons.Music.Midi;

namespace Midi.Net;

public static class DeviceHandler
{
#pragma warning disable CS0618 // Type or member is obsolete
    private static readonly IMidiAccess MidiAccess = MidiAccessManager.Default;
#pragma warning restore CS0618 // Type or member is obsolete

    public static async Task<T?> TryOpen<T>(string deviceSearchTerm) where T : MidiDevice, new()
    {
        IMidiInput? midiInput = null;
        IMidiOutput? midiOutput = null;
        foreach (var input in MidiAccess.Inputs)
        {
            if (input == null)
                continue;

            if (input.Name.ToLowerInvariant().Contains(deviceSearchTerm))
            {
                midiInput = await MidiAccess.OpenInputAsync(input.Id);
                break;
            }
        }

        if (midiInput == null)
        {
            await Console.Error.WriteLineAsync("Failed to open MIDI device input");
            return null;
        }

        foreach (var output in MidiAccess.Outputs)
        {
            if (output == null)
                continue;

            if (output.Name.ToLowerInvariant().Contains(deviceSearchTerm))
            {
                midiOutput = await MidiAccess.OpenOutputAsync(output.Id);
                break;
            }
        }

        if (midiOutput == null)
        {
            await Console.Error.WriteLineAsync("Failed to open MIDI device output");
            midiInput.Dispose();
            return null;
        }

        return new T
        {
            Input = midiInput,
            Output = midiOutput
        };
    }
}