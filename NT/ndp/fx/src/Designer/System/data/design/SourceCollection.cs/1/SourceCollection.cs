//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
namespace System.Data.Design{ 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Design;

 
    /// <summary>
    /// </summary> 
    internal class SourceCollection : DataSourceCollectionBase, ICloneable { 

        internal SourceCollection(DataSourceComponent collectionHost) : base(collectionHost) { 
        }

        protected override Type ItemType {
            get { 
                return typeof(Source);
            } 
        } 

        private DbSource MainSource { 
            get{
                DesignTable table = CollectionHost as DesignTable;

                return table.MainSource as DbSource; 
            }
        } 
 
        protected override INameService NameService {
            get { 
                return SourceNameService.DefaultInstance;
            }
        }
 
        public int Add( Source s ) {
            return List.Add( s ); 
        } 

        public object Clone() { 
            SourceCollection clone = new SourceCollection(null);

            foreach (Source s in this) {
                clone.Add((Source)s.Clone()); 
            }
 
            return clone; 
        }
 
        public bool Contains(Source s) {
            return List.Contains( s );
        }
 
        bool DbSourceNameExist(DbSource dbSource, bool isFillName, string nameToBeChecked) {
            // Check both names even though a dbSource may only use one name at the time. 
            // When a user change the DbSource's type to have two names, we then have a good starting name for her. 
            //
            if (isFillName && StringUtil.EqualValue(nameToBeChecked, dbSource.GetMethodName, true)) { 
                return true;
            }
            if (!isFillName && StringUtil.EqualValue(nameToBeChecked, dbSource.FillMethodName, true)) {
                return true; 
            }
            foreach (DbSource s in this) { 
                if (s != dbSource && s.NameExist(nameToBeChecked)){ 
                    return true;
                } 
            }

            DbSource mainSource = MainSource;
            if (dbSource != mainSource && mainSource != null && mainSource.NameExist(nameToBeChecked)) { 
                return true;
            } 
 
            return false;
        } 


        /// <summary>
        /// </summary> 
        /// <param name="name"></param>
        /// <returns></returns> 
        internal override protected IDataSourceNamedObject FindObject(string name) { 
            DbSource mainSource = MainSource;
 
            if (mainSource != null && mainSource.NameExist(name)) {
                return mainSource;
            }
 
            IEnumerator e = this.InnerList.GetEnumerator();
            while (e.MoveNext()) { 
                DbSource dbSource = e.Current as DbSource; 
                if (dbSource != null) {
                    if (dbSource.NameExist(name)) { 
                        return dbSource;
                    }
                }
                else { 
                    IDataSourceNamedObject existing = (IDataSourceNamedObject)e.Current;
                    if (StringUtil.EqualValue(existing.Name, name, false /*caseinsensitive*/)) { 
                        return existing; 
                    }
                } 
            }

            return null;
        } 

        public int IndexOf(Source s) { 
            return List.IndexOf(s); 
        }
 
        public void Remove(Source s) {
            List.Remove( s );
        }
 
#if not_needed_yet
        /// <summary> 
        /// MainSource is not included in the sources collection, we fix the name validation for MainSource here. 
        /// As MainSource name is fixed and not editable, we don't need to call this function to do validation
        /// </summary> 
        /// <param name="nameToCheck"></param>
        /// <returns></returns>
        internal void ValidateNameForMainSource(string mainSourceNameTobeCheck) {
            base.ValidateName(null, mainSourceNameTobeCheck); 
        }
#endif 
        /// <summary> 
        /// As MainSource is not included in the sources collection, we fix the name validation for sources here.
        /// </summary> 
        /// <param name="nameToCheck"></param>
        /// <returns></returns>
        private void ValidateNameWithMainSource(object dbSourceToCheck, string nameToCheck) {
            DbSource mainSource = MainSource; 
            if (dbSourceToCheck != mainSource && mainSource != null) {
                if (mainSource.NameExist(nameToCheck)){ 
                    throw new NameValidationException(SR.GetString(SR.CM_NameExist, nameToCheck)); 
                }
            } 
        }

        /// <summary>
        /// </summary> 
        /// <param name="obj"></param>
        internal protected override void ValidateName(IDataSourceNamedObject obj) { 
            DbSource dbSource = obj as DbSource; 
            if (dbSource != null) {
                if ((dbSource.GenerateMethods & GenerateMethodTypes.Get) == GenerateMethodTypes.Get) { 
                    this.NameService.ValidateName(dbSource.GetMethodName);
                }
                if ((dbSource.GenerateMethods & GenerateMethodTypes.Fill) == GenerateMethodTypes.Fill) {
                    this.NameService.ValidateName(dbSource.FillMethodName); 
                }
            } 
            else { 
                base.ValidateName(obj);
            } 
        }

        /// <summary>
        /// </summary> 
        /// <param name="obj"></param>
        internal protected override void ValidateUniqueName(IDataSourceNamedObject obj, string proposedName) { 
            Debug.Assert(!(obj is DbSource), "We should not call this function if it is a dbsource"); 
            ValidateNameWithMainSource(obj, proposedName);
            base.ValidateUniqueName(obj, proposedName); 
        }

