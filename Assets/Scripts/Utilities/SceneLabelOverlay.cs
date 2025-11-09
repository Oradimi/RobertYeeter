using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// EDITOR TOOL
/// Displays values of all variables that possess the SceneLabel attribute on the SceneView and the GameView.
/// Those values can either be tied to the object position, or be displayed on the top left of the screen.
/// Used to display debug values in GameView
public class SceneLabelOverlay : MonoBehaviour
{
    /// Set of data passed to external decorators
    public class SceneLabelOverlayData
    {
        public bool IsGameView;
        public float SceneViewScale;
        public float GameViewScale;
    }
    
    [SerializeField] private Font interFont;

    public static System.Action<SceneLabelAttribute, SceneLabelOverlayData> OnSetSpecialAttribute;

    private SceneLabelOverlayData _data;

    private Dictionary<int, Label> _labelMap;
    private Dictionary<int, string> _labelTextShadowMap;
    private Dictionary<int, GameObject> _objectMap;
    private Dictionary<GameObject, List<int>> _reverseObjectMap;
    private Dictionary<int, SceneLabelAttribute> _attributeMap;

    private Camera _gameCamera;
    private int _sceneObjectCount;
    
    private float _checkTimer;
    
    private void Awake()
    {
        _gameCamera = Camera.main;
        _data = new SceneLabelOverlayData
        {
            IsGameView = false,
            SceneViewScale = 1f,
            GameViewScale = 1f
        };
        _labelMap = new Dictionary<int, Label>();
        _labelTextShadowMap = new Dictionary<int, string>();
        _objectMap = new Dictionary<int, GameObject>();
        _reverseObjectMap = new Dictionary<GameObject, List<int>>();
        _attributeMap = new Dictionary<int, SceneLabelAttribute>();
        _checkTimer = 5f;
    }
    
    /// Called every frame when GameView is displaying
    private void OnGUI()
    {
        _checkTimer -= Time.deltaTime;
        
        if (_checkTimer <= 0f)
        {
            _checkTimer = 5f;
            OnChangeTimerRefresh();
        }
        
        if (!interFont)
            return;

        _data.IsGameView = true;
        RefreshMaps();
        
        var absoluteModeHeight = 10f;
        var objectModeHeights = new Dictionary<GameObject, float>();

        foreach (var kv in _labelMap)
        {
            _data.GameViewScale = Screen.height / 640f;
            
            var style = new GUIStyle(GUI.skin.label)
            {
                richText = true,
                font = interFont ? interFont : null,
                fontSize = Mathf.FloorToInt(_attributeMap[kv.Key].FontSize * _data.GameViewScale),
                normal = { textColor = _attributeMap[kv.Key].Color },
                hover = { textColor = _attributeMap[kv.Key].Color },
                alignment = kv.Value.style.unityTextAlign.value,
                fontStyle = _attributeMap[kv.Key].FontStyle
            };

            Vector2 position;
            var screenPos = Vector3.forward;
            
            if (_attributeMap[kv.Key].AbsoluteMode)
            {
                var sceneViewPosition = new Vector2(_labelMap[kv.Key].style.left.value.value, 0)
                {
                    y = float.IsNaN(_labelMap[kv.Key].style.top.value.value) ? absoluteModeHeight : _labelMap[kv.Key].style.top.value.value
                };
                absoluteModeHeight += _attributeMap[kv.Key].FontSize;
                position = sceneViewPosition * _data.GameViewScale;
            }
            else
            {
                if (!_objectMap[kv.Key])
                    continue;
                var worldPos = _objectMap[kv.Key].transform.position + Vector3.up;
                screenPos = _gameCamera.WorldToScreenPoint(worldPos);

                if (screenPos.z < 0)
                    continue;

                var invertedY = Screen.height - screenPos.y;
                
                objectModeHeights.TryGetValue(_objectMap[kv.Key], out var offset);
                position = new Vector2(screenPos.x, invertedY + offset);

                var fontHeight = style.fontSize;
                if (!objectModeHeights.TryAdd(_objectMap[kv.Key], fontHeight))
                    objectModeHeights[_objectMap[kv.Key]] += fontHeight;
            }
            
            var scale = 1f / Mathf.Sqrt(screenPos.z * 0.1f);
            var textAlpha = (scale - 0.75f) * 4f;
            var labelWidth = 240f * _data.GameViewScale;
            var labelHeight = style.fontSize * _data.GameViewScale;
            
            // Label shadow
            style.normal.textColor = new Color(0f, 0f, 0f, 0.8f * textAlpha);
            style.hover.textColor = new Color(0f, 0f, 0f, 0.8f * textAlpha);
            var shadowOffset = 2f * _data.GameViewScale;
            DrawScaledLabel(new Rect(position.x + shadowOffset, position.y + shadowOffset, labelWidth, labelHeight),
                _labelTextShadowMap[kv.Key], style, scale);

            // Label
            style.normal.textColor = new Color(_attributeMap[kv.Key].Color.r,
                _attributeMap[kv.Key].Color.g, _attributeMap[kv.Key].Color.b, textAlpha);
            style.hover.textColor = new Color(_attributeMap[kv.Key].Color.r,
                _attributeMap[kv.Key].Color.g, _attributeMap[kv.Key].Color.b, textAlpha);
            DrawScaledLabel(new Rect(position.x, position.y, labelWidth, labelHeight),
                kv.Value.text, style, scale);
        }
    }
    
