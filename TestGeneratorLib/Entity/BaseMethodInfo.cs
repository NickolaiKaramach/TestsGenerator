using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestGeneratorLib.Entity
{
    public class BaseMethodInfo
    {
        public string Name { get; private set; }

        public List<KeyValuePair<string, string>> ParamTypeNamesByParamName { get; private set; }

        public string ReturnTypeName { get; private set; }

        public void Initialize(BaseMethodDeclarationSyntax methodDeclarationSyntax)
        {
            Name = methodDeclarationSyntax
                .ChildTokens()
                .Last()
                .ToString();

            ReturnTypeName = methodDeclarationSyntax
                .ChildNodes()
                .OfType<TypeSyntax>()
                .FirstOrDefault()
                ?.ToString();

            ParamTypeNamesByParamName = methodDeclarationSyntax
                .DescendantNodes()
                .OfType<ParameterSyntax>()
                .Select(parameterSyntax =>
                    new KeyValuePair<string, string>(parameterSyntax.Identifier.ToString(),
                        parameterSyntax.Type.ToString()))
                .ToList();
        }
    }
}