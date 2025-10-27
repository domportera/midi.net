using System.Reflection.Metadata;
using Commons.Music.Midi;

namespace Midi.Net;

public static partial class DeviceHandler
{
    private static readonly IMidiAccess2 MidiAccess = new MidiOpener();

    [Flags]
    public enum SearchMode
    {
        None = 0,
        UseDeviceName = 1,
        UseDeviceManufacturer = 1 << 1,
        UseDeviceId = 1 << 2,
        UseDeviceVersion = 1 << 3,
        MaxSearchTermMode = UseDeviceVersion,
        UseAll = UseDeviceName | UseDeviceManufacturer | UseDeviceId | UseDeviceVersion,

        CaseInsensitive = 1 << 29,
        Contains = 1 << 30,
        MatchAnyAsFallback = 1 << 31
    }
    
    public static async Task<DeviceOpenResult<T>> TryOpen<T>(string? deviceSearchTerm = null,
        SearchMode searchMode = SearchMode.UseDeviceName | SearchMode.Contains | SearchMode.CaseInsensitive)
        where T : IMidiDevice, new()
    {
        deviceSearchTerm ??= typeof(T).Name;

        var searchTerm = new DeviceSearchTerm(deviceSearchTerm, searchMode);

        var details = await TryOpen(searchTerm, searchMode);
        T? device = default;
        if (details.IsSuccess)
        {
            device = new T
            {
                MidiDevice = new MidiDevice
                {
                    Input = new DeviceOpenResult().Input.Port!,
                    Output = new DeviceOpenResult().Output.Port!
                }
            };
        }
        
        return new DeviceOpenResult<T>(device, details);
    }

    private readonly record struct DeviceSearchTerm(string Term, SearchMode Flags);
    private static readonly PortOpenResult<IMidiInput> InputNotFound = new(PortOpenStatus.NotFound, null, null);
    private static readonly PortOpenResult<IMidiOutput> OutputNotFound = new(PortOpenStatus.NotFound, null, null);

    private static async Task<DeviceOpenResult> TryOpen(DeviceSearchTerm deviceSearchTerm,
        SearchMode searchMode)
    {
        var midiInput = InputNotFound;
        var midiOutput = OutputNotFound;

        var inputs = MidiAccess.Inputs.Where(x => x != null).ToArray();
        foreach (var input in inputs)
        {
            if (!Matches(deviceSearchTerm, searchMode, input)) continue;
            midiInput = await TryOpenInput(input);
            break;
        }
        
        var outputs = MidiAccess.Outputs.ToArray();
        foreach (var output in outputs)
        {
            if (!Matches(deviceSearchTerm, searchMode, output)) continue;
            midiOutput = await TryOpenOutput(output);
            break;
        }


        if (searchMode.Has(SearchMode.MatchAnyAsFallback))
        {
            // we can use a fallback device
            // todo: heuristics on the "best" device to use
            if (midiInput == InputNotFound && inputs.Length > 0)
            {
                var fallbackInput = inputs[0];
            }

            if (midiOutput == OutputNotFound && outputs.Length > 0)
            {
                var fallbackOutput = outputs[0];
            }
        }


        return new DeviceOpenResult(midiInput, midiOutput);


        static async Task<PortOpenResult<IMidiInput>> TryOpenInput(IMidiPortDetails details)
        {
            try
            {
                var port = await MidiAccess.OpenInputAsync(details.Id);
                var status = port.Connection switch
                {
                    MidiPortConnectionState.Open => PortOpenStatus.Success,
                    MidiPortConnectionState.Pending => PortOpenStatus.OpenPending,
                    _ => PortOpenStatus.OpenFailed
                };
                
                return new PortOpenResult<IMidiInput>(status, port, details);
            }
            catch (Exception e)
            {
                return new PortOpenResult<IMidiInput>(PortOpenStatus.OpenFailed, null, details);
            }
        }

        static async Task<PortOpenResult<IMidiOutput>> TryOpenOutput(IMidiPortDetails details)
        {
            try
            {
                var port = await MidiAccess.OpenOutputAsync(details.Id);
                var status = port.Connection switch
                {
                    MidiPortConnectionState.Open => PortOpenStatus.Success,
                    MidiPortConnectionState.Pending => PortOpenStatus.OpenPending,
                    _ => PortOpenStatus.OpenFailed
                };
                
                return new PortOpenResult<IMidiOutput>(status, port, details);
            }
            catch (Exception e)
            {
                return new PortOpenResult<IMidiOutput>(PortOpenStatus.OpenFailed, null, details);
            }
        }
    }

    private static bool Matches(DeviceSearchTerm searchTerm, SearchMode searchMode, IMidiPortDetails details)
    {
        var flags = searchTerm.Flags & searchMode;
        if (flags == SearchMode.None)
            return false;

        var strict = (flags & SearchMode.Contains) != SearchMode.Contains;
        var caseSensitive = (flags & SearchMode.CaseInsensitive) != SearchMode.CaseInsensitive;

        var term = searchTerm.Term;

        // check flags and check for match accordingly
        const int maxSearchTermMode = (int)SearchMode.MaxSearchTermMode;
        for (int i = 1; i <= maxSearchTermMode; i <<= 1)
        {
            var mode = (SearchMode)i;
            if (flags.Has(mode) && Match(GetString(details, mode), term, caseSensitive, strict))
            {
                return true;
            }
        }

        return false;


        static bool Match(string a, string b, bool caseSensitive, bool strict)
        {
            if (a == b)
                return true;

            if (caseSensitive && strict)
                return false;

            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            return strict ? a.Equals(b, comparison) : a.Contains(b, comparison);
        }

        static string GetString(IMidiPortDetails details, SearchMode searchMode)
        {
            return searchMode switch
            {
                SearchMode.UseDeviceName => details.Name,
                SearchMode.UseDeviceManufacturer => details.Manufacturer,
                SearchMode.UseDeviceId => details.Id,
                SearchMode.UseDeviceVersion => details.Version,
                _ => throw new ArgumentOutOfRangeException(nameof(searchMode), searchMode, null)
            };
        }
    }


    private static bool Has(this SearchMode searchMode, SearchMode flag) => (searchMode & flag) != 0;
}