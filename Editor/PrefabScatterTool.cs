using UnityEngine;
using UnityEditor;
using UnityEditor.EditorTools;
using UnityEditor.Overlays;

/// <summary>
/// Adds the Prefab Scatter Tool to Unity's editor tools
/// </summary>
[EditorTool("Prefab Scatter Tool", typeof(Collider))]
public class PrefabScatterTool : EditorTool
{
    public override void OnActivated()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.TryGetOverlay("prefab-scatter-tool-overlay", out Overlay overlay))
        {
            overlay.displayed = true;
            PrefabScatter.GetInstance().AddChildOverlapPositions(Selection.activeGameObject.transform);
        }
    }

    public override void OnWillBeDeactivated()
    {
        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.TryGetOverlay("prefab-scatter-tool-overlay", out Overlay overlay))
        {
            overlay.displayed = false;
            PrefabScatter.GetInstance().ClearOverlapDictionary();
        }
    }

    public override void OnToolGUI(EditorWindow window)
    {
        if (!(window is SceneView))
        {
            return;
        }

        PrefabScatter.GetInstance().CheckUserInput();
    }
}
