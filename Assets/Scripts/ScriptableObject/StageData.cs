using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Splines;

[CreateAssetMenu(menuName = "Stage/Stage Data")]
public class StageData : ScriptableObject
{
    public List<EnemySpawnData> spawns = new();
    public float previewDuration = 10f;
    public float previewQuadSize = 0.3f;
}

[Serializable]
public class EnemySpawnData
{
    public int id;
    public float time;
    public int line;
}
