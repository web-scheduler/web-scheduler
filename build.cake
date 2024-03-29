var target = Argument("Target", "Default");
var configuration =
    HasArgument("Configuration") ? Argument<string>("Configuration") :
    EnvironmentVariable("Configuration", "Release");
var platform =
    HasArgument("Platform") ? Argument<string>("Platform") :
    EnvironmentVariable("Platform", "linux/amd64");
var push =
    HasArgument("Push") ? Argument<bool>("Push") :
    EnvironmentVariable("Push", false);
var version = GetVersion();
System.IO.File.WriteAllText(System.IO.Path.Join(ArtifactsDirectory, "DOCKER_TAG"), $"{version}");



var ArtifactsDirectory = Directory("./Artifacts");

Task("Clean")
    .Description("Cleans the Artifacts, bin and obj directories.")
    .Does(() =>
    {
        CleanDirectory(ArtifactsDirectory);
        DeleteDirectories(GetDirectories("**/bin"), new DeleteDirectorySettings() { Force = true, Recursive = true });
        DeleteDirectories(GetDirectories("**/obj"), new DeleteDirectorySettings() { Force = true, Recursive = true });
    });

Task("Restore")
    .Description("Restores NuGet packages.")
    .IsDependentOn("Clean")
    .Does(() =>
    {
        DotNetRestore();
    });

Task("Build")
    .Description("Builds the solution.")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        DotNetBuild(
            ".",
            new DotNetBuildSettings()
            {
                Configuration = configuration,
                NoRestore = true,
            });
    });

Task("Test")
    .Description("Runs unit tests and outputs test results to the Artifacts directory.")
    .IsDependentOn("Build")
    .DoesForEach(GetFiles("./Tests/**/*.csproj"), project =>
    {
        DotNetTest(
            project.ToString(),
            new DotNetTestSettings()
            {
                Blame = true,
                Collectors = new string[] { "XPlat Code Coverage" },
                Configuration = configuration,
                Loggers = new string[]
                {
                    $"trx;LogFileName={project.GetFilenameWithoutExtension()}.trx",
                    $"html;LogFileName={project.GetFilenameWithoutExtension()}.html",
                },
                NoBuild = true,
                NoRestore = true,
                ResultsDirectory = ArtifactsDirectory,
            });
    });



Task("Pack")
    .Description("Creates NuGet packages and outputs them to the artifacts directory.")
    .IsDependentOn("Publish")
    .DoesForEach(GetFiles("./Source/**/*.csproj"), project =>
    {
        DotNetPack(
            project.ToString(),
            new DotNetPackSettings()
            {

                Configuration = configuration,
                IncludeSymbols = true,
                MSBuildSettings = new DotNetMSBuildSettings()
                {
                    ContinuousIntegrationBuild = !BuildSystem.IsLocalBuild,
                },
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = ArtifactsDirectory + Directory("Pack"),
            });
    });


Task("Publish")
    .Description("Publishes the solution.")
    .IsDependentOn("Test")
    .DoesForEach(GetFiles("./Source/**/*.csproj"), project =>
    {
        DotNetPublish(
            project.ToString(),
            new DotNetPublishSettings()
            {

                Configuration = configuration,
                       MSBuildSettings = new DotNetMSBuildSettings()
                {
                    ContinuousIntegrationBuild = !BuildSystem.IsLocalBuild,
                },
                NoBuild = true,
                NoRestore = true,
                OutputDirectory = ArtifactsDirectory + Directory("Publish"),
            });
    });

