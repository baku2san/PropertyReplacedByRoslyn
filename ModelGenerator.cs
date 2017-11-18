using Microsoft.CSharp;
using System;
using System.CodeDom;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using Microsoft.CodeAnalysis.CSharp.Formatting;
using Microsoft.CodeAnalysis.Options;
using Microsoft.CodeAnalysis.Formatting;
using System.Reflection;
using Microsoft.CodeAnalysis.MSBuild;
using System.ComponentModel.DataAnnotations;

/// <summary>
/// Nuget から以下をDL
/// Microsoft.CodeAnalysis.CSharp.dll
//  Microsoft.CodeAnalysis.CSharp.Workspaces.dll            Editing.SyntaxGenerator用
//  Microsoft.CodeAnalysis.CSharp.Workspaces.Common.dll
/// </summary>
namespace ModelCreator.LogicModels
{
    /// <summary>
    /// Overview　https://github.com/dotnet/roslyn/wiki/Roslyn%20Overview
    /// About Roslyn @ https://joshvarty.wordpress.com/2014/07/11/learn-roslyn-now-part-3-syntax-nodes-and-syntax-tokens/
    /// Syntax trees are made up of three things: Syntax Nodes, Syntax Tokens and Trivia.
    /// Roslyn’s Syntax Visualizer で、↑の具体的なものが見られるはず
    /// → .NET Compiler Platform SDK を入れると使える
    ///      https://marketplace.visualstudio.com/items?itemName=VisualStudioProductTeam.NETCompilerPlatformSDK)
    /// </summary>
    public class ModelGenerator
    {
        private const string BaseFolder = @"Path";
        private const string Extension = ".cs";
        public ModelGenerator()
        {

        }
        public async void Execute(List<PropertyClass> PropertyClasss, string tableName)
        {
            var baseFilePath = BaseFolder + tableName + Extension;
            var newProperties = ConvertToProperties(PropertyClasss, tableName);
            try
            {
                // Parse the code into a SyntaxTree.
                var tree = CSharpSyntaxTree.ParseText(File.ReadAllText(baseFilePath));

                // Get the root CompilationUnitSyntax.
                var root = await tree.GetRootAsync().ConfigureAwait(false) as CompilationUnitSyntax;

                // Get the namespace declaration.
                var @namespace = root.Members.Single(m => m is NamespaceDeclarationSyntax) as NamespaceDeclarationSyntax;

                // Get all class declarations inside the namespace.     Class 名の確認（ValueText == tableName(as className))
                var classDeclaration = @namespace.Members.FirstOrDefault(m => m is ClassDeclarationSyntax && (m as ClassDeclarationSyntax).Identifier.ValueText == tableName) as ClassDeclarationSyntax;

                // Get all property declarations    PK(Id)は、削除せず残しておく（Attributeを保持したい為。追加は面倒だったので）
                var properties = classDeclaration?.Members.Where(m => m is PropertyDeclarationSyntax && !(m as PropertyDeclarationSyntax).Identifier.ToString().EndsWith("Id"));

                // 一旦Propertyを全て削除して、新しいものを追加
                var newClass = classDeclaration.RemoveNodes(properties, SyntaxRemoveOptions.KeepNoTrivia).AddMembers(newProperties.ToArray());

                // SyntaxNodeのClassを置換
                root = root.ReplaceNode(classDeclaration, newClass);
                //root = root.ReplaceNodes(properties, (n1, n2) => { return null; });


                using (var streamWriter = new StreamWriter(baseFilePath, false, Encoding.UTF8))
                {
                    var workspace = new AdhocWorkspace();
                    var formattedTree = Formatter.Format(root, workspace);
                    formattedTree.WriteTo(streamWriter);
                }
                // TODO ：一時的に場所移動出力をみたいので
                if (CSharpCompiler.CanCompile(root.SyntaxTree)) { throw new Exception("can not compile."); }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            finally
            {

            }

        }
        private static List<PropertyDeclarationSyntax> ConvertToProperties(List<PropertyClass> PropertyClasss, string tableName)
        {
            Func<int, AttributeListSyntax> int2AttributeListSyntax = (input) =>
            {
                return SyntaxFactory.AttributeList()
                        .AddAttributes(
                            SyntaxFactory.Attribute(SyntaxFactory.ParseName(typeof(MaxLengthAttribute).ToString()),
                            SyntaxFactory.AttributeArgumentList().AddArguments(
                                SyntaxFactory.AttributeArgument(
                                    SyntaxFactory.LiteralExpression(
                                        SyntaxKind.NumericLiteralExpression,
                                        SyntaxFactory.Literal(input))))));
                //SyntaxFactory.LiteralExpression(          as String
                //    SyntaxKind.StringLiteralExpression, 
                //    SyntaxFactory.Token(default(SyntaxTriviaList),
                //    SyntaxKind.StringLiteralToken,  
                //    "value", "valueText", default(SyntaxTriviaList)))))));
            };
            var attributeListMax2 = int2AttributeListSyntax(2);
            var attributeListMax4 = int2AttributeListSyntax(4);
            Func<AccessSize, AttributeListSyntax> accessType2AttributeList = (input) =>
            {
                switch (input)
                {
                    case AccessSize.WORD:
                        return attributeListMax2;
                    case AccessSize.DWORD:
                        return attributeListMax4;
                    default:
                        return null;
                }
            };
            Func<_Type, TypeSyntax> accessType2TypeSyntax = (input) =>
            {
                switch (input)
                {
                    case _BIT:
                        return SyntaxFactory.ParseTypeName(typeof(bool).ToString());
                    case _BYTE:
                        return (TypeSyntax)SyntaxFactory.ParseTypeName(typeof(Byte).ToString());
                    default:
                        return SyntaxFactory.ParseTypeName(typeof(Byte[]).ToString());
                }
            };
            Func<PropertyDeclarationSyntax, string, PropertyDeclarationSyntax> addAutoGetSetAndModifier = (input, comment) =>
            {
                return input
                    .AddAccessorListAccessors(
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                        SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                    )
                    .WithTrailingTrivia(new SyntaxTriviaList()
                        .Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.WhitespaceTrivia, "\t"))
                        .Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.SingleLineCommentTrivia, "// " + comment))
                        .Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, Environment.NewLine))
                        .Add(SyntaxFactory.SyntaxTrivia(SyntaxKind.EndOfLineTrivia, Environment.NewLine)))
                    .AddModifiers(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
            };

            var properties = new List<PropertyDeclarationSyntax>();

            // Propertyは、設定依存
            PropertyClasss.ForEach(f => properties.Add(
                addAutoGetSetAndModifier(SyntaxFactory.PropertyDeclaration(accessType2TypeSyntax(f.Type), f.Name)
                    .AddAttributeListsEx(accessType2AttributeList(f.Type)), f.Description)
            ));


            // taret token を空にする方法。だけど、Tokenは、Nodeの下位概念
            //  →var noneToken = SyntaxFactory.Token(SyntaxKind.None);
            return properties;
        }
    }
}
