using UnityEngine;

namespace Poi.Tools
{
    public class PoiExportPackageObject : ScriptableObject
    {
        public string title;
        public Object[] filesToRemove;
        public Object[] filesToAdd;
    }
}