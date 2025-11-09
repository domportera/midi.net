using Commons.Music.Midi;

namespace Midi.Net;

/// <summary>
/// A wrapper class to maintain a single object reference for a given device input
/// </summary>
public abstract class MidiSlot : IMidiPort, IDisposable, IAsyncDisposable
{
    public event AsyncEventHandler<bool>? ConnectionStateChanged;

    private DeviceHandler.DeviceSearchTerm _deviceSearchTerms;

    // placeholder to uphold nullable contract
    private class DefaultMidiPortDetails : IMidiPortDetails
    {
        public string Id => "";
        public string Manufacturer => "";
        public string Name => "";
        public string Version => "";
    }

    private readonly IMidiAccess2 _midiAccess;

    protected MidiSlot(IMidiAccess2 midiAccess2, DeviceHandler.DeviceSearchTerm deviceSearchTerms)
    {
        _midiAccess = midiAccess2;
        _deviceSearchTerms = deviceSearchTerms;
        Details = new DefaultMidiPortDetails();
    }

    public void ChangeSearchTerm(DeviceHandler.DeviceSearchTerm newSearchTerm) => _deviceSearchTerms = newSearchTerm;


    public void BeginConnect()
    {
        if (_cts != null)
            throw new Exception("Connection thread already running");

        _cts = new CancellationTokenSource();

        var thread = new Thread(async void (obj) =>
        {
            IMidiPort? port = null;
            const int tryIntervalMs = 1000;
            try
            {
                var token = (CancellationToken)obj!;
                using var resetEvent = new ManualResetEvent(false);
                do
                {
                    var result = await BeginConnectTask(_deviceSearchTerms);
                    if (!result.Success)
                    {
                        // wait a bit first and try again
                        resetEvent.WaitOne(tryIntervalMs);
                        resetEvent.Reset();
                        continue;
                    }

                    Connection = MidiPortConnectionState.Open;

                    port = result.Value;
                    Details = port.Details;

                    OnConnectionStateChanged(port);

                    await Console.Out.WriteLineAsync($"Connected to {port.Details.Name}");

                    try
                    {
                        if (ConnectionStateChanged != null)
                        {
                            await ConnectionStateChanged.Invoke(this, true);
                        }
                    }
                    catch (Exception ex)
                    {
                        await Console.Error.WriteLineAsync(ex.ToString());
                    }

                    // Wait indefinitely while device is open - we can try to reconnect if it closes
                    while (!token.IsCancellationRequested && port.Connection == MidiPortConnectionState.Open)
                    {
                        // todo - wait better? should at least profile it
                        resetEvent.WaitOne(tryIntervalMs);
                        resetEvent.Reset();

                        // extra dumb logic bc disconnections don't seem to be properly implemented in ManagedMidi
                        var shouldExit = false;
                        switch (port)
                        {
                            case IMidiInput:
                                shouldExit = _midiAccess.Inputs.All(x => x.Id != Details.Id);
                                break;
                            case IMidiOutput:
                                shouldExit = _midiAccess.Outputs.All(x => x.Id != Details.Id);
                                break;
                            default:
                                shouldExit = true;
                                break;
                        }

                        if (shouldExit)
                        {
                            break;
                        }
                    }

                    await Disconnect(port);
                } while (!token.IsCancellationRequested);
            }
            catch (OperationCanceledException)
            {
                if (Connection == MidiPortConnectionState.Open)
                {
                    await Disconnect(port);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine(e);
            }
        })
        {
            Name = "Midi Slot Wrapper Connect Thread"
        };

        thread.Start(_cts.Token);
        return;

        async ValueTask Disconnect(IMidiPort? port)
        {
            Connection = MidiPortConnectionState.Closed;

            try
            {
                if (ConnectionStateChanged != null)
                    await ConnectionStateChanged.Invoke(this, false);
            }
            catch (Exception ex)
            {
                await Console.Error.WriteLineAsync(ex.ToString());
            }

            if (port != null)
            {
                try
                {
                    await port.CloseAsync();
                }
                catch (Exception ex)
                {
                    await Console.Error.WriteLineAsync(ex.ToString());
                }
            }

            await Console.Error.WriteLineAsync("Midi device disconnected");
        }
    }

    protected abstract Task<Result<IMidiPort>> BeginConnectTask(DeviceHandler.DeviceSearchTerm searchTerm);

    public async Task CloseAsync()
    {
        if (_cts != null)
        {
            await _cts.CancelAsync();
            _cts.Dispose();
            _cts = null;
        }
    }

    protected abstract void OnConnectionStateChanged(IMidiPort? port);

    public IMidiPortDetails Details { get; private set; }
    public MidiPortConnectionState Connection { get; private set; }


    void IDisposable.Dispose()
    {
        GC.SuppressFinalize(this);
        DisposeAsync(true).AsTask().Wait();
    }

    protected abstract void OnDispose(bool disposing);

    ~MidiSlot()
    {
        Console.Error.WriteLine("MidiSlotWrapper finalized without being disposed");
        DisposeAsync(false).AsTask().Wait();
    }

    public ValueTask DisposeAsync()
    {
        GC.SuppressFinalize(this);
        return DisposeAsync(true);
    }

    private async ValueTask DisposeAsync(bool disposing)
    {
        OnDispose(disposing);
        await CloseAsync();
    }

    private CancellationTokenSource? _cts;
}