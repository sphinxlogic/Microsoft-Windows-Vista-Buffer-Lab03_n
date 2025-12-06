// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
namespace System.Globalization
{ 
    using System; 
    using System.IO;
    using System.Runtime.InteropServices; 
    using System.Text;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution; 
    using System.Collections;
    using Microsoft.Win32.SafeHandles; 
#if DICTIONARY_FOR_CULTURE_CACHE 
    using System.Collections.Generic;
#endif // DICTIONARY_FOR_CULTURE_CACHE 
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;

    // Enum for the IFLAGS field 
    [Flags]
    internal enum CultureFlags 
    { 
        IsSpecificCulture = 0x0001,
    } 

    /*==============================================================================
     *
     * Data record for CultureInfo classes.  Used by System.Globalization.CultureInfo. 
     *
     * NOTE: ALL of the data table/user override/OS version, etc. hacks related to 
     *       the data should be in here.  DTFI, CI, etc. should have NO knowledge 
     *       of eccentricities of the data store.
     * 
     *
     ==============================================================================*/
    // Only statics, does not need to be marked with the serializable attribute
    internal class CultureTableRecord 
    {
        // For spanish sorting 
        internal const int SPANISH_TRADITIONAL_SORT    = 0x040a; 
        private  const int SPANISH_INTERNATIONAL_SORT  = 0x0c0a;
 
        // Sizes defined by the RFC3066 spec
        private  const int MAXSIZE_LANGUAGE                = 8;
        private  const int MAXSIZE_REGION                  = MAXSIZE_LANGUAGE;
        private  const int MAXSIZE_SUFFIX                  = 8 * MAXSIZE_LANGUAGE; 
        private  const int MAXSIZE_FULLTAGNAME             = MAXSIZE_LANGUAGE + MAXSIZE_REGION + MAXSIZE_SUFFIX + 4; // The 2 is for the tags and the prefix
 
        private static Object s_InternalSyncObject; 
        private static Object InternalSyncObject {
            get { 
                if (s_InternalSyncObject == null) {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                } 
                return s_InternalSyncObject;
            } 
        } 

        // 
        // CultureTableRecordCache caches all CultureTableRecord created objects except the objects created by
        // RegionInfo constructor which takes region name and instead will be cached in CultureTableRecordRegionCache
        //
 
        private static Hashtable CultureTableRecordCache;
        private static Hashtable CultureTableRecordRegionCache; 
 
#if !FEATURE_PAL
 
#if DICTIONARY_FOR_CULTURE_CACHE
        //
        // because the synthetic culture creation is very expensive in term of speed and space. so SyntheticDataCache will be used to cache
        // the created synthetic culture data so subsequent calls will not need to repeat the creation process especially when creating the 
        // same culture using "new CultureInfo(...)" as the regular cultures created this way is not cached.
        // The cache is mapping from lcid to the unmanaged allocated safe memory handle that contains the culture data. 
        // 

        // <SyntheticSupport/> 
        private static Dictionary<int, AgileSafeNativeMemoryHandle> SyntheticDataCache;
        internal static Dictionary<int, String> SyntheticLcidToNameCache;
        internal static Dictionary<String, int> SyntheticNameToLcidCache;
#else 

        private static Hashtable SyntheticDataCache; 
        internal static Hashtable SyntheticLcidToNameCache; 
        internal static Hashtable SyntheticNameToLcidCache;
#endif // DICTIONARY_FOR_CULTURE_CACHE 

#endif // !FEATURE_PAL

 
        // CultureTable this data refers to.
        private CultureTable                m_CultureTable; 
        private unsafe CultureTableData*    m_pData; 
        private unsafe ushort*              m_pPool;
        private bool                        m_bUseUserOverride; 
        private int                         m_CultureID;
        private String                      m_CultureName;

        private int                         m_ActualCultureID   = 0; 
        private string                      m_ActualName        = null;
 
        // <SyntheticSupport/> 
        // m_synthetic will be true only if we have synthetic culture or synthetic replacement culture.
        private bool                        m_synthetic         = false; 

        private AgileSafeNativeMemoryHandle nativeMemoryHandle;             // <SyntheticSupport/>
        private string                      m_windowsPath = null;
 
        private const int LOCALE_SLANGUAGE       = 0x00000002;   // localized name of language
/* SSS_WARNINGS_OFF */                private const int LOCALE_SCOUNTRY        = 0x00000006;   // localized name of country /* SSS_WARNINGS_ON */ 
        private const int LOCALE_SNATIVELANGNAME = 0x00000004;   // native name of language 
/* SSS_WARNINGS_OFF */                private const int LOCALE_SNATIVECTRYNAME = 0x00000008;   // native name of country /* SSS_WARNINGS_ON */
        private const int LOCALE_ICALENDARTYPE   = 0x00001009;   // iCalendarType type of calendar 


        ////////////////////////////////////////////////////////////////////////
        // 
        // Create a CultureTable from the given custom/replacement culture name.
        // 
        // SECURITY SECURITY SECURITY 
        //  Before call this function, call ValidateCulturePieceToLower() to verify
        //  that the name does not contain illegal characters (such as "." or backslash. 
        //
        ////////////////////////////////////////////////////////////////////////

        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private unsafe CultureTable GetCustomCultureTable(string name) 
        { 
            CultureTable cultureTable = null;
 
            string customCultureFile = GetCustomCultureFile(name);
            if (customCultureFile == null)
            {
                return null; 
            }
 
            try 
            {
                cultureTable = new CultureTable(customCultureFile, false); 
                if (!cultureTable.IsValid)
                {
                    // If we have invalid culture table then we have custom culture. in that case we'll try
                    // to see if the culture name is one of the framework or synthetic cultures and then 
                    // try to create it otherwise we'll throw.
 
                    String  defaultTableActualName; 
                    int     defaultTableCultureID;
                    int     defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureName( 
                                                    name, out defaultTableCultureID, out defaultTableActualName);

                    if (defaultTableDataItem < 0)       // not built in framework culture
                    { 
#if !FEATURE_PAL
                        InitSyntheticMapping(); 
                        BCLDebug.Assert(CultureTableRecord.SyntheticLcidToNameCache != null, 
                                "[CultureTableRecord::GetCustomCultureTable] cache should be available now.");
                        if (CultureTableRecord.SyntheticNameToLcidCache[name] == null) 
                        {
                            throw new ArgumentException(
                                    String.Format(
                                        CultureInfo.CurrentCulture, 
                                        Environment.GetResourceString("Arg_CorruptedCustomCultureFile"), name));
                        } 
#endif // !FEATURE_PAL 

                    } 

                    return null;  // returning null means fallback to framework or synthetic culture.
                }
            } 
            catch (FileNotFoundException)
            { 
                // 
                // getting here means custom culture file get unregistered/renamed from different AppDomain/Process.
                // just update the cache to point to the empty string as subsequent calls will not bother trying again. 
                //
                cultureTable = null;
            }
 
            return cultureTable;
        } 
 
        ////////////////////////////////////////////////////////////////////////
        // 
        //  Using the specified replacementCultureName, check if there is a replacment
        //  culture file.  If yes, return the CultureTable created from the custom culture file.
        //
        //  Parameters 
        //      replacementCultureName: The culture name to check.  Note that alternative sort like de-DE_phoneb should pass de-DE here.
        //      [OUT] dataItem: The dataItem for the culture name in the custom culture file. 
        //  Returns 
        //      A valid CultureTable from the custom culture file. null, if the custom file can not be found or the dataItem can not be found.
        // 
        ////////////////////////////////////////////////////////////////////////

        internal unsafe CultureTable TryCreateReplacementCulture(String replacementCultureName, out int dataItem)
        { 
            string name = ValidateCulturePieceToLower(replacementCultureName, "cultureName", MAXSIZE_FULLTAGNAME);
 
            //  Before call this function, call ValidateCulturePieceToLower() to verify 
            //  that the name does not contain illegal characters (such as "." or backslash.
            CultureTable cultureTable = GetCustomCultureTable(name); 

            if (cultureTable == null)
            {
                dataItem = -1; 
                return (null);
            } 
            // We have a replacement culture.  Use it. 
            int tempID;
            String tempName; 
            dataItem = cultureTable.GetDataItemFromCultureName(name, out tempID, out tempName);
            return (dataItem >= 0 ? cultureTable : null);
        }
 
#if !FEATURE_PAL
        //<SyntheticSupport> 
        //////////////////////////////////////////////////////////////////////// 
        //
        //  Create the hashtable for mapping synthetic culture names to LCID, 
        //  and hashtable for mapping synthetic culture LCID to names if they
        //  are not yet created.
        //
        //////////////////////////////////////////////////////////////////////// 

        internal static void InitSyntheticMapping() 
        { 
            // <SyntheticSupport/>
            if (SyntheticLcidToNameCache == null || SyntheticNameToLcidCache == null) 
            {
                CacheSyntheticNameLcidMapping();
            }
        } 
        //</SyntheticSupport>
#endif 
 
        //
        // GetCultureTableRecord create CultureTableRecord object for specific culture name. 
        // This method uses CultureTableRecordCache to make sure we don't have to create this
        // object if it is already created before.
        //
 
        internal static CultureTableRecord GetCultureTableRecord(string name, bool useUserOverride)
        { 
            BCLDebug.Assert(name != null, "[CultureTableRecord::GetCultureTableRecord] name should be valid."); 

            // Make sure the cache is valid. 
            if (CultureTableRecordCache == null)
            {
                if (name.Length == 0) // Invariant culture
                { 
                    // First time Invariant culture get created we ignore creating the cache
                    return new CultureTableRecord(name, useUserOverride); 
                } 

                lock (InternalSyncObject) 
                {
                    if (CultureTableRecordCache == null)
                        CultureTableRecordCache = new Hashtable();
                } 
            }
 
            name = ValidateCulturePieceToLower(name, "name", MAXSIZE_FULLTAGNAME); 

            CultureTableRecord [] cultureRecordArray = (CultureTableRecord []) CultureTableRecordCache[name]; 
            if (cultureRecordArray != null)
            {
                int index   = useUserOverride ? 0 : 1;
 
                if (cultureRecordArray[index] == null)
                { 
                    int filled  = index == 0 ? 1 : 0; 
                    cultureRecordArray[index] = (CultureTableRecord) cultureRecordArray[filled].CloneWithUserOverride(useUserOverride);
                } 

                return cultureRecordArray[index];
            }
 
            CultureTableRecord cultureRecord = new CultureTableRecord(name, useUserOverride);
            lock (InternalSyncObject) 
            { 
                if (CultureTableRecordCache[name] == null)
                { 
                    cultureRecordArray = new CultureTableRecord[2];
                    cultureRecordArray[useUserOverride ? 0 : 1] = cultureRecord;
                    CultureTableRecordCache[name] = cultureRecordArray;
                } 
            }
 
            return cultureRecord; 
        }
 
        //
        // GetCultureTableRecord create CultureTableRecord object for specific culture Id.
        // This method convert the culture Id to culture name and then uses GetCultureTableRecord
        // to get the CultureTableRecord object. 
        //
 
        internal static CultureTableRecord GetCultureTableRecord(int cultureId, bool useUserOverride) 
        {
            if (cultureId == CultureInfo.LOCALE_INVARIANT) 
                return GetCultureTableRecord("", false);

            String name = null;
            if (CultureTable.Default.GetDataItemFromCultureID(cultureId, out name) < 0) 
            {
#if !FEATURE_PAL 
                // Try ELK 
                if (CultureInfo.IsValidLCID(cultureId, CultureInfo.LCID_INSTALLED))
                { 
                    InitSyntheticMapping();
                    name = (String) SyntheticLcidToNameCache[cultureId];
                }
#endif // !FEATURE_PAL 
            }
 
            if (name != null && name.Length>0) // GetDataItemFromCultureID can set the name to empty string. 
            {
                return GetCultureTableRecord(name, useUserOverride); 
            }

            throw new ArgumentException(
                String.Format( 
                    CultureInfo.CurrentCulture,
                    Environment.GetResourceString("Argument_CultureNotSupported"), cultureId), "culture"); 
        } 

        // 
        // GetCultureTableRecordForRegion create CultureTableRecord object for specific region name.
        // this method do the following
        //  o   try to get the object from the cache. if found then return otherwise try to create it.
        //  o   it try to get the record from the framework culture table. if found it then create 
        //      the CultureTableRecord object and store it in the cache then return.
        //  o   call GetCultureTableRecord to get the object. if found it then store it in the cache 
        //      and return. notice that GetCultureTableRecord will try the custom culture then synthetic 
        //      culture.
        //  o   otherwise we'll throw ArgumentException. 
        //

        internal static CultureTableRecord GetCultureTableRecordForRegion(string regionName, bool useUserOverride)
        { 
            BCLDebug.Assert(regionName != null, "[CultureTableRecord::GetCultureTableRecordForRegion] regionName should be valid.");
 
            // Make sure the cache is valid. 
            if (CultureTableRecordRegionCache == null)
            { 
                lock (InternalSyncObject)
                {
                    if (CultureTableRecordRegionCache == null)
                        CultureTableRecordRegionCache = new Hashtable(); 
                }
            } 
 
            regionName = ValidateCulturePieceToLower(regionName, "regionName", MAXSIZE_FULLTAGNAME);
 
            CultureTableRecord [] cultureRecordArray = (CultureTableRecord []) CultureTableRecordRegionCache[regionName];
            if (cultureRecordArray != null)
            {
                int index = useUserOverride ? 0 : 1; 
                if (cultureRecordArray[index] == null)
                { 
                    cultureRecordArray[index] = cultureRecordArray[index == 0 ? 1 : 0].CloneWithUserOverride(useUserOverride); 
                }
                return cultureRecordArray[index]; 
            }

            int dataItem = CultureTable.Default.GetDataItemFromRegionName(regionName);
 
            CultureTableRecord cultureRecord = null;
 
            if (dataItem > 0) 
            {
                cultureRecord = new CultureTableRecord(regionName, dataItem, useUserOverride); 
            }
            else
            {
                try 
                {
                    cultureRecord = GetCultureTableRecord(regionName, useUserOverride); 
                } 
                catch (ArgumentException)
                { 
                    throw new ArgumentException(
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("Argument_InvalidRegionName"), regionName), "name"); 
                }
            } 
 
            BCLDebug.Assert(cultureRecord != null, "[CultureTableRecord::GetCultureTableRecordForRegion] cultureRecord should be valid.");
            lock (InternalSyncObject) 
            {
                if (CultureTableRecordRegionCache[regionName] == null)
                {
                    cultureRecordArray = new CultureTableRecord[2]; 
                    cultureRecordArray[useUserOverride ? 0 : 1] = cultureRecord.CloneWithUserOverride(useUserOverride);
                    CultureTableRecordRegionCache[regionName] = cultureRecordArray; 
                } 
            }
 
            return cultureRecord;
        }

        // 
        // This constructor used only to create a Framework culture. it doesn't create custom
        // culture nor synthetic culture. 
        // This is used when requesting the native calendar name for a custom culture with 
        // empty string native calendar name.
        // 
        internal unsafe CultureTableRecord(int cultureId, bool useUserOverride)
        {
            this.m_bUseUserOverride = useUserOverride;
 
            int defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureID(cultureId, out m_ActualName);
            if (defaultTableDataItem < 0) 
            { 
                throw new ArgumentException(
                    String.Format( 
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Argument_CultureNotSupported"), cultureId), "culture");
            }
 
            m_ActualCultureID   = cultureId;
            m_CultureTable      = CultureTable.Default; 
 
            m_pData = (CultureTableData*)(m_CultureTable.m_pItemData + m_CultureTable.m_itemSize * defaultTableDataItem);
            m_pPool = m_CultureTable.m_pDataPool; 

            m_CultureName = SNAME;
            m_CultureID = (cultureId == SPANISH_TRADITIONAL_SORT) ? cultureId : ILANGUAGE;
 
            BCLDebug.Assert(!IsCustomCulture , "[CultureTableRecord::ctor] we shouldn't have custom culture.");
            BCLDebug.Assert(!IsSynthetic, "[CultureTableRecord::ctor] we shouldn't have synthetic culture."); 
        } 

        // 
        // m_bUseUserOverride indicates that if we need to check for user-override values for this CultureInfo instance.
        // For the user default culture of the system, user can choose to override some of the values
        // associated with that culture.  For example, the default short-date format for en-US is
        // "M/d/yyyy", however, one may change it to "dd/MM/yyyy" from the Regional Option in 
        // the control panel.
        // So when a CultureInfo is created, one can specify if the create CultureInfo should check 
        // for user-override values, or should always get the default values. 
        //
        // 
        // AVOID USING P/INVOKE IN THIS CODEPATH.
        // AVOID USING P/INVOKE IN THIS CODEPATH.
        // AVOID USING P/INVOKE IN THIS CODEPATH.
        // 
        // P/Invoke will cause COM to be initialized and sets the thread apartment model. Since CultureInfo is used early in the CLR process,
        // this will prevent Loader from having a chance to look at the executable and setting the apartment model to the one the application wants. 
 

 
        //
        // Search order for creating a culture.
        //
 
        /*
           First, search by name 
                if this is a known culture name from culture.nlp, and it is an alternative sort name (such as de-DE_phoneb) 
                {
                    Get the name from the LANGID by removing the sort ID (so the name becomes de-DE). 
                    This is the name used for search replacment culture.
                }

                Check if this specified name has a custom/replacement culture file. 
                if there is a custom/replacement culture file
                { 
                    // This is a custom culture, or a replacement culture. 
                    return; [CUSTOM/REPLACEMENT CULTURE (.NET CULTURE/SYNTHETIC CULTURE) FOUND BY NAME]
                } 
                From culture.nlp, check if tihs is a vlid culture name
                if this is a valid culture name
                {
                    // This is a .NET culture. 
                    return; [NON-REPLACEMENT .NET CULTURE FOUND BY NAME]
                } 
                Check if this is a valid name from synthetic culture 
                if this is a valid synthetic culture name
                { 
                    // This is a synthetic culture. Set the cultureID, so we will
                    // create it when we search by LCID later.
                    // [SYNTHETIC CULTURE FOUND BY NAME]
                } else 
                {
                    throw exception;    [INVALID NAME] 
                } 
           Then Search by LCID
                we'll come here only if the lcid is filled with synthetic culture Id. 
                    // This is synthetic culture.
                    Get the name for this LANGID of this synthetic LCID.
                    if there is a replacement culture for this LCID by checking name.
                    { 
                        Use it and return the replacement culture for synthetic culture.
                        return;  [REPLACEMENT SYNTHETIC CULTURE] 
                    } 
                    Init and return the synthetic culture.
                    return;  [NON-REPLACEMENT SYNTHETIC CULTURE] 

                }
                otherwise throw exception
 
        */
        // 
        // * IMPORTANT * cultureName should be in lower case. 
        //
        private unsafe CultureTableRecord(String cultureName, bool useUserOverride) 
        {
            BCLDebug.Assert(cultureName != null, "[CultureTableRecord::ctor] cultureName should be valid.");

            int cultureID = 0; 

            // Special case for invariant name 
            if (cultureName.Length == 0) 
            {
                useUserOverride = false; 
                cultureID = CultureInfo.LOCALE_INVARIANT;
            }

            this.m_bUseUserOverride = useUserOverride; 

            // We prefer to look up by name (if available) 
            int iDataItem = -1; 
            if (cultureName.Length > 0)
            { 
                // Check if this is an alternative sort name.
                String defaultTableActualName;
                int defaultTableCultureID;
                string name = cultureName; 
                int defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureName(name, out defaultTableCultureID, out defaultTableActualName);
                if (defaultTableDataItem >= 0 && 
                    (CultureInfo.GetSortID(defaultTableCultureID) > 0 || defaultTableCultureID == SPANISH_TRADITIONAL_SORT)) 
                {
                    String replacmentCultureName; 

                    int nonSortId;
                    if (defaultTableCultureID == SPANISH_TRADITIONAL_SORT)
                        nonSortId = SPANISH_INTERNATIONAL_SORT; 
                    else
                        nonSortId = CultureInfo.GetLangID(defaultTableCultureID); 
 
                    // This is an alternative sort culture.
                    if (CultureTable.Default.GetDataItemFromCultureID(nonSortId, out replacmentCultureName) >= 0) 
                    {
                        // This is the replacement culture name for an alternative sort.
                        name = ValidateCulturePieceToLower(replacmentCultureName, "cultureName", MAXSIZE_FULLTAGNAME);
                    } 
                }
 
                // If the compatibility flag is defined and culture is replacemet culture then we don't 
                // open the custom culture file. instead we'll try to get framework/OS culture.
                if (!Environment.GetCompatibilityFlag(CompatibilityFlag.DisableReplacementCustomCulture) || 
                    IsCustomCultureId(defaultTableCultureID))
                {

                    // we always try the replacement custom cultures first. 
                    //  Before call this function, call ValidateCulturePieceToLower() to verify
                    //  that the name does not contain illegal characters (such as "." or backslash. 
                    m_CultureTable = GetCustomCultureTable(name); 
                }
 
                if (m_CultureTable != null)
                {
                    //
                    // [CUSTOM/REPLACEMENT CULTURE (.NET CULTURE/SYNTHETIC CULTURE) FOUND BY NAME] 
                    //
                    iDataItem = this.m_CultureTable.GetDataItemFromCultureName(name, out this.m_ActualCultureID, out this.m_ActualName); 
                    if (defaultTableDataItem >= 0) 
                    {
                        // This is a replacment culture (since defaultTableDataItem >= 0), use the default ID/Name from the table. 
                        // For de-DE_phoneb, this will set the the actualCultureID to be 0x10407, instead of the LCID for replacment cutlure 0x0407.
                        this.m_ActualCultureID = defaultTableCultureID;
                        this.m_ActualName = defaultTableActualName;
                    } 
                }
 
                if (iDataItem < 0 && defaultTableDataItem >= 0) 
                {
                    // 
                    // [NON-REPLACEMENT .NET CULTURE FOUND BY NAME]
                    //
                    this.m_CultureTable = CultureTable.Default;
                    this.m_ActualCultureID = defaultTableCultureID; 
                    this.m_ActualName = defaultTableActualName;
                    iDataItem = defaultTableDataItem; 
                } 

#if !FEATURE_PAL 
                // If not exist in main table then try the synthetic one.
                if (iDataItem < 0)
                {
                    InitSyntheticMapping(); 
                    BCLDebug.Assert(SyntheticNameToLcidCache != null, "[CultureTableRecord::ctor] cache should be created any way.");
                    if (SyntheticNameToLcidCache[name] != null) 
                    { 
                        // Initialize the lcid so it get created later when trying the creation using the lcid.
                        cultureID = (int) SyntheticNameToLcidCache[name]; 
                        //
                        // [SYNTHETIC CULTURE FOUND BY NAME]
                        //
                    } 
                }
#endif // !FEATURE_PAL 
            } 

            // If we couldn't get it by name, try culture ID. 
            if (iDataItem < 0 && cultureID > 0)
            {
                if (cultureID == CultureInfo.LOCALE_INVARIANT)
                { 
                    // Special case for the Invariant culture.
                    iDataItem = CultureTable.Default.GetDataItemFromCultureID(cultureID, out this.m_ActualName); 
                    if (iDataItem > 0) 
                    {
                        m_ActualCultureID   = cultureID; 
                        m_CultureTable      = CultureTable.Default;
                    }
                }
#if !FEATURE_PAL 
                else
                { 
                    BCLDebug.Assert(CultureInfo.IsValidLCID(cultureID, CultureInfo.LCID_INSTALLED), 
                        "[CultureTableRecord::ctor] culture should be valid ELK culture.");
 
                    BCLDebug.Assert(SyntheticNameToLcidCache != null, "[CultureTableRecord::ctor] cache should be valid.");

                    // Check if we have replacement cutlure for this synthetic culture.
                    // we always try the replacement custom cultures first. 

                    CultureTable replacementTable       = null; 
                    String       replacementCultureName = null; 

                    // 
                    // Here is the logic for creating the synthetic cultures.
                    // Note the code is optimized to prevent any duplication.
                    //
                    //  If (cultureID is Sort Id) 
                    //      If (non sort Id is one of the framework culture)
                    //          If (replacement) 
                    //              Get data info from custom culture 
                    //          Else
                    //              Get culture data from the framework 
                    //      Else if (non sort Id one of the cached synthetic cultures)
                    //          If (replacement)
                    //              Get data info from custom culture
                    //          Else 
                    //              Get culture data from the OS.
                    //      Else 
                    //          Throw exception. 
                    //  Else
                    //      if (cultureID is one of the cached synthetic cultures) 
                    //          If (replacement)
                    //              Get data info from custom culture
                    //          Else
                    //              Get culture data from the OS. 
                    //      Else
                    //          Throw exception. 
 
                    if (CultureInfo.GetSortID(cultureID) > 0)
                    { 
                        iDataItem = CultureTable.Default.GetDataItemFromCultureID(
                                                            CultureInfo.GetLangID(cultureID),
                                                            out replacementCultureName);
                    } 

                    if (iDataItem < 0) 
                    { 
                        // Get synthetic culture name from language ID. This should always succeed because this is a valid LCID.
                        // Note that language ID, instead of LCID, since alternative sort uses the same replacement file as default sort. 
                        replacementCultureName = (String)SyntheticLcidToNameCache[CultureInfo.GetLangID(cultureID)];
                    }
                    String actualName = (String)SyntheticLcidToNameCache[cultureID];
                    int replacementDataItem = -1; 
                    if (actualName != null && replacementCultureName != null &&
                        !Environment.GetCompatibilityFlag(CompatibilityFlag.DisableReplacementCustomCulture)) 
                    { 
                        replacementTable = TryCreateReplacementCulture(replacementCultureName, out replacementDataItem);
                    } 

                    if (replacementTable == null)
                    {
                        if (iDataItem > 0) 
                        {
                            // This is a synthetic culture corresponds to a new OS alternative sort for one of the framework cultures. 
                            // example ja-JP_radstr 
                            m_CultureTable     = CultureTable.Default;
                            m_ActualCultureID  = cultureID; 
                            m_synthetic        = true;
                            // if we are running on Vista nativeGetCultureName will return the normalized name. otherwise
                            // it will return null and then m_ActualName will initialized later with SNAME.
                            m_ActualName       = (string) CultureInfo.nativeGetCultureName(cultureID, true, false); 
                        }
                        else 
                        // <SyntheticSupport/> 
                        if (GetSyntheticCulture(cultureID))
                        { 
                            BCLDebug.Assert(m_pData != null && m_pPool != null, "[CultureTableRecord::ctor] Got synthetic culture data and didn't intialize the data pointers.");
                            //
                            // [NON-REPLACEMENT SYNTHETIC CULTURE]
                            // 
                            return;
                        } 
                        // iDataItem is -1.  Let the code fall thru and we will throw exception. 
                    }
                    else 
                    {
                        //
                        // [REPLACEMENT SYNTHETIC CULTURE]
                        // 
                        this.m_CultureTable = replacementTable;
                        iDataItem = replacementDataItem; 
                        // Don't initialize m_ActualName by actualName now as we need to have the name normalized. 
                        // later it'll be initialized with SNAME which is normalized name.
                        // But if we are running on Vista nativeGetCultureName will return the normalized name. otherwise 
                        // it will return null and then m_ActualName will initialized later with SNAME.
                        m_ActualName = (string) CultureInfo.nativeGetCultureName(cultureID, true, false);
                        this.m_ActualCultureID= cultureID;
                    } 
                }
#endif // !FEATURE_PAL 
            } 

            // If we found one, use it and return 
            if (iDataItem >= 0)
            {
                // Found it, use it
                this.m_pData = (CultureTableData*)(this.m_CultureTable.m_pItemData + 
                    this.m_CultureTable.m_itemSize * iDataItem);
                this.m_pPool = this.m_CultureTable.m_pDataPool; 
                // Use name & ID from the file ('cept spanish traditional, which has to stay the same) 
                this.m_CultureName = this.SNAME;
                this.m_CultureID = (m_ActualCultureID == SPANISH_TRADITIONAL_SORT) ? m_ActualCultureID : this.ILANGUAGE; 

#if !FEATURE_PAL
                CheckCustomSynthetic();
#endif // !FEATURE_PAL 

                return; 
            } 

            // Error, if we have a name throw that name 
            if (cultureName != null)
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture, 
                        Environment.GetResourceString("Argument_InvalidCultureName"), cultureName), "name");
 
            // No name, throw the LCID 
            throw new ArgumentException(
                String.Format( 
                    CultureInfo.CurrentCulture,
                    Environment.GetResourceString("Argument_CultureNotSupported"), cultureID), "culture");
        }
 
        //
        // this constructor will create the CultureTableRecord object and point to the 
        // culture table at the slot dataItem. 
        //
 
        private unsafe CultureTableRecord(string regionName, int dataItem, bool useUserOverride)
        {
            BCLDebug.Assert(regionName != null && regionName.Length > 0,
                "[CultureTableRecord.CultureTableRecord(regionName,bool)]Expected non-null/empty name"); 

            BCLDebug.Assert(dataItem > 0, "[CultureTableRecord.CultureTableRecord(regionName, dataItem, bool)] dataItem > 0 should be true."); 
 
            // Assuming it works we'll want these
            this.m_bUseUserOverride = useUserOverride; 
            this.m_CultureName = regionName;
            this.m_CultureTable = CultureTable.Default;

            // Found it, use it 
            this.m_pData = (CultureTableData*)(this.m_CultureTable.m_pItemData +
                this.m_CultureTable.m_itemSize * dataItem); 
            this.m_pPool = this.m_CultureTable.m_pDataPool; 

            // Use ID from the file 
            this.m_CultureID = this.ILANGUAGE;
        }

 
#if !FEATURE_PAL
 
        // <SyntheticSupport/> 
        private void CheckCustomSynthetic()
        { 
            if (IsCustomCulture)
            {
                InitSyntheticMapping();
                BCLDebug.Assert(SyntheticLcidToNameCache != null, "[CultureTableRecord::CheckCustomSynthetic] cache should be available now."); 

                if (IsCustomCultureId(m_CultureID)) 
                {   // Supplemental 
                    string name = ValidateCulturePieceToLower(m_CultureName, "CultureName", MAXSIZE_FULLTAGNAME);
                    if (SyntheticNameToLcidCache[name] != null) 
                    {
                        m_synthetic = true;
                        m_ActualCultureID = m_CultureID = (int) SyntheticNameToLcidCache[name];
                    } 
                }
                else 
                {   // Replacement 
                    if (SyntheticLcidToNameCache[m_CultureID] != null)
                    { 
                        m_synthetic = true;
                        m_ActualCultureID = m_CultureID;
                    }
                    else if (m_CultureID != m_ActualCultureID && SyntheticLcidToNameCache[m_ActualCultureID] != null) 
                    {
                        // synthetic alternative sort. 
                        m_synthetic = true; 
                    }
                } 
            }
        }
#endif // !FEATURE_PAL
 

        internal static void ResetCustomCulturesCache() 
        { 
#if !FEATURE_PAL
            CultureTableRecordCache = null; 
            CultureTableRecordRegionCache = null;
#endif // !FEATURE_PAL
        }
 
#if !FEATURE_PAL
 
        // 
        // GetScriptTag is used to detect the script of a given culture to be concatenated to the
        // culture name. the way to do that is to get the sort key of some culture native name as 
        // we used native day name and then from the sort key we get the script.
        //
        // Important: this method should return the script tag in lowercase letters.
        // 
        // <SyntheticSupport/>
        private static unsafe bool GetScriptTag(int lcid, out string script) 
        { 
            script = null;
 
            string nativeDayName = (string) CultureInfo.nativeGetCultureName(lcid, false, true);
            if (nativeDayName == null) { return false; }

            // 
            // Try first the scripts that is not supported by the sort key.
            // 
 
            for (int j=0; j<nativeDayName.Length; j++)
            { 
                if (nativeDayName[j] >= 0x1401 && nativeDayName[j] <= 0x1676)   // Canadian Syllabics
                {
                    script = "cans";
                    return true; 
                }
                if (nativeDayName[j] >= 0x1200 && nativeDayName[j] <= 0x137C)   // Ethiopic 
                { 
                    script = "ethi";
                    return true; 
                }
                if (nativeDayName[j] >= 0x1800 && nativeDayName[j] <= 0x1819)   // Mongolian
                {
                    script = "mong"; 
                    return true;
                } 
                if (nativeDayName[j] >= 0xA000 && nativeDayName[j] <= 0xA4C6)   // Yi 
                {
                    script = "yiii"; 
                    return true;
                }
                if (nativeDayName[j] >= 0x13A0 && nativeDayName[j] <= 0x13F4)   // Cherokee
                { 
                    script = "cher";
                    return true; 
                } 
                if (nativeDayName[j] >= 0x1780 && nativeDayName[j] <= 0x17F9)   // Khmer
                { 
                    script = "khmr";
                    return true;
                }
            } 

            byte [] sortKey; 
            int length = CultureInfo.GetNativeSortKey(lcid, 0, nativeDayName, nativeDayName.Length, out sortKey); 
            if (length == 0) { return false; }
 
            int i=0;
            while (i<length && sortKey[i] != 1) // 1 is the seperator byte
            {
                switch (sortKey[i]) 
                {
                    case 14: script = "latn"; return true;  /* LATIN        */ 
                    case 15: script = "grek"; return true;  /* GREEK        */ 
                    case 16: script = "cyrl"; return true;  /* CYRILLIC     */
                    case 17: script = "armn"; return true;  /* ARMENIAN     */ 
                    case 18: script = "hebr"; return true;  /* HEBREW       */
                    case 19: script = "arab"; return true;  /* ARABIC       */
                    case 20: script = "deva"; return true;  /* DEVANAGARI   */
                    case 21: script = "beng"; return true;  /* BENGALI      */ 
                    case 22: script = "guru"; return true;  /* GURMUKKHI    */
                    case 23: script = "gujr"; return true;  /* GUJARATI     */ 
                    case 24: script = "orya"; return true;  /* ORIYA        */ 
                    case 25: script = "taml"; return true;  /* TAMIL        */
                    case 26: script = "telu"; return true;  /* TELUGU       */ 
                    case 27: script = "knda"; return true;  /* KANNADA      */
                    case 28: script = "mlym"; return true;  /* MALAYLAM     */
                    case 29: script = "sinh"; return true;  /* SINHALESE    */
                    case 30: script = "thai"; return true;  /* THAI         */ 
                    case 31: script = "laoo"; return true;  /* LAO          */
                    case 32: script = "tibt"; return true;  /* TIBETAN      */ 
                    case 33: script = "geor"; return true;  /* GEORGIAN     */ 
                    case 34: script = "kana"; return true;  /* KANA         */
                    case 35: script = "bopo"; return true;  /* BOPOMOFO     */ 
                    case 36: script = "hang"; return true;  /* HANGUL       */
                    case 128:script = "hani"; return true;  /* IDEOGRAPH    */
                }
                i += 2; 
            }
            return false; 
        } 

        // <SyntheticSupport/> 
        // IsBuiltInCulture is used when caching the name and lcid mapping to detect if it is
        // a framework culture or synthetic cultures.
        private static bool IsBuiltInCulture(int lcid)
        { 
            return CultureTable.Default.IsExistingCulture(lcid);
        } 
 
        internal static string Concatenate(StringBuilder helper, params string [] stringsToConcat)
        { 
            BCLDebug.Assert(helper != null, "[CultureTableRecord::Concatenate] string builder should be valid.");

            if (helper.Length > 0)
            { 
                helper.Remove(0, helper.Length);
            } 
 
            for (int i=0; i<stringsToConcat.Length; i++)
            { 
                helper.Append(stringsToConcat[i]);
            }

            return helper.ToString(); 
        }
 
        // 
        // GetCultureNamesUsingSNameLCType will try to get the culture names using LOCALE_SNAME type. and will
        // retun true in success and false in failing. 
        //
        // <SyntheticSupport/>

        internal static bool GetCultureNamesUsingSNameLCType(int [] lcidArray, Hashtable lcidToName, Hashtable nameToLcid) 
        {
            string cultureName; 
 
            // first try to see if the LOCALE_SNAME is supported on current platform.
            cultureName = (string) CultureInfo.nativeGetCultureName(lcidArray[0], true, false); 
            if (cultureName == null)
                return false;

            if (!IsBuiltInCulture(lcidArray[0])) 
            {
                cultureName = ValidateCulturePieceToLower(cultureName, "cultureName", cultureName.Length); 
                nameToLcid[cultureName]  = lcidArray[0]; 
                lcidToName[lcidArray[0]] = cultureName;
            } 


            for (int i=1; i<lcidArray.Length; i++)
            { 
                if (IsBuiltInCulture(lcidArray[i]))
                    continue; 
 
                cultureName = (string) CultureInfo.nativeGetCultureName(lcidArray[i], true, false);
                if (cultureName != null) 
                {
                    cultureName = ValidateCulturePieceToLower(cultureName, "cultureName", cultureName.Length);
                    nameToLcid[cultureName]  = lcidArray[i];
                    lcidToName[lcidArray[i]] = cultureName; 
                }
            } 
 
            return true;
        } 

