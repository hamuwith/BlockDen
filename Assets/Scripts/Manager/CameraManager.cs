using UnityEngine;
using System.Collections.Generic;

public class CameraManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private List<Transform> targets;

    [Header("View")]
    [SerializeField] private float padding = 2f;
    [SerializeField] private float minSize = 4f;
    [SerializeField] private float maxSize = 9f;

    [Header("Smoothing")]
    [SerializeField] private float moveSmoothTime = 0.2f;
    [SerializeField] private float sizeLerpSpeed = 5f;

    [Header("Map Bounds (ground XZ)")]
    [SerializeField] private Vector2 mapMinXZ = new Vector2(0f, 0f);
    [SerializeField] private Vector2 mapMaxXZ = new Vector2(40f, 30f);

    private Camera cam;
    private Vector3 moveVelocity;
    Vector3 distance;

    public void Init()
    {
        cam = GetComponent<Camera>();
        distance = new Vector3(transform.position.x * transform.forward.x / transform.forward.y, transform.position.y, transform.position.y * transform.forward.z / transform.forward.y);
        mapMinXZ = new Vector2(mapMinXZ.x + distance.x, mapMinXZ.y + distance.z);
        mapMaxXZ = new Vector2(mapMaxXZ.x + distance.x, mapMaxXZ.y + distance.z);
    }
    public void SetTargets(Transform newTarget)
    {
        targets.Add(newTarget);
    }

    private void LateUpdate()
    {
        if (cam == null || targets == null || targets.Count == 0)
            return;

        // 有効ターゲットの中心
        Vector3 worldCenter = GetTargetsCenter();

        // カメラ回転基準の right / up 方向に、各ターゲットがどれだけ散っているか計算
        float targetSize = CalculateRequiredOrthoSize(worldCenter);

        // min / max
        targetSize = Mathf.Clamp(targetSize, minSize, maxSize);

        // まず中心に寄せたい位置
        Vector3 desiredPos = GetCameraPositionFromCenter(worldCenter);

        // そのサイズでマップ外を映さないように clamp
        desiredPos = ClampCameraPosition(desiredPos, targetSize);

        // なめらか追従
        transform.position = Vector3.SmoothDamp(
            transform.position,
            desiredPos,
            ref moveVelocity,
            moveSmoothTime
        );

        cam.orthographicSize = Mathf.Lerp(
            cam.orthographicSize,
            targetSize,
            Time.deltaTime * sizeLerpSpeed
        );
    }

    private Vector3 GetTargetsCenter()
    {
        Vector3 sum = Vector3.zero;
        int count = 0;

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;
            sum += targets[i].position;
            count++;
        }

        return count > 0 ? sum / count : transform.position;
    }

    private float CalculateRequiredOrthoSize(Vector3 worldCenter)
    {
        Vector3 camRight = transform.right;
        Vector3 camUp = transform.up;

        float maxAbsX = 0f; // カメラ横方向の広がり
        float maxAbsY = 0f; // カメラ縦方向の広がり

        for (int i = 0; i < targets.Count; i++)
        {
            if (targets[i] == null) continue;

            Vector3 offset = targets[i].position - worldCenter;

            float x = Vector3.Dot(offset, camRight);
            float y = Vector3.Dot(offset, camUp);

            maxAbsX = Mathf.Max(maxAbsX, Mathf.Abs(x));
            maxAbsY = Mathf.Max(maxAbsY, Mathf.Abs(y));
        }

        maxAbsX += padding;
        maxAbsY += padding;

        // 縦半分 = orthographicSize
        // 横半分 = orthographicSize * aspect
        float sizeFromHeight = maxAbsY;
        float sizeFromWidth = maxAbsX / cam.aspect;

        return Mathf.Max(sizeFromHeight, sizeFromWidth);
    }

    private Vector3 GetCameraPositionFromCenter(Vector3 worldCenter)
    {
        // 現在のカメラ位置から、中心点までの「前方向距離」を保つ
        // 斜め40度でも回転済みカメラなら自然に追従しやすい
        Vector3 forward = transform.forward;

        return worldCenter + distance;
    }

    private Vector3 ClampCameraPosition(Vector3 desiredPos, float orthoSize)
    {
        // 地面(XZ平面)との交点ベースで clamp
        // 画面四隅を地面に投影し、その半径分だけ中心の可動範囲を狭める

        Plane ground = new Plane(Vector3.up, Vector3.zero);

        Vector3[] viewportCorners = new Vector3[]
        {
            new Vector3(0f, 0f, 0f),
            new Vector3(0f, 1f, 0f),
            new Vector3(1f, 0f, 0f),
            new Vector3(1f, 1f, 0f),
        };

        float minXOffset = float.PositiveInfinity;
        float maxXOffset = float.NegativeInfinity;
        float minZOffset = float.PositiveInfinity;
        float maxZOffset = float.NegativeInfinity;

        Vector3 originalPos = transform.position;
        float originalSize = cam.orthographicSize;

        // 仮に desiredPos / orthoSize の状態で四隅が地面のどこを見るか調べる
        transform.position = desiredPos;
        cam.orthographicSize = orthoSize;

        Vector3 screenCenterGround = RayToGround(cam.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f)), ground);

        for (int i = 0; i < viewportCorners.Length; i++)
        {
            Vector3 cornerGround = RayToGround(cam.ViewportPointToRay(viewportCorners[i]), ground);
            Vector3 delta = cornerGround - screenCenterGround;

            minXOffset = Mathf.Min(minXOffset, delta.x);
            maxXOffset = Mathf.Max(maxXOffset, delta.x);
            minZOffset = Mathf.Min(minZOffset, delta.z);
            maxZOffset = Mathf.Max(maxZOffset, delta.z);
        }

        // 元に戻す
        transform.position = originalPos;
        cam.orthographicSize = originalSize;

        float minCenterX = mapMinXZ.x - minXOffset;
        float maxCenterX = mapMaxXZ.x - maxXOffset;
        float minCenterZ = mapMinXZ.y - minZOffset;
        float maxCenterZ = mapMaxXZ.y - maxZOffset;

        Vector3 clamped = desiredPos;

        if (minCenterX <= maxCenterX)
            clamped.x = Mathf.Clamp(clamped.x, minCenterX, maxCenterX);
        else
            clamped.x = (mapMinXZ.x + mapMaxXZ.x) * 0.5f;

        if (minCenterZ <= maxCenterZ)
            clamped.z = Mathf.Clamp(clamped.z, minCenterZ, maxCenterZ);
        else
            clamped.z = (mapMinXZ.y + mapMaxXZ.y) * 0.5f;

        return clamped;
    }

    private Vector3 RayToGround(Ray ray, Plane ground)
    {
        if (ground.Raycast(ray, out float enter))
            return ray.GetPoint(enter);

        return Vector3.zero;
    }
}