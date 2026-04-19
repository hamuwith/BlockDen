using UnityEngine;

[CreateAssetMenu(fileName = "WeaponData", menuName = "Scriptable Objects/WeaponData")]
public class WeaponData : BlockData
{
    [SerializeField] int arrowId;
    [SerializeField] int damage;
    [SerializeField] int attackSpeed;
    [SerializeField] float range;
    public int ArrowId => arrowId;
    public int Damage => damage;
    public int AttackSpeed => attackSpeed;
    public float Range => range;
}
