//
// JavaScriptCoreDomBinder.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.IO;
using System.Linq;

using ICSharpCode.NRefactory.CSharp;

namespace Xamarin.CrossBrowser.Generator
{
	class JavaScriptCoreDomBinder : DomBinder
	{
		protected override Backend Backend {
			get { return Backend.JavaScriptCore; }
		}

		public override string OutputDirectory {
			get { return Path.Combine ("Xamarin.CrossBrowser.Mac", "Generated"); }
		}

		protected override string BackingFieldName {
			get { return "UnderlyingJSValue"; }
		}

		protected override string BackingParameterName {
			get { return "underlyingJSValue"; }
		}

		readonly TypePair jsValueTypePair = new TypePair ("JSValue", null);

		protected override TypePair FallbackBackingType {
			get { return jsValueTypePair; }
		}

		protected override SyntaxTree ProduceSyntaxTree (TypeDeclaration typeDeclaration)
		{
			var tree = base.ProduceSyntaxTree (typeDeclaration);
			tree.Members.InsertAfter (
				tree.Members.LastOrNullObject (node => node is UsingDeclaration),
				new UsingDeclaration ("JavaScriptCore")
			);
			return tree;
		}

		public override void VisitTypeDeclaration (TypeDeclaration typeDeclaration)
		{
			if (typeDeclaration.ClassType == ClassType.Enum) {
				var primitiveType = typeDeclaration.BaseTypes.FirstOrDefault () as PrimitiveType;
				if (primitiveType?.Keyword == "ushort")
					primitiveType.ReplaceWith (new PrimitiveType ("uint"));
			}

			base.VisitTypeDeclaration (typeDeclaration);
		}

		Expression FromJSValue (AstType type, Expression expression)
		{
			var primitiveType = type as PrimitiveType;
			TypeData enumType;
			if (Types.TryGetType (type.ToString (), out enumType) && enumType.IsEnum)
				primitiveType = enumType.DeclaredType.BaseType as PrimitiveType;
			else
				enumType = null;

			string toMethod;

			switch (primitiveType?.Keyword) {
			case "string":
				toMethod = "ToNullableString";
				break;
			case "int":
				toMethod = "ToInt32";
				break;
			case "uint":
			case "ushort":
				toMethod = "ToUInt32";
				break;
			case "bool":
				toMethod = "ToBool";
				break;
			case "double":
				toMethod = "ToDouble";
				break;
			default:
				if (primitiveType != null)
					throw new Exception ($"unsupported primitive type: {primitiveType}");

				return new IdentifierExpression ("Wrap") {
					TypeArguments = { type.Clone () }
				}.Invoke (expression);
			}

			expression = new InvocationExpression (new MemberReferenceExpression (expression, toMethod));
			return enumType != null ? expression.CastTo (type.Clone ()) : expression;
		}

		Expression ToJSValue (AstType type, Expression expression)
		{
			TypeData typeData;
			if (Types.TryGetType (type.ToString (), out typeData)) {
				if (typeData.IsEnum)
					expression = expression.CastTo (typeData.DeclaredType.BaseType);
				else if (typeData.DeclaredType.BaseType != null)
					expression = new MemberReferenceExpression (expression, BackingFieldName);
			}

			return new InvocationExpression (
				MemberReference ("JSValue", "From"),
				expression,
				MemberReference (BackingFieldName, "Context")
			);
		}

		protected override Expression BindPropertyGetter (PropertyDeclaration property)
		{
			return FromJSValue (
				property.ReturnType,
				new InvocationExpression (
					MemberReference (BackingFieldName, "GetProperty"),
					new PrimitiveExpression (ToW3CName (property.Name))
				)
			);
		}

		protected override Expression BindPropertySetter (PropertyDeclaration property)
		{
			return new InvocationExpression (
				MemberReference (BackingFieldName, "SetProperty"),
				ToJSValue (property.ReturnType, new IdentifierExpression ("value")),
				new PrimitiveExpression (ToW3CName (property.Name))
			);
		}

		protected override Statement BindMethodBody (MethodDeclaration method)
		{
			var expression = new InvocationExpression (
				MemberReference (BackingFieldName, "Invoke"),
				new [] {
					new PrimitiveExpression (ToW3CName (method.Name))
				}.Concat (
					method.Parameters.Select (
						p => ToJSValue (p.Type, new IdentifierExpression (p.Name))
					)
				)
			);

			if ((method.ReturnType as PrimitiveType)?.Keyword == "void")
				return expression;

			return new ReturnStatement (FromJSValue (method.ReturnType, expression));
		}

		protected static Expression MemberReference (string target,
			string memberName, params AstType [] arguments)
		{
			return new MemberReferenceExpression (
				new IdentifierExpression (target),
				memberName,
				arguments
			);
		}
	}
}