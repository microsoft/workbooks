//
// Author:
//   Sandy Armstrong <sandy@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.ComponentModel;

namespace Xamarin.Interactive.Core
{
    public class ChangeableWrapper<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        T val;
        public T Value {
            get { return val; }
            set {
                if (!CanWrite)
                    throw new InvalidOperationException ();
                val = value;
                RaisePropertyChanged ();
            }
        }

        public bool CanWrite { get; }

        public ChangeableWrapper (T value, bool canWrite = false)
        {
            val = value;
            CanWrite = canWrite;
        }

        public void RaisePropertyChanged ()
        {
            if (!CanWrite)
                throw new InvalidOperationException ();
            PropertyChanged?.Invoke (this, new PropertyChangedEventArgs (nameof (Value)));
        }
    }
}

