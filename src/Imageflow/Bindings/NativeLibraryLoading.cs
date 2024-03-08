using System.Collections.Concurrent;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using Imageflow.Internal.Helpers;
#if !NET8_0_OR_GREATER
using System.Reflection;
#endif
namespace Imageflow.Bindings;

internal class LoadLogger : ILibraryLoadLogger
{
    internal string Verb = "loaded";
    internal string Filename = $"{RuntimeFileLocator.SharedLibraryPrefix.Value}";

    internal Exception? FirstException;
    internal Exception? LastException;

    private readonly List<LogEntry> _log = new List<LogEntry>(7);

    private struct LogEntry
    {
        internal string Basename;
        internal string? FullPath;
        internal bool FileExists;
        internal bool PreviouslyLoaded;
        internal int? LoadErrorCode;
    }

    public void NotifyAttempt(string basename, string? fullPath, bool fileExists, bool previouslyLoaded,
        int? loadErrorCode)
    {
        Argument.ThrowIfNull(basename);
        _log.Add(new LogEntry
        {
            Basename = basename,
            FullPath = fullPath,
            FileExists = fileExists,
            PreviouslyLoaded = previouslyLoaded,
            LoadErrorCode = loadErrorCode
        });
    }

    internal void RaiseException()
    {
        var sb = new StringBuilder(_log.Select((e) => e.Basename.Length + (e.FullPath?.Length ?? 0) + 20)
            .Sum());
        sb.AppendFormat(CultureInfo.InvariantCulture, "Looking for \"{0}\" RID=\"{1}-{2}\", IsUnix={3}, IsDotNetCore={4} RelativeSearchPath=\"{5}\"\n",
            Filename,
            RuntimeFileLocator.PlatformRuntimePrefix.Value,
            RuntimeFileLocator.ArchitectureSubdir.Value, RuntimeFileLocator.IsUnix,
            RuntimeFileLocator.IsDotNetCore.Value,
            AppDomain.CurrentDomain.RelativeSearchPath);
        if (FirstException != null)
        {
            sb.AppendFormat(CultureInfo.InvariantCulture, "Before searching: {0}\n", FirstException.Message);
        }

        foreach (var e in _log)
        {
            if (e.PreviouslyLoaded)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "\"{0}\" is already {1}", e.Basename, Verb);
            }
            else if (!e.FileExists)
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "File not found: {0}", e.FullPath);
            }
            else if (e.LoadErrorCode.HasValue)
            {
                string errorCode = e.LoadErrorCode.Value < 0
                    ? string.Format(CultureInfo.InvariantCulture, "0x{0:X8}", e.LoadErrorCode.Value)
                    : e.LoadErrorCode.Value.ToString(CultureInfo.InvariantCulture);

                sb.AppendFormat(CultureInfo.InvariantCulture, "Error \"{0}\" ({1}) loading {2} from {3}",
                    new Win32Exception(e.LoadErrorCode.Value).Message,
                    errorCode,
                    e.Basename, e.FullPath);

                if (e.LoadErrorCode.Value == 193 &&
                    RuntimeFileLocator.PlatformRuntimePrefix.Value == "win")
                {
                    var installed = Environment.Is64BitProcess ? "32-bit (x86)" : "64-bit (x86_64)";
                    var needed = Environment.Is64BitProcess ? "64-bit (x86_64)" : "32-bit (x86)";

                    sb.AppendFormat(CultureInfo.InvariantCulture, "\n> You have installed a {0} copy of imageflow.dll but need the {1} version",
                        installed, needed);
                }

                if (e.LoadErrorCode.Value == 126 &&
                    RuntimeFileLocator.PlatformRuntimePrefix.Value == "win")
                {
                    var crtLink = "https://aka.ms/vs/16/release/vc_redist."
                                  + (Environment.Is64BitProcess ? "x64.exe" : "x86.exe");

                    sb.AppendFormat(CultureInfo.InvariantCulture, "\n> You may need to install the C Runtime from {0}", crtLink);
                }
            }
            else
            {
                sb.AppendFormat(CultureInfo.InvariantCulture, "{0} {1} in {2}", Verb, e.Basename, e.FullPath);
            }
            sb.Append('\n');
        }
        if (LastException != null)
        {
            sb.AppendLine(LastException.Message);
        }

        var stackTrace = (FirstException ?? LastException)?.StackTrace;
        if (stackTrace != null)
        {
            sb.AppendLine(stackTrace);
        }

        throw new DllNotFoundException(sb.ToString());
    }
}

