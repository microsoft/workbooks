//
// InspectView.cs
//
// Authors:
//   Aaron Bockover <abock@xamarin.com>
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright 2014-2015 Xamarin Inc. All rights reserved.
// Copyright 2016-2017 Microsoft. All rights reserved.

using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using Xamarin.Interactive.Core;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Remote
{
    [Serializable]
    class InspectView : ISerializable, IDeserializationCallback, IInspectView
    {
        public static Func<InspectView, object> PeerFactory { get; set; }

        public InspectView ()
        {
        }

        protected InspectView (SerializationInfo info, StreamingContext context)
        {
            SetHandle (info.GetInt64 (nameof(Handle)));
            X = info.GetDouble (nameof(X));
            Y = info.GetDouble (nameof(Y));
            Width = info.GetDouble (nameof(Width));
            Height = info.GetDouble (nameof(Height));
            Type = info.GetString (nameof(Type));
            PublicType = info.GetString (nameof(PublicType));
            PublicCSharpType = info.GetString (nameof(PublicCSharpType));
            DisplayName = info.GetString (nameof(DisplayName));
            Description = info.GetString (nameof(Description));
            CapturedImage = info.GetValue<byte[]> (nameof(CapturedImage));
            Layer = info.GetValue<InspectView> (nameof(Layer));
            Kind = info.GetValue<ViewKind> (nameof(Kind));
            subviews = info.GetValue<List<InspectView>> (nameof(Subviews));
            sublayers = info.GetValue<List<InspectView>> (nameof(Sublayers));
            Visibility = info.GetValue<ViewVisibility> (nameof(Visibility));
            Transform = info.GetValue<ViewTransform> (nameof (Transform));
        }

        void IDeserializationCallback.OnDeserialization (object sender)
        {
            OnDeserialization (sender);
        }

        protected virtual void OnDeserialization (object sender)
        {
            Peer = PeerFactory?.Invoke (this);

            if (subviews != null) {
                foreach (var subview in subviews)
                    subview.Parent = this;
            }

            if (sublayers != null) {
                foreach (var sublayer in sublayers)
                    sublayer.Parent = this;
            }

            if (Layer != null)
                Layer.Parent = this;
        }

        void ISerializable.GetObjectData (SerializationInfo info, StreamingContext context)
        {
            GetObjectData (info, context);
        }

        protected virtual void GetObjectData (SerializationInfo info, StreamingContext context)
        {
            info.AddValue (nameof(Handle), Handle);
            info.AddValue (nameof(X), X);
            info.AddValue (nameof(Y), Y);
            info.AddValue (nameof(Width), Width);
            info.AddValue (nameof(Height), Height);
            info.AddValue (nameof(Type), Type);
            info.AddValue (nameof(PublicType), PublicType);
            info.AddValue (nameof(PublicCSharpType), PublicCSharpType);
            info.AddValue (nameof(DisplayName), DisplayName);
            info.AddValue (nameof(Description), Description);
            info.AddValue (nameof(Subviews), subviews);
            info.AddValue (nameof(CapturedImage), CapturedImage);
            info.AddValue (nameof(Layer), Layer);
            info.AddValue (nameof(Sublayers), Sublayers);
            info.AddValue (nameof(Kind), Kind);
            info.AddValue (nameof(Visibility), Visibility);
            info.AddValue (nameof(Transform), Transform);
        }

        List<InspectView> subviews;
        List<InspectView> sublayers;

        public object Peer { get; private set; }

        public InspectView Parent { get; set; }

        public long Handle { get; private set; }
        public string Type { get; set; }
        public string PublicType { get; set; }
        public string PublicCSharpType { get; set; }
        public string DisplayName { get; set; }
        public ViewVisibility Visibility { get; set; }
        public string Description { get; set; }

        public double X { get; set; }
        public double Y { get; set; }
        public double Width { get; set; }
        public double Height { get; set; }

        public ViewKind Kind { get; set; }

        public byte[] BestCapturedImage {
            get { return Layer?.CapturedImage ?? CapturedImage; }
        }

        public IEnumerable<InspectView> Children {
            get {
                if (Subviews != null)
                    foreach (var subview in Subviews)
                        yield return subview;

                if (Layer != null)
                    yield return Layer;

                if (Sublayers != null)
                    foreach (var sublayer in Sublayers)
                        yield return sublayer;
            }
        }

        public InspectView Layer { get; set; }
        public List<InspectView> Sublayers {
            get { return sublayers; }
            set {
                sublayers = null;
                if (value == null)
                    return;
                foreach (InspectView layer in value)
                    AddSublayer (layer);
            }
        }

        public List<InspectView> Subviews {
            get { return subviews; }
            set {
                subviews = null;
                if (value == null)
                    return;
                foreach (InspectView view in value)
                    AddSubview (view);
            }
        }

        public ViewTransform Transform { get; set; }

        public byte [] CapturedImage { get; set; }

        /// <summary>
		/// For multi-window frameworks, like Mac and WPF, the InspectView returned given a handle of
		/// IntPtr.Zero is a "fake" root whose Subviews are the windows of the app. The convention is
		/// to set PublicType to null for these.
		/// </summary>
        public bool IsFakeRoot { get { return String.IsNullOrEmpty (PublicType); } }

        public unsafe void SetHandle (IntPtr handle)
        {
            Handle = (long)(void *)handle;
        }

        public void SetHandle (long handle)
        {
            Handle = handle;
        }

        public InspectView Root {
            get {
                int depth;
                return CalculateRootAndDepth (out depth);
            }
        }

        public int Depth {
            get {
                int depth;
                CalculateRootAndDepth (out depth);
                return depth;
            }
        }

        protected virtual void UpdateCapturedImage ()
        {
        }

        protected void PopulateTypeInformationFromObject (object obj)
        {
            SetHandle (ObjectCache.Shared.GetHandle (obj));

            var type = obj.GetType ();
            var publicType = type.GetPublicType ();
            Type = type.FullName;
            PublicType = publicType.FullName;
            PublicCSharpType = publicType.GetCSharpTypeName ();
        }

        public void CaptureAll ()
        {
            foreach (var inspectView in this.TraverseTree (i => i.Children))
                inspectView.UpdateCapturedImage ();
        }

        InspectView CalculateRootAndDepth (out int depth)
        {
            depth = 0;
            var view = this;
            while (true) {
                if (view.Parent == null)
                    return view;
                view = view.Parent;
                depth++;
            }
        }

        public virtual void AddSubview (IInspectView subview) => AddSubview ((InspectView) subview);

        public virtual void AddSubview (InspectView subview)
        {
            if (subviews == null)
                subviews = new List<InspectView> ();
            subview.Parent = this;
            subviews.Add (subview);
        }

        public InspectView FindSelfOrChild (Predicate<InspectView> predicate)
        {
            if (predicate == null)
                throw new ArgumentNullException (nameof (predicate));

            foreach (var inspectView in this.TraverseTree (i => i.Children)) {
                if (predicate (inspectView))
                    return inspectView;
            }
            return null;
        }
        
        public virtual void AddSublayer (InspectView sublayer)
        {
            if (sublayers == null)
                sublayers = new List<InspectView> ();
            sublayer.Parent = this;
            sublayers.Add (sublayer);
        }
    }
}