//
// RendererRegistryTests.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

using NUnit.Framework;

using Should;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Rendering;

namespace Xamarin.Interactive.Tests
{
	[TestFixture]
	public class RendererRegistryTests
	{
		sealed class TestRendererRegistry : RendererRegistry
		{
			protected override Type [] GetTypes (Assembly assembly)
			{
				var types = base.GetTypes (assembly);
				Array.Sort (types, (x, y) => {
					var xIsTest = x.Namespace == "Xamarin.Interactive.Tests";
					var yIsTest = x.Namespace == "Xamarin.Interactive.Tests";
					if (xIsTest && !yIsTest)
						return -1;
					if (yIsTest && !xIsTest)
						return 1;
					return string.CompareOrdinal (x.FullName, y.FullName);
				});
				return types;
			}

			public IRenderer GetRenderer (object source)
				=> GetRenderers (source).FirstOrDefault ();

			public TypeMap<Type> GetTypeMap ()
				=> (TypeMap<Type>)typeof (RendererRegistry)
					.GetField ("typeMap", BindingFlags.Instance | BindingFlags.NonPublic)
					.GetValue (this);
		}

		abstract class TestRenderer : IRenderer
		{
			public string CssClass => null;
			public bool CanExpand => true;
			public bool IsEnabled => true;
			public RenderState RenderState => null;
			public void Bind (RenderState renderState) { }
			public IEnumerable<RendererRepresentation> GetRepresentations () { yield break; }
			public void Render (RenderTarget target) { }
			public void Expand () { }
			public void Collapse () { }
		}

		TestRendererRegistry registry;

		[SetUp]
		public void SetUp ()
		{
			registry = new TestRendererRegistry ();
			registry.Initialize ();
		}

		int GetRegisteredRendererCount () => registry.GetTypeMap ().Count;

		[Renderer (typeof(string))]
		class StringRenderer : TestRenderer { }

		[Test]
		public void SingleRendererAttribute ()
		{
			registry.GetRenderer ("woo!").ShouldBeInstanceOf<StringRenderer> ();
		}

		[Renderer (typeof(sbyte))]
		[Renderer (typeof(short))]
		[Renderer (typeof(int))]
		[Renderer (typeof(long))]
		class SignedIntegerRenderer : TestRenderer { }

		[Test]
		public void MultipleRendererAttribtues ()
		{
			registry.GetRenderer ((sbyte)0).ShouldBeInstanceOf<SignedIntegerRenderer> ();
			registry.GetRenderer ((short)0).ShouldBeInstanceOf<SignedIntegerRenderer> ();
			registry.GetRenderer ((int)0).ShouldBeInstanceOf<SignedIntegerRenderer> ();
			registry.GetRenderer ((long)0).ShouldBeInstanceOf<SignedIntegerRenderer> ();
		}

		class LDBT_1 { }
		class LDBT_2 : LDBT_1 { }
		class LDBT_3 : LDBT_2 { }
		class LDBT_4 : LDBT_3 { }

		[Renderer (typeof(LDBT_1), false)]
		class LDBT_Renderer : TestRenderer { }

		[Test]
		public void LeastDerivedBaseTypeRenderer ()
		{
			var count = GetRegisteredRendererCount ();
			registry.GetRenderer (new LDBT_4 ()).ShouldBeInstanceOf<LDBT_Renderer> ();
			registry.GetRenderer (new LDBT_3 ()).ShouldBeInstanceOf<LDBT_Renderer> ();
			registry.GetRenderer (new LDBT_2 ()).ShouldBeInstanceOf<LDBT_Renderer> ();
			registry.GetRenderer (new LDBT_1 ()).ShouldBeInstanceOf<LDBT_Renderer> ();
			GetRegisteredRendererCount ().ShouldEqual (count + 3);
		}

		class MDBT_1 { }
		class MDBT_2 : MDBT_1 { }
		class MDBT_3 : MDBT_2 { }
		class MDBT_4 : MDBT_3 { }
		class MDBT_5 : MDBT_4 { }
		class MDBT_6 : MDBT_5 { }

		[Renderer (typeof(MDBT_1), false)]
		class MDBT_Renderer_1_thru_3 : TestRenderer { }

		[Renderer (typeof(MDBT_4), false)]
		class MDBT_Renderer_4_thru_5 : MDBT_Renderer_1_thru_3 { }

		[Renderer (typeof(MDBT_6))]
		class MDBT_Renderer_6 : MDBT_Renderer_4_thru_5 { }