internal static class RuntimeFileLocator
{
    internal static bool IsUnix => Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

    internal static readonly Lazy<string> SharedLibraryPrefix = new Lazy<string>(() => IsUnix ? "lib" : "", LazyThreadSafetyMode.PublicationOnly);
#if NET8_0_OR_GREATER
    internal static readonly Lazy<bool> IsDotNetCore = new Lazy<bool>(() =>
            true
        , LazyThreadSafetyMode.PublicationOnly);
#else
    internal static readonly Lazy<bool> IsDotNetCore = new Lazy<bool>(() =>
            typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.CodeBase.Contains("Microsoft.NETCore.App")
        , LazyThreadSafetyMode.PublicationOnly);
#endif
    internal static readonly Lazy<string> PlatformRuntimePrefix = new Lazy<string>(() =>
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.MacOSX:
                return "osx";
            case PlatformID.Unix:
                return "linux";
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
            case PlatformID.Xbox:
                return "win";
            default:
                return "win";
        }
    }, LazyThreadSafetyMode.PublicationOnly);

    internal static readonly Lazy<string> SharedLibraryExtension = new Lazy<string>(() =>
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.MacOSX:
                return "dylib";
            case PlatformID.Unix:
                return "so";
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
            case PlatformID.Xbox:
                return "dll";
            default:
                return "dll";
        }
    }, LazyThreadSafetyMode.PublicationOnly);

    internal static readonly Lazy<string> ExecutableExtension = new Lazy<string>(() =>
    {
        switch (Environment.OSVersion.Platform)
        {
            case PlatformID.MacOSX:
                return "";
            case PlatformID.Unix:
                return "";
            case PlatformID.Win32NT:
            case PlatformID.Win32S:
            case PlatformID.Win32Windows:
            case PlatformID.WinCE:
            case PlatformID.Xbox:
                return "dll";
            default:
                return "dll";
        }
    }, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// The output subdirectory that NuGet .props/.targets should be copying unmanaged binaries to.
    /// If you're using .NET Core you don't need this.
    /// </summary>
    internal static readonly Lazy<string> ArchitectureSubdir = new Lazy<string>(() =>
    {
        // ReSharper disable once InvertIf
        if (!IsUnix)
        {
            var architecture = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
            if (string.Equals(architecture, "ia64", StringComparison.OrdinalIgnoreCase))
            {
                return "ia64";
            }
            if (string.Equals(architecture, "arm", StringComparison.OrdinalIgnoreCase))
            {
                return Environment.Is64BitProcess ? "arm64" : "arm";
            }
            // We don't currently support unlisted/unknown architectures. We default to x86/x64 as backup
        }
        //TODO: Add support for arm/arm64 on linux
        return Environment.Is64BitProcess ? "x64" : "x86";
    }, LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Enumerates a set of folders to search within. If the boolean value of the tuple is true,
    /// specific subdirectories can be searched.
    /// </summary>
    /// <param name="customSearchDirectories"></param>
    /// <returns></returns>
    private static IEnumerable<Tuple<bool, string>> BaseFolders(IEnumerable<string>? customSearchDirectories = null)
    {
        // Prioritize user suggestions
        if (customSearchDirectories != null)
        {
            foreach (var d in customSearchDirectories)
            {
                yield return Tuple.Create(true, d);
            }
        }

        // First look in AppDomain.CurrentDomain.RelativeSearchPath - if it is within the BaseDirectory
        if (!string.IsNullOrEmpty(AppDomain.CurrentDomain.RelativeSearchPath) &&
            AppDomain.CurrentDomain.RelativeSearchPath.StartsWith(AppDomain.CurrentDomain.BaseDirectory))
        {
            yield return Tuple.Create(true, AppDomain.CurrentDomain.RelativeSearchPath);
        }
        // look in System.AppContext.BaseDirectory
        if (!string.IsNullOrEmpty(AppContext.BaseDirectory))
        {
            yield return Tuple.Create(true, AppContext.BaseDirectory);
        }

        // Look in the base directory from which .NET looks for managed assemblies
        yield return Tuple.Create(true, AppDomain.CurrentDomain.BaseDirectory);

        //Issue #17 - Azure Functions 2.0 - https://github.com/imazen/imageflow-dotnet/issues/17
        // If the BaseDirectory is /bin/, look one step outside of it.
        if (AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).EndsWith("bin"))
        {
            //Look in the parent directory if we're in /bin/, but only look in ../runtimes/:rid:/native
            var dir = Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory);
            if (dir != null)
            {
                yield return Tuple.Create(false, Path.Combine(dir.FullName,
                    "runtimes", PlatformRuntimePrefix.Value + "-" + ArchitectureSubdir.Value, "native"));
            }
        }

        string? assemblyLocation = null;
#if !NETCOREAPP && !NET5_0_OR_GREATER && !NET8_0_OR_GREATER
        try{
            // Look in the folder that *this* assembly is located.
            assemblyLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

        } catch (NotImplementedException){
            // ignored
        }
#endif
        if (!string.IsNullOrEmpty(assemblyLocation))
        {
            yield return Tuple.Create(true, assemblyLocation!);
        }
    }

    internal static IEnumerable<string> SearchPossibilitiesForFile(string filename, IEnumerable<string>? customSearchDirectories = null)
    {
        var attemptedPaths = new HashSet<string>();
        foreach (var t in BaseFolders(customSearchDirectories))
        {
            if (string.IsNullOrEmpty(t.Item2))
            {
                continue;
            }

            var directory = Path.GetFullPath(t.Item2);
            var searchSubDirs = t.Item1;
            // Try architecture-specific subdirectories first
            string path;

            if (searchSubDirs)
            {
                // First try the simple arch subdir since that is where the nuget native packages unpack
                path = Path.Combine(directory, ArchitectureSubdir.Value, filename);
                if (attemptedPaths.Add(path))
                {
                    yield return path;
                }
            }

            // Try the folder itself
            path = Path.Combine(directory, filename);

            if (attemptedPaths.Add(path))
            {
                yield return path;
            }

            if (searchSubDirs)
            {
                // Last try native runtimes directory in case this is happening in .NET Core
                path = Path.Combine(directory, "runtimes",
                    PlatformRuntimePrefix.Value + "-" + ArchitectureSubdir.Value, "native", filename);
                if (attemptedPaths.Add(path))
                {
                    yield return path;
                }
            }

        }
    }
}

