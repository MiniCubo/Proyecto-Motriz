using UnityEngine;

public class SettingsUIHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject settingsUI;


    /// <summary>
    /// Call this from the "Settings" button to toggle the settings menu
    /// Uses the Scene ScriptableObject's ToggleUI function
    /// </summary>
    public void OnToggleSettingsButtonClick()
    {
        Debug.Log("[SettingsUI] Toggling settings menu");

        if (settingsUI != null)
        {
            Scene.Instance.ToggleUI(settingsUI);
        }
        else
        {
            Debug.LogError("[SettingsUI] Settings UI reference is missing!");
        }
    }

    /// <summary>
    /// Call this from the "Switch Display" button INSIDE settings
    /// This swaps physical displays (iPad <-> Projector)
    /// </summary>
    public void OnSwitchPhysicalDisplayButtonClick()
    {
        Debug.Log("[SettingsUI] Switching physical displays");

        DisplaySwitcher switcher = FindObjectOfType<DisplaySwitcher>();
        if (switcher != null)
        {
            switcher.SwapDisplays();
        }
        else
        {
            Debug.LogError("[SettingsUI] DisplaySwitcher not found in scene!");
        }
    }

    /// <summary>
    /// Alternative: Call this directly from buttons using the ScriptableObject
    /// You can also use this pattern directly in the Inspector without this script
    /// </summary>
    public void SwitchDisplaysDirectly()
    {
        OnSwitchPhysicalDisplayButtonClick();
    }
}