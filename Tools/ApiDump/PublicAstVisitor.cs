//
// PublicAstVisitor.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using ICSharpCode.NRefactory.CSharp;

namespace ApiDump
{
	static class AstExtensions
	{
		public static bool IsPublic (this EntityDeclaration entity)
		{
			if (entity == null)
				return false;

			var declaringType = entity.Parent as TypeDeclaration;
			if (declaringType != null && declaringType.ClassType == ClassType.Interface)
				return true;

			return entity.Modifiers.HasFlag (Modifiers.Public);
		}
	}

	sealed class PublicApiVisitor : DepthFirstAstVisitor
	{
		public override void VisitAttribute (Attribute attribute)
		{
			var section = (AttributeSection)attribute.Parent;

			switch (((SimpleType)attribute.Type).Identifier) {
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
			if (constructorDeclaration.IsPublic ())
				constructorDeclaration.Body = null;
			else
				constructorDeclaration.Remove ();
		}

		public override void VisitDestructorDeclaration (DestructorDeclaration destructorDeclaration)
		{
			if (destructorDeclaration.IsPublic ())
				destructorDeclaration.Body = null;
			else
				destructorDeclaration.Remove ();
		}

		public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
		{
			if (methodDeclaration.IsPublic ())
				methodDeclaration.Body = null;
			else
				methodDeclaration.Remove ();
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
			accessor.Body = null;
			base.VisitAccessor (accessor);
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