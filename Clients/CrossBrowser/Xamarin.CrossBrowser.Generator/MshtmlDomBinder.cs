//
// MshtmlDomBinder.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015 Xamarin Inc. All rights reserved.

using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;

using ICSharpCode.NRefactory.CSharp;

using Attribute = ICSharpCode.NRefactory.CSharp.Attribute;

namespace Xamarin.CrossBrowser.Generator
{
    class MshtmlDomBinder : DomBinder
    {
        protected override Backend Backend {
            get { return Backend.Mshtml; }
        }

        public override string OutputDirectory {
            get { return Path.Combine ("Xamarin.CrossBrowser.Wpf", "Generated"); }
        }

        protected override string BackingFieldName {
            get { return "ComObject"; }
        }

        protected override string BackingParameterName {
            get { return "comObject"; }
        }

        protected override bool IgnoreType (TypeDeclaration typeDeclaration)
        {
            return base.IgnoreType (typeDeclaration) || typeDeclaration.Name == "WrappedObject";
        }

        static Attribute GetAttribute (IEnumerable<AttributeSection> attributes, string attrName)
        {
            return attributes
                .SelectMany (s => s.Attributes)
                .FirstOrDefault (a => {
                    var name = a.Type.ToString ();
                    return name == attrName || name == attrName + "Attribute";
                });
        }

        protected override SyntaxTree ProduceSyntaxTree (TypeDeclaration typeDeclaration)
        {
            var tree = base.ProduceSyntaxTree (typeDeclaration);
            tree.Members.InsertAfter (
                tree.Members.LastOrNullObject (node => node is UsingDeclaration),
                new UsingDeclaration ("mshtml")
            );
            return tree;
        }

        Expression FromMshtml (EntityDeclaration entity, Expression expression)
        {
            var primitiveType = entity.ReturnType as PrimitiveType;
            TypeData enumType;
            if (Types.TryGetType (entity.ReturnType.ToString (), out enumType) && enumType.IsEnum)
                primitiveType = enumType.DeclaredType.BaseType as PrimitiveType;
            else
                enumType = null;

            if (primitiveType == null)
                return new IdentifierExpression ("Wrap") {
                    TypeArguments = { entity.ReturnType.Clone () }
                }.Invoke (expression);

            if (GetAttribute (entity.Attributes, "MshtmlConvert") != null)
                expression = new IdentifierExpression ("Convert") {
                    TypeArguments = { primitiveType.Clone () }
                }.Invoke (expression);

            return enumType != null ? expression.CastTo (entity.ReturnType.Clone ()) : expression;
        }

        Expression ToMshtml (AstType type, Expression expression)
        {
            TypeData typeData;
            if (Types.TryGetType (type.ToString (), out typeData)) {
                if (typeData.IsEnum)
                    expression = expression.CastTo (typeData.DeclaredType.BaseType);
                else if (typeData.DeclaredType.BaseType != null)
                    expression = new MemberReferenceExpression (expression, BackingFieldName);
            }

            return expression;
        }

        Expression BackingFieldCastTo (EntityDeclaration entity)
        {
            var backendType = GetBackendTypeAttribute (entity.Attributes);

            return new MemberReferenceExpression (
                new ParenthesizedExpression (
                    new CastExpression (
                        backendType?.TypeName == null
                            ? CurrentBackingType.Type
                            : AstType.Create (backendType.TypeName),
                        new IdentifierExpression (BackingFieldName)
                    )
                ),
                ToW3CName (entity.Name)
            );
        }

        protected override Expression BindPropertyGetter (PropertyDeclaration property)
        {
            return FromMshtml (property, BackingFieldCastTo (property));
        }

        protected override Expression BindPropertySetter (PropertyDeclaration property)
        {
            return new AssignmentExpression (
                BackingFieldCastTo (property),
                ToMshtml (
                    property.ReturnType,
                    new IdentifierExpression ("value")
                )
            );
        }

        Expression CastParameter (ParameterDeclaration parameter)
        {
            AstType castToType = null;
            var backendType = GetBackendTypeAttribute (parameter.Attributes);
            if (backendType != null)
                castToType = AstType.Create (backendType.TypeName);
            else {
                TypeData mappedTypeData;
                if (Types.TryGetType (parameter.Type.ToString (), out mappedTypeData)) {
                    var backing = mappedTypeData.GetBackingType (Backend.Mshtml);
                    if (backing != null)
                        castToType = backing.Type;
                }
            }

            var expression = new IdentifierExpression (parameter.Name);
            if (castToType == null)
                return expression;

            return expression.CastTo (castToType);
        }

        protected override Statement BindMethodBody (MethodDeclaration method)
        {
            var expression = BackingFieldCastTo (method).Invoke (
                method.Parameters.Select (p => ToMshtml (p.Type, CastParameter (p)))
            );

            if ((method.ReturnType as PrimitiveType)?.Keyword == "void")
                return expression;

            return new ReturnStatement (FromMshtml (method, expression));
        }

        protected override IEnumerable<ParameterDeclaration> CreateConstructorParameters ()
        {
            return new [] {
                new ParameterDeclaration (AstType.Create ("ScriptContext"), "context"),
                new ParameterDeclaration (CurrentBackingType.Type, BackingParameterName)
            };
        }

        protected override ConstructorInitializer CreateConstructorInitializer ()
        {
            var initializer = base.CreateConstructorInitializer ();
            initializer.Arguments.InsertBefore (initializer.Arguments.Last (), new IdentifierExpression ("context"));
            return initializer;
        }
    }
}