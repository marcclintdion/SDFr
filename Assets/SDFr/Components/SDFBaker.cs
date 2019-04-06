using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace SDFr
{
    [ExecuteInEditMode] //required for previewing
    public class SDFBaker : AVolumeBaker<SDFVolume,SDFData>
    {
        
        [SerializeField] private int raySamples = 256;
        [SerializeField] private int jitterSeed = 555;
        [SerializeField] private float jitterScale = 0.75f;
        [SerializeField] private bool sdfFlip; //invert sign of preview
        [SerializeField] private float previewEpsilon = 0.003f;
        [SerializeField] private float previewNormalDelta = 0.03f;
        
        [SerializeField] private SDFData sdfData;
        [SerializeField] private Texture3D debugTex3D; //for viewing existing texture3D not baked with SDFr
        
#if UNITY_EDITOR
        
        private const string _sdfPreviewShaderName = "XRA/SDFr";
        private static Shader _shader; //TODO better way 
        
        protected override void OnDrawGizmos()
        {
            if (aVolume == null || !IsPreviewing) return;

            base.OnDrawGizmos();
        }

        public override AVolumePreview<SDFVolume, SDFData> CreatePreview()
        {
            if (_shader == null)
            {
                _shader = Shader.Find(_sdfPreviewShaderName);
            }

            AVolumePreview<SDFVolume,SDFData> sdf = new SDFPreview(aVolume, sdfData, _shader);
            if (debugTex3D != null)
            {
                (sdf as SDFPreview).debugTex3D = debugTex3D;
            }

            return sdf;
        }

        /// <summary>
        /// renders a procedural quad
        /// TODO fit the bounds of the SDF Volume
        /// </summary>
        private void OnRenderObject()
        {
            if (!IsPreviewing) return;
            
            //try to get active camera...
            Camera cam = Camera.main;
            
            if (UnityEditor.SceneView.lastActiveSceneView != null)
            {
                cam = UnityEditor.SceneView.lastActiveSceneView.camera;
            }
            SDFPreview preview = _aPreview as SDFPreview;
            
            preview?.Draw(cam,sdfFlip,previewEpsilon,previewNormalDelta);
        }
        
        public override void Bake()
        {
            if (bakedRenderers == null)
            {
                bakedRenderers = new List<Renderer>();
            }
            bakedRenderers.Clear();
            
            //first check if any objects are parented to this object
            //if anything is found, try to use renderers from those instead of volume overlap
            if (!GetMeshRenderersInChildren(ref bakedRenderers))
            {
                //otherwise try to get renderers intersecting the volume
                //get mesh renderers within volume
                if (!GetMeshRenderersIntersectingVolume(ref bakedRenderers))
                {
                    //TODO display error?
                    return;
                }
            }

            aVolume?.Bake( raySamples, bakedRenderers, BakeComplete );
        }
        
        //TODO improve asset saving 
        private void BakeComplete( float[] distances, float maxDistance, string path = "" )
        {
            if (sdfData != null)
            {
                //use path of existing sdfData
                path = AssetDatabase.GetAssetPath(sdfData);
                
                //check if asset at path
                Object obj = AssetDatabase.LoadAssetAtPath<SDFData>(path);
                if (obj != null)
                {
                    if (((SDFData) obj).sdfTexture != null)
                    {
                        //destroy old texture
                        Object.DestroyImmediate(((SDFData) obj).sdfTexture,true);
                    }
                    //destroy old asset
                    //TODO this will break references...
                    Object.DestroyImmediate(obj,true);
                }
            }
            
            //create new SDFData
            sdfData = ScriptableObject.CreateInstance<SDFData>();
            sdfData.bounds = aVolume.BoundsLocalAABB;
            sdfData.voxelSize = aVolume.voxelSize;

            float minAxis = Mathf.Min( sdfData.bounds.size.x, Mathf.Min( sdfData.bounds.size.y, sdfData.bounds.size.z ) );
            sdfData.nonUniformScale = new Vector3( sdfData.bounds.size.x/minAxis, sdfData.bounds.size.y/minAxis, sdfData.bounds.size.z/minAxis );

            Texture3D newTex = new Texture3D(
                aVolume.Dimensions.x, aVolume.Dimensions.y, aVolume.Dimensions.z,
                TextureFormat.RHalf, false);

            
            //TODO improve
            Color[] colorBuffer = new Color[distances.Length];
            for (int i = 0; i < distances.Length; i++)
            {
                //NOTE for compatibility with Visual Effect Graph, 
                //the distance must be negative inside surfaces.
                //normalize the distance for better support of scaling bounds
                //Max Distance is always the Magnitude of the baked bound size
                float normalizedDistance = distances[i] / maxDistance;
                colorBuffer[i] = new Color(normalizedDistance,0f,0f,0f);
            }
            newTex.SetPixels(colorBuffer);
            newTex.Apply();

            if (string.IsNullOrEmpty(path))
            {
                //ask for path
                path = EditorUtility.SaveFilePanelInProject("Save As...", "sdfData", "asset", "Save the SDF Data");
            }

            if (string.IsNullOrEmpty(path))
            {
                if (EditorUtility.DisplayDialog("Error", "Path was invalid, retry?", "ok", "cancel"))
                {
                    path = EditorUtility.SaveFilePanelInProject("Save As...", "sdfData", "asset", "Save the SDF Data");
                }

                if (string.IsNullOrEmpty(path))
                {
                    return;
                }
            }

            sdfData.sdfTexture = newTex;
            sdfData.maxDistance = maxDistance;
            
            EditorUtility.SetDirty(sdfData);
                        
            //create it
            AssetDatabase.CreateAsset(sdfData, path);
            AssetDatabase.AddObjectToAsset(newTex,sdfData);
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            //TODO fix
            
            if (IsPreviewing)
                TogglePreview();
            if (!IsPreviewing ) 
                TogglePreview();
            HiddenRenderers = true;
            ToggleBakedRenderers();
        }
#endif
    }
}