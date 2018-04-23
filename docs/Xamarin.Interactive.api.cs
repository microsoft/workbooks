[assembly: AssemblyConfiguration ("Release")]
[assembly: AssemblyCopyright ("Copyright 2016-2018 Microsoft. All rights reserved.\nCopyright 2014-2016 Xamarin Inc. All rights reserved.")]
[assembly: AssemblyProduct ("Xamarin.Interactive")]
[assembly: AssemblyTitle ("Xamarin.Interactive")]
[assembly: BuildInfo]
[assembly: InternalsVisibleTo ("Workbook Mac App (Desktop Profile)")]
[assembly: InternalsVisibleTo ("Workbook Mac App (Mobile Profile)")]
[assembly: InternalsVisibleTo ("workbook")]
[assembly: InternalsVisibleTo ("workbooks-server")]
[assembly: InternalsVisibleTo ("Xamarin Inspector")]
[assembly: InternalsVisibleTo ("Xamarin Workbooks Agent (WPF)")]
[assembly: InternalsVisibleTo ("Xamarin Workbooks")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Client")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Client.Console")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Client.Desktop")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.CodeAnalysis")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Console")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.DotNetCore")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Mac.Desktop")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Mac.Mobile")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Telemetry.Server")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests.Core")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests.Mac")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Tests.Windows")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.VS")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Wpf")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.XS")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Client.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Client.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.DotNetCore")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Forms.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.Forms.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.iOS")]
[assembly: InternalsVisibleTo ("xic")]
[assembly: TargetFramework (".NETStandard,Version=v2.0", FrameworkDisplayName = "")]
namespace Xamarin.Interactive
{
    [AttributeUsage (AttributeTargets.Assembly)]
    public sealed class BuildInfoAttribute : Attribute
    {
        public DateTime Date {
            get;
        }

        public string VersionString {
            get;
        }
    }
    [AttributeUsage (AttributeTargets.Assembly)]
    public sealed class EvaluationContextManagerIntegrationAttribute : Attribute
    {
        public Type IntegrationType {
            get;
        }

        public EvaluationContextManagerIntegrationAttribute (Type integrationType);
    }
    public interface IAgentSynchronizationContext
    {
        SynchronizationContext PeekContext ();

        SynchronizationContext PopContext ();

        SynchronizationContext PushContext (Action<Action> postHandler, Action<Action> sendHandler = null);

        SynchronizationContext PushContext (SynchronizationContext context);
    }
    [JsonObject]
    public sealed class Sdk
    {
        public IReadOnlyList<string> AssemblySearchPaths {
            get;
        }

        public SdkId Id {
            get;
        }

        public string Name {
            get;
        }

        public string Profile {
            get;
        }

        public FrameworkName TargetFramework {
            get;
        }

        public string Version {
            get;
        }

        [JsonConstructor]
        public Sdk (SdkId id, FrameworkName targetFramework, IEnumerable<string> assemblySearchPaths, string name = null, string profile = null, string version = null);

        public static Sdk FromEntryAssembly (SdkId id, string name = null, string profile = null, string version = null);
    }
    public static class SdkExtensions
    {
        public static bool Is (this Sdk sdk, SdkId id);

        public static bool IsNot (this Sdk sdk, SdkId id);
    }
    public struct SdkId : IEquatable<SdkId>
    {
        public static bool operator == (SdkId a, SdkId b);

        public static implicit operator SdkId (string id);

        public static implicit operator string (SdkId id);

        public static bool operator != (SdkId a, SdkId b);

        public static readonly SdkId XamarinIos;

        public static readonly SdkId XamarinMacFull;

        public static readonly SdkId XamarinMacModern;

        public static readonly SdkId XamarinAndroid;

        public static readonly SdkId Wpf;

        public static readonly SdkId ConsoleNetFramework;

        public static readonly SdkId ConsoleNetCore;

        public bool IsNull {
            get;
        }

        public SdkId (string id);

        public bool Equals (SdkId id);

        public override bool Equals (object obj);

        public override int GetHashCode ();

        public override string ToString ();
    }
    public static class Utf8
    {
        public static UTF8Encoding Encoding {
            get;
        }

        public static byte[] GetBytes (string value);

        public static string GetString (byte[] bytes);

        public static string GetString (byte[] bytes, int count);

