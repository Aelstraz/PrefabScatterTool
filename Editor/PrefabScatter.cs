using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Overlays;
using UnityEngine;

/// <summary>
/// Paint/scatter prefabs across the surface of a collider with a wide variety of options/randomizations
/// </summary>
public class PrefabScatter : ScriptableObject
{
    private static PrefabScatter instance = null;
    private RaycastHit hit;
    private readonly string UNDO_ADD_NAME = "Prefab Scatter Tool: Spawned prefabs";
    private readonly string UNDO_DELETE_NAME = "Prefab Scatter Tool: Removed prefabs";
    private PrefabScatterMode scatterMode = PrefabScatterMode.Standard;
    private int layerMask = 2;
    private List<GameObject> scatterPrefabs = new List<GameObject>();
    private float brushRadius = 10f;
    private Vector3 rotation = new Vector3(0f, 360f, 0f);
    private Vector2 scale = new Vector2(0.5f, 1.5f);
    private float heightOffset = 0f;
    private bool preventSpawningOffMesh = true;
    private float noiseScale = 1f;
    private float density = 0.9f;
    private Vector2Int spawnCount = new Vector2Int(1, 10);
    private bool preventOverlap = true;
    private float overlapSpacing = 2f;
    private Dictionary<Vector3Int, HashSet<GameObject>> overlapPositions = new Dictionary<Vector3Int, HashSet<GameObject>>();

    public enum PrefabScatterMode
    {
        Standard,
        Noise
    }

