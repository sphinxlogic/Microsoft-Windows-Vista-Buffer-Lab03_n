// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SatelliteContractVersionAttribute 
**
** 
** Purpose: Specifies which version of a satellite assembly
**          the ResourceManager should ask for.
**
** 
===========================================================*/
 
using System; 

namespace System.Resources { 

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class SatelliteContractVersionAttribute : Attribute 
    {
        private String _version; 
 
        public SatelliteContractVersionAttribute(String version)
        { 
            if (version == null)
                throw new ArgumentNullException("version");
            _version = version;
        } 

        public String Version { 
            get { return _version; } 
        }
    } 
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  SatelliteContractVersionAttribute 
**
** 
** Purpose: Specifies which version of a satellite assembly
**          the ResourceManager should ask for.
**
** 
===========================================================*/
 
using System; 

namespace System.Resources { 

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class SatelliteContractVersionAttribute : Attribute 
    {
        private String _version; 
 
        public SatelliteContractVersionAttribute(String version)
        { 
            if (version == null)
                throw new ArgumentNullException("version");
            _version = version;
        } 

        public String Version { 
            get { return _version; } 
        }
    } 
}
