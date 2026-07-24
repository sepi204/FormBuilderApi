using YekAbr.Domain.Enums;
using YekAbr.Domain.Models;
using YekAbr.Services.DTOs.Cloud;
using YekAbr.Services.Interfaces.Cloud;

namespace YekAbr.Infrastructure.Services.Cloud;

/// <summary>
/// Recursively enumerates files under a provider folder using the shared list abstraction.
/// </summary>
internal static class CloudProviderFileEnumerator
{
    public static async Task<IReadOnlyList<CloudItem>> ListAllFilesRecursiveAsync(
        ICloudFileProviderClient provider,
        string accessToken,
        string? rootParentId,
        CancellationToken cancellationToken = default)
    {
        var files = new List<CloudItem>();
        var folderQueue = new Queue<string?>();
        folderQueue.Enqueue(rootParentId);

        while (folderQueue.Count > 0)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parentId = folderQueue.Dequeue();
            string? pageToken = null;

            do
            {
                var page = await provider.ListItemsAsync(
                    accessToken,
                    new ListCloudItemsRequest
                    {
                        ParentId = parentId,
                        PageSize = 100,
                        PageToken = pageToken,
                        IncludeFiles = true,
                        IncludeFolders = true
                    },
                    cancellationToken);

                foreach (var item in page.Items)
                {
                    if (item.ItemType == CloudItemType.Folder)
                    {
                        folderQueue.Enqueue(item.Id);
                    }
                    else if (item.ItemType == CloudItemType.File)
                    {
                        files.Add(item);
                    }
                }

                pageToken = page.NextPageToken;
            }
            while (!string.IsNullOrWhiteSpace(pageToken));
        }

        return files;
    }
}