        // <SyntheticSupport/>
        internal static void CacheSyntheticNameLcidMapping()
        { 

#if DICTIONARY_FOR_CULTURE_CACHE 
            Dictionary<int, String> lcidToName = new Dictionary<int, String>(); 
            Dictionary<String, int> nameToLcid = new Dictionary<String, int>();
#else 
            Hashtable lcidToName = new Hashtable();
            Hashtable nameToLcid = new Hashtable();
#endif // DICTIONARY_FOR_CULTURE_CACHE
 
            // the lock is important here because GetLcidAndCultureNames is using global static variable
            // which need to be protected accross the threads 
            int [] lcidArray = null; 
            bool result = false;
 
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { 
                // This is intentionally taking a process-global lock, using a CER
                // to guarantee we release the lock. 
                // Note that we should lock on a internal mscorlib type, so that 
                // no public caller can block us by locking on the same type.
                Monitor.ReliableEnter(typeof(CultureTableRecord), ref tookLock); 
                // nativeEnumSystemLocale uses a global variable in the native side, so
                // it needs process-wide synchronization.
                result = CultureInfo.nativeEnumSystemLocales(out lcidArray);
            } 
            finally
            { 
                if (tookLock) 
                    Monitor.Exit(typeof(CultureTableRecord));
            } 

            if (result)
            {
                if (!GetCultureNamesUsingSNameLCType(lcidArray, lcidToName, nameToLcid)) 
                {
                    // Coming here means we couldn't get the names through LOCALE_SNAME so now we'll try to get the names 
/* SSS_WARNINGS_OFF */                            // using the language, country, and script. /* SSS_WARNINGS_ON */ 
                    //
                    // culturesNeedingScriptInName is the hash which holds all the cultures that need a script tag in the name. 
                    // when we encounter a duplicate culture in the nameToLcid table then we know this name will need to have
                    // a script tag. then we remove the name from nameToLcid (to add the one with the script tag instead)
                    // and we add the the removed name to culturesNeedingScriptInName for future checking.
                    // 
                    Hashtable culturesNeedingScriptInName = GetNamesHashtable();
                    string script; 
                    StringBuilder stringHelper = new StringBuilder(); 

                    for (int i=0; i<lcidArray.Length; i++) 
                    {
                        int lcid = lcidArray[i];

                        if ( !IsBuiltInCulture(lcid) ) 
                        {
                            string                          cultureName; 
                            AdjustedSyntheticCultureName    adjustedNames; 

                            GetAdjustedNames(lcid, out adjustedNames); 
                            if (adjustedNames != null) { cultureName = adjustedNames.sName; }                        // Hardcoded culture name
                            else  { cultureName = (string) CultureInfo.nativeGetCultureName(lcid, false, false); }   // Calculated culture name

                            if (cultureName != null) 
                            {
                                cultureName = ValidateCulturePieceToLower(cultureName, "cultureName", cultureName.Length); 
                                if (culturesNeedingScriptInName[cultureName] != null) 
                                {
                                    // we already have 2 cultures so we need the script 
                                    if (GetScriptTag(lcid, out script))
                                    {
                                        script = Concatenate(stringHelper, cultureName, "-", script);
                                        script = GetQualifiedName(script); 
                                        BCLDebug.Assert(nameToLcid[script] == null, "[CultureTableRecord::CacheSyntheticNameLcidMapping] duplicates of the culture names.");
                                        nameToLcid[script]      = lcid; 
                                        lcidToName[lcid]        = script; 
                                    }
                                } 
                                else if (nameToLcid[cultureName] == null)
                                {
                                    // This is the first time weve seen this name.
                                    nameToLcid[cultureName] = lcid; 
                                    lcidToName[lcid]        = cultureName;
                                } 
                                else 
                                {
                                    // we already have a culture with this name, so we need to add scripts to both 
                                    int oldLcid = (int) nameToLcid[cultureName];
                                    nameToLcid.Remove(cultureName);
                                    lcidToName.Remove(oldLcid);
 
                                    culturesNeedingScriptInName[cultureName] = "";
 
                                    if (GetScriptTag(oldLcid, out script)) 
                                    {
                                        script = Concatenate(stringHelper, cultureName, "-", script); 
                                        script = GetQualifiedName(script);
                                        BCLDebug.Assert(nameToLcid[script] == null, "[CultureTableRecord::CacheSyntheticNameLcidMapping] duplicates of the culture names.");
                                        nameToLcid[script]      = oldLcid;
                                        lcidToName[oldLcid]     = script; 
                                    }
 
                                    if (GetScriptTag(lcid, out script)) 
                                    {
                                        script = Concatenate(stringHelper, cultureName, "-", script); 
                                        script = GetQualifiedName(script);
                                        BCLDebug.Assert(nameToLcid[script] == null, "[CultureTableRecord::CacheSyntheticNameLcidMapping] duplicates of the culture names.");
                                        nameToLcid[script]      = lcid;
                                        lcidToName[lcid]        = script; 
                                    }
                                } 
                            } 
                        }
                    } 
                }
            }

            lock (InternalSyncObject) 
            {
                SyntheticLcidToNameCache     = lcidToName; 
                SyntheticNameToLcidCache     = nameToLcid; 
            }
        } 

        // <SyntheticSupport/>
        struct CompositeCultureData
        { 
            internal string sname;
            internal string englishDisplayName; 
            internal string sNativeDisplayName; 
            internal string waCalendars;
            internal string consoleFallbackName; 
            internal string parentName;
            internal int    parentLcid;
        }
 
        // <SyntheticSupport/>
        // AdjustSyntheticCalendars is doing two things. First it ensures the native calendar names are stored at 
        // the correct index in the nativeCalendarNames array. Second it ensure the default calendar in the first 
        // listed one in the waCalendars.
        private static void AdjustSyntheticCalendars(ref CultureData data, ref CompositeCultureData compositeData) 
        {
            BCLDebug.Assert(data.waCalendars.Length > 0,
                    "[CultureTableRecord::AdjustSyntheticCalendars] we should have at least one calendar.");
 
            StringBuilder calendarsId           = new StringBuilder();
            int           defaultCalendarIndex  = 0; 
            ushort        maxCalendarId         = data.waCalendars[0]; 

            calendarsId.Append((char) maxCalendarId); 

            for (int i=1; i<data.waCalendars.Length; i++)
            {
                calendarsId.Append((char) data.waCalendars[i]); 

                if (data.waCalendars[i] == (ushort) data.iDefaultCalender) 
                    defaultCalendarIndex = i; 
                if (data.waCalendars[i] > maxCalendarId)
                    maxCalendarId = data.waCalendars[i]; 
            }

            if (maxCalendarId > 1)
            { 
                string [] nativeCalendarNames = new string[maxCalendarId];
                for (int i=0; i<nativeCalendarNames.Length; i++) 
                    nativeCalendarNames[i] = String.Empty; 

                for (int i=0; i<data.waCalendars.Length; i++) 
                {
                    nativeCalendarNames[data.waCalendars[i] - 1] = data.saNativeCalendarNames[i];
                }
 
                data.saNativeCalendarNames = nativeCalendarNames;
            } 
 
            if (defaultCalendarIndex > 0)
            { 
                char defaultCal                    = calendarsId[defaultCalendarIndex];
                calendarsId[defaultCalendarIndex]  = calendarsId[0];
                calendarsId[0]                     = defaultCal;
            } 

            compositeData.waCalendars = calendarsId.ToString(); 
        } 

        //******************************************************************************* 
        //  Get the synthetic (ELK) culture data
        //      o   it requests the data from the native side
        //      o   allocate native memory and fill it
        //      o   make m_pData and m_pPool point to the allocated data 
        //*******************************************************************************
 
        // <SyntheticSupport/> 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine)] 
        private unsafe bool GetSyntheticCulture(int cultureID)
        {
            if (SyntheticLcidToNameCache == null || SyntheticNameToLcidCache == null)
            { 
                CacheSyntheticNameLcidMapping();
            } 
 
            if (SyntheticLcidToNameCache[cultureID] == null) { return false; }
 
            if (SyntheticDataCache == null)
#if DICTIONARY_FOR_CULTURE_CACHE
            { SyntheticDataCache = new Dictionary<int, AgileSafeNativeMemoryHandle>(); }
#else 
            { SyntheticDataCache = new Hashtable(); }
#endif // DICTIONARY_FOR_CULTURE_CACHE 
            else 
            {
                nativeMemoryHandle = (AgileSafeNativeMemoryHandle) SyntheticDataCache[cultureID]; 
            }

            if (nativeMemoryHandle != null)
            { 
                m_pData = (CultureTableData *) nativeMemoryHandle.DangerousGetHandle();
                m_pPool = (ushort*) (((byte *) m_pData) + sizeof(CultureTableData)); 
                m_CultureTable      = CultureTable.Default; 
                m_CultureName       = SNAME;
                m_CultureID         = cultureID; 
                m_synthetic         = true;
                m_ActualCultureID   = cultureID;
                m_ActualName        = m_CultureName;
                return true; 
            }
 
            CultureData data = new CultureData(); 

            bool success = false; 

            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try 
            {
                // This is intentionally taking a process-global lock, using a CER 
                // to guarantee we release the lock. 
                // Note that we should lock on a internal mscorlib type, so that
                // no public caller can block us by locking on the same type. 
                Monitor.ReliableEnter(typeof(CultureTableRecord), ref tookLock);
                // we should lock on the call of CultureInfo.nativeGetCultureData because it uses native static
                // variables that are not safe to be accessed in same time from different threads.
                success = CultureInfo.nativeGetCultureData(cultureID, ref data); 
            }
            finally 
            { 
                if (tookLock)
                    Monitor.Exit(typeof(CultureTableRecord)); 
            }

            if (!success) { return false; }
 
            CompositeCultureData compositeData = new CompositeCultureData();
            int dataSize = GetCultureDataSize(cultureID, ref data, ref compositeData); 
 
            IntPtr handle = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try { } 
                finally
                { 
                    handle = Marshal.AllocHGlobal(dataSize); 
                    if (handle != IntPtr.Zero)
                        nativeMemoryHandle = new AgileSafeNativeMemoryHandle(handle, true); 
                }
            }
            finally
            { 
                if (nativeMemoryHandle == null && handle != IntPtr.Zero)
                { 
                    // 
                    // if we came here means the thread aborted or OOM thrown after allocating our native memory
                    // we need to free this memory before we proceed so we'll not have any memory leaks. 
                    //

                    Marshal.FreeHGlobal(handle);
                    handle = IntPtr.Zero; 
                }
            } 
 
            if (handle == IntPtr.Zero)
            { 
                throw new OutOfMemoryException( Environment.GetResourceString("OutOfMemory_MemFailPoint") );
            }
            BCLDebug.Assert(!nativeMemoryHandle.IsInvalid, "[CultureTableRecord::GetSyntheticCulture] The native memory is not allocated.");
 
            m_pData = (CultureTableData *) nativeMemoryHandle.DangerousGetHandle();
            m_pPool = (ushort*) (((byte *) m_pData) + sizeof(CultureTableData)); 
 
            uint filledBytes = FillCultureDataMemory(cultureID, ref data, ref compositeData);
            BCLDebug.Assert(filledBytes == dataSize - sizeof(CultureTableData), String.Format( 
                CultureInfo.CurrentCulture,
                "[CultureTableRecord::GetSyntheticCulture] The allocated memory size {0} != Filled memory size {1}.",
                dataSize - sizeof(CultureTableData), filledBytes));
 
            m_CultureTable      = CultureTable.Default;
            m_CultureName       = SNAME; 
            m_CultureID         = cultureID; 
            m_synthetic         = true;
            m_ActualCultureID   = cultureID; 
            m_ActualName        = m_CultureName;

            lock (SyntheticDataCache)
            { 
                if (SyntheticDataCache[cultureID] == null) { SyntheticDataCache[cultureID] = nativeMemoryHandle; }
            } 
 
            BCLDebug.Assert(!nativeMemoryHandle.IsInvalid, "CultureTableData pointer was invalid!");
            return true; 
        }

/* SSS_WARNINGS_OFF */
        /* 
          ELK v1.0 was shipped with wrong ISO language and country names for the cultures listed below
          To support it we need to adjust the names to the correct values. 
 
          -------------------------------------------------------------------------------------------------------------
          LCID    Lang    Region  version Description                           correct SNAME   Wrong SNAME 
          --------------------------------------------------------------------------------------------------------------
          141a    bs        BA  ELK v1  Bosnian (Latin, Bosnia and Herzegovina) bs-BA-Latn      (bs-BA)
          243b    smn       FI  ELK v1  Sami, Inari (Finland)                   smn-FI          (se-FI)
          103b    smj       NO  ELK v1  Sami, Lule (Norway)                     smj-NO          (se-NO) 
          143b    smj       SE  ELK v1  Sami, Lule (Sweden)                     smj-SE          (se-SE)
          203b    sms       FI  ELK v1  Sami, Skolt (Finland)                   sms-FI          (se-FI) 
          183b    sma       NO  ELK v1  Sami, Southern (Norway)                 sma-NO          (se-NO) 
          1c3b    sma       SE  ELK v1  Sami, Southern (Sweden)                 sma-SE          (se-SE)
          046b    quz       BO  ELK v1  Cusco Quechua (Bolivia)                 quz-BO          (qu-BO) 
          086b    quz       EC  ELK v1  Cusco Quechua (Equador)                 quz-EC          (qu-EC)
          0c6b    quz       PE  ELK v1  Cusco Quechua (Peru)                    quz-PE          (qu-PE)
          ------------------------------------------------------------------------------------------------------------- */
/* SSS_WARNINGS_ON */ 
        // <SyntheticSupport/>
        internal class AdjustedSyntheticCultureName 
        { 
/* SSS_WARNINGS_OFF */                    internal AdjustedSyntheticCultureName(int lcid, string isoLanguage, string isoCountry, string sName) /* SSS_WARNINGS_ON */
            { 
                this.lcid           = lcid;
                this.isoLanguage    = isoLanguage;
/* SSS_WARNINGS_OFF */                        this.isoCountry     = isoCountry; /* SSS_WARNINGS_ON */
                this.sName          = sName; 
            }
 
            internal int    lcid; 
            internal string isoLanguage;
/* SSS_WARNINGS_OFF */                    internal string isoCountry; /* SSS_WARNINGS_ON */ 
            internal string sName;
        }

        // <SyntheticSupport/> 
        static AdjustedSyntheticCultureName [] s_adjustedSyntheticNames = null;
 
        // <SyntheticSupport/> 
        static AdjustedSyntheticCultureName [] AdjustedSyntheticNames
        { 
            get
            {
                if (s_adjustedSyntheticNames == null)
                { 
                    // <SyntheticSupport/>
                    // 
                    // IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT 
                    // If you added a new item which has a script tag as part of the name to the AdjustedSyntheticCultureName
                    // array then you need store the culture name (without the script part) into the hashtable in the method 
                    // GetNamesHashtable().
                    //
                    s_adjustedSyntheticNames = new AdjustedSyntheticCultureName[]
                    { 
                        new AdjustedSyntheticCultureName(0x141a, "bs",  "BA",  "bs-Latn-BA" ),       // ELK v1  Bosnian (Latin, Bosnia and Herzegovina) (bs-BA)
                        new AdjustedSyntheticCultureName(0x243b, "smn", "FI",  "smn-FI"     ),       // ELK v1  Sami, Inari (Finland)                   (se-FI) 
                        new AdjustedSyntheticCultureName(0x103b, "smj", "NO",  "smj-NO"     ),       // ELK v1  Sami, Lule (Norway)                     (se-NO) 
                        new AdjustedSyntheticCultureName(0x143b, "smj", "SE",  "smj-SE"     ),       // ELK v1  Sami, Lule (Sweden)                     (se-SE)
                        new AdjustedSyntheticCultureName(0x203b, "sms", "FI",  "sms-FI"     ),       // ELK v1  Sami, Skolt (Finland)                   (se-FI) 
                        new AdjustedSyntheticCultureName(0x183b, "sma", "NO",  "sma-NO"     ),       // ELK v1  Sami, Southern (Norway)                 (se-NO)
                        new AdjustedSyntheticCultureName(0x1c3b, "sma", "SE",  "sma-SE"     ),       // ELK v1  Sami, Southern (Sweden)                 (se-SE)
                        new AdjustedSyntheticCultureName(0x046b, "quz", "BO",  "quz-BO"     ),       // ELK v1  Cusco Quechua (Bolivia)                 (qu-BO)
                        new AdjustedSyntheticCultureName(0x086b, "quz", "EC",  "quz-EC"     ),       // ELK v1  Cusco Quechua (Equador)                 (qu-EC) 
                        new AdjustedSyntheticCultureName(0x0c6b, "quz", "PE",  "quz-PE"     )        // ELK v1  Cusco Quechua (Peru)                    (qu-PE)
                    }; 
                } 
                return (s_adjustedSyntheticNames);
            } 
        }

        // <SyntheticSupport/>
        internal static Hashtable GetNamesHashtable() 
        {
            Hashtable table = new Hashtable(); 
 
            // All keys is the hashtable should be in lowercase.
            table["bs-ba"] = ""; 
            table["tg-tj"] = "";
            table["mn-cn"] = "";
            table["iu-ca"] = "";
 
            return table;
        } 
 
        // <SyntheticSupport/>
        internal static void GetAdjustedNames(int lcid, out AdjustedSyntheticCultureName adjustedNames) 
        {

            for (int i=0; i<AdjustedSyntheticNames.Length; i++)
            { 
                if (AdjustedSyntheticNames[i].lcid == lcid) { adjustedNames = AdjustedSyntheticNames[i]; return; }
            } 
 
            adjustedNames = null;
        } 

        //
        // FillCultureDataMemory fill the data into the allocated memory and return the number of bytes
        // used to fill the whole data. 
        //
        // <SyntheticSupport/> 
        private unsafe uint FillCultureDataMemory(int cultureID, ref CultureData data, ref CompositeCultureData compositeData) 
        {
            BCLDebug.Assert(m_pData != null && m_pPool != null, "[CultureTableRecord::FillCultureDataMemory] we should have valid buffers here."); 

            uint offset = 0;
            Hashtable offsetTable = new Hashtable(30);
 
            // Add padding to get our funny dword alignment
            m_pPool[offset] = 0; 
            offset++; 

            // Set empty string and correct its offset 
            SetPoolString("", offsetTable, ref offset);
            offsetTable[""] = (uint)0;

            m_pData->iLanguage          = (ushort) cultureID; 
            m_pData->sName              = (ushort) SetPoolString(compositeData.sname, offsetTable, ref offset);
            m_pData->iDigits            = (ushort) data.iDigits; 
            m_pData->iNegativeNumber    = (ushort) data.iNegativeNumber; 
            m_pData->iCurrencyDigits    = (ushort) data.iCurrencyDigits;
            m_pData->iCurrency          = (ushort) data.iCurrency; 
            m_pData->iNegativeCurrency  = (ushort) data.iNegativeCurrency;
            m_pData->iLeadingZeros      = (ushort) data.iLeadingZeros;
            m_pData->iFlags             = (ushort) CultureFlags.IsSpecificCulture;
            m_pData->iFirstDayOfWeek    = ConvertFirstDayOfWeekMonToSun(data.iFirstDayOfWeek); 
            m_pData->iFirstWeekOfYear   = (ushort) data.iFirstWeekOfYear;
/* SSS_WARNINGS_OFF */                    m_pData->iCountry           = (ushort) data.iCountry; /* SSS_WARNINGS_ON */ 
            m_pData->iMeasure           = (ushort) data.iMeasure; 
            m_pData->iDigitSubstitution = (ushort) data.iDigitSubstitution;
            m_pData->waGrouping         = (ushort) SetPoolString(data.waGrouping, offsetTable, ref offset); 
            m_pData->waMonetaryGrouping = (ushort) SetPoolString(data.waMonetaryGrouping, offsetTable, ref offset);
            m_pData->sListSeparator     = (ushort) SetPoolString(data.sListSeparator, offsetTable, ref offset);
            m_pData->sDecimalSeparator  = (ushort) SetPoolString(data.sDecimalSeparator, offsetTable, ref offset);
            m_pData->sThousandSeparator = (ushort) SetPoolString(data.sThousandSeparator, offsetTable, ref offset); 
            m_pData->sCurrency          = (ushort) SetPoolString(data.sCurrency, offsetTable, ref offset);
            m_pData->sMonetaryDecimal   = (ushort) SetPoolString(data.sMonetaryDecimal, offsetTable, ref offset); 
            m_pData->sMonetaryThousand  = (ushort) SetPoolString(data.sMonetaryThousand, offsetTable, ref offset); 
            m_pData->sPositiveSign      = (ushort) SetPoolString(data.sPositiveSign, offsetTable, ref offset);
            m_pData->sNegativeSign      = (ushort) SetPoolString(data.sNegativeSign, offsetTable, ref offset); 
            m_pData->sAM1159            = (ushort) SetPoolString(data.sAM1159, offsetTable, ref offset);
            m_pData->sPM2359            = (ushort) SetPoolString(data.sPM2359, offsetTable, ref offset);
            m_pData->saNativeDigits     = (ushort) SetPoolStringArrayFromSingleString(data.saNativeDigits, offsetTable, ref offset);
            m_pData->saTimeFormat       = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saTimeFormat); 
            m_pData->saShortDate        = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saShortDate);
            m_pData->saLongDate         = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saLongDate); 
            m_pData->saYearMonth        = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saYearMonth); 
            m_pData->saDuration         = (ushort) SetPoolStringArray(offsetTable, ref offset, "");
            m_pData->iDefaultLanguage   = m_pData->iLanguage; 
            m_pData->iDefaultAnsiCodePage=(ushort) data.iDefaultAnsiCodePage;
            m_pData->iDefaultOemCodePage= (ushort) data.iDefaultOemCodePage;
            m_pData->iDefaultMacCodePage= (ushort) data.iDefaultMacCodePage;
            m_pData->iDefaultEbcdicCodePage= (ushort) data.iDefaultEbcdicCodePage; 
            m_pData->iGeoId             = (ushort) data.iGeoId;
            m_pData->iPaperSize         = (ushort) data.iPaperSize; 
            m_pData->iIntlCurrencyDigits= (ushort) data.iIntlCurrencyDigits; 
            m_pData->iParent            = (ushort) compositeData.parentLcid;
            m_pData->waCalendars        = (ushort) SetPoolString(compositeData.waCalendars, offsetTable, ref offset); 
            m_pData->sAbbrevLang        = (ushort) SetPoolString(data.sAbbrevLang, offsetTable, ref offset);
            m_pData->sISO639Language    = (ushort) SetPoolString(data.sIso639Language, offsetTable, ref offset);
            m_pData->sEnglishLanguage   = (ushort) SetPoolString(data.sEnglishLanguage, offsetTable, ref offset);
            m_pData->sNativeLanguage    = (ushort) SetPoolString(data.sNativeLanguage, offsetTable, ref offset); 
/* SSS_WARNINGS_OFF */                    m_pData->sEnglishCountry    = (ushort) SetPoolString(data.sEnglishCountry, offsetTable, ref offset); /* SSS_WARNINGS_ON */
/* SSS_WARNINGS_OFF */                    m_pData->sNativeCountry     = (ushort) SetPoolString(data.sNativeCountry, offsetTable, ref offset); /* SSS_WARNINGS_ON */ 
/* SSS_WARNINGS_OFF */                    m_pData->sAbbrevCountry     = (ushort) SetPoolString(data.sAbbrevCountry, offsetTable, ref offset); /* SSS_WARNINGS_ON */ 
/* SSS_WARNINGS_OFF */                    m_pData->sISO3166CountryName= (ushort) SetPoolString(data.sIso3166CountryName, offsetTable, ref offset); /* SSS_WARNINGS_ON */
            m_pData->sIntlMonetarySymbol= (ushort) SetPoolString(data.sIntlMonetarySymbol, offsetTable, ref offset); 
            m_pData->sEnglishCurrency   = (ushort) SetPoolString(data.sEnglishCurrency, offsetTable, ref offset);
            m_pData->sNativeCurrency    = (ushort) SetPoolString(data.sNativeCurrency, offsetTable, ref offset);
            m_pData->waFontSignature    = (ushort) SetPoolString(data.waFontSignature, offsetTable, ref offset);
            m_pData->sISO639Language2   = (ushort) SetPoolString(data.sISO639Language2, offsetTable, ref offset); 
/* SSS_WARNINGS_OFF */                    m_pData->sISO3166CountryName2= (ushort) SetPoolString(data.sISO3166CountryName2, offsetTable, ref offset); /* SSS_WARNINGS_ON */
            m_pData->sParent            = (ushort) SetPoolString(compositeData.parentName, offsetTable, ref offset); 
            m_pData->saDayNames         = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saDayNames); 
            m_pData->saAbbrevDayNames   = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saAbbrevDayNames);
            m_pData->saMonthNames       = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saMonthNames); 
            m_pData->saAbbrevMonthNames = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saAbbrevMonthNames);

            m_pData->saMonthGenitiveNames = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saGenitiveMonthNames);
            m_pData->saAbbrevMonthGenitiveNames = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saAbbrevGenitiveMonthNames); 

            m_pData->saNativeCalendarNames      = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saNativeCalendarNames); 
 
            // To Do: We need to find a better way to get the alternative sort names.
            // look at ComNldInfo::nativeGetCultureData and CultureTableRecord.GetCultureDataSize 
            // m_pData->saAltSortID        = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saAltSortID);

            m_pData->saAltSortID        = (ushort) SetPoolStringArray(offsetTable, ref offset, "");
            m_pData->iNegativePercent   = (ushort) CultureInfo.InvariantCulture.NumberFormat.PercentNegativePattern; 
            m_pData->iPositivePercent   = (ushort) CultureInfo.InvariantCulture.NumberFormat.PercentPositivePattern;
            m_pData->iFormatFlags       = (ushort) DateTimeFormatFlags.None; 
            // We need to keep this since we need value for TextInfo.IsRightToLeft. 
            m_pData->iLineOrientations  = (ushort) 0x0000;
            m_pData->iTextInfo          = m_pData->iLanguage; 
            m_pData->iInputLanguageHandle=m_pData->iLanguage;
            m_pData->iCompareInfo       = m_pData->iLanguage;
            m_pData->sEnglishDisplayName= (ushort) SetPoolString(compositeData.englishDisplayName, offsetTable, ref offset);
            m_pData->sNativeDisplayName = (ushort) SetPoolString(compositeData.sNativeDisplayName, offsetTable, ref offset); 
            m_pData->sPercent           = (ushort) SetPoolString(CultureInfo.InvariantCulture.NumberFormat.PercentSymbol, offsetTable, ref offset);
            m_pData->sNaN               = (ushort) SetPoolString(data.sNaN, offsetTable, ref offset); 
            m_pData->sPositiveInfinity  = (ushort) SetPoolString(data.sPositiveInfinity, offsetTable, ref offset); 
            m_pData->sNegativeInfinity  = (ushort) SetPoolString(data.sNegativeInfinity, offsetTable, ref offset);
            m_pData->sMonthDay          = (ushort) SetPoolString(CultureInfo.InvariantCulture.DateTimeFormat.MonthDayPattern, offsetTable, ref offset); 
            m_pData->sAdEra             = (ushort) SetPoolString(CultureInfo.InvariantCulture.DateTimeFormat.GetEraName(0), offsetTable, ref offset);
            m_pData->sAbbrevAdEra       = (ushort) SetPoolString(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedEraName(0), offsetTable, ref offset);
/* SSS_WARNINGS_OFF */            m_pData->sRegionName        = m_pData->sISO3166CountryName; /* SSS_WARNINGS_ON */
            m_pData->sConsoleFallbackName= (ushort) SetPoolString(compositeData.consoleFallbackName, offsetTable, ref offset); 
            m_pData->saShortTime        = m_pData->saTimeFormat;
            m_pData->saSuperShortDayNames= (ushort) SetPoolStringArray(offsetTable, ref offset, data.saSuperShortDayNames); 
            m_pData->saDateWords        = m_pData->saDuration; 
            m_pData->sSpecificCulture   = m_pData->sName;
            m_pData->sScripts           = 0; 

            // Offset is in words, but we return size in bytes
            return 2 * offset;
        } 

        // Sets a string in the pool, returns string offset in words 
        // <SyntheticSupport/> 
        private unsafe uint SetPoolString(string s, Hashtable offsetTable, ref uint currentOffset)
        { 
            BCLDebug.Assert((currentOffset & 1) == 1,
                "[CultureTableRecord.SetPoolString]Expected offset to be on odd word");
            uint offset = currentOffset;
 
            if (offsetTable[s] == null)
            { 
                offsetTable[s] = currentOffset; 
                m_pPool[currentOffset] = (ushort) s.Length;
                currentOffset++; 

                for (int i=0; i<s.Length; i++)
                {
                    m_pPool[currentOffset] = (ushort) s[i]; 
                    currentOffset++;
                } 
 
                // Keep the alignment
                if ((currentOffset & 1) == 0) 
                {
                    m_pPool[currentOffset] = (ushort) 0;
                    currentOffset++;
                } 

                return offset; 
            } 

            return (uint)offsetTable[s]; 
        }

        // Sets an array of strings in the pool, returns the offset of that array (in words)
        // <SyntheticSupport/> 
        private unsafe uint SetPoolStringArray(Hashtable offsetTable, ref uint currentOffset, params string [] array)
        { 
            BCLDebug.Assert((currentOffset & 1) == 1, 
                "[CultureTableRecord.SetPoolStringArray]Expected offset to be on odd word");
 
            uint [] offsetArray = new uint[array.Length];
            for (int i=0; i<array.Length; i++)
            {
                offsetArray[i] = SetPoolString(array[i], offsetTable, ref currentOffset); 
            }
 
            // We should still be odd WORD aligned 
            BCLDebug.Assert((currentOffset & 1) == 1,
                "[CultureTableRecord.SetPoolStringArray]Expected offset to remain on odd word"); 

            // Remember the offset that starts our array (so we can return it)
            uint offset = currentOffset;
 
            // Remember the length
            m_pPool[currentOffset] = (ushort) offsetArray.Length; 
            currentOffset++; 

            // Need a uint pool 
            uint* uints = (uint*)(m_pPool + currentOffset);

            for (int i=0; i<offsetArray.Length; i++)
            { 
                uints[i] = offsetArray[i];
                currentOffset+=2;   // 2 WORDS in a DWORD 
            } 

            // Keep the alignment if necessary 
            if ((currentOffset & 1) == 0)
            {
                m_pPool[currentOffset] = (ushort) 0;
                currentOffset++; 
            }
 
            return (offset); 
        }
 
        // Sets an array of strings in the pool from a single string, returns the offset of that array (in words)
        // <SyntheticSupport/>
        private unsafe uint SetPoolStringArrayFromSingleString(string s, Hashtable offsetTable, ref uint currentOffset)
        { 
            BCLDebug.Assert(s.Length==10,
                "[CultureTableRecord.SetPoolStringArrayFromSingleString]Expect 10 digits in the saNativeDigits string"); 
            String[] strings = new string[s.Length]; 
            for (int i=0; i< s.Length; i++)
                strings[i] = s.Substring(i,1); 
            return SetPoolStringArray(offsetTable, ref currentOffset, strings);
        }

 
        // <SyntheticSupport/>
        private bool NameHasScriptTag(string tempName) 
        { 
            int dashCount = 0;
            int i= 0; 
            while (i<tempName.Length && dashCount<2)
            {
                if (tempName[i] == '-') { dashCount++; }
                i++; 
            }
            return (dashCount>1); 
        } 

 
        //
        // This method construct the culture name (should be called only when we have script tag).
        //
        // <SyntheticSupport/> 
        private static string GetCasedName(string name)
        { 
            StringBuilder taggedName = new StringBuilder(name.Length); 

            int i = 0; 
            while (i<name.Length && name[i] != '-') // Language part
            {
                taggedName.Append(name[i]);
                i++; 
            }
 
            taggedName.Append("-"); // insert two dashs. we are going to insert script in between later 
            i++;
 
            char c;

            // First character in the script is uppercase and the rest is lower case.
            c = Char.ToUpper(name[i], CultureInfo.InvariantCulture); 
            taggedName.Append(c);
            i++; 
 
            while (i<name.Length && name[i] != '-') // Region part
            { 
                taggedName.Append(name[i]);
                i++;
            }
 
            taggedName.Append("-"); // insert two dashs. we are going to insert script in between later
            i++; 
 
            while (i<name.Length ) // rest of the script
            { 
                c = Char.ToUpper(name[i], CultureInfo.InvariantCulture);
                taggedName.Append(c);
                i++;
            } 

            return taggedName.ToString(); 
        } 

 
        // GetQualifiedName will produce a name with the format Lang-Script-Region
        private static string GetQualifiedName(string name)
        {
            StringBuilder taggedName = new StringBuilder(name.Length); 

            int i = 0; 
            while (i<name.Length && name[i] != '-') // Language part 
            {
                taggedName.Append(name[i]); 
                i++;
            }

            taggedName.Append("--"); // insert two dashs. we are going to insert script in between later 
            i++;
 
            int insertionPosition = i; 

            while (i<name.Length && name[i] != '-') // Region part 
            {
                taggedName.Append(name[i]);
                i++;
            } 

            i++; 
 
            while (i<name.Length ) // rest of the script
            { 
                taggedName.Insert(insertionPosition, name[i]);
                insertionPosition++;
                i++;
            } 

            return taggedName.ToString(); 
        } 

 
        // <SyntheticSupport/>
        private static void GetSyntheticParentData(ref CultureData data, ref CompositeCultureData  compositeData)
        {
            compositeData.parentLcid = CultureInfo.InvariantCulture.LCID; 
            compositeData.parentName = CultureInfo.InvariantCulture.Name;
 
            if (data.sParentName != null) 
            {
                // Vista or above 
                string  defaultTableActualName;
                string  parentName;
                int     defaultTableCultureID;
 
                parentName = ValidateCulturePieceToLower(data.sParentName, "ParentName", MAXSIZE_FULLTAGNAME);
                int defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureName(parentName, out defaultTableCultureID, out defaultTableActualName); 
 
                if (defaultTableDataItem >= 0)
                { 
                    compositeData.parentLcid = defaultTableCultureID;
                    compositeData.parentName = defaultTableActualName;
                }
                else if (SyntheticNameToLcidCache[parentName] != null) 
                {
                    compositeData.parentLcid = (int) SyntheticNameToLcidCache[parentName]; 
                    compositeData.parentName = data.sParentName; 
                }
            } 
        }

        // <SyntheticSupport/>
        private static void GetSyntheticConsoleFallback(ref CultureData data, ref CompositeCultureData  compositeData) 
        {
            compositeData.consoleFallbackName = CultureInfo.InvariantCulture.GetConsoleFallbackUICulture().Name; 
 
            if (data.sConsoleFallbackName != null)
            { 
                // Vista or above
                string  defaultTableActualName;
                string  consoleFallbackName;
                int     defaultTableCultureID; 

                consoleFallbackName = ValidateCulturePieceToLower(data.sConsoleFallbackName, "ConsoleFallbackName", MAXSIZE_FULLTAGNAME); 
                int defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureName(consoleFallbackName, out defaultTableCultureID, out defaultTableActualName); 

                if (defaultTableDataItem >= 0) 
                {
                    compositeData.consoleFallbackName = defaultTableActualName;
                }
                else if (SyntheticNameToLcidCache[consoleFallbackName] != null) 
                {
                    compositeData.consoleFallbackName = data.sConsoleFallbackName; 
                } 
            }
        } 


        // Get the pool size of our data in bytes
        // <SyntheticSupport/> 
        private unsafe int GetCultureDataSize(int cultureID, ref CultureData data, ref CompositeCultureData compositeData)
        { 
            int size = sizeof(CultureTableData); 

            Hashtable offsetTable = new Hashtable(30); 

            // Get us odd word aligned so that our DWORD arrays don't get out of alignment
            size += 2;
 
            // CultureTableData.sName (including the hyphen, length, and null termination)
            size += GetPoolStringSize("", offsetTable); 
 
            // try to detect if we are in platform that supports LOCALE_SNAME by calling nativeGetCultureName.
            // if so we don't have to adjust the names. 
            compositeData.sname = CultureInfo.nativeGetCultureName(cultureID, true, false);
            if (compositeData.sname == null)
            {
                AdjustedSyntheticCultureName adjustedNames; 
                GetAdjustedNames(cultureID, out adjustedNames);
                if (adjustedNames != null) 
                { 
                    data.sIso639Language    = adjustedNames.isoLanguage;
/* SSS_WARNINGS_OFF */                    data.sIso3166CountryName= adjustedNames.isoCountry; /* SSS_WARNINGS_ON */ 
                    compositeData.sname     = adjustedNames.sName;
                }
                else
                { 
                    // this part to make the culture name in proper letter casing.
                    string tempName = (string) SyntheticLcidToNameCache[cultureID]; 
                    if ( NameHasScriptTag(tempName) ) 
                    {
                        compositeData.sname = GetCasedName(tempName); 
                    }
                    else
                    {
                        // construct sname in proper letter casing. 
/* SSS_WARNINGS_OFF */                        compositeData.sname = data.sIso639Language + "-" + data.sIso3166CountryName; /* SSS_WARNINGS_ON */
                    } 
                } 
            }
 
/* SSS_WARNINGS_OFF */            compositeData.englishDisplayName = data.sEnglishLanguage + " (" + data.sEnglishCountry + ")"; /* SSS_WARNINGS_ON */
/* SSS_WARNINGS_OFF */            compositeData.sNativeDisplayName = data.sNativeLanguage  + " (" + data.sNativeCountry  + ")"; /* SSS_WARNINGS_ON */

            // CultureTableData.waCalendars 
            AdjustSyntheticCalendars(ref data, ref compositeData);
 
            size += GetPoolStringSize(compositeData.sname,                offsetTable); 
            size += GetPoolStringSize(compositeData.englishDisplayName,   offsetTable);
            size += GetPoolStringSize(compositeData.sNativeDisplayName,   offsetTable); 
            size += GetPoolStringSize(compositeData.waCalendars,          offsetTable);

            // CultureTableData.sParent
            GetSyntheticParentData(ref data, ref compositeData); 
            BCLDebug.Assert(compositeData.parentName != null,
                "[CultureTableRecord::GetCultureDataSize] compositeData.parentName should be valid."); 
 
            size += GetPoolStringSize(compositeData.parentName, offsetTable);
 
            // CultureTableData.sIso639Language
            size += GetPoolStringSize(data.sIso639Language, offsetTable);

            // CultureTableData.sListSeparator 
            size += GetPoolStringSize(data.sListSeparator, offsetTable);
 
            // CultureTableData.sDecimalSeparator 
            size += GetPoolStringSize(data.sDecimalSeparator, offsetTable);
 
            // CultureTableData.sThousandSeparator
            size += GetPoolStringSize(data.sThousandSeparator, offsetTable);

            // CultureTableData.sCurrency 
            size += GetPoolStringSize(data.sCurrency, offsetTable);
 
            // CultureTableData.sMonetaryDecimal 
            size += GetPoolStringSize(data.sMonetaryDecimal, offsetTable);
 
            // CultureTableData.sMonetaryThousand
            size += GetPoolStringSize(data.sMonetaryThousand, offsetTable);

            // CultureTableData.sPositiveSign 
            size += GetPoolStringSize(data.sPositiveSign, offsetTable);
 
            // CultureTableData.sNegativeSign 
            size += GetPoolStringSize(data.sNegativeSign, offsetTable);
 
            // CultureTableData.sAM1159
            size += GetPoolStringSize(data.sAM1159, offsetTable);

            // CultureTableData.sPM2359 
            size += GetPoolStringSize(data.sPM2359, offsetTable);
 
            // CultureTableData.sAbbrevLang 
            size += GetPoolStringSize(data.sAbbrevLang, offsetTable);
 
            // CultureTableData.sEnglishLanguage
            size += GetPoolStringSize(data.sEnglishLanguage, offsetTable);

            // CultureTableData.sNativeLanguage 
            size += GetPoolStringSize(data.sNativeLanguage, offsetTable);
/* SSS_WARNINGS_OFF */ 
            // CultureTableData.sEnglishCountry 
            size += GetPoolStringSize(data.sEnglishCountry, offsetTable);
 
            // CultureTableData.sNativeCountry
            size += GetPoolStringSize(data.sNativeCountry, offsetTable);

            // CultureTableData.sAbbrevCountry 
            size += GetPoolStringSize(data.sAbbrevCountry, offsetTable);
 
            // CultureTableData.sISO3166CountryName 
            size += GetPoolStringSize(data.sIso3166CountryName, offsetTable);
/* SSS_WARNINGS_ON */ 
            // CultureTableData.sIntlMonetarySymbol
            size += GetPoolStringSize(data.sIntlMonetarySymbol, offsetTable);

            // CultureTableData.sEnglishCurrency 
            size += GetPoolStringSize(data.sEnglishCurrency, offsetTable);
 
            // CultureTableData.sNativeCurrency 
            size += GetPoolStringSize(data.sNativeCurrency, offsetTable);
 
            // CultureTableData.sPercent
            size += GetPoolStringSize(CultureInfo.InvariantCulture.NumberFormat.PercentSymbol, offsetTable);

            // CultureTableData.sNaN 
            if (data.sNaN == null)
                data.sNaN = CultureInfo.InvariantCulture.NumberFormat.NaNSymbol; 
            size += GetPoolStringSize(data.sNaN, offsetTable); 

            // CultureTableData.sPositiveInfinity 
            if (data.sPositiveInfinity == null)
                data.sPositiveInfinity = CultureInfo.InvariantCulture.NumberFormat.PositiveInfinitySymbol;
            size += GetPoolStringSize(data.sPositiveInfinity, offsetTable);
 
            // CultureTableData.sNegativeInfinity
            if (data.sNegativeInfinity == null) 
                data.sNegativeInfinity = CultureInfo.InvariantCulture.NumberFormat.NegativeInfinitySymbol; 
            size += GetPoolStringSize(data.sNegativeInfinity, offsetTable);
 
            // CultureTableData.sMonthDay
            size += GetPoolStringSize(CultureInfo.InvariantCulture.DateTimeFormat.MonthDayPattern, offsetTable);

            // CultureTableData.sAdEra 
            size += GetPoolStringSize(CultureInfo.InvariantCulture.DateTimeFormat.GetEraName(0), offsetTable);
 
            // CultureTableData.sAbbrevAdEra 
            size += GetPoolStringSize(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedEraName(0), offsetTable);
 
            // CultureTableData.sConsoleFallbackName
            GetSyntheticConsoleFallback(ref data, ref compositeData);
            BCLDebug.Assert(compositeData.consoleFallbackName != null,
                "[CultureTableRecord::GetCultureDataSize] compositeData.consoleFallbackName should be valid."); 

            size += GetPoolStringSize(compositeData.consoleFallbackName, offsetTable); 
 
            // CultureTableData.saMonthNames
            size += GetPoolStringArraySize(offsetTable, data.saMonthNames); 

            // CultureTableData.saDayNames
            size += GetPoolStringArraySize(offsetTable, data.saDayNames);
 
            // CultureTableData.saAbbrevDayNames
            size += GetPoolStringArraySize(offsetTable, data.saAbbrevDayNames); 
 
            // CultureTableData.saAbbrevMonthNames
            size += GetPoolStringArraySize(offsetTable, data.saAbbrevMonthNames); 

            // CultureTableData.saMonthGenitiveNames
            data.saGenitiveMonthNames[12] = data.saMonthNames[12];
            size += GetPoolStringArraySize(offsetTable, data.saGenitiveMonthNames); 

            // CultureTableData.saAbbrevMonthGenitiveNames 
            data.saAbbrevGenitiveMonthNames[12] = data.saAbbrevMonthNames[12]; 
            size += GetPoolStringArraySize(offsetTable, data.saAbbrevGenitiveMonthNames);
 
            // CultureTableData.saNativeCalendarNames
            size += GetPoolStringArraySize(offsetTable, data.saNativeCalendarNames);

            // CultureTableData.saTimeFormat 
            size += GetPoolStringArraySize(offsetTable, data.saTimeFormat);
 
            // CultureTableData.saShortDate 
            size += GetPoolStringArraySize(offsetTable, data.saShortDate);
 
            // CultureTableData.saLongDate
            size += GetPoolStringArraySize(offsetTable, data.saLongDate);

            // CultureTableData.saYearMonth 
            size += GetPoolStringArraySize(offsetTable, data.saYearMonth);
 
            // used for CultureTableData.saDuration/saDateWords 
            size += GetPoolStringArraySize(offsetTable, "");
 
            // To Do: We need to find a better way to get the alternative sort names.
            // look at ComNldInfo::nativeGetCultureData and CultureTableRecord.FillCultureDataMemory
            // size += GetPoolStringArraySize(offsetTable, data.saAltSortID);
 
            // CultureTableData.saAltSortID and CultureTableData.saDateWords
            size += GetPoolStringArraySize(offsetTable, ""); 
 
            // CultureTableData.waGrouping
            data.waGrouping = GroupSizesConstruction(data.waGrouping); 
            size += GetPoolStringSize(data.waGrouping, offsetTable);

            // CultureTableData.waMonetaryGrouping
            data.waMonetaryGrouping = GroupSizesConstruction(data.waMonetaryGrouping); 
            size += GetPoolStringSize(data.waMonetaryGrouping, offsetTable);
 
            // CultureTableData.saNativeDigits 
            size += GetPoolStringArraySize(data.saNativeDigits, offsetTable);
 
            // CultureTableData.waFontSignature
            size += GetPoolStringSize(data.waFontSignature, offsetTable);
/* SSS_WARNINGS_OFF */
            if (data.sISO3166CountryName2 == null) 
                data.sISO3166CountryName2 = data.sIso3166CountryName;
            size += GetPoolStringSize(data.sISO3166CountryName2, offsetTable); 
/* SSS_WARNINGS_ON */ 
            if (data.sISO639Language2 == null)
                data.sISO639Language2 = data.sIso639Language; 
            size += GetPoolStringSize(data.sISO639Language2, offsetTable);

            if (data.saSuperShortDayNames == null)
                data.saSuperShortDayNames = data.saAbbrevDayNames; 
            size += GetPoolStringArraySize(offsetTable, data.saSuperShortDayNames);
 
            return size; 
        }
 
        // Return the size of a string in the string pool in bytes
        // This needs to have an even # of words so that any following DWORD
        // array doesn't get unaligned
        // <SyntheticSupport/> 
        private int GetPoolStringSize(string s, Hashtable offsetTable)
        { 
            // 0 size if we know about it already 
            int size = 0;
 
            // See if we already knew about it
            if (offsetTable[s] == null)
            {
                offsetTable[s] = ""; 
                size = 2 * (s.Length + 1 +         // adding one for the length
                            (1 - (s.Length & 1)));  // Plus one to keep us odd aligned if necessary 
            } 

            BCLDebug.Assert((size & 3) == 0, 
                "[CultureTableRecord.GetPoolStringSize]Didn't maintain dword alignment");
            return size;
        }
 
        // Writing every character as seperate string in a string array (size of that array & strings in bytes)
        // <SyntheticSupport/> 
        private int GetPoolStringArraySize(string s, Hashtable offsetTable) 
        {
            // We have to do this the slow way just in case one of these strings is duplicated in the string pool 
            BCLDebug.Assert(s.Length==10,
                "[CultureTableRecord.GetPoolStringArraySize(string, int)]Expect 10 digits in the saNativeDigits string");
            String[] strings = new string[s.Length];
            for (int i=0; i< s.Length; i++) 
                strings[i] = s.Substring(i,1);
 
            return GetPoolStringArraySize(offsetTable, strings); 
        }
 
        // Given array of strings we calculate bytes needed to store them. note it can take one string.
        // <SyntheticSupport/>
        private int GetPoolStringArraySize(Hashtable offsetTable, params string [] array)
        { 
            int stringsSize = 0;
 
            for (int i=0; i<array.Length; i++) 
            {
                stringsSize += GetPoolStringSize(array[i], offsetTable); 
            }

            stringsSize += 2 * (array.Length * 2 + 1 + 1);       // (2 words per entry + 1 count word count + 1 word alignment) * 2 bytes/word
 
            BCLDebug.Assert((stringsSize & 3) == 0,
                "[CultureTableRecord.GetPoolStringSize]Didn't maintain dword alignment"); 
 
            return stringsSize;
        } 

        //
        // Group sizes format is single digit followed by a semicolon.
        // 
        // <SyntheticSupport/>
        private string GroupSizesConstruction(string rawGroupSize) 
        { 
            int length = rawGroupSize.Length;
            if ( rawGroupSize[length - 1] == '0' ) 
            {
                // if Win32 returned zero at the end of the string then weneed to remove it out.
                length--;
            } 

            int i=0; 
            StringBuilder groupSize = new StringBuilder(); 
            while (i<length)
            { 
                BCLDebug.Assert(rawGroupSize[i] >= '0' && rawGroupSize[i] <= '9', "[CultureTableRecord::GroupSizesConstruction] invalid group size number.");
                groupSize.Append((char) ((int) rawGroupSize[i] - (int) '0'));

                i++; 

                if (i<length) 
                { 
                    BCLDebug.Assert(rawGroupSize[i] == ';', "[CultureTableRecord::GroupSizesConstruction] group size separator should be semicolon.");
                    i++; 
                }
            }

            if (length == rawGroupSize.Length) 
            {
                // if Win32 didn't return zero at the end of the string then we need to add it. 
                groupSize.Append((char) 0); 
            }
 
            string groupSizesString = groupSize.ToString();
            BCLDebug.Assert(groupSizesString != null && groupSizesString.Length > 0, "[CultureTableRecord::GroupSizesConstruction] null group sizes.");

            return (groupSizesString); 
        }
 
