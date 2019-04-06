using System;
using UnityEngine;
using UnityEngine.Rendering;
using Object = UnityEngine.Object;

namespace SDFr
{
    public abstract class AVolumePreview<Tvolume,Tdata> : IDisposable 
        where Tvolume : AVolume<Tvolume>, new()
        where Tdata : AVolumeData
    {        
        protected Tvolume _volume;
        protected CommandBuffer _cmd;
        protected MaterialPropertyBlock _props;
        protected Material _material;
        protected Tdata _data;
        private bool _disposing;
        
        
        
        /// <summary>
        /// Creates a volume preview following the transform
        /// </summary>
        /// <param name="data"></param>
        /// <param name="shader"></param>
        /// <param name="transform"></param>
        public AVolumePreview( Tvolume volume, Tdata data, Shader shader )
        {
            if (volume == null || data == null || shader == null)
            {
                Dispose();
                return;
            }

            _volume = volume;
            _data = data;
            _cmd = new CommandBuffer {name = "["+_data.GetType()+"]" + _data.name};
            _props = new MaterialPropertyBlock();
            _material = new Material(shader);
        }

        protected AVolumePreview()
        {
        }

        //public abstract void Draw();
        
        #region IDisposable

        private void ReleaseUnmanagedResources()
        {
            // TODO release unmanaged resources here
        }

        protected virtual void Dispose(bool disposing)
        {
            ReleaseUnmanagedResources();
            if (disposing)
            {
                _cmd?.Dispose();
                _props = null;
                _data = null;
                if (_material != null)
                {
                    Object.DestroyImmediate(_material);
                } 
            }
        }

        ~AVolumePreview()
        {
            Dispose(false);
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        #endregion
    }
}