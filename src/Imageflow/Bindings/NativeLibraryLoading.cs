
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Text;
using System.Threading;

namespace Imageflow.Bindings
{
    internal class LoadLogger : ILibraryLoadLogger
    {
        internal string verb = "loaded";
        internal string filename = $"{ RuntimeFileLocator.SharedLibraryPrefix.Value}";
        
        internal Exception firstException;
        internal Exception lastException;

        private readonly List<LogEntry> _log = new List<LogEntry>(7);

        private struct LogEntry
        {
            internal string basename;
            internal string fullPath;
            internal bool fileExists;
            internal bool previouslyLoaded;
            internal int? loadErrorCode;
        }

        public void NotifyAttempt(string basename, string fullPath, bool fileExists, bool previouslyLoaded,
            int? loadErrorCode)
        {
            _log.Add(new LogEntry
            {
                basename = basename,
                fullPath = fullPath,
                fileExists = fileExists,
                previouslyLoaded = previouslyLoaded,
                loadErrorCode = loadErrorCode
            });
        }

        internal void RaiseException()
        {
            var sb = new StringBuilder(_log.Select((e) => e.basename?.Length ?? 0 + e.fullPath?.Length ?? 0 + 20)
                .Sum());
            sb.AppendFormat("Looking for \"{0}\" RID=\"{1}-{2}\", IsUnix={3}, IsDotNetCore={4} RelativeSearchPath=\"{5}\"\n",
                filename,
                RuntimeFileLocator.PlatformRuntimePrefix.Value,
                RuntimeFileLocator.ArchitectureSubdir.Value, RuntimeFileLocator.IsUnix,
                RuntimeFileLocator.IsDotNetCore.Value,
                AppDomain.CurrentDomain.RelativeSearchPath);
            if (firstException != null) sb.AppendFormat("Before searching: {0}\n", firstException.Message);
            foreach (var e in _log)
            {
                if (e.previouslyLoaded)
                {
                    sb.AppendFormat("\"{0}\" is already {1}", e.basename, verb);
                }
                else if (!e.fileExists)
                {
                    sb.AppendFormat("File not found: {0}", e.fullPath);
                }
                else if (e.loadErrorCode.HasValue)
                {
                    string errorCode = e.loadErrorCode.Value < 0
                        ? string.Format(CultureInfo.InvariantCulture, "0x{0:X8}", e.loadErrorCode.Value)
                        : e.loadErrorCode.Value.ToString(CultureInfo.InvariantCulture);
                    
                    sb.AppendFormat("Error \"{0}\" {1} loading {2} from {3}", 
                        new Win32Exception(e.loadErrorCode.Value).Message,
                        errorCode,
                        e.basename, e.fullPath);

                    if ((uint)e.loadErrorCode.Value == 0x8007000B &&
                        RuntimeFileLocator.PlatformRuntimePrefix.Value == "win")
                    {
                        var installed = Environment.Is64BitProcess ? "32-bit (x86)" : "64-bit (x86_64)" ;
                        var needed = Environment.Is64BitProcess ? "64-bit (x86_64)" : "32-bit (x86)";

                        sb.AppendFormat("\n> You have installed a {0} copy of imageflow.dll but need the {1} version",
                            installed, needed);
                    }

                    if ((uint)e.loadErrorCode.Value == 0x8007007E &&
                        RuntimeFileLocator.PlatformRuntimePrefix.Value == "win")
                    {
                        var crtLink = "https://aka.ms/vs/16/release/vc_redist." 
                                      + (Environment.Is64BitProcess ? "x64.exe" : "x86.exe");

                        sb.Append("\n> You may need to install the C Runtime from {0}");
                    }
                }
                else
                {
                    sb.AppendFormat("{0} {1} in {2}", verb, e.basename, e.fullPath);
                }
                sb.Append('\n');
            }
            if (lastException != null) sb.AppendLine(lastException.Message);
            var stackTrace = (firstException ?? lastException)?.StackTrace;
            if (stackTrace != null) sb.AppendLine(stackTrace);

            throw new DllNotFoundException(sb.ToString());
        }
    }


    internal static class RuntimeFileLocator
    {
        internal static bool IsUnix => Environment.OSVersion.Platform == PlatformID.Unix || Environment.OSVersion.Platform == PlatformID.MacOSX;

        internal static readonly Lazy<string> SharedLibraryPrefix = new Lazy<string>(() => IsUnix ? "lib" : "", LazyThreadSafetyMode.PublicationOnly);