        public static string GetString (byte[] bytes, int index, int count);
    }
}
namespace Xamarin.Interactive.CodeAnalysis
{
    [JsonObject]
    public struct AssemblyLoadResult
    {
        public AssemblyIdentity AssemblyName {
            get;
        }

        public bool InitializedAgentIntegration {
            get;
        }

        public bool Success {
            get;
        }

        [JsonConstructor]
        public AssemblyLoadResult (AssemblyIdentity assemblyName, bool success, bool initializedAgentIntegration);
    }
    public struct CodeCellId : IEquatable<CodeCellId>
    {
        public static bool operator == (CodeCellId a, CodeCellId b);

        public static implicit operator CodeCellId (string id);

        public static implicit operator string (CodeCellId id);

        public static bool operator != (CodeCellId a, CodeCellId b);

        public Guid Id {
            get;
        }

        public Guid ProjectId {
            get;
        }

        public CodeCellId (Guid projectId, Guid id);

        public bool Equals (CodeCellId id);

        public override bool Equals (object obj);

        public override int GetHashCode ();

        public static CodeCellId Parse (string id);

        public override string ToString ();
    }
    [JsonObject]
    public sealed class Compilation
    {
        public CodeCellId CodeCellId {
            get;
        }

        public EvaluationEnvironment EvaluationEnvironment {
            get;
        }

        public AssemblyDefinition ExecutableAssembly {
            get;
        }

        public bool IsResultAnExpression {
            get;
        }

        public IReadOnlyList<AssemblyDefinition> References {
            get;
        }

        public int SubmissionNumber {
            get;
        }

        [JsonConstructor]
        public Compilation (CodeCellId codeCellId, int submissionNumber, EvaluationEnvironment evaluationEnvironment, bool isResultAnExpression, AssemblyDefinition executableAssembly, IReadOnlyList<AssemblyDefinition> references);
    }
    [JsonObject]
    public sealed class TargetCompilationConfiguration
    {
        public IReadOnlyList<string> AssemblySearchPaths {
            get;
        }

        public IReadOnlyList<string> DefaultImports {
            get;
        }

        public IReadOnlyList<string> DefaultWarningSuppressions {
            get;
        }

        public EvaluationContextId EvaluationContextId {
            get;
        }

        public TypeDefinition GlobalStateType {
            get;
        }

        public bool IncludePEImagesInDependencyResolution {
            get;
        }

        public IReadOnlyList<AssemblyDefinition> InitialReferences {
            get;
        }

        public Sdk Sdk {
            get;
        }

        public static TargetCompilationConfiguration CreateInitialForCompilationWorkspace (IReadOnlyList<string> assemblySearchPaths = null);
    }
}
namespace Xamarin.Interactive.CodeAnalysis.Evaluating
{
    [JsonObject]
    public sealed class Evaluation : ICodeCellEvent
    {
        public CodeCellId CodeCellId {
            get;
        }

        public int CultureLCID {
            get;
        }

        public TimeSpan EvaluationDuration {
            get;
        }

        public bool InitializedIntegration {
            get;
        }

        public bool Interrupted {
            get;
        }

        public bool IsNullResult {
            get;
        }

        public IReadOnlyList<AssemblyDefinition> LoadedAssemblies {
            get;
        }

        public EvaluationResultHandling ResultHandling {
            get;
        }

        public IReadOnlyList<object> ResultRepresentations {
            get;
        }

        public IRepresentedType ResultType {
            get;
        }

        public int UICultureLCID {
            get;
        }

        public Evaluation (CodeCellId codeCellId, EvaluationResultHandling resultHandling, object value);
    }
    public sealed class EvaluationAssemblyContext : IDisposable
    {
        public Action<Assembly, AssemblyDefinition> AssemblyResolvedHandler {
            get;
        }

        public EvaluationAssemblyContext (Action<Assembly, AssemblyDefinition> assemblyResolvedHandler = null);

        public void Add (Assembly assembly);

        public void Add (AssemblyDefinition assembly);

        public void AddRange (IEnumerable<AssemblyDefinition> assemblies);

        public void Dispose ();
    }
    public sealed class EvaluationContext
    {
        public IObservable<ICodeCellEvent> Events {
            get;
        }

        public EvaluationContextManager Host {
            get;
        }

