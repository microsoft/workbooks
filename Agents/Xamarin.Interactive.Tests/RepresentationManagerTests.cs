// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Globalization;

using Xunit;

using Xamarin.Interactive.Representations;
using Xamarin.Interactive.Representations.Reflection;
using Xamarin.Interactive.Serialization;
using Xamarin.Interactive.Logging;

namespace Xamarin.Interactive.Representations
{
    public sealed class RepresentationManagerTests
    {
        static RepresentationManagerTests ()
        {
            MainThread.Initialize ();
            Logging.Log.Initialize (new TestLogProvider ());
        }

        sealed class TestLogProvider : Logging.ILogProvider
        {
            public LogLevel LogLevel { get; set; } = LogLevel.Warning;

            #pragma warning disable 67
            public event EventHandler<LogEntry> EntryAdded;
            #pragma warning restore 67

            public void Commit (LogEntry entry)
            {
                switch (entry.Level) {
                case LogLevel.Error:
                case LogLevel.Critical:
                    throw new Exception ($"Log.{entry.Level}: {entry}");
                case LogLevel.Warning:
                    Console.Error.WriteLine ($"Log.Warning: {entry}");
                    break;
                }
            }

            public LogEntry[] GetEntries()
                => throw new NotImplementedException ();
        }

        RepresentationManager manager = new RepresentationManager ();

        #region RepresentationManager.Normalize

        // Types that all agents support without explicit representation boxing

        [Flags]
        enum FlagsEnum
        {
            Zero = 0,
            One = 1,
            Two = 2,
            Four = 4,
            Eight = 8
        }

        enum SByteEnum : sbyte { Min = sbyte.MinValue, Max = sbyte.MaxValue }
        enum ByteEnum : byte { Min = byte.MinValue, Max = byte.MaxValue }
        enum Int16Enum : short { Min = short.MinValue, Max = short.MaxValue }
        enum UInt16Enum : ushort { Min = ushort.MinValue, Max = ushort.MaxValue }
        enum Int32Enum : int { Min = int.MinValue, Max = int.MaxValue }
        enum UInt32Enum : uint { Min = uint.MinValue, Max = uint.MaxValue }
        enum Int64Enum : long { Min = long.MinValue, Max = long.MaxValue }
        enum UInt64Enum : ulong { Min = ulong.MinValue, Max = ulong.MaxValue }

        [Theory]
        [InlineData (FlagsEnum.Zero)]
        [InlineData (FlagsEnum.One)]
        [InlineData (FlagsEnum.Two)]
        [InlineData (FlagsEnum.Four)]
        [InlineData (FlagsEnum.Eight)]
        [InlineData (SByteEnum.Min)]
        [InlineData (SByteEnum.Max)]
        [InlineData (ByteEnum.Min)]
        [InlineData (ByteEnum.Max)]
        [InlineData (Int16Enum.Min)]
        [InlineData (Int16Enum.Max)]
        [InlineData (UInt16Enum.Min)]
        [InlineData (UInt16Enum.Max)]
        [InlineData (Int32Enum.Min)]
        [InlineData (Int32Enum.Max)]
        [InlineData (UInt32Enum.Min)]
        [InlineData (UInt32Enum.Max)]
        [InlineData (Int64Enum.Min)]
        [InlineData (Int64Enum.Max)]
        [InlineData (UInt64Enum.Min)]
        [InlineData (UInt64Enum.Max)]
        public void EnumValue (Enum value)
        {
            var reps = (RepresentedObject)manager.Prepare (value);

            reps.Count.ShouldEqual (2);

            var rep = reps [0].ShouldBeInstanceOf<EnumValue> ();

            var type = value.GetType ();
            var underlyingType = type.GetEnumUnderlyingType ();

            rep.RepresentedType.ShouldEqual (reps.RepresentedType);
            rep.RepresentedType.Name.ShouldEqual (type.ToString ());

            rep.UnderlyingType.ResolvedType.ShouldEqual (underlyingType);

            rep.IsFlags.ShouldEqual (type.IsDefined (typeof (FlagsAttribute), false));

            rep.Value.ShouldEqual (Convert.ChangeType (
                value,
                underlyingType,
                CultureInfo.InvariantCulture));

            rep.Names.ShouldEqual (Enum.GetNames (type));

            var values = Enum.GetValues (type);

            rep.Values.Count.ShouldEqual (values.Length);

            for (int i = 0; i < values.Length; i++)
                rep.Values [i].ShouldEqual (Convert.ChangeType (
                    values.GetValue (i),
                    underlyingType,
                    CultureInfo.InvariantCulture));
        }

        [Fact]
        public void IInteractiveObject ()
        {
            var interactiverObject = new DictionaryInteractiveObject (0, manager.Prepare);
            var reps = (RepresentedObject)manager.Prepare (interactiverObject);
            reps.Count.ShouldEqual (2);
            reps [0].ShouldBeInstanceOf<DictionaryInteractiveObject> ();
        }

