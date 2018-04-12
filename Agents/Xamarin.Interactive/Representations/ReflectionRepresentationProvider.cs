//
// Author:
//   Bojan Rajkovic <bojan.rajkovic@microsoft.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Xamarin.Interactive.Logging;
using Xamarin.Interactive.Serialization;

namespace Xamarin.Interactive.Representations
{
    sealed class ReflectionRepresentationProvider : RepresentationProvider
    {
        const string TAG = nameof (ReflectionRepresentationProvider);

        public static void AgentHasIntegratedWith (IAgentIntegration integration)
        {
            if (integration.GetType ().Name == "OxyPlotAgentIntegration")
                enableOxyPlotSupport = false;
        }

        public override IEnumerable<object> ProvideRepresentations (object obj)
        {
            yield return ProvideSingleRepresentation (obj);
        }

        IRepresentationObject ProvideSingleRepresentation (object obj)
        {
            var type = obj.GetType ();

            switch (type.FullName) {
            case "System.Drawing.Image":
            case "System.Drawing.Bitmap":
                return ImageFromSystemDrawingImage (type, obj);
            case "XPlot.Plotly.PlotlyChart":
            case "XPlot.GoogleCharts.GoogleChart":
            case "XPlot.D3.ForceLayoutChart":
                return HtmlFromXPlotPlot (type, obj);
            case "OxyPlot.PlotModel":
                return SvgFromOxyPlot (type, obj);
            }

            return null;
        }

        #region System.Drawing Helpers

        static Type SystemDrawingImageType;
        static Type SystemDrawingImageFormatType;
        static MethodInfo SystemDrawingSave;
        static ConstructorInfo SystemDrawingImageFormatCtor;
        static PropertyInfo SystemDrawingWidth;
        static PropertyInfo SystemDrawingHeight;
        static PropertyInfo SystemDrawingRawFormat;
        static PropertyInfo SystemDrawingImageFormatGuid;

        const string SystemDrawingPngGuid = "b96b3caf-0728-11d3-9d7b-0000f81ef32e";
        const string SystemDrawingJpegGuid = "b96b3cae-0728-11d3-9d7b-0000f81ef32e";
        const string SystemDrawingGifGuid = "b96b3cb0-0728-11d3-9d7b-0000f81ef32e";

        static void PopulateSystemDrawingReflectedInfo (Assembly sdiAssembly)
        {
            if (sdiAssembly == null)
                throw new ArgumentNullException (nameof (sdiAssembly));

            // If we've got method definitions for at least one method, we've already done this.
            if (SystemDrawingSave != null)
                return;

            SystemDrawingImageType = sdiAssembly.GetType ("System.Drawing.Image");
            SystemDrawingImageFormatType = sdiAssembly.GetType ("System.Drawing.Imaging.ImageFormat");

            var saveArgumentTypes = new [] { typeof (Stream), SystemDrawingImageFormatType };
            SystemDrawingSave = SystemDrawingImageType.GetRuntimeMethod ("Save", saveArgumentTypes);
            SystemDrawingWidth = SystemDrawingImageType.GetRuntimeProperty ("Width");
            SystemDrawingHeight = SystemDrawingImageType.GetRuntimeProperty ("Height");
            SystemDrawingRawFormat = SystemDrawingImageType.GetRuntimeProperty ("RawFormat");

            SystemDrawingImageFormatCtor = SystemDrawingImageFormatType.GetTypeInfo ().DeclaredConstructors
                .Single (ctor => {
                    var parameters = ctor.GetParameters ();
                    return parameters.Length == 1 && parameters [0].ParameterType == typeof (Guid);
                });
            SystemDrawingImageFormatGuid = SystemDrawingImageFormatType.GetRuntimeProperty ("Guid");
        }

        static Image ImageFromSystemDrawingImage (
            object systemDrawingImage,
            object imageFormat,
            ImageFormat xirImageFormat)
        {
            var width = (int)SystemDrawingWidth.GetValue (systemDrawingImage);
            var height = (int)SystemDrawingHeight.GetValue (systemDrawingImage);
            var stream = new MemoryStream ();
            SystemDrawingSave.Invoke (systemDrawingImage, new object [] { stream, imageFormat });
            var imageData = stream.ToArray ();
            return new Image (xirImageFormat, imageData, width, height);
        }

