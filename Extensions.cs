using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Editing;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AutoMigrate.Extensions
{
    public static class Extensions
    {
        public static PropertyDeclarationSyntax AddAttributeListsEx(this PropertyDeclarationSyntax input, AttributeListSyntax attributeList)
        {
            return (attributeList == null) ? input : input.AddAttributeLists(attributeList);
        }
        public static int LengthB(this string input)
        {
            return System.Text.Encoding.GetEncoding("Shift_JIS").GetByteCount(input);
        }
        public static Single ParseExNothingIsMinValue(this string input)
        {
            if (input == "")
            {
                return Single.MinValue;
            }
            return Single.Parse(input);
        }
        public static uint ParseTriedUInt(this string input)
        {
            if (uint.TryParse(input, out uint uintResult))
            {
                return uintResult;
            }
            return 0;
        }
        public static SyntaxNode GlobalTypeExpression(this SyntaxGenerator syntaxGenerator, ITypeSymbol type)
        {
            switch (type.SpecialType)
            {
                case SpecialType.System_Object:
                case SpecialType.System_Boolean:
                case SpecialType.System_Char:
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                case SpecialType.System_String:
                    return syntaxGenerator.TypeExpression(type.SpecialType);
                default:
                    return syntaxGenerator.TypeExpression(type);
            }
        }
    }

    //public static class SyntaxExtensions
    //{
    //    public static UsingDirectiveSyntax GetUsing(this string name)
    //    {
    //        return UsingDirective(ParseName(name));
    //    }

    //    public static NameSyntax ParseName(this string name)
    //    {
    //        return Syntax.ParseName(" " + name);
    //    }
    //}
}
