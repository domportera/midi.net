using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Commons.Music.Midi;
using Commons.Music.Midi.CoreMidiApi;
using Commons.Music.Midi.RtMidi;

namespace Midi.Net;

public static partial class DeviceHandler
{
    private static readonly IMidiAccess2 MidiAccess = new MidiOpener();

    public static async Task<(DeviceOpenResult Info, T? Device)> TryOpen<T>(string deviceSearchTerm)
        where T : MidiDevice, new()
    {
        var (result, midiInput, midiOutput) = await TryOpen(deviceSearchTerm);
        if (result != DeviceOpenResult.Success)
            return (result, null);

        return (result, new T
        {
            Input = midiInput!,
            Output = midiOutput!
        });
    }

    public enum DeviceOpenResult
    {
        Success,
        InputNotFound,
        OutputNotFound,
        InputOpenFailed,
        OutputOpenFailed
    }

    internal static async Task<(DeviceOpenResult, IMidiInput? midiInput, IMidiOutput? midiOutput)> TryOpen(
        string deviceSearchTerm)
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
                    return (DeviceOpenResult.InputOpenFailed, null, null);
                }

                break;
            }
        }

        if (midiInput == null)
        {
            midiOutput?.Dispose();
            midiOutput = null;
            return (DeviceOpenResult.InputNotFound, midiInput, midiOutput);
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
                    return (DeviceOpenResult.OutputOpenFailed, midiInput, midiOutput);
                }

                break;
            }
        }

        if (midiOutput == null)
        {
            await Console.Error.WriteLineAsync("Failed to open MIDI device output");
            midiInput.Dispose();
            midiInput = null;
            return (DeviceOpenResult.OutputNotFound, midiInput, midiOutput);
        }

        return (DeviceOpenResult.Success, midiInput, midiOutput);
    }
}