internal interface ILibraryLoadLogger
{
    void NotifyAttempt(string basename, string? fullPath, bool fileExists, bool previouslyLoaded, int? loadErrorCode);
}

public static class ExecutableLocator
{

    private static string GetFilenameWithoutDirectory(string basename) => RuntimeFileLocator.ExecutableExtension.Value.Length > 0
        ? $"{basename}.{RuntimeFileLocator.ExecutableExtension.Value}"
        : basename;

    /// <summary>
    /// Raises an exception if the file couldn't be found
    /// </summary>
    /// <param name="basename"></param>
    /// <param name="customSearchDirectories"></param>
    /// <returns></returns>
    public static string? FindExecutable(string basename, IEnumerable<string>? customSearchDirectories = null)
    {
        var logger = new LoadLogger { Verb = "located", Filename = GetFilenameWithoutDirectory(basename) };
        if (TryLoadByBasename(basename, logger, out var exePath, customSearchDirectories))
        {
            return exePath;
        }
        logger.RaiseException();
        return null;
    }

    private static readonly Lazy<ConcurrentDictionary<string, string>> ExecutablePathsByName = new Lazy<ConcurrentDictionary<string, string>>(() => new ConcurrentDictionary<string, string>(StringComparer.OrdinalIgnoreCase), LazyThreadSafetyMode.PublicationOnly);

