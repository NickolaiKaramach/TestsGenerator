using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestGeneratorLib.Entity
{
    public class NamespaceInfo
    {
        public NamespaceInfo()
        {
            Classes = new List<TestableClassInfo>();
        }

        public string Name { get; private set; }
        public List<TestableClassInfo> Classes { get; }

        public void Initialize(NamespaceDeclarationSyntax namespaceDeclaration)
        {
            Name = namespaceDeclaration.Name.ToString();
            foreach (var cds in namespaceDeclaration.DescendantNodes()
                .OfType<ClassDeclarationSyntax>())
            {
                var classInfoToAdd = new TestableClassInfo();
                classInfoToAdd.Initialize(cds);
                Classes.Add(classInfoToAdd);
            }
        }
    }
}