using UnityEngine;

namespace SDFr
{
	public class SDFData : AVolumeData
	{
		public Texture3D sdfTexture;
		public float maxDistance;


		private static readonly int _SDFVolumeTex = Shader.PropertyToID("_SDFVolumeTex");
		private static readonly int _SDFVolumeExtents = Shader.PropertyToID("_SDFVolumeExtents");

		public void SetMaterialProperties(MaterialPropertyBlock props)
		{
			props.SetTexture(_SDFVolumeTex, sdfTexture);            
			props.SetVector(_SDFVolumeExtents, bounds.extents);
			//TODO apply atlas etc 
		}
		
		
	
#if UNITY_EDITOR
		//TODO improve 
		public float[] GetDistances()
		{
			if (sdfTexture == null) return null;

			Texture3D tex = sdfTexture;	
		
			Texture3D temp = new Texture3D(tex.width,tex.height,tex.depth,TextureFormat.RGBAHalf,false);
			temp.Apply();
		
			//TODO likely fails since src and dest are different formats
			Graphics.CopyTexture(tex,temp);
		
			Color[] colors = temp.GetPixels();
			float[] distances = new float[colors.Length];
			float maxDistance = bounds.size.magnitude;
			for( int i=0; i<colors.Length; i++ )
			{
				float val = colors[i].r;
				val = (val*2f)-1f;
				val *= maxDistance;
				distances[i] = val;
			}
		
			DestroyImmediate(temp);
		
			return distances;
		}
#endif
	}
}