#endif // !FEATURE_PAL 

 
        private String WindowsPath
        {
            get
            { 
                if (m_windowsPath == null)
                { 
                    m_windowsPath = CultureInfo.nativeGetWindowsDirectory(); 
                }
                return (m_windowsPath); 
            }
        }

        /*---------------------------------------------------------- 
        *
        * Gets a filename of a custom culture covered by the given string. 
        * 
        * Builds a cache of items found (adding String.Empty for files that are not present)
        * to avoid hitting the disk repeatedly. 
        *
        *--------------------------------------------------------*/
        private string GetCustomCultureFile(string name)
        { 
#if !FEATURE_PAL
            // Calling SHGetFolderPath is expensive.  Cache whether 
            // we need to load a data file or not. 

            StringBuilder sbFile = new StringBuilder(WindowsPath); 

            sbFile.Append("\\Globalization\\");
            sbFile.Append(name);
            sbFile.Append(".nlp"); 

            // GetFullPath should not be needed here since a full path is being generated 
            string file = (sbFile.ToString()); 

            bool customCultureExists = CultureInfo.nativeFileExists(file); 

            if (customCultureExists)
                return(file);
#endif // !FEATURE_PAL 
            return(null);
        } 
 
        /*----------------------------------------------------------
        * 
        * Validate a culture name -- throws if it is not valid. If it is valid, return
        * the lowercase version of it, suitable for later caching.
        *
        *--------------------------------------------------------*/ 
        private static string ValidateCulturePieceToLower(string testString, string paramName, int maxLength)
        { 
            if(testString.Length > maxLength) 
            {
                throw new ArgumentException( 
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_NameTooLong"), testString, maxLength), paramName);
            }

            StringBuilder sb = new StringBuilder(testString.Length); 

            for(int ich = 0; ich < testString.Length; ich++) { 
                char ch = testString[ich]; 

                if(ch <= 'Z' && ch >= 'A') { 
                    sb.Append((char)(ch - 'A' + 'a'));
                }
                else if(((ch <= 'z' && ch >= 'a') ||
                    (ch <= '9' && ch >= '0') || 
                    (ch == '_') ||
                    (ch == '-'))) { 
                    sb.Append(ch); 
                }
                else { 
                    throw new ArgumentException(
                        String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_NameContainsInvalidCharacters"), testString), paramName);
                }
            } 

            return(sb.ToString()); 
 
        }
 
        internal static string AnsiToLower(string testString)
        {
            StringBuilder sb = new StringBuilder(testString.Length);
 
            for (int ich = 0; ich < testString.Length; ich++)
            { 
                char ch = testString[ich]; 
                sb.Append(ch <= 'Z' && ch >= 'A' ? (char)(ch - 'A' + 'a') : ch);
            } 

            return(sb.ToString());
        }
 
        // <SyntheticSupport/>
        internal bool IsSynthetic { get { return m_synthetic; } } 
 
        internal bool IsCustomCulture
        { 
            get
            {
                // If we came from the assembly we aren't custom
                return !(this.m_CultureTable.fromAssembly); 
            }
        } 
 
        internal bool IsReplacementCulture
        { 
            get
            {
                return ( this.IsCustomCulture && !IsCustomCultureId(m_CultureID) );
            } 
        }
 
        internal int  CultureID 
        {
            get 
            {
                BCLDebug.Assert(this.m_CultureID > 0, "[CultureTableRecord.CultureID]unexpected m_CultureId");
                return this.m_CultureID;
            } 
        }
 
        internal String  CultureName 
        {
            get 
            {
                BCLDebug.Assert(this.m_CultureName != null, "[CultureTableRecord.CultureName]unexpected m_CultureName");
                return this.m_CultureName;
            } 

            set 
            { 
                BCLDebug.Assert(value != null, "[CultureTableRecord.CultureName]Expected non-null value for culture name");
                this.m_CultureName = value; 
            }
        }

        internal bool UseUserOverride 
        {
            get 
            { 
                return this.m_bUseUserOverride;
            } 
        }

        // A property to indicate if we should retrieve information by calling the Win32 GetLocaleInfo().
 
        internal unsafe bool UseGetLocaleInfo
        { 
            get 
            {
                if (!this.m_bUseUserOverride) 
                {
                    return (false);
                }
                int lcid; 
                CultureInfo.nativeGetUserDefaultLCID(&lcid, CultureInfo.LOCALE_USER_DEFAULT);
 
                if (ActualCultureID == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED && 
                    lcid == CultureInfo.LOCALE_CUSTOM_DEFAULT)
                { 
                    if (SNAME.Equals(CultureInfo.nativeGetCultureName(lcid, true, false)))
                    {
                        return true;
                    } 
                    return false;
                } 
 
                return (this.ActualCultureID == lcid);
            } 
        }

        // A method to check if we can use the Win32 GetLocaleInfo() for the specified locale and the specified calenar in this CultureTableRecord.
        // It will be true when all of the following are true: 
        //  * UseUserOverride is true
        //  * UseGetLocaleInfo is true (which means the specified locale is the current user default locale. 
        //  * The specified calendar is the current calendar in the user default locale. 
        //  Parameters:
        //      calID: The calendar ID to be checked. 
        internal unsafe bool UseCurrentCalendar(int calID)
        {
            return (UseGetLocaleInfo && CultureInfo.nativeGetCurrentCalendar() == calID);
        } 

 
        internal bool IsValidSortID(int sortID) 
        {
            BCLDebug.Assert(sortID >= 0 && sortID <= 0xffff, "sortID is invalid");    // SortID is 16-bit positive integer. 

            if(sortID == 0 ||
               (this.SALTSORTID != null &&
                this.SALTSORTID.Length >= sortID && 
                this.SALTSORTID[sortID - 1].Length != 0))
            { 
                return true; 
            }
            else 
            {
                return false;
            }
        } 

        internal CultureTableRecord CloneWithUserOverride(bool userOverride) 
        { 
            if (m_bUseUserOverride == userOverride)
                return this; 

            CultureTableRecord cultureTableRecord = (CultureTableRecord) this.MemberwiseClone();
            cultureTableRecord.m_bUseUserOverride = userOverride;
 
            return cultureTableRecord;
        } 
 
        //
        // CultureNativeDisplayName called when we need to get the native display name for the culture 
        // from Win32 side. we need to do that in cases like synthetic cultures.
        //

        internal unsafe string CultureNativeDisplayName 
        {
            get 
            { 
                int     lcid;
                CultureInfo.nativeGetUserDefaultUILanguage(&lcid); 

                if (CultureInfo.GetLangID(lcid) == CultureInfo.GetLangID(CultureInfo.CurrentUICulture.LCID))
                {
                    string localizedLanguageName = CultureInfo.nativeGetLocaleInfo(m_ActualCultureID, LOCALE_SLANGUAGE); 
                    if (localizedLanguageName != null)
                    { 
                        // check for null terminated character. 
                        if ( localizedLanguageName[localizedLanguageName.Length - 1] == '\u0000' )
                            return localizedLanguageName.Substring(0, localizedLanguageName.Length - 1); 
                        else
                            return localizedLanguageName;
                    }
                } 

                return this.SNATIVEDISPLAYNAME; 
            } 
        }
 
        //
        // RegionNativeDisplayName called when we need to get the native display name for the region
        // from Win32 side. we need to do that in cases like synthetic cultures.
        // 

        internal unsafe string RegionNativeDisplayName 
        { 
            get
            { 
                int     lcid;
                CultureInfo.nativeGetUserDefaultUILanguage(&lcid);

                if (CultureInfo.GetLangID(lcid) == CultureInfo.GetLangID(CultureInfo.CurrentUICulture.LCID)) 
                { /* SSS_WARNINGS_OFF */
                    string localizedCountryName = CultureInfo.nativeGetLocaleInfo(m_ActualCultureID, LOCALE_SCOUNTRY); 
                    if (localizedCountryName  != null) 
                    {
                        if ( localizedCountryName[localizedCountryName.Length - 1] == '\u0000' ) 
                            return localizedCountryName.Substring(0, localizedCountryName.Length - 1);
                        else
                            return localizedCountryName ;
                    } 
                }
 
                return this.SNATIVECOUNTRY; 
            } /* SSS_WARNINGS_ON */
        } 

        ////////////////////////////////////////////////////////////////////////
        //
        //  Equals 
        //
        //  Implements Object.Equals().  Returns a boolean indicating whether 
        //  or not object refers to the same CultureTableRecord as the current instance. 
        //
        //////////////////////////////////////////////////////////////////////// 
        public override unsafe bool Equals(Object value)
        {
            CultureTableRecord that = value as CultureTableRecord;
            return (that != null) && 
                    (this.m_pData == that.m_pData &&
                     this.m_bUseUserOverride == that.m_bUseUserOverride && 
                     this.m_CultureID == that.m_CultureID && 
                     CultureInfo.InvariantCulture.CompareInfo.Compare(
                        this.m_CultureName, that.m_CultureName, CompareOptions.IgnoreCase ) == 0 && 
                     this.m_CultureTable.Equals(that.m_CultureTable)
                    );
        }
 
        ////////////////////////////////////////////////////////////////////////
        // 
        //  GetHashCode 
        //
        //  Implements Object.GetHashCode().  Returns the hash code for the 
        //  CultureInfo.  The hash code is guaranteed to be the same for RegionInfo
        //  A and B where A.Equals(B) is true.
        //
        //////////////////////////////////////////////////////////////////////// 
        public override int GetHashCode()
        { 
            //This doesn't tell apart user override from non-user override 
            if (!IsCustomCultureId(m_CultureID))
                return (this.m_CultureID); 

            return (this.m_CultureName.GetHashCode());
        }
 
        // Get a String
        private unsafe String GetString(uint iOffset) 
        { 
            char* pCharValues = unchecked((char*)(this.m_pPool + iOffset));
            // For null strings, iOffset, pPool[0] and pPool[1] are all three 0. 
            // The previous implimentation used [1] to test and I was afraid to change it.
            if (pCharValues[1] == 0)
            {
                BCLDebug.Assert(iOffset == 0, 
                    "[CultureTableRecord.GetString]Expected empty strings to have 0 offset");
                return String.Empty; 
            } 
            return new String(pCharValues + 1, 0, (int)pCharValues[0]);
        } 

        private int InteropLCID
        {
            get 
            {
                return ActualCultureID == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED ? 
                    CultureInfo.LOCALE_CUSTOM_DEFAULT : ActualCultureID; 
            }
        } 

        private String GetOverrideString(uint iOffset, int iWindowsFlag)
        {
#if !FEATURE_PAL 
            if (this.UseGetLocaleInfo)
            { 
                // Get User Override value if necessary 
                String value = CultureInfo.nativeGetLocaleInfo(InteropLCID, iWindowsFlag);
                if (value != null && value.Length > 0) 
                    return value;
            }
#endif
            return GetString(iOffset); 
        }
 
        private unsafe String[] GetStringArray(uint iOffset) 
        {
            if (iOffset == 0) return new String[0]; 

            // The offset value is in char, and is related to the begining of string pool.
            ushort* pCount = m_pPool + iOffset;
            int count = (int)pCount[0];    // The number of strings in the array 
            BCLDebug.Assert(count != 0,
                "[CultureTableRecord.GetStringArray]Expected non-zero length array"); 
            String[] values = new String[count]; 

            // Get past count and cast to uint 
            uint* pStringArray = (uint*)(pCount + 1);

            // Get our strings
            for (int i = 0; i < count; i++) 
                values[i] = GetString(pStringArray[i]);
 
            return (values); 
        }
 
        // Get first string in this array of strings
        private unsafe String GetStringArrayDefault(uint iOffset)
        {
            if (iOffset == 0) 
                return String.Empty;
 
            // The offset value is in char, and is related to the begining of string pool. 
            ushort* pCount = m_pPool + iOffset;
            BCLDebug.Assert(pCount[0] != 0, 
                "[CultureTableRecord.GetStringArrayDefault]Expected non-zero length array");

            // Get past count and cast to uint
            uint* pStringArray = (uint*)(pCount + 1); 

            // We had strings, return the first one 
            return GetString(pStringArray[0]); 
        }
 
        // Get the user override or the first array of this string array
        private String GetOverrideStringArrayDefault(uint iOffset, int iWindowsFlag)
        {
#if !FEATURE_PAL 
            if (this.UseGetLocaleInfo)
            { 
                // Get User Override value if necessary 
                String value = CultureInfo.nativeGetLocaleInfo(InteropLCID, iWindowsFlag);
                if (value != null && value.Length > 0) 
                    return value;
            }
#endif
            // If override wasn't available, return the table version 
            return GetStringArrayDefault(iOffset);
        } 
 
        private ushort GetOverrideUSHORT(ushort iData, int iWindowsFlag)
        { 
#if !FEATURE_PAL
            if (this.UseGetLocaleInfo)
            {
                // Get User Override value if necessary 
                String value = CultureInfo.nativeGetLocaleInfo(InteropLCID, iWindowsFlag);
                if (value != null && value.Length > 0) 
                { 
                    // Now we need an integer for this string.
                    Int16 intValue; 
                    if (Int16.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out intValue))
                        return unchecked((ushort)intValue);
                }
            } 
#endif
            return iData; 
        } 

        private unsafe int[] GetWordArray(uint iData) 
        {
            if (iData == 0)
                return new int[0];
 
            ushort* pWord = this.m_pPool + iData;
            int count = (int)pWord[0];          // The number of words in the array 
            BCLDebug.Assert(count != 0, 
                "[CultureTableRecord.GetWordArray]Expected non-zero length array");
 
            int[] values = new int[count];
            pWord++;                            // Get past count
            for (int i = 0; i < count; i++) {
                values[i] = pWord[i]; 
            }
            return (values); 
        } 

        private int[] GetOverrideGrouping(uint iData, int iWindowsFlag) 
        {
#if !FEATURE_PAL
            if (this.UseGetLocaleInfo)
            { 
                // Get User Override value if necessary
                String value = CultureInfo.nativeGetLocaleInfo(InteropLCID, iWindowsFlag); 
                if (value != null && value.Length > 0) 
                {
                    // Got a grouping, need to convert it. 
                    int[] values = ConvertWin32GroupString(value);
                    if (values != null)
                    {
                        // If non-null value are found, retured it. 
                        // Otherwise, fallback to default values.
                        return (values); 
                    } 
                }
            } 
#endif
            // No Override, use it from the tables.
            return GetWordArray(iData);
        } 

        // The actual LCID, used when a name lookup leads to a custom sort (thus 
        // 'de-DE-deudi' will be 0x10407 rather than the plain old 0x0407 of 'de-DE'). 
        internal int ActualCultureID {
            get { 
                if(0 == this.m_ActualCultureID) {
                    this.m_ActualCultureID = this.ILANGUAGE;
                }
 
                return(this.m_ActualCultureID);
            } 
        } 

        // The actual name, used when an LCID lookup leads to a custom sort (thus will be 
        // 0x10407 will be 'de-DE-deudi' rather than the plain old 'de-DE' of 0x0407).
        internal string ActualName {
            get {
                if(null == this.m_ActualName) { 
                    this.m_ActualName = this.SNAME;
                } 
 
                return(this.m_ActualName);
            } 
        }

        internal bool IsNeutralCulture {
            get { 
                return ((IFLAGS & (ushort)CultureFlags.IsSpecificCulture) == 0);
            } 
        } 

        private bool IsOptionalCalendar(int calendarId) 
        {
            for (int i=0; i<IOPTIONALCALENDARS.Length; i++)
            {
                if (IOPTIONALCALENDARS[i] == calendarId) 
                    return true;
            } 
 
            return false;
        } 

        ////////////////////////////////////////////////////////////////////////
        //
        //  All the accessors 
        //
        //  Accessors for our data object items 
        // 
        ////////////////////////////////////////////////////////////////////////
 
        // These ones allow user override
        // Integers
        internal unsafe ushort IDIGITS                  { get { return (this.m_pData->iDigits);} }                   // (user can override) number of fractional digits
        internal unsafe ushort INEGNUMBER               { get { return (this.m_pData->iNegativeNumber); } }          // (user can override) negative number format 
        internal unsafe ushort ICURRDIGITS              { get { return (this.m_pData->iCurrencyDigits); } }          // (user can override) # local monetary fractional digits
        internal unsafe ushort ICURRENCY                { get { return (this.m_pData->iCurrency); } }                // (user can override) positive currency format 
        internal unsafe ushort INEGCURR                 { get { return (this.m_pData->iNegativeCurrency); } }        // (user can override) negative currency format 
//        internal unsafe ushort ILEADINGZEROS            { get { return GetOverrideUSHORT(this.m_pData->iLEADINGZEROS,      CultureTableData.LOCALE_ILEADINGZEROS); } }            // (user can override) leading zeros 0  leading zeros, 1 ading zeros
        internal unsafe ushort ICALENDARTYPE 
        {
            get
            {
#if !FEATURE_PAL 
                // Expand the code of GetOverrideUSHORT() since we don't want to create an int array (IOPTIONALCALENDARS) from every time that we call GetOverrideUSHORT().
                if (this.m_bUseUserOverride) 
                { 
                    // Get User Override value if necessary
                    String value = CultureInfo.nativeGetLocaleInfo(ActualCultureID, CultureTableRecord.LOCALE_ICALENDARTYPE); 
                    if (value != null && value.Length > 0)
                    {
                        // Now we need an integer for this string.
                        Int16 intValue; 
                        if (Int16.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out intValue) &&
                            IsOptionalCalendar((int)intValue)) 
                            return unchecked((ushort)intValue); 
                    }
                } 
#endif // !FEATURE_PAL
                return ((ushort)IOPTIONALCALENDARS[0]);
            }
        } 
        internal unsafe ushort IFIRSTWEEKOFYEAR         { get { return GetOverrideUSHORT(this.m_pData->iFirstWeekOfYear,     CultureTableData.LOCALE_IFIRSTWEEKOFYEAR); } }         // (user can override) first week of year
/* SSS_WARNINGS_OFF *///        internal unsafe ushort ICOUNTRY                 { get { return GetOverrideUSHORT(this.m_pData->iCOUNTRY,           CultureTableData.LOCALE_ICOUNTRY); } }                 // (user can override) country code (RegionInfo) /* SSS_WARNINGS_ON */ 
        internal unsafe ushort IMEASURE                 { get { return GetOverrideUSHORT(this.m_pData->iMeasure,             CultureTableData.LOCALE_IMEASURE); } }                 // (user can override) system of measurement 0ric, 1(RegionInfo) 
        internal unsafe ushort IDIGITSUBSTITUTION       { get { return GetOverrideUSHORT(this.m_pData->iDigitSubstitution,   CultureTableData.LOCALE_IDIGITSUBSTITUTION); } }       // (user can override) Digit substitution 0text, 1e/arabic, 2ive/national (2 seems to be unused)
 
        // Grouping
        internal unsafe int[]  SGROUPING                { get { return GetOverrideGrouping(this.m_pData->waGrouping,         CultureTableData.LOCALE_SGROUPING); } }                   // (user can override) grouping of digits
        internal unsafe int[]  SMONGROUPING             { get { return GetOverrideGrouping(this.m_pData->waMonetaryGrouping, CultureTableData.LOCALE_SMONGROUPING); } }           // (user can override) monetary grouping of digits
 
        // Strings
        internal unsafe String SLIST                    { get { return GetOverrideString(this.m_pData->sListSeparator,       CultureTableData.LOCALE_SLIST); } }                    // (user can override) list Separator 
        internal unsafe String SDECIMAL                 { get { return GetString(this.m_pData->sDecimalSeparator); } }                 // (user can override) decimal Separator 
        internal unsafe String STHOUSAND                { get { return GetString(this.m_pData->sThousandSeparator); } }                // (user can override) thousands Separator
        internal unsafe String SCURRENCY                { get { return GetString(this.m_pData->sCurrency); } }                  // (user can override) local monetary symbol 
        internal unsafe String SMONDECIMALSEP           { get { return GetString(this.m_pData->sMonetaryDecimal); } }           // (user can override) monetary decimal Separator
        internal unsafe String SMONTHOUSANDSEP          { get { return GetString(this.m_pData->sMonetaryThousand); } }          // (user can override) monetary thousands separator
        internal unsafe String SNEGATIVESIGN            { get { return GetString(this.m_pData->sNegativeSign); } }              // (user can override) negative sign
        internal unsafe String S1159                    { get { return GetString(this.m_pData->sAM1159); } }                    // (user can override) AM designator 
        internal unsafe String S2359                    { get { return GetString(this.m_pData->sPM2359); } }                    // (user can override) PM designator
 
        // String array DEFAULTS 
        // Note: GetDTFIOverrideValues does the user overrides for these, so we don't have to.
        internal unsafe String STIMEFORMAT              { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saTimeFormat)); } }       // (user can override) time format 
        internal unsafe String SSHORTTIME               { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saShortTime)); } }        // short time format
        internal unsafe String SSHORTDATE               { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saShortDate)); } }        // (user can override) short date format
        internal unsafe String SLONGDATE                { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saLongDate)); } }         // (user can override) long date format
        internal unsafe String SYEARMONTH               { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saYearMonth)); } }        // (user can override) year/month format 
        internal unsafe String SMONTHDAY                { get { return ReescapeWin32String(GetString(this.m_pData->sMonthDay)); } }                      // month/day format (single string, no override)
 
        // String arrays 
        internal unsafe String[] STIMEFORMATS           { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saTimeFormat)); } }              // (user can override) time format
        internal unsafe String[] SSHORTTIMES            { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saShortTime)); } }               // short time format 
        internal unsafe String[] SSHORTDATES            { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saShortDate)); } }               // (user can override default only) short date format
        internal unsafe String[] SLONGDATES             { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saLongDate)); } }                // (user can override default only) long date format
        internal unsafe String[] SYEARMONTHS            { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saYearMonth)); } }               // (user can override) date year/month format.  (9x doesn't support override)
 
        internal unsafe String[] SNATIVEDIGITS          {                                                          // (user can override) native characters for digits 0-9
            get { 
 
#if !FEATURE_PAL
                string[] values; 

                if (this.m_bUseUserOverride)
                {
                    String value; 

                    // Get User Override value if it exists and convert it to a string array 
                    // If the user default is a custom culture for OS then skip this step. 
                    if((this.CultureID != CultureInfo.LOCALE_CUSTOM_DEFAULT) &&
                        null != (value = CultureInfo.nativeGetLocaleInfo(this.ActualCultureID, CultureTableData.LOCALE_SNATIVEDIGITS)) && 
                        (value.Length == 10))
                    {
                        values = new string[10];
                        for(int i = 0; i < value.Length; i++) 
                        {
                            values[i] = value[i].ToString(CultureInfo.InvariantCulture); 
                        } 

                        return(values); 
                    }
                }
#endif // !FEATURE_PAL
 
                return GetStringArray(this.m_pData->saNativeDigits);
            } 
        } 

        // Integer ones are all pretty trivial 
        internal unsafe ushort ILANGUAGE                { get { return this.m_pData->iLanguage; } }                //
//        internal unsafe ushort IDEFAULTLANGUAGE         { get { return this.m_pData->iDEFAULTLANGUAGE; } }         // Default language if this is a rare lcid (Windows Only)
        internal unsafe ushort IDEFAULTANSICODEPAGE     { get { return this.m_pData->iDefaultAnsiCodePage; } }     // default ansi code page ID (ACP)
        internal unsafe ushort IDEFAULTOEMCODEPAGE      { get { return this.m_pData->iDefaultOemCodePage; } }      // default oem code page ID (OCP or OEM) 
        internal unsafe ushort IDEFAULTMACCODEPAGE      { get { return this.m_pData->iDefaultMacCodePage; } }      // default macintosh code page
        internal unsafe ushort IDEFAULTEBCDICCODEPAGE   { get { return this.m_pData->iDefaultEbcdicCodePage; } }   // default EBCDIC code page 
        internal unsafe ushort IGEOID                   { get { return this.m_pData->iGeoId; } }                   // GeoId (RegionInfo) 
//        internal unsafe ushort IPAPERSIZE               { get { return this.m_pData->iPAPERSIZE; } }               // default paper size (RegionInfo)
//        internal unsafe ushort IINTLCURRENCYDIGITS      { get { return this.m_pData->iINTLCURRENCYDIGITS; } }      // # of digits after decimal in intl currency format (Windows Only) 
        internal unsafe ushort INEGATIVEPERCENT         { get { return this.m_pData->iNegativePercent; } }         //
        internal unsafe ushort IPOSITIVEPERCENT         { get { return this.m_pData->iPositivePercent; } }         //
        internal unsafe ushort IPARENT                  { get { return this.m_pData->iParent; } }                  //
        internal unsafe ushort ILINEORIENTATIONS        { get { return this.m_pData->iLineOrientations; } }        // 
        internal unsafe uint   ICOMPAREINFO             { get { return this.m_pData->iCompareInfo; } }             //
        internal unsafe uint   IFLAGS                   { get { return this.m_pData->iFlags; } }                   // Flags for culture 
 
        // OptionalCalendars
        internal unsafe int[]  IOPTIONALCALENDARS       { get { return GetWordArray(this.m_pData->waCalendars); } }    // additional calendar type(s), semicolon seperated, ie: '1;6' 

        // Strings
        internal unsafe String SNAME                    { get { return GetString(this.m_pData->sName); } }                    //
        internal unsafe String SABBREVLANGNAME          { get { return GetString(this.m_pData->sAbbrevLang); } }              // abbreviated language name 
        internal unsafe String SISO639LANGNAME          { get { return GetString(this.m_pData->sISO639Language); } }          //
//        internal unsafe String SENGLISHLANGUAGE         { get { return GetString(this.m_pData->sENGLISHLANGUAGE); } }       // English name for this language (Windows Only) 
//        internal unsafe String SNATIVELANGUAGE          { get { return GetString(this.m_pData->sNATIVELANGUAGE); } }        // Native name of this language (Windows Only) 
/* SSS_WARNINGS_OFF */        internal unsafe String SENGCOUNTRY              { get { return GetString(this.m_pData->sEnglishCountry); } }          // english country name (RegionInfo)
        internal unsafe String SNATIVECOUNTRY           { get { return GetString(this.m_pData->sNativeCountry); } }           // native country name (RegionInfo) 
        internal unsafe String SABBREVCTRYNAME          { get { return GetString(this.m_pData->sAbbrevCountry); } }           // abbreviated country name (RegionInfo)
        internal unsafe String SISO3166CTRYNAME         { get { return GetString(this.m_pData->sISO3166CountryName); } }      // (RegionInfo)
/* SSS_WARNINGS_ON */        internal unsafe String SINTLSYMBOL              { get { return GetString(this.m_pData->sIntlMonetarySymbol); } }      // international monetary symbol (RegionInfo)
        internal unsafe String SENGLISHCURRENCY         { get { return GetString(this.m_pData->sEnglishCurrency); } }         // English name for this currency (RegionInfo) 
        internal unsafe String SNATIVECURRENCY          { get { return GetString(this.m_pData->sNativeCurrency); } }          // Native name for this currency (RegionInfo)
        internal unsafe String SENGDISPLAYNAME          { get { return GetString(this.m_pData->sEnglishDisplayName); } }      // 
        internal unsafe String SISO639LANGNAME2         { get { return GetString(this.m_pData->sISO639Language2); } }          // 
        internal unsafe String SNATIVEDISPLAYNAME
        { 
            get
            {
/* SSS_WARNINGS_OFF */                // Special case for Taiwan. /* SSS_WARNINGS_ON */
                if (CultureInfo.GetLangID(ActualCultureID) == 0x0404 && 
                    CultureInfo.GetLangID(CultureInfo.InstalledUICulture.LCID) == 0x0404 &&
                    !IsCustomCulture) 
                { 
                    return (CultureInfo.nativeGetLocaleInfo(0x0404, LOCALE_SNATIVELANGNAME) + " (" + CultureInfo.nativeGetLocaleInfo(0x0404, LOCALE_SNATIVECTRYNAME) + ")");
                } 
                return GetString(this.m_pData->sNativeDisplayName);
            }
        }       //
 
        internal unsafe String SPERCENT                 { get { return GetString(this.m_pData->sPercent); } }                 //
        internal unsafe String SNAN                     { get { return GetString(this.m_pData->sNaN); } }                     // 
        internal unsafe String SPOSINFINITY             { get { return GetString(this.m_pData->sPositiveInfinity); } }        // 
        internal unsafe String SNEGINFINITY             { get { return GetString(this.m_pData->sNegativeInfinity); } }        //
        internal unsafe String SADERA                   { get { return GetString(this.m_pData->sAdEra); } }                   // localized names for the A.D. Era 
        internal unsafe String SABBREVADERA             { get { return GetString(this.m_pData->sAbbrevAdEra); } }             // abbreviated localized names for the A.D. Era
/* SSS_WARNINGS_OFF */        internal unsafe String SISO3166CTRYNAME2        { get { return GetString(this.m_pData->sISO3166CountryName2); } }     // (RegionInfo) /* SSS_WARNINGS_ON */
        internal unsafe String SREGIONNAME              { get { return GetString(this.m_pData->sRegionName); } }              // (RegionInfo)
        internal unsafe String SPARENT                  { get { return GetString(this.m_pData->sParent); } }                  // 
        internal unsafe String SCONSOLEFALLBACKNAME     { get { return GetString(this.m_pData->sConsoleFallbackName); } }
        internal unsafe String SSPECIFICCULTURE         { get { return GetString(this.m_pData->sSpecificCulture); } } 
 
        // String Arrays
        internal unsafe String[] SDAYNAMES                { get { return GetStringArray(this.m_pData->saDayNames); } }                // day names 
        internal unsafe String[] SABBREVDAYNAMES          { get { return GetStringArray(this.m_pData->saAbbrevDayNames); } }          // abbreviated day names
        internal unsafe String[] SSUPERSHORTDAYNAMES      { get { return GetStringArray(this.m_pData->saSuperShortDayNames); } }      // one letter day names
        internal unsafe String[] SMONTHNAMES              { get { return GetStringArray(this.m_pData->saMonthNames); } }              // month names
        internal unsafe String[] SABBREVMONTHNAMES        { get { return GetStringArray(this.m_pData->saAbbrevMonthNames); } }        // abbreviated month names 
        internal unsafe String[] SMONTHGENITIVENAMES      { get { return GetStringArray(this.m_pData->saMonthGenitiveNames); } }      //
        internal unsafe String[] SABBREVMONTHGENITIVENAMES{ get { return GetStringArray(this.m_pData->saAbbrevMonthGenitiveNames); } }// 
        internal unsafe String[] SNATIVECALNAMES          { get { return GetStringArray(this.m_pData->saNativeCalendarNames); } }     // Native calendar names.  index of optional calendar - 1, empty if no optional calendar at that number 
        internal unsafe String[] SDATEWORDS               { get { return GetStringArray(this.m_pData->saDateWords); } }               //
        internal unsafe String[] SALTSORTID               { get { return GetStringArray(this.m_pData->saAltSortID); } }               // The array of alternate sort names 

        // Fontsignature
