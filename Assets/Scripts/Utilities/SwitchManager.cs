using System;
using UnityEngine;
using UnityEngine.Events;
using TriInspector;
#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
#endif

/// Singleton component that holds the switch array and handles its calls.
[HideMonoScript]
public class SwitchManager : MonoBehaviour
{
    private static SwitchManager _instance;
    
    private float[] _switches = new float[128];
    
    [SerializeField, HideInInspector] private UnityEvent<int>[] onSwitchChanged = new UnityEvent<int>[128];
    
    private void Awake()
    {
        if (_instance)
        {
            Destroy(this);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(_instance);
    }
    
    /// Set a callback to listen for changes to the switch at the specified channel identifier.
    /// <param name="call">Callback function called when this switch is changed.</param>
    /// <param name="channel">Switch channel to listen to.</param>
    public static void AddListenerOnChannel(UnityAction<int> call, int channel)
    {
        if (!_instance)
        {
            Debug.LogWarning($"SwitchManager instance is null (AddListenerOnChannel)");
            return;
        }
        
        if (channel >= _instance.onSwitchChanged.Length || channel < 0)
        {
            Debug.LogWarning($"Invalid channel {channel} (AddListenerOnChannel)");
            return;
        }

        _instance.onSwitchChanged[channel].AddListener(call);
    }
    
    /// Stop a callback from listening to changes to the switch at the specified channel identifier.
    /// <param name="call">Callback function called when this switch is changed.</param>
    /// <param name="channel">Switch channel to stop listening to.</param>
    public static void RemoveListenerOnChannel(UnityAction<int> call, int channel)
    {
        if (!_instance)
        {
            Debug.LogWarning($"SwitchManager instance is null (RemoveListenerOnChannel)");
            return;
        }

        if (channel >= _instance.onSwitchChanged.Length || channel < 0)
        {
            Debug.LogWarning($"Invalid channel {channel} (RemoveListenerOnChannel)");
            return;
        }
            
        _instance.onSwitchChanged[channel].RemoveListener(call);
    }
    
    /// Get switch's state using specified channel identifier.
    /// <param name="channel">The channel from which to get the switch's state.</param>
    /// <returns>The channel's state.</returns>
    public static bool GetSwitch(int channel)
    {
        if (!_instance)
        {
            Debug.LogWarning($"SwitchManager instance is null (GetSwitch)");
            return false;
        }

        if (channel >= _instance.onSwitchChanged.Length || channel < 0)
        {
            Debug.LogWarning($"Invalid channel {channel} (GetSwitch)");
            return false;
        }
        
        return _instance._switches[(uint)channel] > 0.5f;
    }
    
    /// Get switch's floating value using specified channel identifier.
    /// <param name="channel">The channel from which to get the switch's state.</param>
    /// <returns>The channel's state.</returns>
    public static float GetSwitchFloat(int channel)
    {
        if (!_instance)
        {
            Debug.LogWarning($"SwitchManager instance is null (GetSwitch)");
            return 0;
        }

        if (channel >= _instance.onSwitchChanged.Length || channel < 0)
        {
            Debug.LogWarning($"Invalid channel {channel} (GetSwitch)");
            return 0;
        }
        
        return _instance._switches[(uint)channel];
    }
    
    /// Set switch's state using specified channel identifier. Invokes onSwitchChanged if the switch state changes.
    /// <param name="channel">Switch to be set.</param>
    /// <param name="value">New Switch value.</param>
    public static void SetSwitch(int channel, bool value)
    {
        if (!_instance)
        {
            Debug.LogWarning($"SwitchManager instance is null (SetSwitch)");
            return;
        }

        if (channel >= _instance.onSwitchChanged.Length || channel < 0)
        {
            Debug.LogWarning($"Invalid channel {channel} (SetSwitch)");
            return;
        }
        
        var floatValue = value ? 1 : 0;
        var isSwitchStateChanged = (_instance._switches[(uint)channel] > 0.5f) ^ value;
        _instance._switches[(uint)channel] = floatValue;
        if (isSwitchStateChanged)
            _instance.onSwitchChanged[channel]?.Invoke(channel);
    }
    
    /// Set switch's floating value using specified channel identifier.
    /// Invokes onSwitchChanged if the switch floating value crosses the halfway threshold.
    /// <param name="channel">Switch to be set.</param>
    /// <param name="value">New Switch value.</param>
    public static void SetSwitchFloat(int channel, float value)
    {
        if (!_instance)
        {
            Debug.LogWarning($"SwitchManager instance is null (SetSwitch)");
            return;
        }

        if (channel >= _instance.onSwitchChanged.Length || channel < 0)
        {
            Debug.LogWarning($"Invalid channel {channel} (SetSwitch)");
            return;
        }

        value = Mathf.Clamp01(value);
        var isSwitchStateChanged = (_instance._switches[(uint)channel] > 0.5f) ^ (value > 0.5f);
        _instance._switches[(uint)channel] = value;
        if (isSwitchStateChanged)
            _instance.onSwitchChanged[channel]?.Invoke(channel);
    }
    
    /// Reset all switches.
    public static void ResetSwitches()
    {
        if (!_instance)
        {
            Debug.LogWarning($"SwitchManager instance is null (ResetSwitches)");
            return;
        }
        
        Array.Fill(_instance._switches, 0);
    }
    
#if UNITY_EDITOR
    /// Colors the values depending on the channel number
    [Tooltip("Colors the values depending on the channel number")]
    [Header("[Editor only] Rich Text Settings (SceneLabelOverlay)")]
    [SerializeField] private Gradient _switchValueColorGradient = new Gradient();
    
    /// Display switch value with the channel number
    [Tooltip("Display switch value with the channel number")]
    [SerializeField] private bool _displayValues;
    
    /// EDITOR TOOL
    private void Reset()
    {
        _displayValues = false;
        var colors = new GradientColorKey[3];
        colors[0] = new GradientColorKey(Color.red, 0.0f);
        colors[1] = new GradientColorKey(Color.goldenRod, 0.5f);
        colors[2] = new GradientColorKey(Color.green, 1.0f);
        _switchValueColorGradient.SetColorKeys(colors);
    }
    
    /// EDITOR TOOL
    private void OnEnable()
    {
        SceneLabelOverlay.OnSetSpecialAttribute = SetSwitchValueLabelColor;
    }
    
    /// EDITOR TOOL
    /// Process rich text for switch values
    private static void SetSwitchValueLabelColor(SceneLabelAttribute sceneLabel, SceneLabelOverlay.SceneLabelOverlayData data)
    {
        if (sceneLabel.ID != SceneLabelID.Switch)
            return;

        switch (sceneLabel.Value) {
            default:
                Debug.LogWarning("Scene label value is invalid");
                break;
            case int sceneLabelChannel:
            {
                var value = "";
                if (_instance._displayValues)
                {
                    var fontSize = sceneLabel.FontSize * 0.5f * (data.IsGameView ? data.GameViewScale : data.SceneViewScale);
                    value = $"<size={fontSize}>({_instance._switches[sceneLabelChannel]:F2})</size>";
                    sceneLabel.FormatValue = $"{sceneLabelChannel}{value}";
                }
                else
                {
                    sceneLabel.FormatValue = null;
                }
                var channelColor = _instance._switchValueColorGradient.Evaluate(_instance._switches[sceneLabelChannel]);
                sceneLabel.RichValue = $"<color=#{channelColor.ToHexString()}>{sceneLabelChannel}{value}</color>";
                break;
            }
            case IList list:
            {
                var richElementList = new List<object>();
                var formatElementList = new List<object>();
                foreach (int sceneLabelChannel in list)
                {
                    var value = "";
                    if (_instance._displayValues)
                    {
                        var fontSize = sceneLabel.FontSize * 0.5f * (data.IsGameView ? data.GameViewScale : data.SceneViewScale);
                        value = $"<size={fontSize}>({_instance._switches[sceneLabelChannel]:F2})</size>";
                        formatElementList.Add($"{sceneLabelChannel}{value}");
                    }
                    var channelColor = _instance._switchValueColorGradient.Evaluate(_instance._switches[sceneLabelChannel]);
                    richElementList.Add($"<color=#{channelColor.ToHexString()}>{sceneLabelChannel}{value}</color>");
                }
                sceneLabel.RichValue = richElementList;
                sceneLabel.FormatValue = formatElementList.Count == 0 ? null : formatElementList;
                break;
            }
        }
    }
#endif
}
