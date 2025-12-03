using System.Reflection;
using UnityEngine;

namespace SceneLabel
{
    /// EDITOR TOOL
    /// Member attribute. May be added to any variable to have it displayed by text in the scene.
    [System.AttributeUsage(System.AttributeTargets.Field)]
    public class SceneLabelAttribute : PropertyAttribute
    {
        /// Bare value of the object associated with the field
        public object Value;

        /// Decorated value
        /// All format changing tags are here except colors
        public object FormatValue;
    
        /// Decorated and colored value
        public object RichValue;
    
        /// Associated Game Object
        public GameObject GameObject;
    
        /// Mono
        public MonoBehaviour Mono;
    
        /// Field Info
        public FieldInfo FieldInfo;
    
        /// Constant ID for the attribute
        /// Can be used to set special behaviours on a category of attributes
        /// Defaults to SceneLabelID.Default (0), add another enum in SceneLabelID for special behaviour
        public SceneLabelID ID;
    
        /// Text to display before the value
        public string Prefix;
    
        /// Text to display after the value
        public string Suffix;
    
        /// Color of the text
        public Color Color;
    
        /// Font size of the text
        public int FontSize;
    
        /// Font style of the text
        public FontStyle FontStyle;
    
        /// Draw text in a list independent of the object position
        public bool AbsoluteMode;

        /// Draw text on the same line as the previous attribute
        /// Is ignored if no previous attribute is present on the object
        /// Label will inherit the style of the previous one
        public bool SameLine;

        /// Separator to use when on the same line
        public string Separator;

        public SceneLabelAttribute(SceneLabelID id = SceneLabelID.Default, string prefix = "", string suffix = "", float r = 1f, float g = 1f, float b = 1f, int fontSize = 18,
            FontStyle fontStyle = FontStyle.Bold, bool absoluteMode = false, bool sameLine = false, string separator = "; ")
        {
            ID = id;
            Prefix = prefix;
            Suffix = suffix;
            Color = new Color(r, g, b);
            FontSize = fontSize;
            FontStyle = fontStyle;
            AbsoluteMode = absoluteMode;
            SameLine = sameLine;
            Separator = separator;
        }
    
        /// Internal.
        /// Returns a unique ID to be used as a key for SceneLabelOverlay maps.
        public int GetUniqueID(Component owner, FieldInfo field)
        {
            unchecked
            {
                var hash = 17;
                hash = hash * 31 + owner.GetInstanceID();
                hash = hash * 31 + field.Name.GetHashCode();
                hash = hash * 31 + field.DeclaringType.FullName.GetHashCode();
                hash = hash * 31 + Prefix.GetHashCode();
                return hash;
            }
        }
    }
}