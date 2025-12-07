// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
/*============================================================
** 
** Class:  NeutralResourcesLanguageAttribute 
**
** 
** Purpose: Tells the ResourceManager what language your main
**          assembly's resources are written in.  The
**          ResourceManager won't try loading a satellite
**          assembly for that culture, which helps perf. 
**
** 
===========================================================*/ 

using System; 

namespace System.Resources {

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false)] 
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class NeutralResourcesLanguageAttribute : Attribute 
    { 
        private String _culture;
        private UltimateResourceFallbackLocation _fallbackLoc; 

        public NeutralResourcesLanguageAttribute(String cultureName)
        {
            if (cultureName == null) 
                throw new ArgumentNullException("cultureName");
 
            _culture = cultureName; 
            _fallbackLoc = UltimateResourceFallbackLocation.MainAssembly;
        } 

        public NeutralResourcesLanguageAttribute(String cultureName, UltimateResourceFallbackLocation location)
        {
            if (cultureName == null) 
                throw new ArgumentNullException("cultureName");
            if (!Enum.IsDefined(typeof(UltimateResourceFallbackLocation), location)) 
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidNeutralResourcesLanguage_FallbackLoc", location)); 

            _culture = cultureName; 
            _fallbackLoc = location;
        }

        public String CultureName { 
            get { return _culture; }
        } 
 
        public UltimateResourceFallbackLocation Location {
            get { return _fallbackLoc; } 
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
** Class:  NeutralResourcesLanguageAttribute 
**
** 
** Purpose: Tells the ResourceManager what language your main
**          assembly's resources are written in.  The
**          ResourceManager won't try loading a satellite
**          assembly for that culture, which helps perf. 
**
** 
===========================================================*/ 

using System; 

namespace System.Resources {

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false)] 
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class NeutralResourcesLanguageAttribute : Attribute 
    { 
        private String _culture;
        private UltimateResourceFallbackLocation _fallbackLoc; 

        public NeutralResourcesLanguageAttribute(String cultureName)
        {
            if (cultureName == null) 
                throw new ArgumentNullException("cultureName");
 
            _culture = cultureName; 
            _fallbackLoc = UltimateResourceFallbackLocation.MainAssembly;
        } 

        public NeutralResourcesLanguageAttribute(String cultureName, UltimateResourceFallbackLocation location)
        {
            if (cultureName == null) 
                throw new ArgumentNullException("cultureName");
            if (!Enum.IsDefined(typeof(UltimateResourceFallbackLocation), location)) 
                throw new ArgumentException(Environment.GetResourceString("Arg_InvalidNeutralResourcesLanguage_FallbackLoc", location)); 

            _culture = cultureName; 
            _fallbackLoc = location;
        }

        public String CultureName { 
            get { return _culture; }
        } 
 
        public UltimateResourceFallbackLocation Location {
            get { return _fallbackLoc; } 
        }
    }
}