        internal void ValidateUniqueDbSourceName(DbSource dbSource, string proposedName, bool isFillName) {
            if (this.DbSourceNameExist(dbSource, isFillName, proposedName)) { 
                throw new NameValidationException(SR.GetString(SR.CM_NameExist, proposedName));
            } 
            this.NameService.ValidateName(proposedName); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential.
// </copyright> 
//-----------------------------------------------------------------------------
namespace System.Data.Design{ 
 
    using System;
    using System.Collections; 
    using System.Diagnostics;
    using System.Design;

 
    /// <summary>
    /// </summary> 
    internal class SourceCollection : DataSourceCollectionBase, ICloneable { 

        internal SourceCollection(DataSourceComponent collectionHost) : base(collectionHost) { 
        }

        protected override Type ItemType {
            get { 
                return typeof(Source);
            } 
        } 

        private DbSource MainSource { 
            get{
                DesignTable table = CollectionHost as DesignTable;

                return table.MainSource as DbSource; 
            }
        } 
 
        protected override INameService NameService {
            get { 
                return SourceNameService.DefaultInstance;
            }
        }
 
        public int Add( Source s ) {
            return List.Add( s ); 
        } 

        public object Clone() { 
            SourceCollection clone = new SourceCollection(null);

            foreach (Source s in this) {
                clone.Add((Source)s.Clone()); 
            }
 
            return clone; 
        }
 
        public bool Contains(Source s) {
            return List.Contains( s );
        }
 
        bool DbSourceNameExist(DbSource dbSource, bool isFillName, string nameToBeChecked) {
            // Check both names even though a dbSource may only use one name at the time. 
            // When a user change the DbSource's type to have two names, we then have a good starting name for her. 
            //
            if (isFillName && StringUtil.EqualValue(nameToBeChecked, dbSource.GetMethodName, true)) { 
                return true;
            }
            if (!isFillName && StringUtil.EqualValue(nameToBeChecked, dbSource.FillMethodName, true)) {
                return true; 
            }
            foreach (DbSource s in this) { 
                if (s != dbSource && s.NameExist(nameToBeChecked)){ 
                    return true;
                } 
            }

            DbSource mainSource = MainSource;
            if (dbSource != mainSource && mainSource != null && mainSource.NameExist(nameToBeChecked)) { 
                return true;
            } 
 
            return false;
        } 


        /// <summary>
        /// </summary> 
        /// <param name="name"></param>
        /// <returns></returns> 
        internal override protected IDataSourceNamedObject FindObject(string name) { 
            DbSource mainSource = MainSource;
 
            if (mainSource != null && mainSource.NameExist(name)) {
                return mainSource;
            }
 
            IEnumerator e = this.InnerList.GetEnumerator();
            while (e.MoveNext()) { 
                DbSource dbSource = e.Current as DbSource; 
                if (dbSource != null) {
                    if (dbSource.NameExist(name)) { 
                        return dbSource;
                    }
                }
                else { 
                    IDataSourceNamedObject existing = (IDataSourceNamedObject)e.Current;
                    if (StringUtil.EqualValue(existing.Name, name, false /*caseinsensitive*/)) { 
                        return existing; 
                    }
                } 
            }

            return null;
        } 

        public int IndexOf(Source s) { 
            return List.IndexOf(s); 
        }
 
        public void Remove(Source s) {
            List.Remove( s );
        }
 
#if not_needed_yet
        /// <summary> 
        /// MainSource is not included in the sources collection, we fix the name validation for MainSource here. 
        /// As MainSource name is fixed and not editable, we don't need to call this function to do validation
        /// </summary> 
        /// <param name="nameToCheck"></param>
        /// <returns></returns>
        internal void ValidateNameForMainSource(string mainSourceNameTobeCheck) {
            base.ValidateName(null, mainSourceNameTobeCheck); 
        }
#endif 
        /// <summary> 
        /// As MainSource is not included in the sources collection, we fix the name validation for sources here.
        /// </summary> 
        /// <param name="nameToCheck"></param>
        /// <returns></returns>
        private void ValidateNameWithMainSource(object dbSourceToCheck, string nameToCheck) {
            DbSource mainSource = MainSource; 
            if (dbSourceToCheck != mainSource && mainSource != null) {
                if (mainSource.NameExist(nameToCheck)){ 
                    throw new NameValidationException(SR.GetString(SR.CM_NameExist, nameToCheck)); 
                }
            } 
        }

        /// <summary>
        /// </summary> 
        /// <param name="obj"></param>
        internal protected override void ValidateName(IDataSourceNamedObject obj) { 
            DbSource dbSource = obj as DbSource; 
            if (dbSource != null) {
                if ((dbSource.GenerateMethods & GenerateMethodTypes.Get) == GenerateMethodTypes.Get) { 
                    this.NameService.ValidateName(dbSource.GetMethodName);
                }
                if ((dbSource.GenerateMethods & GenerateMethodTypes.Fill) == GenerateMethodTypes.Fill) {
                    this.NameService.ValidateName(dbSource.FillMethodName); 
                }
            } 
            else { 
                base.ValidateName(obj);
            } 
        }

        /// <summary>
        /// </summary> 
        /// <param name="obj"></param>
        internal protected override void ValidateUniqueName(IDataSourceNamedObject obj, string proposedName) { 
            Debug.Assert(!(obj is DbSource), "We should not call this function if it is a dbsource"); 
            ValidateNameWithMainSource(obj, proposedName);
            base.ValidateUniqueName(obj, proposedName); 
        }

        internal void ValidateUniqueDbSourceName(DbSource dbSource, string proposedName, bool isFillName) {
            if (this.DbSourceNameExist(dbSource, isFillName, proposedName)) { 
                throw new NameValidationException(SR.GetString(SR.CM_NameExist, proposedName));
            } 
            this.NameService.ValidateName(proposedName); 
        }
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
