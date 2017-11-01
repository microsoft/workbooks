// InteractiveObjectEditor.cs
//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xamarin.Interactive.Client;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Representations;

using Xamarin.PropertyEditing;

namespace Xamarin.Interactive.PropertyEditor
{
	sealed class InteractiveObjectEditor : IObjectEditor
	{
		const string TAG = nameof (InteractiveObjectEditor);

		InteractiveObject item;
		readonly List<InteractivePropertyInfo> properties;
		readonly ClientSession clientSession;
		public readonly IPropertyViewHelper PropertyHelper;

		public InteractiveObjectEditor (ClientSession clientSession, IPropertyViewHelper propertyHelper, InteractiveObject item)
		{
			this.item = item ?? throw new ArgumentNullException (nameof (item));
			this.clientSession = clientSession ?? throw new ArgumentNullException (nameof (clientSession));
			this.PropertyHelper = propertyHelper ?? throw new ArgumentNullException (nameof (propertyHelper));

			properties = new List<InteractivePropertyInfo> (item.Members.Length);
			for (var i = 0; i < item.Members.Length; i++) {

				var propertyInfo = InteractivePropertyInfo.CreateInstance (this, i);
				if (propertyInfo.Type == null)
					continue;

				properties.Add (propertyInfo);
			}
		}

		object IObjectEditor.Target => item;

		public InteractiveObject Target => item;

		public IReadOnlyCollection<IPropertyInfo> Properties => properties;

		public IObjectEditor Parent => null;

		public IReadOnlyList<IObjectEditor> DirectChildren => Array.Empty<IObjectEditor> ();

		public string TypeName => item.RepresentedType.Name;

		public event EventHandler<EditorPropertyChangedEventArgs> PropertyChanged;

		public Task<ValueInfo<T>> GetValueAsync<T> (IPropertyInfo property, PropertyVariation variation = null)
		{
			var prop = (property as InteractivePropertyInfo) ?? throw new ArgumentException (nameof (property));
			var value = prop.ToLocalValue<T> ();

			return Task.FromResult (new ValueInfo<T> {
				Source = ValueSource.Local,
				Value = value
			});
		}

		public async Task SetValueAsync<T> (IPropertyInfo property, ValueInfo<T> value, PropertyVariation variation = null)
		{
			var prop = (property as InteractivePropertyInfo) ?? throw new ArgumentException (nameof (property));

			var newRemoteValue = prop.ToRemoteValue<T> (value.Value);
			var response = await clientSession.Agent.Api.SetObjectMemberAsync (
				item.Handle,
				prop.Member,
				newRemoteValue,
				true);

			if (!response.Success)
				throw new Exception ("This should never happen");

			item = response.UpdatedValue;
			PropertyChanged?.Invoke (this, new EditorPropertyChangedEventArgs (prop));
		}

	}
}
