using Commons.Music.Midi;

namespace Midi.Net;

public static partial class DeviceHandler
{
    private static readonly IMidiAccess2 MidiAccess = new MidiOpener();

    public static async Task<DeviceOpenResult<T>> TryOpen<T>(string deviceSearchTerm)
        where T : MidiDevice, new()
    {
        var (result, midiInput, midiOutput) = await TryOpen(deviceSearchTerm);
        if (result != DeviceOpenResult.Success)
            return new DeviceOpenResult<T>(result, null);


        var device = new T
        {
            Input = midiInput!,
            Output = midiOutput!
        };

        await device.OnConnect();
        return new DeviceOpenResult<T>(result, device);
    }

    private static async Task<DeviceOpenResult<IMidiInput, IMidiOutput>> TryOpen(string deviceSearchTerm)
    {
        IMidiInput? midiInput = null;
        IMidiOutput? midiOutput = null;
        foreach (var input in MidiAccess.Inputs)
        {
            if (input == null)
                continue;

            if (input.Name.ToLowerInvariant().Contains(deviceSearchTerm))
            {
                try
                {
                    midiInput = await MidiAccess.OpenInputAsync(input.Id);
                }
                catch (Exception)
                {
                    return new DeviceOpenResult<IMidiInput, IMidiOutput>(DeviceOpenResult.InputOpenFailed, null, null);
                }

                break;
            }
        }

        if (midiInput == null)
        {
            midiOutput?.Dispose();
            midiOutput = null;
            return new DeviceOpenResult<IMidiInput, IMidiOutput>(DeviceOpenResult.InputNotFound, midiInput, midiOutput);
        }

        foreach (var output in MidiAccess.Outputs)
        {
            if (output == null)
                continue;

            if (output.Name.ToLowerInvariant().Contains(deviceSearchTerm))
            {
                try
                {
                    midiOutput = await MidiAccess.OpenOutputAsync(output.Id);
                }
                catch (Exception)
                {
                    return new DeviceOpenResult<IMidiInput, IMidiOutput>(DeviceOpenResult.OutputOpenFailed, midiInput,
                        midiOutput);
                }

                break;
            }
        }

        if (midiOutput == null)
        {
            await Console.Error.WriteLineAsync("Failed to open MIDI device output");
            midiInput.Dispose();
            midiInput = null;
            return new DeviceOpenResult<IMidiInput, IMidiOutput>(DeviceOpenResult.OutputNotFound, midiInput,
                midiOutput);
        }

        return new DeviceOpenResult<IMidiInput, IMidiOutput>(DeviceOpenResult.Success, midiInput, midiOutput);
    }
}