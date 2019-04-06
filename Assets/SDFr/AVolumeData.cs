using UnityEngine;

namespace SDFr
{
    public struct AVolumeSettings
    {
        public Vector3Int Dimensions;
        public Bounds Bounds;
    }
    
    public abstract class AVolumeData : ScriptableObject
    {
        public Vector3Int dimensions;
        public Bounds bounds;
        public Vector3 voxelSize;
        public Vector3 nonUniformScale;
    }
}