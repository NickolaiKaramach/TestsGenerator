using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestGeneratorLib.Entity
{
    public class TestableClassInfo
    {
        private static string _publicModifier = "public";
        public string Name { get; private set; }
        public List<BaseMethodInfo> Methods { get; private set; }
        public List<BaseMethodInfo> Constructors { get; private set; }

        public TestableClassInfo()
        {
            Methods = new List<BaseMethodInfo>();
            Constructors = new List<BaseMethodInfo>();
        }

        internal void Initialize(ClassDeclarationSyntax cds)
        {
            Name = cds.Identifier.ToString();

            foreach (var mds in cds.DescendantNodes().OfType<MethodDeclarationSyntax>())
            {
                SyntaxToken publicToken = mds.ChildTokens().ToList().Find(token => token.ValueText == _publicModifier);
                if (publicToken == default)
                    continue;

                BaseMethodInfo miToAdd = new BaseMethodInfo();
                miToAdd.Initialize(mds);
                Methods.Add(miToAdd);
            }

            foreach (var ctor in cds.DescendantNodes().OfType<ConstructorDeclarationSyntax>())
            {
                SyntaxToken publicToken = cds.ChildTokens().ToList().Find(token => token.ValueText == _publicModifier);
                if (publicToken == default)
                    continue;

                BaseMethodInfo miToAdd = new BaseMethodInfo();
                miToAdd.Initialize(ctor);
                Constructors.Add(miToAdd);
            }
        }
    }
}