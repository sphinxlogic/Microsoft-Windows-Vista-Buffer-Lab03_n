// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
#if MDA_SUPPORTED
 
namespace System.Runtime.InteropServices 
{
    using System; 
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
 
    using ObjectHandle = IntPtr;
    using GCHandleCookie = IntPtr; 
 
    // Internal class used to map a GCHandle to an IntPtr. Instead of handing out the underlying CLR
    // handle, we now hand out a cookie that can later be converted back to the CLR handle it 
    // is associated with.
    internal class GCHandleCookieTable
    {
        private const int MaxListSize = 0xFFFFFF; 
        private const uint CookieMaskIndex = 0x00FFFFFF;
        private const uint CookieMaskSentinal = 0xFF000000; 
 
        internal GCHandleCookieTable()
        { 
            m_HandleList = new ObjectHandle[10];
            m_CycleCounts = new byte[10];
            m_HandleToCookieMap = new Hashtable();
            m_FreeIndex = 1; 

            for (int i=0; i < 10; i++) 
            { 
                m_HandleList[i] = ObjectHandle.Zero;
                m_CycleCounts[i] = 0; 
            }
        }

        // Retrieve a cookie for the passed in handle. If no cookie has yet been allocated for 
        // this handle, one will be created. This method is thread safe.
        internal GCHandleCookie FindOrAddHandle(ObjectHandle handle) 
        { 
            // Don't accept a null object handle
            if (handle == ObjectHandle.Zero) 
                return GCHandleCookie.Zero;

            // First see if we already have a cookie for this handle.
            object tempobj = null; 
            tempobj = m_HandleToCookieMap[handle];
            if (tempobj != null) 
                return (GCHandleCookie)tempobj; 

 
            GCHandleCookie cookie = GCHandleCookie.Zero;

            // First, try the m_FreeIndex as a quick check
            int idx = m_FreeIndex; 
            if ((idx < m_HandleList.Length) && (m_HandleList[idx] == ObjectHandle.Zero))
            { 
                if (Interlocked.CompareExchange(ref m_HandleList[idx], handle, ObjectHandle.Zero) == ObjectHandle.Zero) 
                {
                    cookie = GetCookieFromData((uint)idx, m_CycleCounts[idx]); 

                    // Set our next guess just one higher if valid as this index is no longer a good guess.
                    if (idx+1 < m_HandleList.Length)
                        m_FreeIndex = idx+1; 
                }
            } 
 
            // Free index was taken...find an empty entry in the objecthandle list and fill it with the handle
            if (cookie == GCHandleCookie.Zero) 
            {
                for (idx = 1; idx < MaxListSize; idx++)
                {
                    // If we found an empty entry, try to use it. 
                    if (m_HandleList[idx] == ObjectHandle.Zero)
                    { 
                        if (Interlocked.CompareExchange(ref m_HandleList[idx], handle, ObjectHandle.Zero) == ObjectHandle.Zero) 
                        {
                            cookie = GetCookieFromData((uint)idx, m_CycleCounts[idx]); 

                            // Set our next guess just one higher if we ended up traversing the array.
                            if (idx+1 < m_HandleList.Length)
                                m_FreeIndex = idx+1; 

                            break; 
                        } 
                    }
 
                    // Make sure we have enough space in the list to continue the search.
                    if (idx+1 >= m_HandleList.Length)
                    {
                        lock(this) 
                        {
                            if (idx+1 >= m_HandleList.Length) 
                            { 
                                GrowArrays();
                            } 
                        }
                    }
                }
            } 

            // If we overflowed the array, we're out of memory. 
            if (cookie == GCHandleCookie.Zero) 
                throw new OutOfMemoryException(Environment.GetResourceString("OutOfMemory_GCHandleMDA"));
 
            // Now that we have a cookie - remember it in our hash table.
            lock (this)
            {
                // First check to see if another thread already added the handle. 
                tempobj = m_HandleToCookieMap[handle];
                if (tempobj != null) 
                { 
                    // The handle has already been added so release the cookie
                    m_HandleList[idx] = ObjectHandle.Zero; 
                    cookie = (GCHandleCookie)tempobj;
                }
                else
                { 
                    // This handle hasn't been added to the map yet so add it.
                    m_HandleToCookieMap[handle] = cookie; 
                } 
            }
 
            return cookie;
        }

