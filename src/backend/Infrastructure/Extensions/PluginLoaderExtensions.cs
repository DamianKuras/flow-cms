using System.Reflection;
using System.Runtime.Loader;

namespace Infrastructure.Extensions;

/// <summary>
/// Utility methods for loading plugin assemblies from the file system.
/// </summary>
public static class PluginLoaderExtensions
{
    /// <summary>
    /// Enumerates and loads all valid <c>.dll</c> files found directly inside
    /// <paramref name="pluginsFolder"/>.  Files that cannot be loaded are silently skipped.
    /// </summary>
    /// <param name="pluginsFolder">Path to the directory that contains plugin assemblies.</param>
    /// <returns>Each successfully loaded <see cref="Assembly"/>.</returns>
    public static IEnumerable<Assembly> LoadPluginAssemblies(string pluginsFolder)
    {
        if (!Directory.Exists(pluginsFolder))
        {
            yield break;
        }

        foreach (
            string file in Directory.EnumerateFiles(
                pluginsFolder,
                "*.dll",
                SearchOption.TopDirectoryOnly
            )
        )
        {
            Assembly? asm = null;
            try
            {
                asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.GetFullPath(file));
            }
            catch
            {
                // log or ignore invalid assembly
            }

            if (asm != null)
            {
                yield return asm;
            }
        }
    }
}
