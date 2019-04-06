using System.Collections.Generic;
using UnityEngine;

namespace SDFr
{
    public abstract class AVolumeBaker<T,D> : MonoBehaviour 
        where T : AVolume<T>, new()
        where D : AVolumeData
    {
        [SerializeField]
        protected Bounds bounds;
        [SerializeField]
        protected Vector3Int dimensions = new Vector3Int(32,32,32);
        [SerializeField] private bool fitToVertices = true;
        [SerializeField] 
        protected List<Renderer> bakedRenderers;

        protected T aVolume;
        
        public bool HasAVolume => aVolume != null;

        public abstract AVolumePreview<T, D> CreatePreview();
        
        public void TogglePreview()
        {
            if (aVolume == null) return;
            
            if (_aPreview == null)
            {
                _aPreview = CreatePreview();
            }
            else
            {
                _aPreview?.Dispose();
                _aPreview = null;
                if (HiddenRenderers)
                {
                    HiddenRenderers = false;
                    ToggleBakedRenderers();
                }
            }
        }
        
        public void ToggleBakedRenderers()
        {
            if (bakedRenderers == null || bakedRenderers.Count == 0) return;
            HiddenRenderers = !HiddenRenderers;
            foreach (var r in bakedRenderers)
            {
                if ( r.enabled != HiddenRenderers ) r.enabled = HiddenRenderers;
            }
        }
        
        protected bool GetMeshRenderersInChildren(ref List<Renderer> renderers )
        {
            if (renderers == null) return false;
            
            //get renderers in children
            Renderer[] mrs = gameObject.GetComponentsInChildren<Renderer>();
            if (mrs == null || mrs.Length == 0) return false;

            Bounds newBounds = new Bounds(transform.position,Vector3.zero);
            
            foreach (var r in mrs)
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                //skip inactive renderers
                if (!r.gameObject.activeSelf) continue;
                if (!r.enabled) continue;

                if (!fitToVertices)
                {
                    newBounds.Encapsulate(r.bounds);
                }
                else
                {
                    //iterate all vertices and encapsulate
                    Mesh mesh = null;
                    if (r is MeshRenderer)
                    {
                        MeshFilter mf = r.GetComponent<MeshFilter>();
                        if (mf != null && mf.sharedMesh != null)
                        {
                            mesh = mf.sharedMesh;
                        }
                    }
                    else
                    {
                        mesh = (r as SkinnedMeshRenderer).sharedMesh;
                    }

                    if (mesh != null)
                    {
                        EncapsulateVertices(ref newBounds, mesh, r.localToWorldMatrix);
                    }
                }

                renderers.Add(r);
            }
            
            if (renderers.Count == 0)
            {
                return false;
            }

            //assign new bounds
            //remove the world offset            
            newBounds = new Bounds(newBounds.center - transform.position, newBounds.size);
            
            bounds = aVolume.UpdateBounds(newBounds,true);

            return true;
        }
        
        protected bool GetMeshRenderersIntersectingVolume( ref List<Renderer> renderers )
        {
            if (renderers == null) return false;
            
            Renderer[] mrs = FindObjectsOfType<Renderer>();
            if (mrs == null || mrs.Length == 0)
            {
                return false;
            }

            foreach (var r in mrs)
            {             
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                //skip inactive renderers
                if (!r.gameObject.activeSelf) continue;
                if (!r.enabled) continue;
                if (!aVolume.BoundsWorldAABB.Intersects(r.bounds)) continue;
                renderers.Add(r);
            }

            if (renderers.Count == 0)
            {
                return false;
            }

            return true;
        }

        protected void EncapsulateVertices(ref Bounds newBounds, Mesh mesh, Matrix4x4 localToWorld )
        {
            Vector3[] vertices = new Vector3[mesh.vertexCount];
            mesh.vertices.CopyTo(vertices,0);
            foreach (Vector3 v in vertices)
            {
                newBounds.Encapsulate(localToWorld.MultiplyPoint3x4(v));
            }
        }
        
#if UNITY_EDITOR
        private static Color colorPreviewBounds = new Color(0.5f,1f,0.5f,0.5f);
    
        protected AVolumePreview<T,D> _aPreview;
        public bool IsPreviewing => _aPreview != null;
        public bool HiddenRenderers { get; protected set; }
        
        private void OnValidate()
        {
            //need to ensure that bounds are not zero, and dimensions are at least something reasonable
            if (bounds.extents.x < float.Epsilon) bounds.extents = new Vector3(float.Epsilon,bounds.extents.y,bounds.extents.z);
            if (bounds.extents.y < float.Epsilon) bounds.extents = new Vector3(bounds.extents.x,float.Epsilon,bounds.extents.z);
            if (bounds.extents.z < float.Epsilon) bounds.extents = new Vector3(bounds.extents.x,bounds.extents.y,float.Epsilon);;

            dimensions.x = Mathf.Clamp(dimensions.x, 1, 256);
            dimensions.y = Mathf.Clamp(dimensions.y, 1, 256);
            dimensions.z = Mathf.Clamp(dimensions.z, 1, 256);
        }
        
        public virtual void EditorCreateAVolume()
        {
            EditorDestroyAVolume();

            AVolumeSettings settings = new AVolumeSettings
            {
                Bounds = bounds, Dimensions = dimensions
            };
        
            //create new
            aVolume = AVolume<T>.CreateVolume(transform,settings);
        }
        
        public virtual void EditorDestroyAVolume()
        {
            //cleanup existing
            aVolume?.Dispose();
        }

        public abstract void Bake();

        protected virtual void OnDrawGizmos()
        {
            if (aVolume == null ) return;
        
            //draw bounds to indicate preview
            //ignore scale of transform
            Gizmos.matrix = Matrix4x4.TRS(aVolume.Transform.position, aVolume.Transform.rotation, Vector3.one);
            Gizmos.color = colorPreviewBounds;
            Gizmos.DrawWireCube(aVolume.BoundsLocalAABB.center, aVolume.BoundsLocalAABB.size);
        }
#endif
        
    }
}