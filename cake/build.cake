#addin "Cake.Xamarin"
#addin "Cake.FileHelpers"
#addin "Cake.AndroidAppManifest"
#addin "Cake.Plist"
#addin "Cake.Git"

#load "scripts/utils.cake"

var target = Argument("target", "Default");

// ------- Global Tasks ------------

Task("Default")
    .IsDependentOn("Build")
    .IsDependentOn("Package");

Task("Build")
    .IsDependentOn("Clear")
    .IsDependentOn("RestorePackages")
    .IsDependentOn("UpdateAssemblyInfo")
    .IsDependentOn("BuildAndroid");
    //.IsDependentOn("BuildiOS");

Task("Package")
    //.IsDependentOn("Package-iOS") 
    .IsDependentOn("Package-Android");

/*Task("PublishToAppCenter")
    .IsDependentOn("Publish-Android")
    .IsDependentOn("Publish-iOS");*/

// ------- end Global Tasks ------------

// ------- Build Params ------------

var buildConfiguration = Argument("configuration","Release");
var env = Argument("env", "");
var bundle = Argument<string>("bundle", null);
var distribution = Argument("distribution", "");
var iOSbuildConfiguration = "Ad-Hoc";

if (string.IsNullOrWhiteSpace(bundle))
{
    bundle = null;
}

// ------- End Build Params -----------

// -------- Project constants ------

var pathRoot = "../";
var projectCommonPartName = "XamarinCIEx";
var assemblyInfoFile = File(Combine(pathRoot, "CommonAssemblyInfo.cs"));
var solutionFile = File(Combine(pathRoot, projectCommonPartName + ".sln"));
var nameFolderDroid	= projectCommonPartName + ".Android";
var nameFolderIOS = projectCommonPartName + ".iOS";
var pathProjDroid = Combine(pathRoot, nameFolderDroid);
var pathProjiOS	= Combine(pathRoot, nameFolderIOS);
var plistFile = File(Combine(pathProjiOS, "Info.plist"));
var entitlementsFile = File(Combine(pathProjiOS, "Entitlements.plist"));
var projectFileDroid = File(Combine(pathProjDroid, nameFolderDroid + ".csproj"));
var projectFileIOS = File(Combine(pathProjiOS, nameFolderIOS + ".csproj"));
var manifestFile = File(Combine(pathProjDroid, "Properties", "AndroidManifest.xml"));

// -------- End Project constants ------

// NOTE: Should MSBuild treat any errors as warnings.
var treatWarningsAsErrors = "false";

// -------- App Metadata ------------

var versionLabel = Argument<string>("version_label", "develop");
var versionCode = int.Parse(EnvironmentVariable("BUILD_NUMBER") ?? "1");

Information("Version: " + versionLabel);

// -------- End App Metadata ------------

/// =============================
/// Main Tasks
/// =============================

Task("Clear")
    .Does(() =>
    {
        CleanDirectories(Combine(pathRoot, "artifacts", "**"));
        
        MSBuild(solutionFile, configurator => {
            configurator.SetConfiguration(buildConfiguration)
                .WithTarget("Clean");
        });

        MSBuild(solutionFile, configurator => {
            var config = configurator.SetConfiguration(iOSbuildConfiguration);
            if (IsRunningOnWindows())
            {
                var serverAddress = EnvVariable("MAC_SERVER_ADDRESS");
                var serverUser = EnvVariable("MAC_SERVER_USER");
                var serverPassword = EnvVariable("MAC_SERVER_PASSWORD");
                
                Information("MAC_SERVER_ADDRESS: " + serverAddress);
                Information("MAC_SERVER_USER: " + serverUser);
                
                config.WithProperty("ServerAddress", serverAddress)
                    .WithProperty("ServerUser", serverUser)
                    .WithProperty("ServerPassword", serverPassword);
            }
            config.WithTarget("Clean")
                .WithProperty("Platform", "iPhone");
        });
    });

Task("RestorePackages")
    .Does(() =>
    {
        NuGetRestore(solutionFile);
    });

Task("UpdateAssemblyInfo")
    .Does(() =>
    {
        CreateAssemblyInfo(assemblyInfoFile, new AssemblyInfoSettings() {
            Version = versionLabel,
            FileVersion = versionLabel
        });
    });

Task("UpdateAndroidManifest")
    .Does(() =>
    {
        var manifest = DeserializeAppManifest(manifestFile);
        manifest.VersionName = versionLabel;
        manifest.VersionCode = versionCode;
        manifest.PackageName = bundle ?? manifest.PackageName;
        SerializeAppManifest(manifestFile, manifest);
    });

