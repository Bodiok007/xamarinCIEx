#addin Newtonsoft.Json
#addin RestSharp
#addin Cake.Json

#load "utils.cake"

using RestSharp;

public class ReleaseData
{
    public string upload_id { get; set; }
    public string upload_url { get; set; }
}

public class ReleaseUrl
{
    public string release_url { get; set; }
}

public class Release
{
    public string release_url { get;set; }
}

public class Distribute 
{
    public string status { get; set; }
}

void UploadToAppCenter(
    string appName, 
    string apiToken, 
    string userName, 
    string distributionGroup, 
    string pathToFile, 
    string releaseNotes = "")
{
    Information($"Upload {appName} started");

    var client = new RestClient();
    client.BaseUrl = new Uri("https://api.appcenter.ms/");

    // NOTE: Start release upload
    var requestReleaseData = new RestRequest(
        $"/v0.1/apps/{userName}/{appName}/release_uploads",
        Method.POST);

    requestReleaseData.AddHeader("x-api-token", apiToken);
    requestReleaseData.AddHeader("accept", "application/json");
    requestReleaseData.AddHeader("content-type", "application/json");
    
    IRestResponse responseReleaseData = client.Execute(requestReleaseData);
    if (!responseReleaseData.IsSuccessful)
    {
        throw new Exception($"Failed to start uploading process: {responseReleaseData.Content}");
    }

    var releaseData = DeserializeJson<ReleaseData>(responseReleaseData.Content);

    // NOTE: Upload file
    var requestUPLOAD = new RestRequest(
        releaseData.upload_url,
        Method.POST
    );

    // NOTE: ipa - REST parameter for Android and iOS files
    requestUPLOAD.AddFile("ipa", pathToFile);

    IRestResponse responseUPLOAD = client.Execute(requestUPLOAD);
    if (responseUPLOAD.Content != "")
    {
        throw new Exception($"ERROR UPLOAD APP TO MOBILE CENTER: \n {responseUPLOAD.Content}");
    }

    // NOTE: Commit upload
    var requestToCommit = new RestRequest(
        $"/v0.1/apps/{userName}/{appName}/release_uploads/" + releaseData.upload_id,
        Method.PATCH);

    requestToCommit.AddHeader("x-api-token", apiToken);
    requestToCommit.AddHeader("accept", "application/json");
    requestToCommit.AddHeader("content-type", "application/json");
    requestToCommit.AddParameter("application/json", "{ \"status\": \"committed\" }", ParameterType.RequestBody);
    
    IRestResponse responseToCommit = client.Execute(requestToCommit);

    var releaseUrl = DeserializeJson<ReleaseUrl>(responseToCommit.Content);

    // NOTE: Distribute to group
    var requestToDistribute = new RestRequest(
        releaseUrl.release_url,
        Method.PATCH);

    requestToDistribute.AddHeader("x-api-token", apiToken);
    requestToDistribute.AddHeader("accept", "application/json");
    requestToDistribute.AddHeader("content-type", "application/json");
    requestToDistribute.AddParameter(
        "application/json", 
        "{ \"distribution_group_name\": \"" + distributionGroup +"\",\"release_notes\": \""+ releaseNotes +"\"}", 
        ParameterType.RequestBody);

    IRestResponse responseToDistribute = client.Execute(requestToDistribute);

    var resultDistribute = DeserializeJson<Distribute>(responseToDistribute.Content);

    if (!responseToDistribute.IsSuccessful)
    {
        Information($"ERROR MESSAGE: {responseToDistribute.ErrorMessage}");
        throw new Exception($"UPLOAD ERROR. STATUS CODE: {responseToDistribute.StatusCode.ToString()}");
    }

    Information($"Upload {appName} completed");
}

Task("Publish-Android")
    .Does(() =>
{
    var mobileCenterApiToken = EnvVariable("MOBILE_CENTER_API_TOKEN");
    var mobileCenterUsername = EnvVariable("MOBILE_CENTER_USER_NAME");
    var mobileCenterDistributionGroup = EnvVariable("MOBILE_CENTER_DISTRIBUTION_GROUP");
    var mobileCenterAppName = EnvironmentVariable("MOBILE_CENTER_ANDROID_APP_NAME");
   
    var filePath = GetFiles(Combine(pathRoot, "artifacts","android", "*-Signed.apk")).First().FullPath;
    Information("Apk: " + filePath);

    UploadToAppCenter(
        mobileCenterAppName, 
        mobileCenterApiToken, 
        mobileCenterUsername, 
        mobileCenterDistributionGroup, 
        filePath
    );
});

Task("Publish-iOS")
    .Does(() =>
{
    var mobileCenterApiToken = EnvVariable("MOBILE_CENTER_API_TOKEN");
    var mobileCenterUsername = EnvVariable("MOBILE_CENTER_USER_NAME");
    var mobileCenterDistributionGroup = EnvVariable("MOBILE_CENTER_DISTRIBUTION_GROUP");
    var mobileCenterAppName = EnvVariable("MOBILE_CENTER_IOS_APP_NAME");
    
    var filePath = GetFiles(Combine(pathRoot, "artifacts","apple", "*.ipa")).First().FullPath;
    Information("ipa: " + filePath);

    UploadToAppCenter(
        mobileCenterAppName, 
        mobileCenterApiToken, 
        mobileCenterUsername, 
        mobileCenterDistributionGroup, 
        filePath
    );
});