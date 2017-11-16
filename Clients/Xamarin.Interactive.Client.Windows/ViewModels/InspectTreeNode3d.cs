using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using Xamarin.Interactive.Client.Windows.Views;

namespace Xamarin.Interactive.Client.Windows.ViewModels
{
    interface IInspectTreeModel3D<T>
    {
        void BuildPrimaryPlane (TreeState state);
        T BuildChild (InspectTreeNode node, TreeState state);
        void Add (T child);
    }

    class InspectTreeNode3D : ModelVisual3D, IInspectTreeModel3D<InspectTreeNode3D>
    {
        static readonly double ZSpacing = .1;
        static readonly double zFightIncrement = 1 / 800.0;

        public InspectTreeNode Node { get; }
        static Color FocusColor => Color.FromArgb (128, 128, 128, 255);
        static Color BlurColor => Color.FromArgb (200, 255, 255, 255);
        static Color EmptyColor => Color.FromArgb (1, 255, 255, 255);
        static Color HoverColor => Color.FromArgb (128, 128, 255, 255);

        int childIndex;
        DiffuseMaterial material;

        public InspectTreeNode3D (InspectTreeNode node, TreeState state)
        {
            void NodePropertyChanged (object sender, PropertyChangedEventArgs args)
            {
                switch (args.PropertyName) {
                case nameof (InspectTreeNode.Children):
                    break;
                case nameof (InspectTreeNode.IsSelected):
                case nameof (InspectTreeNode.IsMouseOver):
                    UpdateMaterial ();
                    break;
                case nameof (InspectTreeNode.IsExpanded):
                    break;
                }
            }
            Node = node;
            node.PropertyChanged += NodePropertyChanged;
            childIndex = state.AddChild (node.View);
        }

        void BuildPrimaryPlane (TreeState state)
        {
            var displayMode = state.Mode;
            Brush brush = new SolidColorBrush (EmptyColor);
            var view = Node.View;
            var parent = Node.View.Parent;
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

            if ((parent == null && !Node.View.IsFakeRoot) || (parent?.IsFakeRoot ?? false)) {
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

        void UpdateMaterial ()
        {
            if (material == null)
                return;

            var solid = material.Brush as SolidColorBrush;
            if (solid == null)
                material.Color = Node.IsSelected ? FocusColor : (Node.IsMouseOver ? HoverColor :BlurColor);
            else
                solid.Color = Node.IsSelected ? FocusColor : (Node.IsMouseOver ? HoverColor : EmptyColor);
        }

        void IInspectTreeModel3D<InspectTreeNode3D>.BuildPrimaryPlane (TreeState state) =>
            BuildPrimaryPlane (state);

        public InspectTreeNode3D BuildChild (InspectTreeNode node, TreeState state)
        {
            var child = new InspectTreeNode3D (node, state);
            node.Build3D (child, state);
            return child;
        }

        public void Add (InspectTreeNode3D child) =>
            Children.Add (child);
       
    }
}