		[Test]
		public void MoreDerivedBaseTypeRenderer ()
		{
			var count = GetRegisteredRendererCount ();

			registry.GetRenderer (new MDBT_1 ()).ShouldBeInstanceOf<MDBT_Renderer_1_thru_3> ();
			registry.GetRenderer (new MDBT_2 ()).ShouldBeInstanceOf<MDBT_Renderer_1_thru_3> ();
			registry.GetRenderer (new MDBT_3 ()).ShouldBeInstanceOf<MDBT_Renderer_1_thru_3> ();
			registry.GetRenderer (new MDBT_1 ()).ShouldBeInstanceOf<MDBT_Renderer_1_thru_3> ();
			registry.GetRenderer (new MDBT_2 ()).ShouldBeInstanceOf<MDBT_Renderer_1_thru_3> ();
			registry.GetRenderer (new MDBT_3 ()).ShouldBeInstanceOf<MDBT_Renderer_1_thru_3> ();
			GetRegisteredRendererCount ().ShouldEqual (count = count + 2);

			registry.GetRenderer (new MDBT_4 ()).ShouldBeInstanceOf<MDBT_Renderer_4_thru_5> ();
			registry.GetRenderer (new MDBT_5 ()).ShouldBeInstanceOf<MDBT_Renderer_4_thru_5> ();
			registry.GetRenderer (new MDBT_4 ()).ShouldBeInstanceOf<MDBT_Renderer_4_thru_5> ();
			registry.GetRenderer (new MDBT_5 ()).ShouldBeInstanceOf<MDBT_Renderer_4_thru_5> ();
			GetRegisteredRendererCount ().ShouldEqual (count = count + 1);

			registry.GetRenderer (new MDBT_6 ()).ShouldBeInstanceOf<MDBT_Renderer_6> ();
			registry.GetRenderer (new MDBT_6 ()).ShouldBeInstanceOf<MDBT_Renderer_6> ();
			GetRegisteredRendererCount ().ShouldEqual (count);
		}

		interface IThingA { }
		interface IThingB : IThingA { }

		class ThingA : IThingA { }
		class ThingA_ThingA : ThingA { }
		class ThingB : IThingB { }
		class ThingB_ThingB : ThingB { }

		class ThingA_ThingB : IThingA, IThingB { }
		class ThingA_ThingA_ThingB_ThingB : ThingA_ThingB { }
		class ThingA_ThingA_ThingA_ThingB_ThingB_ThingB : ThingA_ThingA_ThingB_ThingB { }

		[Renderer (typeof(IThingA))]
		class IThingARenderer : TestRenderer { }

		[Renderer (typeof(IThingB))]
		class IThingBRenderer : TestRenderer { }

		[Renderer (typeof(ThingA_ThingB), false)]
		class ThingA_ThingBRenderer : TestRenderer { }

		[Test]
		public void InterfaceRenderer ()
		{
			registry.GetRenderer (new ThingA ()).ShouldBeInstanceOf<IThingARenderer> ();
			registry.GetRenderer (new ThingA ()).ShouldBeInstanceOf<IThingARenderer> ();

			registry.GetRenderer (new ThingA_ThingA ()).ShouldBeInstanceOf<IThingARenderer> ();
			registry.GetRenderer (new ThingA_ThingA ()).ShouldBeInstanceOf<IThingARenderer> ();

			registry.GetRenderer (new ThingB ()).ShouldBeInstanceOf<IThingBRenderer> ();
			registry.GetRenderer (new ThingB ()).ShouldBeInstanceOf<IThingBRenderer> ();

			registry.GetRenderer (new ThingB_ThingB ()).ShouldBeInstanceOf<IThingBRenderer> ();
			registry.GetRenderer (new ThingB_ThingB ()).ShouldBeInstanceOf<IThingBRenderer> ();

			registry.GetRenderer (new ThingA_ThingB ()).ShouldBeInstanceOf<ThingA_ThingBRenderer> ();
			registry.GetRenderer (new ThingA_ThingB ()).ShouldBeInstanceOf<ThingA_ThingBRenderer> ();

			registry.GetRenderer (new ThingA_ThingA_ThingB_ThingB ()).ShouldBeInstanceOf<ThingA_ThingBRenderer> ();
			registry.GetRenderer (new ThingA_ThingA_ThingB_ThingB ()).ShouldBeInstanceOf<ThingA_ThingBRenderer> ();

			registry.GetRenderer (new ThingA_ThingA_ThingA_ThingB_ThingB_ThingB ()).ShouldBeInstanceOf<ThingA_ThingBRenderer> ();
			registry.GetRenderer (new ThingA_ThingA_ThingA_ThingB_ThingB_ThingB ()).ShouldBeInstanceOf<ThingA_ThingBRenderer> ();
		}

		class CustomType { }
		class DerivedCustomType : CustomType { }

		[Renderer (typeof(CustomType), false)]
		class CustomTypeRendererA : TestRenderer { }

		[Renderer (typeof(CustomType), false)]
		class CustomTypeRendererB : TestRenderer { }

		[Test]
		public void MultipleRenderers ()
		{
			var renderers = registry.GetRenderers (new DerivedCustomType ()).ToArray ();
			renderers.Length.ShouldEqual (2);
			(renderers [0] is CustomTypeRendererA || renderers [0] is CustomTypeRendererB).ShouldBeTrue ();
			(renderers [1] is CustomTypeRendererA || renderers [1] is CustomTypeRendererB).ShouldBeTrue ();
			renderers [0].GetType ().ShouldNotEqual (renderers [1].GetType ());
		}
	}
}