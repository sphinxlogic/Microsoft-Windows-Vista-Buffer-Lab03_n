// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: ManifestResourceInfo 
**
** 
** Purpose: For info regarding a manifest resource's topology.
**
**
=============================================================================*/ 

namespace System.Reflection { 
    using System; 

[System.Runtime.InteropServices.ComVisible(true)] 
    public class ManifestResourceInfo {
        private Assembly _containingAssembly;
        private String _containingFileName;
        private ResourceLocation _resourceLocation; 

        internal ManifestResourceInfo(Assembly containingAssembly, 
                                      String containingFileName, 
                                      ResourceLocation resourceLocation)
        { 
            _containingAssembly = containingAssembly;
            _containingFileName = containingFileName;
            _resourceLocation = resourceLocation;
        } 

        public virtual Assembly ReferencedAssembly 
        { 
            get {
                return _containingAssembly; 
            }
        }

        public virtual String FileName 
        {
            get { 
                return _containingFileName; 
            }
        } 

        public virtual ResourceLocation ResourceLocation
        {
            get { 
                return _resourceLocation;
            } 
        } 
    }
 
    // The ResourceLocation is a combination of these flags, set or not.
    // Linked means not Embedded.
    [Flags, Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public enum ResourceLocation
    { 
        Embedded = 0x1, 
        ContainedInAnotherAssembly = 0x2,
        ContainedInManifestFile = 0x4 
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*==============================================================================
** 
** Class: ManifestResourceInfo 
**
** 
** Purpose: For info regarding a manifest resource's topology.
**
**
=============================================================================*/ 

namespace System.Reflection { 
    using System; 

[System.Runtime.InteropServices.ComVisible(true)] 
    public class ManifestResourceInfo {
        private Assembly _containingAssembly;
        private String _containingFileName;
        private ResourceLocation _resourceLocation; 

        internal ManifestResourceInfo(Assembly containingAssembly, 
                                      String containingFileName, 
                                      ResourceLocation resourceLocation)
        { 
            _containingAssembly = containingAssembly;
            _containingFileName = containingFileName;
            _resourceLocation = resourceLocation;
        } 

        public virtual Assembly ReferencedAssembly 
        { 
            get {
                return _containingAssembly; 
            }
        }

        public virtual String FileName 
        {
            get { 
                return _containingFileName; 
            }
        } 

        public virtual ResourceLocation ResourceLocation
        {
            get { 
                return _resourceLocation;
            } 
        } 
    }
 
    // The ResourceLocation is a combination of these flags, set or not.
    // Linked means not Embedded.
    [Flags, Serializable]
[System.Runtime.InteropServices.ComVisible(true)] 
    public enum ResourceLocation
    { 
        Embedded = 0x1, 
        ContainedInAnotherAssembly = 0x2,
        ContainedInManifestFile = 0x4 
    }
}