        // Get a handle. 
        internal ObjectHandle GetHandle(GCHandleCookie cookie)
        { 
            ObjectHandle oh = ObjectHandle.Zero; 

            if (!ValidateCookie(cookie)) 
                return ObjectHandle.Zero;

            oh = m_HandleList[GetIndexFromCookie(cookie)];
 
            return oh;
        } 
 
        // Remove the handle from the cookie table if it is present.
        // 
        // IMPORTANT: This method is NOT synchronized so the caller of this function must ensure
        // it is only called once for a given handle.
        internal void RemoveHandleIfPresent(ObjectHandle handle)
        { 
            if (handle == ObjectHandle.Zero)
                return; 
 
            object tempobj = m_HandleToCookieMap[handle];
            if (tempobj != null) 
            {
                GCHandleCookie cookie = (GCHandleCookie)tempobj;

                // Remove it from the array first 
                if (!ValidateCookie(cookie))
                    return; 
 
                int index = GetIndexFromCookie(cookie);
 
                m_CycleCounts[index]++;
                m_HandleList[index] = ObjectHandle.Zero;

                // Remove it from the hashtable last 
                m_HandleToCookieMap.Remove(handle);
 
                // Update our guess 
                m_FreeIndex = index;
            } 
        }

        private bool ValidateCookie(GCHandleCookie cookie)
        { 
            int index;
            byte xorData; 
 
            GetDataFromCookie(cookie, out index, out xorData);
 
            // Validate the index
            if (index >= MaxListSize)
                return false;
 
            if (index >= m_HandleList.Length)
                return false; 
 
            if (m_HandleList[index] == ObjectHandle.Zero)
                return false; 

            // Validate the xorData byte (this contains the cycle count and appdomain id).
            byte ADID = (byte)(AppDomain.CurrentDomain.Id % 0xFF);
            byte goodData = (byte)(m_CycleCounts[index] ^ ADID); 
            if (xorData != goodData)
                return false; 
 
            return true;
        } 

        // Double the size of our arrays - must be called with the lock taken.
        private void GrowArrays()
        { 
            int CurrLength = m_HandleList.Length;
 
            ObjectHandle[] newHandleList = new ObjectHandle[CurrLength*2]; 
            byte[] newCycleCounts = new byte[CurrLength*2];
 
            Array.Copy(m_HandleList, newHandleList, CurrLength);
            Array.Copy(m_CycleCounts, newCycleCounts, CurrLength);

            m_HandleList = newHandleList; 
            m_CycleCounts = newCycleCounts;
        } 
 
        // Generate a cookie based on the index, cyclecount, and current domain id.
        private GCHandleCookie GetCookieFromData(uint index, byte cycleCount) 
        {
            byte ADID = (byte) (AppDomain.CurrentDomain.Id % 0xFF);
            return (GCHandleCookie) (((cycleCount ^ ADID) << 24) + index + 1);
        } 

        // Break down the cookie into its parts 
        private void GetDataFromCookie(GCHandleCookie cookie, out int index, out byte xorData) 
        {
            uint intCookie = (uint)cookie; 
            index = (int)(intCookie & CookieMaskIndex) - 1;
            xorData = (byte)((intCookie & CookieMaskSentinal) >> 24);
        }
 
        // Just get the index from the cookie
        private int GetIndexFromCookie(GCHandleCookie cookie) 
        { 
            uint intCookie = (uint)cookie;
            return (int)(intCookie & CookieMaskIndex) - 1; 
        }

        private Hashtable                                       m_HandleToCookieMap;
        private ObjectHandle[]                                  m_HandleList; 
        private byte[]                                          m_CycleCounts;
        int                                                     m_FreeIndex; 
    } 
}
 
#endif

