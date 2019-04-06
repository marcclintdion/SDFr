using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace SDFr.Editor
{
    public abstract class AVolumeBakerInspector<T,V,D> : UnityEditor.Editor 
        where V : AVolume<V>, new()
        where T : AVolumeBaker<V,D>, new()
        where D : AVolumeData
    {
        //serialized properties
        protected SerializedProperty DimensionsProperty;
        protected SerializedProperty BoundsProperty;

        protected const string StrBake = "Bake";
        protected const string StrPreview = "Preview";
        protected const string StrEndPreview = "End Preview";
        protected const string StrHideRenderers = "Hide Baked Renderers";
        protected const string StrShowRenderers = "Show Baked Renderers";
        protected const string StrPropDimensions = "dimensions";
        protected const string StrPropBounds = "bounds";
        
        [SerializeField] protected Color ColorHandles = new Color(0.5f,1f,1f,1f);
        [SerializeField] protected Color ColorWires = new Color(0.5f,1f,0.5f,1f);
        
        protected BoxBoundsHandle BoxBoundsHandle;

        protected virtual void CollectSerializedProperties()
        {
            //collect serialized properties
            BoundsProperty = serializedObject.FindProperty(StrPropBounds);
            DimensionsProperty = serializedObject.FindProperty(StrPropDimensions);
        }
        
        protected virtual void OnEnable()
        {
            if ( BoxBoundsHandle == null ) BoxBoundsHandle = new BoxBoundsHandle();
        
            CollectSerializedProperties();
                
            //create editor AVolume
            T av = target as T;
            if (av == null) return;
            if (av.HasAVolume) return; 
            av.EditorCreateAVolume();
        }
        
        protected virtual void OnDisable()
        {
            BoxBoundsHandle = null;
            T av = target as T;
            if (av == null) return;
            av.EditorDestroyAVolume();
        }
        
        protected void RebuildAVolume()
        {
            T av = serializedObject.targetObject as T;
            if (av == null) return;
            //TODO should this update only the modified properties?
            //in most cases if bounds/dims change the volume needs rebuilding anyway...
            av.EditorDestroyAVolume();
            av.EditorCreateAVolume();
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            DrawVolumeGUI();
            
            if (serializedObject.ApplyModifiedProperties())
            {
                RebuildAVolume();
            }
        }
    
        protected virtual void DrawVolumeGUI()
        {
            //dimensions
            EditorGUILayout.PropertyField(DimensionsProperty);
            //bounds
            EditorGUILayout.PropertyField(BoundsProperty);
        }

        protected virtual void BakeControls()
        {
            T baker = target as T;
            if (baker == null) return;
            if (GUILayout.Button(StrBake))
            {
                RebuildAVolume();
                baker.Bake();
            }
    
            if (GUILayout.Button((baker.IsPreviewing) ? StrEndPreview : StrPreview))
            {
                baker.TogglePreview();
                SceneView.RepaintAll();
            }

            if (baker.IsPreviewing)
            {
                if (GUILayout.Button(baker.HiddenRenderers ? StrHideRenderers : StrShowRenderers))
                {
                    baker.ToggleBakedRenderers();
                }
            }
        }
        
        public void OnSceneGUI()
        {
            if (Selection.activeGameObject == null) return;

            DrawEditableBounds();
        }
        
        private void DrawEditableBounds()
        {
            //draw editable bounds
            EditorGUI.BeginChangeCheck();
        
            Transform st = Selection.activeGameObject.transform;
            
            //ignore scale of transform
            Matrix4x4 localToWorld = Matrix4x4.TRS(st.position, st.rotation, Vector3.one);

            Handles.matrix = localToWorld;
            BoxBoundsHandle.handleColor = ColorHandles;
            BoxBoundsHandle.wireframeColor = ColorWires;
            BoxBoundsHandle.center = BoundsProperty.boundsValue.center;
            BoxBoundsHandle.size = BoundsProperty.boundsValue.size;
            BoxBoundsHandle.DrawHandle();

            if (!EditorGUI.EndChangeCheck()) return;
        
            //update bounds
            BoundsProperty.boundsValue = new Bounds
            {
                center = BoxBoundsHandle.center,
                size = BoxBoundsHandle.size
            };

            if (serializedObject.ApplyModifiedProperties())
            {
                RebuildAVolume();
            }
        }
    }
}