using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ModelCreator.LogicModels
{
    /// <summary>
    /// 自動生成したコードのCompile確認用
    /// </summary>
    public class CSharpCompiler
    {
        private static readonly CSharpCompilationOptions DefaultCompilationOptions =
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                .WithOverflowChecks(true)
                .WithPlatform(Platform.X86)
                .WithOptimizationLevel(OptimizationLevel.Release)
                .WithUsings(DefaultNamespaces);
        private static readonly IEnumerable<string> DefaultNamespaces =
            new[]
            {
                "System",
                "System.IO",
                "System.Net",
                "System.Linq",
                "System.Text",
                "System.Text.RegularExpressions",
                "System.ComponentModel.DataAnnotations"
            };
        private static readonly IEnumerable<MetadataReference> DefaultReferences =
            new[]
            {
                MetadataReference.CreateFromFile(typeof (object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (System.GenericUriParser).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (Microsoft.CSharp.RuntimeBinder.RuntimeBinderException).Assembly.Location),
                MetadataReference.CreateFromFile(typeof (System.ComponentModel.DataAnnotations.MaxLengthAttribute).Assembly.Location)
            };
        /// <summary>
        /// コンパイル確認。
        /// </summary>
        /// <param name="st"></param>
        /// <returns></returns>
        public static bool CanCompile(SyntaxTree st)
        {
            var compilation = CSharpCompilation.Create("TestRoslyn.dll", new SyntaxTree[] { st }, null, DefaultCompilationOptions);
            compilation = compilation.WithReferences(DefaultReferences);
            using (var stream = new MemoryStream())
            {
                Microsoft.CodeAnalysis.Emit.EmitResult result = compilation.Emit(stream);
                //var assembly = Assembly.Load(stream.GetBuffer());
                return result.Success;
            }
        }
    }
}
