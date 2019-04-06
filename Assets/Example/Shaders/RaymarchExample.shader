Shader "Custom/RaymarchExample"
{
    Properties
    {
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert_proc_quad
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            #include "Assets/SDFr/Shaders/SDFrProcedural.hlsl"
            #include "Assets/SDFr/Shaders/SDFrVolumeTex.hlsl"

            #define MAX_STEPS 512
            #define EPSILON 0.003
            #define NORMAL_DELTA 0.03
            
            uniform float4x4 _PixelCoordToViewDirWS;
            
            Texture3D _VolumeATex;
            Texture3D _VolumeBTex;
            
            StructuredBuffer<SDFrVolumeData> _VolumeBuffer;
            float4 _Sphere;
            float4 _Box;
            
            float sdSphere( float3 p, float s )
            {
                return length(p)-s;
            }
            
            float sdBox( float3 p, float3 b )
            {
              float3 d = abs(p) - b;
              return min(max(d.x,max(d.y,d.z)),0.0) + length(max(d,0.0));
            }
                        
            float DistanceFunction( float3 rayPos, float3 rayOrigin, float3 rayEnd )
            {
                float sphere = sdSphere(rayPos-_Sphere.xyz,_Sphere.w);
                float box = sdBox(rayPos-_Box.xyz,float3(1,1,1)*_Box.w);
                
                float d = min(box,sphere);
                          
                float ad = DistanceFunctionTex3D( rayPos, 0, rayOrigin, rayEnd, _VolumeBuffer[0], _VolumeATex );
                d = min(ad,d);
                
                float bd = DistanceFunctionTex3D( rayPos, 0, rayOrigin, rayEnd, _VolumeBuffer[1], _VolumeBTex );
                d = min(bd,d);
                
                return d;
            }
            
            half4 frag (Varyings_Proc input) : SV_Target
            {
                //ray origin
                float3 ro = _WorldSpaceCameraPos;
                //ray from camera to pixel coordinates in world space
                float3 rd = -normalize(mul(float3(input.positionCS.xy, 1.0), (float3x3)_PixelCoordToViewDirWS));                
                float3 re = ro + rd * _ProjectionParams.z;
            
                float dist = 0;
                
                for( int s=0; s<MAX_STEPS; s++)
                {
                    float3 rayPos = ro + rd * dist;
                                        
                    float d = DistanceFunction(rayPos,ro,re); 
                    
                    if ( d < EPSILON )
                    {
                        //fast normal
                        float3 nx = rayPos + float3(NORMAL_DELTA,0,0);
                        float3 ny = rayPos + float3(0,NORMAL_DELTA,0);
                        float3 nz = rayPos + float3(0,0,NORMAL_DELTA);
                        float dx = DistanceFunction(nx,ro,re)-d;
                        float dy = DistanceFunction(ny,ro,re)-d;
                        float dz = DistanceFunction(nz,ro,re)-d;
                        float3 normalWS = normalize(float3(dx,dy,dz));
                    
                        //TODO lighting 
                    
                        return half4(normalWS,1);
                    }
                    dist += d;
                }
                
                return half4(0.2,0.2,0.2,1);
            }
            ENDCG
        }
    }
}