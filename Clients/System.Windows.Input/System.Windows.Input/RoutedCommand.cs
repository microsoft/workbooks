//
// RoutedCommand.cs
//
// Author:
//   Aaron Bockover <abock@xamarin.com>
//
// Copyright 2017 Microsoft. All rights reserved.

using System.Runtime.InteropServices;

using AppKit;
using Foundation;
using ObjCRuntime;

namespace System.Windows.Input
{
    public class RoutedCommand : ICommand
    {
        public sealed class ParameterProxy : NSObject
        {
            public object Value { get; }

            ParameterProxy (object value)
            {
                Value = value;
            }

            internal static ParameterProxy Create (object parameter)
                => parameter == null ? null : new ParameterProxy (parameter);
        }

        readonly bool selectorSupportsParameter;
        readonly Selector executeSelector;
        readonly Selector canExecuteSelector;

        public string Name { get; }
        public Type OwnerType { get; }
        public InputGestureCollection InputGestures { get; }

        #pragma warning disable 0067
        public event EventHandler CanExecuteChanged;
        #pragma warning restore 0067

        public RoutedCommand (
            string name = "",
            Type ownerType = null,
            InputGestureCollection inputGestures = null)
        {
            Name = name ?? "";
            OwnerType = ownerType;
            InputGestures = inputGestures;

            string selNamePart = null;
            if (Name.Length > 0) {
                // the command name is in the form of an explicit selector, so
                // use that instead of building one out by convention; in this
                // format however, there is no 'CanExecute' variant that can
                // accept a parameter to validate.
                if (Name.IndexOf (':') == Name.Length - 1) {
                    executeSelector = new Selector (Name);
                    return;
                }

                selNamePart = "_" + Name;
            }

            selectorSupportsParameter = true;

            executeSelector = new Selector (
                $"RoutedCommand_Execute_{OwnerType.Name}{selNamePart}:parameter:");

            canExecuteSelector = new Selector (
                $"RoutedCommand_CanExecute_{OwnerType.Name}{selNamePart}:parameter:");
        }

        void ICommand.Execute (object parameter)
            => Execute (parameter, null);

        [DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        static extern void objc_msgSend_void_IntPtr (
            IntPtr handle, IntPtr selector, IntPtr sender);

        [DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        static extern void objc_msgSend_void_IntPtr_IntPtr
            (IntPtr handle, IntPtr selector, IntPtr sender, IntPtr parameter);

        public void Execute (object parameter, NSObject source)
        {
            var responder = ResponderForSelector (executeSelector);
            if (responder == null)
                return;

            if (selectorSupportsParameter) {
                var parameterProxy = ParameterProxy.Create (parameter);
                try {
                    objc_msgSend_void_IntPtr_IntPtr (
                        responder.Handle,
                        executeSelector.Handle,
                        source == null ? IntPtr.Zero : source.Handle,
                        parameterProxy == null ? IntPtr.Zero : parameterProxy.Handle);
                    return;
                } finally {
                    parameterProxy?.Dispose ();
                }
            }

            objc_msgSend_void_IntPtr (
                responder.Handle,
                executeSelector.Handle,
                source == null ? IntPtr.Zero : source.Handle);
        }

        static NSResponder ResponderForSelector (Selector selector)
        {
            var responder = NSApplication.SharedApplication?.KeyWindow?.FirstResponder;
            while (responder != null) {
                if (responder.RespondsToSelector (selector))
                    return responder;
                responder = responder.NextResponder;
            }
            return null;
        }

        bool ICommand.CanExecute (object parameter)
            => CanExecute (parameter, null);

        [DllImport (Constants.ObjectiveCLibrary, EntryPoint = "objc_msgSend")]
        static extern bool objc_msgSend_bool_IntPtr_IntPtr (
            IntPtr handle, IntPtr selector, IntPtr sender, IntPtr parameter);

        public bool CanExecute (object parameter, NSObject source)
        {
            if (canExecuteSelector != null) {
                var responder = ResponderForSelector (canExecuteSelector);
                if (responder != null) {
                    var parameterProxy = ParameterProxy.Create (parameter);
                    try {
                        return objc_msgSend_bool_IntPtr_IntPtr (
                            responder.Handle,
                            executeSelector.Handle,
                            source == null ? IntPtr.Zero : source.Handle,
                            parameterProxy == null ? IntPtr.Zero : parameterProxy.Handle);
                    } finally {
                        parameterProxy?.Dispose ();
                    }
                }
            }

            return ResponderForSelector (executeSelector) != null;
        }
    }
}