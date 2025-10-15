using Commons.Music.Midi;

namespace Midi.Net;

public static class DeviceHandler
{
#pragma warning disable CS0618 // Type or member is obsolete
    private static readonly IMidiAccess MidiAccess = MidiAccessManager.Default;
#pragma warning restore CS0618 // Type or member is obsolete

    public static async Task<T?> TryOpen<T>(string deviceSearchTerm) where T : MidiDevice, new()
    {
        var (success, midiInput, midiOutput) = await TryOpen(deviceSearchTerm);
        if (!success)
            return null;

        return new T
        {
            Input = midiInput!,
            Output = midiOutput!
        };
    }

    internal static async Task<(bool, IMidiInput? midiInput, IMidiOutput? midiOutput)> TryOpen(string deviceSearchTerm)
    {
        IMidiInput? midiInput = null;
        IMidiOutput? midiOutput = null;
        foreach (var input in MidiAccess.Inputs)
        {
            if (input == null)
                continue;

            if (input.Name.ToLowerInvariant().Contains(deviceSearchTerm))
            {
                bool success = false;
                int tryCount = 0;
                while (!success)
                {
                    try
                    {
                        ++tryCount;
                        if (tryCount > 5)
                        {
                            return (false, null, null);
                        }
                        midiInput = await MidiAccess.OpenInputAsync(input.Id);
                        success = true;
                    }
                    catch (Exception e)
                    {
                        await Console.Error.WriteLineAsync($"Failed to open MIDI device input, retrying: {e.Message}\n{e.StackTrace}");
                        await Task.Delay(1000);
                    }
                }

                break;
            }
        }

        if (midiInput == null)
        {
            await Console.Error.WriteLineAsync("Failed to open MIDI device input");
            midiOutput?.Dispose();
            midiOutput = null;
            return (false, midiInput, midiOutput);
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
            midiInput = null;
            return (false, midiInput, midiOutput);
        }

        return (true, midiInput, midiOutput);
    }
}