using System.Text;
using Commons.Music.Midi;
using Commons.Music.Midi.Alsa;
using Commons.Music.Midi.WinMM;

namespace Midi.Net;

public static partial class DeviceHandler
{
    /// <summary>
    /// Utility class to use IMidiAccess2 rather than the obsolete IMidiAccess
    /// Honestly this is completely unnecessary, but I was troubleshooting and it represents
    /// some knowledge I gained in the process. So it stays >:|
    /// </summary>
    private class MidiOpener : IMidiAccess2
    {
#pragma warning disable CS0618 // Type or member is obsolete
        private readonly IMidiAccess _access;
#pragma warning restore CS0618 // Type or member is obsolete
        private readonly MidiAccessExtensionManager? _extensionManager;
        // ReSharper disable once NotAccessedField.Local
        private readonly MidiPortCreatorExtension? _portCreatorExtension;

        public MidiOpener()
        {
            if (OperatingSystem.IsLinux())
            {
                var access = new AlsaMidiAccess();
                _extensionManager = access.ExtensionManager;
                _portCreatorExtension = _extensionManager.GetInstance<MidiPortCreatorExtension>();
                _access = access;
            }
            else if (OperatingSystem.IsWindows())
            {
                _access = new WinMMMidiAccess();
            }
            else
            {
                throw new PlatformNotSupportedException();
            }
        }

        public async Task<IMidiInput> OpenInputAsync(string portId)
        {
            var input = await _access.OpenInputAsync(portId);
            await LogAsync(input);
            return input;
        }

        private static async Task LogAsync(IMidiPort input)
        {
            var sb = new StringBuilder();
            sb.Append(input is IMidiInput ? "Input: " : "Output: ");
            AppendPortDetails(sb, input.Details);
            sb.Append("| Port status: ").Append(input.Connection);
            await Console.Out.WriteLineAsync(sb.ToString());
        }

        private static void AppendPortDetails(StringBuilder sb, IMidiPortDetails details)
        {
            sb.Append(details.Name)
                .Append(" (").Append(details.Id).Append(')')
                .Append("| Manufacturer: ").Append(details.Manufacturer)
                .Append("| Version: ").Append(details.Version);
        }

        public async Task<IMidiOutput> OpenOutputAsync(string portId)
        {
            var device = await _access.OpenOutputAsync(portId);
            await LogAsync(device);
            return device;
        }

        public IEnumerable<IMidiPortDetails> Inputs
        {
            get
            {
                var sb = new StringBuilder();
                var inputs = _access.Inputs.ToArray();
                foreach (var input in inputs)
                {
                    sb.Append("Found input device: ");
                    AppendPortDetails(sb, input);
                    Console.WriteLine(sb.ToString());
                    sb.Clear();
                }
                foreach (var input in inputs)
                {
                    yield return input;
                }
            }
        }

        public IEnumerable<IMidiPortDetails> Outputs
        {
            get
            {
                var sb = new StringBuilder();
                var outputs = _access.Outputs.ToArray();
                foreach (var output in outputs)
                {
                    sb.Append("Found output device: ");
                    AppendPortDetails(sb, output);
                    Console.WriteLine(sb.ToString());
                    sb.Clear();
                }
                
                foreach(var output in outputs)
                {
                    yield return output;
                }
            }
        }

        public event EventHandler<MidiConnectionEventArgs>? StateChanged;

        MidiAccessExtensionManager IMidiAccess2.ExtensionManager =>
            _extensionManager ?? throw new InvalidOperationException();
    }
}