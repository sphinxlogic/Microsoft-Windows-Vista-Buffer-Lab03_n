//------------------------------------------------------------------------------ 
// <copyright file="assemblycache.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
//-----------------------------------------------------------------------------
//ThisAssembly class keeps a map of the following: 
//AssemblyName to AssemblyID
//TypeName to TypeID
//AssemblyID to AssemblyRef and State
//TypeID to TypeRef and AssemblyId 
//
//Adding an assembly to this class will NOT enable users to create types from that assembly. Users should explicitely add type details and link types to assemblies. 
// This class also registers for assembly resolve events so that dependent assemblies can be resolved if they are registered. 
//This class does NOT know anything about assembly dependencies. It simply loads assemblies as handed over to it.
//Users can take advantage of connection pooling by tying this instance to a pooling-aware component. 
//-----------------------------------------------------------------------------

using System;
using System.Collections; 
using System.Runtime.InteropServices;
using System.Reflection; 
using System.Globalization; 
using System.Diagnostics;
using System.Data.Sql; 
using System.Data.SqlTypes;
using System.IO;
using System.Security;
using System.Security.Policy; 
using System.Security.Permissions;
using System.Data.Common; 
 
using Microsoft.SqlServer.Server;
 
namespace System.Data.SqlClient {

#if WINFSFunctionality
    // 
    internal enum AssemblyState {NotFound = -1,NotTried,Failed,InteropFailed,Loaded};
 
    //expose these to managed clients, but hide them from native users 
    internal sealed class TypeInfo{
        internal int assemblyId; 
        internal String className;
        internal String typeName;
        //this field is false for all udts except for instantiated types.
        internal bool isInstantiated; 
        internal int genericType;//For WinFS, this is always MultiSet.We map this in GetUdtTypeGeneric
        internal int[] parameters;//these are the backend typeids of the template parameters of the instantiated type 
        internal Type typeRef;   //instantiated type. Cached when first created 
    }
 
    internal sealed class AssemblyInfo{
        internal AssemblyName assemblyName;
        internal Assembly assemblyRef;
        internal AssemblyState assemblyState; 
        internal int permissions;
    } 
#endif 

    internal sealed class AssemblyCache { 
#if WINFSFunctionality
        //maps typesname to corresponding structure that describes the type, TypeInfo.
        private Hashtable htTypeIdToInfo;
 
        //map assembly id to a struct that describes assemblyinfo
        private Hashtable htAssemblyIdToInfo; 
 
        //map (sql)typename to type id
        private Hashtable htTypeNameToId; 

