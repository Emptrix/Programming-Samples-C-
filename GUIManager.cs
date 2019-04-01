using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class RegisteredGUIAssets
{
    [Header("Initialized During Runtime")]
    public GameObject asset;
    [Header ("Customizable")]
    public GUIAssetTag assetTag;
}

public class GUIManager : MonoBehaviour
{
    [Header("Required")]
    [SerializeField]
    private List<RegisteredGUIAssets> registeredGUIAssets = new List<RegisteredGUIAssets>();

    private bool assetsNotInitialized = true;
    private void LoadAssets()
    {
        assetsNotInitialized = false;
        foreach (GUIAsset asset in GetComponentsInChildren<GUIAsset>())
            foreach(RegisteredGUIAssets registeredAsset in registeredGUIAssets)
                if (asset.assetTag == registeredAsset.assetTag)
                {
                    registeredAsset.asset = asset.gameObject;
                    break;
                }
    }

    public GameObject GetGUIAsset(GUIAssetTag guiAssetTag)
    {
        if (assetsNotInitialized)
            LoadAssets();

        foreach (RegisteredGUIAssets guiAsset in registeredGUIAssets)
            if (guiAsset.assetTag == guiAssetTag)
                return guiAsset.asset.gameObject;
        return null;
    }
}
