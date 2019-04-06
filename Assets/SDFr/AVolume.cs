using System;
using UnityEngine;

namespace SDFr
{
    /// <summary>
    /// Atlassed Volume
    /// </summary>
    public abstract class AVolume<T> : IDisposable where T : AVolume<T>, new()
    {
        public static T CreateVolume( Transform transform, AVolumeSettings settings )
        {
            T v = new T();
            v.Initialize(transform, settings);
            return v;
        }

        public Transform Transform
        {
            get => _transform;
            set => _transform = value;
        }

        public Bounds BoundsLocalAABB
        {
            get => boundsLocalAabb;
            set => boundsLocalAabb = value;
        }

        public Bounds BoundsWorldAABB { get; private set; }
        public Vector3 voxelSize { get; private set; }
        
        protected Transform _transform;
        protected Bounds boundsLocalAabb;
        protected Vector4 _dimensions;
        protected int _maxDimension; //largest dimension component
        public Vector3Int Dimensions { get; private set; }
        protected int _cellCount;
        protected Vector3 _halfVoxel;

        public Matrix4x4 LocalToWorldNoScale
        {
            get
            {
                if ( _transform == null ) return Matrix4x4.identity;
                return Matrix4x4.TRS(
                    _transform.position + boundsLocalAabb.center,
                    _transform.rotation,
                    Vector3.one);
            }
        }
        
        public Matrix4x4 LocalToWorld
        {
            get
            {
                if ( _transform == null ) return Matrix4x4.identity;
                
                return Matrix4x4.TRS(
                    _transform.position + boundsLocalAabb.center,
                    _transform.rotation,
                    _transform.localScale);
            }
        }

        public Matrix4x4 WorldToLocal => LocalToWorld.inverse;

        protected virtual void Initialize(Transform transform, AVolumeSettings settings)
        {
            _transform = transform;
            _dimensions = new Vector4(settings.Dimensions.x,settings.Dimensions.y,settings.Dimensions.z,1f);
            Dimensions = settings.Dimensions;
            _cellCount = Dimensions.x * Dimensions.y * Dimensions.z;
            //dimensions x y z is resolution (cells) on x y z
            _maxDimension = (int)Mathf.Max(_dimensions.x, _dimensions.y, _dimensions.z);

            UpdateBounds(settings.Bounds);
        }

        public Bounds UpdateBounds( Bounds newBounds, bool addBorder = false )
        {
            boundsLocalAabb = newBounds;
            BoundsWorldAABB = new Bounds(_transform.position+boundsLocalAabb.center,boundsLocalAabb.size);
            voxelSize = new Vector3(
                boundsLocalAabb.size.x/_dimensions.x,
                boundsLocalAabb.size.y/_dimensions.y,
                boundsLocalAabb.size.z/_dimensions.z);
            _halfVoxel = voxelSize * 0.5f;

            if (!addBorder) return boundsLocalAabb;
            
            //divide current by dimensions-2 so that it gets the voxel size without the borders
            //then add the size to the bounds
                
            Vector3 extraBorderVoxelSize = new Vector3(
                boundsLocalAabb.size.x/(_dimensions.x-2),
                boundsLocalAabb.size.y/(_dimensions.y-2),
                boundsLocalAabb.size.z/(_dimensions.z-2));
            
            boundsLocalAabb = new Bounds(boundsLocalAabb.center, boundsLocalAabb.size + extraBorderVoxelSize*2f);
            
            //and update again, without expanding border
            return UpdateBounds(boundsLocalAabb, false);
        }

        public Vector3Int FromIndex(int index)
        {
            int i = index;
            int zz = i / (Dimensions.x*Dimensions.y);
            i -= zz * Dimensions.x*Dimensions.y;
            int yy = i / Dimensions.x;
            int xx = i % Dimensions.x;
            return new Vector3Int(xx,yy,zz);
        }

        public Vector3 ToPositionWS(int index)
        {
            Vector3Int xyz = FromIndex(index);

            //0 to 1 normalized bound space
            Vector3 positionBS = new Vector3(
                xyz.x/_dimensions.x-0.5f,
                xyz.y/_dimensions.y-0.5f,
                xyz.z/_dimensions.z-0.5f
            );
            //scale by bound size
            positionBS = Vector3.Scale(positionBS, boundsLocalAabb.size);
            //transform by the local to world matrix
            //NOTE scale cannot be used here otherwise it would double scale the bounds
            //(which have already encapsulated renderers in world space)
            return LocalToWorldNoScale.MultiplyPoint3x4(positionBS);
        }
        
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}