// InteractiveEditorProvider.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Threading.Tasks;

using Xamarin.Interactive.Client;

using Xamarin.PropertyEditing;

namespace Xamarin.Interactive.PropertyEditor
{
	sealed class InteractiveEditorProvider : IEditorProvider
	{
		readonly ClientSession clientSession;
		readonly IPropertyViewHelper propertyHelper;

		public InteractiveEditorProvider (ClientSession session, IPropertyViewHelper propertyHelper)
		{
			clientSession = session ?? throw new ArgumentNullException (nameof (session));
			this.propertyHelper = propertyHelper ?? throw new ArgumentNullException (nameof (propertyHelper));
		}

		public Task<IObjectEditor> GetObjectEditorAsync (object item) =>
			Task.FromResult<IObjectEditor> (new InteractiveObjectEditor (clientSession, propertyHelper, item as Representations.InteractiveObject));
	}
}