        public EvaluationContextId Id {
            get;
        }

        public TargetCompilationConfiguration TargetCompilationConfiguration {
            get;
        }
    }
    public class EvaluationContextGlobalObject
    {
        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Clear all previous REPL results", ShowReturnType = false, LiveInspectOnly = true)]
        public static readonly Guid clear;

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Get or set the current thread culture")]
        public static CultureInfo CurrentCulture {
            get;
            set;
        }

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Get or set the current thread culture by name")]
        public static string CurrentCultureName {
            get;
            set;
        }

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Direct public access to the evaluation context powering the interactive session")]
        public EvaluationContext EvaluationContext {
            get;
            internal set;
        }

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "This help text", ShowReturnType = false)]
        public object help {
            get;
        }

        [EditorBrowsable (EditorBrowsableState.Never)]
        public T __AssignmentMonitor<T> (T value, string symbolName, string nodeType, int spanStart, int spanEnd);

        public T GetObject<T> (long handle);

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Shows declared global variables", ShowReturnType = false)]
        public object GetVars ();

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Render an image from a path or URI")]
        public static Image Image (Uri uri);

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Render an image from a path or URI, or SVG data")]
        public static Image Image (string uriOrTextData);

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Render an image from raw data")]
        public static Image Image (byte[] data);

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Render an image from a stream")]
        public static Task<Image> ImageAsync (Stream stream);

        [EvaluationContextGlobalObject.InteractiveHelpAttribute (Description = "Uses a Stopwatch to time the specified action delegate")]
        public static TimeSpan Time (Action action);
    }
    public struct EvaluationContextId : IEquatable<EvaluationContextId>
    {
        public static bool operator == (EvaluationContextId a, EvaluationContextId b);

        public static explicit operator EvaluationContextId (string id);

        public static implicit operator string (EvaluationContextId id);

        public static bool operator != (EvaluationContextId a, EvaluationContextId b);

        public bool Equals (EvaluationContextId id);

        public override bool Equals (object obj);

        public override int GetHashCode ();

        public override string ToString ();
    }
    public class EvaluationContextManager : IEvaluationContextManager
    {
        public IObservable<ICodeCellEvent> Events {
            get;
        }

        public RepresentationManager RepresentationManager {
            get;
        }

        public IAgentSynchronizationContext SynchronizationContexts {
            get;
        }

        public Task AbortEvaluationAsync (EvaluationContextId evaluationContextId, CancellationToken cancellationToken = default(CancellationToken));

        public Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (CancellationToken cancellationToken = default(CancellationToken));

        public Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (TargetCompilationConfiguration targetCompilationConfiguration, CancellationToken cancellationToken = default(CancellationToken));

        public Task EvaluateAsync (EvaluationContextId evaluationContextId, Compilation compilation, CancellationToken cancellationToken = default(CancellationToken));

        public Task<IReadOnlyList<AssemblyLoadResult>> LoadAssembliesAsync (EvaluationContextId evaluationContextId, IReadOnlyList<AssemblyDefinition> assemblies, CancellationToken cancellationToken = default(CancellationToken));

        public void PublishValueForCell (CodeCellId codeCellId, object result, EvaluationResultHandling resultHandling = EvaluationResultHandling.Replace);

        public void RegisterResetStateHandler (Action handler);

        public Task ResetStateAsync (EvaluationContextId evaluationContextId, CancellationToken cancellationToken = default(CancellationToken));
    }
    [JsonObject]
    public struct EvaluationEnvironment
    {
        public FilePath WorkingDirectory {
            get;
        }

        [JsonConstructor]
        public EvaluationEnvironment (FilePath workingDirectory);
    }
    public sealed class EvaluationInFlight : ICodeCellEvent
    {
        public CodeCellId CodeCellId {
            get;
        }

        public Compilation Compilation {
            get;
        }

        public Evaluation Evaluation {
            get;
        }

        public object OriginalValue {
            get;
        }

        public EvaluationPhase Phase {
            get;
        }
    }
    public enum EvaluationPhase
    {
        None,
        Compiled,
        Evaluated,
        Completed
    }
    public enum EvaluationResultHandling
    {
        Replace,
        Append,
        Ignore
    }
    public interface IEvaluationContextManager
    {
        IObservable<ICodeCellEvent> Events {
            get;
        }

        Task AbortEvaluationAsync (EvaluationContextId evaluationContextId, CancellationToken cancellationToken = default(CancellationToken));

        Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (CancellationToken cancellationToken = default(CancellationToken));

        Task<TargetCompilationConfiguration> CreateEvaluationContextAsync (TargetCompilationConfiguration initialConfiguration, CancellationToken cancellationToken = default(CancellationToken));

        Task EvaluateAsync (EvaluationContextId evaluationContextId, Compilation compilation, CancellationToken cancellationToken = default(CancellationToken));

        Task<IReadOnlyList<AssemblyLoadResult>> LoadAssembliesAsync (EvaluationContextId evaluationContextId, IReadOnlyList<AssemblyDefinition> assemblies, CancellationToken cancellationToken = default(CancellationToken));

        Task ResetStateAsync (EvaluationContextId evaluationContextId, CancellationToken cancellationToken = default(CancellationToken));
    }
    public interface IEvaluationContextManagerIntegration
    {
        void IntegrateWith (EvaluationContextManager evaluationContextManager);
    }
}
namespace Xamarin.Interactive.CodeAnalysis.Events
{
    public interface ICodeCellEvent
    {
        CodeCellId CodeCellId {
            get;
        }
    }
}
namespace Xamarin.Interactive.CodeAnalysis.Resolving
{
    [JsonObject]
    public sealed class AssemblyContent
    {
        public byte[] DebugSymbols {
            get;
        }