        [Fact]
        public void ExceptionNode ()
        {
            foreach (var exception in new Exception [] {
                new Exception ("test exception"),
                new Exception ("outer", new Exception ("inner"))
            }) {
                var reps = (RepresentedObject)manager.Prepare (exception);
                reps.Count.ShouldEqual (3);
                var rep = reps [0].ShouldBeInstanceOf<ExceptionNode> ();
                reps [2].ShouldBeInstanceOf<ReflectionInteractiveObject> ();
                rep.Message.ShouldEqual (exception.Message);
            }
        }

        public string MemberProperty { get; set; }

        [Fact]
        public void MemberInfo ()
        {
            var member = GetType ().GetProperty (nameof (MemberProperty));

            var reps = (RepresentedObject)manager.Prepare (member);

            reps.Count.ShouldEqual (3);

            var rep = reps [0].ShouldBeInstanceOf<Property> ();
            rep.Name.ShouldEqual (member.Name);
            rep.DeclaringType.Name.Namespace.ShouldEqual (member.DeclaringType.Namespace);
            rep.DeclaringType.Name.Name.ShouldEqual (member.DeclaringType.Name);
            rep.PropertyType.Name.Namespace.ShouldEqual (member.PropertyType.Namespace);
            rep.PropertyType.Name.Name.ShouldEqual (member.PropertyType.Name);
            rep.Getter.ShouldNotBeNull ();
            rep.Setter.ShouldNotBeNull ();

            reps [2].ShouldBeInstanceOf<ReflectionInteractiveObject> ();
        }

        #endregion

        #region RepresentationManager.Prepare: raw types

        // Raw types do not need to be boxed into serialization-safe containers.
        // All supported runtimes can directly handle the types in compatible ways.

        sealed class EditAllTheThingsRepresentationProvider : RepresentationProvider
        {
            public override IEnumerable<object> ProvideRepresentations (object obj)
            {
                yield return new Representation (obj, canEdit: true);
            }
        }

        void AssertRaw (Array values, bool hasInteractiveRepresentation)
        {
            for (int i = 0; i < values.Length; i++)
                AssertRaw (values.GetValue (i), hasInteractiveRepresentation);
        }

        void AssertRaw (object value, bool hasInteractiveRepresentation)
        {
            value.ShouldNotBeNull ();

            manager = new RepresentationManager (); // [SetUp] doesn't know we're looping

            for (int i = 0; i < 2; i++) {
                if (i == 1)
                    manager.AddProvider (new EditAllTheThingsRepresentationProvider ());

                var valueType = value.GetType ();
                var reps = (RepresentedObject)manager.Prepare (value);

                reps.Count.ShouldEqual (hasInteractiveRepresentation ? 3 : 2);
                reps.RepresentedType.ResolvedType.ShouldEqual (valueType);

                reps [0].GetType ().ShouldEqual (valueType);
                reps [0].ShouldEqual (value);

                reps.GetRepresentation (0).CanEdit.ShouldEqual (i == 1);
            }
        }

        [Theory]
        [InlineData (true)]
        [InlineData (false)]
        [InlineData (Char.MinValue)]
        [InlineData (Char.MaxValue)]
        [InlineData (SByte.MinValue)]
        [InlineData (SByte.MaxValue)]
        [InlineData (Byte.MinValue)]
        [InlineData (Byte.MaxValue)]
        [InlineData (Int16.MinValue)]
        [InlineData (UInt16.MaxValue)]
        [InlineData (Int32.MinValue)]
        [InlineData (UInt32.MaxValue)]
        [InlineData (Int64.MinValue)]
        [InlineData (UInt64.MaxValue)]
        [InlineData (Single.MinValue)]
        [InlineData (Single.MaxValue)]
        [InlineData (Single.Epsilon)]
        [InlineData (Single.PositiveInfinity)]
        [InlineData (Single.NegativeInfinity)]
        [InlineData (Single.NaN)]
        [InlineData (Double.MinValue)]
        [InlineData (Double.MaxValue)]
        [InlineData (Double.Epsilon)]
        [InlineData (Double.PositiveInfinity)]
        [InlineData (Double.NegativeInfinity)]
        [InlineData (Double.NaN)]
        [InlineData (Math.PI)]
        [InlineData (Math.E)]
        public void Raw_TypeCode_Constable (object value)
            => AssertRaw (value, hasInteractiveRepresentation: false);

        [Theory]
        [InlineData ("")]
        [InlineData ("🙀 🐖 💨")]
        public void Raw_TypeCode_String (string value)
            => AssertRaw (value, hasInteractiveRepresentation: true);

        [Fact]
        public void Raw_TypeCode_DateTime ()
            => AssertRaw (
                new [] {
                    DateTime.MinValue,
                    DateTime.MaxValue,
                    DateTime.Now,
                    DateTime.UtcNow,
                    DateTime.Today
                },
                hasInteractiveRepresentation: true);

        [Fact]
        public void Raw_TypeCode_Decimal ()
            => AssertRaw (
                new [] {
                    Decimal.MinValue,
                    Decimal.MaxValue,
                    Decimal.Zero,
                    Decimal.MinusOne,
                    Decimal.One
                },
                hasInteractiveRepresentation: false);