        //instance of RegistrationServices
        [NonSerialized]
        private RegistrationServices comReg; 
#endif
 
 
#if WINFSFunctionality
        internal AssemblyCache() { 
            htTypeIdToInfo = new Hashtable();
            htAssemblyIdToInfo = new Hashtable();
            htTypeNameToId = new Hashtable();
 
            //Initialize the interop services object
            comReg = new RegistrationServices(); 
            //_evidence = null; 
#else
        private AssemblyCache() { /* prevent utility class from being insantiated*/ 
#endif
        }

#if WINFSFunctionality 
        static internal Int64 GetKey(int db, int id){
            return (Int64)(((UInt64)(UInt32)db) << 32 | ((UInt64)(UInt32)id)); 
        } 

        internal bool AddAssemblyToCache(int dbId, int id, AssemblyName name,int permissions){ 
            lock(this) {
                if(htAssemblyIdToInfo.ContainsKey(GetKey(dbId,id))){
                    return true;
                } 

                AssemblyInfo info = new AssemblyInfo(); 
                info.assemblyName = name; 
                info.assemblyState = AssemblyState.NotTried;
                info.permissions = permissions; 

                //add to the maps
                htAssemblyIdToInfo[GetKey(dbId,id)] = info;
            } 

            return true; 
        } 

        //get the type instance from its sql type id. 
        //If you are looking up by Id, then you know that you can add type details using AddTypeRefToCache, if doesnt exist
        internal Type GetTypeFromId(int dbId,int typeId){
            TypeInfo info = null;
            AssemblyInfo aInfo = null; 
            Type type = null;
 
                if(htTypeIdToInfo.ContainsKey(GetKey(dbId,typeId))) 
                    info = (TypeInfo)htTypeIdToInfo[GetKey(dbId,typeId)];
 
                if(null == info){
                    type =  null;
                }
                else { 
                    Debug.Assert((info.assemblyId != 0 || info.isInstantiated) && info.className != null);
                    if(htAssemblyIdToInfo.ContainsKey(GetKey(dbId,info.assemblyId))) 
                        aInfo = (AssemblyInfo)htAssemblyIdToInfo[GetKey(dbId,info.assemblyId)]; 

                    if (info.isInstantiated) { 
                        type = info.typeRef;
                    }
                    else if (null != aInfo && aInfo.assemblyRef != null) {
                        type =  aInfo.assemblyRef.GetType(info.className); 
                    }
                    else { 
                        type =  null; 
                    }
                } 

            return type;
        }
 
        internal bool AddTypeRefToCache(int dbId, int typeId, String typeName, String className, int assemblyId, bool multiValued) {
            // todo: multiValue flag not used yet 
 
            AssemblyInfo aInfo = null;
            TypeInfo info = null; 
            bool retval = false;

            // note that if multiValued == true assemblyId == 0 is ok
            if ((0 == assemblyId && !multiValued)) { 
                throw ADP.Argument(Res.GetString(Res.SqlUdtReason_MultivaluedAssemblyId));
            } 
            if (0 == typeId) { 
                throw ADP.ArgumentNull("typeId");
            } 
            ADP.CheckArgumentNull(typeName, "typeName");
            ADP.CheckArgumentNull(className, "className");

            //Is the assembly found? 
            if(false == htAssemblyIdToInfo.ContainsKey(GetKey(dbId,assemblyId))){
                retval =  false; 
            } 

            //already in the map? 
            if(true == htTypeIdToInfo.ContainsKey(GetKey(dbId,typeId))){
                retval =  true;
            }
 
            if (dbId != 0 && assemblyId != 0) {
                aInfo = (AssemblyInfo)htAssemblyIdToInfo[GetKey(dbId,assemblyId)]; 
 
                Debug.Assert(aInfo != null, "Maps are inconsistent. Assembly can not be located for this type");
            } 
            else {
                aInfo = null;
            }
 
            //fill the typeinfo. For MultiValued types, these may not make sense
            info = new TypeInfo(); 
            info.className = className; 
            info.assemblyId = assemblyId;
            info.typeName = typeName; 
            info.isInstantiated = multiValued;

            //add it to the type id map
            htTypeIdToInfo[GetKey(dbId,typeId)] = info; 
            htTypeNameToId[typeName] = GetKey(dbId,typeId);
            retval = true; 
 
            return retval;
        } 

        //
        internal bool FindAndLoadAssembly(int dbId,int aId,bool interop) {
            //find the assembly 
            AssemblyInfo aInfo = null;
            if(false == htAssemblyIdToInfo.ContainsKey(GetKey(dbId,aId))) 
                return false; 
            aInfo = (AssemblyInfo)htAssemblyIdToInfo[GetKey(dbId,aId)];
            Debug.Assert(aInfo != null, "ids not found in the map"); 

            if(AssemblyState.Loaded == aInfo.assemblyState) {
                Debug.Assert(aInfo.assemblyRef != null, "AssemblyState is inconsistent");
                return true; 
            }
            //if it failed before, 
            else if(aInfo.assemblyState == AssemblyState.Failed ||aInfo.assemblyState == AssemblyState.InteropFailed ) 
                return false;
            else { 
                aInfo.assemblyRef = Assembly.Load(aInfo.assemblyName);//, GetEvidence(aInfo.permissions));
                if(null != aInfo.assemblyRef)
                    aInfo.assemblyState = AssemblyState.Loaded;
                else 
                    return false;
            } 
 
            if(interop && false == comReg.RegisterAssembly(aInfo.assemblyRef,(AssemblyRegistrationFlags)0)){
                aInfo.assemblyState = AssemblyState.InteropFailed; 
                return false;
            }

            return (aInfo.assemblyRef != null); 
            //register the assembly for interop
        } 
 
        internal TypeInfo GetTypeInfo(int dbId, int typeId) {
            if(htTypeIdToInfo.ContainsKey(GetKey(dbId,typeId))) 
                return (TypeInfo)htTypeIdToInfo[GetKey(dbId,typeId)];
            else
                return null;
        } 

        internal AssemblyInfo GetAssemblyInfo(int dbId, int aId) { 
            if(htAssemblyIdToInfo.ContainsKey(GetKey(dbId,aId))) 
                return (AssemblyInfo)htAssemblyIdToInfo[GetKey(dbId,aId)];
            else 
                return null;
        }
#endif
 

#if WINFSFunctionality 
        internal static int GetLength(Object inst, bool isWinFS){ 
#else
        internal static int GetLength(Object inst){ 
#endif
            //caller should have allocated enough, based on MaxByteSize
            // NOTE: if we're talking to a winfs server we should be using
            // SerializationHelper.SizeInBytes defined in udtextensions.dll, 
            // note the one in system.data
#if WINFSFunctionality 
            if (isWinFS) 
                return SqlConnection.SizeInBytes(inst);
            else 
#endif
                return SerializationHelperSql9.SizeInBytes(inst);
        }
 
        //The attribute we are looking for is now moved to an external dll that server provides. If the name is changed.
        //then we we have to make corresponding changes here. 
        //please also change sqludcdatetime.cs, sqltime.cs and sqldate.cs 

        internal static SqlUdtInfo GetInfoFromType(Type t) { 
            Debug.Assert(t != null, "Type object cant be NULL");

            Type [....] = t;
            do { 
                SqlUdtInfo attr = SqlUdtInfo.TryGetFromType(t);
 
                if (attr != null ) { 
                    return attr;
                } 
                else {
                    t = t.BaseType;
                }
            } 
            while (t != null);
 
            throw SQL.UDTInvalidSqlType([....].AssemblyQualifiedName); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="assemblycache.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
// <owner current="true" primary="true">[....]</owner> 
// <owner current="true" primary="false">[....]</owner>
//----------------------------------------------------------------------------- 
 
//-----------------------------------------------------------------------------
//ThisAssembly class keeps a map of the following: 
//AssemblyName to AssemblyID
//TypeName to TypeID
//AssemblyID to AssemblyRef and State
//TypeID to TypeRef and AssemblyId 
//
//Adding an assembly to this class will NOT enable users to create types from that assembly. Users should explicitely add type details and link types to assemblies. 
// This class also registers for assembly resolve events so that dependent assemblies can be resolved if they are registered. 
//This class does NOT know anything about assembly dependencies. It simply loads assemblies as handed over to it.
//Users can take advantage of connection pooling by tying this instance to a pooling-aware component. 
//-----------------------------------------------------------------------------

using System;
using System.Collections; 
using System.Runtime.InteropServices;
using System.Reflection; 
using System.Globalization; 
using System.Diagnostics;
using System.Data.Sql; 
using System.Data.SqlTypes;
using System.IO;
using System.Security;
using System.Security.Policy; 
using System.Security.Permissions;
using System.Data.Common; 
 
using Microsoft.SqlServer.Server;
 
namespace System.Data.SqlClient {

#if WINFSFunctionality
    // 
    internal enum AssemblyState {NotFound = -1,NotTried,Failed,InteropFailed,Loaded};
 
    //expose these to managed clients, but hide them from native users 
    internal sealed class TypeInfo{
        internal int assemblyId; 
        internal String className;
        internal String typeName;
        //this field is false for all udts except for instantiated types.
        internal bool isInstantiated; 
        internal int genericType;//For WinFS, this is always MultiSet.We map this in GetUdtTypeGeneric
        internal int[] parameters;//these are the backend typeids of the template parameters of the instantiated type 
        internal Type typeRef;   //instantiated type. Cached when first created 
    }
 
    internal sealed class AssemblyInfo{
        internal AssemblyName assemblyName;
        internal Assembly assemblyRef;
        internal AssemblyState assemblyState; 
        internal int permissions;
    } 
#endif 

    internal sealed class AssemblyCache { 
#if WINFSFunctionality
        //maps typesname to corresponding structure that describes the type, TypeInfo.
        private Hashtable htTypeIdToInfo;
 
        //map assembly id to a struct that describes assemblyinfo
        private Hashtable htAssemblyIdToInfo; 
 
        //map (sql)typename to type id
        private Hashtable htTypeNameToId; 

        //instance of RegistrationServices
        [NonSerialized]
        private RegistrationServices comReg; 
#endif
 
 
#if WINFSFunctionality
        internal AssemblyCache() { 
            htTypeIdToInfo = new Hashtable();
            htAssemblyIdToInfo = new Hashtable();
            htTypeNameToId = new Hashtable();
 
            //Initialize the interop services object
            comReg = new RegistrationServices(); 
            //_evidence = null; 
#else
        private AssemblyCache() { /* prevent utility class from being insantiated*/ 
#endif
        }

#if WINFSFunctionality 
        static internal Int64 GetKey(int db, int id){
            return (Int64)(((UInt64)(UInt32)db) << 32 | ((UInt64)(UInt32)id)); 
        } 

        internal bool AddAssemblyToCache(int dbId, int id, AssemblyName name,int permissions){ 
            lock(this) {
                if(htAssemblyIdToInfo.ContainsKey(GetKey(dbId,id))){
                    return true;
                } 

                AssemblyInfo info = new AssemblyInfo(); 
                info.assemblyName = name; 
                info.assemblyState = AssemblyState.NotTried;
                info.permissions = permissions; 

                //add to the maps
                htAssemblyIdToInfo[GetKey(dbId,id)] = info;
            } 

            return true; 
        } 

        //get the type instance from its sql type id. 
        //If you are looking up by Id, then you know that you can add type details using AddTypeRefToCache, if doesnt exist
        internal Type GetTypeFromId(int dbId,int typeId){
            TypeInfo info = null;
            AssemblyInfo aInfo = null; 
            Type type = null;
 
                if(htTypeIdToInfo.ContainsKey(GetKey(dbId,typeId))) 
                    info = (TypeInfo)htTypeIdToInfo[GetKey(dbId,typeId)];
 
                if(null == info){
                    type =  null;
                }
                else { 
                    Debug.Assert((info.assemblyId != 0 || info.isInstantiated) && info.className != null);
                    if(htAssemblyIdToInfo.ContainsKey(GetKey(dbId,info.assemblyId))) 
                        aInfo = (AssemblyInfo)htAssemblyIdToInfo[GetKey(dbId,info.assemblyId)]; 

                    if (info.isInstantiated) { 
                        type = info.typeRef;
                    }
                    else if (null != aInfo && aInfo.assemblyRef != null) {
                        type =  aInfo.assemblyRef.GetType(info.className); 
                    }
                    else { 
                        type =  null; 
                    }
                } 

            return type;
        }
 
        internal bool AddTypeRefToCache(int dbId, int typeId, String typeName, String className, int assemblyId, bool multiValued) {
            // todo: multiValue flag not used yet 
 
            AssemblyInfo aInfo = null;
            TypeInfo info = null; 
            bool retval = false;

            // note that if multiValued == true assemblyId == 0 is ok
            if ((0 == assemblyId && !multiValued)) { 
                throw ADP.Argument(Res.GetString(Res.SqlUdtReason_MultivaluedAssemblyId));
            } 
            if (0 == typeId) { 
                throw ADP.ArgumentNull("typeId");
            } 
            ADP.CheckArgumentNull(typeName, "typeName");
            ADP.CheckArgumentNull(className, "className");

            //Is the assembly found? 
            if(false == htAssemblyIdToInfo.ContainsKey(GetKey(dbId,assemblyId))){
                retval =  false; 
            } 

            //already in the map? 
            if(true == htTypeIdToInfo.ContainsKey(GetKey(dbId,typeId))){
                retval =  true;
            }
 
            if (dbId != 0 && assemblyId != 0) {
                aInfo = (AssemblyInfo)htAssemblyIdToInfo[GetKey(dbId,assemblyId)]; 
 
                Debug.Assert(aInfo != null, "Maps are inconsistent. Assembly can not be located for this type");
            } 
            else {
                aInfo = null;
            }
 
            //fill the typeinfo. For MultiValued types, these may not make sense
            info = new TypeInfo(); 
            info.className = className; 
            info.assemblyId = assemblyId;
            info.typeName = typeName; 
            info.isInstantiated = multiValued;

            //add it to the type id map
            htTypeIdToInfo[GetKey(dbId,typeId)] = info; 
            htTypeNameToId[typeName] = GetKey(dbId,typeId);
            retval = true; 
 
            return retval;
        } 

        //
        internal bool FindAndLoadAssembly(int dbId,int aId,bool interop) {
            //find the assembly 
            AssemblyInfo aInfo = null;
            if(false == htAssemblyIdToInfo.ContainsKey(GetKey(dbId,aId))) 
                return false; 
            aInfo = (AssemblyInfo)htAssemblyIdToInfo[GetKey(dbId,aId)];
            Debug.Assert(aInfo != null, "ids not found in the map"); 

            if(AssemblyState.Loaded == aInfo.assemblyState) {
                Debug.Assert(aInfo.assemblyRef != null, "AssemblyState is inconsistent");
                return true; 
            }
            //if it failed before, 
            else if(aInfo.assemblyState == AssemblyState.Failed ||aInfo.assemblyState == AssemblyState.InteropFailed ) 
                return false;
            else { 
                aInfo.assemblyRef = Assembly.Load(aInfo.assemblyName);//, GetEvidence(aInfo.permissions));
                if(null != aInfo.assemblyRef)
                    aInfo.assemblyState = AssemblyState.Loaded;
                else 
                    return false;
            } 
 
            if(interop && false == comReg.RegisterAssembly(aInfo.assemblyRef,(AssemblyRegistrationFlags)0)){
                aInfo.assemblyState = AssemblyState.InteropFailed; 
                return false;
            }

            return (aInfo.assemblyRef != null); 
            //register the assembly for interop
        } 
 
        internal TypeInfo GetTypeInfo(int dbId, int typeId) {
            if(htTypeIdToInfo.ContainsKey(GetKey(dbId,typeId))) 
                return (TypeInfo)htTypeIdToInfo[GetKey(dbId,typeId)];
            else
                return null;
        } 

        internal AssemblyInfo GetAssemblyInfo(int dbId, int aId) { 
            if(htAssemblyIdToInfo.ContainsKey(GetKey(dbId,aId))) 
                return (AssemblyInfo)htAssemblyIdToInfo[GetKey(dbId,aId)];
            else 
                return null;
        }
#endif
 

#if WINFSFunctionality 
        internal static int GetLength(Object inst, bool isWinFS){ 
#else
        internal static int GetLength(Object inst){ 
#endif
            //caller should have allocated enough, based on MaxByteSize
            // NOTE: if we're talking to a winfs server we should be using
            // SerializationHelper.SizeInBytes defined in udtextensions.dll, 
            // note the one in system.data
#if WINFSFunctionality 
            if (isWinFS) 
                return SqlConnection.SizeInBytes(inst);
            else 
#endif
                return SerializationHelperSql9.SizeInBytes(inst);
        }
 
        //The attribute we are looking for is now moved to an external dll that server provides. If the name is changed.
        //then we we have to make corresponding changes here. 
        //please also change sqludcdatetime.cs, sqltime.cs and sqldate.cs 

        internal static SqlUdtInfo GetInfoFromType(Type t) { 
            Debug.Assert(t != null, "Type object cant be NULL");

            Type [....] = t;
            do { 
                SqlUdtInfo attr = SqlUdtInfo.TryGetFromType(t);
 
                if (attr != null ) { 
                    return attr;
                } 
                else {
                    t = t.BaseType;
                }
            } 
            while (t != null);
 
            throw SQL.UDTInvalidSqlType([....].AssemblyQualifiedName); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