        public FilePath Location {
            get;
        }

        public byte[] PEImage {
            get;
        }

        public Stream OpenPEImage ();
    }
    [JsonObject]
    public sealed class AssemblyDefinition
    {
        public AssemblyContent Content {
            get;
        }

        public AssemblyEntryPoint EntryPoint {
            get;
        }

        public IReadOnlyList<AssemblyDependency> ExternalDependencies {
            get;
        }

        public bool HasIntegration {
            get;
        }

        public AssemblyIdentity Name {
            get;
        }

        public AssemblyDefinition (AssemblyIdentity name, FilePath location, string entryPointType = null, string entryPointMethod = null, byte[] peImage = null, byte[] debugSymbols = null, AssemblyDependency[] externalDependencies = null, bool hasIntegration = false);

        public AssemblyDefinition (AssemblyName name, FilePath location, string entryPointType = null, string entryPointMethod = null, byte[] peImage = null, byte[] debugSymbols = null, AssemblyDependency[] externalDependencies = null, bool hasIntegration = false);
    }
    [JsonObject]
    public struct AssemblyDependency
    {
        public byte[] Data {
            get;
        }

        public FilePath Location {
            get;
        }
    }
    [JsonObject]
    public struct AssemblyEntryPoint
    {
        public string MethodName {
            get;
        }

        public string TypeName {
            get;
        }
    }
    [JsonObject]
    public sealed class AssemblyIdentity
    {
        public static implicit operator AssemblyName (AssemblyIdentity assemblyIdentity);

        public string FullName {
            get;
        }

        public string Name {
            get;
        }

        public Version Version {
            get;
        }

        public bool Equals (AssemblyIdentity other);

        public override bool Equals (object obj);

        public override int GetHashCode ();

        public override string ToString ();
    }
    [JsonObject]
    public sealed class TypeDefinition
    {
        public AssemblyDefinition Assembly {
            get;
        }

        public string Name {
            get;
        }

        public Type ResolvedType {
            get;
        }

        [JsonConstructor]
        public TypeDefinition (AssemblyDefinition assembly, string name, Type resolvedType = null);

        public TypeDefinition WithResolvedType (Type resolvedType);
    }
}
namespace Xamarin.Interactive.CodeAnalysis.Workbooks
{
    public static class EvaluationContextGlobalsExtensions
    {
        public static VerbatimHtml AsHtml (this string str);
    }
}
namespace Xamarin.Interactive.Core
{
    [TypeConverter (typeof(FilePath.FilePathTypeConverter))]
    [Serializable]
    public struct FilePath : IComparable<FilePath>, IComparable, IEquatable<FilePath>
    {
        public static bool operator == (FilePath a, FilePath b);

        public static implicit operator FilePath (string path);

        public static implicit operator string (FilePath path);

        public static bool operator != (FilePath a, FilePath b);

