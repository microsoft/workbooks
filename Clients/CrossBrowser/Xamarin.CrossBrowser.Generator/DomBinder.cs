//
// DomBinder.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.Linq;
using System.Collections.Generic;

using ICSharpCode.NRefactory.CSharp;

using Attribute = ICSharpCode.NRefactory.CSharp.Attribute;

namespace Xamarin.CrossBrowser.Generator
{
	abstract class DomBinder : DepthFirstAstVisitor
	{
		protected class TypePair
		{
			readonly AstType type;
			readonly AstType baseType;

			public TypePair (string type, string baseType)
			{
				this.type = AstType.Create (type);
				if (baseType != null)
					this.baseType = AstType.Create (baseType);
			}

			public TypePair (AstType type, AstType baseType)
			{
				this.type = type;
				this.baseType = baseType;
			}

			public AstType Type {
				get { return type.Clone (); }
			}

			public AstType BaseType {
				get { return baseType?.Clone (); }
			}
		}

		protected class TypeData
		{
			readonly Dictionary<Backend, TypePair> backingTypes = new Dictionary<Backend, TypePair> ();

			public DomBinder DomBinder { get; set; }
			public bool IsEnum { get; set; }
			public bool IsReadOnlyList { get; set; }
			public TypePair DeclaredType { get; set; }

			public void AddBackingType (Backend backend, TypePair backingType)
			{
				backingTypes.Add (backend, backingType);
			}

			public TypePair GetBackingType (Backend backend)
			{
				TypePair backingType;
				if (!backingTypes.TryGetValue (backend, out backingType))
					return DomBinder?.FallbackBackingType;
				return backingType;
			}
		}

		protected class TypeCollector : DepthFirstAstVisitor
		{
			readonly DomBinder domBinder;
			readonly Dictionary<string, TypeData> declaredTypes = new Dictionary<string, TypeData> ();

			public TypeCollector (DomBinder domBinder)
			{
				this.domBinder = domBinder;
			}

			public TypeData this [string declaredTypeName] {
				get { return declaredTypes [declaredTypeName]; }
			}

			public bool TryGetType (string declaredTypeName, out TypeData typeData)
			{
				return declaredTypes.TryGetValue (declaredTypeName, out typeData);
			}

			public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
			{
				TypeData typeData = null;

				switch (typeDeclaration.ClassType) {
				case ClassType.Enum:
					typeData = new TypeData {
						IsEnum = true,
						DeclaredType = new TypePair (
							AstType.Create (typeDeclaration.Name),
							typeDeclaration.BaseTypes.FirstOrDefault ()
								as PrimitiveType ?? new PrimitiveType ("int")
						)
					};
					break;
				case ClassType.Interface:
					typeData = new TypeData {
						DeclaredType = new TypePair (
							AstType.Create (typeDeclaration.Name),
							typeDeclaration.BaseTypes.FirstOrDefault ()
						)
					};

					foreach (var attr in GetTypeAttributes (typeDeclaration.Attributes))
						typeData.AddBackingType (
							attr.Backend,
							new TypePair (attr.TypeName, attr.BaseTypeName)
						);

					typeData.IsReadOnlyList = typeDeclaration
						.Attributes
						.SelectMany (section => section.Attributes)
						.FirstOrDefault (attr =>
							attr.Type.ToString () == "IReadOnlyList") != null;
					break;
				}

				if (typeData != null) {
					typeData.DomBinder = domBinder;
					declaredTypes.Add (typeDeclaration.Name, typeData);
				}

				base.VisitTypeDeclaration (typeDeclaration);
			}
		}

		static void RemoveAttributes (IEnumerable<AttributeSection> sections)
		{
			foreach (var section in sections)
				section.Remove ();
		}

		protected TypeAttribute GetBackendTypeAttribute (IEnumerable<AttributeSection> sections)
		{
			return GetTypeAttributes (sections).FirstOrDefault (type => type.Backend == Backend);
		}

		protected static IEnumerable<TypeAttribute> GetTypeAttributes (IEnumerable<AttributeSection> sections)
		{
			foreach (var attr in sections.SelectMany (section => section.Attributes)) {
				var name = attr.Type.ToString ();
				if (name != "Type" && name != "TypeAttribute")
					continue;
				var args = attr.Arguments.ToList ();
				yield return new TypeAttribute (
					(Backend)Enum.Parse (typeof(Backend),
						((MemberReferenceExpression)args [0]).MemberName),
					(string)((PrimitiveExpression)args [1]).Value,
					args.Count > 2
						? (string)((PrimitiveExpression)args [2]).Value
						: null
				);
			}
		}

		protected static string ToW3CName (string name)
		{
			return Char.ToLower (name [0]) + name.Substring (1);
		}

		public readonly List<SyntaxTree> SyntaxTrees = new List<SyntaxTree> ();
		public abstract string OutputDirectory { get; }

		protected readonly TypeCollector Types;

