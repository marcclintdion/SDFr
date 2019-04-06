using SDFr;
using UnityEditor;
using UnityEngine;

namespace SDFr.Editor
{
    //TODO add tooltips, move bake / preview to base inspector 
    [CustomEditor(typeof(SDFBaker))]
    public class SDFBakerInspector : AVolumeBakerInspector<SDFBaker,SDFVolume,SDFData>
    {
        private SerializedProperty fitToVerticesProperty;
        private SerializedProperty raySamplesProperty;
        private SerializedProperty sdfDataProperty;
        private SerializedProperty sdfFlipProperty;
        private SerializedProperty jitterSeedProperty;
        private SerializedProperty jitterScaleProperty;
        private SerializedProperty epsilonProperty;
        private SerializedProperty normalDeltaProperty;

        private const string strPropFitToVertices = "fitToVertices";
        private const string strPropRaySamples = "raySamples";
        private const string strPropSdfData = "sdfData";
        private const string strPropSdfFlip = "sdfFlip";
        private const string strPropJitterSeed = "jitterSeed";
        private const string strPropJitterScale = "jitterScale";
        private const string strPropEpsilon = "previewEpsilon";
        private const string strPropNormalDelta = "previewNormalDelta";
        
        protected override void CollectSerializedProperties()
        {
            base.CollectSerializedProperties();
            fitToVerticesProperty = serializedObject.FindProperty(strPropFitToVertices);
            raySamplesProperty = serializedObject.FindProperty(strPropRaySamples);
            sdfDataProperty = serializedObject.FindProperty(strPropSdfData);
            sdfFlipProperty = serializedObject.FindProperty(strPropSdfFlip);
            jitterSeedProperty = serializedObject.FindProperty(strPropJitterSeed);
            jitterScaleProperty = serializedObject.FindProperty(strPropJitterScale);
            epsilonProperty = serializedObject.FindProperty(strPropEpsilon);
            normalDeltaProperty = serializedObject.FindProperty(strPropNormalDelta);
        }

        protected override void OnDisable()
        {
            BoxBoundsHandle = null;
            SDFBaker baker = target as SDFBaker;
            if (baker == null) return;
            if (!baker.IsPreviewing) //only destroy if not previewing
            {
                baker.EditorDestroyAVolume();
            }
        }
        
        protected override void DrawVolumeGUI()
        {            
            SDFBaker baker = target as SDFBaker;
            if (baker == null) return;
            
            EditorGUI.BeginChangeCheck();
            
            //disable these when previewing to enforce idea that they are for baking
            EditorGUI.BeginDisabledGroup(baker.IsPreviewing);
            //dimensions
            EditorGUILayout.PropertyField(DimensionsProperty);
            //bounds
            EditorGUILayout.PropertyField(BoundsProperty);
            //ray samples
            EditorGUILayout.PropertyField(raySamplesProperty);
            //fit to vertices of mesh, if false the bounds will be used, bounds may be larger than mesh and waste space
            EditorGUILayout.PropertyField(fitToVerticesProperty);
            //jitter seed 
            //EditorGUILayout.PropertyField(jitterSeedProperty);
            //jitter scale (0.75 to 1.0 seems good)
            //EditorGUILayout.PropertyField(jitterScaleProperty);
            EditorGUI.EndDisabledGroup();

            //disable these when not previewing 
            EditorGUI.BeginDisabledGroup(!baker.IsPreviewing);
            EditorGUILayout.Slider(epsilonProperty, 0.0001f, 0.1f);
            EditorGUILayout.Slider(normalDeltaProperty, 0.0001f, 0.5f);
            //inverse sign of SDF preview
            EditorGUILayout.PropertyField(sdfFlipProperty);
            
            //toggle world space normals & steps 
            if (GUILayout.Button("Toggle Shading"))
            {
                if (Shader.IsKeywordEnabled("SDFr_VISUALIZE_STEPS"))
                {
                    Shader.DisableKeyword("SDFr_VISUALIZE_STEPS");
                }
                else
                {
                    Shader.EnableKeyword("SDFr_VISUALIZE_STEPS");
                }
                SceneView.RepaintAll();
            }
            
            EditorGUI.EndDisabledGroup();
            
            //TODO if SDF Data assigned via drag & drop, adjust the bounds and settings to match SDF data
            EditorGUILayout.PropertyField(sdfDataProperty);

            BakeControls();
        }
    }
}