        public bool DirectoryExists {
            get;
        }

        public bool Exists {
            get;
        }

        public string Extension {
            get;
        }

        public bool FileExists {
            get;
        }

        public long FileSize {
            get;
        }

        public string FullPath {
            get;
        }

        public bool IsNull {
            get;
        }

        public bool IsRooted {
            get;
        }

        public string Name {
            get;
        }

        public string NameWithoutExtension {
            get;
        }

        public FilePath ParentDirectory {
            get;
        }

        public FilePath (string path);

        public static FilePath Build (params string[] paths);

        public FilePath ChangeExtension (string extension);

        public string Checksum ();

        public FilePath Combine (params string[] paths);

        public int CompareTo (FilePath other);

        public int CompareTo (object obj);

        public DirectoryInfo CreateDirectory ();

        public IEnumerable<FilePath> EnumerateDirectories (string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        public IEnumerable<FilePath> EnumerateFiles (string searchPattern = "*", SearchOption searchOption = SearchOption.TopDirectoryOnly);

        public bool Equals (FilePath other);

        public override bool Equals (object obj);

        public override int GetHashCode ();

        public FilePath GetRelativePath (FilePath relativeToPath);

        public static FilePath GetTempPath ();

        public bool IsChildOfDirectory (FilePath parentDirectory);

        public FileStream OpenRead ();

        public override string ToString ();
    }
}
namespace Xamarin.Interactive.Logging
{
    public static class Log
    {
        public static bool IsInitialized {
            get;
        }