        [Fact]
        public void Raw_TimeSpan ()
            => AssertRaw (
                new [] {
                    TimeSpan.Zero,
                    TimeSpan.MinValue,
                    TimeSpan.MaxValue
                },
                hasInteractiveRepresentation: true);

        [Fact]
        public void Raw_Guid ()
            => AssertRaw (
                new [] {
                    Guid.Empty,
                    Guid.NewGuid ()
                },
                hasInteractiveRepresentation: true);

        #endregion

        #region RepresentationManager.Prepare: WordSizedNumber

        void AssertWordSizedNumber<T> (T [] values, WordSizedNumberFlags expectedFlags)
        {
            manager = new RepresentationManager (); // [SetUp] doesn't know we're looping
            //#if MAC || IOS
            //manager.AddProvider (new Unified.UnifiedRepresentationProvider ());
            //#endif

            for (int i = 0; i < values.Length; i++) {
                var reps = (RepresentedObject)manager.Prepare (values [i]);
                reps.Count.ShouldEqual (2);
                var word = reps [0].ShouldBeInstanceOf<WordSizedNumber> ();
                word.Flags.ShouldEqual (expectedFlags);
                word.Value.ToString ().ShouldEqual (values [i].ToString ());
            }
        }

        [Fact]
        public void WordSizedNumber_IntPtr ()
            => AssertWordSizedNumber (
                new [] {
                    IntPtr.Zero,
                    (IntPtr)(IntPtr.Size == 4 ? Int32.MinValue : Int64.MinValue),
                    (IntPtr)(IntPtr.Size == 4 ? Int32.MaxValue : Int64.MaxValue)
                },
                WordSizedNumberFlags.Pointer | WordSizedNumberFlags.Signed);

        [Fact]
        public void WordSizedNumber_UIntPtr ()
            => AssertWordSizedNumber (
                new [] {
                    UIntPtr.Zero,
                    (UIntPtr)(UIntPtr.Size == 4 ? UInt32.MinValue : UInt64.MinValue),
                    (UIntPtr)(UIntPtr.Size == 4 ? UInt32.MaxValue : UInt64.MaxValue)
                },
                WordSizedNumberFlags.Pointer);

        #if false && (MAC || IOS)

        [Fact]
        public void WordSizedNumber_NInt ()
            => AssertWordSizedNumber (
                new [] {
                    nint.MinValue,
                    nint.MaxValue
                },
                WordSizedNumberFlags.Signed);

        [Fact]
        public void WordSizedNumber_NUint ()
            => AssertWordSizedNumber (
                new [] {
                    nuint.MinValue,
                    nuint.MaxValue
                },
                WordSizedNumberFlags.None);

        [Fact]
        public void WordSizedNumber_NFloat ()
            => AssertWordSizedNumber (
                new [] {
                    nfloat.MinValue,
                    nfloat.MaxValue,
                    nfloat.Epsilon,
                    nfloat.PositiveInfinity,
                    nfloat.NegativeInfinity,
                    nfloat.NaN
                },
                WordSizedNumberFlags.Real);

        #endif

        #endregion

        [Fact]
        public void RootObject_AllowISerializableObject ()
        {
            var reps = (RepresentedObject)manager.Prepare (new Color (0.5, 1, 0.25, 0.3));

            reps.Count.ShouldEqual (3);

            reps [0].ShouldBeInstanceOf<Color> ();
            reps [reps.Count - 1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();
        }

        [Fact]
        public void ChildObject_AllowISerializableObject ()
        {
            var reps = (RepresentedObject)manager.Prepare (new {
                Color = new Color (0.5, 1, 0.25, 0.3),
                Point = new Point (10, 20),
                String = "hello"
            });

            reps.Count.ShouldEqual (2);

            reps [1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();

            var root = (ReflectionInteractiveObject)reps [1];
            root.Interact (new InteractiveObject.ReadAllMembersInteractMessage ());

            root.Members.ShouldNotBeNull ();
            root.Values.ShouldNotBeNull ();

            root.Members.Length.ShouldEqual (3);
            root.Values.Length.ShouldEqual (3);

            var colorRep = root.Values [0].ShouldBeInstanceOf<RepresentedObject> ();
            colorRep.Count.ShouldEqual (3);
            colorRep [0].ShouldBeInstanceOf<Color> ();
            colorRep [colorRep.Count - 1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();

            var pointRep = root.Values [1].ShouldBeInstanceOf<RepresentedObject> ();
            pointRep.Count.ShouldEqual (3);
            pointRep [0].ShouldBeInstanceOf<Point> ();
            pointRep [pointRep.Count - 1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();

            var stringRep = root.Values [2].ShouldBeInstanceOf<RepresentedObject> ();
            stringRep.Count.ShouldEqual (3);
            stringRep [0].ShouldBeInstanceOf<string> ();
            stringRep [stringRep.Count - 1].ShouldBeInstanceOf<ReflectionInteractiveObject> ();
        }
    }
}