using System.Text.Json.Serialization;

namespace CreatorCompanionPatcher.Manager.Models;

public record GithubRelease(
    string Url,
    [property: JsonPropertyName("assets_url")]
    string AssetsUrl,
    [property: JsonPropertyName("upload_url")]
    string UploadUrl,
    [property: JsonPropertyName("html_url")]
    string HtmlUrl,
    int Id,
    [property: JsonPropertyName("node_id")]
    string NodeId,
    [property: JsonPropertyName("tag_name")]
    string TagName,
    [property: JsonPropertyName("target_commitish")]
    string TargetCommitish,
    string Name,
    bool Draft,
    bool Prerelease,
    [property: JsonPropertyName("created_at")]
    string CreatedAt,
    [property: JsonPropertyName("published_at")]
    string PublishedAt,
    GithubReleaseAssets[] Assets,
    [property: JsonPropertyName("tarball_url")]
    string TarballUrl,
    [property: JsonPropertyName("zipball_url")]
    string ZipballUrl,
    string Body
);

public record GithubReleaseAssets(
    string Url,
    int Id,
    [property: JsonPropertyName("node_id")]
    string NodeId,
    string Name,
    object Label,
    [property: JsonPropertyName("content_type")]
    string ContentType,
    string State,
    int Size,
    [property: JsonPropertyName("download_count")]
    int DownloadCount,
    [property: JsonPropertyName("created_at")]
    string CreatedAt,
    [property: JsonPropertyName("updated_at")]
    string UpdatedAt,
    [property: JsonPropertyName("browser_download_url")]
    string BrowserDownloadUrl
);