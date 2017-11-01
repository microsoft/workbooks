//
// TypeMap.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.
// Copyright 2016 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;

namespace Xamarin.Interactive.Core
{
	/// <summary>
	/// An efficient and thread safe System.Type to TValue map that supports 
	/// optionally matching both base types and interfaces of the key type.
	/// Matches are cached for fast subsequent lookups. If an exact match is
	/// unavailable and the key is registered to also match against its
	/// base types and implemented interfaces, the base type hierarchy is
	/// searched first, followed by matching against the most derived interface.
	/// </summary>
	class TypeMap<TValue>
	{
		struct TypeOrString : IEquatable<TypeOrString>
		{
			public Type Type { get; }
			public string String { get; }

			public TypeOrString (Type type)
			{
				if (type == null)
					throw new ArgumentNullException (nameof (type));

				Type = type;
				String = null;
			}

			public TypeOrString (string @string)
			{
				if (@string == null)
					throw new ArgumentNullException (nameof (@string));

				Type = null;
				String = @string;
			}

			public static implicit operator TypeOrString (Type type)
				=> new TypeOrString (type);

			public static implicit operator TypeOrString (string @string)
				=> new TypeOrString (@string);

			public bool Equals (TypeOrString obj)
				=> obj.Type == Type && obj.String == String;

			public override bool Equals (object obj)
				=> obj is TypeOrString && Equals ((TypeOrString)obj);

			public override int GetHashCode ()
				=> Type == null ? String.GetHashCode () : Type.GetHashCode ();
		}

		struct MatchInfo<T> : IEquatable<MatchInfo<T>>
		{
			public TypeOrString Key { get; }
			public T Value { get; }
			public bool ExactMatchRequired { get; }

			public MatchInfo (TypeOrString key, T value, bool exactMatchRequired)
			{
				Key = key;
				Value = value;
				ExactMatchRequired = exactMatchRequired;
			}

			public bool Equals (MatchInfo<T> obj)
				=> obj.Equals (Key) && Object.Equals (obj.Value, Value);

			public override bool Equals (object obj)
				=> obj is MatchInfo<T> && Equals ((MatchInfo<T>)obj);

			public override int GetHashCode ()
				=> Hash.Combine (Value.GetHashCode (), Key.GetHashCode ());
		}

		readonly ReaderWriterLockSlim rwlock = new ReaderWriterLockSlim ();
		readonly Dictionary<TypeOrString, List<MatchInfo<TValue>>> map
			= new Dictionary<TypeOrString, List<MatchInfo<TValue>>> ();

		volatile bool hasAny;
		public bool HasAny => hasAny;

		/// <summary>
		/// The number of key Types registered.
		/// </summary>
		public int Count {
			get {
				rwlock.EnterReadLock ();
				try {
					return map.Count;
				} finally {
					rwlock.ExitReadLock ();
				}
			}
		}

		/// <summary>
		/// Register a new Type to TValue mapping.
		/// </summary>
		/// <param name="key">The key Type.</param>
		/// <param name="exactMatchRequired"><c>false</c> to also match interfaces or subclasses.</param>
		/// <param name="value">The mapped value.</param>
		public void Add (Type key, bool exactMatchRequired, TValue value)
		{
			if (key == null)
				throw new ArgumentNullException (nameof (key));

			rwlock.EnterWriteLock ();
			try {
				AddLocked (key, exactMatchRequired, value);
			} finally {
				rwlock.ExitWriteLock ();
			}
		}

		/// <summary>
		/// Register a new Type to TValue mapping.
		/// </summary>
		/// <param name="typeNameKey">The key type name (as provided by Type.ToString())
		/// as a string for later binding.</param>
		/// <param name="exactMatchRequired"><c>false</c> to also match interfaces or subclasses.</param>
		/// <param name="value">The mapped value.</param>
		public void Add (string typeNameKey, bool exactMatchRequired, TValue value)
		{
			if (typeNameKey == null)
				throw new ArgumentNullException (nameof (typeNameKey));

			rwlock.EnterWriteLock ();
			try {
				AddLocked (typeNameKey, exactMatchRequired, value);
			} finally {
				rwlock.ExitWriteLock ();
			}
		}

		void AddLocked (TypeOrString key, bool exactMatchRequired, TValue value)
		{
			List<MatchInfo<TValue>> matchInfoList;
			if (!map.TryGetValue (key, out matchInfoList))
				map.Add (key, matchInfoList = new List<MatchInfo<TValue>> ());
			matchInfoList.Insert (0, new MatchInfo<TValue> (key, value, exactMatchRequired));
			hasAny = true;
		}

		/// <summary>
		/// Return all TValues that map from a registered Type key.
		/// </summary>
		public TValue [] GetValues (Type key)
		{
			rwlock.EnterUpgradeableReadLock ();
			try {
				var values = GetValuesForTypeKey (key).ToArray ();
				if (values.Length > 0)
					return values;

				var stringKey = key.ToString ();
				List<MatchInfo<TValue>> matchInfos;
				if (!map.TryGetValue (stringKey, out matchInfos))
					return values; // empty array

				rwlock.EnterWriteLock ();
				try {
					// upgrade the string match to a type match
					// and re-run the type match
					map.Remove (stringKey);
					map.Add (key, matchInfos);
					return GetValuesForTypeKey (key).ToArray ();
				} finally {
					rwlock.ExitWriteLock ();
				}
			} finally {
				rwlock.ExitUpgradeableReadLock ();
			}
		}

		IEnumerable<TValue> GetValuesForTypeKey (Type key)
		{
			List<MatchInfo<TValue>> matchInfos = null;
			var k = key;
			var ifaceMatch = false;

			// first match an exact type match, then any base types
			while (k != null) {
				if (map.TryGetValue (k, out matchInfos))
					break;
				k = k.GetTypeInfo ().BaseType;
			}

			if (matchInfos == null) {
				// try to match the most derived interface
				var ifaces = key.GetTypeInfo ().ImplementedInterfaces;
				foreach (var it in ifaces.Except (
					ifaces.SelectMany (it => it.GetTypeInfo ().ImplementedInterfaces))) {
					if (map.TryGetValue (it, out matchInfos)) {
						ifaceMatch = true;
						break;
					}
				}
			}

			if (matchInfos == null)
				yield break;

			foreach (var matchInfo in matchInfos) {
				if (k != key || ifaceMatch) {
					if (matchInfo.ExactMatchRequired && !ifaceMatch)
						continue;

					rwlock.EnterWriteLock ();
					try {
						// register the match explicitly so subsequent lookups are fast
						AddLocked (key, false, matchInfo.Value);
					} finally {
						rwlock.ExitWriteLock ();
					}
				}

				yield return matchInfo.Value;
			}
		}
	}
}