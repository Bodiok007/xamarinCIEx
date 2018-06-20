#addin Cake.HockeyApp

var pathArtifacts = "artifacts";
var canBeDonloaded = Argument("publish", false);

var version = Argument<string>("build_version");

Task("Publish-Android-HockeyApp")
    .Does(() =>
    {
        var apiTokenAndroid = EnvironmentVariable("HOCKEYAPP_API_TOKEN_ANDROID");
        if (string.IsNullOrEmpty(apiTokenAndroid))
        {
            throw new Exception("The HOCKEYAPP_API_TOKEN_ANDROID environment variable is not defined.");
        }

        var appIdFromHockeyAppAndroid = EnvironmentVariable("HOCKEYAPP_APP_ID_ANDROID");
        if (string.IsNullOrEmpty(appIdFromHockeyAppAndroid))
        {
            throw new Exception("The HOCKEYAPP_APP_ID_ANDROID environment variable is not defined.");
        }

        var distributionGroupId = EnvironmentVariable("HOCKEYAPP_ID_DISTRIBUTION_GROUP");
        if (string.IsNullOrEmpty(appIdFromHockeyAppAndroid))
        {
            throw new Exception("The HOCKEYAPP_ID_DISTRIBUTION_GROUP environment variable is not defined.");
        }

        var hockeyAppSettingsAndroid = new HockeyAppUploadSettings 
        {
            ApiToken = apiTokenAndroid,
            AppId = appIdFromHockeyAppAndroid,
            ShortVersion = version,
            Version = version,
            Teams = new[] { Int32.Parse(distributionGroupId) }
        };

        if (canBeDonloaded)
        {
            hockeyAppSettingsAndroid.Status = DownloadStatus.Allowed;
        }

        var fileApk = GetFiles(pathArtifacts + "/android/*-Signed.apk").First();
        Information("Apk: " + fileApk);

        UploadToHockeyApp(fileApk, null, hockeyAppSettingsAndroid);
    });

Task("Publish-iOS-HockeyApp")
    .Does(() =>
    {
        var apiTokeniOS = EnvironmentVariable("HOCKEYAPP_API_TOKEN_IOS");
        if (string.IsNullOrEmpty(apiTokeniOS))
        {
            throw new Exception("The HOCKEYAPP_API_TOKEN_IOS environment variable is not defined.");
        }

        var appIdFromHockeyAppiOS = EnvironmentVariable("HOCKEYAPP_APP_ID_IOS");
        if (string.IsNullOrEmpty(appIdFromHockeyAppiOS))
        {
            throw new Exception("The HOCKEYAPP_APP_ID_IOS environment variable is not defined.");
        }

        var distributionGroupId = EnvironmentVariable("HOCKEYAPP_ID_DISTRIBUTION_GROUP");
        if (string.IsNullOrEmpty(appIdFromHockeyAppAndroid))
        {
            throw new Exception("The HOCKEYAPP_ID_DISTRIBUTION_GROUP environment variable is not defined.");
        }

        var hockeyAppSettingsiOS = new HockeyAppUploadSettings 
        {
            ApiToken = apiTokeniOS,
            AppId = appIdFromHockeyAppiOS,
            ShortVersion = version,
            Version = version,
            Teams = new[] { Int32.Parse(distributionGroupId) }
        };

        if (canBeDonloaded)
        {
            hockeyAppSettingsiOS.Status = DownloadStatus.Allowed;
        }

        var fileIpa = GetFiles(pathArtifacts + "/apple/*.ipa").First();        
        Information("Ipa: " + fileIpa);

        UploadToHockeyApp(fileIpa, hockeyAppSettingsiOS);
    });