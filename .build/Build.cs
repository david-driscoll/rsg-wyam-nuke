using System;
using System.Linq;
using Nuke.Common;
using Nuke.Common.Execution;
using Nuke.Common.ProjectModel;
using Nuke.Common.Tooling;
using Nuke.Common.Utilities.Collections;
using static Nuke.Common.EnvironmentInfo;
using static Nuke.Common.IO.FileSystemTasks;
using static Nuke.Common.IO.PathConstruction;
using Wyam.Core;
using Wyam.Core.Execution;
using Wyam.Configuration;
using Wyam.Common.Tracing;
using System.Diagnostics;
using Wyam.Configuration.Preprocessing;
using Wyam.Common.Configuration;
using Wyam.Common.Execution;
using Wyam.Common.Documents;
using Wyam.Common.IO;
using Wyam.Common.Shortcodes;
using Wyam.Docs;
using Wyam.Common.Meta;
using Wyam.Core.Modules.IO;
using Wyam.Yaml;
using Wyam.Core.Modules.Control;
using Wyam.Core.Modules.Metadata;
using Wyam.Razor;

[UnsetVisualStudioEnvironmentVariables]
class Build : NukeBuild
{
    /// Support plugins are available for:
    ///   - JetBrains ReSharper        https://nuke.build/resharper
    ///   - JetBrains Rider            https://nuke.build/rider
    ///   - Microsoft VisualStudio     https://nuke.build/visualstudio
    ///   - Microsoft VSCode           https://nuke.build/vscode

    public static int Main() => Execute<Build>(x => x.Compile);

    [Parameter("Configuration to build - Default is 'Debug' (local) or 'Release' (server)")]
    readonly Configuration Configuration = IsLocalBuild ? Configuration.Debug : Configuration.Release;

    Target Clean => _ => _
        .Before(Restore)
        .Executes(() =>
        {
        });

    Target Restore => _ => _
        .Executes(() =>
        {
        });

    Target Compile => _ => _
        .DependsOn(Restore)
        .Executes(() =>
        {
            Wyam.Common.Tracing.Trace.AddListener(new NukeTraceListener());
            var engine = new Engine();
            // engine.
            var configurator = new Configurator(engine);
            configurator.Recipe = new Wyam.Docs.Docs();
            configurator.Theme = "Samson";
            configurator.Configure("");
            new Configuration2(engine);
            engine.Execute();
        });

}

class Configuration2 : ConfigurationEngineBase
{
    public Configuration2(Engine engine) : base(engine)
    {
        Settings[DocsKeys.Title] = "Rocket Surgeons Guild";
        Settings[Keys.Host] = "rocketsurgeonsguild.github.io/";
        Settings[Keys.LinksUseHttps] = true;
        Settings[DocsKeys.SourceFiles] = "../release/repo/src/**/{!bin,!obj,!packages,!*.Tests,}/**/*.cs";
        Settings[DocsKeys.IncludeDateInPostPath] = true;
        Settings[DocsKeys.BaseEditUrl] = "https://github.com/RocketSurgeonsGuild/rocketsurgeonsguild.github.io/blob/dev/input/";

        Pipelines.InsertBefore(Docs.Code, "Package",
             new ReadFiles("../pkgs/*.yml"),
            new Yaml()
        );

        Pipelines.InsertAfter("Package", "PackageCategories",
            new GroupByMany((ctx, _) => ctx.List<string>("Categories"),
                new Documents("Package")
            ),
            new Meta(Keys.WritePath, new FilePath("pkgs/" + Settings.String(Keys.GroupKey).ToLower().Replace(" ", "-") + "/index.html")),
            new Meta(Keys.RelativeFilePath, (ctx, _) => ctx.FilePath(Keys.WritePath)),
            new OrderBy((ctx, _) => ctx.String(Keys.GroupKey))
        );
        Pipelines.Add("RenderPackage",
            new Documents("PackageCategories"),
            new Razor()
                .WithLayout("/_PackageLayout.cshtml"),
            new WriteFiles()
        );
    }
}

class ConfigurationEngineBase : IEngine
{
    private readonly Engine engine;

    public ConfigurationEngineBase(Engine engine)
    {
        this.engine = engine;
    }

    public IFileSystem FileSystem => engine.FileSystem;

    public ISettings Settings => engine.Settings;

    public IPipelineCollection Pipelines => engine.Pipelines;

    public IShortcodeCollection Shortcodes => engine.Shortcodes;

    public IDocumentCollection Documents => engine.Documents;

    public INamespacesCollection Namespaces => engine.Namespaces;

    public IRawAssemblyCollection DynamicAssemblies => engine.DynamicAssemblies;

    public string ApplicationInput { get => engine.ApplicationInput; set => engine.ApplicationInput = value; }
    public IDocumentFactory DocumentFactory { get => engine.DocumentFactory; set => engine.DocumentFactory = value; }
}

class NukeTraceListener : TextWriterTraceListener
{
    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string message)
    {
        TraceEvent(eventCache, source, eventType, id, "{0}", message);
    }

    public override void TraceEvent(TraceEventCache eventCache, string source, TraceEventType eventType, int id, string format, params object[] args)
    {
        switch (eventType)
        {
            case TraceEventType.Critical:
            case TraceEventType.Error:
                Logger.Error(format, args);
                break;
            case TraceEventType.Information:
                Logger.Info(format, args);
                break;
            case TraceEventType.Verbose:
                Logger.Trace(format, args);
                break;
            case TraceEventType.Warning:
                Logger.Warn(format, args);
                break;
            case TraceEventType.Resume:
            case TraceEventType.Start:
            case TraceEventType.Stop:
            case TraceEventType.Suspend:
            case TraceEventType.Transfer:
                Logger.Trace(format, args);
                break;
        }
    }
}
