using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Splines;
using Cysharp.Threading.Tasks;
using System.Threading;
using static StageJsonLoader;
using System.IO;

public class EnemyManager : MonoBehaviour
{
    [SerializeField] EnemyPool[] enemyPools;
    [SerializeField] private string resourcePath = "stage";
    [SerializeField] float poisonEffectDuration = 3f;
    [SerializeField] int poisonEffectDamage = 2;
    [SerializeField] float poisonEffectInterval = 0.5f;
    [SerializeField] float iceEffectDuration = 0.6f;
    [SerializeField] float iceEffectSlowMultiplier = 0.6f;
    [SerializeField] float lightningEffectRange = 0.3f;
    [SerializeField] int lightningEffectDamage = 1;
    [SerializeField] float shiningEffectDuration = 0.2f;
    [SerializeField] float darkEffectDuration = 2f;
    [SerializeField] float strongEffectMultiplier = 0.1f;
    public float PoisonEffectDuration => poisonEffectDuration;
    public int PoisonEffectDamage => poisonEffectDamage;
    public float PoisonEffectInterval => poisonEffectInterval;
    public float IceEffectDuration => iceEffectDuration;
    public float IceEffectSlowMultiplier => iceEffectSlowMultiplier;
    public float LightningEffectRange => lightningEffectRange;
    public int LightningEffectDamage => lightningEffectDamage;
    public float ShiningEffectDuration => shiningEffectDuration;
    public float DarkEffectDuration => darkEffectDuration;
    public float StrongEffectMultiplier => strongEffectMultiplier;

    List<Enemy> enemies;
    List<EnemySpawn> enemyDatas;
    SplineContainer splineContainer;
    CancellationTokenSource cancellationTokenSource;
    public MainManager MainManager { get; private set; }
    //bool clear;
    public int EnemyNum => enemies.Count;
    public float[] SplineLengths { get; private set; }
    /// <summary>
    /// EnemyManagerを初期化します。
    /// </summary>
    /// <param name="mainManager"></param>
    public void Init(MainManager mainManager)
    {
        MainManager = mainManager;
        string path = Path.Combine(mainManager.ResourceFolder, resourcePath);
        StageJson json = LoadFromResources(path);

        StageRuntimeData runtimeData = StageRuntimeData.FromStageJson(json);

        splineContainer = runtimeData.splineContainer;

        enemyDatas = runtimeData.spawns;

        SplineLengths = new float[splineContainer.Splines.Count];
        for (int i = 0; i < splineContainer.Splines.Count; i++)
        {
            SplineLengths[i] =  SplineUtility.CalculateLength(splineContainer.Splines[i], splineContainer.transform.localToWorldMatrix);
        }
        enemies = new List<Enemy>();
        for (int i = 0; i < enemyPools.Length; i++)
        {
            enemyPools[i].Init(this);
        }
        //clear = false;
        cancellationTokenSource = new CancellationTokenSource();
        SpawnEnemy(cancellationTokenSource.Token).Forget();
    }
    private async UniTaskVoid SpawnEnemy(CancellationToken cancellationToken)
    {
        int index = 0;
        while (enemyDatas.Count > index)
        {
            float nextTime = enemyDatas[index].time;
            await UniTask.Delay((int)(nextTime * 1000), cancellationToken: cancellationToken);
            while (enemyDatas.Count > index)
            {
                var enemyData = enemyDatas[index];
                if (enemyData.time == nextTime)
                {
                    enemyPools[enemyData.id].GetEnemy().Init(this, splineContainer.Splines[enemyData.line], SplineLengths[enemyData.line]);
                    index++;
                }
                else
                {
                    break;
                }
            }
        }
        while (EnemyNum > 0)
        {
            await UniTask.Yield(cancellationToken);
        }
        //clear = true;
    }
    /// <summary>
    /// Enemyを管理リストに追加します。
    /// </summary>
    /// <param name="enemy"></param>
    public void AddEnemy(Enemy enemy)
    {
        enemies.Add(enemy);
    }
    /// <summary>
    /// Enemyを管理リストから削除します。
    /// </summary>
    /// <param name="enemy"></param>
    public void RemoveEnemy(Enemy enemy)
    {
        enemies.Remove(enemy);
    }
    /// <summary>
    /// 指定した位置から最も近いEnemyを返します。
    /// </summary>
    /// <param name="position"></param>
    /// <returns></returns>
    public Enemy NearestEnemy(Vector3 position)
    {
        Enemy nearest = null;
        float minDistance = float.MaxValue;
        foreach (var enemy in enemies)
        {
            float distance = Vector3.Distance(position, enemy.transform.position);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearest = enemy;
            }
        }
        return nearest;
    }
    /// <summary>
    /// Enemyの中で最もHPの高いEnemyを返します。
    /// </summary>
    /// <returns></returns>
    public Enemy HighestEnemy()
    {
        Enemy highest = null;
        float maxHeight = float.MinValue;
        foreach (var enemy in enemies)
        {
            float height = enemy.transform.position.y;
            if (height > maxHeight)
            {
                maxHeight = height;
                highest = enemy;
            }
        }
        return highest;
    }
}
/// <summary>
/// ステージデータをJSONから読み込むためのクラスです。
/// </summary>

