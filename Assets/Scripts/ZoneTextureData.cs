using UnityEngine;

[CreateAssetMenu(fileName = "ZoneTextureData", menuName = "ScriptableObjects/ZoneTextureData", order = 1)]
public class ZoneTextureData : ScriptableObject
{
    public Texture2D floorMain;
    public Texture2D floorDetail;
    public Texture2D wallMain;
    public Texture2D wallDetail;
    [ColorUsage(false, true)]
    public Color ambientColor;
    public Color fogColor;
}