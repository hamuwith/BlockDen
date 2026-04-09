using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Splines;

/// <summary>
/// StageSceneBindingは、StageSceneに必要なデータをまとめるクラスです。
/// </summary>
public class StageSceneBinding : MonoBehaviour
{
    public Enemy[] enemyPrefabs;
    public StageData stageData;
    public SplineContainer splineContainer;
}