// ==++== 
//
//   Copyright (c) Microsoft Corporation.  All rights reserved.
//
// ==--== 
#if MDA_SUPPORTED
 
namespace System.Runtime.InteropServices 
{
    using System; 
    using System.Collections;
    using System.Collections.Generic;
    using System.Threading;
 
    using ObjectHandle = IntPtr;
    using GCHandleCookie = IntPtr; 
 
    // Internal class used to map a GCHandle to an IntPtr. Instead of handing out the underlying CLR
    // handle, we now hand out a cookie that can later be converted back to the CLR handle it 
    // is associated with.
    internal class GCHandleCookieTable
    {
        private const int MaxListSize = 0xFFFFFF; 
        private const uint CookieMaskIndex = 0x00FFFFFF;
        private const uint CookieMaskSentinal = 0xFF000000; 
 
        internal GCHandleCookieTable()
        { 
            m_HandleList = new ObjectHandle[10];
            m_CycleCounts = new byte[10];
            m_HandleToCookieMap = new Hashtable();
            m_FreeIndex = 1; 

            for (int i=0; i < 10; i++) 
            { 
                m_HandleList[i] = ObjectHandle.Zero;
                m_CycleCounts[i] = 0; 
            }
        }

        // Retrieve a cookie for the passed in handle. If no cookie has yet been allocated for 
        // this handle, one will be created. This method is thread safe.
        internal GCHandleCookie FindOrAddHandle(ObjectHandle handle) 
        { 
            // Don't accept a null object handle
            if (handle == ObjectHandle.Zero) 
                return GCHandleCookie.Zero;

            // First see if we already have a cookie for this handle.
            object tempobj = null; 
            tempobj = m_HandleToCookieMap[handle];
            if (tempobj != null) 
                return (GCHandleCookie)tempobj; 

 
            GCHandleCookie cookie = GCHandleCookie.Zero;

            // First, try the m_FreeIndex as a quick check
            int idx = m_FreeIndex; 
            if ((idx < m_HandleList.Length) && (m_HandleList[idx] == ObjectHandle.Zero))
            { 
                if (Interlocked.CompareExchange(ref m_HandleList[idx], handle, ObjectHandle.Zero) == ObjectHandle.Zero) 
                {
                    cookie = GetCookieFromData((uint)idx, m_CycleCounts[idx]); 

                    // Set our next guess just one higher if valid as this index is no longer a good guess.
                    if (idx+1 < m_HandleList.Length)
                        m_FreeIndex = idx+1; 
                }
            } 
 
            // Free index was taken...find an empty entry in the objecthandle list and fill it with the handle
            if (cookie == GCHandleCookie.Zero) 
            {
                for (idx = 1; idx < MaxListSize; idx++)
                {
                    // If we found an empty entry, try to use it. 
                    if (m_HandleList[idx] == ObjectHandle.Zero)
                    { 
                        if (Interlocked.CompareExchange(ref m_HandleList[idx], handle, ObjectHandle.Zero) == ObjectHandle.Zero) 
                        {
                            cookie = GetCookieFromData((uint)idx, m_CycleCounts[idx]); 

                            // Set our next guess just one higher if we ended up traversing the array.
                            if (idx+1 < m_HandleList.Length)
                                m_FreeIndex = idx+1; 

                            break; 
                        } 
                    }
 
                    // Make sure we have enough space in the list to continue the search.
                    if (idx+1 >= m_HandleList.Length)
                    {
                        lock(this) 
                        {
                            if (idx+1 >= m_HandleList.Length) 
                            { 
                                GrowArrays();
                            } 
                        }
                    }
                }
            } 

            // If we overflowed the array, we're out of memory. 
            if (cookie == GCHandleCookie.Zero) 
                throw new OutOfMemoryException(Environment.GetResourceString("OutOfMemory_GCHandleMDA"));
 
            // Now that we have a cookie - remember it in our hash table.
            lock (this)
            {
                // First check to see if another thread already added the handle. 
                tempobj = m_HandleToCookieMap[handle];
                if (tempobj != null) 
                { 
                    // The handle has already been added so release the cookie
                    m_HandleList[idx] = ObjectHandle.Zero; 
                    cookie = (GCHandleCookie)tempobj;
                }
                else
                { 
                    // This handle hasn't been added to the map yet so add it.
                    m_HandleToCookieMap[handle] = cookie; 
                } 
            }
 
            return cookie;
        }

