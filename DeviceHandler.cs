using System.Diagnostics;
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

    public static async Task<Result<T>> TryOpen<T>(string? deviceSearchTerm = null,
        SearchMode searchMode = SearchMode.UseDeviceName | SearchMode.Contains | SearchMode.CaseInsensitive)
        where T : class, IMidiDevice, new()
    {
        deviceSearchTerm ??= typeof(T).Name;
        var input = await TryOpenInput(deviceSearchTerm, searchMode);
        var output = await TryOpenOutput(deviceSearchTerm, searchMode);

        if (!input.Success || !output.Success)
        {
            if (input.Success)
            {
                input.Value?.Dispose();
            }

            if (output.Success)
            {
                output.Value?.Dispose();
            }
            
            return new Result<T>(null, false, $"Input: {input.Message}\n\nOutput: {output.Message}");
        }
        
        var device = new T
        {
            MidiDevice = new MidiDevice
            {
                Input = input.Value!,
                Output = output.Value!
            }
        };

        try
        {
            await device.OnConnect();
        }
        catch (Exception ex)
        {
            await device.CloseAsync();
            return new Result<T>(null, false, $"Failure in post-connection method: {ex}");
        }

        device.MidiDevice.Reconnected += (_,_) => _ = device.OnConnect();
        return new Result<T>(device, true, null);
    }

    public readonly record struct DeviceSearchTerm(string Term, SearchMode Flags);

    public static async Task<Result<IMidiInput>> TryOpenInput(string? searchTerm = null,
        SearchMode searchMode = DefaultSearchMode) =>
        await TryOpenPort<IMidiInput>(new DeviceSearchTerm(searchTerm ?? "", searchMode));

    public static async Task<Result<IMidiOutput>> TryOpenOutput(string? searchTerm = null,
        SearchMode searchMode = DefaultSearchMode) =>
        await TryOpenPort<IMidiOutput>(new DeviceSearchTerm(searchTerm ?? "", searchMode));

    public static async Task<Result<IMidiInput>> TryOpenInput(DeviceSearchTerm deviceSearchTerm) =>
        await TryOpenPort<IMidiInput>(deviceSearchTerm);

    public static async Task<Result<IMidiOutput>> TryOpenOutput(DeviceSearchTerm deviceSearchTerm) =>
        await TryOpenPort<IMidiOutput>(deviceSearchTerm);

    public static async Task<Result<IMidiInput>> TryOpenInput(IMidiPortDetails details) =>
        await TryOpenPort<IMidiInput>(details);

    public static async Task<Result<IMidiOutput>> TryOpenOutput(IMidiPortDetails details) =>
        await TryOpenPort<IMidiOutput>(details);

    private static Result<T> NotFound<T>() where T : IMidiPort => new(default, false, "Not found");

    private static async Task<Result<T>> TryOpenPort<T>(DeviceSearchTerm deviceSearchTerm)
        where T : IMidiPort, IDisposable
    {
        AssertPortType<T>();

        var midiInput = NotFound<T>();

        var inputs = MidiAccess.Inputs.Where(x => x != null).ToArray();
        foreach (var input in inputs)
        {
            if (!Matches(deviceSearchTerm, input)) continue;
            midiInput = await TryOpenPort<T>(input);
            break;
        }

        if (deviceSearchTerm.Flags.Has(SearchMode.MatchAnyAsFallback))
        {
            // we can use a fallback device
            // todo: heuristics on the "best" device to use
            if (midiInput == NotFound<T>() && inputs.Length > 0)
            {
                var fallbackInput = inputs[0];
                midiInput = await TryOpenPort<T>(fallbackInput);
            }
        }


        if (!midiInput.Success)
        {
            midiInput.Value?.Dispose();
            return new Result<T>(default, false, midiInput.Message);
        }

        Debug.Assert(midiInput.Value != null);
        return new Result<T>(midiInput.Value, true, null);
    }

    private static void AssertPortType<T>() where T : IMidiPort, IDisposable
    {
        if (typeof(T) != typeof(IMidiInput) && typeof(T) != typeof(IMidiOutput))
        {
            throw new ArgumentException(
                $"Type provided ({typeof(T)}) must be either {typeof(IMidiInput)} or {typeof(IMidiOutput)}");
        }
    }


    private static async Task<Result<T>> TryOpenPort<T>(IMidiPortDetails details) where T : IMidiPort, IDisposable
    {
        AssertPortType<T>();
        try
        {
            IMidiPort port = typeof(T) == typeof(IMidiInput)
                ? await MidiAccess.OpenInputAsync(details.Id)
                : await MidiAccess.OpenOutputAsync(details.Id);
            var status = port.Connection switch
            {
                MidiPortConnectionState.Open => PortOpenStatus.Success,
                MidiPortConnectionState.Pending => PortOpenStatus.OpenPending,
                _ => PortOpenStatus.OpenFailed
            };

            return new Result<T>((T)port, true, null);
        }
        catch (Exception e)
        {
            return new Result<T>(default, false, e.ToString());
        }
    }

    private static bool Matches(DeviceSearchTerm searchTerm, IMidiPortDetails details)
    {
        if (string.IsNullOrWhiteSpace(searchTerm.Term))
        {
            if (!searchTerm.Flags.Has(SearchMode.MatchAnyAsFallback))
            {
                Console.Error.WriteLine($"No search term provided, and search mode does not include " +
                                        $"{SearchMode.MatchAnyAsFallback} - is this intended?");
            }

            return true;
        }

        var flags = searchTerm.Flags;
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

    private const SearchMode DefaultSearchMode =
        SearchMode.UseDeviceName | SearchMode.Contains | SearchMode.CaseInsensitive;
}