		protected abstract Backend Backend { get; }
		protected abstract string BackingParameterName { get; }
		protected abstract string BackingFieldName { get; }

		protected abstract Expression BindPropertyGetter (PropertyDeclaration property);
		protected abstract Expression BindPropertySetter (PropertyDeclaration property);
		protected abstract Statement BindMethodBody (MethodDeclaration method);

		protected TypeData CurrentTypeData { get; private set; }
		protected TypePair CurrentDeclaredType { get; private set; }
		protected TypePair CurrentBackingType { get; private set; }
		protected virtual TypePair FallbackBackingType {
			get { return null; }
		}

		protected DomBinder ()
		{
			Types = new TypeCollector (this);
		}

		public override void VisitSyntaxTree (SyntaxTree syntaxTree)
		{
			syntaxTree.AcceptVisitor (Types);
			base.VisitSyntaxTree (syntaxTree);
		}

		protected virtual SyntaxTree ProduceSyntaxTree (TypeDeclaration typeDeclaration)
		{
			var tree = new SyntaxTree { FileName = typeDeclaration.Name + ".cs" };
			tree.Members.Add (new UsingDeclaration ("System"));
			if (CurrentTypeData != null && CurrentTypeData.IsReadOnlyList) {
				tree.Members.Add (new UsingDeclaration ("System.Collections"));
				tree.Members.Add (new UsingDeclaration ("System.Collections.Generic"));
			}
			var ns = new NamespaceDeclaration ("Xamarin.CrossBrowser");
			ns.Members.Add (typeDeclaration);
			tree.Members.Add (ns);
			return tree;
		}

		bool IsIgnored (EntityDeclaration entity)
		{
			return entity
				.Attributes
				.SelectMany (section => section.Attributes)
				.Where (attr => attr.Type.ToString () == "Ignore")
				.Any (attr => (Backend)Enum.Parse (typeof(Backend),
					((MemberReferenceExpression)attr.Arguments.First ()).MemberName) == Backend);
		}

		protected virtual IEnumerable<ParameterDeclaration> CreateConstructorParameters ()
		{
			return new [] {
				new ParameterDeclaration (CurrentBackingType.Type, BackingParameterName)
			};
		}

		protected virtual ConstructorInitializer CreateConstructorInitializer ()
		{
			Expression baseTypeArg = new IdentifierExpression (BackingParameterName);
			if (CurrentBackingType != null && CurrentBackingType.BaseType != null &&
				CurrentBackingType.Type.ToString () != CurrentBackingType.BaseType.ToString ())
				baseTypeArg = baseTypeArg.CastTo (CurrentBackingType.BaseType);
			return new ConstructorInitializer {
				Arguments = { baseTypeArg }
			};
		}

		void AugmentType (TypeDeclaration typeDeclaration)
		{
			CurrentTypeData = Types [typeDeclaration.Name];
			CurrentDeclaredType = CurrentTypeData.DeclaredType;
			CurrentBackingType = CurrentTypeData.GetBackingType (Backend);

			var ctor = new ConstructorDeclaration {
				Modifiers = Modifiers.Internal,
				Name = typeDeclaration.Name,
				Body = new BlockStatement ()
			};

			ctor.Parameters.AddRange (CreateConstructorParameters ());

			if (CurrentDeclaredType.BaseType != null)
				ctor.Initializer = CreateConstructorInitializer ();

			typeDeclaration.Members.InsertBefore (typeDeclaration.Members.FirstOrNullObject (), ctor);

			if (typeDeclaration.Name != "WrappedObject")
				return;
			
			typeDeclaration.Members.InsertBefore (ctor, new FieldDeclaration {
				Modifiers = Modifiers.Internal | Modifiers.Readonly,
				ReturnType = CurrentBackingType.Type,
				Variables = { new VariableInitializer (BackingFieldName) }
			});

			ctor.Body = new BlockStatement {
				new IfElseStatement (
					new BinaryOperatorExpression (
						new IdentifierExpression (BackingParameterName),
						BinaryOperatorType.Equality,
						new NullReferenceExpression ()
					),
					new ThrowStatement (
						new ObjectCreateExpression (
							AstType.Create ("ArgumentNullException"),
							new PrimitiveExpression (BackingParameterName)
						)
					)
				),
				new AssignmentExpression (
					new IdentifierExpression (BackingFieldName),
					new IdentifierExpression (BackingParameterName)
				)
			};
		}

		protected virtual bool IgnoreType (TypeDeclaration typeDeclaration)
		{
			switch (typeDeclaration.Name) {
			case "EventListener":
			case "ScrollIntoViewOptions":
				return true;
			}

			return false;
		}

