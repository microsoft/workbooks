//
// RenderState.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Globalization;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;

namespace Xamarin.Interactive.Rendering
{
	sealed class RenderState
	{
		public RendererContext Context { get; }
		public RenderState ParentState { get; }
		public object OriginatingSource { get; }
		public object Source { get; }
		public RemoteMemberInfo RemoteMember { get; }

		readonly CultureInfo cultureInfo;
		public CultureInfo CultureInfo => cultureInfo ?? ParentState?.CultureInfo;

		public RepresentedType RepresentedType => (Source as RepresentedObject)?.RepresentedType
			?? (OriginatingSource as RepresentedObject)?.RepresentedType;

		RenderState (
			RendererContext context,
			RenderState parentState,
			object originatingSource,
			object source,
			CultureInfo cultureInfo = null,
			RemoteMemberInfo remoteMemberInfo = null)
		{
			Context = context;
			ParentState = parentState;
			OriginatingSource = originatingSource;
			Source = source;
			this.cultureInfo = cultureInfo;
			RemoteMember = remoteMemberInfo;
		}

		public static RenderState Create (
			object source,
			CultureInfo cultureInfo = null,
			RemoteMemberInfo remoteMemberInfo = null)
			=> new RenderState (
				null,
				null,
				null,
				source,
				cultureInfo,
				remoteMemberInfo);

		public RenderState CreateChild (
			object source,
			RemoteMemberInfo remoteMemberInfo = null)
			=> new RenderState (
				Context,
				this,
				null,
				source,
				remoteMemberInfo: remoteMemberInfo);

		public RenderState WithSource (object source) => With (Context, source);

		internal RenderState With (RendererContext context, object source)
		{
			if (context == null)
				throw new ArgumentNullException (nameof (context));

			return new RenderState (
				context,
				ParentState,
				Source,
				source,
				cultureInfo,
				RemoteMember);
		}
	}
}