    private void DrawScaledLabel(Rect rect, string text, GUIStyle style, float scale)
    {
        var matrixBackup = GUI.matrix;
        GUIUtility.ScaleAroundPivot(Vector2.one * scale, rect.position);
        GUI.Label(rect, text, style);
        GUI.matrix = matrixBackup;
    }

    
    private void OnDestroy()
    {
        foreach (var label in _labelMap.Values)
        {
            label?.RemoveFromHierarchy();
        }
        _labelMap.Clear();
        _labelTextShadowMap.Clear();
        _objectMap.Clear();
        _reverseObjectMap.Clear();
        _attributeMap.Clear();
    }

    private void OnChangeTimerRefresh()
    {
        CheckForDeletedObjects();
        CreateMaps();
    }
    
    /// Check for nulled out objects and delete associated data
    private void CheckForDeletedObjects()
    {
        var idsPendingDeletion = new List<int>();
        var gameObjectsPendingDeletion = new List<GameObject>();
        
        foreach (var obj in _reverseObjectMap)
        {
            if (obj.Key)
                continue;
            idsPendingDeletion.AddRange(obj.Value);
            gameObjectsPendingDeletion.Add(obj.Key);
        }

        foreach (var obj in gameObjectsPendingDeletion)
            _reverseObjectMap.Remove(obj);

        foreach (var id in idsPendingDeletion)
        {
            _labelMap[id].RemoveFromHierarchy();
            _labelMap.Remove(id);
            _labelTextShadowMap.Remove(id);
            _objectMap.Remove(id);
            _attributeMap.Remove(id);
        }
    }
    
