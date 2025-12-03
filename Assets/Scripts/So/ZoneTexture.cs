using UnityEngine;

namespace So
{
    [CreateAssetMenu(fileName = "ZoneTextureData", menuName = "ScriptableObjects/ZoneTextureData", order = 1)]
    public class ZoneTexture : ScriptableObject
    {
        public Texture2D floorMain;
        public Texture2D floorDetail;
        public Texture2D wallMain;
        public Texture2D wallDetail;
        [ColorUsage(false, true)]
        public Color ambientColor;
        public Color fogColor;
    }
}