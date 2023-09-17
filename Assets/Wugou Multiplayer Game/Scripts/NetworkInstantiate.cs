using Mirror;
using Wugou;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class NetworkInstantiate : NetworkBehaviour
{
    [SyncVar(hook = nameof(Instantiate))]
    public string assetName;

    private void Instantiate(string oldAsset, string newAsset)
    {
        InstantiateInternal(newAsset);
    }

    private async void InstantiateInternal(string assetName)
    {
        var loader = await AssetBundleAssetLoader.GetOrCreate(GamePlay.dynamicResourcePath);
        var soilderPrefab = await loader.LoadAssetAsync<GameObject>(assetName);
        var visualObject = Instantiate<GameObject>(soilderPrefab, Vector3.zero, Quaternion.identity, transform);
        visualObject.transform.localPosition = Vector3.zero;
        visualObject.transform.localRotation = Quaternion.identity;
    }
}
