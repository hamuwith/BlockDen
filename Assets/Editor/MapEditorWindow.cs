using UnityEditor;
using UnityEngine;

public class MapEditorWindow : EditorWindow
{
    private static readonly Vector3Int InvalidGridPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

    private MapDataHolder mapHolder;
    private int placeBlockId;
    private bool editMode;
    private bool isDeleteMode;
    private Vector3Int lastPlacedGridPos = InvalidGridPos;
    private Vector3Int lastDeletedGridPos = InvalidGridPos;

    [MenuItem("Tools/Map Editor")]
    public static void Open()
    {
        GetWindow<MapEditorWindow>("Map Editor");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("Map Editor", EditorStyles.boldLabel);

        mapHolder = (MapDataHolder)EditorGUILayout.ObjectField(
            "Map Holder",
            mapHolder,
            typeof(MapDataHolder),
            true
        );

        if (mapHolder != null)
        {
            mapHolder.EnsureInitialized();
        }

        editMode = EditorGUILayout.Toggle("Edit Mode", editMode);

        EditorGUILayout.Space();

        DrawBlockSelector();

        EditorGUILayout.HelpBox(
            "Turn on Edit Mode. Left click places a block. Hold D and left click to delete. Press A to fill one block above each column.",
            MessageType.Info
        );

        using (new EditorGUI.DisabledScope(mapHolder == null))
        {
            if (GUILayout.Button("Reset Map"))
            {
                Undo.RecordObject(mapHolder, "Reset Map");

                for (int i = mapHolder.transform.childCount - 1; i >= 0; i--)
                {
                    Undo.DestroyObjectImmediate(mapHolder.transform.GetChild(i).gameObject);
                }

                mapHolder.InitializeBlockIds(-1);
                EditorUtility.SetDirty(mapHolder);
            }

            if (GUILayout.Button("Rebuild Map Preview"))
            {
                mapHolder.RebuildMap();
                EditorUtility.SetDirty(mapHolder);
            }

            if (GUILayout.Button("Save Map Json"))
            {
                string path = EditorUtility.SaveFilePanel(
                    "Save Map Json",
                    Application.dataPath,
                    "map.json",
                    "json"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    mapHolder.SaveToJson(path);
                }
            }

            if (GUILayout.Button("Load Map Json"))
            {
                string path = EditorUtility.OpenFilePanel(
                    "Load Map Json",
                    Application.dataPath,
                    "json"
                );

                if (!string.IsNullOrEmpty(path))
                {
                    mapHolder.LoadFromJson(path);
                    EditorUtility.SetDirty(mapHolder);
                }
            }
        }
    }

    private void DrawBlockSelector()
    {
        if (mapHolder == null)
        {
            EditorGUILayout.LabelField("Block ID", "No Map Holder selected");
            return;
        }

        if (!mapHolder.HasBlockDefinitions)
        {
            EditorGUILayout.LabelField("Block ID", "No block data");
            EditorGUILayout.HelpBox(
                "Assign BlockDataSO and Block Base Prefab on MapDataHolder.",
                MessageType.Warning
            );
            return;
        }

        placeBlockId = Mathf.Clamp(placeBlockId, 0, Mathf.Max(0, mapHolder.BlockOptionCount - 1));
        placeBlockId = EditorGUILayout.Popup("Block ID", placeBlockId, mapHolder.GetBlockOptions());

        if (mapHolder.UsesBlockData)
        {
            EditorGUILayout.HelpBox("Using BlockDataSO + base prefab.", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("Assign Block Base Prefab on MapDataHolder.", MessageType.Warning);
        }
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!editMode || mapHolder == null)
        {
            return;
        }

        mapHolder.EnsureInitialized();

        Event e = Event.current;
        Ray ray = HandleUtility.GUIPointToWorldRay(e.mousePosition);

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.D)
        {
            isDeleteMode = true;
            e.Use();
        }

        if (e.type == EventType.KeyUp && e.keyCode == KeyCode.D)
        {
            isDeleteMode = false;
            e.Use();
        }

        if (e.type == EventType.KeyDown && e.keyCode == KeyCode.A)
        {
            FillTopPlusOne();
            e.Use();
        }

        if (isDeleteMode)
        {
            bool hasDeleteTarget = TryGetDeletePosition(ray, out Vector3Int deleteGridPos);
            if (hasDeleteTarget)
            {
                DrawDeletePreview(deleteGridPos);
            }

            if (!e.alt)
            {
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    if (hasDeleteTarget && deleteGridPos != lastDeletedGridPos)
                    {
                        DeleteBlock(ray);
                        lastDeletedGridPos = deleteGridPos;
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    lastDeletedGridPos = InvalidGridPos;
                }
            }
        }
        else
        {
            bool hasPlacementTarget = TryGetPlacementPosition(ray, out Vector3Int placeGridPos)
                && mapHolder.IsInRange(placeGridPos);

            if (hasPlacementTarget)
            {
                DrawPreview(placeGridPos);
            }

            if (!e.alt)
            {
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    if (hasPlacementTarget && placeGridPos != lastPlacedGridPos)
                    {
                        PlaceBlock(placeGridPos);
                        lastPlacedGridPos = placeGridPos;
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    lastPlacedGridPos = InvalidGridPos;
                }
            }
        }

