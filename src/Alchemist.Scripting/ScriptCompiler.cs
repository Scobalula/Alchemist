using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Alchemist.Scripting
{
    /// <summary>
    /// A class to handle compiling scripts and optionally caching compiled scripts.
    /// </summary>
    public class ScriptCompiler
    {
        /// <summary>
        /// Gets the name of the file that contains the names of the default libraries.
        /// </summary>
        public const string LibrariesListFileName = "ScriptingLibraries.txt";

        /// <summary>
        /// Gets the name of the file that contains the names of the default includes.
        /// </summary>
        public const string IncludesListFileName = "ScriptingIncludes.txt";

        /// <summary>
        /// The folder that contains shared .NET libraries for use with scripting.
        /// </summary>
        public const string SharedLibrariesFolder = "SharedLibraries";

        /// <summary>
        /// The folder that contains cached .NET libraries for use with scripting.
        /// </summary>
        public const string CachedLibrariesFolder = "CachedLibraries";

        /// <summary>
        /// Gets or Sets the global references for use during compilation.
        /// </summary>
        public List<MetadataReference> GlobalReferences { get; set; } = new();

        /// <summary>
        /// Gets or Sets the global includes for use during compilation.
        /// </summary>
        public List<string> GlobalIncludes { get; set; } = new();

        /// <summary>
        /// Whether or not script caching is enabled.
        /// </summary>
        public bool EnableCaching { get; set; }

        /// <summary>
        /// Gets or Sets the hash algorithm used for file caching.
        /// </summary>
        public HashAlgorithm HashAlgorithm { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCompiler"/> class.
        /// </summary>
        public ScriptCompiler()
        {
            EnableCaching = true;
            HashAlgorithm = MD5.Create();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ScriptCompiler"/> class.
        /// </summary>
        /// <param name="enableCaching">Whether or not to enable caching.</param>
        public ScriptCompiler(bool enableCaching)
        {
            EnableCaching = enableCaching;
            HashAlgorithm = MD5.Create();
        }

        /// <summary>
        /// Registers scripting libraries from a library text file.
        /// </summary>
        /// <returns>The current <see cref="ScriptCompiler"/> this is called on.</returns>
        public ScriptCompiler RegisterScriptingLibraries() => RegisterScriptingLibraries(LibrariesListFileName);

        /// <summary>
        /// Registers scripting libraries from a library text file.
        /// </summary>
        /// <param name="libsFileName">The libraries text file that contains a list of libraries to load.</param>
        /// <returns>The current <see cref="ScriptCompiler"/> this is called on.</returns>
        public ScriptCompiler RegisterScriptingLibraries(string libsFileName)
        {
            // Dynamically resolve the current runtime position from the object assembly. We could be
            // in different versions of .NET or running self-contained, this is a solid way to determine.
            var runtimeLocation = Path.GetDirectoryName(typeof(object).Assembly.Location) ?? string.Empty;

            foreach (var line in File.ReadAllLines(libsFileName))
            {
                if (!string.IsNullOrWhiteSpace(line))
                {
                    var file = line;

                    // Check if DLL resides in our directory.
                    if (!File.Exists(file))
                        file = Path.Combine(SharedLibrariesFolder, line);
                    // Final check, a standard .NET library.
                    if (!File.Exists(file))
                        file = Path.Combine(runtimeLocation, line);

                    if (File.Exists(file))
                        RegisterGlobalAssemblyReference(file);
                }
            }

            return this;
        }

        /// <summary>
        /// Registers default global includes for use during compilation.
        /// </summary>
        /// <returns>The current <see cref="ScriptCompiler"/> this is called on.</returns>
        public ScriptCompiler RegisterIncludes() => RegisterIncludes(IncludesListFileName);

        /// <summary>
        /// Registers default global includes for use during compilation.
        /// </summary>
        /// <param name="includesFileName">The includes text file that contains a list of includes to load.</param>
        /// <returns>The current <see cref="ScriptCompiler"/> this is called on.</returns>
        public ScriptCompiler RegisterIncludes(string includesFileName)
        {
            foreach (var line in File.ReadAllLines(includesFileName))
            {
                if (!string.IsNullOrWhiteSpace(line))
                    GlobalIncludes.Add(line);
            }

            return this;
        }

        /// <summary>
        /// Registers a global assembly reference for use during script compilation.
        /// </summary>
        /// <param name="assemblyPath">The path of the assembly to load.</param>
        /// <returns>The current <see cref="ScriptCompiler"/> this is called on.</returns>
        public ScriptCompiler RegisterGlobalAssemblyReference(string assemblyPath)
        {
            GlobalReferences.Add(MetadataReference.CreateFromFile(assemblyPath));
            return this;
        }

        /// <summary>
        /// Compiles the provided script.
        /// </summary>
        /// <param name="source"></param>
        public bool Compile(string name, string source, IEnumerable<string>? references, out string fileName, [NotNullWhen(false)] out string? error)
        {
            // Obtain a cached file name, regardless of if caching is enabled, we'll still write to disk.
            fileName = Path.Combine(CachedLibrariesFolder, $"{name}_{Convert.ToHexString(HashAlgorithm.ComputeHash(Encoding.Unicode.GetBytes(source)))}.dll");
            error = null;

            // Cache is disabled and/or doesn't exist.
            if (!EnableCaching || !File.Exists(fileName))
            {
                // Add default usings.
                StringBuilder builder = new();

                // Add includes as a string, we'll sort of inject it into
                // the source string.
                foreach (var include in GlobalIncludes)
                    builder.Append($"using {include};");

                var localReferences = new List<MetadataReference>();

                // Copy over to local references
                foreach (var reference in GlobalReferences)
                    localReferences.Add(reference);

                var tree = SyntaxFactory.ParseSyntaxTree(builder.ToString() + source);
                var compilation = CSharpCompilation.Create($"{name}.dll")
                    .WithOptions(new(
                        OutputKind.DynamicallyLinkedLibrary,
                        platform: Platform.X64,
                        optimizationLevel: OptimizationLevel.Release))
                    .WithReferences(localReferences)
                    .AddSyntaxTrees(tree);

                Directory.CreateDirectory(CachedLibrariesFolder);

                using var stream = new MemoryStream();

                // Obtain a cached file name, regardless of if caching is enabled, we'll still write to disk.
                var result = compilation.Emit(stream);

                // If we have an error, we'll need to output our error.
                if (!result.Success)
                {
                    var sb = new StringBuilder();
                    foreach (var diag in result.Diagnostics)
                    {
                        sb.AppendLine(diag.ToString());
                    }

                    error = sb.ToString();
                    return false;
                }

                File.WriteAllBytes(fileName, stream.ToArray());
            }

            return true;
        }
    }
}