        // Get a handle. 
        internal ObjectHandle GetHandle(GCHandleCookie cookie)
        { 
            ObjectHandle oh = ObjectHandle.Zero; 

            if (!ValidateCookie(cookie)) 
                return ObjectHandle.Zero;

            oh = m_HandleList[GetIndexFromCookie(cookie)];
 
            return oh;
        } 
 
        // Remove the handle from the cookie table if it is present.
        // 
        // IMPORTANT: This method is NOT synchronized so the caller of this function must ensure
        // it is only called once for a given handle.
        internal void RemoveHandleIfPresent(ObjectHandle handle)
        { 
            if (handle == ObjectHandle.Zero)
                return; 
 
            object tempobj = m_HandleToCookieMap[handle];
            if (tempobj != null) 
            {
                GCHandleCookie cookie = (GCHandleCookie)tempobj;

                // Remove it from the array first 
                if (!ValidateCookie(cookie))
                    return; 
 
                int index = GetIndexFromCookie(cookie);
 
                m_CycleCounts[index]++;
                m_HandleList[index] = ObjectHandle.Zero;

                // Remove it from the hashtable last 
                m_HandleToCookieMap.Remove(handle);
 
                // Update our guess 
                m_FreeIndex = index;
            } 
        }

        private bool ValidateCookie(GCHandleCookie cookie)
        { 
            int index;
            byte xorData; 
 
            GetDataFromCookie(cookie, out index, out xorData);
 
            // Validate the index
            if (index >= MaxListSize)
                return false;
 
            if (index >= m_HandleList.Length)
                return false; 
 
            if (m_HandleList[index] == ObjectHandle.Zero)
                return false; 

            // Validate the xorData byte (this contains the cycle count and appdomain id).
            byte ADID = (byte)(AppDomain.CurrentDomain.Id % 0xFF);
            byte goodData = (byte)(m_CycleCounts[index] ^ ADID); 
            if (xorData != goodData)
                return false; 
 
            return true;
        } 

        // Double the size of our arrays - must be called with the lock taken.
        private void GrowArrays()
        { 
            int CurrLength = m_HandleList.Length;
 
            ObjectHandle[] newHandleList = new ObjectHandle[CurrLength*2]; 
            byte[] newCycleCounts = new byte[CurrLength*2];
 
            Array.Copy(m_HandleList, newHandleList, CurrLength);
            Array.Copy(m_CycleCounts, newCycleCounts, CurrLength);

            m_HandleList = newHandleList; 
            m_CycleCounts = newCycleCounts;
        } 
 
        // Generate a cookie based on the index, cyclecount, and current domain id.
        private GCHandleCookie GetCookieFromData(uint index, byte cycleCount) 
        {
            byte ADID = (byte) (AppDomain.CurrentDomain.Id % 0xFF);
            return (GCHandleCookie) (((cycleCount ^ ADID) << 24) + index + 1);
        } 

        // Break down the cookie into its parts 
        private void GetDataFromCookie(GCHandleCookie cookie, out int index, out byte xorData) 
        {
            uint intCookie = (uint)cookie; 
            index = (int)(intCookie & CookieMaskIndex) - 1;
            xorData = (byte)((intCookie & CookieMaskSentinal) >> 24);
        }
 
        // Just get the index from the cookie
        private int GetIndexFromCookie(GCHandleCookie cookie) 
        { 
            uint intCookie = (uint)cookie;
            return (int)(intCookie & CookieMaskIndex) - 1; 
        }

        private Hashtable                                       m_HandleToCookieMap;
        private ObjectHandle[]                                  m_HandleList; 
        private byte[]                                          m_CycleCounts;
        int                                                     m_FreeIndex; 
    } 
}
 
#endif

