using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestGeneratorLib.Entity
{
    public class FileInfo
    {
        private SyntaxTree _tree;

        public FileInfo()
        {
            Namespaces = new List<NamespaceInfo>();
        }

        public List<NamespaceInfo> Namespaces { get; }

        public void Initialize(string fileContent)
        {
            _tree = CSharpSyntaxTree.ParseText(fileContent);
            var root = _tree.GetRoot();

            foreach (var namespaceDeclaration in root.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
            {
                var namespaceInfoToAdd = new NamespaceInfo();
                namespaceInfoToAdd.Initialize(namespaceDeclaration);
                Namespaces.Add(namespaceInfoToAdd);
            }
        }
    }
}