//        internal unsafe ushort FONTSIGNATURE            { get { return this.m_pData->waFONTSIGNATURE; } }            // Font signature (16 WORDS) (Windows Only)
 
        // DateTimeFormatFlags
        internal unsafe DateTimeFormatFlags IFORMATFLAGS{ get { return (DateTimeFormatFlags)this.m_pData->iFormatFlags; } }   // 
 
        // Special handling required for these fields
        // (user can override) positive sign.  We use "+" if empty (windows data is usually empty) 
        internal unsafe String SPOSITIVESIGN
        {
            get
            { 
                String strTemp = GetString(this.m_pData->sPositiveSign);
                if (strTemp == null || strTemp.Length == 0) strTemp = "+"; 
                return strTemp; 
            }
        } 

        internal static bool IsCustomCultureId(int cultureId)
        {
            if (cultureId == CultureInfo.LOCALE_CUSTOM_DEFAULT || cultureId == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED) 
                return true;
 
            return false; 
        }
 
        private unsafe ushort ConvertFirstDayOfWeekMonToSun(int iTemp)
        {
            // Convert Mon-Sun to Sun-Sat format
            if (iTemp < 0 || iTemp > 6) { 
                // If invalid data exist in registry, assume
                // the first day of week is Monday. 
                iTemp = 1; 
            } else {
                if (iTemp == 6) { 
                    iTemp = 0;
                } else {
                    iTemp++;
                } 
            }
            return unchecked((ushort)iTemp); 
        } 

        // (user can override) first day of week (0 is Sunday) 
        internal unsafe ushort IFIRSTDAYOFWEEK
        {
            get
            { 
                return this.m_pData->iFirstDayOfWeek;
            } 
        } 

        internal unsafe ushort IINPUTLANGUAGEHANDLE 
        {
            get
            {
                // Remember this returns SPANISH_INTERNATIONAL_SORT even 
                // in the deprecated case.
                return (this.m_pData->iInputLanguageHandle); 
            } 
        }
 
        internal unsafe ushort ITEXTINFO
        {
            get
            { 
                ushort textInfo = this.m_pData->iTextInfo;
 
                // Need to return SPANISH_TRADITIONAL_SORT even if we're faking it 
                // (Hack because SPANISH_TRADITIONAL_SORT isn't in the table)
                if (this.CultureID == (ushort)SPANISH_TRADITIONAL_SORT) 
                    textInfo = (ushort)SPANISH_TRADITIONAL_SORT;

                // Make sure custom culture and unknown get something
                if (textInfo == CultureInfo.LOCALE_CUSTOM_DEFAULT || textInfo == 0) 
                    textInfo = CultureInfo.LOCALE_INVARIANT;
                return textInfo; 
             } 
         }
 
#if !FEATURE_PAL
        // If we get a group from the registry, then its in 3;0 format with the 0 backwards
        // of how NLS uses it (ie: if the string has a 0, then the int[] shouldn't and vice versa)
        static private int[] ConvertWin32GroupString(String win32Str) 
        {
            // None of these cases make any sense 
            if (win32Str == null || win32Str.Length == 0 || win32Str[0] == '0') { 
                return (new int[] {3});
            } 

            // Since its in n;n;n;n;n format, we can always get the length quickly
            int[] values;
            if (win32Str[win32Str.Length - 1] == '0') 
            {
                // Trailing 0 gets dropped. 1;0 -> 1 
                values = new int[(win32Str.Length / 2)]; 
            }
            else 
            {
                // Need extra space for trailing zero 1 -> 1;0
                values = new int[(win32Str.Length / 2) + 2];
                values[values.Length - 1] = 0; 
            }
 
            int i; 
            int j;
            for (i = 0, j = 0; i < win32Str.Length && j < values.Length; i += 2, j++) 
            {
                // Note that this # shouldn't ever be zero, 'cause 0 is only at end
                // But we'll test because its registry that could be anything
                if (win32Str[i] < '1' || win32Str[i] > '9') 
                    return new int[] {3};
 
                values[j] = (int)(win32Str[i] - '0'); 
            }
 
            return (values);
        }
#endif
 
        ////////////////////////////////////////////////////////////////////////////
        // 
        // Unescape a Win32 style quote string 
        //
        // This is also the escaping style used by custom culture data files 
        //
        // This removes the 'fred' and 'fred''s' windows quoted formatting from a string.
        // The output string will NOT have ANY escaping.  Currently its used for
        // separators, where the output string has no characters with special meaning 
        //
        // We don't build the stringbuilder unless we find a '.  If we find a ', we 
        // always build a stringbuilder because we need to remove the '. 
        //
        //////////////////////////////////////////////////////////////////////////// 
        static private String UnescapeWin32String(String str, int start, int end)
        {
            StringBuilder result = null;
 
            bool inQuote = false;
            for (int i = start; i < str.Length && i <= end; i++) 
            { 
                // Look for quote
                if (str[i] == '\'') 
                {
                    // Already in quote?
                    if (inQuote)
                    { 
                        BCLDebug.Assert(result != null, "[CultureTable.UnescapeWin32String]Expect result to be non-null");
                        // See another single quote.  Is this '' of 'fred''s' or ending quote? 
                        if (i + 1 < str.Length) 
                        {
                            if (str[i+1] == '\'') 
                            {
                                // Append a ' and keep going (so we don't turn off quote mode)
                                result.Append('\'');
                                i++; 
                                continue;
                            } 
                        } 

                        inQuote = false; 
                    }
                    else
                    {
                        // Found beginning quote, remove it. 
                        inQuote = true;
                        if (result == null) 
                            result = new StringBuilder(str, start, i - start, str.Length); 
                    }
                } 
                else
                {
                    // If we have a builder we need to add our non-quote char
                    if (result != null) 
                        result.Append(str[i]);
                } 
            } 

            // No ', just return input string substring 
            if (result == null)
                return (str.Substring(start, end-start + 1));

            // Had ', need to use the builder 
            return (result.ToString());
        } 
 

        //////////////////////////////////////////////////////////////////////////// 
        //
        // Reescape a Win32 style quote string as a NLS+ style quoted string
        //
        // This is also the escaping style used by custom culture data files 
        //
        // NLS+ uses \ to escape the next character, whether in a quoted string or 
        // not, so we always have to change \ to \\. 
        //
        // NLS+ uses \' to escape a quote inside a quoted string so we have to change 
        // '' to \' (if inside a quoted string)
        //
        // We don't build the stringbuilder unless we find something to change
        //////////////////////////////////////////////////////////////////////////// 
        static private String ReescapeWin32String(String str)
        { 
            // If we don't have data, then don't try anything 
            if (str == null)
                return null; 

            StringBuilder result = null;

            bool inQuote = false; 
            for (int i = 0; i < str.Length; i++)
            { 
                // Look for quote 
                if (str[i] == '\'')
                { 
                    // Already in quote?
                    if (inQuote)
                    {
                        // See another single quote.  Is this '' of 'fred''s' or '''', or is it an ending quote? 
                        if (i + 1 < str.Length && str[i+1] == '\'')
                        { 
                            // Found another ', so we have ''.  Need to add \' instead. 
                            // 1st make sure we have our stringbuilder
                            if (result == null) 
                                result = new StringBuilder(str, 0, i, str.Length * 2);

                            // Append a \' and keep going (so we don't turn off quote mode)
                            result.Append("\\'"); 
                            i++;
                            continue; 
                        } 

                        // Turning off quote mode, fall through to add it 
                        inQuote = false;
                    }
                    else
                    { 
                        // Found beginning quote, fall through to add it
                        inQuote = true; 
                    } 
                }
                // Is there a single \ character? 
                else if (str[i] == '\\')
                {
                    // Found a \, need to change it to \\
                    // 1st make sure we have our stringbuilder 
                    if (result == null)
                        result = new StringBuilder(str, 0, i, str.Length * 2); 
 
                    // Append our \\ to the string & continue
                    result.Append("\\\\"); 
                    continue;
                }

                // If we have a builder we need to add our character 
                if (result != null)
                    result.Append(str[i]); 
            } 

            // Unchanged string? , just return input string 
            if (result == null)
                return str;

            // String changed, need to use the builder 
            return result.ToString();
        } 
 
        static private String[] ReescapeWin32Strings(String[] array)
        { 
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                { 
                    array[i] = ReescapeWin32String(array[i]);
                } 
            } 

            return array; 
        }

        internal unsafe String STIME
        { 
            get
            { 
                // Compute SDATE from STIMEFORMAT 
                String timeFormat = GetOverrideStringArrayDefault(this.m_pData->saTimeFormat, CultureTableData.LOCALE_STIMEFORMAT);
                return GetTimeSeparator(timeFormat); 
            }
        }

        internal unsafe String SDATE 
        {
            get 
            { 
                // Compute SDATE from SSHORTDATE
                String shortDate = GetOverrideStringArrayDefault(this.m_pData->saShortDate, CultureTableData.LOCALE_SSHORTDATE); 
                return GetDateSeparator(shortDate);
            }
        }
 
        static private String GetTimeSeparator(String format)
        { 
            // Time format separator (ie: : in 12:39:00) 
            //
            // We calculate this from the provided time format 
            //

            //
            //  Find the time separator so that we can pretend we know STIME. 
            //
            String strUse = String.Empty; 
            int count = 0; 
            int separatorStart = -1;
 
            // Look through the whole string
            for (count = 0; count < format.Length; count++)
            {
                // See if we have Hhms 
                if (format[count] == 'H' || format[count] == 'h' || format[count] == 'm' || format[count] == 's')
                { 
                    // Found a time part, find out when it changes 
                    char cFound = format[count];
 
                    for (count++; count < format.Length && format[count] == cFound; count++)
                    {
                        // Done
                    } 

                    // Did we find anything? 
                    if (count < format.Length) 
                    {
                        // We found start of separator 
                        separatorStart = count;
                    }

                    // In either case we changed hms types, so we found one or we didn't, but we need to stop 
                    break;
                } 
 
                // If it was quotes, ignore quoted stuff
                if (format[count] == '\'') 
                {
                    //
                    //  Ignore quotes.
                    // 

                    for (count++; count < format.Length && (format[count] != '\''); count++) 
                    { 
                        // Done
                    } 

                    // Don't go past end of string
                }
 
                // Advance to next char (skipping unknown char or last quote)
            } 
 
            // Now we need to find the end of the separator
            if (separatorStart != -1) 
            {
                for (count = separatorStart; count < format.Length; count++)
                {
                    // See if we have Hhms 
                    if (format[count] == 'H' || format[count] == 'h' || format[count] == 'm' || format[count] == 's')                    {
                        // Found a time part, stop, we can look for our separator 
                        // From [separatorStart, count) is our string, except we don't want ''s 
                        strUse = UnescapeWin32String(format, separatorStart, count - 1);
                        break; 
                    }

                    // If it was quotes, ignore quoted stuff
                    if (format[count] == '\'') 
                    {
                        // 
                        //  Ignore quotes. 
                        //
                        for (count++; count < format.Length && (format[count] != '\''); count++) 
                        {
                            // Done
                        }
 
                        // Don't go past end of string
                    } 
 
                    // Advance to next char (skipping unknown char or last quote)
                } 
            }

            // Return the one we're using
            return strUse; 
        }
 
        static private String GetDateSeparator(String format) 
        {
            // Date format separator (ie: / in 9/1/03) 
            //
            // We calculate this from the provided short date
            //
 
            //
            //  Find the date separator so that we can pretend we know SDATE. 
            // 
            String strUse = String.Empty;
            int count = 0; 
            int separatorStart = -1;

            // Look through the whole string
            for (count = 0; count < format.Length; count++) 
            {
                // See if we have dyM 
                if (format[count] == 'd' || format[count] == 'y' || format[count] == 'M') 
                {
                    // Found a time part, find out when it changes 
                    char cFound = format[count];

                    for (count++; count < format.Length && format[count] == cFound; count++)
                    { 
                        // Done
                    } 
 
                    // Did we find anything?
                    if (count < format.Length) 
                    {
                        // We found start of separator
                        separatorStart = count;
                    } 

                    // In either case we changed dyM types, so we found one or we didn't, but we need to stop 
                    break; 
                }
 
                // If it was quotes, ignore quoted stuff
                if (format[count] == '\'')
                {
                    // 
                    //  Ignore quotes.
                    // 
 
                    for (count++; count < format.Length && (format[count] != '\''); count++)
                    { 
                        // Done
                    }

                    // Don't go past end of string 
                }
 
                // Advance to next char (skipping unknown char or last quote) 
            }
 
            // Now we need to find the end of the separator
            if (separatorStart != -1)
            {
                for (count = separatorStart; count < format.Length; count++) 
                {
                    // See if we have yMd 
                    if (format[count] == 'y' || format[count] == 'M' || format[count] == 'd') 
                    {
                        // Found a time part, stop, we can look for our separator 
                        // From [separatorStart, count) is our string, except we don't want ''s
                        strUse = UnescapeWin32String(format, separatorStart, count - 1);
                        break;
                    } 

                    // If it was quotes, ignore quoted stuff 
                    if (format[count] == '\'') 
                    {
                        // 
                        //  Ignore quotes.
                        //
                        for (count++; count < format.Length && (format[count] != '\''); count++)
                        { 
                            // Done
                        } 
 
                        // Don't go past end of string
                    } 

                    // Advance to next char (skipping unknown char or last quote)
                }
            } 

            // Return the one we're using 
            return strUse; 
        }
 
        ////////////////////////////////////////////////////////////////////////////
        //
        // Parameters:
        //      calendarValueOnly   Retrieve the values which are affected by the calendar change of DTFI. 
        //                          This will cause values like longTimePattern not be retrieved since it is
        //                          not affected by the Calendar property in DTFI. 
        // 
        ////////////////////////////////////////////////////////////////////////////
        internal unsafe void GetDTFIOverrideValues(ref DTFIUserOverrideValues values) { 
            BCLDebug.Assert(UseUserOverride, "CultureTableRecord.GetDTFIOverrideValues(): Call this only when UseUserOverride is true.");
            bool result = false;
            if (UseGetLocaleInfo)
                result = CultureInfo.nativeGetDTFIUserValues(InteropLCID, ref values); 

            if (result) { 
 
                // if we got values.yearMonthPattern = null this means the data is not located in the registry and
                // we couldn't call GetLocaleInfo. we leave yearMonthPattern as null here so the caller (DTFI) 
                // will initialize it properly.

                values.firstDayOfWeek = ConvertFirstDayOfWeekMonToSun((int)values.firstDayOfWeek);
 
                // Need to do escaping of win32/file type patterns to NLS type patterns
                values.shortDatePattern = ReescapeWin32String(values.shortDatePattern); 
                values.longDatePattern  = ReescapeWin32String(values.longDatePattern); 
                values.longTimePattern  = ReescapeWin32String(values.longTimePattern);
                values.yearMonthPattern = ReescapeWin32String(values.yearMonthPattern); 
            } else {
                //
                // We do not use user-override values or something failed during the call to GetLocaleInfo().  Use the information in culture.nlp.
                // 
                values.firstDayOfWeek = IFIRSTDAYOFWEEK;
                values.calendarWeekRule = IFIRSTWEEKOFYEAR; 
                values.shortDatePattern = SSHORTDATE; 
                values.longDatePattern = SLONGDATE;
                values.yearMonthPattern = SYEARMONTH; 
                values.amDesignator = S1159;
                values.pmDesignator = S2359;
                values.longTimePattern = STIMEFORMAT;
            } 
        }
 
        internal unsafe void GetNFIOverrideValues(NumberFormatInfo nfi) { 
            bool result = false;
            if (UseGetLocaleInfo) { 
                result = CultureInfo.nativeGetNFIUserValues(InteropLCID, nfi);
            }

            if (!result) { 
                // Something failed during the call to GetLocaleInfo().  Use the information in culture.nlp.
                nfi.numberDecimalDigits     = IDIGITS; 
                nfi.numberNegativePattern   = INEGNUMBER; 
                nfi.currencyDecimalDigits   = ICURRDIGITS;
                nfi.currencyPositivePattern = ICURRENCY; 
                nfi.currencyNegativePattern = INEGCURR;
                nfi.negativeSign            = SNEGATIVESIGN;
                nfi.numberDecimalSeparator  = SDECIMAL;
                nfi.numberGroupSeparator    = STHOUSAND; 
                nfi.positiveSign            = SPOSITIVESIGN;
                nfi.currencyDecimalSeparator= SMONDECIMALSEP; 
                nfi.currencySymbol          = SCURRENCY; 
                nfi.currencyGroupSeparator  = SMONTHOUSANDSEP;
                nfi.nativeDigits            = SNATIVEDIGITS; 
                nfi.digitSubstitution       = IDIGITSUBSTITUTION;
            }
            else if(-1 == nfi.digitSubstitution) {
                // This is a Win2000 and above property, so when it is marked as -1 
                // (an invalid value) we know it failed for Win9x reasons and that
                // we should fall back to getting this infotmation from culture.nlp. 
                nfi.digitSubstitution       = IDIGITSUBSTITUTION; 
            }
 
            nfi.numberGroupSizes        = SGROUPING;
            nfi.currencyGroupSizes      = SMONGROUPING;

            nfi.percentDecimalDigits    = nfi.numberDecimalDigits; 
            nfi.percentDecimalSeparator = nfi.numberDecimalSeparator;
            nfi.percentGroupSizes       = nfi.numberGroupSizes; 
            nfi.percentGroupSeparator   = nfi.numberGroupSeparator; 
            nfi.percentNegativePattern  = INEGATIVEPERCENT;
            nfi.percentPositivePattern  = IPOSITIVEPERCENT; 
            nfi.percentSymbol           = SPERCENT;

            if (nfi.positiveSign == null || nfi.positiveSign.Length == 0) nfi.positiveSign = "+";
 
            //Special case for Italian.  The currency decimal separator in the control panel is the empty string. When the user
            //specifies C4 as the currency format, this results in the number apparently getting multiplied by 10000 because the 
            //decimal point doesn't show up.  We'll just hack this here because our default currency format will never use nfi. 
            if (nfi.currencyDecimalSeparator.Length==0) {
                nfi.currencyDecimalSeparator= SMONDECIMALSEP; 
            }


        } 

 
 
        // EverettDataItem
        // 
        // Everett can't deserialize using names/ids, so it has to use the data item.
        internal unsafe int EverettDataItem()
        {
            // See if its a custom culture 
            if (this.IsCustomCulture)
            { 
                // They're hosed, this is a custom culture, return 0 (Invariant) 
                // It'd be better if Everett threw an error, but the accessors don't have to do
                // range checking on this, so we'd just read off the end of the data and get 
                // junk, which wouldn't be guaranteed to throw an error.  Invariant is a better choice.
                return 0;
            }
 
            InitEverettCultureDataItemMapping();
            // Normal culture, look up its data item from our LCID 
            // Do a binary search 
            int left = 0;
            int right = (m_EverettCultureDataItemMappingsSize/2) - 1; 

            while (left <= right)
            {
                int mid = (left+right)/2; 
                int result = this.m_CultureID - m_EverettCultureDataItemMappings[mid*2];
                if (result == 0) 
                { 
                    // Found it, return the index
                    return m_EverettCultureDataItemMappings[mid*2 + 1]; 
                }
                if (result < 0)
                    right = mid - 1;
                else 
                    left = mid + 1;
            } 
 
            // They're hosed, couldn't find an Everett data item for this culture.
            // It'd be better if Everett threw an error, but the accessors don't have to do 
            // range checking on this, so we'd just read off the end of the data and get
            // junk, which wouldn't be guaranteed to throw an error.  Invariant is a better choice.
            return 0;
        } 

        internal unsafe int EverettRegionDataItem() 
        { 
            // See if its a custom culture
            if (this.IsCustomCulture) 
            {
                // They're hosed, this is a custom culture, return 0 (Invariant)
                // It'd be better if Everett threw an error, but the accessors don't have to do
                // range checking on this, so we'd just read off the end of the data and get 
                // junk, which wouldn't be guaranteed to throw an error.  Invariant is a better choice.
                return 0; 
            } 

            InitEverettRegionDataItemMapping(); 
            // Normal culture, look up its data item from our LCID
            // Do a binary search
            int left = 0;
            int right = (m_EverettRegionDataItemMappingsSize/2) - 1; 

            while (left <= right) 
            { 
                int mid = (left+right)/2;
                int result = this.m_CultureID - m_EverettRegionDataItemMappings[mid*2]; 
                if (result == 0)
                {
                    // Found it, return the index
                    return m_EverettRegionDataItemMappings[mid*2 + 1]; 
                }
                if (result < 0) 
                    right = mid - 1; 
                else
                    left = mid + 1; 
            }

            // They're hosed, couldn't find an Everett data item for this culture.
            // It'd be better if Everett threw an error, but the accessors don't have to do 
            // range checking on this, so we'd just read off the end of the data and get
            // junk, which wouldn't be guaranteed to throw an error.  Invariant is a better choice. 
            return 0; 
        }
 
        internal static unsafe int IdFromEverettDataItem(int iDataItem)
        {
            InitEverettDataItemToLCIDMappings();
 
            // Assert that it exists
            BCLDebug.Assert(iDataItem >= 0 && iDataItem < m_EverettDataItemToLCIDMappingsSize, 
                String.Format( 
                    CultureInfo.CurrentCulture,
                    "[CultureTableRecord.IdFromEverettDataItem]Expected Everett data item in range of data table {0}", iDataItem)); 
            if (iDataItem < 0 || iDataItem >= m_EverettDataItemToLCIDMappingsSize)
            {
                // If the dataItem is not valid, throw.
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFieldState")); 
            }
            return m_EverettDataItemToLCIDMappings[iDataItem]; 
        } 

        internal static unsafe int IdFromEverettRegionInfoDataItem(int iDataItem) 
        {
            InitEverettRegionDataItemToLCIDMappings();

            // Assert that it exists 
            BCLDebug.Assert(iDataItem >= 0 && iDataItem < m_EverettRegionInfoDataItemToLCIDMappingsSize,
                String.Format( 
                    CultureInfo.CurrentCulture, 
                    "[CultureTableRecord.IdFromEverettRegionInfoDataItem]Expected Everett data item in range of data table {0}", iDataItem));
            if (iDataItem < 0 || iDataItem >= m_EverettRegionInfoDataItemToLCIDMappingsSize) 
            {
                // If the dataItem is not valid, throw.
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFieldState"));
            } 
            return m_EverettRegionInfoDataItemToLCIDMappings[iDataItem];
        } 
 
        // The const here should be in sync with the one defined in the native side.
        const int INT32TABLE_EVERETT_REGION_DATA_ITEM_MAPPINGS          = 0; 
        const int INT32TABLE_EVERETT_CULTURE_DATA_ITEM_MAPPINGS         = 1;
        const int INT32TABLE_EVERETT_DATA_ITEM_TO_LCID_MAPPINGS         = 2;
        const int INT32TABLE_EVERETT_REGION_DATA_ITEM_TO_LCID_MAPPINGS  = 3;
 
        // Call InitEverettRegionDataItemMapping() before using these two.
        static unsafe int* m_EverettRegionDataItemMappings = null; 
        static unsafe int m_EverettRegionDataItemMappingsSize = 0; 

        //////////////////////////////////////////////////////////////////////// 
        //
        // Initialize the data used for mapping RegionInfo ID to dataItem.
        // Everett uses dataItem in persisting RegionInfo.
        // 
        ////////////////////////////////////////////////////////////////////////
 
        private static unsafe void InitEverettRegionDataItemMapping() 
        {
            if (m_EverettRegionDataItemMappings == null) 
            {
                int* temp = CultureInfo.nativeGetStaticInt32DataTable(INT32TABLE_EVERETT_REGION_DATA_ITEM_MAPPINGS, out m_EverettRegionDataItemMappingsSize);
                m_EverettRegionDataItemMappings = temp;
                BCLDebug.Assert(m_EverettRegionDataItemMappings != null, "CultureTableRecord.m_EverettRegionDataItemMappings can not be null"); 
                BCLDebug.Assert(m_EverettRegionDataItemMappingsSize > 0, "CultureTableRecord.m_EverettRegionDataItemMappingsSize > 0");
            } 
        } 

        // Call InitEverettCultureDataItemMapping before using these two. 
        unsafe static int* m_EverettCultureDataItemMappings = null;
        static int m_EverettCultureDataItemMappingsSize = 0;

        //////////////////////////////////////////////////////////////////////// 
        //
        // Initialize the data used for mapping CultureInfo ID to dataItem. 
        // Everett uses dataItem in persisting CultureInfo. 
        //
        //////////////////////////////////////////////////////////////////////// 

        private static unsafe void InitEverettCultureDataItemMapping()
        {
            if (m_EverettCultureDataItemMappings == null) 
            {
                int* temp = CultureInfo.nativeGetStaticInt32DataTable(INT32TABLE_EVERETT_CULTURE_DATA_ITEM_MAPPINGS, out m_EverettCultureDataItemMappingsSize); 
                m_EverettCultureDataItemMappings = temp; 
                BCLDebug.Assert(m_EverettCultureDataItemMappings != null, "CultureTableRecord.m_EverettCultureDataItemMappings can not be null");
                BCLDebug.Assert(m_EverettCultureDataItemMappingsSize > 0, "CultureTableRecord.m_EverettCultureDataItemMappingsSize > 0"); 
            }
        }

 
        // Call InitEverettDataItemToLCIDMappings() before using these two.
        private static unsafe int* m_EverettDataItemToLCIDMappings = null; 
        private static int m_EverettDataItemToLCIDMappingsSize = 0; 

        //////////////////////////////////////////////////////////////////////// 
        //
        // Initialize the CultureInfo data used for mapping an Everett dataItem to a LCID.
        // Everett uses dataItem in persisting CultureInfo.
        // 
        ////////////////////////////////////////////////////////////////////////
 
        private static unsafe void InitEverettDataItemToLCIDMappings() 
        {
            if (m_EverettDataItemToLCIDMappings == null) 
            {
                int* temp = CultureInfo.nativeGetStaticInt32DataTable(INT32TABLE_EVERETT_DATA_ITEM_TO_LCID_MAPPINGS, out m_EverettDataItemToLCIDMappingsSize);
                m_EverettDataItemToLCIDMappings = temp;
                BCLDebug.Assert(m_EverettDataItemToLCIDMappings != null, "CultureTableRecord.m_EverettDataItemToLCIDMappings can not be null"); 
                BCLDebug.Assert(m_EverettDataItemToLCIDMappingsSize > 0, "CultureTableRecord.m_EverettDataItemToLCIDMappingsSize > 0");
            } 
        } 

        // Call InitEverettRegionDataItemToLCIDMappings() before using these two. 
        private static unsafe int* m_EverettRegionInfoDataItemToLCIDMappings = null;
        private static int m_EverettRegionInfoDataItemToLCIDMappingsSize = 0;

        //////////////////////////////////////////////////////////////////////// 
        //
        // Initialize the RegionInfo data used for mapping an Everett dataItem to a LCID. 
        // Everett uses dataItem in persisting RegionInfo. 
        //
        //////////////////////////////////////////////////////////////////////// 

        private static unsafe void InitEverettRegionDataItemToLCIDMappings()
        {
            if (m_EverettRegionInfoDataItemToLCIDMappings == null) 
            {
                int* temp = CultureInfo.nativeGetStaticInt32DataTable( 
                                            INT32TABLE_EVERETT_REGION_DATA_ITEM_TO_LCID_MAPPINGS, 
                                            out m_EverettRegionInfoDataItemToLCIDMappingsSize);
 
                m_EverettRegionInfoDataItemToLCIDMappings = temp;
                BCLDebug.Assert(m_EverettRegionInfoDataItemToLCIDMappings != null,
                                "CultureTableRecord.m_EverettRegionInfoDataItemToLCIDMappings can not be null");
                BCLDebug.Assert(m_EverettRegionInfoDataItemToLCIDMappingsSize > 0, 
                                "CultureTableRecord.m_EverettRegionInfoDataItemToLCIDMappingsSize > 0");
            } 
        } 
    }
 
    ////////////////////////////////////////////////////////////////////////////
    //
    // This structure contains DateTimeFormatInfo properties that can be overridden by users.
    // We define this structure so that we can fill these values in one FCALL, instead of calling GetLocaleInfo() multiple times in 
    // separate FCalls.
    // 
    // NOTE: When adding int fields, be sure to pad an extra int so that they are 
    // aligned in DWORD.  By doing so, it will make sure that String fields are
    // aligned in DWORD. 
    //
    ////////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack=2)] 
    internal struct DTFIUserOverrideValues {
        // DTFI values that are affected by calendar setttings. 
        internal String shortDatePattern; 
        internal String longDatePattern;
        internal String yearMonthPattern; 

        // DTFI values that will not be affected by calendar settings.
        internal String amDesignator;
        internal String pmDesignator; 
        internal String longTimePattern;
        internal int firstDayOfWeek; 
        internal int padding1;  // Add padding to make sure that we are aligned in DWORD. This is important for 64-bit platforms 
        internal int calendarWeekRule;
        internal int padding2;  // Add padding to make sure that we are aligned in DWORD. This is important for 64-bit platforms 
    }

    // CultureData has a cloned strucure in the native side. we send this struct to the native side to be filled
    // by the native APIs (mostly GetLocaleInfo) to load the synthetic cultures data. 
    //
    // IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT 
    // any change in this structure require a change in the cloned one in the native side. (ComNlsInfo.h/.cpp) 
    //
    // Also we use the default alignment which is 8-bytes in the managed and native sides so don't use the "Pack" property here 
    //
    // <SyntheticSupport/>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CultureData 
    {
        internal string     sIso639Language;               // LOCALE_SISO639LANGNAME       (TwoLetterISOLanguageName) 
/* SSS_WARNINGS_OFF */        internal string     sIso3166CountryName;           // LOCALE_SISO3166CTRYNAME      (TwoLetterISORegionName) /* SSS_WARNINGS_ON */ 
        internal string     sListSeparator;                // LOCALE_SLIST                 (ListSeparator)
        internal string     sDecimalSeparator;             // LOCALE_SDECIMAL              (NumberDecimalSeparator) 
        internal string     sThousandSeparator;            // LOCALE_STHOUSAND             (NumberGroupSeparator)
        internal string     sCurrency;                     // LOCALE_SCURRENCY             (CurrencySymbol)
        internal string     sMonetaryDecimal;              // LOCALE_SMONDECIMALSEP        (CurrencyDecimalSeparator)
        internal string     sMonetaryThousand;             // LOCALE_SMONTHOUSANDSEP       (CurrencyGroupSeparator) 
        internal string     sNegativeSign;                 // LOCALE_SNEGATIVESIGN         (NegativeSign)
        internal string     sAM1159;                       // LOCALE_S1159                 (AMDesignator) 
        internal string     sPM2359;                       // LOCALE_S2359                 (PMDesignator) 
        internal string     sAbbrevLang;                   // LOCALE_SABBREVLANGNAME       (ThreeLetterWindowsLanguageName)
        internal string     sEnglishLanguage;              // LOCALE_SENGLANGUAGE          (Part of EnglishName) 
/* SSS_WARNINGS_OFF */        internal string     sEnglishCountry;               // LOCALE_SENGCOUNTRY           (Part of EnglishName)
        internal string     sNativeLanguage;               // LOCALE_SNATIVELANGNAME       (Part of NativeName)
        internal string     sNativeCountry;                // LOCALE_SNATIVECTRYNAME       (Part of NativeName)
        internal string     sAbbrevCountry;                // LOCALE_SABBREVCTRYNAME       (ThreeLetterWindowsRegionName) 
/* SSS_WARNINGS_ON */        internal string     sIntlMonetarySymbol;           // LOCALE_SINTLSYMBOL           (ISOCurrencySymbol)
        internal string     sEnglishCurrency;              // LOCALE_SENGCURRNAME          (CurrencyEnglishName) 
        internal string     sNativeCurrency;               // LOCALE_SNATIVECURRNAME       (CurrencyNativeName) 
        internal string     saAltSortID;                   // LOCALE_SSORTNAME             (SortName)
        internal string     sParentName;                   // LOCALE_SPARENT               (Parent) 
        internal string     sConsoleFallbackName;          // LOCALE_SCONSOLEFALLBACKNAME  (GetConsoleFallbackUICulture)


        // sPositiveSign in NLS always return empty string 
        internal string     sPositiveSign;                 // LOCALE_SPOSITIVESIGN         (PositiveSign)
 
        // saNativeDigits should be converted to array of string instead of array of characters later. 
        internal string     saNativeDigits;                // LOCALE_SNATIVEDIGITS         (NativeDigits)
 
        internal string     waGrouping;                    // LOCALE_SGROUPING             (NumberGroupSizes)
        internal string     waMonetaryGrouping;            // LOCALE_SMONGROUPING          (CurrencyGroupSizes)
        internal string     waFontSignature;               // LOCALE_FONTSIGNATURE         (No API for it)
 
        // Some fields defined only post XP
        internal string     sNaN;                          // LOCALE_SNAN                  (NaNSymbol) 
        internal string     sPositiveInfinity;             // LOCALE_SPOSINFINITY          (PositiveInfinitySymbol) 
        internal string     sNegativeInfinity;             // LOCALE_SNEGINFINITY          (NegativeInfinitySymbol)
/* SSS_WARNINGS_OFF */        internal string     sISO3166CountryName2;          // LOCALE_SISO3166CTRYNAME2     (ThreeLetterISORegionName) /* SSS_WARNINGS_ON */ 
        internal string     sISO639Language2;              // LOCALE_SISO639LANGNAME2      (ThreeLetterISOLanguageName)

        internal string []  saSuperShortDayNames;          // LOCALE_SSHORTESTDAYNAME1..LOCALE_SSHORTESTDAYNAME7 (ShortestDayNames)
        // End of the fields defined only post XP 

        internal string []  saTimeFormat;                  // EnumTimeFormats              (GetAllDateTimePatterns('T')) 
 
        internal string []  saShortDate;                   // EnumDateFormatsEx            (GetAllDateTimePatterns('d'))
        internal string []  saLongDate;                    // EnumDateFormatsEx            (GetAllDateTimePatterns('D')) 
        internal string []  saYearMonth;                   // EnumDateFormatsEx            (GetAllDateTimePatterns("Y"))

        internal string []  saMonthNames;                  // LOCALE_SMONTHNAME(1~13)      (MonthNames)
 
        // LOCALE_SDAYNAME1 means Monday in NLS (need conversion in NLS+
        internal string []  saDayNames;                    // LOCALE_SDAYNAME(1~7)         (GetDayOfWeekNames) 
        // LOCALE_SABBREVDAYNAME means Monday in NLS (need conversion in NLS+ 
        internal string []  saAbbrevDayNames;              // LOCALE_SABBREVDAYNAME(1~7)   (GetAbbreviatedDayOfWeekNames/SuperShortDayNames)
        internal string []  saAbbrevMonthNames;            // LOCALE_SABBREVMONTHNAME(1~13)(AbbreviatedMonthNames) 
        internal string []  saNativeCalendarNames;         // GetCalendarInfo/CAL_SCALNAME (NativeCalendarName)

        internal string []  saGenitiveMonthNames;          // GetDateFormat with "dd MMMM" (MonthGenitiveNames)
        internal string []  saAbbrevGenitiveMonthNames;    // GetDateFormat with "d MMM"   (AbbreviatedMonthGenitiveNames) 

        // use also EnumCalendarInfo/CAL_ICALINTVALUE 
        internal ushort []  waCalendars;                   // LOCALE_IOPTIONALCALENDAR     (OptionalCalendars) 

        // iFirstDayOfWeek (0 is Monday for NLS and is Sunday in NLS+) 
        internal int        iFirstDayOfWeek;               // LOCALE_IFIRSTDAYOFWEEK       (FirstDayOfWeek)
        internal int        iDigits;                       // LOCALE_IDIGITS               (NumberDecimalDigits)
        internal int        iNegativeNumber;               // LOCALE_INEGNUMBER            (NumberNegativePattern)
        internal int        iCurrencyDigits;               // LOCALE_ICURRDIGITS           (CurrencyDecimalDigits) 
        internal int        iCurrency;                     // LOCALE_ICURRENCY             (CurrencyPositivePattern)
        internal int        iNegativeCurrency;             // LOCALE_INEGCURR              (CurrencyNegativePattern) 
        internal int        iFirstWeekOfYear;              // LOCALE_IFIRSTWEEKOFYEAR      (CalendarWeekRule) 
        internal int        iMeasure;                      // LOCALE_IMEASURE              (IsMetric)
        internal int        iDigitSubstitution;            // LOCALE_IDIGITSUBSTITUTION    (DigitSubstitution) 
        internal int        iDefaultAnsiCodePage;          // LOCALE_IDEFAULTANSICODEPAGE  (ANSICodePage)
        internal int        iDefaultOemCodePage;           // LOCALE_IDEFAULTCODEPAGE      (OEMCodePage)
        internal int        iDefaultMacCodePage;           // LOCALE_IDEFAULTMACCODEPAGE   (MacCodePage)
        internal int        iDefaultEbcdicCodePage;        // LOCALE_IDEFAULTEBCDICCODEPAGE(EBCDICCodePage) 
/* SSS_WARNINGS_OFF */        internal int        iCountry;                      // LOCALE_ICOUNTRY              (No API for this field) /* SSS_WARNINGS_ON */
        internal int        iPaperSize;                    // LOCALE_IPAPERSIZE            (No API for this field) 
        internal int        iLeadingZeros;                 // LOCALE_IDAYLZERO             (No API for this field) 
        internal int        iIntlCurrencyDigits;           // LOCALE_IINTLCURRDIGITS       (No API for this field)
        internal int        iGeoId;                        // EnumSystemGeoID/GetGeoInfo   (RegionInfo.GeoId) 
        internal int        iDefaultCalender;              // LOCALE_ICALENDARTYPE         (No API for this field)
    }
}
// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
namespace System.Globalization
{ 
    using System; 
    using System.IO;
    using System.Runtime.InteropServices; 
    using System.Text;
    using System.Threading;
    using System.Runtime.CompilerServices;
    using System.Runtime.ConstrainedExecution; 
    using System.Collections;
    using Microsoft.Win32.SafeHandles; 
#if DICTIONARY_FOR_CULTURE_CACHE 
    using System.Collections.Generic;
#endif // DICTIONARY_FOR_CULTURE_CACHE 
    using System.Runtime.Serialization;
    using System.Runtime.Versioning;

    // Enum for the IFLAGS field 
    [Flags]
    internal enum CultureFlags 
    { 
        IsSpecificCulture = 0x0001,
    } 