    // Not yet implemented.
    // static readonly Lazy<ConcurrentDictionary<string, IntPtr>> LibraryHandlesByFullPath = new Lazy<ConcurrentDictionary<string, IntPtr>>(() => new ConcurrentDictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase), LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Searches known directories for the provided file basename (or returns true if one is already loaded)
    /// basename 'imageflow' -> imageflow.exe, imageflow
    /// Basename is case-sensitive
    /// </summary>
    /// <param name="basename">The executable name sans extension</param>
    /// <param name="log">Where to log attempts at assembly search and load</param>
    /// <param name="exePath"></param>
    /// <param name="customSearchDirectories">Provide this if you want a custom search folder</param>
    /// <returns>True if previously or successfully loaded</returns>
    internal static bool TryLoadByBasename(string basename, ILibraryLoadLogger log, out string? exePath,
        IEnumerable<string>? customSearchDirectories = null)
    {
        Argument.ThrowIfNull(basename);

        if (ExecutablePathsByName.Value.TryGetValue(basename, out exePath))
        {
            return true;
        }

        var filename = GetFilenameWithoutDirectory(basename);

        exePath = null;
        foreach (var path in RuntimeFileLocator.SearchPossibilitiesForFile(filename, customSearchDirectories))
        {
            if (!File.Exists(path))
            {
                log.NotifyAttempt(basename, path, false, false, 0);
            }
            else
            {
                exePath = path;
                ExecutablePathsByName.Value[basename] = exePath;
                return true;
            }
        }
        return false;
    }
}

internal static class NativeLibraryLoader
{
    private static string GetFilenameWithoutDirectory(string basename) => $"{RuntimeFileLocator.SharedLibraryPrefix.Value}{basename}.{RuntimeFileLocator.SharedLibraryExtension.Value}";

    /// <summary>
    /// Attempts to resolve DllNotFoundException and BadImageFormatExceptions
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="basename"></param>
    /// <param name="invokingOperation"></param>
    /// <param name="customSearchDirectories"></param>
    /// <returns></returns>
    public static T? FixDllNotFoundException<T>(string basename, Func<T> invokingOperation,
        IEnumerable<string>? customSearchDirectories = null)
    {
        // It turns out that trying to do it "before" is 4-5x slower in cases where the standard loading mechanism works
        // And catching the DllNotFoundException does not seem to measurably slow us down. So no "preventative" stuff.
        Exception? caughtException;
        try
        {
            return invokingOperation();
        }
        catch (BadImageFormatException a)
        {
            caughtException = a;
        }
        catch (DllNotFoundException b)
        {
            caughtException = b;
        }

        //Try loading
        var logger = new LoadLogger
        { FirstException = caughtException, Filename = GetFilenameWithoutDirectory(basename) };
        if (TryLoadByBasename(basename, logger, out _, customSearchDirectories))
        {
            try
            {
                return invokingOperation();
            }
            catch (DllNotFoundException last)
            {
                logger.LastException = last;
            }
        }
        logger.RaiseException();
        return default;
    }

