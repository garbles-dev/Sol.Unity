#module nuget:?package=Cake.DotNetTool.Module&version=0.4.0
#addin nuget:?package=Cake.Coverlet&version=2.5.4
#tool dotnet:?package=dotnet-reportgenerator-globaltool&version=4.8.7

var testProjectsRelativePaths = new string[]
{
    "./test/Solana.Unity.Rpc.Test/Solana.Unity.Rpc.Test.csproj",
    "./test/Solana.Unity.Wallet.Test/Solana.Unity.Wallet.Test.csproj",
    "./test/Solana.Unity.KeyStore.Test/Solana.Unity.KeyStore.Test.csproj",
    "./test/Solana.Unity.Programs.Test/Solana.Unity.Programs.Test.csproj",
    "./test/Solana.Unity.Extensions.Test/Solana.Unity.Extensions.Test.csproj"
};

var target = Argument("target", "Pack");
var configuration = Argument("configuration", "Debug");
var configurationRelease = Argument("configuration", "Release");
var solutionFolder = "./";
var artifactsDir = MakeAbsolute(Directory("artifacts"));

var reportTypes = "HtmlInline";
var coverageFolder = "./code_coverage";

var coberturaFileName = "results";
var coverageFilePath = Directory(coverageFolder) + File(coberturaFileName + ".info");
var jsonFilePath = Directory(coverageFolder) + File(coberturaFileName + ".json");
var packagesDir = artifactsDir.Combine(Directory("packages"));


Task("Clean")
    .Does(() => {
        CleanDirectory(artifactsDir);
        CleanDirectory(coverageFolder);
    });

Task("Restore")
    .Does(() => {
    DotNetCoreRestore(solutionFolder);
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() => {
        DotNetCoreBuild(solutionFolder, new DotNetCoreBuildSettings
        {
            NoRestore = true,
            Configuration = configuration
        });
    });

Task("BuildRelease")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() => {
        DotNetCoreBuild(solutionFolder, new DotNetCoreBuildSettings
        {
            NoRestore = true,
            Configuration = configurationRelease
        });
    });
    
Task("Test")
    .IsDependentOn("Build")
    .Does(() => {
    
        var coverletSettings = new CoverletSettings {
            CollectCoverage = true,
            CoverletOutputDirectory = coverageFolder,
            CoverletOutputName = coberturaFileName
        };

        var testSettings = new DotNetCoreTestSettings
        {
            NoRestore = true,
            Configuration = configuration,
            NoBuild = true,
            ArgumentCustomization = args => args.Append($"--logger trx")
        };

        DotNetCoreTest(testProjectsRelativePaths[0], testSettings, coverletSettings);

        coverletSettings.MergeWithFile = jsonFilePath;
        for (int i = 1; i < testProjectsRelativePaths.Length; i++)
        {
            if (i == testProjectsRelativePaths.Length - 1)
            {
                coverletSettings.CoverletOutputFormat  = CoverletOutputFormat.lcov;
            }
            DotNetCoreTest(testProjectsRelativePaths[i], testSettings, coverletSettings);
        }
    });


Task("Report")
    .IsDependentOn("Test")
    .Does(() =>
{
    var reportSettings = new ReportGeneratorSettings
    {
        ArgumentCustomization = args => args.Append($"-reportTypes:{reportTypes}")
    };
    ReportGenerator(coverageFilePath, Directory(coverageFolder), reportSettings);
});


Task("Publish")
    .IsDependentOn("Report")
    .IsDependentOn("BuildRelease")
    .Does(() => {
        DotNetCorePublish(solutionFolder, new DotNetCorePublishSettings
        {
            NoRestore = true,
            Configuration = configurationRelease,
            NoBuild = true,
            OutputDirectory = artifactsDir
        });
    });

Task("Pack")
    .IsDependentOn("Publish")
    .Does(() =>
    {
        var settings = new DotNetCorePackSettings
        {
            Configuration = configurationRelease,
            NoBuild = true,
            NoRestore = true,
            OutputDirectory = packagesDir,
        };


        GetFiles("./src/*/*.csproj")
            .ToList()
            .ForEach(f => DotNetCorePack(f.FullPath, settings));
    });

RunTarget(target);