    /*==============================================================================
     *
     * Data record for CultureInfo classes.  Used by System.Globalization.CultureInfo. 
     *
     * NOTE: ALL of the data table/user override/OS version, etc. hacks related to 
     *       the data should be in here.  DTFI, CI, etc. should have NO knowledge 
     *       of eccentricities of the data store.
     * 
     *
     ==============================================================================*/
    // Only statics, does not need to be marked with the serializable attribute
    internal class CultureTableRecord 
    {
        // For spanish sorting 
        internal const int SPANISH_TRADITIONAL_SORT    = 0x040a; 
        private  const int SPANISH_INTERNATIONAL_SORT  = 0x0c0a;
 
        // Sizes defined by the RFC3066 spec
        private  const int MAXSIZE_LANGUAGE                = 8;
        private  const int MAXSIZE_REGION                  = MAXSIZE_LANGUAGE;
        private  const int MAXSIZE_SUFFIX                  = 8 * MAXSIZE_LANGUAGE; 
        private  const int MAXSIZE_FULLTAGNAME             = MAXSIZE_LANGUAGE + MAXSIZE_REGION + MAXSIZE_SUFFIX + 4; // The 2 is for the tags and the prefix
 
        private static Object s_InternalSyncObject; 
        private static Object InternalSyncObject {
            get { 
                if (s_InternalSyncObject == null) {
                    Object o = new Object();
                    Interlocked.CompareExchange(ref s_InternalSyncObject, o, null);
                } 
                return s_InternalSyncObject;
            } 
        } 

        // 
        // CultureTableRecordCache caches all CultureTableRecord created objects except the objects created by
        // RegionInfo constructor which takes region name and instead will be cached in CultureTableRecordRegionCache
        //
 
        private static Hashtable CultureTableRecordCache;
        private static Hashtable CultureTableRecordRegionCache; 
 
#if !FEATURE_PAL
 
#if DICTIONARY_FOR_CULTURE_CACHE
        //
        // because the synthetic culture creation is very expensive in term of speed and space. so SyntheticDataCache will be used to cache
        // the created synthetic culture data so subsequent calls will not need to repeat the creation process especially when creating the 
        // same culture using "new CultureInfo(...)" as the regular cultures created this way is not cached.
        // The cache is mapping from lcid to the unmanaged allocated safe memory handle that contains the culture data. 
        // 

        // <SyntheticSupport/> 
        private static Dictionary<int, AgileSafeNativeMemoryHandle> SyntheticDataCache;
        internal static Dictionary<int, String> SyntheticLcidToNameCache;
        internal static Dictionary<String, int> SyntheticNameToLcidCache;
#else 

        private static Hashtable SyntheticDataCache; 
        internal static Hashtable SyntheticLcidToNameCache; 
        internal static Hashtable SyntheticNameToLcidCache;
#endif // DICTIONARY_FOR_CULTURE_CACHE 

#endif // !FEATURE_PAL

 
        // CultureTable this data refers to.
        private CultureTable                m_CultureTable; 
        private unsafe CultureTableData*    m_pData; 
        private unsafe ushort*              m_pPool;
        private bool                        m_bUseUserOverride; 
        private int                         m_CultureID;
        private String                      m_CultureName;

        private int                         m_ActualCultureID   = 0; 
        private string                      m_ActualName        = null;
 
        // <SyntheticSupport/> 
        // m_synthetic will be true only if we have synthetic culture or synthetic replacement culture.
        private bool                        m_synthetic         = false; 

        private AgileSafeNativeMemoryHandle nativeMemoryHandle;             // <SyntheticSupport/>
        private string                      m_windowsPath = null;
 
        private const int LOCALE_SLANGUAGE       = 0x00000002;   // localized name of language
/* SSS_WARNINGS_OFF */                private const int LOCALE_SCOUNTRY        = 0x00000006;   // localized name of country /* SSS_WARNINGS_ON */ 
        private const int LOCALE_SNATIVELANGNAME = 0x00000004;   // native name of language 
/* SSS_WARNINGS_OFF */                private const int LOCALE_SNATIVECTRYNAME = 0x00000008;   // native name of country /* SSS_WARNINGS_ON */
        private const int LOCALE_ICALENDARTYPE   = 0x00001009;   // iCalendarType type of calendar 


        ////////////////////////////////////////////////////////////////////////
        // 
        // Create a CultureTable from the given custom/replacement culture name.
        // 
        // SECURITY SECURITY SECURITY 
        //  Before call this function, call ValidateCulturePieceToLower() to verify
        //  that the name does not contain illegal characters (such as "." or backslash. 
        //
        ////////////////////////////////////////////////////////////////////////

        [ResourceExposure(ResourceScope.None)] 
        [ResourceConsumption(ResourceScope.Machine, ResourceScope.Machine)]
        private unsafe CultureTable GetCustomCultureTable(string name) 
        { 
            CultureTable cultureTable = null;
 
            string customCultureFile = GetCustomCultureFile(name);
            if (customCultureFile == null)
            {
                return null; 
            }
 
            try 
            {
                cultureTable = new CultureTable(customCultureFile, false); 
                if (!cultureTable.IsValid)
                {
                    // If we have invalid culture table then we have custom culture. in that case we'll try
                    // to see if the culture name is one of the framework or synthetic cultures and then 
                    // try to create it otherwise we'll throw.
 
                    String  defaultTableActualName; 
                    int     defaultTableCultureID;
                    int     defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureName( 
                                                    name, out defaultTableCultureID, out defaultTableActualName);

                    if (defaultTableDataItem < 0)       // not built in framework culture
                    { 
#if !FEATURE_PAL
                        InitSyntheticMapping(); 
                        BCLDebug.Assert(CultureTableRecord.SyntheticLcidToNameCache != null, 
                                "[CultureTableRecord::GetCustomCultureTable] cache should be available now.");
                        if (CultureTableRecord.SyntheticNameToLcidCache[name] == null) 
                        {
                            throw new ArgumentException(
                                    String.Format(
                                        CultureInfo.CurrentCulture, 
                                        Environment.GetResourceString("Arg_CorruptedCustomCultureFile"), name));
                        } 
#endif // !FEATURE_PAL 

                    } 

                    return null;  // returning null means fallback to framework or synthetic culture.
                }
            } 
            catch (FileNotFoundException)
            { 
                // 
                // getting here means custom culture file get unregistered/renamed from different AppDomain/Process.
                // just update the cache to point to the empty string as subsequent calls will not bother trying again. 
                //
                cultureTable = null;
            }
 
            return cultureTable;
        } 
 
        ////////////////////////////////////////////////////////////////////////
        // 
        //  Using the specified replacementCultureName, check if there is a replacment
        //  culture file.  If yes, return the CultureTable created from the custom culture file.
        //
        //  Parameters 
        //      replacementCultureName: The culture name to check.  Note that alternative sort like de-DE_phoneb should pass de-DE here.
        //      [OUT] dataItem: The dataItem for the culture name in the custom culture file. 
        //  Returns 
        //      A valid CultureTable from the custom culture file. null, if the custom file can not be found or the dataItem can not be found.
        // 
        ////////////////////////////////////////////////////////////////////////

        internal unsafe CultureTable TryCreateReplacementCulture(String replacementCultureName, out int dataItem)
        { 
            string name = ValidateCulturePieceToLower(replacementCultureName, "cultureName", MAXSIZE_FULLTAGNAME);
 
            //  Before call this function, call ValidateCulturePieceToLower() to verify 
            //  that the name does not contain illegal characters (such as "." or backslash.
            CultureTable cultureTable = GetCustomCultureTable(name); 

            if (cultureTable == null)
            {
                dataItem = -1; 
                return (null);
            } 
            // We have a replacement culture.  Use it. 
            int tempID;
            String tempName; 
            dataItem = cultureTable.GetDataItemFromCultureName(name, out tempID, out tempName);
            return (dataItem >= 0 ? cultureTable : null);
        }
 
#if !FEATURE_PAL
        //<SyntheticSupport> 
        //////////////////////////////////////////////////////////////////////// 
        //
        //  Create the hashtable for mapping synthetic culture names to LCID, 
        //  and hashtable for mapping synthetic culture LCID to names if they
        //  are not yet created.
        //
        //////////////////////////////////////////////////////////////////////// 

        internal static void InitSyntheticMapping() 
        { 
            // <SyntheticSupport/>
            if (SyntheticLcidToNameCache == null || SyntheticNameToLcidCache == null) 
            {
                CacheSyntheticNameLcidMapping();
            }
        } 
        //</SyntheticSupport>
#endif 
 
        //
        // GetCultureTableRecord create CultureTableRecord object for specific culture name. 
        // This method uses CultureTableRecordCache to make sure we don't have to create this
        // object if it is already created before.
        //
 
        internal static CultureTableRecord GetCultureTableRecord(string name, bool useUserOverride)
        { 
            BCLDebug.Assert(name != null, "[CultureTableRecord::GetCultureTableRecord] name should be valid."); 

            // Make sure the cache is valid. 
            if (CultureTableRecordCache == null)
            {
                if (name.Length == 0) // Invariant culture
                { 
                    // First time Invariant culture get created we ignore creating the cache
                    return new CultureTableRecord(name, useUserOverride); 
                } 

                lock (InternalSyncObject) 
                {
                    if (CultureTableRecordCache == null)
                        CultureTableRecordCache = new Hashtable();
                } 
            }
 
            name = ValidateCulturePieceToLower(name, "name", MAXSIZE_FULLTAGNAME); 

            CultureTableRecord [] cultureRecordArray = (CultureTableRecord []) CultureTableRecordCache[name]; 
            if (cultureRecordArray != null)
            {
                int index   = useUserOverride ? 0 : 1;
 
                if (cultureRecordArray[index] == null)
                { 
                    int filled  = index == 0 ? 1 : 0; 
                    cultureRecordArray[index] = (CultureTableRecord) cultureRecordArray[filled].CloneWithUserOverride(useUserOverride);
                } 

                return cultureRecordArray[index];
            }
 
            CultureTableRecord cultureRecord = new CultureTableRecord(name, useUserOverride);
            lock (InternalSyncObject) 
            { 
                if (CultureTableRecordCache[name] == null)
                { 
                    cultureRecordArray = new CultureTableRecord[2];
                    cultureRecordArray[useUserOverride ? 0 : 1] = cultureRecord;
                    CultureTableRecordCache[name] = cultureRecordArray;
                } 
            }
 
            return cultureRecord; 
        }
 
        //
        // GetCultureTableRecord create CultureTableRecord object for specific culture Id.
        // This method convert the culture Id to culture name and then uses GetCultureTableRecord
        // to get the CultureTableRecord object. 
        //
 
        internal static CultureTableRecord GetCultureTableRecord(int cultureId, bool useUserOverride) 
        {
            if (cultureId == CultureInfo.LOCALE_INVARIANT) 
                return GetCultureTableRecord("", false);

            String name = null;
            if (CultureTable.Default.GetDataItemFromCultureID(cultureId, out name) < 0) 
            {
#if !FEATURE_PAL 
                // Try ELK 
                if (CultureInfo.IsValidLCID(cultureId, CultureInfo.LCID_INSTALLED))
                { 
                    InitSyntheticMapping();
                    name = (String) SyntheticLcidToNameCache[cultureId];
                }
#endif // !FEATURE_PAL 
            }
 
            if (name != null && name.Length>0) // GetDataItemFromCultureID can set the name to empty string. 
            {
                return GetCultureTableRecord(name, useUserOverride); 
            }

            throw new ArgumentException(
                String.Format( 
                    CultureInfo.CurrentCulture,
                    Environment.GetResourceString("Argument_CultureNotSupported"), cultureId), "culture"); 
        } 

        // 
        // GetCultureTableRecordForRegion create CultureTableRecord object for specific region name.
        // this method do the following
        //  o   try to get the object from the cache. if found then return otherwise try to create it.
        //  o   it try to get the record from the framework culture table. if found it then create 
        //      the CultureTableRecord object and store it in the cache then return.
        //  o   call GetCultureTableRecord to get the object. if found it then store it in the cache 
        //      and return. notice that GetCultureTableRecord will try the custom culture then synthetic 
        //      culture.
        //  o   otherwise we'll throw ArgumentException. 
        //

        internal static CultureTableRecord GetCultureTableRecordForRegion(string regionName, bool useUserOverride)
        { 
            BCLDebug.Assert(regionName != null, "[CultureTableRecord::GetCultureTableRecordForRegion] regionName should be valid.");
 
            // Make sure the cache is valid. 
            if (CultureTableRecordRegionCache == null)
            { 
                lock (InternalSyncObject)
                {
                    if (CultureTableRecordRegionCache == null)
                        CultureTableRecordRegionCache = new Hashtable(); 
                }
            } 
 
            regionName = ValidateCulturePieceToLower(regionName, "regionName", MAXSIZE_FULLTAGNAME);
 
            CultureTableRecord [] cultureRecordArray = (CultureTableRecord []) CultureTableRecordRegionCache[regionName];
            if (cultureRecordArray != null)
            {
                int index = useUserOverride ? 0 : 1; 
                if (cultureRecordArray[index] == null)
                { 
                    cultureRecordArray[index] = cultureRecordArray[index == 0 ? 1 : 0].CloneWithUserOverride(useUserOverride); 
                }
                return cultureRecordArray[index]; 
            }

            int dataItem = CultureTable.Default.GetDataItemFromRegionName(regionName);
 
            CultureTableRecord cultureRecord = null;
 
            if (dataItem > 0) 
            {
                cultureRecord = new CultureTableRecord(regionName, dataItem, useUserOverride); 
            }
            else
            {
                try 
                {
                    cultureRecord = GetCultureTableRecord(regionName, useUserOverride); 
                } 
                catch (ArgumentException)
                { 
                    throw new ArgumentException(
                                String.Format(
                                    CultureInfo.CurrentCulture,
                                    Environment.GetResourceString("Argument_InvalidRegionName"), regionName), "name"); 
                }
            } 
 
            BCLDebug.Assert(cultureRecord != null, "[CultureTableRecord::GetCultureTableRecordForRegion] cultureRecord should be valid.");
            lock (InternalSyncObject) 
            {
                if (CultureTableRecordRegionCache[regionName] == null)
                {
                    cultureRecordArray = new CultureTableRecord[2]; 
                    cultureRecordArray[useUserOverride ? 0 : 1] = cultureRecord.CloneWithUserOverride(useUserOverride);
                    CultureTableRecordRegionCache[regionName] = cultureRecordArray; 
                } 
            }
 
            return cultureRecord;
        }

        // 
        // This constructor used only to create a Framework culture. it doesn't create custom
        // culture nor synthetic culture. 
        // This is used when requesting the native calendar name for a custom culture with 
        // empty string native calendar name.
        // 
        internal unsafe CultureTableRecord(int cultureId, bool useUserOverride)
        {
            this.m_bUseUserOverride = useUserOverride;
 
            int defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureID(cultureId, out m_ActualName);
            if (defaultTableDataItem < 0) 
            { 
                throw new ArgumentException(
                    String.Format( 
                        CultureInfo.CurrentCulture,
                        Environment.GetResourceString("Argument_CultureNotSupported"), cultureId), "culture");
            }
 
            m_ActualCultureID   = cultureId;
            m_CultureTable      = CultureTable.Default; 
 
            m_pData = (CultureTableData*)(m_CultureTable.m_pItemData + m_CultureTable.m_itemSize * defaultTableDataItem);
            m_pPool = m_CultureTable.m_pDataPool; 

            m_CultureName = SNAME;
            m_CultureID = (cultureId == SPANISH_TRADITIONAL_SORT) ? cultureId : ILANGUAGE;
 
            BCLDebug.Assert(!IsCustomCulture , "[CultureTableRecord::ctor] we shouldn't have custom culture.");
            BCLDebug.Assert(!IsSynthetic, "[CultureTableRecord::ctor] we shouldn't have synthetic culture."); 
        } 

        // 
        // m_bUseUserOverride indicates that if we need to check for user-override values for this CultureInfo instance.
        // For the user default culture of the system, user can choose to override some of the values
        // associated with that culture.  For example, the default short-date format for en-US is
        // "M/d/yyyy", however, one may change it to "dd/MM/yyyy" from the Regional Option in 
        // the control panel.
        // So when a CultureInfo is created, one can specify if the create CultureInfo should check 
        // for user-override values, or should always get the default values. 
        //
        // 
        // AVOID USING P/INVOKE IN THIS CODEPATH.
        // AVOID USING P/INVOKE IN THIS CODEPATH.
        // AVOID USING P/INVOKE IN THIS CODEPATH.
        // 
        // P/Invoke will cause COM to be initialized and sets the thread apartment model. Since CultureInfo is used early in the CLR process,
        // this will prevent Loader from having a chance to look at the executable and setting the apartment model to the one the application wants. 
 

 
        //
        // Search order for creating a culture.
        //
 
        /*
           First, search by name 
                if this is a known culture name from culture.nlp, and it is an alternative sort name (such as de-DE_phoneb) 
                {
                    Get the name from the LANGID by removing the sort ID (so the name becomes de-DE). 
                    This is the name used for search replacment culture.
                }

                Check if this specified name has a custom/replacement culture file. 
                if there is a custom/replacement culture file
                { 
                    // This is a custom culture, or a replacement culture. 
                    return; [CUSTOM/REPLACEMENT CULTURE (.NET CULTURE/SYNTHETIC CULTURE) FOUND BY NAME]
                } 
                From culture.nlp, check if tihs is a vlid culture name
                if this is a valid culture name
                {
                    // This is a .NET culture. 
                    return; [NON-REPLACEMENT .NET CULTURE FOUND BY NAME]
                } 
                Check if this is a valid name from synthetic culture 
                if this is a valid synthetic culture name
                { 
                    // This is a synthetic culture. Set the cultureID, so we will
                    // create it when we search by LCID later.
                    // [SYNTHETIC CULTURE FOUND BY NAME]
                } else 
                {
                    throw exception;    [INVALID NAME] 
                } 
           Then Search by LCID
                we'll come here only if the lcid is filled with synthetic culture Id. 
                    // This is synthetic culture.
                    Get the name for this LANGID of this synthetic LCID.
                    if there is a replacement culture for this LCID by checking name.
                    { 
                        Use it and return the replacement culture for synthetic culture.
                        return;  [REPLACEMENT SYNTHETIC CULTURE] 
                    } 
                    Init and return the synthetic culture.
                    return;  [NON-REPLACEMENT SYNTHETIC CULTURE] 

                }
                otherwise throw exception
 
        */
        // 
        // * IMPORTANT * cultureName should be in lower case. 
        //
        private unsafe CultureTableRecord(String cultureName, bool useUserOverride) 
        {
            BCLDebug.Assert(cultureName != null, "[CultureTableRecord::ctor] cultureName should be valid.");

            int cultureID = 0; 

            // Special case for invariant name 
            if (cultureName.Length == 0) 
            {
                useUserOverride = false; 
                cultureID = CultureInfo.LOCALE_INVARIANT;
            }

            this.m_bUseUserOverride = useUserOverride; 

            // We prefer to look up by name (if available) 
            int iDataItem = -1; 
            if (cultureName.Length > 0)
            { 
                // Check if this is an alternative sort name.
                String defaultTableActualName;
                int defaultTableCultureID;
                string name = cultureName; 
                int defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureName(name, out defaultTableCultureID, out defaultTableActualName);
                if (defaultTableDataItem >= 0 && 
                    (CultureInfo.GetSortID(defaultTableCultureID) > 0 || defaultTableCultureID == SPANISH_TRADITIONAL_SORT)) 
                {
                    String replacmentCultureName; 

                    int nonSortId;
                    if (defaultTableCultureID == SPANISH_TRADITIONAL_SORT)
                        nonSortId = SPANISH_INTERNATIONAL_SORT; 
                    else
                        nonSortId = CultureInfo.GetLangID(defaultTableCultureID); 
 
                    // This is an alternative sort culture.
                    if (CultureTable.Default.GetDataItemFromCultureID(nonSortId, out replacmentCultureName) >= 0) 
                    {
                        // This is the replacement culture name for an alternative sort.
                        name = ValidateCulturePieceToLower(replacmentCultureName, "cultureName", MAXSIZE_FULLTAGNAME);
                    } 
                }
 
                // If the compatibility flag is defined and culture is replacemet culture then we don't 
                // open the custom culture file. instead we'll try to get framework/OS culture.
                if (!Environment.GetCompatibilityFlag(CompatibilityFlag.DisableReplacementCustomCulture) || 
                    IsCustomCultureId(defaultTableCultureID))
                {

                    // we always try the replacement custom cultures first. 
                    //  Before call this function, call ValidateCulturePieceToLower() to verify
                    //  that the name does not contain illegal characters (such as "." or backslash. 
                    m_CultureTable = GetCustomCultureTable(name); 
                }
 
                if (m_CultureTable != null)
                {
                    //
                    // [CUSTOM/REPLACEMENT CULTURE (.NET CULTURE/SYNTHETIC CULTURE) FOUND BY NAME] 
                    //
                    iDataItem = this.m_CultureTable.GetDataItemFromCultureName(name, out this.m_ActualCultureID, out this.m_ActualName); 
                    if (defaultTableDataItem >= 0) 
                    {
                        // This is a replacment culture (since defaultTableDataItem >= 0), use the default ID/Name from the table. 
                        // For de-DE_phoneb, this will set the the actualCultureID to be 0x10407, instead of the LCID for replacment cutlure 0x0407.
                        this.m_ActualCultureID = defaultTableCultureID;
                        this.m_ActualName = defaultTableActualName;
                    } 
                }
 
                if (iDataItem < 0 && defaultTableDataItem >= 0) 
                {
                    // 
                    // [NON-REPLACEMENT .NET CULTURE FOUND BY NAME]
                    //
                    this.m_CultureTable = CultureTable.Default;
                    this.m_ActualCultureID = defaultTableCultureID; 
                    this.m_ActualName = defaultTableActualName;
                    iDataItem = defaultTableDataItem; 
                } 

#if !FEATURE_PAL 
                // If not exist in main table then try the synthetic one.
                if (iDataItem < 0)
                {
                    InitSyntheticMapping(); 
                    BCLDebug.Assert(SyntheticNameToLcidCache != null, "[CultureTableRecord::ctor] cache should be created any way.");
                    if (SyntheticNameToLcidCache[name] != null) 
                    { 
                        // Initialize the lcid so it get created later when trying the creation using the lcid.
                        cultureID = (int) SyntheticNameToLcidCache[name]; 
                        //
                        // [SYNTHETIC CULTURE FOUND BY NAME]
                        //
                    } 
                }
#endif // !FEATURE_PAL 
            } 

            // If we couldn't get it by name, try culture ID. 
            if (iDataItem < 0 && cultureID > 0)
            {
                if (cultureID == CultureInfo.LOCALE_INVARIANT)
                { 
                    // Special case for the Invariant culture.
                    iDataItem = CultureTable.Default.GetDataItemFromCultureID(cultureID, out this.m_ActualName); 
                    if (iDataItem > 0) 
                    {
                        m_ActualCultureID   = cultureID; 
                        m_CultureTable      = CultureTable.Default;
                    }
                }
#if !FEATURE_PAL 
                else
                { 
                    BCLDebug.Assert(CultureInfo.IsValidLCID(cultureID, CultureInfo.LCID_INSTALLED), 
                        "[CultureTableRecord::ctor] culture should be valid ELK culture.");
 
                    BCLDebug.Assert(SyntheticNameToLcidCache != null, "[CultureTableRecord::ctor] cache should be valid.");

                    // Check if we have replacement cutlure for this synthetic culture.
                    // we always try the replacement custom cultures first. 

                    CultureTable replacementTable       = null; 
                    String       replacementCultureName = null; 

                    // 
                    // Here is the logic for creating the synthetic cultures.
                    // Note the code is optimized to prevent any duplication.
                    //
                    //  If (cultureID is Sort Id) 
                    //      If (non sort Id is one of the framework culture)
                    //          If (replacement) 
                    //              Get data info from custom culture 
                    //          Else
                    //              Get culture data from the framework 
                    //      Else if (non sort Id one of the cached synthetic cultures)
                    //          If (replacement)
                    //              Get data info from custom culture
                    //          Else 
                    //              Get culture data from the OS.
                    //      Else 
                    //          Throw exception. 
                    //  Else
                    //      if (cultureID is one of the cached synthetic cultures) 
                    //          If (replacement)
                    //              Get data info from custom culture
                    //          Else
                    //              Get culture data from the OS. 
                    //      Else
                    //          Throw exception. 
 
                    if (CultureInfo.GetSortID(cultureID) > 0)
                    { 
                        iDataItem = CultureTable.Default.GetDataItemFromCultureID(
                                                            CultureInfo.GetLangID(cultureID),
                                                            out replacementCultureName);
                    } 

                    if (iDataItem < 0) 
                    { 
                        // Get synthetic culture name from language ID. This should always succeed because this is a valid LCID.
                        // Note that language ID, instead of LCID, since alternative sort uses the same replacement file as default sort. 
                        replacementCultureName = (String)SyntheticLcidToNameCache[CultureInfo.GetLangID(cultureID)];
                    }
                    String actualName = (String)SyntheticLcidToNameCache[cultureID];
                    int replacementDataItem = -1; 
                    if (actualName != null && replacementCultureName != null &&
                        !Environment.GetCompatibilityFlag(CompatibilityFlag.DisableReplacementCustomCulture)) 
                    { 
                        replacementTable = TryCreateReplacementCulture(replacementCultureName, out replacementDataItem);
                    } 

                    if (replacementTable == null)
                    {
                        if (iDataItem > 0) 
                        {
                            // This is a synthetic culture corresponds to a new OS alternative sort for one of the framework cultures. 
                            // example ja-JP_radstr 
                            m_CultureTable     = CultureTable.Default;
                            m_ActualCultureID  = cultureID; 
                            m_synthetic        = true;
                            // if we are running on Vista nativeGetCultureName will return the normalized name. otherwise
                            // it will return null and then m_ActualName will initialized later with SNAME.
                            m_ActualName       = (string) CultureInfo.nativeGetCultureName(cultureID, true, false); 
                        }
                        else 
                        // <SyntheticSupport/> 
                        if (GetSyntheticCulture(cultureID))
                        { 
                            BCLDebug.Assert(m_pData != null && m_pPool != null, "[CultureTableRecord::ctor] Got synthetic culture data and didn't intialize the data pointers.");
                            //
                            // [NON-REPLACEMENT SYNTHETIC CULTURE]
                            // 
                            return;
                        } 
                        // iDataItem is -1.  Let the code fall thru and we will throw exception. 
                    }
                    else 
                    {
                        //
                        // [REPLACEMENT SYNTHETIC CULTURE]
                        // 
                        this.m_CultureTable = replacementTable;
                        iDataItem = replacementDataItem; 
                        // Don't initialize m_ActualName by actualName now as we need to have the name normalized. 
                        // later it'll be initialized with SNAME which is normalized name.
                        // But if we are running on Vista nativeGetCultureName will return the normalized name. otherwise 
                        // it will return null and then m_ActualName will initialized later with SNAME.
                        m_ActualName = (string) CultureInfo.nativeGetCultureName(cultureID, true, false);
                        this.m_ActualCultureID= cultureID;
                    } 
                }
#endif // !FEATURE_PAL 
            } 

            // If we found one, use it and return 
            if (iDataItem >= 0)
            {
                // Found it, use it
                this.m_pData = (CultureTableData*)(this.m_CultureTable.m_pItemData + 
                    this.m_CultureTable.m_itemSize * iDataItem);
                this.m_pPool = this.m_CultureTable.m_pDataPool; 
                // Use name & ID from the file ('cept spanish traditional, which has to stay the same) 
                this.m_CultureName = this.SNAME;
                this.m_CultureID = (m_ActualCultureID == SPANISH_TRADITIONAL_SORT) ? m_ActualCultureID : this.ILANGUAGE; 

#if !FEATURE_PAL
                CheckCustomSynthetic();
#endif // !FEATURE_PAL 

                return; 
            } 

            // Error, if we have a name throw that name 
            if (cultureName != null)
                throw new ArgumentException(
                    String.Format(
                        CultureInfo.CurrentCulture, 
                        Environment.GetResourceString("Argument_InvalidCultureName"), cultureName), "name");
 
            // No name, throw the LCID 
            throw new ArgumentException(
                String.Format( 
                    CultureInfo.CurrentCulture,
                    Environment.GetResourceString("Argument_CultureNotSupported"), cultureID), "culture");
        }
 
        //
        // this constructor will create the CultureTableRecord object and point to the 
        // culture table at the slot dataItem. 
        //
 
        private unsafe CultureTableRecord(string regionName, int dataItem, bool useUserOverride)
        {
            BCLDebug.Assert(regionName != null && regionName.Length > 0,
                "[CultureTableRecord.CultureTableRecord(regionName,bool)]Expected non-null/empty name"); 

            BCLDebug.Assert(dataItem > 0, "[CultureTableRecord.CultureTableRecord(regionName, dataItem, bool)] dataItem > 0 should be true."); 
 
            // Assuming it works we'll want these
            this.m_bUseUserOverride = useUserOverride; 
            this.m_CultureName = regionName;
            this.m_CultureTable = CultureTable.Default;

            // Found it, use it 
            this.m_pData = (CultureTableData*)(this.m_CultureTable.m_pItemData +
                this.m_CultureTable.m_itemSize * dataItem); 
            this.m_pPool = this.m_CultureTable.m_pDataPool; 

            // Use ID from the file 
            this.m_CultureID = this.ILANGUAGE;
        }

 
#if !FEATURE_PAL
 
        // <SyntheticSupport/> 
        private void CheckCustomSynthetic()
        { 
            if (IsCustomCulture)
            {
                InitSyntheticMapping();
                BCLDebug.Assert(SyntheticLcidToNameCache != null, "[CultureTableRecord::CheckCustomSynthetic] cache should be available now."); 

                if (IsCustomCultureId(m_CultureID)) 
                {   // Supplemental 
                    string name = ValidateCulturePieceToLower(m_CultureName, "CultureName", MAXSIZE_FULLTAGNAME);
                    if (SyntheticNameToLcidCache[name] != null) 
                    {
                        m_synthetic = true;
                        m_ActualCultureID = m_CultureID = (int) SyntheticNameToLcidCache[name];
                    } 
                }
                else 
                {   // Replacement 
                    if (SyntheticLcidToNameCache[m_CultureID] != null)
                    { 
                        m_synthetic = true;
                        m_ActualCultureID = m_CultureID;
                    }
                    else if (m_CultureID != m_ActualCultureID && SyntheticLcidToNameCache[m_ActualCultureID] != null) 
                    {
                        // synthetic alternative sort. 
                        m_synthetic = true; 
                    }
                } 
            }
        }
#endif // !FEATURE_PAL
 

        internal static void ResetCustomCulturesCache() 
        { 
#if !FEATURE_PAL
            CultureTableRecordCache = null; 
            CultureTableRecordRegionCache = null;
#endif // !FEATURE_PAL
        }
 
#if !FEATURE_PAL
 
        // 
        // GetScriptTag is used to detect the script of a given culture to be concatenated to the
        // culture name. the way to do that is to get the sort key of some culture native name as 
        // we used native day name and then from the sort key we get the script.
        //
        // Important: this method should return the script tag in lowercase letters.
        // 
        // <SyntheticSupport/>
        private static unsafe bool GetScriptTag(int lcid, out string script) 
        { 
            script = null;
 
            string nativeDayName = (string) CultureInfo.nativeGetCultureName(lcid, false, true);
            if (nativeDayName == null) { return false; }

            // 
            // Try first the scripts that is not supported by the sort key.
            // 
 
            for (int j=0; j<nativeDayName.Length; j++)
            { 
                if (nativeDayName[j] >= 0x1401 && nativeDayName[j] <= 0x1676)   // Canadian Syllabics
                {
                    script = "cans";
                    return true; 
                }
                if (nativeDayName[j] >= 0x1200 && nativeDayName[j] <= 0x137C)   // Ethiopic 
                { 
                    script = "ethi";
                    return true; 
                }
                if (nativeDayName[j] >= 0x1800 && nativeDayName[j] <= 0x1819)   // Mongolian
                {
                    script = "mong"; 
                    return true;
                } 
                if (nativeDayName[j] >= 0xA000 && nativeDayName[j] <= 0xA4C6)   // Yi 
                {
                    script = "yiii"; 
                    return true;
                }
                if (nativeDayName[j] >= 0x13A0 && nativeDayName[j] <= 0x13F4)   // Cherokee
                { 
                    script = "cher";
                    return true; 
                } 
                if (nativeDayName[j] >= 0x1780 && nativeDayName[j] <= 0x17F9)   // Khmer
                { 
                    script = "khmr";
                    return true;
                }
            } 

            byte [] sortKey; 
            int length = CultureInfo.GetNativeSortKey(lcid, 0, nativeDayName, nativeDayName.Length, out sortKey); 
            if (length == 0) { return false; }
 
            int i=0;
            while (i<length && sortKey[i] != 1) // 1 is the seperator byte
            {
                switch (sortKey[i]) 
                {
                    case 14: script = "latn"; return true;  /* LATIN        */ 
                    case 15: script = "grek"; return true;  /* GREEK        */ 
                    case 16: script = "cyrl"; return true;  /* CYRILLIC     */
                    case 17: script = "armn"; return true;  /* ARMENIAN     */ 
                    case 18: script = "hebr"; return true;  /* HEBREW       */
                    case 19: script = "arab"; return true;  /* ARABIC       */
                    case 20: script = "deva"; return true;  /* DEVANAGARI   */
                    case 21: script = "beng"; return true;  /* BENGALI      */ 
                    case 22: script = "guru"; return true;  /* GURMUKKHI    */
                    case 23: script = "gujr"; return true;  /* GUJARATI     */ 
                    case 24: script = "orya"; return true;  /* ORIYA        */ 
                    case 25: script = "taml"; return true;  /* TAMIL        */
                    case 26: script = "telu"; return true;  /* TELUGU       */ 
                    case 27: script = "knda"; return true;  /* KANNADA      */
                    case 28: script = "mlym"; return true;  /* MALAYLAM     */
                    case 29: script = "sinh"; return true;  /* SINHALESE    */
                    case 30: script = "thai"; return true;  /* THAI         */ 
                    case 31: script = "laoo"; return true;  /* LAO          */
                    case 32: script = "tibt"; return true;  /* TIBETAN      */ 
                    case 33: script = "geor"; return true;  /* GEORGIAN     */ 
                    case 34: script = "kana"; return true;  /* KANA         */
                    case 35: script = "bopo"; return true;  /* BOPOMOFO     */ 
                    case 36: script = "hang"; return true;  /* HANGUL       */
                    case 128:script = "hani"; return true;  /* IDEOGRAPH    */
                }
                i += 2; 
            }
            return false; 
        } 

        // <SyntheticSupport/> 
        // IsBuiltInCulture is used when caching the name and lcid mapping to detect if it is
        // a framework culture or synthetic cultures.
        private static bool IsBuiltInCulture(int lcid)
        { 
            return CultureTable.Default.IsExistingCulture(lcid);
        } 
 
        internal static string Concatenate(StringBuilder helper, params string [] stringsToConcat)
        { 
            BCLDebug.Assert(helper != null, "[CultureTableRecord::Concatenate] string builder should be valid.");

            if (helper.Length > 0)
            { 
                helper.Remove(0, helper.Length);
            } 
 
            for (int i=0; i<stringsToConcat.Length; i++)
            { 
                helper.Append(stringsToConcat[i]);
            }

            return helper.ToString(); 
        }
 
        // 
        // GetCultureNamesUsingSNameLCType will try to get the culture names using LOCALE_SNAME type. and will
        // retun true in success and false in failing. 
        //
        // <SyntheticSupport/>

        internal static bool GetCultureNamesUsingSNameLCType(int [] lcidArray, Hashtable lcidToName, Hashtable nameToLcid) 
        {
            string cultureName; 
 
            // first try to see if the LOCALE_SNAME is supported on current platform.
            cultureName = (string) CultureInfo.nativeGetCultureName(lcidArray[0], true, false); 
            if (cultureName == null)
                return false;

            if (!IsBuiltInCulture(lcidArray[0])) 
            {
                cultureName = ValidateCulturePieceToLower(cultureName, "cultureName", cultureName.Length); 
                nameToLcid[cultureName]  = lcidArray[0]; 
                lcidToName[lcidArray[0]] = cultureName;
            } 


            for (int i=1; i<lcidArray.Length; i++)
            { 
                if (IsBuiltInCulture(lcidArray[i]))
                    continue; 
 
                cultureName = (string) CultureInfo.nativeGetCultureName(lcidArray[i], true, false);
                if (cultureName != null) 
                {
                    cultureName = ValidateCulturePieceToLower(cultureName, "cultureName", cultureName.Length);
                    nameToLcid[cultureName]  = lcidArray[i];
                    lcidToName[lcidArray[i]] = cultureName; 
                }
            } 
 
            return true;
        } 

