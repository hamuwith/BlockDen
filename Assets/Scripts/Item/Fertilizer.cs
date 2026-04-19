public class Fertilizer : Item
{

    public void SumFertilizer(ref FertilizerStatus fertilizerStatus, FertilizerStatus attachFertilizerStatus)
    {
        fertilizerStatus.Rate += attachFertilizerStatus.Rate;
        fertilizerStatus.PlusNum += attachFertilizerStatus.PlusNum;
        fertilizerStatus.Speed += attachFertilizerStatus.Speed;
    }
}
public struct FertilizerStatus
{
    public float Rate;
    public int PlusNum;
    public float Speed;
}
