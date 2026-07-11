using UnityEditor;
using UnityEditor.Overlays;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// Editor user interface for configuring the Prefab Scatter Tool
/// </summary>
[Overlay(typeof(SceneView), id: "prefab-scatter-tool-overlay", displayName: "Prefab Scatter Tool")]
public class PrefabScatterToolOverlay : Overlay
{
    private Foldout prefabContainer = null;

    public override VisualElement CreatePanelContent()
    {
        VisualElement root = new VisualElement();
        root.style.width = 350;

        //add IMGUIContainer to listen to editor object picker events
        var imguiListener = new IMGUIContainer(() =>
        {
            ObjectPickerEventListener();
        });
        root.Add(imguiListener);

        Foldout settingsContainer = new Foldout();
        settingsContainer.text = "Settings";
        root.Add(settingsContainer);

        Box mainContainer = new Box();
        LoadMainFields(mainContainer);
        settingsContainer.Add(mainContainer);

        Box scatterModeContainer = new Box();
        LoadScatterModeFields(scatterModeContainer);
        settingsContainer.Add(scatterModeContainer);

        Box overlapContainer = new Box();
        LoadOverlapFields(overlapContainer);
        settingsContainer.Add(overlapContainer);

        prefabContainer = new Foldout();
        LoadPrefabFields(prefabContainer);
        root.Add(prefabContainer);

        return root;
    }

    private void LoadMainFields(VisualElement container)
    {
        container.Clear();
        container.style.marginBottom = 10f;

        Label titleLabel = new Label("[Main]");
        titleLabel.style.color = Color.green;
        container.Add(titleLabel);

        FloatField brushRadiusField = new FloatField("Brush Radius");
        brushRadiusField.tooltip = "Configures the circle radius of the brush in the scene/world";
        brushRadiusField.value = PrefabScatter.GetInstance().GetBrushRadius();
        brushRadiusField.RegisterValueChangedCallback(evt =>
        {
            PrefabScatter.GetInstance().SetBrushRadius(evt.newValue);
            brushRadiusField.value = evt.newValue;
        });
        container.Add(brushRadiusField);

        Vector3Field rotationField = new Vector3Field("Rotation");
        rotationField.tooltip = "Applies a random rotation (relative to the prefab) per spawned object, with per axis control (X, Y, Z)";
        rotationField.value = PrefabScatter.GetInstance().GetRotation();
        rotationField.RegisterValueChangedCallback(evt =>
        {
            PrefabScatter.GetInstance().SetRotation(evt.newValue);
            rotationField.value = evt.newValue;
        });
        container.Add(rotationField);

        Vector2Field scaleField = new Vector2Field("Scale");
        scaleField.tooltip = "Applies a random scale (relative to the prefab) per spawned object, ";
        scaleField.value = PrefabScatter.GetInstance().GetScale();
        scaleField.RegisterValueChangedCallback(evt =>
        {
            PrefabScatter.GetInstance().SetScale(evt.newValue);
            scaleField.value = evt.newValue;
        });
        container.Add(scaleField);

        FloatField heightOffsetField = new FloatField("Height Offset");
        heightOffsetField.tooltip = "Applies a fixed height offset (relative to the normal of the surface being painted)";
        heightOffsetField.value = PrefabScatter.GetInstance().GetHeightOffset();
        heightOffsetField.RegisterValueChangedCallback(evt =>
        {
            PrefabScatter.GetInstance().SetHeightOffset(evt.newValue);
            heightOffsetField.value = evt.newValue;
        });
        container.Add(heightOffsetField);

        LayerField layerField = new LayerField("Layer Mask", PrefabScatter.GetInstance().GetLayerMask());
        layerField.tooltip = "Configures the layer mask that the collider is moved in/out of when painting (to avoid raycasts hitting other colliders)";
        layerField.RegisterValueChangedCallback(evt =>
        {
            PrefabScatter.GetInstance().SetLayerMask(evt.newValue);
            layerField.value = evt.newValue;
        });
        container.Add(layerField);

        Toggle preventSpawningOffMeshToggle = new Toggle("Prevent Spawning Off Mesh");
        preventSpawningOffMeshToggle.tooltip = "Forces spawned objects to only be spawned on the current surface by performing a raycast for each object";
        preventSpawningOffMeshToggle.value = PrefabScatter.GetInstance().GetPreventSpawningOffMesh();
        preventSpawningOffMeshToggle.RegisterValueChangedCallback(evt =>
        {
            PrefabScatter.GetInstance().SetPreventSpawningOffMesh(evt.newValue);
            preventSpawningOffMeshToggle.value = evt.newValue;
        });
        container.Add(preventSpawningOffMeshToggle);
    }

