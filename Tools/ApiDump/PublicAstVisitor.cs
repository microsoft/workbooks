//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using ICSharpCode.NRefactory.CSharp;

namespace ApiDump
{
    static class AstExtensions
    {
        public static bool IsPublic (this EntityDeclaration entity)
        {
            if (entity == null)
                return false;

            if (entity.Parent is TypeDeclaration declaringType &&
                declaringType.ClassType == ClassType.Interface)
                return true;

            if (entity is Accessor && (entity.Modifiers & (Modifiers.Internal | Modifiers.Private)) == 0)
                return true;

            return entity.Modifiers.HasFlag (Modifiers.Public);
        }
    }

    sealed class PublicApiVisitor : DepthFirstAstVisitor
    {
        public override void VisitAttribute (Attribute attribute)
        {
            var section = (AttributeSection)attribute.Parent;

            // Equivalent to SimpleType.Identifier and MemberType.MemberName
            // (attribute.Type is a MemberType in the case of inner classes)
            var attributeName = attribute.Type.GetChildByRole (Roles.Identifier).Name;

            switch (attributeName) {
            case "CompilationRelaxations":
            case "RuntimeCompatibility":
            case "SecurityPermission":
            case "AssemblyVersion":
            case "AssemblyFileVersion":
            case "Debuggable":
            case "UnverifiableCode":
            case "CompilerGenerated":
            case "StructLayout":
            case "DebuggerStepThrough":
            case "AsyncStateMachine":
            case "IteratorStateMachine":
                attribute.Remove ();
                break;
            }

            if (section.Attributes.Count == 0)
                section.Remove ();
        }

        public override void VisitNamespaceDeclaration (NamespaceDeclaration namespaceDeclaration)
        {
            base.VisitNamespaceDeclaration (namespaceDeclaration);
            if (namespaceDeclaration.Members.Count == 0)
                namespaceDeclaration.Remove ();
        }

        public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
        {
            if (!typeDeclaration.Modifiers.HasFlag (Modifiers.Public))
                typeDeclaration.Remove ();
            else
                base.VisitTypeDeclaration (typeDeclaration);
        }

        public override void VisitDelegateDeclaration (DelegateDeclaration delegateDeclaration)
        {
            if (delegateDeclaration.IsPublic ())
                base.VisitDelegateDeclaration (delegateDeclaration);
            else
                delegateDeclaration.Remove ();
        }

        public override void VisitConstructorDeclaration (ConstructorDeclaration constructorDeclaration)
        {
            if (constructorDeclaration.IsPublic ()) {
                base.VisitConstructorDeclaration (constructorDeclaration);
                constructorDeclaration.Body = null;
            } else {
                constructorDeclaration.Remove ();
            }
        }

        public override void VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration)
        {
            if (destructorDeclaration.IsPublic ()) {
                base.VisitDestructorDeclaration (destructorDeclaration);
                destructorDeclaration.Body = null;
            } else {
                destructorDeclaration.Remove ();
            }
        }

        public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
        {
            if (methodDeclaration.IsPublic ()) {
                base.VisitMethodDeclaration (methodDeclaration);
                methodDeclaration.Body = null;
            } else {
                methodDeclaration.Remove ();
            }
        }

        public override void VisitOperatorDeclaration (OperatorDeclaration operatorDeclaration)
        {
            if (operatorDeclaration.IsPublic ())
                operatorDeclaration.Body = null;
            else
                operatorDeclaration.Remove ();
        }

        public override void VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration)
        {
            if (propertyDeclaration.IsPublic ())
                base.VisitPropertyDeclaration (propertyDeclaration);
            else
                propertyDeclaration.Remove ();
        }

        public override void VisitIndexerDeclaration (IndexerDeclaration indexerDeclaration)
        {
            if (indexerDeclaration.IsPublic ())
                base.VisitIndexerDeclaration (indexerDeclaration);
            else
                indexerDeclaration.Remove ();
        }

        public override void VisitAccessor (Accessor accessor)
        {
            if (accessor.IsPublic ()) {
                accessor.Body = null;
                base.VisitAccessor (accessor);
            } else {
                accessor.Remove ();
            }
        }

        public override void VisitCustomEventDeclaration (CustomEventDeclaration eventDeclaration)
        {
            if (eventDeclaration.IsPublic ())
                base.VisitCustomEventDeclaration (eventDeclaration);
            else
                eventDeclaration.Remove ();
        }

        public override void VisitEventDeclaration (EventDeclaration eventDeclaration)
        {
            if (eventDeclaration.IsPublic ())
                base.VisitEventDeclaration (eventDeclaration);
            else
                eventDeclaration.Remove ();
        }

        public override void VisitFieldDeclaration (FieldDeclaration fieldDeclaration)
        {
            if (fieldDeclaration.IsPublic ())
                base.VisitFieldDeclaration (fieldDeclaration);
            else
                fieldDeclaration.Remove ();
        }
    }
}