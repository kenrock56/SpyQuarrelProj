using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public static class AssetLoader
{
    public static async Task<T> Get<T>(string assetPath)
    {
        var handle = Addressables.LoadAssetAsync<T>(assetPath);
        await handle.Task;

        if (handle.Status == AsyncOperationStatus.Succeeded)
        {
            T result = handle.Result;
            Addressables.Release(handle);
            return result;
        }

        Debug.LogError($"Failed to load addressable asset: {assetPath}");
        return default;
    }
}