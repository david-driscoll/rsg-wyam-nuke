using Wyam.Core.Execution;
using Wyam.Common.Configuration;
using Wyam.Common.Execution;
using Wyam.Common.Documents;
using Wyam.Common.IO;
using Wyam.Common.Shortcodes;

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