    private void LoadScatterModeFields(VisualElement container)
    {
        container.Clear();
        container.style.marginBottom = 10f;

        Label titleLabel = new Label("[Mode]");
        titleLabel.style.color = Color.green;
        container.Add(titleLabel);

        EnumField scatterModeField = new EnumField("Scatter Mode", PrefabScatter.GetInstance().GetScatterMode());
        scatterModeField.tooltip = "Configures the way the scatter tool places objects\n\n[Standard] Randomly selects positions\n[Noise] Uses 2D Perlin noise to calculate positions";
        scatterModeField.RegisterValueChangedCallback(evt =>
        {
            PrefabScatter.GetInstance().SetScatterMode((PrefabScatter.PrefabScatterMode)evt.newValue);
            scatterModeField.value = evt.newValue;
            LoadScatterModeFields(container);
        });
        container.Add(scatterModeField);

        switch (PrefabScatter.GetInstance().GetScatterMode())
        {
            case PrefabScatter.PrefabScatterMode.Noise:
                FloatField densityField = new FloatField("Density");
                densityField.tooltip = "Configures the spawn threshold (relative to the calculated Perlin noise)";
                densityField.value = PrefabScatter.GetInstance().GetDensity();
                densityField.RegisterValueChangedCallback(evt =>
                {
                    PrefabScatter.GetInstance().SetDensity(evt.newValue);
                    densityField.value = evt.newValue;
                });
                container.Add(densityField);

                FloatField noiseScaleField = new FloatField("Noise Scale");
                noiseScaleField.tooltip = "Configures the resolution of the Perlin noise";
                noiseScaleField.value = PrefabScatter.GetInstance().GetNoiseScale();
                noiseScaleField.RegisterValueChangedCallback(evt =>
                {
                    PrefabScatter.GetInstance().SetNoiseScale(evt.newValue);
                    noiseScaleField.value = evt.newValue;
                });
                container.Add(noiseScaleField);
                break;
            case PrefabScatter.PrefabScatterMode.Standard:
                Vector2IntField spawnCountField = new Vector2IntField("Spawn Count");
                spawnCountField.tooltip = "Configures the number of objects to randomly spawn (chooses a random number between min and max)";
                spawnCountField.value = PrefabScatter.GetInstance().GetSpawnCount();
                spawnCountField.RegisterValueChangedCallback(evt =>
                {
                    PrefabScatter.GetInstance().SetSpawnCount(evt.newValue);
                    spawnCountField.value = evt.newValue;
                });
                container.Add(spawnCountField);
                break;
        }
    }

    private void LoadOverlapFields(VisualElement container)
    {
        container.Clear();
        container.style.marginBottom = 10f;

        Label titleLabel = new Label("[Overlap]");
        titleLabel.style.color = Color.green;
        container.Add(titleLabel);

        Toggle preventOverlapToggle = new Toggle("Prevent Overlap");
        preventOverlapToggle.tooltip = "Forces spawned objects to not occupy the same position";
        preventOverlapToggle.value = PrefabScatter.GetInstance().GetPreventOverlap();
        preventOverlapToggle.RegisterValueChangedCallback(evt =>
        {
            PrefabScatter.GetInstance().SetPreventOverlap(evt.newValue);
            preventOverlapToggle.value = evt.newValue;
            //reload this section
            LoadOverlapFields(container);
        });
        container.Add(preventOverlapToggle);

        if (PrefabScatter.GetInstance().GetPreventOverlap())
        {
            FloatField overlapSpacingField = new FloatField("Overlap Spacing");
            overlapSpacingField.tooltip = "Configures the spacing allowed between spawned objects";
            overlapSpacingField.value = PrefabScatter.GetInstance().GetOverlapSpacing();
            overlapSpacingField.RegisterValueChangedCallback(evt =>
            {
                PrefabScatter.GetInstance().SetOverlapSpacing(evt.newValue);
                overlapSpacingField.value = evt.newValue;
                //reset the overlap dictionary
                PrefabScatter.GetInstance().ClearOverlapDictionary();
                PrefabScatter.GetInstance().AddChildOverlapPositions(Selection.activeGameObject.transform);
            });
            container.Add(overlapSpacingField);
        }
    }

    private void LoadPrefabFields(Foldout container)
    {
        container.Clear();
        container.text = $"Prefabs ({PrefabScatter.GetInstance().GetScatterPrefabCount()})";
        container.tooltip = "Configures the list of randomly selected objects to spawn";

        for (int i = 0; i < PrefabScatter.GetInstance().GetScatterPrefabCount(); i++)
        {
            GameObject prefab = PrefabScatter.GetInstance().GetScatterPrefab(i);
            VisualElement rowContainer = new VisualElement();
            rowContainer.style.flexDirection = FlexDirection.Row;

            Label prefabLabel = new Label(prefab.name);
            prefabLabel.style.color = Color.green;
            prefabLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            rowContainer.Add(prefabLabel);

            Button removeButton = new Button(() =>
            {
                PrefabScatter.GetInstance().RemoveScatterPrefab(prefab);
                //reload this section
                LoadPrefabFields(container);
            })
            { text = "Remove" };
            removeButton.tooltip = "Removes this prefab from the list of randomly selected objects to spawn";
            rowContainer.Add(removeButton);

            container.Add(rowContainer);
        }

        Button addPrefabButton = new Button(() =>
        {
            EditorGUIUtility.ShowObjectPicker<GameObject>(null, false, "t:Prefab", GUIUtility.GetControlID(FocusType.Passive));
        })
        { text = "Add Prefab" };
        addPrefabButton.tooltip = "Add a prefab to the list of randomly selected objects to spawn";
        addPrefabButton.style.marginTop = 10f;
        container.Add(addPrefabButton);
    }

    private void ObjectPickerEventListener()
    {
        Event currentEvent = Event.current;
        if (currentEvent != null && currentEvent.type == EventType.ExecuteCommand)
        {
            //check if the object picker was closed
            if (currentEvent.commandName == "ObjectSelectorClosed")
            {
                Object selected = EditorGUIUtility.GetObjectPickerObject();

                //check if the selected object is not already in our list of scatter prefabs
                if (selected != null && !PrefabScatter.GetInstance().ContainsScatterPrefab(selected as GameObject))
                {
                    //add the selected object to our list of scatter prefabs
                    PrefabScatter.GetInstance().AddScatterPrefab(selected as GameObject);
                    //reload the prefab fields section
                    LoadPrefabFields(prefabContainer);
                }
            }
        }
    }
}
