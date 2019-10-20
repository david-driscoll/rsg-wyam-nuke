using Nuke.Common;
using Wyam.Core.Execution;
using Wyam.Common.Execution;
using Wyam.Common.IO;
using Wyam.Docs;
using Wyam.Common.Meta;
using Wyam.Core.Modules.IO;
using Wyam.Yaml;
using Wyam.Core.Modules.Control;
using Wyam.Core.Modules.Metadata;
using Wyam.Razor;
using Wyam.Configuration;
using Nuke.Common.IO;
using static Nuke.Common.IO.PathConstruction;
using System.Linq;
using Wyam.Html;
using Wyam.Web;
using Wyam.Feeds;
using Wyam.CodeAnalysis;

class WyamConfiguration : ConfigurationEngineBase
{
    public WyamConfiguration(Engine engine, Build build) : base(engine)
    {
        var configurator = new Configurator(engine);
        configurator.Recipe = new Wyam.Docs.Docs();
        configurator.Theme = "Samson";
        configurator.Configure("");
        configurator.AssemblyLoader.DirectAssemblies.Add(typeof(HtmlKeys).Assembly);
        configurator.AssemblyLoader.DirectAssemblies.Add(typeof(WebKeys).Assembly);
        configurator.AssemblyLoader.DirectAssemblies.Add(typeof(FeedKeys).Assembly);
        configurator.AssemblyLoader.DirectAssemblies.Add(typeof(CodeAnalysisKeys).Assembly);

        var assemblyFiles = build.PackageSpecs
            .SelectMany(x => x.Assemblies)
            .SelectMany(x => GlobFiles(NukeBuild.TemporaryDirectory / "_packages", x.TrimStart('/', '\\')))
            .Distinct()
            .Select(x => GetRelativePath(NukeBuild.RootDirectory / "input", x));
        Logger.Info(string.Join(", ", assemblyFiles));

        Settings["AssemblyFiles"] = assemblyFiles;

        Settings[DocsKeys.Title] = "Rocket Surgeons Guild";
        Settings[Keys.Host] = "rocketsurgeonsguild.github.io/";
        Settings[Keys.LinksUseHttps] = true;
        Settings[DocsKeys.SourceFiles] = "../release/repo/src/**/{!bin,!obj,!packages,!*.Tests,}/**/*.cs";
        Settings[DocsKeys.IncludeDateInPostPath] = true;
        Settings[DocsKeys.BaseEditUrl] = "https://github.com/RocketSurgeonsGuild/rocketsurgeonsguild.github.io/blob/dev/input/";

        Pipelines.InsertBefore(Docs.Code, "Package",
            new ReadFiles(NukeBuild.RootDirectory.ToString() + "/packages/*.yml"),
            new Yaml()
        );

        Pipelines.InsertAfter("Package", "PackageCategories",
            new GroupByMany((doc, _) => doc.List<string>("Categories"),
                new Documents("Package")
            ),
            new Meta(Keys.WritePath, (doc, _) => new FilePath("packages/" + doc.String(Keys.GroupKey).ToLower().Replace(" ", "-") + "/index.html")),
            new Meta(Keys.RelativeFilePath, (ctx, _) => ctx.FilePath(Keys.WritePath)),
            new OrderBy((ctx, _) => ctx.String(Keys.GroupKey))
        );

        Pipelines.Add("RenderPackage",
            new Documents("PackageCategories"),
            new Razor().WithLayout("/_PackageLayout.cshtml"),
            new WriteFiles()
        );
    }
}