        // <SyntheticSupport/>
        internal static void CacheSyntheticNameLcidMapping()
        { 

#if DICTIONARY_FOR_CULTURE_CACHE 
            Dictionary<int, String> lcidToName = new Dictionary<int, String>(); 
            Dictionary<String, int> nameToLcid = new Dictionary<String, int>();
#else 
            Hashtable lcidToName = new Hashtable();
            Hashtable nameToLcid = new Hashtable();
#endif // DICTIONARY_FOR_CULTURE_CACHE
 
            // the lock is important here because GetLcidAndCultureNames is using global static variable
            // which need to be protected accross the threads 
            int [] lcidArray = null; 
            bool result = false;
 
            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try
            { 
                // This is intentionally taking a process-global lock, using a CER
                // to guarantee we release the lock. 
                // Note that we should lock on a internal mscorlib type, so that 
                // no public caller can block us by locking on the same type.
                Monitor.ReliableEnter(typeof(CultureTableRecord), ref tookLock); 
                // nativeEnumSystemLocale uses a global variable in the native side, so
                // it needs process-wide synchronization.
                result = CultureInfo.nativeEnumSystemLocales(out lcidArray);
            } 
            finally
            { 
                if (tookLock) 
                    Monitor.Exit(typeof(CultureTableRecord));
            } 

            if (result)
            {
                if (!GetCultureNamesUsingSNameLCType(lcidArray, lcidToName, nameToLcid)) 
                {
                    // Coming here means we couldn't get the names through LOCALE_SNAME so now we'll try to get the names 
/* SSS_WARNINGS_OFF */                            // using the language, country, and script. /* SSS_WARNINGS_ON */ 
                    //
                    // culturesNeedingScriptInName is the hash which holds all the cultures that need a script tag in the name. 
                    // when we encounter a duplicate culture in the nameToLcid table then we know this name will need to have
                    // a script tag. then we remove the name from nameToLcid (to add the one with the script tag instead)
                    // and we add the the removed name to culturesNeedingScriptInName for future checking.
                    // 
                    Hashtable culturesNeedingScriptInName = GetNamesHashtable();
                    string script; 
                    StringBuilder stringHelper = new StringBuilder(); 

                    for (int i=0; i<lcidArray.Length; i++) 
                    {
                        int lcid = lcidArray[i];

                        if ( !IsBuiltInCulture(lcid) ) 
                        {
                            string                          cultureName; 
                            AdjustedSyntheticCultureName    adjustedNames; 

                            GetAdjustedNames(lcid, out adjustedNames); 
                            if (adjustedNames != null) { cultureName = adjustedNames.sName; }                        // Hardcoded culture name
                            else  { cultureName = (string) CultureInfo.nativeGetCultureName(lcid, false, false); }   // Calculated culture name

                            if (cultureName != null) 
                            {
                                cultureName = ValidateCulturePieceToLower(cultureName, "cultureName", cultureName.Length); 
                                if (culturesNeedingScriptInName[cultureName] != null) 
                                {
                                    // we already have 2 cultures so we need the script 
                                    if (GetScriptTag(lcid, out script))
                                    {
                                        script = Concatenate(stringHelper, cultureName, "-", script);
                                        script = GetQualifiedName(script); 
                                        BCLDebug.Assert(nameToLcid[script] == null, "[CultureTableRecord::CacheSyntheticNameLcidMapping] duplicates of the culture names.");
                                        nameToLcid[script]      = lcid; 
                                        lcidToName[lcid]        = script; 
                                    }
                                } 
                                else if (nameToLcid[cultureName] == null)
                                {
                                    // This is the first time weve seen this name.
                                    nameToLcid[cultureName] = lcid; 
                                    lcidToName[lcid]        = cultureName;
                                } 
                                else 
                                {
                                    // we already have a culture with this name, so we need to add scripts to both 
                                    int oldLcid = (int) nameToLcid[cultureName];
                                    nameToLcid.Remove(cultureName);
                                    lcidToName.Remove(oldLcid);
 
                                    culturesNeedingScriptInName[cultureName] = "";
 
                                    if (GetScriptTag(oldLcid, out script)) 
                                    {
                                        script = Concatenate(stringHelper, cultureName, "-", script); 
                                        script = GetQualifiedName(script);
                                        BCLDebug.Assert(nameToLcid[script] == null, "[CultureTableRecord::CacheSyntheticNameLcidMapping] duplicates of the culture names.");
                                        nameToLcid[script]      = oldLcid;
                                        lcidToName[oldLcid]     = script; 
                                    }
 
                                    if (GetScriptTag(lcid, out script)) 
                                    {
                                        script = Concatenate(stringHelper, cultureName, "-", script); 
                                        script = GetQualifiedName(script);
                                        BCLDebug.Assert(nameToLcid[script] == null, "[CultureTableRecord::CacheSyntheticNameLcidMapping] duplicates of the culture names.");
                                        nameToLcid[script]      = lcid;
                                        lcidToName[lcid]        = script; 
                                    }
                                } 
                            } 
                        }
                    } 
                }
            }

            lock (InternalSyncObject) 
            {
                SyntheticLcidToNameCache     = lcidToName; 
                SyntheticNameToLcidCache     = nameToLcid; 
            }
        } 

        // <SyntheticSupport/>
        struct CompositeCultureData
        { 
            internal string sname;
            internal string englishDisplayName; 
            internal string sNativeDisplayName; 
            internal string waCalendars;
            internal string consoleFallbackName; 
            internal string parentName;
            internal int    parentLcid;
        }
 
        // <SyntheticSupport/>
        // AdjustSyntheticCalendars is doing two things. First it ensures the native calendar names are stored at 
        // the correct index in the nativeCalendarNames array. Second it ensure the default calendar in the first 
        // listed one in the waCalendars.
        private static void AdjustSyntheticCalendars(ref CultureData data, ref CompositeCultureData compositeData) 
        {
            BCLDebug.Assert(data.waCalendars.Length > 0,
                    "[CultureTableRecord::AdjustSyntheticCalendars] we should have at least one calendar.");
 
            StringBuilder calendarsId           = new StringBuilder();
            int           defaultCalendarIndex  = 0; 
            ushort        maxCalendarId         = data.waCalendars[0]; 

            calendarsId.Append((char) maxCalendarId); 

            for (int i=1; i<data.waCalendars.Length; i++)
            {
                calendarsId.Append((char) data.waCalendars[i]); 

                if (data.waCalendars[i] == (ushort) data.iDefaultCalender) 
                    defaultCalendarIndex = i; 
                if (data.waCalendars[i] > maxCalendarId)
                    maxCalendarId = data.waCalendars[i]; 
            }

            if (maxCalendarId > 1)
            { 
                string [] nativeCalendarNames = new string[maxCalendarId];
                for (int i=0; i<nativeCalendarNames.Length; i++) 
                    nativeCalendarNames[i] = String.Empty; 

                for (int i=0; i<data.waCalendars.Length; i++) 
                {
                    nativeCalendarNames[data.waCalendars[i] - 1] = data.saNativeCalendarNames[i];
                }
 
                data.saNativeCalendarNames = nativeCalendarNames;
            } 
 
            if (defaultCalendarIndex > 0)
            { 
                char defaultCal                    = calendarsId[defaultCalendarIndex];
                calendarsId[defaultCalendarIndex]  = calendarsId[0];
                calendarsId[0]                     = defaultCal;
            } 

            compositeData.waCalendars = calendarsId.ToString(); 
        } 

        //******************************************************************************* 
        //  Get the synthetic (ELK) culture data
        //      o   it requests the data from the native side
        //      o   allocate native memory and fill it
        //      o   make m_pData and m_pPool point to the allocated data 
        //*******************************************************************************
 
        // <SyntheticSupport/> 
        [ResourceExposure(ResourceScope.None)]
        [ResourceConsumption(ResourceScope.Machine)] 
        private unsafe bool GetSyntheticCulture(int cultureID)
        {
            if (SyntheticLcidToNameCache == null || SyntheticNameToLcidCache == null)
            { 
                CacheSyntheticNameLcidMapping();
            } 
 
            if (SyntheticLcidToNameCache[cultureID] == null) { return false; }
 
            if (SyntheticDataCache == null)
#if DICTIONARY_FOR_CULTURE_CACHE
            { SyntheticDataCache = new Dictionary<int, AgileSafeNativeMemoryHandle>(); }
#else 
            { SyntheticDataCache = new Hashtable(); }
#endif // DICTIONARY_FOR_CULTURE_CACHE 
            else 
            {
                nativeMemoryHandle = (AgileSafeNativeMemoryHandle) SyntheticDataCache[cultureID]; 
            }

            if (nativeMemoryHandle != null)
            { 
                m_pData = (CultureTableData *) nativeMemoryHandle.DangerousGetHandle();
                m_pPool = (ushort*) (((byte *) m_pData) + sizeof(CultureTableData)); 
                m_CultureTable      = CultureTable.Default; 
                m_CultureName       = SNAME;
                m_CultureID         = cultureID; 
                m_synthetic         = true;
                m_ActualCultureID   = cultureID;
                m_ActualName        = m_CultureName;
                return true; 
            }
 
            CultureData data = new CultureData(); 

            bool success = false; 

            bool tookLock = false;
            RuntimeHelpers.PrepareConstrainedRegions();
            try 
            {
                // This is intentionally taking a process-global lock, using a CER 
                // to guarantee we release the lock. 
                // Note that we should lock on a internal mscorlib type, so that
                // no public caller can block us by locking on the same type. 
                Monitor.ReliableEnter(typeof(CultureTableRecord), ref tookLock);
                // we should lock on the call of CultureInfo.nativeGetCultureData because it uses native static
                // variables that are not safe to be accessed in same time from different threads.
                success = CultureInfo.nativeGetCultureData(cultureID, ref data); 
            }
            finally 
            { 
                if (tookLock)
                    Monitor.Exit(typeof(CultureTableRecord)); 
            }

            if (!success) { return false; }
 
            CompositeCultureData compositeData = new CompositeCultureData();
            int dataSize = GetCultureDataSize(cultureID, ref data, ref compositeData); 
 
            IntPtr handle = IntPtr.Zero;
            RuntimeHelpers.PrepareConstrainedRegions(); 
            try
            {
                RuntimeHelpers.PrepareConstrainedRegions();
                try { } 
                finally
                { 
                    handle = Marshal.AllocHGlobal(dataSize); 
                    if (handle != IntPtr.Zero)
                        nativeMemoryHandle = new AgileSafeNativeMemoryHandle(handle, true); 
                }
            }
            finally
            { 
                if (nativeMemoryHandle == null && handle != IntPtr.Zero)
                { 
                    // 
                    // if we came here means the thread aborted or OOM thrown after allocating our native memory
                    // we need to free this memory before we proceed so we'll not have any memory leaks. 
                    //

                    Marshal.FreeHGlobal(handle);
                    handle = IntPtr.Zero; 
                }
            } 
 
            if (handle == IntPtr.Zero)
            { 
                throw new OutOfMemoryException( Environment.GetResourceString("OutOfMemory_MemFailPoint") );
            }
            BCLDebug.Assert(!nativeMemoryHandle.IsInvalid, "[CultureTableRecord::GetSyntheticCulture] The native memory is not allocated.");
 
            m_pData = (CultureTableData *) nativeMemoryHandle.DangerousGetHandle();
            m_pPool = (ushort*) (((byte *) m_pData) + sizeof(CultureTableData)); 
 
            uint filledBytes = FillCultureDataMemory(cultureID, ref data, ref compositeData);
            BCLDebug.Assert(filledBytes == dataSize - sizeof(CultureTableData), String.Format( 
                CultureInfo.CurrentCulture,
                "[CultureTableRecord::GetSyntheticCulture] The allocated memory size {0} != Filled memory size {1}.",
                dataSize - sizeof(CultureTableData), filledBytes));
 
            m_CultureTable      = CultureTable.Default;
            m_CultureName       = SNAME; 
            m_CultureID         = cultureID; 
            m_synthetic         = true;
            m_ActualCultureID   = cultureID; 
            m_ActualName        = m_CultureName;

            lock (SyntheticDataCache)
            { 
                if (SyntheticDataCache[cultureID] == null) { SyntheticDataCache[cultureID] = nativeMemoryHandle; }
            } 
 
            BCLDebug.Assert(!nativeMemoryHandle.IsInvalid, "CultureTableData pointer was invalid!");
            return true; 
        }

/* SSS_WARNINGS_OFF */
        /* 
          ELK v1.0 was shipped with wrong ISO language and country names for the cultures listed below
          To support it we need to adjust the names to the correct values. 
 
          -------------------------------------------------------------------------------------------------------------
          LCID    Lang    Region  version Description                           correct SNAME   Wrong SNAME 
          --------------------------------------------------------------------------------------------------------------
          141a    bs        BA  ELK v1  Bosnian (Latin, Bosnia and Herzegovina) bs-BA-Latn      (bs-BA)
          243b    smn       FI  ELK v1  Sami, Inari (Finland)                   smn-FI          (se-FI)
          103b    smj       NO  ELK v1  Sami, Lule (Norway)                     smj-NO          (se-NO) 
          143b    smj       SE  ELK v1  Sami, Lule (Sweden)                     smj-SE          (se-SE)
          203b    sms       FI  ELK v1  Sami, Skolt (Finland)                   sms-FI          (se-FI) 
          183b    sma       NO  ELK v1  Sami, Southern (Norway)                 sma-NO          (se-NO) 
          1c3b    sma       SE  ELK v1  Sami, Southern (Sweden)                 sma-SE          (se-SE)
          046b    quz       BO  ELK v1  Cusco Quechua (Bolivia)                 quz-BO          (qu-BO) 
          086b    quz       EC  ELK v1  Cusco Quechua (Equador)                 quz-EC          (qu-EC)
          0c6b    quz       PE  ELK v1  Cusco Quechua (Peru)                    quz-PE          (qu-PE)
          ------------------------------------------------------------------------------------------------------------- */
/* SSS_WARNINGS_ON */ 
        // <SyntheticSupport/>
        internal class AdjustedSyntheticCultureName 
        { 
/* SSS_WARNINGS_OFF */                    internal AdjustedSyntheticCultureName(int lcid, string isoLanguage, string isoCountry, string sName) /* SSS_WARNINGS_ON */
            { 
                this.lcid           = lcid;
                this.isoLanguage    = isoLanguage;
/* SSS_WARNINGS_OFF */                        this.isoCountry     = isoCountry; /* SSS_WARNINGS_ON */
                this.sName          = sName; 
            }
 
            internal int    lcid; 
            internal string isoLanguage;
/* SSS_WARNINGS_OFF */                    internal string isoCountry; /* SSS_WARNINGS_ON */ 
            internal string sName;
        }

        // <SyntheticSupport/> 
        static AdjustedSyntheticCultureName [] s_adjustedSyntheticNames = null;
 
        // <SyntheticSupport/> 
        static AdjustedSyntheticCultureName [] AdjustedSyntheticNames
        { 
            get
            {
                if (s_adjustedSyntheticNames == null)
                { 
                    // <SyntheticSupport/>
                    // 
                    // IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT 
                    // If you added a new item which has a script tag as part of the name to the AdjustedSyntheticCultureName
                    // array then you need store the culture name (without the script part) into the hashtable in the method 
                    // GetNamesHashtable().
                    //
                    s_adjustedSyntheticNames = new AdjustedSyntheticCultureName[]
                    { 
                        new AdjustedSyntheticCultureName(0x141a, "bs",  "BA",  "bs-Latn-BA" ),       // ELK v1  Bosnian (Latin, Bosnia and Herzegovina) (bs-BA)
                        new AdjustedSyntheticCultureName(0x243b, "smn", "FI",  "smn-FI"     ),       // ELK v1  Sami, Inari (Finland)                   (se-FI) 
                        new AdjustedSyntheticCultureName(0x103b, "smj", "NO",  "smj-NO"     ),       // ELK v1  Sami, Lule (Norway)                     (se-NO) 
                        new AdjustedSyntheticCultureName(0x143b, "smj", "SE",  "smj-SE"     ),       // ELK v1  Sami, Lule (Sweden)                     (se-SE)
                        new AdjustedSyntheticCultureName(0x203b, "sms", "FI",  "sms-FI"     ),       // ELK v1  Sami, Skolt (Finland)                   (se-FI) 
                        new AdjustedSyntheticCultureName(0x183b, "sma", "NO",  "sma-NO"     ),       // ELK v1  Sami, Southern (Norway)                 (se-NO)
                        new AdjustedSyntheticCultureName(0x1c3b, "sma", "SE",  "sma-SE"     ),       // ELK v1  Sami, Southern (Sweden)                 (se-SE)
                        new AdjustedSyntheticCultureName(0x046b, "quz", "BO",  "quz-BO"     ),       // ELK v1  Cusco Quechua (Bolivia)                 (qu-BO)
                        new AdjustedSyntheticCultureName(0x086b, "quz", "EC",  "quz-EC"     ),       // ELK v1  Cusco Quechua (Equador)                 (qu-EC) 
                        new AdjustedSyntheticCultureName(0x0c6b, "quz", "PE",  "quz-PE"     )        // ELK v1  Cusco Quechua (Peru)                    (qu-PE)
                    }; 
                } 
                return (s_adjustedSyntheticNames);
            } 
        }

        // <SyntheticSupport/>
        internal static Hashtable GetNamesHashtable() 
        {
            Hashtable table = new Hashtable(); 
 
            // All keys is the hashtable should be in lowercase.
            table["bs-ba"] = ""; 
            table["tg-tj"] = "";
            table["mn-cn"] = "";
            table["iu-ca"] = "";
 
            return table;
        } 
 
        // <SyntheticSupport/>
        internal static void GetAdjustedNames(int lcid, out AdjustedSyntheticCultureName adjustedNames) 
        {

            for (int i=0; i<AdjustedSyntheticNames.Length; i++)
            { 
                if (AdjustedSyntheticNames[i].lcid == lcid) { adjustedNames = AdjustedSyntheticNames[i]; return; }
            } 
 
            adjustedNames = null;
        } 

        //
        // FillCultureDataMemory fill the data into the allocated memory and return the number of bytes
        // used to fill the whole data. 
        //
        // <SyntheticSupport/> 
        private unsafe uint FillCultureDataMemory(int cultureID, ref CultureData data, ref CompositeCultureData compositeData) 
        {
            BCLDebug.Assert(m_pData != null && m_pPool != null, "[CultureTableRecord::FillCultureDataMemory] we should have valid buffers here."); 

            uint offset = 0;
            Hashtable offsetTable = new Hashtable(30);
 
            // Add padding to get our funny dword alignment
            m_pPool[offset] = 0; 
            offset++; 

            // Set empty string and correct its offset 
            SetPoolString("", offsetTable, ref offset);
            offsetTable[""] = (uint)0;

            m_pData->iLanguage          = (ushort) cultureID; 
            m_pData->sName              = (ushort) SetPoolString(compositeData.sname, offsetTable, ref offset);
            m_pData->iDigits            = (ushort) data.iDigits; 
            m_pData->iNegativeNumber    = (ushort) data.iNegativeNumber; 
            m_pData->iCurrencyDigits    = (ushort) data.iCurrencyDigits;
            m_pData->iCurrency          = (ushort) data.iCurrency; 
            m_pData->iNegativeCurrency  = (ushort) data.iNegativeCurrency;
            m_pData->iLeadingZeros      = (ushort) data.iLeadingZeros;
            m_pData->iFlags             = (ushort) CultureFlags.IsSpecificCulture;
            m_pData->iFirstDayOfWeek    = ConvertFirstDayOfWeekMonToSun(data.iFirstDayOfWeek); 
            m_pData->iFirstWeekOfYear   = (ushort) data.iFirstWeekOfYear;
/* SSS_WARNINGS_OFF */                    m_pData->iCountry           = (ushort) data.iCountry; /* SSS_WARNINGS_ON */ 
            m_pData->iMeasure           = (ushort) data.iMeasure; 
            m_pData->iDigitSubstitution = (ushort) data.iDigitSubstitution;
            m_pData->waGrouping         = (ushort) SetPoolString(data.waGrouping, offsetTable, ref offset); 
            m_pData->waMonetaryGrouping = (ushort) SetPoolString(data.waMonetaryGrouping, offsetTable, ref offset);
            m_pData->sListSeparator     = (ushort) SetPoolString(data.sListSeparator, offsetTable, ref offset);
            m_pData->sDecimalSeparator  = (ushort) SetPoolString(data.sDecimalSeparator, offsetTable, ref offset);
            m_pData->sThousandSeparator = (ushort) SetPoolString(data.sThousandSeparator, offsetTable, ref offset); 
            m_pData->sCurrency          = (ushort) SetPoolString(data.sCurrency, offsetTable, ref offset);
            m_pData->sMonetaryDecimal   = (ushort) SetPoolString(data.sMonetaryDecimal, offsetTable, ref offset); 
            m_pData->sMonetaryThousand  = (ushort) SetPoolString(data.sMonetaryThousand, offsetTable, ref offset); 
            m_pData->sPositiveSign      = (ushort) SetPoolString(data.sPositiveSign, offsetTable, ref offset);
            m_pData->sNegativeSign      = (ushort) SetPoolString(data.sNegativeSign, offsetTable, ref offset); 
            m_pData->sAM1159            = (ushort) SetPoolString(data.sAM1159, offsetTable, ref offset);
            m_pData->sPM2359            = (ushort) SetPoolString(data.sPM2359, offsetTable, ref offset);
            m_pData->saNativeDigits     = (ushort) SetPoolStringArrayFromSingleString(data.saNativeDigits, offsetTable, ref offset);
            m_pData->saTimeFormat       = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saTimeFormat); 
            m_pData->saShortDate        = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saShortDate);
            m_pData->saLongDate         = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saLongDate); 
            m_pData->saYearMonth        = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saYearMonth); 
            m_pData->saDuration         = (ushort) SetPoolStringArray(offsetTable, ref offset, "");
            m_pData->iDefaultLanguage   = m_pData->iLanguage; 
            m_pData->iDefaultAnsiCodePage=(ushort) data.iDefaultAnsiCodePage;
            m_pData->iDefaultOemCodePage= (ushort) data.iDefaultOemCodePage;
            m_pData->iDefaultMacCodePage= (ushort) data.iDefaultMacCodePage;
            m_pData->iDefaultEbcdicCodePage= (ushort) data.iDefaultEbcdicCodePage; 
            m_pData->iGeoId             = (ushort) data.iGeoId;
            m_pData->iPaperSize         = (ushort) data.iPaperSize; 
            m_pData->iIntlCurrencyDigits= (ushort) data.iIntlCurrencyDigits; 
            m_pData->iParent            = (ushort) compositeData.parentLcid;
            m_pData->waCalendars        = (ushort) SetPoolString(compositeData.waCalendars, offsetTable, ref offset); 
            m_pData->sAbbrevLang        = (ushort) SetPoolString(data.sAbbrevLang, offsetTable, ref offset);
            m_pData->sISO639Language    = (ushort) SetPoolString(data.sIso639Language, offsetTable, ref offset);
            m_pData->sEnglishLanguage   = (ushort) SetPoolString(data.sEnglishLanguage, offsetTable, ref offset);
            m_pData->sNativeLanguage    = (ushort) SetPoolString(data.sNativeLanguage, offsetTable, ref offset); 
/* SSS_WARNINGS_OFF */                    m_pData->sEnglishCountry    = (ushort) SetPoolString(data.sEnglishCountry, offsetTable, ref offset); /* SSS_WARNINGS_ON */
/* SSS_WARNINGS_OFF */                    m_pData->sNativeCountry     = (ushort) SetPoolString(data.sNativeCountry, offsetTable, ref offset); /* SSS_WARNINGS_ON */ 
/* SSS_WARNINGS_OFF */                    m_pData->sAbbrevCountry     = (ushort) SetPoolString(data.sAbbrevCountry, offsetTable, ref offset); /* SSS_WARNINGS_ON */ 
/* SSS_WARNINGS_OFF */                    m_pData->sISO3166CountryName= (ushort) SetPoolString(data.sIso3166CountryName, offsetTable, ref offset); /* SSS_WARNINGS_ON */
            m_pData->sIntlMonetarySymbol= (ushort) SetPoolString(data.sIntlMonetarySymbol, offsetTable, ref offset); 
            m_pData->sEnglishCurrency   = (ushort) SetPoolString(data.sEnglishCurrency, offsetTable, ref offset);
            m_pData->sNativeCurrency    = (ushort) SetPoolString(data.sNativeCurrency, offsetTable, ref offset);
            m_pData->waFontSignature    = (ushort) SetPoolString(data.waFontSignature, offsetTable, ref offset);
            m_pData->sISO639Language2   = (ushort) SetPoolString(data.sISO639Language2, offsetTable, ref offset); 
/* SSS_WARNINGS_OFF */                    m_pData->sISO3166CountryName2= (ushort) SetPoolString(data.sISO3166CountryName2, offsetTable, ref offset); /* SSS_WARNINGS_ON */
            m_pData->sParent            = (ushort) SetPoolString(compositeData.parentName, offsetTable, ref offset); 
            m_pData->saDayNames         = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saDayNames); 
            m_pData->saAbbrevDayNames   = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saAbbrevDayNames);
            m_pData->saMonthNames       = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saMonthNames); 
            m_pData->saAbbrevMonthNames = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saAbbrevMonthNames);

            m_pData->saMonthGenitiveNames = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saGenitiveMonthNames);
            m_pData->saAbbrevMonthGenitiveNames = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saAbbrevGenitiveMonthNames); 

            m_pData->saNativeCalendarNames      = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saNativeCalendarNames); 
 
            // To Do: We need to find a better way to get the alternative sort names.
            // look at ComNldInfo::nativeGetCultureData and CultureTableRecord.GetCultureDataSize 
            // m_pData->saAltSortID        = (ushort) SetPoolStringArray(offsetTable, ref offset, data.saAltSortID);

            m_pData->saAltSortID        = (ushort) SetPoolStringArray(offsetTable, ref offset, "");
            m_pData->iNegativePercent   = (ushort) CultureInfo.InvariantCulture.NumberFormat.PercentNegativePattern; 
            m_pData->iPositivePercent   = (ushort) CultureInfo.InvariantCulture.NumberFormat.PercentPositivePattern;
            m_pData->iFormatFlags       = (ushort) DateTimeFormatFlags.None; 
            // We need to keep this since we need value for TextInfo.IsRightToLeft. 
            m_pData->iLineOrientations  = (ushort) 0x0000;
            m_pData->iTextInfo          = m_pData->iLanguage; 
            m_pData->iInputLanguageHandle=m_pData->iLanguage;
            m_pData->iCompareInfo       = m_pData->iLanguage;
            m_pData->sEnglishDisplayName= (ushort) SetPoolString(compositeData.englishDisplayName, offsetTable, ref offset);
            m_pData->sNativeDisplayName = (ushort) SetPoolString(compositeData.sNativeDisplayName, offsetTable, ref offset); 
            m_pData->sPercent           = (ushort) SetPoolString(CultureInfo.InvariantCulture.NumberFormat.PercentSymbol, offsetTable, ref offset);
            m_pData->sNaN               = (ushort) SetPoolString(data.sNaN, offsetTable, ref offset); 
            m_pData->sPositiveInfinity  = (ushort) SetPoolString(data.sPositiveInfinity, offsetTable, ref offset); 
            m_pData->sNegativeInfinity  = (ushort) SetPoolString(data.sNegativeInfinity, offsetTable, ref offset);
            m_pData->sMonthDay          = (ushort) SetPoolString(CultureInfo.InvariantCulture.DateTimeFormat.MonthDayPattern, offsetTable, ref offset); 
            m_pData->sAdEra             = (ushort) SetPoolString(CultureInfo.InvariantCulture.DateTimeFormat.GetEraName(0), offsetTable, ref offset);
            m_pData->sAbbrevAdEra       = (ushort) SetPoolString(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedEraName(0), offsetTable, ref offset);
/* SSS_WARNINGS_OFF */            m_pData->sRegionName        = m_pData->sISO3166CountryName; /* SSS_WARNINGS_ON */
            m_pData->sConsoleFallbackName= (ushort) SetPoolString(compositeData.consoleFallbackName, offsetTable, ref offset); 
            m_pData->saShortTime        = m_pData->saTimeFormat;
            m_pData->saSuperShortDayNames= (ushort) SetPoolStringArray(offsetTable, ref offset, data.saSuperShortDayNames); 
            m_pData->saDateWords        = m_pData->saDuration; 
            m_pData->sSpecificCulture   = m_pData->sName;
            m_pData->sScripts           = 0; 

            // Offset is in words, but we return size in bytes
            return 2 * offset;
        } 

        // Sets a string in the pool, returns string offset in words 
        // <SyntheticSupport/> 
        private unsafe uint SetPoolString(string s, Hashtable offsetTable, ref uint currentOffset)
        { 
            BCLDebug.Assert((currentOffset & 1) == 1,
                "[CultureTableRecord.SetPoolString]Expected offset to be on odd word");
            uint offset = currentOffset;
 
            if (offsetTable[s] == null)
            { 
                offsetTable[s] = currentOffset; 
                m_pPool[currentOffset] = (ushort) s.Length;
                currentOffset++; 

                for (int i=0; i<s.Length; i++)
                {
                    m_pPool[currentOffset] = (ushort) s[i]; 
                    currentOffset++;
                } 
 
                // Keep the alignment
                if ((currentOffset & 1) == 0) 
                {
                    m_pPool[currentOffset] = (ushort) 0;
                    currentOffset++;
                } 

                return offset; 
            } 

            return (uint)offsetTable[s]; 
        }

        // Sets an array of strings in the pool, returns the offset of that array (in words)
        // <SyntheticSupport/> 
        private unsafe uint SetPoolStringArray(Hashtable offsetTable, ref uint currentOffset, params string [] array)
        { 
            BCLDebug.Assert((currentOffset & 1) == 1, 
                "[CultureTableRecord.SetPoolStringArray]Expected offset to be on odd word");
 
            uint [] offsetArray = new uint[array.Length];
            for (int i=0; i<array.Length; i++)
            {
                offsetArray[i] = SetPoolString(array[i], offsetTable, ref currentOffset); 
            }
 
            // We should still be odd WORD aligned 
            BCLDebug.Assert((currentOffset & 1) == 1,
                "[CultureTableRecord.SetPoolStringArray]Expected offset to remain on odd word"); 

            // Remember the offset that starts our array (so we can return it)
            uint offset = currentOffset;
 
            // Remember the length
            m_pPool[currentOffset] = (ushort) offsetArray.Length; 
            currentOffset++; 

            // Need a uint pool 
            uint* uints = (uint*)(m_pPool + currentOffset);

            for (int i=0; i<offsetArray.Length; i++)
            { 
                uints[i] = offsetArray[i];
                currentOffset+=2;   // 2 WORDS in a DWORD 
            } 

            // Keep the alignment if necessary 
            if ((currentOffset & 1) == 0)
            {
                m_pPool[currentOffset] = (ushort) 0;
                currentOffset++; 
            }
 
            return (offset); 
        }
 
        // Sets an array of strings in the pool from a single string, returns the offset of that array (in words)
        // <SyntheticSupport/>
        private unsafe uint SetPoolStringArrayFromSingleString(string s, Hashtable offsetTable, ref uint currentOffset)
        { 
            BCLDebug.Assert(s.Length==10,
                "[CultureTableRecord.SetPoolStringArrayFromSingleString]Expect 10 digits in the saNativeDigits string"); 
            String[] strings = new string[s.Length]; 
            for (int i=0; i< s.Length; i++)
                strings[i] = s.Substring(i,1); 
            return SetPoolStringArray(offsetTable, ref currentOffset, strings);
        }

 
        // <SyntheticSupport/>
        private bool NameHasScriptTag(string tempName) 
        { 
            int dashCount = 0;
            int i= 0; 
            while (i<tempName.Length && dashCount<2)
            {
                if (tempName[i] == '-') { dashCount++; }
                i++; 
            }
            return (dashCount>1); 
        } 

 
        //
        // This method construct the culture name (should be called only when we have script tag).
        //
        // <SyntheticSupport/> 
        private static string GetCasedName(string name)
        { 
            StringBuilder taggedName = new StringBuilder(name.Length); 

            int i = 0; 
            while (i<name.Length && name[i] != '-') // Language part
            {
                taggedName.Append(name[i]);
                i++; 
            }
 
            taggedName.Append("-"); // insert two dashs. we are going to insert script in between later 
            i++;
 
            char c;

            // First character in the script is uppercase and the rest is lower case.
            c = Char.ToUpper(name[i], CultureInfo.InvariantCulture); 
            taggedName.Append(c);
            i++; 
 
            while (i<name.Length && name[i] != '-') // Region part
            { 
                taggedName.Append(name[i]);
                i++;
            }
 
            taggedName.Append("-"); // insert two dashs. we are going to insert script in between later
            i++; 
 
            while (i<name.Length ) // rest of the script
            { 
                c = Char.ToUpper(name[i], CultureInfo.InvariantCulture);
                taggedName.Append(c);
                i++;
            } 

            return taggedName.ToString(); 
        } 

 
        // GetQualifiedName will produce a name with the format Lang-Script-Region
        private static string GetQualifiedName(string name)
        {
            StringBuilder taggedName = new StringBuilder(name.Length); 

            int i = 0; 
            while (i<name.Length && name[i] != '-') // Language part 
            {
                taggedName.Append(name[i]); 
                i++;
            }

            taggedName.Append("--"); // insert two dashs. we are going to insert script in between later 
            i++;
 
            int insertionPosition = i; 

            while (i<name.Length && name[i] != '-') // Region part 
            {
                taggedName.Append(name[i]);
                i++;
            } 

            i++; 
 
            while (i<name.Length ) // rest of the script
            { 
                taggedName.Insert(insertionPosition, name[i]);
                insertionPosition++;
                i++;
            } 

            return taggedName.ToString(); 
        } 

 
        // <SyntheticSupport/>
        private static void GetSyntheticParentData(ref CultureData data, ref CompositeCultureData  compositeData)
        {
            compositeData.parentLcid = CultureInfo.InvariantCulture.LCID; 
            compositeData.parentName = CultureInfo.InvariantCulture.Name;
 
            if (data.sParentName != null) 
            {
                // Vista or above 
                string  defaultTableActualName;
                string  parentName;
                int     defaultTableCultureID;
 
                parentName = ValidateCulturePieceToLower(data.sParentName, "ParentName", MAXSIZE_FULLTAGNAME);
                int defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureName(parentName, out defaultTableCultureID, out defaultTableActualName); 
 
                if (defaultTableDataItem >= 0)
                { 
                    compositeData.parentLcid = defaultTableCultureID;
                    compositeData.parentName = defaultTableActualName;
                }
                else if (SyntheticNameToLcidCache[parentName] != null) 
                {
                    compositeData.parentLcid = (int) SyntheticNameToLcidCache[parentName]; 
                    compositeData.parentName = data.sParentName; 
                }
            } 
        }

        // <SyntheticSupport/>
        private static void GetSyntheticConsoleFallback(ref CultureData data, ref CompositeCultureData  compositeData) 
        {
            compositeData.consoleFallbackName = CultureInfo.InvariantCulture.GetConsoleFallbackUICulture().Name; 
 
            if (data.sConsoleFallbackName != null)
            { 
                // Vista or above
                string  defaultTableActualName;
                string  consoleFallbackName;
                int     defaultTableCultureID; 

                consoleFallbackName = ValidateCulturePieceToLower(data.sConsoleFallbackName, "ConsoleFallbackName", MAXSIZE_FULLTAGNAME); 
                int defaultTableDataItem = CultureTable.Default.GetDataItemFromCultureName(consoleFallbackName, out defaultTableCultureID, out defaultTableActualName); 

                if (defaultTableDataItem >= 0) 
                {
                    compositeData.consoleFallbackName = defaultTableActualName;
                }
                else if (SyntheticNameToLcidCache[consoleFallbackName] != null) 
                {
                    compositeData.consoleFallbackName = data.sConsoleFallbackName; 
                } 
            }
        } 


        // Get the pool size of our data in bytes
        // <SyntheticSupport/> 
        private unsafe int GetCultureDataSize(int cultureID, ref CultureData data, ref CompositeCultureData compositeData)
        { 
            int size = sizeof(CultureTableData); 

            Hashtable offsetTable = new Hashtable(30); 

            // Get us odd word aligned so that our DWORD arrays don't get out of alignment
            size += 2;
 
            // CultureTableData.sName (including the hyphen, length, and null termination)
            size += GetPoolStringSize("", offsetTable); 
 
            // try to detect if we are in platform that supports LOCALE_SNAME by calling nativeGetCultureName.
            // if so we don't have to adjust the names. 
            compositeData.sname = CultureInfo.nativeGetCultureName(cultureID, true, false);
            if (compositeData.sname == null)
            {
                AdjustedSyntheticCultureName adjustedNames; 
                GetAdjustedNames(cultureID, out adjustedNames);
                if (adjustedNames != null) 
                { 
                    data.sIso639Language    = adjustedNames.isoLanguage;
/* SSS_WARNINGS_OFF */                    data.sIso3166CountryName= adjustedNames.isoCountry; /* SSS_WARNINGS_ON */ 
                    compositeData.sname     = adjustedNames.sName;
                }
                else
                { 
                    // this part to make the culture name in proper letter casing.
                    string tempName = (string) SyntheticLcidToNameCache[cultureID]; 
                    if ( NameHasScriptTag(tempName) ) 
                    {
                        compositeData.sname = GetCasedName(tempName); 
                    }
                    else
                    {
                        // construct sname in proper letter casing. 
/* SSS_WARNINGS_OFF */                        compositeData.sname = data.sIso639Language + "-" + data.sIso3166CountryName; /* SSS_WARNINGS_ON */
                    } 
                } 
            }
 
/* SSS_WARNINGS_OFF */            compositeData.englishDisplayName = data.sEnglishLanguage + " (" + data.sEnglishCountry + ")"; /* SSS_WARNINGS_ON */
/* SSS_WARNINGS_OFF */            compositeData.sNativeDisplayName = data.sNativeLanguage  + " (" + data.sNativeCountry  + ")"; /* SSS_WARNINGS_ON */

            // CultureTableData.waCalendars 
            AdjustSyntheticCalendars(ref data, ref compositeData);
 
            size += GetPoolStringSize(compositeData.sname,                offsetTable); 
            size += GetPoolStringSize(compositeData.englishDisplayName,   offsetTable);
            size += GetPoolStringSize(compositeData.sNativeDisplayName,   offsetTable); 
            size += GetPoolStringSize(compositeData.waCalendars,          offsetTable);

            // CultureTableData.sParent
            GetSyntheticParentData(ref data, ref compositeData); 
            BCLDebug.Assert(compositeData.parentName != null,
                "[CultureTableRecord::GetCultureDataSize] compositeData.parentName should be valid."); 
 
            size += GetPoolStringSize(compositeData.parentName, offsetTable);
 
            // CultureTableData.sIso639Language
            size += GetPoolStringSize(data.sIso639Language, offsetTable);

            // CultureTableData.sListSeparator 
            size += GetPoolStringSize(data.sListSeparator, offsetTable);
 
            // CultureTableData.sDecimalSeparator 
            size += GetPoolStringSize(data.sDecimalSeparator, offsetTable);
 
            // CultureTableData.sThousandSeparator
            size += GetPoolStringSize(data.sThousandSeparator, offsetTable);

            // CultureTableData.sCurrency 
            size += GetPoolStringSize(data.sCurrency, offsetTable);
 
            // CultureTableData.sMonetaryDecimal 
            size += GetPoolStringSize(data.sMonetaryDecimal, offsetTable);
 
            // CultureTableData.sMonetaryThousand
            size += GetPoolStringSize(data.sMonetaryThousand, offsetTable);

            // CultureTableData.sPositiveSign 
            size += GetPoolStringSize(data.sPositiveSign, offsetTable);
 
            // CultureTableData.sNegativeSign 
            size += GetPoolStringSize(data.sNegativeSign, offsetTable);
 
            // CultureTableData.sAM1159
            size += GetPoolStringSize(data.sAM1159, offsetTable);

            // CultureTableData.sPM2359 
            size += GetPoolStringSize(data.sPM2359, offsetTable);
 
            // CultureTableData.sAbbrevLang 
            size += GetPoolStringSize(data.sAbbrevLang, offsetTable);
 
            // CultureTableData.sEnglishLanguage
            size += GetPoolStringSize(data.sEnglishLanguage, offsetTable);

            // CultureTableData.sNativeLanguage 
            size += GetPoolStringSize(data.sNativeLanguage, offsetTable);
/* SSS_WARNINGS_OFF */ 
            // CultureTableData.sEnglishCountry 
            size += GetPoolStringSize(data.sEnglishCountry, offsetTable);
 
            // CultureTableData.sNativeCountry
            size += GetPoolStringSize(data.sNativeCountry, offsetTable);

            // CultureTableData.sAbbrevCountry 
            size += GetPoolStringSize(data.sAbbrevCountry, offsetTable);
 
            // CultureTableData.sISO3166CountryName 
            size += GetPoolStringSize(data.sIso3166CountryName, offsetTable);
/* SSS_WARNINGS_ON */ 
            // CultureTableData.sIntlMonetarySymbol
            size += GetPoolStringSize(data.sIntlMonetarySymbol, offsetTable);

            // CultureTableData.sEnglishCurrency 
            size += GetPoolStringSize(data.sEnglishCurrency, offsetTable);
 
            // CultureTableData.sNativeCurrency 
            size += GetPoolStringSize(data.sNativeCurrency, offsetTable);
 
            // CultureTableData.sPercent
            size += GetPoolStringSize(CultureInfo.InvariantCulture.NumberFormat.PercentSymbol, offsetTable);

            // CultureTableData.sNaN 
            if (data.sNaN == null)
                data.sNaN = CultureInfo.InvariantCulture.NumberFormat.NaNSymbol; 
            size += GetPoolStringSize(data.sNaN, offsetTable); 

            // CultureTableData.sPositiveInfinity 
            if (data.sPositiveInfinity == null)
                data.sPositiveInfinity = CultureInfo.InvariantCulture.NumberFormat.PositiveInfinitySymbol;
            size += GetPoolStringSize(data.sPositiveInfinity, offsetTable);
 
            // CultureTableData.sNegativeInfinity
            if (data.sNegativeInfinity == null) 
                data.sNegativeInfinity = CultureInfo.InvariantCulture.NumberFormat.NegativeInfinitySymbol; 
            size += GetPoolStringSize(data.sNegativeInfinity, offsetTable);
 
            // CultureTableData.sMonthDay
            size += GetPoolStringSize(CultureInfo.InvariantCulture.DateTimeFormat.MonthDayPattern, offsetTable);

            // CultureTableData.sAdEra 
            size += GetPoolStringSize(CultureInfo.InvariantCulture.DateTimeFormat.GetEraName(0), offsetTable);
 
            // CultureTableData.sAbbrevAdEra 
            size += GetPoolStringSize(CultureInfo.InvariantCulture.DateTimeFormat.GetAbbreviatedEraName(0), offsetTable);
 
            // CultureTableData.sConsoleFallbackName
            GetSyntheticConsoleFallback(ref data, ref compositeData);
            BCLDebug.Assert(compositeData.consoleFallbackName != null,
                "[CultureTableRecord::GetCultureDataSize] compositeData.consoleFallbackName should be valid."); 

            size += GetPoolStringSize(compositeData.consoleFallbackName, offsetTable); 
 
            // CultureTableData.saMonthNames
            size += GetPoolStringArraySize(offsetTable, data.saMonthNames); 

            // CultureTableData.saDayNames
            size += GetPoolStringArraySize(offsetTable, data.saDayNames);
 
            // CultureTableData.saAbbrevDayNames
            size += GetPoolStringArraySize(offsetTable, data.saAbbrevDayNames); 
 
            // CultureTableData.saAbbrevMonthNames
            size += GetPoolStringArraySize(offsetTable, data.saAbbrevMonthNames); 

            // CultureTableData.saMonthGenitiveNames
            data.saGenitiveMonthNames[12] = data.saMonthNames[12];
            size += GetPoolStringArraySize(offsetTable, data.saGenitiveMonthNames); 

            // CultureTableData.saAbbrevMonthGenitiveNames 
            data.saAbbrevGenitiveMonthNames[12] = data.saAbbrevMonthNames[12]; 
            size += GetPoolStringArraySize(offsetTable, data.saAbbrevGenitiveMonthNames);
 
            // CultureTableData.saNativeCalendarNames
            size += GetPoolStringArraySize(offsetTable, data.saNativeCalendarNames);

            // CultureTableData.saTimeFormat 
            size += GetPoolStringArraySize(offsetTable, data.saTimeFormat);
 
            // CultureTableData.saShortDate 
            size += GetPoolStringArraySize(offsetTable, data.saShortDate);
 
            // CultureTableData.saLongDate
            size += GetPoolStringArraySize(offsetTable, data.saLongDate);

            // CultureTableData.saYearMonth 
            size += GetPoolStringArraySize(offsetTable, data.saYearMonth);
 
            // used for CultureTableData.saDuration/saDateWords 
            size += GetPoolStringArraySize(offsetTable, "");
 
            // To Do: We need to find a better way to get the alternative sort names.
            // look at ComNldInfo::nativeGetCultureData and CultureTableRecord.FillCultureDataMemory
            // size += GetPoolStringArraySize(offsetTable, data.saAltSortID);
 
            // CultureTableData.saAltSortID and CultureTableData.saDateWords
            size += GetPoolStringArraySize(offsetTable, ""); 
 
            // CultureTableData.waGrouping
            data.waGrouping = GroupSizesConstruction(data.waGrouping); 
            size += GetPoolStringSize(data.waGrouping, offsetTable);

            // CultureTableData.waMonetaryGrouping
            data.waMonetaryGrouping = GroupSizesConstruction(data.waMonetaryGrouping); 
            size += GetPoolStringSize(data.waMonetaryGrouping, offsetTable);
 
            // CultureTableData.saNativeDigits 
            size += GetPoolStringArraySize(data.saNativeDigits, offsetTable);
 
            // CultureTableData.waFontSignature
            size += GetPoolStringSize(data.waFontSignature, offsetTable);
/* SSS_WARNINGS_OFF */
            if (data.sISO3166CountryName2 == null) 
                data.sISO3166CountryName2 = data.sIso3166CountryName;
            size += GetPoolStringSize(data.sISO3166CountryName2, offsetTable); 
/* SSS_WARNINGS_ON */ 
            if (data.sISO639Language2 == null)
                data.sISO639Language2 = data.sIso639Language; 
            size += GetPoolStringSize(data.sISO639Language2, offsetTable);

            if (data.saSuperShortDayNames == null)
                data.saSuperShortDayNames = data.saAbbrevDayNames; 
            size += GetPoolStringArraySize(offsetTable, data.saSuperShortDayNames);
 
            return size; 
        }
 
        // Return the size of a string in the string pool in bytes
        // This needs to have an even # of words so that any following DWORD
        // array doesn't get unaligned
        // <SyntheticSupport/> 
        private int GetPoolStringSize(string s, Hashtable offsetTable)
        { 
            // 0 size if we know about it already 
            int size = 0;
 
            // See if we already knew about it
            if (offsetTable[s] == null)
            {
                offsetTable[s] = ""; 
                size = 2 * (s.Length + 1 +         // adding one for the length
                            (1 - (s.Length & 1)));  // Plus one to keep us odd aligned if necessary 
            } 

            BCLDebug.Assert((size & 3) == 0, 
                "[CultureTableRecord.GetPoolStringSize]Didn't maintain dword alignment");
            return size;
        }
 
        // Writing every character as seperate string in a string array (size of that array & strings in bytes)
        // <SyntheticSupport/> 
        private int GetPoolStringArraySize(string s, Hashtable offsetTable) 
        {
            // We have to do this the slow way just in case one of these strings is duplicated in the string pool 
            BCLDebug.Assert(s.Length==10,
                "[CultureTableRecord.GetPoolStringArraySize(string, int)]Expect 10 digits in the saNativeDigits string");
            String[] strings = new string[s.Length];
            for (int i=0; i< s.Length; i++) 
                strings[i] = s.Substring(i,1);
 
            return GetPoolStringArraySize(offsetTable, strings); 
        }
 
        // Given array of strings we calculate bytes needed to store them. note it can take one string.
        // <SyntheticSupport/>
        private int GetPoolStringArraySize(Hashtable offsetTable, params string [] array)
        { 
            int stringsSize = 0;
 
            for (int i=0; i<array.Length; i++) 
            {
                stringsSize += GetPoolStringSize(array[i], offsetTable); 
            }

            stringsSize += 2 * (array.Length * 2 + 1 + 1);       // (2 words per entry + 1 count word count + 1 word alignment) * 2 bytes/word
 
            BCLDebug.Assert((stringsSize & 3) == 0,
                "[CultureTableRecord.GetPoolStringSize]Didn't maintain dword alignment"); 
 
            return stringsSize;
        } 

        //
        // Group sizes format is single digit followed by a semicolon.
        // 
        // <SyntheticSupport/>
        private string GroupSizesConstruction(string rawGroupSize) 
        { 
            int length = rawGroupSize.Length;
            if ( rawGroupSize[length - 1] == '0' ) 
            {
                // if Win32 returned zero at the end of the string then weneed to remove it out.
                length--;
            } 

            int i=0; 
            StringBuilder groupSize = new StringBuilder(); 
            while (i<length)
            { 
                BCLDebug.Assert(rawGroupSize[i] >= '0' && rawGroupSize[i] <= '9', "[CultureTableRecord::GroupSizesConstruction] invalid group size number.");
                groupSize.Append((char) ((int) rawGroupSize[i] - (int) '0'));

                i++; 

                if (i<length) 
                { 
                    BCLDebug.Assert(rawGroupSize[i] == ';', "[CultureTableRecord::GroupSizesConstruction] group size separator should be semicolon.");
                    i++; 
                }
            }

            if (length == rawGroupSize.Length) 
            {
                // if Win32 didn't return zero at the end of the string then we need to add it. 
                groupSize.Append((char) 0); 
            }
 
            string groupSizesString = groupSize.ToString();
            BCLDebug.Assert(groupSizesString != null && groupSizesString.Length > 0, "[CultureTableRecord::GroupSizesConstruction] null group sizes.");

            return (groupSizesString); 
        }
 
