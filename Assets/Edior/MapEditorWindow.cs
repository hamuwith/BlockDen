using UnityEditor;
using UnityEngine;

public class MapEditorWindow : EditorWindow
{
    private MapDataHolder mapHolder;
    private int placeBlockId = 0;
    private bool editMode = false; 
    private bool isDeleteMode; 
    private Vector3Int lastPlacedGridPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
    private Vector3Int lastDeletedGridPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);

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
        if (mapHolder != null)
        {
            mapHolder.EnsureInitialized();
        }

        EditorGUILayout.LabelField("Map Editor", EditorStyles.boldLabel);

        mapHolder = (MapDataHolder)EditorGUILayout.ObjectField(
            "Map Holder",
            mapHolder,
            typeof(MapDataHolder),
            true
        );

        editMode = EditorGUILayout.Toggle("Edit Mode", editMode);

        EditorGUILayout.Space();

        if (mapHolder != null && mapHolder.blockPrefabs != null && mapHolder.blockPrefabs.Length > 0)
        {
            string[] options = new string[mapHolder.blockPrefabs.Length];
            for (int i = 0; i < options.Length; i++)
            {
                options[i] = mapHolder.blockPrefabs[i] != null
                    ? $"{i}: {mapHolder.blockPrefabs[i].name}"
                    : $"{i}: None";
            }

            placeBlockId = EditorGUILayout.Popup("Block ID", placeBlockId, options);
        }
        else
        {
            EditorGUILayout.LabelField("Block ID", "No Prefabs");
        }

        EditorGUILayout.HelpBox(
            "Edit Mode を ON にして SceneView 上で左クリックするとブロックを配置します。",
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
                mapHolder.initialized = true;
                EditorUtility.SetDirty(mapHolder);
            }
        }

        if (mapHolder != null)
        {
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

    private void OnSceneGUI(SceneView sceneView)
    {
        if (!editMode || mapHolder == null)
            return;

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
            if (TryGetDeletePosition(ray, out Vector3Int deleteGridPos))
            {
                DrawDeletePreview(deleteGridPos);
            }
            if (!e.alt)
            {
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    if (deleteGridPos != lastDeletedGridPos)
                    {
                        DeleteBlock(ray);
                        lastDeletedGridPos = deleteGridPos;
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    lastDeletedGridPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
                }
            }
        }
        else {
            if(TryGetPlacementPosition(ray, out Vector3Int placeGridPos) && mapHolder.IsInRange(placeGridPos))
            {
                DrawPreview(placeGridPos);
            }
            if (!e.alt)
            {
                if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) && e.button == 0)
                {
                    if (placeGridPos != lastPlacedGridPos)
                    {
                        PlaceBlock(placeGridPos);
                        lastPlacedGridPos = placeGridPos;
                    }
                    e.Use();
                }
                else if (e.type == EventType.MouseUp && e.button == 0)
                {
                    lastPlacedGridPos = new Vector3Int(int.MinValue, int.MinValue, int.MinValue);
                }
            }
        }

        sceneView.Repaint();
    }
    private bool TryGetPlacementPosition(Ray ray, out Vector3Int gridPos)
    {
        gridPos = Vector3Int.zero;

        // まず既存ブロックに当たるか試す
        if (Physics.Raycast(ray, out RaycastHit hit, 1000f))
        {
            Vector3 placePos = hit.collider.transform.position + hit.normal;
            gridPos = Vector3Int.RoundToInt(placePos);
            return true;
        }

        // 当たらなければ y=0 の床にフォールバック
        Plane groundPlane = new Plane(Vector3.up, Vector3.zero);

        if (!groundPlane.Raycast(ray, out float enter))
            return false;

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
        Vector3 center = new Vector3(
            gridPos.x,
            gridPos.y,
            gridPos.z
        );

        Color fillColor = new Color(0f, 1f, 1f, 0.25f);
        Color wireColor = new Color(0f, 1f, 1f, 1f);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;
        Handles.DrawWireCube(center, Vector3.one);

        Color prevColor = Handles.color;
        Handles.color = fillColor;
        Handles.CubeHandleCap(0, center, Quaternion.identity, 1f, EventType.Repaint);
        Handles.color = prevColor;

        Handles.color = wireColor;
        Handles.DrawWireCube(center, Vector3.one);
    }

    private void PlaceBlock(Vector3Int gridPos)
    {
        if (!mapHolder.IsEmpty(gridPos))
            return;

        int prefabIndex = placeBlockId;
        if (mapHolder.blockPrefabs == null
            || prefabIndex < 0
            || prefabIndex >= mapHolder.blockPrefabs.Length
            || mapHolder.blockPrefabs[prefabIndex] == null)
        {
            Debug.LogWarning("対応するプレハブがありません。");
            return;
        }

        GameObject prefab = mapHolder.blockPrefabs[prefabIndex];
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, mapHolder.transform);

        instance.transform.position = new Vector3(gridPos.x, gridPos.y, gridPos.z);
        instance.name = $"Block_{placeBlockId}_{gridPos.x}_{gridPos.y}_{gridPos.z}";

        Undo.RegisterCreatedObjectUndo(instance, "Place Block");
        Undo.RecordObject(mapHolder, "Set Block ID");
        mapHolder.SetBlockId(gridPos, placeBlockId);

        EditorUtility.SetDirty(mapHolder);
    }

    private bool TryGetDeletePosition(Ray ray, out Vector3Int gridPos)
    {
        gridPos = Vector3Int.zero;

        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
            return false;

        gridPos = Vector3Int.RoundToInt(hit.collider.transform.position);
        return mapHolder.IsInRange(gridPos) && !mapHolder.IsEmpty(gridPos);
    }

    private void DrawDeletePreview(Vector3Int gridPos)
    {
        Vector3 center = new Vector3(
            gridPos.x,
            gridPos.y,
            gridPos.z
        );

        Color fillColor = new Color(1f, 0f, 0f, 0.25f);
        Color wireColor = new Color(1f, 0f, 0f, 1f);

        Handles.zTest = UnityEngine.Rendering.CompareFunction.LessEqual;

        Color prevColor = Handles.color;

        Handles.color = fillColor;
        Handles.CubeHandleCap(0, center, Quaternion.identity, 1f, EventType.Repaint);

        Handles.color = wireColor;
        Handles.DrawWireCube(center, Vector3.one);

        Handles.color = prevColor;
    }

    private void DeleteBlock(Ray ray)
    {
        if (!Physics.Raycast(ray, out RaycastHit hit, 1000f))
            return;

        Vector3Int gridPos = Vector3Int.RoundToInt(hit.collider.transform.position);

        if (!mapHolder.IsInRange(gridPos))
            return;

        if (mapHolder.IsEmpty(gridPos))
            return;

        Undo.RecordObject(mapHolder, "Delete Block");
        mapHolder.SetBlockId(gridPos, -1);

        Undo.DestroyObjectImmediate(hit.collider.gameObject);
        EditorUtility.SetDirty(mapHolder);
    }
    private void FillTopPlusOne()
    {
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();

        for (int x = 0; x < mapHolder.Width; x++)
        {
            for (int z = 0; z < mapHolder.Depth; z++)
            {
                Vector3Int? placePos = null;

                // まず下から見て、途中の空洞を1マスだけ埋める
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

                // 空洞がなければ、その列の一番上に1マス足す
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
