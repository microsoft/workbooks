[assembly: AgentProcessRegistration ("console", typeof(ConsoleAgentProcess))]
[assembly: AgentProcessRegistration ("console-netcore", typeof(DotNetCoreAgentProcess))]
[assembly: AgentProcessRegistration ("webassembly-monowebassembly", typeof(WebAssemblyAgentProcess))]
[assembly: AssemblyConfiguration ("Release")]
[assembly: AssemblyCopyright ("Copyright 2016-2018 Microsoft. All rights reserved.\nCopyright 2014-2016 Xamarin Inc. All rights reserved.")]
[assembly: AssemblyProduct ("Xamarin.Interactive.Client")]
[assembly: AssemblyTitle ("Xamarin.Interactive.Client")]
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
[assembly: InternalsVisibleTo ("Xamarin.Interactive.CodeAnalysis.Tests")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Console")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.DotNetCore")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms.Android")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Forms.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.iOS")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Mac.Desktop")]
[assembly: InternalsVisibleTo ("Xamarin.Interactive.Mac.Mobile")]
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
[assembly: InternalsVisibleTo ("Xamarin.Workbooks.WebAssembly")]
[assembly: InternalsVisibleTo ("xic")]
[assembly: TargetFramework (".NETStandard,Version=v2.0", FrameworkDisplayName = "")]
namespace CommonMark.Formatters
{
    public sealed class MarkdownFormatterSettings
    {
        public static MarkdownFormatterSettings Default {
            get;
        }

        public int? MaxWidth {
            get;
            set;
        }

        public char ThematicBreakChar {
            get;
            set;
        }

        public int? ThematicBreakWidth {
            get;
            set;
        }

        public MarkdownFormatterSettings ();
    }
}
namespace Xamarin.Interactive.Client.CommandLineTool
{
    public sealed class Driver
    {
        public string[] ClientLaunchUris {
            get;
            set;
        }

        public bool Verbose {
            get;
            set;
        }

        public Driver ();

        public void LogErrorVerbose (string message);

        public void LogVerbose (string message);

        public int Run ();
    }
}
namespace Xamarin.Interactive.Client.Updater
{
    public sealed class ApplicationNode
    {
        [XmlAttribute ("id")]
        public string Id {
            get;
            set;
        }

        [XmlAttribute ("name")]
        public string Name {
            get;
            set;
        }

        [XmlElement ("Update")]
        public List<UpdateNode> Updates {
            get;
            set;
        }

        public ApplicationNode ();
    }
    [XmlRoot ("UpdateInfo")]
    public class UpdateManifest
    {
        [XmlElement ("Application")]
        public List<ApplicationNode> Applications {
            get;
            set;
        }

        public UpdateManifest ();

        public static UpdateManifest Deserialize (Stream stream);
    }
    public sealed class UpdateNode : IXmlSerializable
    {
        public DateTime Date {
            get;
            set;
        }

        public string Hash {
            get;
            set;
        }

        public string Id {
            get;
            set;
        }

        public bool Interactive {
            get;
            set;
        }

        public bool IsValid {
            get;
        }

        public string Level {
            get;
            set;
        }

        public string ReleaseNotes {
            get;
            set;
        }

        public bool Restart {
            get;
            set;
        }

        public long Size {
            get;
            set;
        }

        public Uri Url {
            get;
            set;
        }

        public string Version {
            get;
            set;
        }

        public long VersionId {
            get;
            set;
        }

        public UpdateNode ();
    }
}
namespace Xamarin.Interactive.Collections
{
    public class AggregateObservableCollection<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T>, INotifyPropertyChanging, INotifyPropertyChanged, INotifyCollectionChanged
    {
        public event NotifyCollectionChangedEventHandler CollectionChanged {
            add;
            remove;
        }

        public event PropertyChangedEventHandler PropertyChanged {
            add;
            remove;
        }

        public event PropertyChangingEventHandler PropertyChanging {
            add;
            remove;
        }

        public int Count {
            get;
        }

        public AggregateObservableCollection ();

        public T this [int index] {
            get;
        }

        public void AddSource (IReadOnlyList<T> source);

        public IEnumerator<T> GetEnumerator ();
    }
}
namespace Xamarin.Interactive.Core
{
    public class ChangeableWrapper<T> : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged {
            add;
            remove;
        }

        public bool CanWrite {
            get;
        }

        public T Value {
            get;
            set;
        }

        public ChangeableWrapper (T value, bool canWrite = false);

        public void RaisePropertyChanged ();
    }
}
namespace Xamarin.Interactive.Events
{
    public interface IEvent
    {
        object Source {
            get;
        }

        DateTime Timestamp {
            get;
        }
    }
}
namespace Xamarin.Interactive.Rendering
{
    public static class HtmlHelpers
    {
        public static string HtmlEscape (this string str, bool newlineToBr = false);

        public static bool TryHtmlEscape (this char c, out string escaped, bool newlineToBr = false);

        public static void WriteHtmlEscaped (this TextWriter writer, char c, bool newlineToBr = false);

        public static void WriteHtmlEscaped (this TextWriter writer, string str, bool newlineToBr = false);
    }
    [AttributeUsage (AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RendererAttribute : Attribute
    {
        public bool ExactMatchRequired {
            get;
        }

        public Type SourceType {
            get;
        }

        public RendererAttribute (Type sourceType, bool exactMatchRequired = true);
    }
    [Flags]
    public enum RendererRepresentationOptions
    {
        None = 0,
        ForceExpand = 1,
        ExpandedByDefault = 2,
        ExpandedFromMenu = 4,
        SuppressDisplayNameHint = 8
    }
}
namespace Xamarin.Interactive.Session
{
    public sealed class InteractiveSession : IMessageService, IDisposable
    {
        public IObservable<InteractiveSessionEvent> Events {
            get;
        }

        public void Dispose ();

        public Task InitializeAsync (InteractiveSessionDescription sessionDescription, CancellationToken cancellationToken = default(CancellationToken));

        public void NotifySessionDescriptionChanged (InteractiveSessionDescription sessionDescription);

        public void TerminateAgentConnection ();
    }
    public sealed class InteractiveSessionDescription
    {
        public EvaluationEnvironment EvaluationEnvironment {
            get;
        }

        public LanguageDescription LanguageDescription {
            get;
        }

        public string TargetPlatformIdentifier {
            get;
        }

        [JsonConstructor]
        public InteractiveSessionDescription (LanguageDescription languageDescription, string targetPlatformIdentifier, EvaluationEnvironment evaluationEnvironment = default(EvaluationEnvironment));
    }
    public struct InteractiveSessionEvent
    {
        public object Data {
            get;
        }

        public InteractiveSessionEventKind Kind {
            get;
        }

        [JsonConstructor]
        public InteractiveSessionEvent (InteractiveSessionEventKind kind, object data = null);
    }
    public enum InteractiveSessionEventKind
    {
        None,
        ConnectingToAgent,
        InitializingWorkspace,
        Ready,
        AgentFeaturesUpdated,
        AgentDisconnected,
        Evaluation
    }
}