#endif // !FEATURE_PAL 

 
        private String WindowsPath
        {
            get
            { 
                if (m_windowsPath == null)
                { 
                    m_windowsPath = CultureInfo.nativeGetWindowsDirectory(); 
                }
                return (m_windowsPath); 
            }
        }

        /*---------------------------------------------------------- 
        *
        * Gets a filename of a custom culture covered by the given string. 
        * 
        * Builds a cache of items found (adding String.Empty for files that are not present)
        * to avoid hitting the disk repeatedly. 
        *
        *--------------------------------------------------------*/
        private string GetCustomCultureFile(string name)
        { 
#if !FEATURE_PAL
            // Calling SHGetFolderPath is expensive.  Cache whether 
            // we need to load a data file or not. 

            StringBuilder sbFile = new StringBuilder(WindowsPath); 

            sbFile.Append("\\Globalization\\");
            sbFile.Append(name);
            sbFile.Append(".nlp"); 

            // GetFullPath should not be needed here since a full path is being generated 
            string file = (sbFile.ToString()); 

            bool customCultureExists = CultureInfo.nativeFileExists(file); 

            if (customCultureExists)
                return(file);
#endif // !FEATURE_PAL 
            return(null);
        } 
 
        /*----------------------------------------------------------
        * 
        * Validate a culture name -- throws if it is not valid. If it is valid, return
        * the lowercase version of it, suitable for later caching.
        *
        *--------------------------------------------------------*/ 
        private static string ValidateCulturePieceToLower(string testString, string paramName, int maxLength)
        { 
            if(testString.Length > maxLength) 
            {
                throw new ArgumentException( 
                    String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_NameTooLong"), testString, maxLength), paramName);
            }

            StringBuilder sb = new StringBuilder(testString.Length); 

            for(int ich = 0; ich < testString.Length; ich++) { 
                char ch = testString[ich]; 

                if(ch <= 'Z' && ch >= 'A') { 
                    sb.Append((char)(ch - 'A' + 'a'));
                }
                else if(((ch <= 'z' && ch >= 'a') ||
                    (ch <= '9' && ch >= '0') || 
                    (ch == '_') ||
                    (ch == '-'))) { 
                    sb.Append(ch); 
                }
                else { 
                    throw new ArgumentException(
                        String.Format(CultureInfo.CurrentCulture, Environment.GetResourceString("Argument_NameContainsInvalidCharacters"), testString), paramName);
                }
            } 

            return(sb.ToString()); 
 
        }
 
        internal static string AnsiToLower(string testString)
        {
            StringBuilder sb = new StringBuilder(testString.Length);
 
            for (int ich = 0; ich < testString.Length; ich++)
            { 
                char ch = testString[ich]; 
                sb.Append(ch <= 'Z' && ch >= 'A' ? (char)(ch - 'A' + 'a') : ch);
            } 

            return(sb.ToString());
        }
 
        // <SyntheticSupport/>
        internal bool IsSynthetic { get { return m_synthetic; } } 
 
        internal bool IsCustomCulture
        { 
            get
            {
                // If we came from the assembly we aren't custom
                return !(this.m_CultureTable.fromAssembly); 
            }
        } 
 
        internal bool IsReplacementCulture
        { 
            get
            {
                return ( this.IsCustomCulture && !IsCustomCultureId(m_CultureID) );
            } 
        }
 
        internal int  CultureID 
        {
            get 
            {
                BCLDebug.Assert(this.m_CultureID > 0, "[CultureTableRecord.CultureID]unexpected m_CultureId");
                return this.m_CultureID;
            } 
        }
 
        internal String  CultureName 
        {
            get 
            {
                BCLDebug.Assert(this.m_CultureName != null, "[CultureTableRecord.CultureName]unexpected m_CultureName");
                return this.m_CultureName;
            } 

            set 
            { 
                BCLDebug.Assert(value != null, "[CultureTableRecord.CultureName]Expected non-null value for culture name");
                this.m_CultureName = value; 
            }
        }

        internal bool UseUserOverride 
        {
            get 
            { 
                return this.m_bUseUserOverride;
            } 
        }

        // A property to indicate if we should retrieve information by calling the Win32 GetLocaleInfo().
 
        internal unsafe bool UseGetLocaleInfo
        { 
            get 
            {
                if (!this.m_bUseUserOverride) 
                {
                    return (false);
                }
                int lcid; 
                CultureInfo.nativeGetUserDefaultLCID(&lcid, CultureInfo.LOCALE_USER_DEFAULT);
 
                if (ActualCultureID == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED && 
                    lcid == CultureInfo.LOCALE_CUSTOM_DEFAULT)
                { 
                    if (SNAME.Equals(CultureInfo.nativeGetCultureName(lcid, true, false)))
                    {
                        return true;
                    } 
                    return false;
                } 
 
                return (this.ActualCultureID == lcid);
            } 
        }

        // A method to check if we can use the Win32 GetLocaleInfo() for the specified locale and the specified calenar in this CultureTableRecord.
        // It will be true when all of the following are true: 
        //  * UseUserOverride is true
        //  * UseGetLocaleInfo is true (which means the specified locale is the current user default locale. 
        //  * The specified calendar is the current calendar in the user default locale. 
        //  Parameters:
        //      calID: The calendar ID to be checked. 
        internal unsafe bool UseCurrentCalendar(int calID)
        {
            return (UseGetLocaleInfo && CultureInfo.nativeGetCurrentCalendar() == calID);
        } 

 
        internal bool IsValidSortID(int sortID) 
        {
            BCLDebug.Assert(sortID >= 0 && sortID <= 0xffff, "sortID is invalid");    // SortID is 16-bit positive integer. 

            if(sortID == 0 ||
               (this.SALTSORTID != null &&
                this.SALTSORTID.Length >= sortID && 
                this.SALTSORTID[sortID - 1].Length != 0))
            { 
                return true; 
            }
            else 
            {
                return false;
            }
        } 

        internal CultureTableRecord CloneWithUserOverride(bool userOverride) 
        { 
            if (m_bUseUserOverride == userOverride)
                return this; 

            CultureTableRecord cultureTableRecord = (CultureTableRecord) this.MemberwiseClone();
            cultureTableRecord.m_bUseUserOverride = userOverride;
 
            return cultureTableRecord;
        } 
 
        //
        // CultureNativeDisplayName called when we need to get the native display name for the culture 
        // from Win32 side. we need to do that in cases like synthetic cultures.
        //

        internal unsafe string CultureNativeDisplayName 
        {
            get 
            { 
                int     lcid;
                CultureInfo.nativeGetUserDefaultUILanguage(&lcid); 

                if (CultureInfo.GetLangID(lcid) == CultureInfo.GetLangID(CultureInfo.CurrentUICulture.LCID))
                {
                    string localizedLanguageName = CultureInfo.nativeGetLocaleInfo(m_ActualCultureID, LOCALE_SLANGUAGE); 
                    if (localizedLanguageName != null)
                    { 
                        // check for null terminated character. 
                        if ( localizedLanguageName[localizedLanguageName.Length - 1] == '\u0000' )
                            return localizedLanguageName.Substring(0, localizedLanguageName.Length - 1); 
                        else
                            return localizedLanguageName;
                    }
                } 

                return this.SNATIVEDISPLAYNAME; 
            } 
        }
 
        //
        // RegionNativeDisplayName called when we need to get the native display name for the region
        // from Win32 side. we need to do that in cases like synthetic cultures.
        // 

        internal unsafe string RegionNativeDisplayName 
        { 
            get
            { 
                int     lcid;
                CultureInfo.nativeGetUserDefaultUILanguage(&lcid);

                if (CultureInfo.GetLangID(lcid) == CultureInfo.GetLangID(CultureInfo.CurrentUICulture.LCID)) 
                { /* SSS_WARNINGS_OFF */
                    string localizedCountryName = CultureInfo.nativeGetLocaleInfo(m_ActualCultureID, LOCALE_SCOUNTRY); 
                    if (localizedCountryName  != null) 
                    {
                        if ( localizedCountryName[localizedCountryName.Length - 1] == '\u0000' ) 
                            return localizedCountryName.Substring(0, localizedCountryName.Length - 1);
                        else
                            return localizedCountryName ;
                    } 
                }
 
                return this.SNATIVECOUNTRY; 
            } /* SSS_WARNINGS_ON */
        } 

        ////////////////////////////////////////////////////////////////////////
        //
        //  Equals 
        //
        //  Implements Object.Equals().  Returns a boolean indicating whether 
        //  or not object refers to the same CultureTableRecord as the current instance. 
        //
        //////////////////////////////////////////////////////////////////////// 
        public override unsafe bool Equals(Object value)
        {
            CultureTableRecord that = value as CultureTableRecord;
            return (that != null) && 
                    (this.m_pData == that.m_pData &&
                     this.m_bUseUserOverride == that.m_bUseUserOverride && 
                     this.m_CultureID == that.m_CultureID && 
                     CultureInfo.InvariantCulture.CompareInfo.Compare(
                        this.m_CultureName, that.m_CultureName, CompareOptions.IgnoreCase ) == 0 && 
                     this.m_CultureTable.Equals(that.m_CultureTable)
                    );
        }
 
        ////////////////////////////////////////////////////////////////////////
        // 
        //  GetHashCode 
        //
        //  Implements Object.GetHashCode().  Returns the hash code for the 
        //  CultureInfo.  The hash code is guaranteed to be the same for RegionInfo
        //  A and B where A.Equals(B) is true.
        //
        //////////////////////////////////////////////////////////////////////// 
        public override int GetHashCode()
        { 
            //This doesn't tell apart user override from non-user override 
            if (!IsCustomCultureId(m_CultureID))
                return (this.m_CultureID); 

            return (this.m_CultureName.GetHashCode());
        }
 
        // Get a String
        private unsafe String GetString(uint iOffset) 
        { 
            char* pCharValues = unchecked((char*)(this.m_pPool + iOffset));
            // For null strings, iOffset, pPool[0] and pPool[1] are all three 0. 
            // The previous implimentation used [1] to test and I was afraid to change it.
            if (pCharValues[1] == 0)
            {
                BCLDebug.Assert(iOffset == 0, 
                    "[CultureTableRecord.GetString]Expected empty strings to have 0 offset");
                return String.Empty; 
            } 
            return new String(pCharValues + 1, 0, (int)pCharValues[0]);
        } 

        private int InteropLCID
        {
            get 
            {
                return ActualCultureID == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED ? 
                    CultureInfo.LOCALE_CUSTOM_DEFAULT : ActualCultureID; 
            }
        } 

        private String GetOverrideString(uint iOffset, int iWindowsFlag)
        {
#if !FEATURE_PAL 
            if (this.UseGetLocaleInfo)
            { 
                // Get User Override value if necessary 
                String value = CultureInfo.nativeGetLocaleInfo(InteropLCID, iWindowsFlag);
                if (value != null && value.Length > 0) 
                    return value;
            }
#endif
            return GetString(iOffset); 
        }
 
        private unsafe String[] GetStringArray(uint iOffset) 
        {
            if (iOffset == 0) return new String[0]; 

            // The offset value is in char, and is related to the begining of string pool.
            ushort* pCount = m_pPool + iOffset;
            int count = (int)pCount[0];    // The number of strings in the array 
            BCLDebug.Assert(count != 0,
                "[CultureTableRecord.GetStringArray]Expected non-zero length array"); 
            String[] values = new String[count]; 

            // Get past count and cast to uint 
            uint* pStringArray = (uint*)(pCount + 1);

            // Get our strings
            for (int i = 0; i < count; i++) 
                values[i] = GetString(pStringArray[i]);
 
            return (values); 
        }
 
        // Get first string in this array of strings
        private unsafe String GetStringArrayDefault(uint iOffset)
        {
            if (iOffset == 0) 
                return String.Empty;
 
            // The offset value is in char, and is related to the begining of string pool. 
            ushort* pCount = m_pPool + iOffset;
            BCLDebug.Assert(pCount[0] != 0, 
                "[CultureTableRecord.GetStringArrayDefault]Expected non-zero length array");

            // Get past count and cast to uint
            uint* pStringArray = (uint*)(pCount + 1); 

            // We had strings, return the first one 
            return GetString(pStringArray[0]); 
        }
 
        // Get the user override or the first array of this string array
        private String GetOverrideStringArrayDefault(uint iOffset, int iWindowsFlag)
        {
#if !FEATURE_PAL 
            if (this.UseGetLocaleInfo)
            { 
                // Get User Override value if necessary 
                String value = CultureInfo.nativeGetLocaleInfo(InteropLCID, iWindowsFlag);
                if (value != null && value.Length > 0) 
                    return value;
            }
#endif
            // If override wasn't available, return the table version 
            return GetStringArrayDefault(iOffset);
        } 
 
        private ushort GetOverrideUSHORT(ushort iData, int iWindowsFlag)
        { 
#if !FEATURE_PAL
            if (this.UseGetLocaleInfo)
            {
                // Get User Override value if necessary 
                String value = CultureInfo.nativeGetLocaleInfo(InteropLCID, iWindowsFlag);
                if (value != null && value.Length > 0) 
                { 
                    // Now we need an integer for this string.
                    Int16 intValue; 
                    if (Int16.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out intValue))
                        return unchecked((ushort)intValue);
                }
            } 
#endif
            return iData; 
        } 

        private unsafe int[] GetWordArray(uint iData) 
        {
            if (iData == 0)
                return new int[0];
 
            ushort* pWord = this.m_pPool + iData;
            int count = (int)pWord[0];          // The number of words in the array 
            BCLDebug.Assert(count != 0, 
                "[CultureTableRecord.GetWordArray]Expected non-zero length array");
 
            int[] values = new int[count];
            pWord++;                            // Get past count
            for (int i = 0; i < count; i++) {
                values[i] = pWord[i]; 
            }
            return (values); 
        } 

        private int[] GetOverrideGrouping(uint iData, int iWindowsFlag) 
        {
#if !FEATURE_PAL
            if (this.UseGetLocaleInfo)
            { 
                // Get User Override value if necessary
                String value = CultureInfo.nativeGetLocaleInfo(InteropLCID, iWindowsFlag); 
                if (value != null && value.Length > 0) 
                {
                    // Got a grouping, need to convert it. 
                    int[] values = ConvertWin32GroupString(value);
                    if (values != null)
                    {
                        // If non-null value are found, retured it. 
                        // Otherwise, fallback to default values.
                        return (values); 
                    } 
                }
            } 
#endif
            // No Override, use it from the tables.
            return GetWordArray(iData);
        } 

        // The actual LCID, used when a name lookup leads to a custom sort (thus 
        // 'de-DE-deudi' will be 0x10407 rather than the plain old 0x0407 of 'de-DE'). 
        internal int ActualCultureID {
            get { 
                if(0 == this.m_ActualCultureID) {
                    this.m_ActualCultureID = this.ILANGUAGE;
                }
 
                return(this.m_ActualCultureID);
            } 
        } 

        // The actual name, used when an LCID lookup leads to a custom sort (thus will be 
        // 0x10407 will be 'de-DE-deudi' rather than the plain old 'de-DE' of 0x0407).
        internal string ActualName {
            get {
                if(null == this.m_ActualName) { 
                    this.m_ActualName = this.SNAME;
                } 
 
                return(this.m_ActualName);
            } 
        }

        internal bool IsNeutralCulture {
            get { 
                return ((IFLAGS & (ushort)CultureFlags.IsSpecificCulture) == 0);
            } 
        } 

        private bool IsOptionalCalendar(int calendarId) 
        {
            for (int i=0; i<IOPTIONALCALENDARS.Length; i++)
            {
                if (IOPTIONALCALENDARS[i] == calendarId) 
                    return true;
            } 
 
            return false;
        } 

        ////////////////////////////////////////////////////////////////////////
        //
        //  All the accessors 
        //
        //  Accessors for our data object items 
        // 
        ////////////////////////////////////////////////////////////////////////
 
        // These ones allow user override
        // Integers
        internal unsafe ushort IDIGITS                  { get { return (this.m_pData->iDigits);} }                   // (user can override) number of fractional digits
        internal unsafe ushort INEGNUMBER               { get { return (this.m_pData->iNegativeNumber); } }          // (user can override) negative number format 
        internal unsafe ushort ICURRDIGITS              { get { return (this.m_pData->iCurrencyDigits); } }          // (user can override) # local monetary fractional digits
        internal unsafe ushort ICURRENCY                { get { return (this.m_pData->iCurrency); } }                // (user can override) positive currency format 
        internal unsafe ushort INEGCURR                 { get { return (this.m_pData->iNegativeCurrency); } }        // (user can override) negative currency format 
//        internal unsafe ushort ILEADINGZEROS            { get { return GetOverrideUSHORT(this.m_pData->iLEADINGZEROS,      CultureTableData.LOCALE_ILEADINGZEROS); } }            // (user can override) leading zeros 0  leading zeros, 1 ading zeros
        internal unsafe ushort ICALENDARTYPE 
        {
            get
            {
#if !FEATURE_PAL 
                // Expand the code of GetOverrideUSHORT() since we don't want to create an int array (IOPTIONALCALENDARS) from every time that we call GetOverrideUSHORT().
                if (this.m_bUseUserOverride) 
                { 
                    // Get User Override value if necessary
                    String value = CultureInfo.nativeGetLocaleInfo(ActualCultureID, CultureTableRecord.LOCALE_ICALENDARTYPE); 
                    if (value != null && value.Length > 0)
                    {
                        // Now we need an integer for this string.
                        Int16 intValue; 
                        if (Int16.TryParse(value, NumberStyles.None, CultureInfo.InvariantCulture, out intValue) &&
                            IsOptionalCalendar((int)intValue)) 
                            return unchecked((ushort)intValue); 
                    }
                } 
#endif // !FEATURE_PAL
                return ((ushort)IOPTIONALCALENDARS[0]);
            }
        } 
        internal unsafe ushort IFIRSTWEEKOFYEAR         { get { return GetOverrideUSHORT(this.m_pData->iFirstWeekOfYear,     CultureTableData.LOCALE_IFIRSTWEEKOFYEAR); } }         // (user can override) first week of year
/* SSS_WARNINGS_OFF *///        internal unsafe ushort ICOUNTRY                 { get { return GetOverrideUSHORT(this.m_pData->iCOUNTRY,           CultureTableData.LOCALE_ICOUNTRY); } }                 // (user can override) country code (RegionInfo) /* SSS_WARNINGS_ON */ 
        internal unsafe ushort IMEASURE                 { get { return GetOverrideUSHORT(this.m_pData->iMeasure,             CultureTableData.LOCALE_IMEASURE); } }                 // (user can override) system of measurement 0ric, 1(RegionInfo) 
        internal unsafe ushort IDIGITSUBSTITUTION       { get { return GetOverrideUSHORT(this.m_pData->iDigitSubstitution,   CultureTableData.LOCALE_IDIGITSUBSTITUTION); } }       // (user can override) Digit substitution 0text, 1e/arabic, 2ive/national (2 seems to be unused)
 
        // Grouping
        internal unsafe int[]  SGROUPING                { get { return GetOverrideGrouping(this.m_pData->waGrouping,         CultureTableData.LOCALE_SGROUPING); } }                   // (user can override) grouping of digits
        internal unsafe int[]  SMONGROUPING             { get { return GetOverrideGrouping(this.m_pData->waMonetaryGrouping, CultureTableData.LOCALE_SMONGROUPING); } }           // (user can override) monetary grouping of digits
 
        // Strings
        internal unsafe String SLIST                    { get { return GetOverrideString(this.m_pData->sListSeparator,       CultureTableData.LOCALE_SLIST); } }                    // (user can override) list Separator 
        internal unsafe String SDECIMAL                 { get { return GetString(this.m_pData->sDecimalSeparator); } }                 // (user can override) decimal Separator 
        internal unsafe String STHOUSAND                { get { return GetString(this.m_pData->sThousandSeparator); } }                // (user can override) thousands Separator
        internal unsafe String SCURRENCY                { get { return GetString(this.m_pData->sCurrency); } }                  // (user can override) local monetary symbol 
        internal unsafe String SMONDECIMALSEP           { get { return GetString(this.m_pData->sMonetaryDecimal); } }           // (user can override) monetary decimal Separator
        internal unsafe String SMONTHOUSANDSEP          { get { return GetString(this.m_pData->sMonetaryThousand); } }          // (user can override) monetary thousands separator
        internal unsafe String SNEGATIVESIGN            { get { return GetString(this.m_pData->sNegativeSign); } }              // (user can override) negative sign
        internal unsafe String S1159                    { get { return GetString(this.m_pData->sAM1159); } }                    // (user can override) AM designator 
        internal unsafe String S2359                    { get { return GetString(this.m_pData->sPM2359); } }                    // (user can override) PM designator
 
        // String array DEFAULTS 
        // Note: GetDTFIOverrideValues does the user overrides for these, so we don't have to.
        internal unsafe String STIMEFORMAT              { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saTimeFormat)); } }       // (user can override) time format 
        internal unsafe String SSHORTTIME               { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saShortTime)); } }        // short time format
        internal unsafe String SSHORTDATE               { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saShortDate)); } }        // (user can override) short date format
        internal unsafe String SLONGDATE                { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saLongDate)); } }         // (user can override) long date format
        internal unsafe String SYEARMONTH               { get { return ReescapeWin32String(GetStringArrayDefault(this.m_pData->saYearMonth)); } }        // (user can override) year/month format 
        internal unsafe String SMONTHDAY                { get { return ReescapeWin32String(GetString(this.m_pData->sMonthDay)); } }                      // month/day format (single string, no override)
 
        // String arrays 
        internal unsafe String[] STIMEFORMATS           { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saTimeFormat)); } }              // (user can override) time format
        internal unsafe String[] SSHORTTIMES            { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saShortTime)); } }               // short time format 
        internal unsafe String[] SSHORTDATES            { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saShortDate)); } }               // (user can override default only) short date format
        internal unsafe String[] SLONGDATES             { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saLongDate)); } }                // (user can override default only) long date format
        internal unsafe String[] SYEARMONTHS            { get { return ReescapeWin32Strings(GetStringArray(this.m_pData->saYearMonth)); } }               // (user can override) date year/month format.  (9x doesn't support override)
 
        internal unsafe String[] SNATIVEDIGITS          {                                                          // (user can override) native characters for digits 0-9
            get { 
 
#if !FEATURE_PAL
                string[] values; 

                if (this.m_bUseUserOverride)
                {
                    String value; 

                    // Get User Override value if it exists and convert it to a string array 
                    // If the user default is a custom culture for OS then skip this step. 
                    if((this.CultureID != CultureInfo.LOCALE_CUSTOM_DEFAULT) &&
                        null != (value = CultureInfo.nativeGetLocaleInfo(this.ActualCultureID, CultureTableData.LOCALE_SNATIVEDIGITS)) && 
                        (value.Length == 10))
                    {
                        values = new string[10];
                        for(int i = 0; i < value.Length; i++) 
                        {
                            values[i] = value[i].ToString(CultureInfo.InvariantCulture); 
                        } 

                        return(values); 
                    }
                }
#endif // !FEATURE_PAL
 
                return GetStringArray(this.m_pData->saNativeDigits);
            } 
        } 

        // Integer ones are all pretty trivial 
        internal unsafe ushort ILANGUAGE                { get { return this.m_pData->iLanguage; } }                //
//        internal unsafe ushort IDEFAULTLANGUAGE         { get { return this.m_pData->iDEFAULTLANGUAGE; } }         // Default language if this is a rare lcid (Windows Only)
        internal unsafe ushort IDEFAULTANSICODEPAGE     { get { return this.m_pData->iDefaultAnsiCodePage; } }     // default ansi code page ID (ACP)
        internal unsafe ushort IDEFAULTOEMCODEPAGE      { get { return this.m_pData->iDefaultOemCodePage; } }      // default oem code page ID (OCP or OEM) 
        internal unsafe ushort IDEFAULTMACCODEPAGE      { get { return this.m_pData->iDefaultMacCodePage; } }      // default macintosh code page
        internal unsafe ushort IDEFAULTEBCDICCODEPAGE   { get { return this.m_pData->iDefaultEbcdicCodePage; } }   // default EBCDIC code page 
        internal unsafe ushort IGEOID                   { get { return this.m_pData->iGeoId; } }                   // GeoId (RegionInfo) 
//        internal unsafe ushort IPAPERSIZE               { get { return this.m_pData->iPAPERSIZE; } }               // default paper size (RegionInfo)
//        internal unsafe ushort IINTLCURRENCYDIGITS      { get { return this.m_pData->iINTLCURRENCYDIGITS; } }      // # of digits after decimal in intl currency format (Windows Only) 
        internal unsafe ushort INEGATIVEPERCENT         { get { return this.m_pData->iNegativePercent; } }         //
        internal unsafe ushort IPOSITIVEPERCENT         { get { return this.m_pData->iPositivePercent; } }         //
        internal unsafe ushort IPARENT                  { get { return this.m_pData->iParent; } }                  //
        internal unsafe ushort ILINEORIENTATIONS        { get { return this.m_pData->iLineOrientations; } }        // 
        internal unsafe uint   ICOMPAREINFO             { get { return this.m_pData->iCompareInfo; } }             //
        internal unsafe uint   IFLAGS                   { get { return this.m_pData->iFlags; } }                   // Flags for culture 
 
        // OptionalCalendars
        internal unsafe int[]  IOPTIONALCALENDARS       { get { return GetWordArray(this.m_pData->waCalendars); } }    // additional calendar type(s), semicolon seperated, ie: '1;6' 

        // Strings
        internal unsafe String SNAME                    { get { return GetString(this.m_pData->sName); } }                    //
        internal unsafe String SABBREVLANGNAME          { get { return GetString(this.m_pData->sAbbrevLang); } }              // abbreviated language name 
        internal unsafe String SISO639LANGNAME          { get { return GetString(this.m_pData->sISO639Language); } }          //
//        internal unsafe String SENGLISHLANGUAGE         { get { return GetString(this.m_pData->sENGLISHLANGUAGE); } }       // English name for this language (Windows Only) 
//        internal unsafe String SNATIVELANGUAGE          { get { return GetString(this.m_pData->sNATIVELANGUAGE); } }        // Native name of this language (Windows Only) 
/* SSS_WARNINGS_OFF */        internal unsafe String SENGCOUNTRY              { get { return GetString(this.m_pData->sEnglishCountry); } }          // english country name (RegionInfo)
        internal unsafe String SNATIVECOUNTRY           { get { return GetString(this.m_pData->sNativeCountry); } }           // native country name (RegionInfo) 
        internal unsafe String SABBREVCTRYNAME          { get { return GetString(this.m_pData->sAbbrevCountry); } }           // abbreviated country name (RegionInfo)
        internal unsafe String SISO3166CTRYNAME         { get { return GetString(this.m_pData->sISO3166CountryName); } }      // (RegionInfo)
/* SSS_WARNINGS_ON */        internal unsafe String SINTLSYMBOL              { get { return GetString(this.m_pData->sIntlMonetarySymbol); } }      // international monetary symbol (RegionInfo)
        internal unsafe String SENGLISHCURRENCY         { get { return GetString(this.m_pData->sEnglishCurrency); } }         // English name for this currency (RegionInfo) 
        internal unsafe String SNATIVECURRENCY          { get { return GetString(this.m_pData->sNativeCurrency); } }          // Native name for this currency (RegionInfo)
        internal unsafe String SENGDISPLAYNAME          { get { return GetString(this.m_pData->sEnglishDisplayName); } }      // 
        internal unsafe String SISO639LANGNAME2         { get { return GetString(this.m_pData->sISO639Language2); } }          // 
        internal unsafe String SNATIVEDISPLAYNAME
        { 
            get
            {
/* SSS_WARNINGS_OFF */                // Special case for Taiwan. /* SSS_WARNINGS_ON */
                if (CultureInfo.GetLangID(ActualCultureID) == 0x0404 && 
                    CultureInfo.GetLangID(CultureInfo.InstalledUICulture.LCID) == 0x0404 &&
                    !IsCustomCulture) 
                { 
                    return (CultureInfo.nativeGetLocaleInfo(0x0404, LOCALE_SNATIVELANGNAME) + " (" + CultureInfo.nativeGetLocaleInfo(0x0404, LOCALE_SNATIVECTRYNAME) + ")");
                } 
                return GetString(this.m_pData->sNativeDisplayName);
            }
        }       //
 
        internal unsafe String SPERCENT                 { get { return GetString(this.m_pData->sPercent); } }                 //
        internal unsafe String SNAN                     { get { return GetString(this.m_pData->sNaN); } }                     // 
        internal unsafe String SPOSINFINITY             { get { return GetString(this.m_pData->sPositiveInfinity); } }        // 
        internal unsafe String SNEGINFINITY             { get { return GetString(this.m_pData->sNegativeInfinity); } }        //
        internal unsafe String SADERA                   { get { return GetString(this.m_pData->sAdEra); } }                   // localized names for the A.D. Era 
        internal unsafe String SABBREVADERA             { get { return GetString(this.m_pData->sAbbrevAdEra); } }             // abbreviated localized names for the A.D. Era
/* SSS_WARNINGS_OFF */        internal unsafe String SISO3166CTRYNAME2        { get { return GetString(this.m_pData->sISO3166CountryName2); } }     // (RegionInfo) /* SSS_WARNINGS_ON */
        internal unsafe String SREGIONNAME              { get { return GetString(this.m_pData->sRegionName); } }              // (RegionInfo)
        internal unsafe String SPARENT                  { get { return GetString(this.m_pData->sParent); } }                  // 
        internal unsafe String SCONSOLEFALLBACKNAME     { get { return GetString(this.m_pData->sConsoleFallbackName); } }
        internal unsafe String SSPECIFICCULTURE         { get { return GetString(this.m_pData->sSpecificCulture); } } 
 
        // String Arrays
        internal unsafe String[] SDAYNAMES                { get { return GetStringArray(this.m_pData->saDayNames); } }                // day names 
        internal unsafe String[] SABBREVDAYNAMES          { get { return GetStringArray(this.m_pData->saAbbrevDayNames); } }          // abbreviated day names
        internal unsafe String[] SSUPERSHORTDAYNAMES      { get { return GetStringArray(this.m_pData->saSuperShortDayNames); } }      // one letter day names
        internal unsafe String[] SMONTHNAMES              { get { return GetStringArray(this.m_pData->saMonthNames); } }              // month names
        internal unsafe String[] SABBREVMONTHNAMES        { get { return GetStringArray(this.m_pData->saAbbrevMonthNames); } }        // abbreviated month names 
        internal unsafe String[] SMONTHGENITIVENAMES      { get { return GetStringArray(this.m_pData->saMonthGenitiveNames); } }      //
        internal unsafe String[] SABBREVMONTHGENITIVENAMES{ get { return GetStringArray(this.m_pData->saAbbrevMonthGenitiveNames); } }// 
        internal unsafe String[] SNATIVECALNAMES          { get { return GetStringArray(this.m_pData->saNativeCalendarNames); } }     // Native calendar names.  index of optional calendar - 1, empty if no optional calendar at that number 
        internal unsafe String[] SDATEWORDS               { get { return GetStringArray(this.m_pData->saDateWords); } }               //
        internal unsafe String[] SALTSORTID               { get { return GetStringArray(this.m_pData->saAltSortID); } }               // The array of alternate sort names 

        // Fontsignature