		public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
		{
			typeDeclaration.Remove ();

			switch (typeDeclaration.ClassType) {
			case ClassType.Interface:
				if (IgnoreType (typeDeclaration))
					return;

				typeDeclaration.ClassType = ClassType.Class;
				typeDeclaration.Modifiers |= Modifiers.Partial;

				AugmentType (typeDeclaration);
				break;
			case ClassType.Class:
			case ClassType.Struct:
				return;
			}

			typeDeclaration.Modifiers |= Modifiers.Public;

			SyntaxTrees.Add (ProduceSyntaxTree (typeDeclaration));

			base.VisitTypeDeclaration (typeDeclaration);

			RemoveAttributes (typeDeclaration.Attributes);
		}

		public override void VisitPropertyDeclaration (PropertyDeclaration propertyDeclaration)
		{
			propertyDeclaration.Modifiers |= Modifiers.Public;

			if (!propertyDeclaration.Getter.IsNull)
				propertyDeclaration.Getter.Body = new BlockStatement {
				new ReturnStatement (BindPropertyGetter (propertyDeclaration))
			};

			if (!propertyDeclaration.Setter.IsNull)
				propertyDeclaration.Setter.Body = new BlockStatement {
				BindPropertySetter (propertyDeclaration)
			};

			if (IsIgnored (propertyDeclaration))
				propertyDeclaration.Remove ();

			RemoveAttributes (propertyDeclaration.Attributes);

			if (CurrentTypeData.IsReadOnlyList && propertyDeclaration.Name == "Length")
				propertyDeclaration.Name = "Count";
		}

		public override void VisitMethodDeclaration (MethodDeclaration methodDeclaration)
		{
			if (!methodDeclaration.Body.IsNull) {
				base.VisitMethodDeclaration (methodDeclaration);
				return;
			}

			if (methodDeclaration.Name == "ToString" && methodDeclaration.Parameters.Count == 0)
				methodDeclaration.Modifiers |= Modifiers.Override;

			methodDeclaration.Modifiers |= Modifiers.Public;
			methodDeclaration.Body = new BlockStatement {
				BindMethodBody (methodDeclaration)
			};

			base.VisitMethodDeclaration (methodDeclaration);

			if (IsIgnored (methodDeclaration))
				methodDeclaration.Remove ();

			RemoveAttributes (methodDeclaration.Attributes);

			var typeDeclaration = methodDeclaration.GetParent<TypeDeclaration> ();
			if (typeDeclaration == null ||
				!CurrentTypeData.IsReadOnlyList ||
				methodDeclaration.Name != "Item")
				return;

			var indexerDeclaration = new IndexerDeclaration {
				Modifiers = Modifiers.Public,
				ReturnType = methodDeclaration.ReturnType.Clone (),
				Getter = new Accessor {
					Body = (BlockStatement)methodDeclaration.Body.Clone ()
				}
			};

			foreach (var param in methodDeclaration.Parameters)
				indexerDeclaration.Parameters.Add (param.Clone ());

			methodDeclaration.ReplaceWith (indexerDeclaration);

			typeDeclaration.BaseTypes.Add (
				new SimpleType ("IReadOnlyList", methodDeclaration.ReturnType.Clone ())
			);

			var forStatement = new ForStatement {
				Initializers = {
					new VariableDeclarationStatement (
						indexerDeclaration.Parameters.First ().Type.Clone (),
						"i",
						new PrimitiveExpression (0)
					),
					new ExpressionStatement (
						new AssignmentExpression (
							new IdentifierExpression ("n"),
							new IdentifierExpression ("Count")
						)
					)
				},
				Condition = new BinaryOperatorExpression (
					new IdentifierExpression ("i"),
					BinaryOperatorType.LessThan,
					new IdentifierExpression ("n")
				),
				Iterators = {
					new ExpressionStatement (
						new UnaryOperatorExpression (
							UnaryOperatorType.PostIncrement,
							new IdentifierExpression ("i")
						)
					)
				},
				EmbeddedStatement = new YieldReturnStatement {
					Expression = new IndexerExpression (
						new ThisReferenceExpression (),
						new IdentifierExpression ("i")
					)
				},

			};

			typeDeclaration.Members.Add (new MethodDeclaration {
				Modifiers = Modifiers.Public,
				Name = "GetEnumerator",
				ReturnType = new SimpleType ("IEnumerator", methodDeclaration.ReturnType.Clone ()),
				Body = new BlockStatement {
					forStatement
				}
			});

			typeDeclaration.Members.Add (new MethodDeclaration {
				Name = "IEnumerable.GetEnumerator",
				ReturnType = AstType.Create ("IEnumerator"),
				Body = new BlockStatement {
					new ReturnStatement (
						new IdentifierExpression ("GetEnumerator").Invoke ()
					)
				}
			});
		}

		public override void VisitParameterDeclaration (ParameterDeclaration parameterDeclaration)
		{
			base.VisitParameterDeclaration (parameterDeclaration);
			RemoveAttributes (parameterDeclaration.Attributes);
		}

		public override void VisitComment (Comment comment)
		{
			comment.Remove ();
		}
	}
}