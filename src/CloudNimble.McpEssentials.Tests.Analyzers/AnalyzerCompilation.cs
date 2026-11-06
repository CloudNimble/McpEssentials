using CloudNimble.McpEssentials.Analyzers;
using CloudNimble.McpEssentials.Core.Tasks.Elicitations;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CloudNimble.McpEssentials.Tests.Analyzers
{

    /// <summary>
    /// Compiles a C# snippet with Core referenced and runs the form elicitation analyzers.
    /// </summary>
    internal static class AnalyzerCompilation
    {

        #region Public Methods

        /// <summary>
        /// Compiles <paramref name="source"/> as a console fragment and returns analyzer diagnostics.
        /// </summary>
        /// <param name="source">C# statements and type declarations.</param>
        /// <returns>Analyzer diagnostics only (compiler errors are thrown).</returns>
        public static async Task<ImmutableArray<Diagnostic>> GetDiagnosticsAsync(string source)
        {
            var tree = CSharpSyntaxTree.ParseText($$"""
                using System;
                using System.Collections.Generic;
                using CloudNimble.McpEssentials.Core.Tasks.Elicitations;

                {{source}}
                """);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(FormElicitationBuilder).Assembly.Location)
            };

            foreach (var assembly in typeof(FormElicitationBuilder).Assembly.GetReferencedAssemblies())
            {
                try
                {
                    var loaded = System.Reflection.Assembly.Load(assembly);
                    if (!string.IsNullOrEmpty(loaded.Location) && File.Exists(loaded.Location))
                    {
                        references.Add(MetadataReference.CreateFromFile(loaded.Location));
                    }
                }
                catch (FileNotFoundException)
                {
                }
            }

            AddFrameworkReference(references, "System.Runtime");
            AddFrameworkReference(references, "netstandard");
            AddFrameworkReference(references, "System.Collections");

            var compilation = CSharpCompilation.Create(
                "AnalyzerTest",
                [tree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

            var compilerErrors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();
            if (compilerErrors.Length > 0)
            {
                throw new InvalidOperationException(
                    "Test snippet failed to compile:" + Environment.NewLine + string.Join(Environment.NewLine, compilerErrors.Select(d => d.ToString())));
            }

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(
                new FormElicitationFromTypeAnalyzer(),
                new FormElicitationExpressionAnalyzer(),
                new FormElicitationSealedAnalyzer());

            var withAnalyzers = compilation.WithAnalyzers(analyzers);
            return await withAnalyzers.GetAnalyzerDiagnosticsAsync(CancellationToken.None).ConfigureAwait(false);
        }

        /// <summary>
        /// Compiles <paramref name="source"/> and returns the compilation for symbol tests.
        /// </summary>
        /// <param name="source">C# statements and type declarations.</param>
        /// <returns>A compilation that references Core.</returns>
        public static CSharpCompilation Compile(string source)
        {
            var tree = CSharpSyntaxTree.ParseText($$"""
                using System;
                using System.Collections.Generic;
                using CloudNimble.McpEssentials.Core.Tasks.Elicitations;

                {{source}}
                """);

            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Uri).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(FormElicitationBuilder).Assembly.Location)
            };

            foreach (var assembly in typeof(FormElicitationBuilder).Assembly.GetReferencedAssemblies())
            {
                try
                {
                    var loaded = System.Reflection.Assembly.Load(assembly);
                    if (!string.IsNullOrEmpty(loaded.Location) && File.Exists(loaded.Location))
                    {
                        references.Add(MetadataReference.CreateFromFile(loaded.Location));
                    }
                }
                catch (FileNotFoundException)
                {
                }
            }

            AddFrameworkReference(references, "System.Runtime");
            AddFrameworkReference(references, "netstandard");
            AddFrameworkReference(references, "System.Collections");

            return CSharpCompilation.Create(
                "AnalyzerTest",
                [tree],
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Adds a trusted-platform assembly by simple name when it is loadable.
        /// </summary>
        /// <param name="references">The reference list.</param>
        /// <param name="assemblyName">The assembly simple name.</param>
        private static void AddFrameworkReference(List<MetadataReference> references, string assemblyName)
        {
            try
            {
                var assembly = System.Reflection.Assembly.Load(assemblyName);
                if (!string.IsNullOrEmpty(assembly.Location) && File.Exists(assembly.Location))
                {
                    references.Add(MetadataReference.CreateFromFile(assembly.Location));
                }
            }
            catch (FileNotFoundException)
            {
            }
        }

        #endregion

    }

}
