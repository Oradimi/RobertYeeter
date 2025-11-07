#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object = UnityEngine.Object;

/// EDITOR TOOL
/// Displays values of all variables that possess the SceneLabel attribute on the SceneView and the GameView.
/// Those values can either be tied to the object position, or be displayed on the top left of the screen.
[InitializeOnLoad]
public static class SceneLabelOverlay
{
    /// Set of data passed to external decorators
    public class SceneLabelOverlayData
    {
        public bool IsGameView;
        public float SceneViewScale;
        public float GameViewScale;
    }
    
    public static System.Action<SceneLabelAttribute, SceneLabelOverlayData> OnSetSpecialAttribute;
    
    private static bool _visible = true;
    private static bool _richText = true;
    
    private static GameViewOverlay _runtimeOverlay;
    private static SceneLabelOverlayData _data;
    
    private static Dictionary<int, Label> _labelMap;
    private static Dictionary<int, string> _labelTextShadowMap;
    private static Dictionary<int, GameObject> _objectMap;
    private static Dictionary<GameObject, List<int>> _reverseObjectMap;
    private static Dictionary<int, SceneLabelAttribute> _attributeMap;
    
    private static Camera _gameCamera;
    private static float _sceneViewHeight;
    private static int _sceneObjectCount;
    private static float _gameViewTimer;

