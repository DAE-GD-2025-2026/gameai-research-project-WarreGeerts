using System;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIControl : MonoBehaviour
{
    [SerializeField] private GameObject speedInput;
    [SerializeField] private Toggle toggleDebug;

    public static Action GenerateAction;
    public static Action<float> SpeedChangeAction;
    public static Action<bool> DebugChangeAction;

    public void Generate()
    {
        GenerateAction?.Invoke();
    }

    public void SpeedChange()
    {
        var input = speedInput.GetComponent<TMP_InputField>();

        if (float.TryParse(input.text, NumberStyles.Float, CultureInfo.InvariantCulture, out float num))
        {
            Debug.Log(num);
            SpeedChangeAction?.Invoke(num);
            Debug.Log($"Parsed: {num}");
        }
        else
        {
            Debug.LogWarning($"Could not parse speed value: '{input.text}'");
        }
    }

    public void DebugChange()
    {
        Debug.Log(toggleDebug.isOn);
        DebugChangeAction?.Invoke(toggleDebug.isOn);
    }
    
}