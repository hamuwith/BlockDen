using UnityEngine;

[CreateAssetMenu(fileName = "AttachmentData", menuName = "Scriptable Objects/AttachmentData")]
public class AttachmentData : ItemData
{
    [SerializeField] AttachmentStatus attachmentStatus;
    public AttachmentStatus AttachmentStatus => attachmentStatus;
}