        Image ImageFromSystemDrawingImage (Type type, object value)
        {
            try {
                PopulateSystemDrawingReflectedInfo (type.GetTypeInfo ().Assembly);

                // Check the raw format. If it's a PNG or JPEG image, we can use the MemoryBmp
                // representation and get a passed-through stream.
                var rawFormat = SystemDrawingRawFormat.GetValue (value);
                var formatGuid = (Guid)SystemDrawingImageFormatGuid.GetValue (rawFormat);

                ImageFormat xirImageFormat = ImageFormat.Png;
                object imageFormat;

                switch (formatGuid.ToString ("D")) {
                case SystemDrawingPngGuid:
                    xirImageFormat = ImageFormat.Png;
                    imageFormat = SystemDrawingImageFormatCtor.Invoke (
                        new object [] { new Guid (SystemDrawingPngGuid) });
                    break;
                case SystemDrawingJpegGuid:
                    xirImageFormat = ImageFormat.Jpeg;
                    imageFormat = SystemDrawingImageFormatCtor.Invoke (
                        new object [] { new Guid (SystemDrawingJpegGuid) });
                    break;
                case SystemDrawingGifGuid:
                    xirImageFormat = ImageFormat.Gif;
                    imageFormat = SystemDrawingImageFormatCtor.Invoke (
                        new object [] { new Guid (SystemDrawingGifGuid) });
                    break;
                default:
                    xirImageFormat = ImageFormat.Png;
                    imageFormat = SystemDrawingImageFormatCtor.Invoke (
                        new object [] { new Guid (SystemDrawingPngGuid) });
                    break;
                }

                return ImageFromSystemDrawingImage (value, imageFormat, xirImageFormat);
            } catch (Exception e) {
                Log.Error (TAG, "Could not get image data for System.Drawing image/bitmap.", e);
                return null;
            }
        }

        #endregion

        #region XPlot Helpers

        static readonly Dictionary<string, MethodInfo> xplotMethodCache
            = new Dictionary<string, MethodInfo> ();

        static VerbatimHtml HtmlFromXPlotPlot (Type xplotType, object value)
        {
            if (xplotType == null)
                throw new ArgumentNullException (nameof (xplotType));

            if (value == null)
                return null;

            var typeName = xplotType.FullName;
            if (!xplotMethodCache.TryGetValue (typeName, out var getHtmlMethod)) {
                getHtmlMethod = xplotType.GetRuntimeMethod ("GetHtml", Array.Empty<Type> ());

                // Drop a warning into the log the first time, subsequent hits will just hit
                // the cache and get a null.
                if (getHtmlMethod == null)
                    Log.Warning (TAG, $"Could not find GetHtml method on XPlot type {typeName}.");

                xplotMethodCache [typeName] = getHtmlMethod;
            }

            if (getHtmlMethod == null)
                return null;

            var plotHtml = (string)getHtmlMethod.Invoke (value, Array.Empty<object> ());
            return new VerbatimHtml (plotHtml);
        }

        #endregion

        #region OxyPlot Helpers

        static bool enableOxyPlotSupport = true;
        static MethodInfo OxyPlot_SvgExporter_ExportToString;

        static Image SvgFromOxyPlot (Type type, object value)
        {
            if (type == null)
                throw new ArgumentNullException (nameof (type));

            if (!enableOxyPlotSupport || value == null)
                return null;

            if (OxyPlot_SvgExporter_ExportToString == null)
                OxyPlot_SvgExporter_ExportToString = type
                    .GetTypeInfo ()
                    .Assembly
                    .GetType ("OxyPlot.SvgExporter")
                    ?.GetRuntimeMethods ()
                    ?.FirstOrDefault (m => m.IsStatic && m.Name == "ExportToString");

            if (OxyPlot_SvgExporter_ExportToString != null)
                return Image.FromSvg ((string)OxyPlot_SvgExporter_ExportToString.Invoke (
                    null, new object [] { value, 600, 400, true, null }));

            return null;
        }

        #endregion
    }
}