    private static readonly Lazy<ConcurrentDictionary<string, IntPtr>> LibraryHandlesByBasename = new Lazy<ConcurrentDictionary<string, IntPtr>>(() => new ConcurrentDictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase), LazyThreadSafetyMode.PublicationOnly);

    // Not yet implemented.
    // static readonly Lazy<ConcurrentDictionary<string, IntPtr>> LibraryHandlesByFullPath = new Lazy<ConcurrentDictionary<string, IntPtr>>(() => new ConcurrentDictionary<string, IntPtr>(StringComparer.OrdinalIgnoreCase), LazyThreadSafetyMode.PublicationOnly);

    /// <summary>
    /// Searches known directories for the provided file basename (or returns true if one is already loaded)
    /// basename 'imageflow' -> imageflow.dll, libimageflow.so, libimageflow.dylib.
    /// Basename is case-sensitive
    /// </summary>
    /// <param name="basename">The library name sans extension or "lib" prefix</param>
    /// <param name="log">Where to log attempts at assembly search and load</param>
    /// <param name="handle">Where to store the loaded library handle</param>
    /// <param name="customSearchDirectories">Provide this if you want a custom search folder</param>
    /// <returns>True if previously or successfully loaded</returns>
    public static bool TryLoadByBasename(string basename, ILibraryLoadLogger log, out IntPtr handle, IEnumerable<string>? customSearchDirectories = null)
    {
        if (string.IsNullOrEmpty(basename))
        {
            throw new ArgumentNullException(nameof(basename));
        }

        if (LibraryHandlesByBasename.Value.TryGetValue(basename, out handle))
        {
            log.NotifyAttempt(basename, null, true, true, 0);
            return true;
        }
        lock (LibraryHandlesByBasename)
        {
            if (LibraryHandlesByBasename.Value.TryGetValue(basename, out handle))
            {
                log.NotifyAttempt(basename, null, true, true, 0);
                return true;
            }
            var success = TryLoadByBasenameInternal(basename, log, out handle, customSearchDirectories);
            if (success)
            {
                LibraryHandlesByBasename.Value[basename] = handle;
            }

            return success;
        }
    }

    private static bool TryLoadByBasenameInternal(string basename, ILibraryLoadLogger log, out IntPtr handle, IEnumerable<string>? customSearchDirectories = null)
    {
        var filename = GetFilenameWithoutDirectory(basename);
        foreach (var path in RuntimeFileLocator.SearchPossibilitiesForFile(filename, customSearchDirectories))
        {
            if (!File.Exists(path))
            {
                log.NotifyAttempt(basename, path, false, false, 0);
            }
            else
            {
                var success = LoadLibrary(path, out handle, out var errorCode);
                log.NotifyAttempt(basename, path, true, false, errorCode);
                if (success)
                {
                    return true;
                }
            }
        }
        handle = IntPtr.Zero;
        return false;
    }

    private static bool LoadLibrary(string fullPath, out IntPtr handle, out int? errorCode)
    {
        handle = RuntimeFileLocator.IsUnix ? UnixLoadLibrary.Execute(fullPath) : WindowsLoadLibrary.Execute(fullPath);
        if (handle == IntPtr.Zero)
        {
            errorCode = Marshal.GetLastWin32Error();
            return false;
        }
        errorCode = null;
        return true;
    }
}

[SuppressUnmanagedCodeSecurity]
[SecurityCritical]
internal static class WindowsLoadLibrary
{
    [DllImport("kernel32", CallingConvention = CallingConvention.Winapi, CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern IntPtr LoadLibraryEx(string fileName, IntPtr reservedNull, uint flags);

    public static IntPtr Execute(string fileName)
    {
        // Look in the library dir instead of the process dir
        const uint loadWithAlteredSearchPath = 0x00000008;
        return LoadLibraryEx(fileName, IntPtr.Zero, loadWithAlteredSearchPath);
    }
}

[SuppressUnmanagedCodeSecurity]
[SecurityCritical]
internal static class UnixLoadLibrary
{
    // TODO: unsure if this works on Mac OS X; it might be libc instead. dncore works, but mono is untested
    [DllImport("libdl.so", SetLastError = true, CharSet = CharSet.Ansi)]
    private static extern IntPtr dlopen(string fileName, int flags);

    public static IntPtr Execute(string fileName)
    {
        const int rtldNow = 2;
        return dlopen(fileName, rtldNow);
    }
}