        internal static readonly Lazy<bool> IsDotNetCore = new Lazy<bool>(() =>
            typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly.CodeBase.Contains("Microsoft.NETCore.App")
            , LazyThreadSafetyMode.PublicationOnly);

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
            return Environment.Is64BitProcess ? "x64" : "x86";
        }, LazyThreadSafetyMode.PublicationOnly);

        /// <summary>
        /// Enumerates a set of folders to search within. If the boolean value of the tuple is true,
        /// specific subdirectories can be searched.
        /// </summary>
        /// <param name="customSearchDirectories"></param>
        /// <returns></returns>
        private static IEnumerable<Tuple<bool,string>> BaseFolders(IEnumerable<string> customSearchDirectories = null)
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
            
            // Look in the base directory from which .NET looks for managed assemblies
            yield return Tuple.Create(true, AppDomain.CurrentDomain.BaseDirectory);
            
            //Issue #17 - Azure Functions 2.0 - https://github.com/imazen/imageflow-dotnet/issues/17
            // If the BaseDirectory is /bin/, look one step outside of it.
            if(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).EndsWith("bin"))
            {
                //Look in the parent directory if we're in /bin/, but only look in ../runtimes/:rid:/native
                yield return Tuple.Create(false, Path.Combine(Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory).FullName, 
                    "runtimes", PlatformRuntimePrefix.Value + "-" + ArchitectureSubdir.Value, "native"));
                
            }

            // Look in the folder that *this* assembly is located.
            yield return Tuple.Create(true, Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
        }

        internal static IEnumerable<string> SearchPossibilitiesForFile(string filename, IEnumerable<string> customSearchDirectories = null)
        {
            var attemptedPaths = new HashSet<string>();
            foreach (var t in BaseFolders(customSearchDirectories))
            {
                if (string.IsNullOrEmpty(t.Item2)) continue;
                var directory = Path.GetFullPath(t.Item2);
                var searchSubDirs = t.Item1;
                // Try architecture-specific subdirectories first
                string path;

                if (searchSubDirs)
                {
                    // First try the simple arch subdir since that is where the nuget native packages unpack
                    path = Path.Combine(directory, ArchitectureSubdir.Value, filename);
                    if (!attemptedPaths.Contains(path))
                    {
                        attemptedPaths.Add(path);
                        yield return path;
                    }
                }

                // Try the folder itself
                path = Path.Combine(directory, filename);
                
                if (!attemptedPaths.Contains(path))
                {
                    attemptedPaths.Add(path);
                    yield return path;
                }

                if (searchSubDirs)
                {
                    // Last try native runtimes directory in case this is happening in .NET Core
                    path = Path.Combine(directory, "runtimes",
                        PlatformRuntimePrefix.Value + "-" + ArchitectureSubdir.Value, "native", filename);
                    if (!attemptedPaths.Contains(path))
                    {
                        attemptedPaths.Add(path);
                        yield return path;
                    }
                }

            }
        }

        /// <summary>
        /// Return the path of the first file found with the given filename, or null if none found
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="customSearchDirectories"></param>
        /// <returns></returns>
        static string SearchForFile(string filename, IEnumerable<string> customSearchDirectories = null)
        {
            return SearchPossibilitiesForFile(filename, customSearchDirectories).FirstOrDefault(File.Exists);
        }
    }
    
    internal interface ILibraryLoadLogger
    {
        void NotifyAttempt(string basename, string fullPath, bool fileExists, bool previouslyLoaded, int? loadErrorCode);
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
        public static string FindExecutable(string basename, IEnumerable<string> customSearchDirectories = null)
        {
            var logger = new LoadLogger { verb = "located", filename = GetFilenameWithoutDirectory(basename) };
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
        /// <param name="customSearchDirectory">Provide this if you want a custom search folder</param>
        /// <returns>True if previously or successfully loaded</returns>
        internal static bool TryLoadByBasename(string basename, ILibraryLoadLogger log, out string exePath,
            IEnumerable<string> customSearchDirectories = null)
        {
            if (string.IsNullOrEmpty(basename))
                throw new ArgumentNullException("filenameWithoutExtension");

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
        private static string GetFilenameWithoutDirectory(string basename) =>  $"{RuntimeFileLocator.SharedLibraryPrefix.Value}{basename}.{RuntimeFileLocator.SharedLibraryExtension.Value}";
        
        /// <summary>
        /// Attempts to resolve DllNotFoundException
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="basename"></param>
        /// <param name="invokingOperation"></param>
        /// <returns></returns>
        public static T FixDllNotFoundException<T>(string basename, Func<T> invokingOperation, IEnumerable<string> customSearchDirectories = null)
        {
            // It turns out that trying to do it "before" is 4-5x slower in cases where the standard loading mechanism works
            // And catching the DllNotFoundException does not seem to measurably slow us down. So no "preventative" stuff.
            try
            {
                return invokingOperation();
            }
            catch (DllNotFoundException first)
            {
                var logger = new LoadLogger {firstException = first, filename = GetFilenameWithoutDirectory(basename) };
                if (TryLoadByBasename(basename, logger, out var _, customSearchDirectories))
                {
                    try
                    {
                        return invokingOperation();
                    }
                    catch (DllNotFoundException last)
                    {
                        logger.lastException = last;
                    }
                }
                logger.RaiseException();
            }
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
        /// <param name="customSearchDirectory">Provide this if you want a custom search folder</param>
        /// <returns>True if previously or successfully loaded</returns>
        public static bool TryLoadByBasename(string basename, ILibraryLoadLogger log, out IntPtr handle, IEnumerable<string> customSearchDirectories = null)
        {
            if (string.IsNullOrEmpty(basename))
                throw new ArgumentNullException("filenameWithoutExtension");

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
                if (success) LibraryHandlesByBasename.Value[basename] = handle;
                return success;
            }
        }     

              
        private static bool TryLoadByBasenameInternal(string basename, ILibraryLoadLogger log, out IntPtr handle, IEnumerable<string> customSearchDirectories = null)
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
            const uint LOAD_WITH_ALTERED_SEARCH_PATH = 0x00000008;
            return LoadLibraryEx(fileName, IntPtr.Zero, LOAD_WITH_ALTERED_SEARCH_PATH);
        }
    }

    [SuppressUnmanagedCodeSecurity]
    [SecurityCritical]
    internal static class UnixLoadLibrary
    {
        // TODO: unsure if this works on Mac OS X; it might be libc instead
        [DllImport("libdl.so", SetLastError = true, CharSet = CharSet.Ansi)]
        private static extern IntPtr dlopen(string fileName, int flags);

        public static IntPtr Execute(string fileName)
        {
            const int RTLD_NOW = 2;
            return dlopen(fileName, RTLD_NOW);
        }
    }

    
}
