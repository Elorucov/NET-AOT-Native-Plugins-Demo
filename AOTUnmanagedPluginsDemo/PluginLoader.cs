using System.Runtime.InteropServices;

namespace AOTUnmanagedPluginsDemo
{
    /// <summary>
    /// Loads an unmanaged plugin library and provides type-safe wrappers for its functions
    ///
    /// Using NativeLibrary and function pointers (delegate* unmanaged) because they are compatible with NativeAOT
    /// </summary>
    internal sealed class PluginLoader : IDisposable
    {
        // Function pointers
        private unsafe delegate* unmanaged[Cdecl]<int> _version;
        private unsafe delegate* unmanaged[Cdecl]<long, long, long> _compute;
        private unsafe delegate* unmanaged[Cdecl]<byte*, byte*, int, int> _greet;

        private readonly nint _handle;
        private bool _disposed;

        public PluginLoader(string libraryPath)
        {
            _handle = NativeLibrary.Load(libraryPath);
            LoadSymbols();
        }

        private unsafe void LoadSymbols()
        {
            _version = (delegate* unmanaged[Cdecl]<int>)GetExport("plugin_version");
            _compute = (delegate* unmanaged[Cdecl]<long, long, long>)GetExport("plugin_compute");
            _greet = (delegate* unmanaged[Cdecl]<byte*, byte*, int, int>)GetExport("plugin_greet");
        }

        private nint GetExport(string name) =>
            NativeLibrary.GetExport(_handle, name);

        //  Sync wrappers

        /// <summary>Gets the plugin version.</summary>
        public unsafe int Version()
        {
            ThrowIfDisposed();
            return _version();
        }

        /// <summary>
        /// "Heavy" calculation in the plugin (with delay in plugin).
        /// </summary>
        public unsafe long ComputeSync(long a, long b)
        {
            ThrowIfDisposed();
            return _compute(a, b);
        }

        /// <summary>
        /// Writes the greeting to a managed string.
        /// Returns null on buffer error.
        /// </summary>
        public unsafe string Greet(string name)
        {
            ThrowIfDisposed();

            // Converts managed string to UTF-8 bytes in stack (stackalloc).
            // For AOT, it's safer than Marshal.StringToHGlobalAnsi.
            int nameBytes = System.Text.Encoding.UTF8.GetByteCount(name);
            Span<byte> nameUtf8 = nameBytes <= 256
                ? stackalloc byte[nameBytes + 1]
                : new byte[nameBytes + 1];

            System.Text.Encoding.UTF8.GetBytes(name, nameUtf8);
            nameUtf8[nameBytes] = 0; // null terminator

            const int BufSize = 512;
            Span<byte> outBuf = stackalloc byte[BufSize];

            int written;
            fixed (byte* pName = nameUtf8)
            fixed (byte* pOut = outBuf)
            {
                written = _greet(pName, pOut, BufSize);
            }

            if (written < 0) return null;
            return System.Text.Encoding.UTF8.GetString(outBuf[..written]);
        }

        //  Async wrappers
        //  Native call can block the thread for an arbitrary amount of time.
        //  We wrap it in Task.Run to:
        //    1. not block the UI/event-loop thread;
        //    2. allow awaiting the result with CancellationToken.

        /// <summary>
        /// Async version of ComputeSync.
        /// </summary>
        public Task<long> ComputeAsync(long a, long b, CancellationToken ct = default)
        {
            ThrowIfDisposed();
            ct.ThrowIfCancellationRequested();

            return Task.Run(() =>
            {
                ct.ThrowIfCancellationRequested(); // checking before the native call
                long result = ComputeSync(a, b);
                ct.ThrowIfCancellationRequested(); // and after
                return result;
            }, ct);
        }

        //  IDisposable
        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
            NativeLibrary.Free(_handle);
        }

        private void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }
    }
}