Task("UpdateApplePlist")
    .IsDependentOn("UpdateEntitlements")
    .Does(() =>
    {
        dynamic plist = DeserializePlist(plistFile);
        plist["CFBundleShortVersionString"] = versionLabel;
        plist["CFBundleVersion"] = versionCode.ToString();
		plist["CFBundleIdentifier"] = bundle ?? plist["CFBundleIdentifier"];
        SerializePlist(plistFile, plist);
    });

Task("UpdateEntitlements")
    .Does(() =>
    {
        dynamic entitlements = DeserializePlist(entitlementsFile);
        entitlements["aps-environment"] = "production";
        SerializePlist(entitlementsFile, entitlements);
    });

Task("BuildAndroid")
    .IsDependentOn("UpdateAndroidManifest")
    .Does(() =>
    {
        var keyStore = EnvVariable("ANDROID_KEYSTORE");
        var keyStorePassword = EnvVariable("ANDROID_KEYSTORE_PASSWORD");
        var keyStoreAlias = EnvVariable("ANDROID_KEYSTORE_ALIAS");
        var keyStoreAliasPassword = EnvVariable("ANDROID_KEYSTORE_ALIAS_PASSWORD");
        
        Information("ANDROID_KEYSTORE: " + keyStore);
        Information("ANDROID_KEYSTORE_ALIAS: " + keyStoreAlias);
        
        MSBuild(projectFileDroid, settings =>
		{
            settings.SetConfiguration(buildConfiguration)
                .WithTarget("SignAndroidPackage")
                .WithProperty("AndroidKeyStore", "true")
                .WithProperty("AndroidSigningKeyStore", keyStore)
                .WithProperty("AndroidSigningStorePass", keyStorePassword)
                .WithProperty("AndroidSigningKeyAlias", keyStoreAlias)
                .WithProperty("AndroidSigningKeyPass", keyStoreAliasPassword)
                .WithProperty("DebugSymbols", "false")
                .WithProperty("OutputPath", "bin/Release/")
                .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors)
                .WithProperty("Env", env)
                .WithProperty("Distribution", distribution)
                .WithProperty("Bundle", bundle);
		});
    });

Task("BuildiOS")
    .IsDependentOn("UpdateApplePlist")
    .Does(() =>
    {
        MSBuild(projectFileIOS, settings =>
        {
            // NOTE: Fixed issue with building iOS application
            settings.ToolVersion = MSBuildToolVersion.VS2017;
            // NOTE: x86
            settings.MSBuildPlatform = (Cake.Common.Tools.MSBuild.MSBuildPlatform)1;

            var config = settings.SetConfiguration(iOSbuildConfiguration)
                                 .SetVerbosity(Verbosity.Verbose);
            if (IsRunningOnWindows())
            {
                var serverAddress = EnvVariable("MAC_SERVER_ADDRESS");
                var serverUser = EnvVariable("MAC_SERVER_USER");
                var serverPassword = EnvironmentVariable("MAC_SERVER_PASSWORD");
                
                Information("MAC_SERVER_ADDRESS: " + serverAddress);
                Information("MAC_SERVER_USER: " + serverUser);
                
                config.WithProperty("ServerAddress", serverAddress)
                    .WithProperty("ServerUser", serverUser)
                    .WithProperty("ServerPassword", serverPassword);
            }
            config.WithTarget("Rebuild")
                 .WithProperty("Platform", "iPhone")
                 .WithProperty("DebugSymbols", "false")
                 .WithProperty("BuildIpa", "true")
                 .WithProperty("OutputPath", "bin/iPhone/")
                 .WithProperty("TreatWarningsAsErrors", treatWarningsAsErrors)
                 .WithProperty("Env", env)
                 .WithProperty("Distribution", distribution);
        });
    });

// ==================================
// Artifacts
// ==================================

Task("Package-Android")
    .Does(() =>
    {
        var androidRelease = Combine(pathRoot, "artifacts", "android");
        EnsureDirectoryExists(androidRelease);
        CopyFiles(Combine(pathProjDroid,"bin","Release","*-Signed.apk"), androidRelease);
    });

Task("Package-iOS")
    .Does(() =>
    {
        var appleRelease = Combine(pathRoot, "artifacts", "apple");
        EnsureDirectoryExists(appleRelease);
        CopyFiles(Combine(pathProjiOS,"bin", "iPhone", "*.ipa"), appleRelease);
    });

// ==================================
// Run
// ==================================

RunTarget(target)