    /// Create data associated with a GameObject that has SceneLabel attributes
    private void CreateMaps()
    {
        var sceneActiveGameObjects = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
        foreach (var mono in sceneActiveGameObjects)
        {
            var type = mono.GetType();
            var fields = type.GetFields(System.Reflection.BindingFlags.Public |
                                        System.Reflection.BindingFlags.NonPublic |
                                        System.Reflection.BindingFlags.Instance);

            foreach (var field in fields)
            {
                var attr = (SceneLabelAttribute)System.Attribute.GetCustomAttribute(field,
                    typeof(SceneLabelAttribute));
                if (attr == null)
                    continue;

                var value = field.GetValue(mono);
                attr.Value = value;
                attr.GameObject = mono.gameObject;
                OnSetSpecialAttribute?.Invoke(attr, _data);
                var text = TextBuilder(attr.RichValue ?? attr.Value, attr, mono.name);
                var uncoloredText = TextBuilder(attr.FormatValue ?? attr.Value, attr, mono.name);
                var id = attr.GetUniqueID(mono, field);

                if (_labelMap.TryGetValue(id, out _))
                    continue;
                
                if (attr.SameLine && _reverseObjectMap.TryGetValue(mono.gameObject, out _))
                {
                    _attributeMap.TryAdd(id, attr);
                    continue;
                }

                var newLabel = new Label(text)
                {
                    style =
                    {
                        unityFont = interFont,
                        position = Position.Absolute,
                        color = attr.Color,
                        fontSize = attr.FontSize,
                        unityFontStyleAndWeight = attr.FontStyle,
                        unityTextAlign = TextAnchor.UpperLeft,
                        textShadow = new TextShadow
                        {
                            color = new Color(0, 0, 0, 0.8f),
                            offset = new Vector2(2, 2),
                            blurRadius = 1
                        }
                    },
                    pickingMode = PickingMode.Ignore,
                    usageHints = attr.AbsoluteMode ? UsageHints.None : UsageHints.DynamicTransform,
                    enableRichText = true
                };

                _labelMap.Add(id, newLabel);
                _labelTextShadowMap.Add(id, uncoloredText);
                _objectMap.Add(id, mono.gameObject);
                _reverseObjectMap.TryAdd(mono.gameObject, new List<int>());
                _reverseObjectMap[mono.gameObject].Add(id);
                _attributeMap.Add(id, attr);
            }
        }
    }
    
    /// Refresh text data for existing GameObjects that have SceneLabel attributes
    private void RefreshMaps()
    {
        var absoluteModeHeight = 10f;
        
        foreach (var kv in _attributeMap)
        {
            if (!kv.Value.GameObject)
                continue;
            
            OnSetSpecialAttribute?.Invoke(kv.Value, _data);
            
            var text = TextBuilder(kv.Value.RichValue ?? kv.Value.Value, kv.Value, kv.Value.GameObject.name);
            var uncoloredText = TextBuilder(kv.Value.FormatValue ?? kv.Value.Value, kv.Value, kv.Value.GameObject.name);

            if (_labelMap.TryGetValue(kv.Key, out var label))
            {
                label.text = text;
                _labelTextShadowMap[kv.Key] = uncoloredText;
                if (kv.Value.AbsoluteMode)
                {
                    label.style.left = 50;
                    label.style.top = absoluteModeHeight;
                    absoluteModeHeight += label.resolvedStyle.height;
                }
                continue;
            }
        
            if (kv.Value.SameLine && _reverseObjectMap.TryGetValue(kv.Value.GameObject, out var key))
            {
                _labelMap[key[^1]].text += text;
                _labelTextShadowMap[key[^1]] += uncoloredText;
            }
        }
    }
    
    /// Build label text
    private string TextBuilder(object value, SceneLabelAttribute attr, string objName)
    {
        var separator = attr.AbsoluteMode && !attr.SameLine ? $"[{objName}] " :
            attr.SameLine ? attr.Separator : string.Empty;
        
        switch (value)
        {
            default:
                return $"{separator}{attr.Prefix}{value}{attr.Suffix}";
            case null:
                return $"{separator}{attr.Prefix}null{attr.Suffix}";
            case IList { Count: 0 }:
                return $"{separator}{attr.Prefix}[]{attr.Suffix}";
            case IList list:
            {
                var sb = new System.Text.StringBuilder();
                sb.Append(separator);
                sb.Append(attr.Prefix);
                sb.Append("[");

                var displayCount = Mathf.Min(list.Count, 10);
                var i = 0;
                foreach (var element in list)
                {
                    sb.Append(element);

                    i += 1;
                    if (i < displayCount)
                        sb.Append(", ");
                }

                if (list.Count > displayCount)
                    sb.Append($" … ({list.Count} total)");

                sb.Append("]");
                sb.Append(attr.Suffix);
                
                return sb.ToString();
            }
        }
    }
}