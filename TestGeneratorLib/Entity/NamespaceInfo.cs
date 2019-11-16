using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace TestGeneratorLib.Entity
{
    public class NamespaceInfo
    {
        public string Name { get; private set; }
        public List<TestableClassInfo> Classes { get; private set; }

        public NamespaceInfo()
        {
            Classes = new List<TestableClassInfo>();
        }

        public void Initialize(NamespaceDeclarationSyntax nds)
        {
            Name = nds.Name.ToString();
            foreach (ClassDeclarationSyntax cds in nds.DescendantNodes().OfType<ClassDeclarationSyntax>())
            {
                TestableClassInfo ciToAdd = new TestableClassInfo();
                ciToAdd.Initialize(cds);
                Classes.Add(ciToAdd);
            }
        }
    }
}