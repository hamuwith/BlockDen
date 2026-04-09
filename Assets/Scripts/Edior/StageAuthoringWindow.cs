#if UNITY_EDITOR
using System.Collections.Generic;
using System;
using Unity.Mathematics;
using UnityEditor;
using UnityEngine;
using System.IO;
using UnityEngine.Splines;


public class StageAuthoringWindow : EditorWindow
{
    private float previewTime;
    private StageSceneBinding binding; 
    private bool isPlaying;
    private double lastEditorTime; 
    private float flattenY = 0f;
    private bool autoSmoothPreview = true; 
    private int targetSplineIndex = 0;

    [MenuItem("Tools/Stage Authoring")]
    public static void Open()
    {
        GetWindow<StageAuthoringWindow>("Stage Authoring");
    }

    private void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI; 
        lastEditorTime = EditorApplication.timeSinceStartup;
    }

    private void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    private void OnGUI()
    {
        binding = (StageSceneBinding)EditorGUILayout.ObjectField(
    "Binding", binding, typeof(StageSceneBinding), true);

        if (binding == null || binding.stageData == null) return;

        targetSplineIndex = EditorGUILayout.IntField("Target Spline Index", targetSplineIndex);
        targetSplineIndex = Mathf.Clamp(targetSplineIndex, 0, Mathf.Max(0, binding.splineContainer.Splines.Count - 1));

        flattenY = EditorGUILayout.FloatField("Flatten Y", flattenY);
        autoSmoothPreview = EditorGUILayout.Toggle("Keep Auto Smooth", autoSmoothPreview);

        using (new EditorGUI.DisabledScope(binding == null || binding.splineContainer == null))
        {
            if (GUILayout.Button("Flatten Spline Y"))
            {
                FlattenSplineY(binding.splineContainer, targetSplineIndex, flattenY);

                if (autoSmoothPreview)
                    ForceAutoSmooth(binding.splineContainer, targetSplineIndex);
            }

            if (GUILayout.Button("Force Auto Smooth"))
            {
                ForceAutoSmooth(binding.splineContainer, targetSplineIndex);
            }
        }

        previewTime = EditorGUILayout.FloatField("Preview Time", previewTime);

        isPlaying = EditorGUILayout.Toggle("Play Preview", isPlaying);

        if (GUILayout.Button("Reset Preview Time"))
        {
            previewTime = 0f;
            lastEditorTime = EditorApplication.timeSinceStartup;
        }

        if (GUILayout.Button("Export JSON"))
        {
            StageJsonExporter.Export(binding);
        }
    }
    private static void FlattenSplineY(SplineContainer container, int splineIndex, float targetY)
    {
        if (container == null)
            return;

        if (splineIndex < 0 || splineIndex >= container.Splines.Count)
            return;

        Undo.RecordObject(container, "Flatten Spline Y");

        var spline = container.Splines[splineIndex];

        for (int knotIndex = 0; knotIndex < spline.Count; knotIndex++)
        {
            var knot = spline[knotIndex];
            knot.Position.y = targetY;
            spline[knotIndex] = knot;
        }

        EditorUtility.SetDirty(container);
    }
    private static void ForceAutoSmooth(SplineContainer container, int splineIndex)
    {
        if (container == null)
            return;

        if (splineIndex < 0 || splineIndex >= container.Splines.Count)
            return;

        Undo.RecordObject(container, "Force Auto Smooth");

        var spline = container.Splines[splineIndex];

        for (int knotIndex = 0; knotIndex < spline.Count; knotIndex++)
        {
            spline.SetTangentMode(knotIndex, TangentMode.AutoSmooth);
            spline.SetAutoSmoothTension(knotIndex, SplineUtility.DefaultTension);
        }

        EditorUtility.SetDirty(container);
    }
    private void Update()
    {
        if (!isPlaying)
        {
            lastEditorTime = EditorApplication.timeSinceStartup;
            return;
        }

        double now = EditorApplication.timeSinceStartup;
        float deltaTime = (float)(now - lastEditorTime);
        lastEditorTime = now;

        previewTime += deltaTime;

        Repaint();
        SceneView.RepaintAll();
    }

    private void OnSceneGUI(SceneView sceneView)
    {
        if (binding == null || binding.stageData == null || binding.splineContainer == null) return;
        StagePreviewDrawer.Draw(binding, previewTime);
        sceneView.Repaint();
    }
}
public static class StagePreviewDrawer
{
    public static void Draw(StageSceneBinding binding, float previewTime)
    {
        var data = binding.stageData;
        var splineContainer = binding.splineContainer;

        foreach (var spawn in data.spawns)
        {
            if (spawn.line < 0 || spawn.line >= splineContainer.Splines.Count)
                continue;

            if (spawn.id < 0 || binding.enemyPrefabs == null || spawn.id >= binding.enemyPrefabs.Length)
                continue;

            Enemy enemy = binding.enemyPrefabs[spawn.id];
            if (enemy == null || enemy.MoveSpeed <= 0f)
                continue;

            if (previewTime < spawn.time)
                continue;

            float elapsed = previewTime - spawn.time;
            if (elapsed < 0f)
                continue;

            float splineLength = splineContainer.CalculateLength(spawn.line);
            float duration = splineLength / enemy.MoveSpeed;
            if (duration <= 0f)
                continue;

            float t = Mathf.Clamp01((previewTime - spawn.time) / duration);

            if (!splineContainer.Evaluate(spawn.line, t, out float3 pos, out float3 dir, out float3 up))
                continue;

            DrawQuad((Vector3)pos, (Vector3)dir, (Vector3)up, data.previewQuadSize, GetColor(spawn.id));
        }
    }

