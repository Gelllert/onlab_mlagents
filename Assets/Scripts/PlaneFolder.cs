using UnityEngine;

public class PlaneFolder
{
    private SkinnedMeshRenderer _skinnedMesh;
    private int _blendShapeIndex = 0;
    private bool _animated = true;
    private float _progress = 0f;

    public PlaneFolder(bool animated = true)
    {
        _animated = animated;
    }

    public void Setup(GameObject planePrefab)
    {
        _skinnedMesh = planePrefab.GetComponentInChildren<SkinnedMeshRenderer>();
        
        if (_skinnedMesh != null && _skinnedMesh.sharedMesh.blendShapeCount > 0)
        {
            SetProgress(_progress);
        }
    }

    public void SetProgress(float progress)
    {
        if(!_animated) return;


        if(_skinnedMesh != null && _skinnedMesh.sharedMesh.blendShapeCount > 0)
        {
            _progress = Mathf.Clamp01(progress);
            _skinnedMesh.SetBlendShapeWeight(_blendShapeIndex, _progress * 100f);
        }

    }

    public float GetProgress()
    {
        return _progress;
    }
}