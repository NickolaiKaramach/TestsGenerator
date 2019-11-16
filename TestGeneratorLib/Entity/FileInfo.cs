using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace TestGeneratorLib.Entity
{
    public class FileInfo
    {
        public List<NamespaceInfo> Namespaces { get; private set; }
        private SyntaxTree _tree;

        public FileInfo()
        {
            Namespaces = new List<NamespaceInfo>();
        }

        public void Initialize(string fileContent)
        {
            _tree = CSharpSyntaxTree.ParseText(fileContent);
            SyntaxNode root = _tree.GetRoot();
            
            foreach (NamespaceDeclarationSyntax ns in root.DescendantNodes().OfType<NamespaceDeclarationSyntax>())
            {
                NamespaceInfo niToAdd = new NamespaceInfo();
                niToAdd.Initialize(ns);
                Namespaces.Add(niToAdd);
            }
        }
    }
}