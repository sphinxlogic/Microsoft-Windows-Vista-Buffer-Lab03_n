//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System; 
    using System.Collections;
 
    // <include file='doc\DesignerActionItemCollection.uex' path='docs/doc[@for="DesignerActionItemCollection"]/*' />
    /// <devdoc>
    ///     [tbd]
    /// </devdoc> 
    public class DesignerActionItemCollection : CollectionBase
    { 
        public DesignerActionItemCollection()    { 
        }
 
        public DesignerActionItem this[int index]  {
            get {
                return (DesignerActionItem)(List[index]);
            } 
            set {
                List[index] = value; 
            } 
        }
 
        public int Add(DesignerActionItem value)   {
            int index = List.Add(value);
            return index;
        } 

        public bool Contains(DesignerActionItem value)   { 
            return List.Contains(value); 
        }
 
        public void CopyTo(DesignerActionItem[] array, int index)    {
            List.CopyTo(array, index);
        }
 
        public int IndexOf(DesignerActionItem value)   {
            return List.IndexOf(value); 
        } 

        public void Insert(int index, DesignerActionItem value)   { 
            List.Insert(index, value);
        }

        public void Remove(DesignerActionItem value)     { 
            List.Remove(value);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerActionList.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.ComponentModel.Design { 
    using System; 
    using System.Collections;
 
    // <include file='doc\DesignerActionItemCollection.uex' path='docs/doc[@for="DesignerActionItemCollection"]/*' />
    /// <devdoc>
    ///     [tbd]
    /// </devdoc> 
    public class DesignerActionItemCollection : CollectionBase
    { 
        public DesignerActionItemCollection()    { 
        }
 
        public DesignerActionItem this[int index]  {
            get {
                return (DesignerActionItem)(List[index]);
            } 
            set {
                List[index] = value; 
            } 
        }
 
        public int Add(DesignerActionItem value)   {
            int index = List.Add(value);
            return index;
        } 

        public bool Contains(DesignerActionItem value)   { 
            return List.Contains(value); 
        }
 
        public void CopyTo(DesignerActionItem[] array, int index)    {
            List.CopyTo(array, index);
        }
 
        public int IndexOf(DesignerActionItem value)   {
            return List.IndexOf(value); 
        } 

        public void Insert(int index, DesignerActionItem value)   { 
            List.Insert(index, value);
        }

        public void Remove(DesignerActionItem value)     { 
            List.Remove(value);
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
