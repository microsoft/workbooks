//
// Authors:
//   Bojan Rajkovic <brajkovic@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Runtime.CompilerServices;

namespace WebAssembly
{
    class Runtime
    {
        /// <summary>
        /// Invokes a block of JavaScript code in <paramref name="javascriptCode"/> through
        /// the WASM runtime.
        /// </summary>
        /// <returns>
        /// A stringified value returned from the JavaScript code, or a stringified
        /// error if one occurred.
        /// </returns>
        /// <param name="javascriptCode">
        /// The block of JavaScript to evaluate.
        /// <param>
        /// <param name="gotException">
        /// An out parameter that will be set to 1 if an exception was thrown during JavaScript
        /// evaluation.
        /// </param>
        /// <remarks>
        /// This is moderately safe, as it eval's the code passed to it. This does mean that you
        /// have to be careful about quoting, but it is, on the whole, less of a footgun than
        /// <see cname="m:WebAssembly.Runtime.InvokeJSRaw" />.
        /// </remarks>
        [MethodImpl (MethodImplOptions.InternalCall)]
        public static extern string InvokeJS (string javascriptCode, out int gotException);

        /// <summary>
        /// Invokes a JavaScript function <paramref name="funcName"/> passing the pointer to the
        /// argument <paramref name="arg0"/> to it.
        /// </summary>
        /// <returns>The JavaScript function's return value.</returns>
        /// <param name="exception">
        /// A reference to a variable where any exception thrown by the JavaScript function
        /// can be stored.
        /// </param>
        /// <param name="funcName">The name of the JavaScript function.</param>
        /// <param name="arg0">The argument to the JavaScript function.</param>
        /// <remarks>
        /// The JavaScript function will receive a _naked pointer to the object_
        /// <paramref name="arg0"/>. This means that it is very unsafe to store this
        /// value anywhere on the JavaScript side. Really, you should only pass
        /// strings this way, or objects that you can copy into something that does *not*
        /// have a reference to the naked pointer.
        /// </remarks>
        [MethodImpl (MethodImplOptions.InternalCall)]
        public static extern TRes InvokeJSRaw<T0, TRes> (out string exception, string funcName, T0 arg0);
    }
}
