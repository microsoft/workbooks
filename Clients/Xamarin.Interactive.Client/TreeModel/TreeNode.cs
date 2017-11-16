//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;

namespace Xamarin.Interactive.TreeModel
{
    class TreeNode : INotifyPropertyChanged, INotifyPropertyChanging
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public event PropertyChangingEventHandler PropertyChanging;

        protected virtual void NotifyPropertyChanged ([CallerMemberName] string propertyName = null)
            => PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (propertyName));

        protected virtual void NotifyPropertyChanging ([CallerMemberName] string propertyName = null)
            => PropertyChanging?.Invoke (this, new PropertyChangingEventArgs (propertyName));

        IReadOnlyList<TreeNode> children;
        public IReadOnlyList<TreeNode> Children {
            get { return children; }
            set {
                if (children != value) {
                    NotifyPropertyChanging ();
                    children = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        object representedObject;
        public virtual object RepresentedObject {
            get { return representedObject; }
            set {
                if (representedObject != value) {
                    representedObject = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        string id;
        public string Id {
            get { return id; }
            set {
                if (id != value) {
                    id = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        string iconName;
        public string IconName {
            get { return iconName; }
            set {
                if (iconName != value) {
                    iconName = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        string name;
        public virtual string Name {
            get { return name; }
            set {
                if (name != value) {
                    name = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        string toolTip;
        public string ToolTip {
            get { return toolTip; }
            set {
                if (toolTip != value) {
                    toolTip = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        bool isExpanded;
        public bool IsExpanded {
            get { return isExpanded; }
            set {
                if (isExpanded != value) {
                    isExpanded = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        bool isSelectable;
        public bool IsSelectable {
            get { return isSelectable; }
            set {
                if (isSelectable != value) {
                    isSelectable = value;
                    NotifyPropertyChanged ();
                    NotifyPropertyChanged (nameof (IsSelected));
                }
            }
        }

        bool isSelected;
        public bool IsSelected {
            get => isSelected && isSelectable;
            set {
                if (isSelected != value) {
                    isSelected = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        bool isMouseOver;
        public bool IsMouseOver {
            get => isMouseOver;
            set {
                if (isMouseOver != value) {
                    isMouseOver = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        bool isRenamable;
        public bool IsRenamable {
            get { return isRenamable; }
            set {
                if (isRenamable != value) {
                    isRenamable = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        bool isEditing;
        public bool IsEditing {
            get { return isEditing; }
            set {
                if (isEditing != value) {
                    isEditing = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        IReadOnlyList<RoutedUICommand> commands;
        public IReadOnlyList<RoutedUICommand> Commands {
            get { return commands; }
            set {
                if (commands != value) {
                    commands = value;
                    NotifyPropertyChanged ();
                }
            }
        }

        RoutedUICommand defaultCommand;
        public RoutedUICommand DefaultCommand {
            get { return defaultCommand; }
            set {
                if (defaultCommand != value) {
                    defaultCommand = value;
                    NotifyPropertyChanged ();
                }
            }
        }
    }
}