using UnityEngine;

[CreateAssetMenu(fileName = "WeaponBaseData", menuName = "Scriptable Objects/WeaponBaseData")]
public class WeaponBaseData : BlockData
{
    [SerializeField] Vector2Int boardSize;
    public Vector2Int BoardSize => boardSize;
}
