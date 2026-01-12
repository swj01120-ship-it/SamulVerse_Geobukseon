// Assets/Editor/FixUIMaterials.cs
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class FixUIMaterials
{
    [MenuItem("Tools/UI/Fix VisionUI Materials In Open Scene")]
    public static void FixInScene()
    {
        int fixedCount = 0;

        // Image
        foreach (var img in Object.FindObjectsOfType<Image>(true))
        {
            if (img.material != null && img.material.shader != null)
            {
                string s = img.material.shader.name;
                if (s.StartsWith("VisionUI/"))
                {
                    Undo.RecordObject(img, "Remove VisionUI Material");
                    img.material = null;
                    EditorUtility.SetDirty(img);
                    fixedCount++;
                }
            }
        }

        // RawImage
        foreach (var raw in Object.FindObjectsOfType<RawImage>(true))
        {
            if (raw.material != null && raw.material.shader != null)
            {
                string s = raw.material.shader.name;
                if (s.StartsWith("VisionUI/"))
                {
                    Undo.RecordObject(raw, "Remove VisionUI Material");
                    raw.material = null;
                    EditorUtility.SetDirty(raw);
                    fixedCount++;
                }
            }
        }

        Debug.Log($"[FixUIMaterials] Removed VisionUI materials: {fixedCount}");
    }
}