        sceneView.Repaint();
    }

    private bool TryGetPlacementPosition(Ray ray, out Vector3Int gridPos)
    {
        gridPos = Vector3Int.zero;

        if (Physics.Raycast(ray, out RaycastHit hit, 1000f)
            && hit.collider.transform.IsChildOf(mapHolder.transform))
        {
            Vector3 placePos = hit.collider.transform.position + hit.normal;
            gridPos = Vector3Int.RoundToInt(placePos);
            return true;
        }

        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);
        if (!groundPlane.Raycast(ray, out float enter))
        {
            return false;
        }

        Vector3 hitPoint = ray.GetPoint(enter);
        gridPos = new Vector3Int(
            Mathf.FloorToInt(hitPoint.x),
            0,
            Mathf.FloorToInt(hitPoint.z)
        );

        return true;
    }

    private void DrawPreview(Vector3Int gridPos)
    {
        Vector3 center = new Vector3(gridPos.x, gridPos.y, gridPos.z);

        Color fillColor = new Color(0f, 1f, 1f, 0.25f);
        Color wireColor = new Color(0f, 1f, 1f, 1f);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.DrawWireCube(center, Vector3.one);

        Color previousColor = Handles.color;
        Handles.color = fillColor;
        Handles.CubeHandleCap(0, center, Quaternion.identity, 1f, EventType.Repaint);
        Handles.color = wireColor;
        Handles.DrawWireCube(center, Vector3.one);
        Handles.color = previousColor;
    }

    private void PlaceBlock(Vector3Int gridPos)
    {
        if (!mapHolder.IsInRange(gridPos) || !mapHolder.IsEmpty(gridPos))
        {
            return;
        }

        if (!mapHolder.CanInstantiateBlock(placeBlockId))
        {
            Debug.LogWarning("No block data or prefab is configured for the selected block ID.");
            return;
        }

        Undo.RecordObject(mapHolder, "Set Block ID");
        GameObject instance = mapHolder.CreatePlacedBlock(placeBlockId, gridPos);
        if (instance == null)
        {
            Debug.LogWarning("Failed to create the block preview object.");
            return;
        }

        Undo.RegisterCreatedObjectUndo(instance, "Place Block");
        mapHolder.SetBlockId(gridPos, placeBlockId);
        EditorUtility.SetDirty(mapHolder);
    }

    private bool TryGetDeletePosition(Ray ray, out Vector3Int gridPos)
    {
        gridPos = Vector3Int.zero;

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return false;
        }

        if (!hit.collider.transform.IsChildOf(mapHolder.transform))
        {
            return false;
        }

        gridPos = Vector3Int.RoundToInt(hit.collider.transform.position);
        return mapHolder.IsInRange(gridPos) && !mapHolder.IsEmpty(gridPos);
    }

    private void DrawDeletePreview(Vector3Int gridPos)
    {
        Vector3 center = new Vector3(gridPos.x, gridPos.y, gridPos.z);

        Color fillColor = new Color(1f, 0f, 0f, 0.25f);
        Color wireColor = new Color(1f, 0f, 0f, 1f);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        Color previousColor = Handles.color;
        Handles.color = fillColor;
        Handles.CubeHandleCap(0, center, Quaternion.identity, 1f, EventType.Repaint);
        Handles.color = wireColor;
        Handles.DrawWireCube(center, Vector3.one);
        Handles.color = previousColor;
    }

    private void DeleteBlock(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            return;
        }

        if (!hit.collider.transform.IsChildOf(mapHolder.transform))
        {
            return;
        }

        Vector3Int gridPos = Vector3Int.RoundToInt(hit.collider.transform.position);
        if (!mapHolder.IsInRange(gridPos) || mapHolder.IsEmpty(gridPos))
        {
            return;
        }

        Undo.RecordObject(mapHolder, "Delete Block");
        mapHolder.SetBlockId(gridPos, -1);
        Undo.DestroyObjectImmediate(hit.collider.gameObject);
        EditorUtility.SetDirty(mapHolder);
    }

    private void FillTopPlusOne()
    {
        if (mapHolder == null)
        {
            return;
        }

        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        for (int x = 0; x < mapHolder.Width; x++)
        {
            for (int z = 0; z < mapHolder.Depth; z++)
            {
                Vector3Int? placePos = null;

                for (int y = 1; y < mapHolder.Height; y++)
                {
                    Vector3Int current = new Vector3Int(x, y, z);
                    Vector3Int below = new Vector3Int(x, y - 1, z);

                    if (mapHolder.IsEmpty(current) && !mapHolder.IsEmpty(below))
                    {
                        placePos = current;
                        break;
                    }
                }

                if (placePos == null)
                {
                    int highestY = -1;

                    for (int y = 0; y < mapHolder.Height; y++)
                    {
                        if (!mapHolder.IsEmpty(new Vector3Int(x, y, z)))
                        {
                            highestY = y;
                        }
                    }

                    int targetY = highestY + 1;
                    if (targetY >= 0 && targetY < mapHolder.Height)
                    {
                        Vector3Int pos = new Vector3Int(x, targetY, z);
                        if (mapHolder.IsEmpty(pos))
                        {
                            placePos = pos;
                        }
                    }
                }

                if (placePos.HasValue)
                {
                    PlaceBlock(placePos.Value);
                }
            }
        }

        Undo.CollapseUndoOperations(undoGroup);
    }
}
