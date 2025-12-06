 
//------------------------------------------------------------------------------
// <copyright from='1997' to='2003' company='Microsoft Corporation'>
//    Copyright (c) Microsoft Corporation. All Rights Reserved.
//    Information Contained Herein is Proprietary and Confidential. 
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Windows.Forms;
    using System.Diagnostics; 
 

    /// <summary> 
    /// </summary>
    internal abstract class Source: DataSourceComponent, IDataSourceNamedObject, ICloneable {
        protected string                      name;
        private MemberAttributes            modifier; 
        protected DataSourceComponent owner;
        private bool webMethod; 
        private string webMethodDescription; 

        private string userSourceName = null; 
        private string generatorSourceName = null;
 		private string generatorGetMethodName = null;
		private string generatorSourceNameForPaging = null;
		private string generatorGetMethodNameForPaging = null; 

        internal Source() { 
            modifier = MemberAttributes.Public; 
        }
 
        [
            DataSourceXmlAttribute(),
            DefaultValue(false)
        ] 
        public bool EnableWebMethods {
            get { 
                return this.webMethod; 
            }
            set { 
                this.webMethod = value;
            }
        }
 
        internal bool IsMainSource {
            get { 
                DesignTable table = Owner as DesignTable; 
                return (table != null && table.MainSource == this);
            } 
        }

        [
            DefaultValue(MemberAttributes.Public), 
            DataSourceXmlAttribute()
        ] 
        public MemberAttributes Modifier { 
            get {
                return modifier; 
            }
            set {
                modifier = value;
            } 
        }
 
        /// <summary> 
        /// </summary>
        [ 
            DefaultValue(""),
            DataSourceXmlAttribute(),
            MergableProperty(false)
        ] 
        public virtual string Name{
            get{ 
                return name; 
            }
            set{ 
                if (name != value) {
                    if (this.CollectionParent != null) {
                        this.CollectionParent.ValidateUniqueName (this, value);
                    } 
                    name = value;
                } 
            } 
        }
 
        /// <summary>
        /// Displayed in the Table Control with signature, different with that in the property grid
        /// </summary>
        internal virtual string DisplayName { 
            get {
                return Name; 
            } 
            set {
            } 
        }

        /// <summary>
        /// This is the reference to a DesignTable or DesignDataSource that owns this source. 
        /// Only one DataSourceComponent at a time can own a given source.
        /// </summary> 
        [Browsable(false)] 
        internal DataSourceComponent Owner {
            get { 
                if (this.owner == null) {
                    // check to see we can find the owner from parent
                    if (this.CollectionParent != null) {
                        SourceCollection collection = this.CollectionParent as SourceCollection; 
                        Debug.Assert (collection != null, " Source is not in a SourceCollection?");
                        if (collection != null) { 
                            this.owner = (DataSourceComponent)collection.CollectionHost; 
                        }
                    } 
                }
                return this.owner;
            }
            set { 
                Debug.Assert( value == null || value is DesignTable || value is DesignDataSource );
                this.owner = value; 
            } 
        }
 
        [Browsable(false)]
        public virtual string PublicTypeName {
            get {
                return "Function"; 
            }
        } 
 
        [
			DataSourceXmlAttribute(), 
            DefaultValue("")
        ]
        public string WebMethodDescription {
            get{ 
                return this.webMethodDescription;
            } 
            set { 
                this.webMethodDescription = value;
            } 
        }


 
        public abstract object Clone();
 
        /// <summary> 
        /// Compare the source by name
        /// </summary> 
        /// <param name="srcToComapre"></param>
        /// <returns></returns>
        internal virtual bool NameExist(string nameToCheck) {
            Debug.Assert(StringUtil.NotEmptyAfterTrim(nameToCheck)); 
            return StringUtil.EqualValue(this.Name, nameToCheck, true /*CaseInsensitive*/);
        } 
 
        /// <summary>
        /// IDataSourceCollectionMember implementation 
        /// </summary>
        public override void SetCollection(DataSourceCollectionBase collection) {
            base.SetCollection(collection);
            if (collection != null) { 
                Debug.Assert(collection is SourceCollection);
                this.Owner = (DataSourceComponent)collection.CollectionHost;  // Might be null 
            } 
            else {
                this.Owner = null; 
            }
        }

        public override string ToString() { 
            return this.PublicTypeName + " " + this.DisplayName;
        } 
 
        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string UserSourceName { 
            get {
                return userSourceName;
            }
            set { 
                userSourceName = value;
            } 
        } 

        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)] 
        public string GeneratorSourceName {
            get {
                return generatorSourceName;
            } 
            set {
                generatorSourceName = value; 
            } 
        }
 
        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string GeneratorGetMethodName {
            get {
                return generatorGetMethodName; 
            }
            set { 
                generatorGetMethodName = value; 
            }
        } 

        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string GeneratorSourceNameForPaging {
            get { 
                return generatorSourceNameForPaging;
            } 
            set { 
                generatorSourceNameForPaging = value;
            } 
        }

        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string GeneratorGetMethodNameForPaging { 
            get {
                return generatorGetMethodNameForPaging; 
            } 
            set {
                generatorGetMethodNameForPaging = value; 
            }
        }

 
        // IDataSourceRenamableObject implementation
        [Browsable(false)] 
        public override string GeneratorName { 
            get {
                return GeneratorSourceName; 
            }
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
namespace System.Data.Design { 

    using System; 
    using System.CodeDom;
    using System.Collections;
    using System.ComponentModel;
    using System.Data; 
    using System.Windows.Forms;
    using System.Diagnostics; 
 

    /// <summary> 
    /// </summary>
    internal abstract class Source: DataSourceComponent, IDataSourceNamedObject, ICloneable {
        protected string                      name;
        private MemberAttributes            modifier; 
        protected DataSourceComponent owner;
        private bool webMethod; 
        private string webMethodDescription; 

        private string userSourceName = null; 
        private string generatorSourceName = null;
 		private string generatorGetMethodName = null;
		private string generatorSourceNameForPaging = null;
		private string generatorGetMethodNameForPaging = null; 

        internal Source() { 
            modifier = MemberAttributes.Public; 
        }
 
        [
            DataSourceXmlAttribute(),
            DefaultValue(false)
        ] 
        public bool EnableWebMethods {
            get { 
                return this.webMethod; 
            }
            set { 
                this.webMethod = value;
            }
        }
 
        internal bool IsMainSource {
            get { 
                DesignTable table = Owner as DesignTable; 
                return (table != null && table.MainSource == this);
            } 
        }

        [
            DefaultValue(MemberAttributes.Public), 
            DataSourceXmlAttribute()
        ] 
        public MemberAttributes Modifier { 
            get {
                return modifier; 
            }
            set {
                modifier = value;
            } 
        }
 
        /// <summary> 
        /// </summary>
        [ 
            DefaultValue(""),
            DataSourceXmlAttribute(),
            MergableProperty(false)
        ] 
        public virtual string Name{
            get{ 
                return name; 
            }
            set{ 
                if (name != value) {
                    if (this.CollectionParent != null) {
                        this.CollectionParent.ValidateUniqueName (this, value);
                    } 
                    name = value;
                } 
            } 
        }
 
        /// <summary>
        /// Displayed in the Table Control with signature, different with that in the property grid
        /// </summary>
        internal virtual string DisplayName { 
            get {
                return Name; 
            } 
            set {
            } 
        }

        /// <summary>
        /// This is the reference to a DesignTable or DesignDataSource that owns this source. 
        /// Only one DataSourceComponent at a time can own a given source.
        /// </summary> 
        [Browsable(false)] 
        internal DataSourceComponent Owner {
            get { 
                if (this.owner == null) {
                    // check to see we can find the owner from parent
                    if (this.CollectionParent != null) {
                        SourceCollection collection = this.CollectionParent as SourceCollection; 
                        Debug.Assert (collection != null, " Source is not in a SourceCollection?");
                        if (collection != null) { 
                            this.owner = (DataSourceComponent)collection.CollectionHost; 
                        }
                    } 
                }
                return this.owner;
            }
            set { 
                Debug.Assert( value == null || value is DesignTable || value is DesignDataSource );
                this.owner = value; 
            } 
        }
 
        [Browsable(false)]
        public virtual string PublicTypeName {
            get {
                return "Function"; 
            }
        } 
 
        [
			DataSourceXmlAttribute(), 
            DefaultValue("")
        ]
        public string WebMethodDescription {
            get{ 
                return this.webMethodDescription;
            } 
            set { 
                this.webMethodDescription = value;
            } 
        }


 
        public abstract object Clone();
 
        /// <summary> 
        /// Compare the source by name
        /// </summary> 
        /// <param name="srcToComapre"></param>
        /// <returns></returns>
        internal virtual bool NameExist(string nameToCheck) {
            Debug.Assert(StringUtil.NotEmptyAfterTrim(nameToCheck)); 
            return StringUtil.EqualValue(this.Name, nameToCheck, true /*CaseInsensitive*/);
        } 
 
        /// <summary>
        /// IDataSourceCollectionMember implementation 
        /// </summary>
        public override void SetCollection(DataSourceCollectionBase collection) {
            base.SetCollection(collection);
            if (collection != null) { 
                Debug.Assert(collection is SourceCollection);
                this.Owner = (DataSourceComponent)collection.CollectionHost;  // Might be null 
            } 
            else {
                this.Owner = null; 
            }
        }

        public override string ToString() { 
            return this.PublicTypeName + " " + this.DisplayName;
        } 
 
        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string UserSourceName { 
            get {
                return userSourceName;
            }
            set { 
                userSourceName = value;
            } 
        } 

        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)] 
        public string GeneratorSourceName {
            get {
                return generatorSourceName;
            } 
            set {
                generatorSourceName = value; 
            } 
        }
 
        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string GeneratorGetMethodName {
            get {
                return generatorGetMethodName; 
            }
            set { 
                generatorGetMethodName = value; 
            }
        } 

        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string GeneratorSourceNameForPaging {
            get { 
                return generatorSourceNameForPaging;
            } 
            set { 
                generatorSourceNameForPaging = value;
            } 
        }

        [DataSourceXmlAttribute(), Browsable (false), DefaultValue(null)]
        public string GeneratorGetMethodNameForPaging { 
            get {
                return generatorGetMethodNameForPaging; 
            } 
            set {
                generatorGetMethodNameForPaging = value; 
            }
        }

 
        // IDataSourceRenamableObject implementation
        [Browsable(false)] 
        public override string GeneratorName { 
            get {
                return GeneratorSourceName; 
            }
        }

    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
