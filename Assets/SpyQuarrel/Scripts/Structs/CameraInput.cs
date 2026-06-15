using UnityEngine;

namespace SpyQuarrelRuntime
{ 
    public struct CameraInput 
    { 
        public Vector2 Look {get => _lookInput; private set => _lookInput = value; } 
        
        private Vector2 _lookInput; 
        
        public CameraInput(Vector2 lookInput) 
        { 
            _lookInput = lookInput; 
        } 
    }
}