        public static void Commit (LogLevel level, string tag, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Critical (string tag, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Critical (string tag, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Critical (string tag, string message, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Debug (string tag, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Error (string tag, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Error (string tag, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Error (string tag, string message, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Info (string tag, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Verbose (string tag, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Warning (string tag, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Warning (string tag, string message, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);

        public static void Warning (string tag, string message, Exception exception, [CallerMemberName] string callerMemberName = null, [CallerFilePath] string callerFilePath = null, [CallerLineNumber] int callerLineNumber = 0);
    }
    [Serializable]
    public struct LogEntry
    {
        public string CallerFilePath {
            get;
        }

        public int CallerLineNumber {
            get;
        }

        public string CallerMemberName {
            get;
        }

        public LogLevel Level {
            get;
        }

        public string Message {
            get;
        }

        public TimeSpan RelativeTime {
            get;
        }

        public string Tag {
            get;
        }

        public DateTime Time {
            get;
        }

        public override string ToString ();
    }
    [Serializable]
    public enum LogLevel
    {
        Verbose,
        Debug,
        Info,
        Warning,
        Error,
        Critical
    }
}
namespace Xamarin.Interactive.Representations
{
    [JsonObject]
    public sealed class Color : IRepresentationObject, IEquatable<Color>
    {
        public double Alpha {
            get;
        }

        public double Blue {
            get;
        }

        public ColorSpace ColorSpace {
            get;
        }

        public double Green {
            get;
        }

        public double Red {
            get;
        }

        [JsonConstructor]
        public Color (double red, double green, double blue, double alpha = 1.0);

        public bool Equals (Color other);

        public override bool Equals (object obj);

        public override int GetHashCode ();
    }
    public enum ColorSpace
    {
        Rgb
    }
    [JsonObject]
    public sealed class GeoLocation : IRepresentationObject
    {
        public double? Altitude {
            get;
        }

        public double? Bearing {
            get;
        }

        public double? HorizontalAccuracy {
            get;
        }

        public double Latitude {
            get;
        }

        public double Longitude {
            get;
        }

        public double? Speed {
            get;
        }

        public DateTime Timestamp {
            get;
        }

        public double? VerticalAccuracy {
            get;
        }

        [JsonConstructor]
        public GeoLocation (double latitude, double longitude, double? altitude = null, double? horizontalAccuracy = null, double? verticalAccuracy = null, double? speed = null, double? bearing = null, DateTime timestamp = default(DateTime));
    }
    [JsonObject]
    public sealed class GeoPolyline : IRepresentationObject
    {
        public IReadOnlyList<GeoLocation> Points {
            get;
        }

        [JsonConstructor]
        public GeoPolyline (IReadOnlyList<GeoLocation> points);
    }
    [JsonObject]
    public sealed class Image : IRepresentationObject
    {
        public byte[] Data {
            get;
        }

        public ImageFormat Format {
            get;
        }

        public int Height {
            get;
        }

        public double Scale {
            get;
        }

        public int Width {
            get;
        }

        [JsonConstructor]
        public Image (ImageFormat format, byte[] data, int width = 0, int height = 0, double scale = 1.0);

        public static Image FromData (byte[] data, int width = 0, int height = 0);

        public static Image FromData (string data, int width = 0, int height = 0);

        public static async Task<Image> FromStreamAsync (Stream stream, CancellationToken cancellationToken = default(CancellationToken));

        public static Image FromSvg (string svgData, int width = 0, int height = 0);

        public static Image FromUri (string uri, int width = 0, int height = 0);
    }
    public enum ImageFormat
    {
        Unknown,
        Png,
        Jpeg,
        Gif,
        Rgba32,
        Rgb24,
        Bgra32,
        Bgr24,
        Uri,
        Svg
    }
    [JsonObject]
    public sealed class Point : IRepresentationObject
    {
        public double X {
            get;
        }

        public double Y {
            get;
        }

        [JsonConstructor]
        public Point (double x, double y);
    }
    [JsonObject]
    public sealed class Rectangle : IRepresentationObject
    {
        public double Height {
            get;
        }

        public double Width {
            get;
        }

        public double X {
            get;
        }

        public double Y {
            get;
        }

        [JsonConstructor]
        public Rectangle (double x, double y, double width, double height);
    }
    [JsonObject]
    public struct Representation : IRepresentationObject
    {
        public static readonly Representation Empty;

        public bool CanEdit {
            get;
        }

        public object Value {
            get;
        }

        [JsonConstructor]
        public Representation (object value, bool canEdit = false);
    }
    public sealed class RepresentationManager
    {
        public RepresentationManager (RepresentationManagerOptions options = RepresentationManagerOptions.EnforceMainThread | RepresentationManagerOptions.YieldInteractive);

        public void AddProvider (RepresentationProvider provider);

        public void AddProvider (string typeName, Func<object, object> handler);

        public void AddProvider<T> (Func<T, object> handler);

        public void AddProvider<TRepresentationProvider> () where TRepresentationProvider : RepresentationProvider, new();

        public object Prepare (object obj);
    }
    [Flags]
    public enum RepresentationManagerOptions
    {
        None = 0,
        EnforceMainThread = 1,
        YieldOriginal = 2,
        YieldInteractive = 4
    }
    public abstract class RepresentationProvider
    {
        public virtual bool HasSensibleEnumerator (IEnumerable enumerable);

        public virtual IEnumerable<object> ProvideRepresentations (object obj);

        public virtual bool ShouldReadMemberValue (IRepresentedMemberInfo representedMemberInfo, object obj);

        public virtual bool ShouldReflect (object obj);

        public virtual bool TryConvertFromRepresentation (IRepresentedType representedType, object[] representations, out object represented);
    }
    [JsonObject]
    public sealed class Size : IRepresentationObject
    {
        public double Height {
            get;
        }

        public double Width {
            get;
        }

        [JsonConstructor]
        public Size (double width, double height);
    }
    [JsonObject]
    public sealed class VerbatimHtml : IRepresentationObject
    {
        [JsonConstructor]
        public VerbatimHtml (string content);

        public VerbatimHtml (StringBuilder builder);

        public override string ToString ();
    }
}
namespace Xamarin.Interactive.Representations.Reflection
{
    public interface IRepresentedMemberInfo
    {
        bool CanWrite {
            get;
        }

        IRepresentedType DeclaringType {
            get;
        }

        RepresentedMemberKind MemberKind {
            get;
        }

        IRepresentedType MemberType {
            get;
        }

        string Name {
            get;
        }
    }
    public interface IRepresentedType
    {
        IRepresentedType BaseType {
            get;
        }

        string Name {
            get;
        }

        Type ResolvedType {
            get;
        }
    }
    [Serializable]
    public enum RepresentedMemberKind : byte
    {
        None,
        Field,
        Property
    }
}
namespace Xamarin.Interactive.Serialization
{
    [Flags]
    public enum PropertyOptions
    {
        None = 0,
        SerializeIfNull = 1,
        SerializeIfEmpty = 2,
        Default = 2
    }
}
