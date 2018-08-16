// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Xamarin.IO
{
    /// <summary>
    /// Various utilities that should be provided via System.IO.Path.
    /// </summary>
    public static class PathHelpers
    {
        [DllImport ("libc")]
        static extern string realpath (string path, IntPtr resolvedName);

        static volatile bool haveRealpath = Environment.OSVersion.Platform == PlatformID.Unix;

        /// <summary>
        /// A more exhaustive version of <see cref="System.IO.Path.GetFullPath(string)"/>
        /// that additionally resolves symlinks via `realpath` on Unix systems. The path
        /// returned is also trimmed of any trailing directory separator characters.
        /// </summary>
        /// <param name="pathComponents">
        /// The path components to resolve to a full path. If multiple path compnents are
        /// specified, they are combined with <see cref="Path.Combine"/> before resolving.
        /// </param>
        /// <returns>
        /// Returns `null` if <paramref name="path"/> is `null` or an empty,
        /// string, otherwise the fully resolved path.
        /// </returns>
        public static string ResolveFullPath (params string [] pathComponents)
        {
            string path;

            if (pathComponents == null || pathComponents.Length == 0)
                return null;
            else if (pathComponents.Length == 1)
                path = pathComponents [0];
            else
                path = Path.Combine (pathComponents);

            if (string.IsNullOrEmpty (path))
                return null;

            var fullPath = Path.GetFullPath (NormalizePath (path));

            if (haveRealpath) {
                try {
                    // Path.GetFullPath expands the path, but on Unix systems
                    // does not resolve symlinks. Always attempt to resolve
                    // symlinks via realpath.
                    var realPath = realpath (fullPath, IntPtr.Zero);
                    if (!string.IsNullOrEmpty (realPath))
                        fullPath = realPath;
                } catch {
                    haveRealpath = false;
                }
            }

            if (fullPath.Length == 0)
                return null;

            if (fullPath [fullPath.Length - 1] == Path.DirectorySeparatorChar)
                return fullPath.TrimEnd (Path.DirectorySeparatorChar);

            if (fullPath [fullPath.Length - 1] == Path.AltDirectorySeparatorChar)
                return fullPath.TrimEnd (Path.AltDirectorySeparatorChar);

            return fullPath;
        }

        /// <summary>
        /// Returns a path with / and \ directory separators
        /// normalized to <see cref="Path.DirectorySeparatorChar"/>.
        /// </summary>
        public static string NormalizePath (string path)
            => path
                ?.Replace ('\\', Path.DirectorySeparatorChar)
                ?.Replace ('/', Path.DirectorySeparatorChar);

        /// <summary>
        /// Helper functions for directory structures within a Git repository.
        /// </summary>
        public static class Git
        {
            /// <summary>
            /// Finds the root directory of a git repository from an assembly.
            /// If no assembly is provided, the calling assembly is implied.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="DirectoryNotFoundException"/> if the root repository directory cannot be found.
            /// </remarks>
            public static string FindRepositoryRootPathFromAssembly (Assembly assemblyInRepository = null)
                => FindRepositoryRootPath ((assemblyInRepository ?? Assembly.GetCallingAssembly ()).Location);

            /// <summary>
            /// Finds the root directory of a git repository from a child path within.
            /// </summary>
            /// <remarks>
            /// Throws <see cref="DirectoryNotFoundException"/> if the root repository directory cannot be found.
            /// </remarks>
            public static string FindRepositoryRootPath (string childPathInRepository)
            {
                var path = childPathInRepository;

                while (File.Exists (path) || Directory.Exists (path)) {
                    if (Directory.Exists (Path.Combine (path, ".git")))
                        return ResolveFullPath (path);

                    path = Path.GetDirectoryName (path);
                }

                throw new DirectoryNotFoundException ($"{childPathInRepository} is not in a git repository");
            }
        }
    }
}