//        internal unsafe ushort FONTSIGNATURE            { get { return this.m_pData->waFONTSIGNATURE; } }            // Font signature (16 WORDS) (Windows Only)
 
        // DateTimeFormatFlags
        internal unsafe DateTimeFormatFlags IFORMATFLAGS{ get { return (DateTimeFormatFlags)this.m_pData->iFormatFlags; } }   // 
 
        // Special handling required for these fields
        // (user can override) positive sign.  We use "+" if empty (windows data is usually empty) 
        internal unsafe String SPOSITIVESIGN
        {
            get
            { 
                String strTemp = GetString(this.m_pData->sPositiveSign);
                if (strTemp == null || strTemp.Length == 0) strTemp = "+"; 
                return strTemp; 
            }
        } 

        internal static bool IsCustomCultureId(int cultureId)
        {
            if (cultureId == CultureInfo.LOCALE_CUSTOM_DEFAULT || cultureId == CultureInfo.LOCALE_CUSTOM_UNSPECIFIED) 
                return true;
 
            return false; 
        }
 
        private unsafe ushort ConvertFirstDayOfWeekMonToSun(int iTemp)
        {
            // Convert Mon-Sun to Sun-Sat format
            if (iTemp < 0 || iTemp > 6) { 
                // If invalid data exist in registry, assume
                // the first day of week is Monday. 
                iTemp = 1; 
            } else {
                if (iTemp == 6) { 
                    iTemp = 0;
                } else {
                    iTemp++;
                } 
            }
            return unchecked((ushort)iTemp); 
        } 

        // (user can override) first day of week (0 is Sunday) 
        internal unsafe ushort IFIRSTDAYOFWEEK
        {
            get
            { 
                return this.m_pData->iFirstDayOfWeek;
            } 
        } 

        internal unsafe ushort IINPUTLANGUAGEHANDLE 
        {
            get
            {
                // Remember this returns SPANISH_INTERNATIONAL_SORT even 
                // in the deprecated case.
                return (this.m_pData->iInputLanguageHandle); 
            } 
        }
 
        internal unsafe ushort ITEXTINFO
        {
            get
            { 
                ushort textInfo = this.m_pData->iTextInfo;
 
                // Need to return SPANISH_TRADITIONAL_SORT even if we're faking it 
                // (Hack because SPANISH_TRADITIONAL_SORT isn't in the table)
                if (this.CultureID == (ushort)SPANISH_TRADITIONAL_SORT) 
                    textInfo = (ushort)SPANISH_TRADITIONAL_SORT;

                // Make sure custom culture and unknown get something
                if (textInfo == CultureInfo.LOCALE_CUSTOM_DEFAULT || textInfo == 0) 
                    textInfo = CultureInfo.LOCALE_INVARIANT;
                return textInfo; 
             } 
         }
 
#if !FEATURE_PAL
        // If we get a group from the registry, then its in 3;0 format with the 0 backwards
        // of how NLS uses it (ie: if the string has a 0, then the int[] shouldn't and vice versa)
        static private int[] ConvertWin32GroupString(String win32Str) 
        {
            // None of these cases make any sense 
            if (win32Str == null || win32Str.Length == 0 || win32Str[0] == '0') { 
                return (new int[] {3});
            } 

            // Since its in n;n;n;n;n format, we can always get the length quickly
            int[] values;
            if (win32Str[win32Str.Length - 1] == '0') 
            {
                // Trailing 0 gets dropped. 1;0 -> 1 
                values = new int[(win32Str.Length / 2)]; 
            }
            else 
            {
                // Need extra space for trailing zero 1 -> 1;0
                values = new int[(win32Str.Length / 2) + 2];
                values[values.Length - 1] = 0; 
            }
 
            int i; 
            int j;
            for (i = 0, j = 0; i < win32Str.Length && j < values.Length; i += 2, j++) 
            {
                // Note that this # shouldn't ever be zero, 'cause 0 is only at end
                // But we'll test because its registry that could be anything
                if (win32Str[i] < '1' || win32Str[i] > '9') 
                    return new int[] {3};
 
                values[j] = (int)(win32Str[i] - '0'); 
            }
 
            return (values);
        }
#endif
 
        ////////////////////////////////////////////////////////////////////////////
        // 
        // Unescape a Win32 style quote string 
        //
        // This is also the escaping style used by custom culture data files 
        //
        // This removes the 'fred' and 'fred''s' windows quoted formatting from a string.
        // The output string will NOT have ANY escaping.  Currently its used for
        // separators, where the output string has no characters with special meaning 
        //
        // We don't build the stringbuilder unless we find a '.  If we find a ', we 
        // always build a stringbuilder because we need to remove the '. 
        //
        //////////////////////////////////////////////////////////////////////////// 
        static private String UnescapeWin32String(String str, int start, int end)
        {
            StringBuilder result = null;
 
            bool inQuote = false;
            for (int i = start; i < str.Length && i <= end; i++) 
            { 
                // Look for quote
                if (str[i] == '\'') 
                {
                    // Already in quote?
                    if (inQuote)
                    { 
                        BCLDebug.Assert(result != null, "[CultureTable.UnescapeWin32String]Expect result to be non-null");
                        // See another single quote.  Is this '' of 'fred''s' or ending quote? 
                        if (i + 1 < str.Length) 
                        {
                            if (str[i+1] == '\'') 
                            {
                                // Append a ' and keep going (so we don't turn off quote mode)
                                result.Append('\'');
                                i++; 
                                continue;
                            } 
                        } 

                        inQuote = false; 
                    }
                    else
                    {
                        // Found beginning quote, remove it. 
                        inQuote = true;
                        if (result == null) 
                            result = new StringBuilder(str, start, i - start, str.Length); 
                    }
                } 
                else
                {
                    // If we have a builder we need to add our non-quote char
                    if (result != null) 
                        result.Append(str[i]);
                } 
            } 

            // No ', just return input string substring 
            if (result == null)
                return (str.Substring(start, end-start + 1));

            // Had ', need to use the builder 
            return (result.ToString());
        } 
 

        //////////////////////////////////////////////////////////////////////////// 
        //
        // Reescape a Win32 style quote string as a NLS+ style quoted string
        //
        // This is also the escaping style used by custom culture data files 
        //
        // NLS+ uses \ to escape the next character, whether in a quoted string or 
        // not, so we always have to change \ to \\. 
        //
        // NLS+ uses \' to escape a quote inside a quoted string so we have to change 
        // '' to \' (if inside a quoted string)
        //
        // We don't build the stringbuilder unless we find something to change
        //////////////////////////////////////////////////////////////////////////// 
        static private String ReescapeWin32String(String str)
        { 
            // If we don't have data, then don't try anything 
            if (str == null)
                return null; 

            StringBuilder result = null;

            bool inQuote = false; 
            for (int i = 0; i < str.Length; i++)
            { 
                // Look for quote 
                if (str[i] == '\'')
                { 
                    // Already in quote?
                    if (inQuote)
                    {
                        // See another single quote.  Is this '' of 'fred''s' or '''', or is it an ending quote? 
                        if (i + 1 < str.Length && str[i+1] == '\'')
                        { 
                            // Found another ', so we have ''.  Need to add \' instead. 
                            // 1st make sure we have our stringbuilder
                            if (result == null) 
                                result = new StringBuilder(str, 0, i, str.Length * 2);

                            // Append a \' and keep going (so we don't turn off quote mode)
                            result.Append("\\'"); 
                            i++;
                            continue; 
                        } 

                        // Turning off quote mode, fall through to add it 
                        inQuote = false;
                    }
                    else
                    { 
                        // Found beginning quote, fall through to add it
                        inQuote = true; 
                    } 
                }
                // Is there a single \ character? 
                else if (str[i] == '\\')
                {
                    // Found a \, need to change it to \\
                    // 1st make sure we have our stringbuilder 
                    if (result == null)
                        result = new StringBuilder(str, 0, i, str.Length * 2); 
 
                    // Append our \\ to the string & continue
                    result.Append("\\\\"); 
                    continue;
                }

                // If we have a builder we need to add our character 
                if (result != null)
                    result.Append(str[i]); 
            } 

            // Unchanged string? , just return input string 
            if (result == null)
                return str;

            // String changed, need to use the builder 
            return result.ToString();
        } 
 
        static private String[] ReescapeWin32Strings(String[] array)
        { 
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                { 
                    array[i] = ReescapeWin32String(array[i]);
                } 
            } 

            return array; 
        }

        internal unsafe String STIME
        { 
            get
            { 
                // Compute SDATE from STIMEFORMAT 
                String timeFormat = GetOverrideStringArrayDefault(this.m_pData->saTimeFormat, CultureTableData.LOCALE_STIMEFORMAT);
                return GetTimeSeparator(timeFormat); 
            }
        }

        internal unsafe String SDATE 
        {
            get 
            { 
                // Compute SDATE from SSHORTDATE
                String shortDate = GetOverrideStringArrayDefault(this.m_pData->saShortDate, CultureTableData.LOCALE_SSHORTDATE); 
                return GetDateSeparator(shortDate);
            }
        }
 
        static private String GetTimeSeparator(String format)
        { 
            // Time format separator (ie: : in 12:39:00) 
            //
            // We calculate this from the provided time format 
            //

            //
            //  Find the time separator so that we can pretend we know STIME. 
            //
            String strUse = String.Empty; 
            int count = 0; 
            int separatorStart = -1;
 
            // Look through the whole string
            for (count = 0; count < format.Length; count++)
            {
                // See if we have Hhms 
                if (format[count] == 'H' || format[count] == 'h' || format[count] == 'm' || format[count] == 's')
                { 
                    // Found a time part, find out when it changes 
                    char cFound = format[count];
 
                    for (count++; count < format.Length && format[count] == cFound; count++)
                    {
                        // Done
                    } 

                    // Did we find anything? 
                    if (count < format.Length) 
                    {
                        // We found start of separator 
                        separatorStart = count;
                    }

                    // In either case we changed hms types, so we found one or we didn't, but we need to stop 
                    break;
                } 
 
                // If it was quotes, ignore quoted stuff
                if (format[count] == '\'') 
                {
                    //
                    //  Ignore quotes.
                    // 

                    for (count++; count < format.Length && (format[count] != '\''); count++) 
                    { 
                        // Done
                    } 

                    // Don't go past end of string
                }
 
                // Advance to next char (skipping unknown char or last quote)
            } 
 
            // Now we need to find the end of the separator
            if (separatorStart != -1) 
            {
                for (count = separatorStart; count < format.Length; count++)
                {
                    // See if we have Hhms 
                    if (format[count] == 'H' || format[count] == 'h' || format[count] == 'm' || format[count] == 's')                    {
                        // Found a time part, stop, we can look for our separator 
                        // From [separatorStart, count) is our string, except we don't want ''s 
                        strUse = UnescapeWin32String(format, separatorStart, count - 1);
                        break; 
                    }

                    // If it was quotes, ignore quoted stuff
                    if (format[count] == '\'') 
                    {
                        // 
                        //  Ignore quotes. 
                        //
                        for (count++; count < format.Length && (format[count] != '\''); count++) 
                        {
                            // Done
                        }
 
                        // Don't go past end of string
                    } 
 
                    // Advance to next char (skipping unknown char or last quote)
                } 
            }

            // Return the one we're using
            return strUse; 
        }
 
        static private String GetDateSeparator(String format) 
        {
            // Date format separator (ie: / in 9/1/03) 
            //
            // We calculate this from the provided short date
            //
 
            //
            //  Find the date separator so that we can pretend we know SDATE. 
            // 
            String strUse = String.Empty;
            int count = 0; 
            int separatorStart = -1;

            // Look through the whole string
            for (count = 0; count < format.Length; count++) 
            {
                // See if we have dyM 
                if (format[count] == 'd' || format[count] == 'y' || format[count] == 'M') 
                {
                    // Found a time part, find out when it changes 
                    char cFound = format[count];

                    for (count++; count < format.Length && format[count] == cFound; count++)
                    { 
                        // Done
                    } 
 
                    // Did we find anything?
                    if (count < format.Length) 
                    {
                        // We found start of separator
                        separatorStart = count;
                    } 

                    // In either case we changed dyM types, so we found one or we didn't, but we need to stop 
                    break; 
                }
 
                // If it was quotes, ignore quoted stuff
                if (format[count] == '\'')
                {
                    // 
                    //  Ignore quotes.
                    // 
 
                    for (count++; count < format.Length && (format[count] != '\''); count++)
                    { 
                        // Done
                    }

                    // Don't go past end of string 
                }
 
                // Advance to next char (skipping unknown char or last quote) 
            }
 
            // Now we need to find the end of the separator
            if (separatorStart != -1)
            {
                for (count = separatorStart; count < format.Length; count++) 
                {
                    // See if we have yMd 
                    if (format[count] == 'y' || format[count] == 'M' || format[count] == 'd') 
                    {
                        // Found a time part, stop, we can look for our separator 
                        // From [separatorStart, count) is our string, except we don't want ''s
                        strUse = UnescapeWin32String(format, separatorStart, count - 1);
                        break;
                    } 

                    // If it was quotes, ignore quoted stuff 
                    if (format[count] == '\'') 
                    {
                        // 
                        //  Ignore quotes.
                        //
                        for (count++; count < format.Length && (format[count] != '\''); count++)
                        { 
                            // Done
                        } 
 
                        // Don't go past end of string
                    } 

                    // Advance to next char (skipping unknown char or last quote)
                }
            } 

            // Return the one we're using 
            return strUse; 
        }
 
        ////////////////////////////////////////////////////////////////////////////
        //
        // Parameters:
        //      calendarValueOnly   Retrieve the values which are affected by the calendar change of DTFI. 
        //                          This will cause values like longTimePattern not be retrieved since it is
        //                          not affected by the Calendar property in DTFI. 
        // 
        ////////////////////////////////////////////////////////////////////////////
        internal unsafe void GetDTFIOverrideValues(ref DTFIUserOverrideValues values) { 
            BCLDebug.Assert(UseUserOverride, "CultureTableRecord.GetDTFIOverrideValues(): Call this only when UseUserOverride is true.");
            bool result = false;
            if (UseGetLocaleInfo)
                result = CultureInfo.nativeGetDTFIUserValues(InteropLCID, ref values); 

            if (result) { 
 
                // if we got values.yearMonthPattern = null this means the data is not located in the registry and
                // we couldn't call GetLocaleInfo. we leave yearMonthPattern as null here so the caller (DTFI) 
                // will initialize it properly.

                values.firstDayOfWeek = ConvertFirstDayOfWeekMonToSun((int)values.firstDayOfWeek);
 
                // Need to do escaping of win32/file type patterns to NLS type patterns
                values.shortDatePattern = ReescapeWin32String(values.shortDatePattern); 
                values.longDatePattern  = ReescapeWin32String(values.longDatePattern); 
                values.longTimePattern  = ReescapeWin32String(values.longTimePattern);
                values.yearMonthPattern = ReescapeWin32String(values.yearMonthPattern); 
            } else {
                //
                // We do not use user-override values or something failed during the call to GetLocaleInfo().  Use the information in culture.nlp.
                // 
                values.firstDayOfWeek = IFIRSTDAYOFWEEK;
                values.calendarWeekRule = IFIRSTWEEKOFYEAR; 
                values.shortDatePattern = SSHORTDATE; 
                values.longDatePattern = SLONGDATE;
                values.yearMonthPattern = SYEARMONTH; 
                values.amDesignator = S1159;
                values.pmDesignator = S2359;
                values.longTimePattern = STIMEFORMAT;
            } 
        }
 
        internal unsafe void GetNFIOverrideValues(NumberFormatInfo nfi) { 
            bool result = false;
            if (UseGetLocaleInfo) { 
                result = CultureInfo.nativeGetNFIUserValues(InteropLCID, nfi);
            }

            if (!result) { 
                // Something failed during the call to GetLocaleInfo().  Use the information in culture.nlp.
                nfi.numberDecimalDigits     = IDIGITS; 
                nfi.numberNegativePattern   = INEGNUMBER; 
                nfi.currencyDecimalDigits   = ICURRDIGITS;
                nfi.currencyPositivePattern = ICURRENCY; 
                nfi.currencyNegativePattern = INEGCURR;
                nfi.negativeSign            = SNEGATIVESIGN;
                nfi.numberDecimalSeparator  = SDECIMAL;
                nfi.numberGroupSeparator    = STHOUSAND; 
                nfi.positiveSign            = SPOSITIVESIGN;
                nfi.currencyDecimalSeparator= SMONDECIMALSEP; 
                nfi.currencySymbol          = SCURRENCY; 
                nfi.currencyGroupSeparator  = SMONTHOUSANDSEP;
                nfi.nativeDigits            = SNATIVEDIGITS; 
                nfi.digitSubstitution       = IDIGITSUBSTITUTION;
            }
            else if(-1 == nfi.digitSubstitution) {
                // This is a Win2000 and above property, so when it is marked as -1 
                // (an invalid value) we know it failed for Win9x reasons and that
                // we should fall back to getting this infotmation from culture.nlp. 
                nfi.digitSubstitution       = IDIGITSUBSTITUTION; 
            }
 
            nfi.numberGroupSizes        = SGROUPING;
            nfi.currencyGroupSizes      = SMONGROUPING;

            nfi.percentDecimalDigits    = nfi.numberDecimalDigits; 
            nfi.percentDecimalSeparator = nfi.numberDecimalSeparator;
            nfi.percentGroupSizes       = nfi.numberGroupSizes; 
            nfi.percentGroupSeparator   = nfi.numberGroupSeparator; 
            nfi.percentNegativePattern  = INEGATIVEPERCENT;
            nfi.percentPositivePattern  = IPOSITIVEPERCENT; 
            nfi.percentSymbol           = SPERCENT;

            if (nfi.positiveSign == null || nfi.positiveSign.Length == 0) nfi.positiveSign = "+";
 
            //Special case for Italian.  The currency decimal separator in the control panel is the empty string. When the user
            //specifies C4 as the currency format, this results in the number apparently getting multiplied by 10000 because the 
            //decimal point doesn't show up.  We'll just hack this here because our default currency format will never use nfi. 
            if (nfi.currencyDecimalSeparator.Length==0) {
                nfi.currencyDecimalSeparator= SMONDECIMALSEP; 
            }


        } 

 
 
        // EverettDataItem
        // 
        // Everett can't deserialize using names/ids, so it has to use the data item.
        internal unsafe int EverettDataItem()
        {
            // See if its a custom culture 
            if (this.IsCustomCulture)
            { 
                // They're hosed, this is a custom culture, return 0 (Invariant) 
                // It'd be better if Everett threw an error, but the accessors don't have to do
                // range checking on this, so we'd just read off the end of the data and get 
                // junk, which wouldn't be guaranteed to throw an error.  Invariant is a better choice.
                return 0;
            }
 
            InitEverettCultureDataItemMapping();
            // Normal culture, look up its data item from our LCID 
            // Do a binary search 
            int left = 0;
            int right = (m_EverettCultureDataItemMappingsSize/2) - 1; 

            while (left <= right)
            {
                int mid = (left+right)/2; 
                int result = this.m_CultureID - m_EverettCultureDataItemMappings[mid*2];
                if (result == 0) 
                { 
                    // Found it, return the index
                    return m_EverettCultureDataItemMappings[mid*2 + 1]; 
                }
                if (result < 0)
                    right = mid - 1;
                else 
                    left = mid + 1;
            } 
 
            // They're hosed, couldn't find an Everett data item for this culture.
            // It'd be better if Everett threw an error, but the accessors don't have to do 
            // range checking on this, so we'd just read off the end of the data and get
            // junk, which wouldn't be guaranteed to throw an error.  Invariant is a better choice.
            return 0;
        } 

        internal unsafe int EverettRegionDataItem() 
        { 
            // See if its a custom culture
            if (this.IsCustomCulture) 
            {
                // They're hosed, this is a custom culture, return 0 (Invariant)
                // It'd be better if Everett threw an error, but the accessors don't have to do
                // range checking on this, so we'd just read off the end of the data and get 
                // junk, which wouldn't be guaranteed to throw an error.  Invariant is a better choice.
                return 0; 
            } 

            InitEverettRegionDataItemMapping(); 
            // Normal culture, look up its data item from our LCID
            // Do a binary search
            int left = 0;
            int right = (m_EverettRegionDataItemMappingsSize/2) - 1; 

            while (left <= right) 
            { 
                int mid = (left+right)/2;
                int result = this.m_CultureID - m_EverettRegionDataItemMappings[mid*2]; 
                if (result == 0)
                {
                    // Found it, return the index
                    return m_EverettRegionDataItemMappings[mid*2 + 1]; 
                }
                if (result < 0) 
                    right = mid - 1; 
                else
                    left = mid + 1; 
            }

            // They're hosed, couldn't find an Everett data item for this culture.
            // It'd be better if Everett threw an error, but the accessors don't have to do 
            // range checking on this, so we'd just read off the end of the data and get
            // junk, which wouldn't be guaranteed to throw an error.  Invariant is a better choice. 
            return 0; 
        }
 
        internal static unsafe int IdFromEverettDataItem(int iDataItem)
        {
            InitEverettDataItemToLCIDMappings();
 
            // Assert that it exists
            BCLDebug.Assert(iDataItem >= 0 && iDataItem < m_EverettDataItemToLCIDMappingsSize, 
                String.Format( 
                    CultureInfo.CurrentCulture,
                    "[CultureTableRecord.IdFromEverettDataItem]Expected Everett data item in range of data table {0}", iDataItem)); 
            if (iDataItem < 0 || iDataItem >= m_EverettDataItemToLCIDMappingsSize)
            {
                // If the dataItem is not valid, throw.
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFieldState")); 
            }
            return m_EverettDataItemToLCIDMappings[iDataItem]; 
        } 

        internal static unsafe int IdFromEverettRegionInfoDataItem(int iDataItem) 
        {
            InitEverettRegionDataItemToLCIDMappings();

            // Assert that it exists 
            BCLDebug.Assert(iDataItem >= 0 && iDataItem < m_EverettRegionInfoDataItemToLCIDMappingsSize,
                String.Format( 
                    CultureInfo.CurrentCulture, 
                    "[CultureTableRecord.IdFromEverettRegionInfoDataItem]Expected Everett data item in range of data table {0}", iDataItem));
            if (iDataItem < 0 || iDataItem >= m_EverettRegionInfoDataItemToLCIDMappingsSize) 
            {
                // If the dataItem is not valid, throw.
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidFieldState"));
            } 
            return m_EverettRegionInfoDataItemToLCIDMappings[iDataItem];
        } 
 
        // The const here should be in sync with the one defined in the native side.
        const int INT32TABLE_EVERETT_REGION_DATA_ITEM_MAPPINGS          = 0; 
        const int INT32TABLE_EVERETT_CULTURE_DATA_ITEM_MAPPINGS         = 1;
        const int INT32TABLE_EVERETT_DATA_ITEM_TO_LCID_MAPPINGS         = 2;
        const int INT32TABLE_EVERETT_REGION_DATA_ITEM_TO_LCID_MAPPINGS  = 3;
 
        // Call InitEverettRegionDataItemMapping() before using these two.
        static unsafe int* m_EverettRegionDataItemMappings = null; 
        static unsafe int m_EverettRegionDataItemMappingsSize = 0; 

        //////////////////////////////////////////////////////////////////////// 
        //
        // Initialize the data used for mapping RegionInfo ID to dataItem.
        // Everett uses dataItem in persisting RegionInfo.
        // 
        ////////////////////////////////////////////////////////////////////////
 
        private static unsafe void InitEverettRegionDataItemMapping() 
        {
            if (m_EverettRegionDataItemMappings == null) 
            {
                int* temp = CultureInfo.nativeGetStaticInt32DataTable(INT32TABLE_EVERETT_REGION_DATA_ITEM_MAPPINGS, out m_EverettRegionDataItemMappingsSize);
                m_EverettRegionDataItemMappings = temp;
                BCLDebug.Assert(m_EverettRegionDataItemMappings != null, "CultureTableRecord.m_EverettRegionDataItemMappings can not be null"); 
                BCLDebug.Assert(m_EverettRegionDataItemMappingsSize > 0, "CultureTableRecord.m_EverettRegionDataItemMappingsSize > 0");
            } 
        } 

        // Call InitEverettCultureDataItemMapping before using these two. 
        unsafe static int* m_EverettCultureDataItemMappings = null;
        static int m_EverettCultureDataItemMappingsSize = 0;

        //////////////////////////////////////////////////////////////////////// 
        //
        // Initialize the data used for mapping CultureInfo ID to dataItem. 
        // Everett uses dataItem in persisting CultureInfo. 
        //
        //////////////////////////////////////////////////////////////////////// 

        private static unsafe void InitEverettCultureDataItemMapping()
        {
            if (m_EverettCultureDataItemMappings == null) 
            {
                int* temp = CultureInfo.nativeGetStaticInt32DataTable(INT32TABLE_EVERETT_CULTURE_DATA_ITEM_MAPPINGS, out m_EverettCultureDataItemMappingsSize); 
                m_EverettCultureDataItemMappings = temp; 
                BCLDebug.Assert(m_EverettCultureDataItemMappings != null, "CultureTableRecord.m_EverettCultureDataItemMappings can not be null");
                BCLDebug.Assert(m_EverettCultureDataItemMappingsSize > 0, "CultureTableRecord.m_EverettCultureDataItemMappingsSize > 0"); 
            }
        }

 
        // Call InitEverettDataItemToLCIDMappings() before using these two.
        private static unsafe int* m_EverettDataItemToLCIDMappings = null; 
        private static int m_EverettDataItemToLCIDMappingsSize = 0; 

        //////////////////////////////////////////////////////////////////////// 
        //
        // Initialize the CultureInfo data used for mapping an Everett dataItem to a LCID.
        // Everett uses dataItem in persisting CultureInfo.
        // 
        ////////////////////////////////////////////////////////////////////////
 
        private static unsafe void InitEverettDataItemToLCIDMappings() 
        {
            if (m_EverettDataItemToLCIDMappings == null) 
            {
                int* temp = CultureInfo.nativeGetStaticInt32DataTable(INT32TABLE_EVERETT_DATA_ITEM_TO_LCID_MAPPINGS, out m_EverettDataItemToLCIDMappingsSize);
                m_EverettDataItemToLCIDMappings = temp;
                BCLDebug.Assert(m_EverettDataItemToLCIDMappings != null, "CultureTableRecord.m_EverettDataItemToLCIDMappings can not be null"); 
                BCLDebug.Assert(m_EverettDataItemToLCIDMappingsSize > 0, "CultureTableRecord.m_EverettDataItemToLCIDMappingsSize > 0");
            } 
        } 

        // Call InitEverettRegionDataItemToLCIDMappings() before using these two. 
        private static unsafe int* m_EverettRegionInfoDataItemToLCIDMappings = null;
        private static int m_EverettRegionInfoDataItemToLCIDMappingsSize = 0;

        //////////////////////////////////////////////////////////////////////// 
        //
        // Initialize the RegionInfo data used for mapping an Everett dataItem to a LCID. 
        // Everett uses dataItem in persisting RegionInfo. 
        //
        //////////////////////////////////////////////////////////////////////// 

        private static unsafe void InitEverettRegionDataItemToLCIDMappings()
        {
            if (m_EverettRegionInfoDataItemToLCIDMappings == null) 
            {
                int* temp = CultureInfo.nativeGetStaticInt32DataTable( 
                                            INT32TABLE_EVERETT_REGION_DATA_ITEM_TO_LCID_MAPPINGS, 
                                            out m_EverettRegionInfoDataItemToLCIDMappingsSize);
 
                m_EverettRegionInfoDataItemToLCIDMappings = temp;
                BCLDebug.Assert(m_EverettRegionInfoDataItemToLCIDMappings != null,
                                "CultureTableRecord.m_EverettRegionInfoDataItemToLCIDMappings can not be null");
                BCLDebug.Assert(m_EverettRegionInfoDataItemToLCIDMappingsSize > 0, 
                                "CultureTableRecord.m_EverettRegionInfoDataItemToLCIDMappingsSize > 0");
            } 
        } 
    }
 
    ////////////////////////////////////////////////////////////////////////////
    //
    // This structure contains DateTimeFormatInfo properties that can be overridden by users.
    // We define this structure so that we can fill these values in one FCALL, instead of calling GetLocaleInfo() multiple times in 
    // separate FCalls.
    // 
    // NOTE: When adding int fields, be sure to pad an extra int so that they are 
    // aligned in DWORD.  By doing so, it will make sure that String fields are
    // aligned in DWORD. 
    //
    ////////////////////////////////////////////////////////////////////////////

    [StructLayout(LayoutKind.Sequential, Pack=2)] 
    internal struct DTFIUserOverrideValues {
        // DTFI values that are affected by calendar setttings. 
        internal String shortDatePattern; 
        internal String longDatePattern;
        internal String yearMonthPattern; 

        // DTFI values that will not be affected by calendar settings.
        internal String amDesignator;
        internal String pmDesignator; 
        internal String longTimePattern;
        internal int firstDayOfWeek; 
        internal int padding1;  // Add padding to make sure that we are aligned in DWORD. This is important for 64-bit platforms 
        internal int calendarWeekRule;
        internal int padding2;  // Add padding to make sure that we are aligned in DWORD. This is important for 64-bit platforms 
    }

    // CultureData has a cloned strucure in the native side. we send this struct to the native side to be filled
    // by the native APIs (mostly GetLocaleInfo) to load the synthetic cultures data. 
    //
    // IMPORTANT IMPORTANT IMPORTANT IMPORTANT IMPORTANT 
    // any change in this structure require a change in the cloned one in the native side. (ComNlsInfo.h/.cpp) 
    //
    // Also we use the default alignment which is 8-bytes in the managed and native sides so don't use the "Pack" property here 
    //
    // <SyntheticSupport/>
    [StructLayout(LayoutKind.Sequential)]
    internal struct CultureData 
    {
        internal string     sIso639Language;               // LOCALE_SISO639LANGNAME       (TwoLetterISOLanguageName) 
/* SSS_WARNINGS_OFF */        internal string     sIso3166CountryName;           // LOCALE_SISO3166CTRYNAME      (TwoLetterISORegionName) /* SSS_WARNINGS_ON */ 
        internal string     sListSeparator;                // LOCALE_SLIST                 (ListSeparator)
        internal string     sDecimalSeparator;             // LOCALE_SDECIMAL              (NumberDecimalSeparator) 
        internal string     sThousandSeparator;            // LOCALE_STHOUSAND             (NumberGroupSeparator)
        internal string     sCurrency;                     // LOCALE_SCURRENCY             (CurrencySymbol)
        internal string     sMonetaryDecimal;              // LOCALE_SMONDECIMALSEP        (CurrencyDecimalSeparator)
        internal string     sMonetaryThousand;             // LOCALE_SMONTHOUSANDSEP       (CurrencyGroupSeparator) 
        internal string     sNegativeSign;                 // LOCALE_SNEGATIVESIGN         (NegativeSign)
        internal string     sAM1159;                       // LOCALE_S1159                 (AMDesignator) 
        internal string     sPM2359;                       // LOCALE_S2359                 (PMDesignator) 
        internal string     sAbbrevLang;                   // LOCALE_SABBREVLANGNAME       (ThreeLetterWindowsLanguageName)
        internal string     sEnglishLanguage;              // LOCALE_SENGLANGUAGE          (Part of EnglishName) 
/* SSS_WARNINGS_OFF */        internal string     sEnglishCountry;               // LOCALE_SENGCOUNTRY           (Part of EnglishName)
        internal string     sNativeLanguage;               // LOCALE_SNATIVELANGNAME       (Part of NativeName)
        internal string     sNativeCountry;                // LOCALE_SNATIVECTRYNAME       (Part of NativeName)
        internal string     sAbbrevCountry;                // LOCALE_SABBREVCTRYNAME       (ThreeLetterWindowsRegionName) 
/* SSS_WARNINGS_ON */        internal string     sIntlMonetarySymbol;           // LOCALE_SINTLSYMBOL           (ISOCurrencySymbol)
        internal string     sEnglishCurrency;              // LOCALE_SENGCURRNAME          (CurrencyEnglishName) 
        internal string     sNativeCurrency;               // LOCALE_SNATIVECURRNAME       (CurrencyNativeName) 
        internal string     saAltSortID;                   // LOCALE_SSORTNAME             (SortName)
        internal string     sParentName;                   // LOCALE_SPARENT               (Parent) 
        internal string     sConsoleFallbackName;          // LOCALE_SCONSOLEFALLBACKNAME  (GetConsoleFallbackUICulture)


        // sPositiveSign in NLS always return empty string 
        internal string     sPositiveSign;                 // LOCALE_SPOSITIVESIGN         (PositiveSign)
 
        // saNativeDigits should be converted to array of string instead of array of characters later. 
        internal string     saNativeDigits;                // LOCALE_SNATIVEDIGITS         (NativeDigits)
 
        internal string     waGrouping;                    // LOCALE_SGROUPING             (NumberGroupSizes)
        internal string     waMonetaryGrouping;            // LOCALE_SMONGROUPING          (CurrencyGroupSizes)
        internal string     waFontSignature;               // LOCALE_FONTSIGNATURE         (No API for it)
 
        // Some fields defined only post XP
        internal string     sNaN;                          // LOCALE_SNAN                  (NaNSymbol) 
        internal string     sPositiveInfinity;             // LOCALE_SPOSINFINITY          (PositiveInfinitySymbol) 
        internal string     sNegativeInfinity;             // LOCALE_SNEGINFINITY          (NegativeInfinitySymbol)
/* SSS_WARNINGS_OFF */        internal string     sISO3166CountryName2;          // LOCALE_SISO3166CTRYNAME2     (ThreeLetterISORegionName) /* SSS_WARNINGS_ON */ 
        internal string     sISO639Language2;              // LOCALE_SISO639LANGNAME2      (ThreeLetterISOLanguageName)

        internal string []  saSuperShortDayNames;          // LOCALE_SSHORTESTDAYNAME1..LOCALE_SSHORTESTDAYNAME7 (ShortestDayNames)
        // End of the fields defined only post XP 

        internal string []  saTimeFormat;                  // EnumTimeFormats              (GetAllDateTimePatterns('T')) 
 
        internal string []  saShortDate;                   // EnumDateFormatsEx            (GetAllDateTimePatterns('d'))
        internal string []  saLongDate;                    // EnumDateFormatsEx            (GetAllDateTimePatterns('D')) 
        internal string []  saYearMonth;                   // EnumDateFormatsEx            (GetAllDateTimePatterns("Y"))

        internal string []  saMonthNames;                  // LOCALE_SMONTHNAME(1~13)      (MonthNames)
 
        // LOCALE_SDAYNAME1 means Monday in NLS (need conversion in NLS+
        internal string []  saDayNames;                    // LOCALE_SDAYNAME(1~7)         (GetDayOfWeekNames) 
        // LOCALE_SABBREVDAYNAME means Monday in NLS (need conversion in NLS+ 
        internal string []  saAbbrevDayNames;              // LOCALE_SABBREVDAYNAME(1~7)   (GetAbbreviatedDayOfWeekNames/SuperShortDayNames)
        internal string []  saAbbrevMonthNames;            // LOCALE_SABBREVMONTHNAME(1~13)(AbbreviatedMonthNames) 
        internal string []  saNativeCalendarNames;         // GetCalendarInfo/CAL_SCALNAME (NativeCalendarName)

        internal string []  saGenitiveMonthNames;          // GetDateFormat with "dd MMMM" (MonthGenitiveNames)
        internal string []  saAbbrevGenitiveMonthNames;    // GetDateFormat with "d MMM"   (AbbreviatedMonthGenitiveNames) 

        // use also EnumCalendarInfo/CAL_ICALINTVALUE 
        internal ushort []  waCalendars;                   // LOCALE_IOPTIONALCALENDAR     (OptionalCalendars) 

        // iFirstDayOfWeek (0 is Monday for NLS and is Sunday in NLS+) 
        internal int        iFirstDayOfWeek;               // LOCALE_IFIRSTDAYOFWEEK       (FirstDayOfWeek)
        internal int        iDigits;                       // LOCALE_IDIGITS               (NumberDecimalDigits)
        internal int        iNegativeNumber;               // LOCALE_INEGNUMBER            (NumberNegativePattern)
        internal int        iCurrencyDigits;               // LOCALE_ICURRDIGITS           (CurrencyDecimalDigits) 
        internal int        iCurrency;                     // LOCALE_ICURRENCY             (CurrencyPositivePattern)
        internal int        iNegativeCurrency;             // LOCALE_INEGCURR              (CurrencyNegativePattern) 
        internal int        iFirstWeekOfYear;              // LOCALE_IFIRSTWEEKOFYEAR      (CalendarWeekRule) 
        internal int        iMeasure;                      // LOCALE_IMEASURE              (IsMetric)
        internal int        iDigitSubstitution;            // LOCALE_IDIGITSUBSTITUTION    (DigitSubstitution) 
        internal int        iDefaultAnsiCodePage;          // LOCALE_IDEFAULTANSICODEPAGE  (ANSICodePage)
        internal int        iDefaultOemCodePage;           // LOCALE_IDEFAULTCODEPAGE      (OEMCodePage)
        internal int        iDefaultMacCodePage;           // LOCALE_IDEFAULTMACCODEPAGE   (MacCodePage)
        internal int        iDefaultEbcdicCodePage;        // LOCALE_IDEFAULTEBCDICCODEPAGE(EBCDICCodePage) 
/* SSS_WARNINGS_OFF */        internal int        iCountry;                      // LOCALE_ICOUNTRY              (No API for this field) /* SSS_WARNINGS_ON */
        internal int        iPaperSize;                    // LOCALE_IPAPERSIZE            (No API for this field) 
        internal int        iLeadingZeros;                 // LOCALE_IDAYLZERO             (No API for this field) 
        internal int        iIntlCurrencyDigits;           // LOCALE_IINTLCURRDIGITS       (No API for this field)
        internal int        iGeoId;                        // EnumSystemGeoID/GetGeoInfo   (RegionInfo.GeoId) 
        internal int        iDefaultCalender;              // LOCALE_ICALENDARTYPE         (No API for this field)
    }
}