public static class StageJsonLoader
{
    /// <summary>
    /// ResourcesフォルダからJSONを読み込み、StageJsonオブジェクトに変換します。
    /// </summary>
    /// <param name="resourcePath"></param>
    /// <returns></returns>
    public static StageJson LoadFromResources(string resourcePath)
    {
        TextAsset jsonAsset = Resources.Load<TextAsset>(resourcePath);
        if (jsonAsset == null)
        {
            Debug.LogError($"JSON not found in Resources: {resourcePath}");
            return null;
        }

        StageJson data = JsonUtility.FromJson<StageJson>(jsonAsset.text);
        if (data == null)
        {
            Debug.LogError($"Failed to parse JSON: {resourcePath}");
            return null;
        }

        return data;
    }
    /// <summary>
    /// PersistentDataPathからJSONを読み込み、StageJsonオブジェクトに変換します。
    /// </summary>
    /// <param name="fileName"></param>
    /// <returns></returns>

    public static StageJson LoadFromPersistent(string fileName)
    {
        string path = Path.Combine(Application.persistentDataPath, fileName);

        if (!File.Exists(path))
        {
            Debug.LogError($"JSON file not found: {path}");
            return null;
        }

        string json = File.ReadAllText(path);
        StageJson data = JsonUtility.FromJson<StageJson>(json);
        if (data == null)
        {
            Debug.LogError($"Failed to parse JSON: {path}");
            return null;
        }

        return data;
    }
    /// <summary>
    /// StageJsonオブジェクトからステージの実行時データを構築します。
    /// </summary>
    public class StageRuntimeData
    {
        public SplineContainer splineContainer;
        public List<EnemySpawn> spawns = new();

        public static StageRuntimeData FromStageJson(StageJson json)
        {
            if (json == null)
                return null;

            StageRuntimeData data = new StageRuntimeData();

            data.splineContainer = BuildSplineContainer(json, "RuntimeSplineContainer");
            data.spawns.AddRange(json.spawns);

            return data;
        }

        private static SplineContainer BuildSplineContainer(StageJson json, string objectName)
        {
            GameObject go = new GameObject(objectName);
            SplineContainer container = go.AddComponent<SplineContainer>();

            var splines = new List<Spline>();

            foreach (var splineJson in json.splines)
            {
                if (splineJson == null || splineJson.knots == null || splineJson.knots.Count < 2)
                    continue;

                Spline spline = new Spline
                {
                    Closed = splineJson.closed
                };

                var knotPositions = new List<Unity.Mathematics.float3>(splineJson.knots.Count);
                foreach (var knot in splineJson.knots)
                {
                    Vector3 p = knot.ToVector3();
                    knotPositions.Add(new Unity.Mathematics.float3(p.x, p.y, p.z));
                }

                spline.AddRange(knotPositions);
                splines.Add(spline);
            }

            container.Splines = splines;
            return container;
        }
    }
}
