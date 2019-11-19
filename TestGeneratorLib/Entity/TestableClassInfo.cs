using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestGeneratorLib.Entity
{
    public class TestableClassInfo
    {
        private const string PublicModifier = "public";

        public TestableClassInfo()
        {
            Methods = new List<BaseMethodInfo>();
            Constructors = new List<BaseMethodInfo>();
        }

        public string Name { get; private set; }
        public List<BaseMethodInfo> Methods { get; }
        public List<BaseMethodInfo> Constructors { get; }

        internal void Initialize(ClassDeclarationSyntax classDeclaration)
        {
            Name = classDeclaration.Identifier.ToString();

            foreach (var mds in classDeclaration.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                var publicToken = mds
                    .ChildTokens()
                    .ToList()
                    .Find(token => token.ValueText == PublicModifier);

                if (publicToken == default)
                {
                    continue;
                }

                var methodInfoToAdd = new BaseMethodInfo();
                methodInfoToAdd.Initialize(mds);
                Methods.Add(methodInfoToAdd);
            }

            foreach (var ctor in classDeclaration.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
            {
                var publicToken = classDeclaration
                    .ChildTokens()
                    .ToList()
                    .Find(token => token.ValueText == PublicModifier);

                if (publicToken == default)
                {
                    continue;
                }

                var methodInfoToAdd = new BaseMethodInfo();
                methodInfoToAdd.Initialize(ctor);
                Constructors.Add(methodInfoToAdd);
            }
        }
    }
}