    /// <summary>
    /// Checks for user input and updates the scatter tool accordingly
    /// </summary>
    public void CheckUserInput()
    {
        if(Selection.activeGameObject == null)
        {
            return;
        }
        
        //set the layer of the selected object to our configured layer
        int objectLayer = Selection.activeGameObject.layer;
        Selection.activeGameObject.layer = GetLayerMask();
        //cast a ray from the mouse position using the configured layer
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        Physics.Raycast(ray, out hit, 1000f, LayerMask.GetMask(LayerMask.LayerToName(GetLayerMask())), QueryTriggerInteraction.Collide);

        //check if we hit our selected object
        if (hit.collider != null && hit.collider == Selection.activeGameObject.GetComponent<Collider>())
        {
            //draw a disc in the scene to represent the brush
            Handles.color = Color.green;
            Handles.DrawWireDisc(hit.point, hit.normal, GetBrushRadius());

            //check user mouse inputs
            Event e = Event.current;
            if (e.isMouse && e.button == 0)
            {
                if (e.type == EventType.MouseDown || e.type == EventType.MouseDrag)
                {
                    if (e.control)
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            Undo.RegisterFullObjectHierarchyUndo(Selection.activeGameObject, UNDO_DELETE_NAME);
                        }
                        DeletePrefabs();
                    }
                    else
                    {
                        if (e.type == EventType.MouseDown)
                        {
                            Undo.RegisterFullObjectHierarchyUndo(Selection.activeGameObject, UNDO_ADD_NAME);
                        }
                        SpawnPrefabs();
                    }

                    e.Use();
                }
            }
        }

        Selection.activeGameObject.layer = objectLayer;
    }

    /// <summary>
    /// Handles the undo/redo event and resets overlap positions
    /// </summary>
    /// <param name="info">The undo/redo information</param>
    public void OnUndoRedoEvent(in UndoRedoInfo info)
    {
        if (info.undoName == UNDO_ADD_NAME || info.undoName == UNDO_DELETE_NAME)
        {
            ClearOverlapDictionary();
            if (Selection.activeGameObject != null)
            {
                AddChildOverlapPositions(Selection.activeGameObject.transform);
            }
        }
    }

    /// <summary>
    /// Handles the selection changed event and updates overlap positions based on the selected GameObject
    /// </summary>
    private void OnSelectionChangedEvent()
    {
        if (Selection.activeGameObject == null)
        {
            ClearOverlapDictionary();
        }

        SceneView sceneView = SceneView.lastActiveSceneView;
        if (sceneView != null && sceneView.TryGetOverlay("prefab-scatter-tool-overlay", out Overlay overlay))
        {
            if (Selection.activeGameObject == null)
            {
                overlay.displayed = false;
                return;
            }
            else if (overlay.displayed)
            {
                ClearOverlapDictionary();
                AddChildOverlapPositions(Selection.activeGameObject.transform);
            }
        }
    }

    /// <summary>
    /// Deletes all prefabs within the brush area
    /// </summary>
    private void DeletePrefabs()
    {
        if (hit.collider == null || hit.collider != Selection.activeGameObject.GetComponent<Collider>())
        {
            return;
        }

        //loop through brush area
        for (float x = -GetBrushRadius(); x <= GetBrushRadius(); x++)
        {
            for (float y = -GetBrushRadius(); y <= GetBrushRadius(); y++)
            {
                //check if point is inside circle
                Vector2 pointInCircle = new Vector2(x, y);
                if (pointInCircle.magnitude > GetBrushRadius())
                {
                    continue;
                }

                //align point with normal of hit collider and transform to world space
                Vector3 localPoint = new Vector3(pointInCircle.x, pointInCircle.y, 0f);
                Quaternion circleRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                Vector3 newPosition = hit.point + circleRotation * localPoint;

                //destroy/remove all spawned objects at the position
                DestroyOverlapObjects(PositionToOverlapKey(newPosition));
            }
        }
    }

    /// <summary>
    /// Spawns prefabs based within the brush area based on the current scatter mode
    /// </summary>
    private void SpawnPrefabs()
    {
        if (hit.collider == null || hit.collider != Selection.activeGameObject.GetComponent<Collider>() || GetScatterPrefabCount() <= 0)
        {
            return;
        }

        switch (GetScatterMode())
        {
            case PrefabScatterMode.Noise:
                ScatterNoise();
                break;
            case PrefabScatterMode.Standard:
                ScatterStandard();
                break;
        }
    }

    /// <summary>
    /// Randomly scatters prefabs within the brush area
    /// </summary>
    public void ScatterStandard()
    {
        //get the random number of prefabs to spawn within the brush area
        int randomPrefabCount = Random.Range(GetSpawnCount().x, GetSpawnCount().y + 1);

        for (float i = 0; i < randomPrefabCount; i++)
        {
            //generate a random point within the circle
            Vector2 pointInCircle = Random.insideUnitCircle * GetBrushRadius();
            //align point with the normal of the hit collider and transform to world space
            Vector3 localPoint = new Vector3(pointInCircle.x, pointInCircle.y, 0f);
            Quaternion circleRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
            Vector3 newPosition = hit.point + circleRotation * localPoint;
            //calculate grid key position
            Vector3Int gridKey = PositionToOverlapKey(newPosition);

            //ignore spawning if the position overlaps with another object
            if (GetPreventOverlap() && OverlapDictionaryHasKey(gridKey))
            {
                continue;
            }

            if (GetPreventSpawningOffMesh())
            {
                //check if the position is off mesh
                Ray ray = new Ray(newPosition + hit.normal, -hit.normal);
                RaycastHit newHit;
                Physics.Raycast(ray, out newHit, 1.1f, LayerMask.GetMask(LayerMask.LayerToName(GetLayerMask())), QueryTriggerInteraction.Collide);

                if (newHit.collider == null || newHit.collider != Selection.activeGameObject.GetComponent<Collider>())
                {
                    continue;
                }
            }

            //apply height offset relative to the hit normal
            newPosition += hit.normal * heightOffset;

            //select a random prefab
            int randomPrefabIndex = Random.Range(0, GetScatterPrefabCount());
            //generate random scale
            Vector3 newScale = GetScatterPrefabScale(randomPrefabIndex) * Random.Range(GetScale().x, GetScale().y);
            //generate random rotation
            Vector3 originalPrefabEulerAngles = GetScatterPrefabEulerAngles(randomPrefabIndex);
            Quaternion newRotation = Quaternion.Euler(
                originalPrefabEulerAngles.x + Random.Range(0f, GetRotation().x),
                originalPrefabEulerAngles.y + Random.Range(0f, GetRotation().y),
                originalPrefabEulerAngles.z + Random.Range(0f, GetRotation().z));
            //apply rotation relative to the hit normal
            newRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * newRotation;

            //instantiate the randomly selected prefab and apply our randomized properties
            GameObject obj = PrefabUtility.InstantiatePrefab(GetScatterPrefab(randomPrefabIndex)) as GameObject;
            obj.transform.SetPositionAndRotation(newPosition, newRotation);
            obj.transform.localScale = newScale;
            obj.transform.SetParent(Selection.activeGameObject.transform, true);
            //add the object to the overlap dictionary
            AddOverlapObject(gridKey, obj);
        }
    }

    /// <summary>
    /// Scatters prefabs based on Perlin noise
    /// </summary>
    private void ScatterNoise()
    {
        //loop through brush area
        for (float x = -GetBrushRadius(); x <= GetBrushRadius(); x++)
        {
            for (float y = -GetBrushRadius(); y <= GetBrushRadius(); y++)
            {
                //check if point is inside circle
                Vector2 pointInCircle = new Vector2(x, y);
                if (pointInCircle.magnitude > GetBrushRadius())
                {
                    continue;
                }

                //align point with the normal of the hit collider and transform to world space
                Vector3 localPoint = new Vector3(pointInCircle.x, pointInCircle.y, 0f);
                Quaternion circleRotation = Quaternion.FromToRotation(Vector3.forward, hit.normal);
                Vector3 newPosition = hit.point + circleRotation * localPoint;

                //generate 2D perlin noise at our point
                float noiseValue = Mathf.PerlinNoise(newPosition.x * GetNoiseScale(), newPosition.z * GetNoiseScale());
                //check if generated noise meets or exceeds the density cutoff
                if (noiseValue < GetDensity())
                {
                    continue;
                }

                //calculate grid key position
                Vector3Int gridKey = PositionToOverlapKey(newPosition);

                //ignore spawning if the position overlaps with another object
                if (GetPreventOverlap() && OverlapDictionaryHasKey(gridKey))
                {
                    continue;
                }

                //apply height offset relative to the hit normal
                newPosition += hit.normal * heightOffset;

                //select a random prefab
                int randomPrefabIndex = Random.Range(0, GetScatterPrefabCount());
                //generate random scale
                Vector3 newScale = GetScatterPrefabScale(randomPrefabIndex) * Random.Range(GetScale().x, GetScale().y);
                //generate random rotation
                Vector3 originalPrefabEulerAngles = GetScatterPrefabEulerAngles(randomPrefabIndex);
                Quaternion newRotation = Quaternion.Euler(
                    originalPrefabEulerAngles.x + Random.Range(0f, GetRotation().x),
                    originalPrefabEulerAngles.y + Random.Range(0f, GetRotation().y),
                    originalPrefabEulerAngles.z + Random.Range(0f, GetRotation().z));
                //apply rotation relative to the hit normal
                newRotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * newRotation;

                //instantiate the randomly selected prefab and apply our randomized properties
                GameObject obj = PrefabUtility.InstantiatePrefab(GetScatterPrefab(randomPrefabIndex)) as GameObject;
                obj.transform.SetPositionAndRotation(newPosition, newRotation);
                obj.transform.localScale = newScale;
                obj.transform.SetParent(Selection.activeGameObject.transform, true);
                //add the object to the overlap dictionary
                AddOverlapObject(gridKey, obj);
            }
        }
    }

    /// <summary>
    /// Adds all children and their positions to the overlap dictionary
    /// </summary>
    /// <param name="parent">The parent transform containing all children to add to the overlap dictionary</param>
    public void AddChildOverlapPositions(Transform parent)
    {
        if (!preventOverlap)
        {
            return;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child != null)
            {
                AddOverlapObject(child.gameObject);
            }
        }
    }

    /// <summary>
    /// Adds the specified object and its position to the overlap dictionary
    /// </summary>
    /// <param name="gameObject">The GameObject to add</param>
    public void AddOverlapObject(GameObject gameObject)
    {
        Vector3Int key = PositionToOverlapKey(gameObject.transform.position);
        AddOverlapObject(key, gameObject);
    }

    /// <summary>
    /// Adds the specified object and its position to the overlap dictionary
    /// </summary>
    /// <param name="key">The rounded grid position to use as the dictionary key</param>
    /// <param name="gameObject">The GameObject to add</param>
    public void AddOverlapObject(Vector3Int key, GameObject gameObject)
    {
        if (overlapPositions.TryGetValue(key, out HashSet<GameObject> values))
        {
            values.Add(gameObject);
        }
        else
        {
            overlapPositions.Add(key, new HashSet<GameObject>() { gameObject });
        }
    }

    /// <summary>
    /// Destroys all objects contained at the specified key and removes it from the dictionary
    /// </summary>
    /// <param name="key">The rounded grid position to use as the dictionary key</param>
    public void DestroyOverlapObjects(Vector3Int key)
    {
        if (overlapPositions.TryGetValue(key, out HashSet<GameObject> values))
        {
            foreach (GameObject obj in values)
            {
                DestroyImmediate(obj);
            }
            overlapPositions.Remove(key);
        }
    }

    /// <summary>
    /// Gets the prefab at the specified index in the list of scatter prefabs
    /// </summary>
    /// <param name="index">The index of the prefab to retrieve</param>
    /// <returns>The prefab at the specified index, or null if the index is out of range</returns>
    public GameObject GetScatterPrefab(int index)
    {
        if (index >= 0 && index < scatterPrefabs.Count)
        {
            return scatterPrefabs[index];
        }
        return null;
    }

    /// <summary>
    /// Gets the rotation of the prefab at the specified index in the list of scatter prefabs
    /// </summary>
    /// <param name="index">The index of the prefab to retrieve</param>
    /// <returns>The euler rotation of the prefab at the specified index, or Vector3.zero if the index is out of range</returns>
    public Vector3 GetScatterPrefabEulerAngles(int index)
    {
        if (index >= 0 && index < scatterPrefabs.Count)
        {
            return scatterPrefabs[index].transform.rotation.eulerAngles;
        }
        return Vector3.zero;
    }

    /// <summary>
    /// Gets the scale of the prefab at the specified index in the list of scatter prefabs
    /// </summary>
    /// <param name="index">The index of the prefab to retrieve</param>
    /// <returns>The scale of the prefab at the specified index, or Vector3.one if the index is out of range</returns>
    public Vector3 GetScatterPrefabScale(int index)
    {
        if (index >= 0 && index < scatterPrefabs.Count)
        {
            return scatterPrefabs[index].transform.localScale;
        }
        return Vector3.one;
    }

    /// <summary>
    /// Gets the static PrefabScatter singleton instance
    /// </summary>
    /// <returns>The PrefabScatter singleton instance</returns>
    public static PrefabScatter GetInstance()
    {
        if (instance == null)
        {
            instance = CreateInstance(typeof(PrefabScatter)) as PrefabScatter;
            Undo.undoRedoEvent += instance.OnUndoRedoEvent;
            Selection.selectionChanged += instance.OnSelectionChangedEvent;
        }
        return instance;
    }

    public bool OverlapDictionaryHasKey(Vector3Int gridKey) => overlapPositions.ContainsKey(gridKey);
    public void ClearOverlapDictionary() => overlapPositions.Clear();
    private Vector3Int PositionToOverlapKey(Vector3 position) => new Vector3Int(PositionToOverlapKey(position.x), PositionToOverlapKey(position.y), PositionToOverlapKey(position.z));
    private int PositionToOverlapKey(float coordinate) => Mathf.FloorToInt(coordinate / overlapSpacing);
    public bool ContainsScatterPrefab(GameObject prefab) => scatterPrefabs.Contains(prefab);
    public void RemoveScatterPrefab(GameObject prefab) => scatterPrefabs.Remove(prefab);
    public void AddScatterPrefab(GameObject prefab) => scatterPrefabs.Add(prefab);
    public int GetScatterPrefabCount() => scatterPrefabs.Count;
    public PrefabScatterMode GetScatterMode() => scatterMode;
    public void SetScatterMode(PrefabScatterMode scatterMode) => this.scatterMode = scatterMode;
    public int GetLayerMask() => layerMask;
    public void SetLayerMask(int layerMask) => this.layerMask = layerMask;
    public float GetBrushRadius() => brushRadius;
    public void SetBrushRadius(float brushRadius) => this.brushRadius = brushRadius;
    public Vector3 GetRotation() => rotation;
    public void SetRotation(Vector3 rotation) => this.rotation = rotation;
    public Vector2 GetScale() => scale;
    public void SetScale(Vector2 scale) => this.scale = scale;
    public float GetNoiseScale() => noiseScale;
    public void SetNoiseScale(float noiseScale) => this.noiseScale = noiseScale;
    public float GetDensity() => density;
    public void SetDensity(float density) => this.density = density;
    public Vector2Int GetSpawnCount() => spawnCount;
    public void SetSpawnCount(Vector2Int spawnCount) => this.spawnCount = spawnCount;
    public bool GetPreventOverlap() => preventOverlap;
    public void SetPreventOverlap(bool preventOverlap) => this.preventOverlap = preventOverlap;
    public float GetOverlapSpacing() => overlapSpacing;
    public void SetOverlapSpacing(float overlapSpacing) => this.overlapSpacing = overlapSpacing;
    public float GetHeightOffset() => heightOffset;
    public void SetHeightOffset(float heightOffset) => this.heightOffset = heightOffset;
    public bool GetPreventSpawningOffMesh() => preventSpawningOffMesh;
    public void SetPreventSpawningOffMesh(bool preventSpawningOffMesh) => this.preventSpawningOffMesh = preventSpawningOffMesh;
}

