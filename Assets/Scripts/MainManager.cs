using Cysharp.Threading.Tasks.Triggers;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    [SerializeField] string mapName;
    public string ResourceFolder => mapName;
    public MapManager MapManager { get; private set; }
    public ItemManager ItemManager { get; private set; }
    public WeaponManager WeaponManager { get; private set; }
    public EnemyManager EnemyManager { get; private set; }
    void Start()
    {
        ItemManager = GetComponent<ItemManager>();
        MapManager = GetComponent<MapManager>();
        WeaponManager = GetComponent<WeaponManager>();
        EnemyManager = GetComponent<EnemyManager>();
        ItemManager.Init(this);
        MapManager.Init(this);
        WeaponManager.Init(this);
        EnemyManager.Init(this);
    }
}