    private static void DrawQuad(Vector3 pos, Vector3 forward, Vector3 up, float size, Color color)
    {
        if (forward.sqrMagnitude < 0.0001f) forward = Vector3.forward;
        if (up.sqrMagnitude < 0.0001f) up = Vector3.up;

        Quaternion rot = Quaternion.LookRotation(forward, up);
        Vector3 right = rot * Vector3.right * size * 0.5f;
        Vector3 top = rot * Vector3.up * size * 0.5f;

        Vector3 p0 = pos - right - top;
        Vector3 p1 = pos + right - top;
        Vector3 p2 = pos + right + top;
        Vector3 p3 = pos - right + top;

        Color old = Handles.color;
        Handles.color = color;
        Handles.DrawAAConvexPolygon(p0, p1, p2, p3);
        Handles.color = Color.black;
        Handles.DrawAAPolyLine(2f, p0, p1, p2, p3, p0);
        Handles.color = old;
    }

    private static Color GetColor(int id)
    {
        return Color.HSVToRGB((id * 0.17f) % 1f, 0.7f, 1f);
    }
}
public static class StageJsonExporter
{
    public static void Export(StageSceneBinding binding)
    {
        if (binding == null || binding.stageData == null || binding.splineContainer == null) return;

        var data = binding.stageData;
        var splineContainer = binding.splineContainer;
        var jsonData = new StageJson();

        for (int i = 0; i < splineContainer.Splines.Count; i++)
        {
            var spline = splineContainer.Splines[i];
            var sj = new SplineJson
            {
                line = i,
                closed = spline.Closed
            };

            foreach (var knot in spline.Knots)
            {
                Vector3 world = splineContainer.transform.TransformPoint((Vector3)knot.Position);
                sj.knots.Add(new Vec3Json(world));
            }

            jsonData.splines.Add(sj);
        }

        foreach (var s in data.spawns)
        {
            jsonData.spawns.Add(new EnemySpawn
            {
                id = s.id,
                time = s.time,
                line = s.line
            });
        }

        string path = EditorUtility.SaveFilePanel("Export Stage Json", Application.dataPath, "stage.json", "json");
        if (string.IsNullOrEmpty(path)) return;

        string json = JsonUtility.ToJson(jsonData, true);
        File.WriteAllText(path, json);
        AssetDatabase.Refresh();
    }
}
#endif
[Serializable]
public class StageJson
{
    public List<SplineJson> splines = new();
    public List<EnemySpawn> spawns = new();
}

[Serializable]
public class SplineJson
{
    public int line;
    public bool closed;
    public List<Vec3Json> knots = new();
}

[Serializable]
public class EnemySpawn
{
    public int id;
    public float time;
    public int line;
}

[Serializable]
public class Vec3Json
{
    public float x;
    public float y;
    public float z;

    public Vec3Json() { }
    public Vec3Json(Vector3 v) { x = v.x; y = v.y; z = v.z; }

    public Vector3 ToVector3() => new Vector3(x, y, z);
}