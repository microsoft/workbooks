//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Xamarin.Interactive.Client.ViewInspector;
using Xamarin.Interactive.Inspection;
using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Remote;

namespace Xamarin.Interactive.Client.Windows.Views
{
    class InspectViewNode : ModelVisual3D
    {
        static readonly double ZSpacing = .1;
        static readonly double zFightIncrement = 1 / 800.0;

        public InspectView InspectView { get; }
        static Color FocusColor => Color.FromArgb (128, 128, 128, 255);
        static Color BlurColor => Color.FromArgb (200, 255, 255, 255);
        static Color EmptyColor => Color.FromArgb (1, 255, 255, 255);

        int childIndex;
        DiffuseMaterial material;

        public InspectViewNode (InspectView inspectView, int childIndex)
        {
            if (inspectView == null)
                throw new ArgumentNullException (nameof (inspectView));

            InspectView = inspectView;
            this.childIndex = childIndex;
        }

        public InspectViewNode Rebuild (InspectTreeState state)
        {
            try {
                Content = null;
                Children.Clear ();

                BuildPrimaryPlane (state.Mode);

                state.PushGeneration ();
                var subviews = InspectView.Subviews;
                var nodes = BuildNodesForCollection (subviews, state);

                var sublayers = InspectView.Layer?.Sublayers ?? InspectView.Sublayers;
                nodes = BuildNodesForCollection (sublayers, state);
                state.PopGeneration ();
            } catch (Exception e) {
                Log.Error (nameof (InspectViewNode), $"Could not rebuild InspectViewNode: {e.Message}", e);
            }

            return this;
        }

        InspectViewNode BuildChild (InspectView view, InspectTreeState state)
        {
            var childNode = new InspectViewNode (view, state.AddChild(view));
            Children.Add (childNode);
            childNode.Rebuild (state);
            return childNode;
        }

        List<InspectViewNode> BuildNodesForCollection (
            List<InspectView> collection,
            InspectTreeState state)
        {
            var results = new List<InspectViewNode> ();
            var count = collection != null ? collection.Count : 0;
            for (var i = 0; i < count; i++) {
                var inspectView = collection [i];
                // If the InspectView reports that it's in a Collapsed state, that means it was neither
                // drawn, nor considered for layout. Hidden views were at least considered
                // for layout, so draw them if `showHidden` is true, otherwise only draw Visible views.
                if (inspectView.Visibility == ViewVisibility.Collapsed ||
                    (inspectView.Visibility != ViewVisibility.Visible && !state.ShowHidden))
                    continue;

                results.Add (BuildChild (inspectView, state));
            }
            return results;
        }

        public static void Focus (InspectViewNode root, Predicate<InspectViewNode> predicate)
        {
            foreach (var node in root.TraverseTree (i => i.Children.OfType<InspectViewNode>())) {
                if (predicate (node))
                    node.Focus ();
                else
                    node.Blur ();
            }
        }

        public void Focus ()
        {
            if (material == null)
                return;

            var solid = material.Brush as SolidColorBrush;

            if (solid == null)
                material.Color = FocusColor;
            else
                solid.Color = FocusColor;
        }
        public void Blur ()
        {
            if (material == null)
                return;

            var solid = material.Brush as SolidColorBrush;

            if (solid == null)
                material.Color = BlurColor;
            else
                solid.Color = EmptyColor;
        }

        void BuildPrimaryPlane (DisplayMode displayMode)
        {
            Brush brush = new SolidColorBrush (EmptyColor);
            var view = this.InspectView;
            var matrix = Matrix3D.Identity;

            if (view.Layer != null)
                view = view.Layer;

            var zFightOffset = childIndex * zFightIncrement;
            var zOffset = ZSpacing + zFightOffset;

            if (view.Transform != null) {
                var render = view.Transform;
                matrix = new Matrix3D {
                    M11 = render.M11,
                    M12 = render.M12,
                    M13 = render.M13,
                    M14 = render.M14,
                    M21 = render.M21,
                    M22 = render.M22,
                    M23 = render.M23,
                    M24 = render.M24,
                    M31 = render.M31,
                    M32 = render.M32,
                    M33 = render.M33,
                    M34 = render.M34,
                    OffsetX = render.OffsetX,
                    OffsetY = render.OffsetY,
                    OffsetZ = render.OffsetZ + zOffset
                };
            }

            var size = new Size (view.Width, view.Height);
            var visual = new DrawingVisual ();
            using (var context = visual.RenderOpen ()) {

                if (view.BestCapturedImage != null && displayMode.HasFlag (DisplayMode.Content)) {
                    var bitmap = new BitmapImage ();
                    bitmap.BeginInit ();
                    bitmap.StreamSource = new MemoryStream (view.BestCapturedImage);
                    bitmap.EndInit ();

                    context.DrawImage (bitmap, new Rect (size));
                }

                if (displayMode.HasFlag (DisplayMode.Frames))
                    context.DrawRectangle (
                        null,
                        new Pen (new SolidColorBrush (Color.FromRgb (0xd3, 0xd3, 0xd3)), 0.5),
                        new Rect (size));
            }

            brush = new ImageBrush { ImageSource = new DrawingImage (visual.Drawing) };

            var geometry = new MeshGeometry3D () {
                Positions = new Point3DCollection {
                    new Point3D (0, 0, 0),
                    new Point3D (0, -size.Height, 0),
                    new Point3D (size.Width, -size.Height, 0),
                    new Point3D (size.Width, 0, 0)
                },
                TextureCoordinates = new PointCollection {
                    new Point (0,0),
                    new Point (0,1),
                    new Point (1,1),
                    new Point (1,0)
                },
                TriangleIndices = new Int32Collection { 0, 1, 2, 0, 2, 3 },
            };

            var backGeometry = new MeshGeometry3D () {
                Positions = geometry.Positions,
                TextureCoordinates = geometry.TextureCoordinates,
                TriangleIndices = geometry.TriangleIndices,
                Normals = new Vector3DCollection {
                    new Vector3D (0, 0, -1),
                    new Vector3D (0, 0, -1),
                    new Vector3D (0, 0, -1),
                    new Vector3D (0, 0, -1)
                }
            };

            material = new DiffuseMaterial (brush) { Color = BlurColor };

            Content = new Model3DGroup () {
                Children = new Model3DCollection {
                    new GeometryModel3D {
                        Geometry = geometry,
                        Material = material
                    },
                    new GeometryModel3D {
                        Geometry = backGeometry,
                        BackMaterial = material,
                    },
                },
                Transform = new ScaleTransform3D {
                    ScaleX = Math.Ceiling (view.Width) / size.Width,
                    ScaleY = -Math.Ceiling (view.Height) / size.Height,
                    ScaleZ = 1
                }
            };

            if ((InspectView.Parent == null && !InspectView.IsFakeRoot) || (InspectView.Parent?.IsFakeRoot ?? false)) {
                var unitScale = 1.0 / Math.Max (view.Width, view.Height);

                Transform = new Transform3DGroup {
                    Children = new Transform3DCollection {
                        new TranslateTransform3D {
                            OffsetX = -view.Width / 2.0,
                            OffsetY = -view.Height / 2.0,
                            OffsetZ = zOffset
                        },
                        new ScaleTransform3D (unitScale, -unitScale, 1),
                    }
                };
            } else {
                if (view.Transform != null) {
                    Transform = new MatrixTransform3D () { Matrix = matrix };
                } else {
                    Transform = new TranslateTransform3D (view.X, view.Y, zOffset);
                }
            }
        }
    }
}