Task("DockerBuild")
    .Description("Builds a Docker image.")
    .DoesForEach(GetFiles("./**/Dockerfile"), dockerfile =>
    {
        if(dockerfile.GetDirectory().GetDirectoryName().ToLower() == ".devcontainer") {
            return;
        }

        var tag = $"{Environment.GetEnvironmentVariable("DOCKER_REGISTRY")}/{Environment.GetEnvironmentVariable("DOCKER_REPOSITORY_NAME")}/{dockerfile.GetDirectory().GetDirectoryName().ToLower().Replace(".", "-")}";
        Console.WriteLine($"Building for '{tag}:{version}'");
        var gitCommitSha = GetGitCommitSha();

        // Docker buildx allows you to build Docker images for multiple platforms (including x64, x86 and ARM64) and
        // push them at the same time. To enable buildx, you may need to enable experimental support with these commands:
        // docker buildx create --name builder --driver docker-container --use
        // docker buildx inspect --bootstrap
        // To stop using buildx remove the buildx parameter and the --platform, --progress switches.
        // See https://github.com/docker/buildx

        var exitCode = StartProcess(
            "docker",
            new ProcessArgumentBuilder()
                .Append("buildx")
                .Append("build")
                .AppendSwitchQuoted("--platform", platform)
                .AppendSwitchQuoted("--progress", BuildSystem.IsLocalBuild ? "auto" : "plain")
                .Append($"--push={push}")
                .AppendSwitchQuoted("--tag", $"{tag}:{version}")
                .AppendSwitchQuoted("--build-arg", $"Configuration={configuration}")
                .AppendSwitchQuoted("--label", $"org.opencontainers.image.created={DateTimeOffset.UtcNow:o}")
                .AppendSwitchQuoted("--label", $"org.opencontainers.image.revision={gitCommitSha}")
                .AppendSwitchQuoted("--label", $"org.opencontainers.image.version={version}")
                .AppendSwitchQuoted("--file", dockerfile.ToString())
                .Append(".")
                .RenderSafe());
        if (exitCode != 0)
        {
            throw new Exception($"Docker build failed with non zero exit code {exitCode}.");
        }

        // If you'd rather not use buildx, then you can uncomment these lines instead.
        // var exitCode = StartProcess(
        //     "docker",
        //     new ProcessArgumentBuilder()
        //         .Append("build")
        //         .AppendSwitchQuoted("--tag", $"{tag}:{version}")
        //         .AppendSwitchQuoted("--build-arg", $"Configuration={configuration}")
        //         .AppendSwitchQuoted("--label", $"org.opencontainers.image.created={DateTimeOffset.UtcNow:o}")
        //         .AppendSwitchQuoted("--label", $"org.opencontainers.image.revision={gitCommitSha}")
        //         .AppendSwitchQuoted("--label", $"org.opencontainers.image.version={version}")
        //         .AppendSwitchQuoted("--file", dockerfile.ToString())
        //         .Append(".")
        //         .RenderSafe());
        // if (exitCode != 0)
        // {
        //     throw new Exception($"Docker build failed with non zero exit code {exitCode}.");
        // }
        //
        // if (push)
        // {
        //     var pushExitCode = StartProcess(
        //         "docker",
        //         new ProcessArgumentBuilder()
        //             .AppendSwitchQuoted("push", $"{tag}:{version}")
        //             .RenderSafe());
        //     if (pushExitCode != 0)
        //     {
        //         throw new Exception($"Docker build failed with non zero exit code {pushExitCode}.");
        //     }
        // }


    });
string GetVersion()
{
    var directoryBuildPropsFilePath = GetFiles("Directory.Build.props").Single().ToString();
    var directoryBuildPropsDocument = System.Xml.Linq.XDocument.Load(directoryBuildPropsFilePath);
    var preReleasePhase = directoryBuildPropsDocument.Descendants("MinVerDefaultPreReleaseIdentifiers").Single().Value;

    var exitCode = StartProcess(
        "dotnet",
        new ProcessSettings()
            .WithArguments(x => x
                .Append("minver"))
                // .AppendSwitch("--default-pre-release-phase", preReleasePhase)
            .SetRedirectStandardOutput(true),
            out var versionLines);

    if (exitCode != 0)
    {
        throw new Exception($"dotnet minver failed with non zero exit code {exitCode}.");
    }

    return versionLines.LastOrDefault();
}

string GetGitCommitSha()
{
    var exitCode = StartProcess(
        "git",
        new ProcessSettings()
            .WithArguments(x => x.Append("rev-parse HEAD"))
            .SetRedirectStandardOutput(true),
        out var shaLines);

    if (exitCode != 0)
    {
        throw new Exception($"git rev-parse failed with non zero exit code {exitCode}.");
    }
    return shaLines.LastOrDefault();
}
Task("Default")
    .Description("Cleans, restores NuGet packages, builds the solution, runs unit tests and then builds a Docker image, then publishes packages.")
    .IsDependentOn("Build")
    .IsDependentOn("Test")
    .IsDependentOn("Pack")
    .IsDependentOn("Publish")
    .IsDependentOn("DockerBuild");


RunTarget(target);