    static SceneLabelOverlay()
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
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorSceneManager.sceneClosed += OnSceneClosed;
        SceneView.duringSceneGui += OnSceneGUI;
        EditorApplication.hierarchyChanged += OnHierarchyChanged;
        EditorApplication.update += EnsureGameViewLabel;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode)
    {
        _gameCamera = Camera.main;
    }
    
    private static void OnSceneClosed(Scene scene)
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

    private static void OnHierarchyChanged()
    {
        CheckForDeletedObjects();
        CreateMaps();
    }
    
    /// Check for nulled out objects and delete associated data
    private static void CheckForDeletedObjects()
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
    
    /// Called when the SceneView repaints
    private static void OnSceneGUI(SceneView sceneView)
    {
        if (!_visible)
            return;
        
        _gameViewTimer += Time.deltaTime;
        _data.IsGameView = _gameViewTimer < 0.5f;
        RefreshMaps();
        
        var root = sceneView.rootVisualElement;
        var objectModeHeights = new Dictionary<GameObject, float>();
        
        foreach (var kv in _labelMap)
        {
            if (!_attributeMap[kv.Key].AbsoluteMode)
            {
                if (!_objectMap[kv.Key])
                    continue;
                
                var cam = sceneView.camera;
                var worldPos = _objectMap[kv.Key].transform.position + Vector3.up;
                var screenPos = cam.WorldToScreenPoint(worldPos);

                if (screenPos.z < 0)
                    continue;
                
                _sceneViewHeight = sceneView.position.height;
                var invertedY = _sceneViewHeight - screenPos.y;
                
                objectModeHeights.TryGetValue(_objectMap[kv.Key], out var offset);
                kv.Value.style.left = screenPos.x;
                kv.Value.style.top = invertedY + offset;
                
                var labelHeight = kv.Value.style.fontSize.value.value;
                if (!objectModeHeights.TryAdd(_objectMap[kv.Key], labelHeight))
                    objectModeHeights[_objectMap[kv.Key]] += labelHeight;
            }

            if (!root.Contains(kv.Value))
                root.Add(kv.Value);
        }
    }
    
    /// Ensure an invisible object is present all the time in scenes.
    /// This object is used to display debug info in GameView.
    private static void EnsureGameViewLabel()
    {
        if (_runtimeOverlay)
            return;
        
        var go = GameObject.Find("__GameViewOverlay");
        if (!go)
        {
            go = new GameObject("__GameViewOverlay")
            {
                hideFlags = HideFlags.HideAndDontSave
            };
        }
    
        _runtimeOverlay = go.GetComponent<GameViewOverlay>();
        
        if (!_runtimeOverlay)
            _runtimeOverlay = go.AddComponent<GameViewOverlay>();
    }
    
    /// Create data associated with a GameObject that has SceneLabel attributes
    private static void CreateMaps()
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
                if (_richText && Application.isPlaying)
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
                    enableRichText = _richText
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
    private static void RefreshMaps()
    {
        var absoluteModeHeight = 10f;
        
        if (_richText && Application.isPlaying)
        {
            foreach (var kv in _attributeMap)
                OnSetSpecialAttribute?.Invoke(kv.Value, _data);
        }

        foreach (var kv in _attributeMap)
        {
            if (!kv.Value.GameObject)
                continue;
            
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
    private static string TextBuilder(object value, SceneLabelAttribute attr, string objName)
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
    
    /// Used to display debug values in GameView
    [ExecuteInEditMode]
    private class GameViewOverlay : MonoBehaviour
    {
        private Font _interFont;
        
        private void Awake()
        {
            _gameCamera =  Camera.main;
            _interFont = EditorGUIUtility.Load("Fonts/Inter/Inter-Regular.ttf") as Font;
        }
        
        /// Called every frame when GameView is displaying
        private void OnGUI()
        {
            if (!_visible)
                return;
            
            if (Event.current.type != EventType.Repaint && Event.current.type != EventType.Layout)
                return;

            _gameViewTimer = 0f;
            _data.IsGameView = true;
            RefreshMaps();
            
            var absoluteModeHeight = 10f;
            var objectModeHeights = new Dictionary<GameObject, float>();

            foreach (var kv in _labelMap)
            {
                var applyDefaultStyle = _attributeMap[kv.Key].FormatValue != null ||
                                     float.IsNaN(kv.Value.resolvedStyle.width);
                
                _data.GameViewScale = _sceneViewHeight == 0f ? Screen.height / 640f : Screen.height / _sceneViewHeight;
                
                var style = new GUIStyle(GUI.skin.label)
                {
                    richText = _richText,
                    font = _interFont ? _interFont : null,
                    fontSize = Mathf.FloorToInt(_attributeMap[kv.Key].FontSize * _data.GameViewScale),
                    normal = { textColor = _attributeMap[kv.Key].Color },
                    alignment = kv.Value.style.unityTextAlign.value,
                    fontStyle = _attributeMap[kv.Key].FontStyle
                };

                Vector2 position;
                
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
                    var screenPos = _gameCamera.WorldToScreenPoint(worldPos);

                    if (screenPos.z < 0)
                        continue;

                    var invertedY = Screen.height - screenPos.y;
                    
                    objectModeHeights.TryGetValue(_objectMap[kv.Key], out var offset);
                    position = new Vector2(screenPos.x, invertedY + offset);

                    var labelHeight = style.fontSize;
                    if (!objectModeHeights.TryAdd(_objectMap[kv.Key], labelHeight))
                        objectModeHeights[_objectMap[kv.Key]] += labelHeight;
                }
                
                // Label shadow
                style.normal.textColor = new Color(0f, 0f, 0f, 0.8f);
                GUI.Label(new Rect(position.x + 2 * _data.GameViewScale, position.y + 2 * _data.GameViewScale,
                        applyDefaultStyle ? 960f * _data.GameViewScale : kv.Value.resolvedStyle.width * _data.GameViewScale,
                        float.IsNaN(kv.Value.resolvedStyle.height) ? style.fontSize * 1.5f : kv.Value.resolvedStyle.height * _data.GameViewScale),
                    _labelTextShadowMap[kv.Key], style);
                
                // Label
                style.normal.textColor = _attributeMap[kv.Key].Color;
                GUI.Label(new Rect(position.x, position.y,
                        applyDefaultStyle ? 960f * _data.GameViewScale : kv.Value.resolvedStyle.width * _data.GameViewScale,
                        float.IsNaN(kv.Value.resolvedStyle.height) ? style.fontSize * 1.5f : kv.Value.resolvedStyle.height * _data.GameViewScale),
                    kv.Value.text, style);
            }
        }
    }
    
    /// Settings menu for SceneLabelOverlays that can be accessed in the scene view
    [Overlay(typeof(SceneView), "Scene Label Overlay")]
    private class SceneLabelOverlayUI : Overlay
    {
        public override VisualElement CreatePanelContent()
        {
            var container = new VisualElement
            {
                style =
                {
                    paddingLeft = 8,
                    paddingTop = 4
                }
            };

            var showLabels = new Toggle("Show Labels") { value = _visible };
            showLabels.RegisterValueChangedCallback(evt =>
            {
                _visible = evt.newValue;
                foreach (var label in _labelMap.Values)
                    label.style.display = _visible ? DisplayStyle.Flex : DisplayStyle.None;
                _runtimeOverlay = null;
            });
            
            var richText = new Toggle("Rich Text") { value = _richText };
            richText.RegisterValueChangedCallback(evt =>
            {
                _richText = evt.newValue;
                foreach (var attr in _attributeMap.Values)
                {
                    attr.RichValue = null;
                    attr.FormatValue = null;
                }
            });

            container.Add(showLabels);
            container.Add(richText);
            return container;
        }
    }
}
#endif