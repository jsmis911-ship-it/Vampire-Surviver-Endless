using TMPro;
using UnityEngine;

// Stable asset GUID/local-ID ordering is generated in the Editor, never instance IDs.
public sealed class LanAssetCatalog : ScriptableObject
{
    public Sprite[] sprites;
    public string contentHash;
    public TMP_FontAsset font;
}
