//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Linq;

using ICSharpCode.NRefactory.CSharp;

namespace ApiDump
{
    sealed class SortTreeVisitor : DepthFirstAstVisitor
    {
        static int GetNodeSortGroup (AstNode node)
        {
            if (node is DelegateDeclaration)
                return 1;
            if (node is TypeDeclaration)
                return 2;
            if (node is FieldDeclaration)
                return 3;
            if (node is PropertyDeclaration)
                return 4;
            if (node is EventDeclaration)
                return 5;
            if (node is ConstructorDeclaration)
                return 6;
            if (node is DestructorDeclaration)
                return 7;
            if (node is IndexerDeclaration)
                return 8;
            if (node is MethodDeclaration)
                return 9;
            return 0;
        }

        static string GetNodeSortName (AstNode node)
        {
            if (node is EntityDeclaration entity)
                return entity.Name;

            if (node is TypeDeclaration type)
                return type.Name;

            if (node is NamespaceDeclaration ns)
                return ns.FullName;

            if (node is AttributeSection attributeSection)
                return attributeSection.ToString ();

            return null;
        }

        static string GetNodeSortSignature (AstNode node)
        {
            if (node is MethodDeclaration ||
                node is IndexerDeclaration ||
                node is DelegateDeclaration ||
                node is ConstructorDeclaration)
                return node.ToString ();
            return null;
        }

        static void SortNodeCollection<T> (AstNodeCollection<T> collection) where T : AstNode
        {
            var sortedMembers = collection
                .OrderBy (GetNodeSortGroup)
                .ThenBy (GetNodeSortName)
                .ThenBy (GetNodeSortSignature)
                .ToList ();
            collection.Clear ();
            collection.AddRange (sortedMembers);
        }

        public override void VisitSyntaxTree (SyntaxTree syntaxTree)
        {
            SortNodeCollection (syntaxTree.GetChildrenByRole (EntityDeclaration.AttributeRole));
            SortNodeCollection (syntaxTree.Members);
            base.VisitSyntaxTree (syntaxTree);
        }

        public override void VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration)
        {
            SortNodeCollection (namespaceDeclaration.Members);
            base.VisitNamespaceDeclaration (namespaceDeclaration);
        }

        public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
        {
            if (typeDeclaration.ClassType != ClassType.Enum)
                SortNodeCollection (typeDeclaration.Members);
            base.VisitTypeDeclaration (typeDeclaration);
        }
    }
}