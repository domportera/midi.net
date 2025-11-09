using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
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

    public static T Create<T>(string? deviceSearchTerm = null,
        SearchMode searchMode = SearchMode.UseDeviceName | SearchMode.Contains | SearchMode.CaseInsensitive)
        where T : class, IMidiDevice, new()
    {
        deviceSearchTerm ??= typeof(T).Name;
        var device = new T
        {
            MidiDevice = new MidiDevice(MidiAccess, 
                inputTerm: new DeviceSearchTerm(deviceSearchTerm, searchMode), 
                outputTerm: new DeviceSearchTerm(deviceSearchTerm, searchMode))
        };
        
        return device;
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

    private static Result<T> NotFound<T>() where T : class, IMidiPort => ResultFactory.Fail<T>("Not found");

    [SuppressMessage("ReSharper", "PossibleMultipleEnumeration")]
    private static async Task<Result<T>> TryOpenPort<T>(DeviceSearchTerm deviceSearchTerm)
        where T : class, IMidiPort, IDisposable
    {
        AssertPortType<T>();

        var portResult = NotFound<T>();

        IEnumerable<IMidiPortDetails> details = typeof(T) == typeof(IMidiInput) 
            ? MidiAccess.Inputs 
            : MidiAccess.Outputs;

        foreach (var port in details)
        {
            if (!Matches(deviceSearchTerm, port)) continue;
            portResult = await TryOpenPort<T>(port);
            break;
        }

        if (portResult == NotFound<T>() && deviceSearchTerm.Flags.Has(SearchMode.MatchAnyAsFallback))
        {
            // we can use a fallback device
            // todo: heuristics on the "best" device to use
            if (details.Any())
            {
                portResult = await TryOpenPort<T>(details.First());
            }
        }

        if (!portResult.Success)
        {
            portResult.Value?.Dispose();
            return ResultFactory.Fail<T>(portResult.Message);
        }

        Debug.Assert(portResult.Value != null);
        return ResultFactory.Success(portResult.Value);
    }

    private static void AssertPortType<T>() where T : IMidiPort, IDisposable
    {
        if (typeof(T) != typeof(IMidiInput) && typeof(T) != typeof(IMidiOutput))
        {
            throw new ArgumentException(
                $"Type provided ({typeof(T)}) must be either {typeof(IMidiInput)} or {typeof(IMidiOutput)}");
        }
    }


    private static async Task<Result<T>> TryOpenPort<T>(IMidiPortDetails details) where T : class, IMidiPort, IDisposable
    {
        AssertPortType<T>();
        try
        {
            IMidiPort port = typeof(T) == typeof(IMidiInput)
                ? await MidiAccess.OpenInputAsync(details.Id)
                : await MidiAccess.OpenOutputAsync(details.Id);

            return ResultFactory.Success((T)port);
        }
        catch (Exception e)
        {
            return ResultFactory.Fail<T>(e.ToString());
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