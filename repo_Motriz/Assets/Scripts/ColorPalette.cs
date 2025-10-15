using UnityEngine;

[CreateAssetMenu(fileName = "ColorPalette", menuName = "Project/Color Palette")]
public class ColorPalette : ScriptableObject
{
    [System.Serializable]
    public struct NamedColor
    {
        public string name;
        public Color color;
    }

    public NamedColor[] colors;

    public Color GetColor(string colorName)
    {
        foreach (var c in colors)
        {
            if (c.name == colorName) return c.color;
        }
        Debug.LogWarning($"Color '{colorName}' not found in palette.");
        return Color.white;
    }
}
