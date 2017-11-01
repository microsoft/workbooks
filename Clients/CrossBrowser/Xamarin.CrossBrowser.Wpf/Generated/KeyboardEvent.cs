//
// WARNING - GENERATED CODE - DO NOT EDIT
//
// KeyboardEvent.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2015-2016 Xamarin Inc. All rights reserved.

using System;
using mshtml;

namespace Xamarin.CrossBrowser
{
    public partial class KeyboardEvent : UIEvent
    {
        internal KeyboardEvent (ScriptContext context, IDOMKeyboardEvent comObject) : base (context, (IDOMUIEvent)comObject)
        {
        }

        public bool AltKey {
            get {
                return ((IDOMKeyboardEvent)ComObject).altKey;
            }
        }

        public bool CtrlKey {
            get {
                return ((IDOMKeyboardEvent)ComObject).ctrlKey;
            }
        }

        public bool MetaKey {
            get {
                return ((IDOMKeyboardEvent)ComObject).metaKey;
            }
        }

        public bool ShiftKey {
            get {
                return ((IDOMKeyboardEvent)ComObject).shiftKey;
            }
        }

        public bool Repeat {
            get {
                return ((IDOMKeyboardEvent)ComObject).repeat;
            }
        }

        public int KeyCode {
            get {
                return ((IDOMKeyboardEvent)ComObject).keyCode;
            }
        }

        public int CharCode {
            get {
                return ((IDOMKeyboardEvent)ComObject).charCode;
            }
        }

        public string Key {
            get {
                return ((IDOMKeyboardEvent)ComObject).key;
            }
        }
    }
}