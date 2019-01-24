using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AssetGenerator
{
    

    public class Model
    {
        public Runtime.GLTF GLTF;
        public Action<glTFLoader.Schema.Gltf> PostRuntimeChanges = gltf => {};
        public Func<Type, object> CreateSchemaInstance = Activator.CreateInstance;
    }

 
}
