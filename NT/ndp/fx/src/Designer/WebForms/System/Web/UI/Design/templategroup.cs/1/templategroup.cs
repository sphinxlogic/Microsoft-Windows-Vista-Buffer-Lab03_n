//------------------------------------------------------------------------------ 
// <copyright file="TemplateGroup" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Web.UI.WebControls; 

    /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup"]/*' />
    public class TemplateGroup {
        private static TemplateDefinition[] emptyTemplateDefinitionArray = new TemplateDefinition[0]; 

        private string _groupName; 
        private Style _groupStyle; 
        private ArrayList _templates;
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.TemplateGroup"]/*' />
        public TemplateGroup(string groupName) : this(groupName, null) {
        }
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.TemplateGroup1"]/*' />
        public TemplateGroup(string groupName, Style groupStyle) { 
            _groupName = groupName; 
            _groupStyle = groupStyle;
        } 

        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.IsEmpty"]/*' />
        public bool IsEmpty {
            get { 
                return (_templates == null) || (_templates.Count == 0);
            } 
        } 

        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.GroupName"]/*' /> 
        public string GroupName {
            get {
                return _groupName;
            } 
        }
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.GroupStyle"]/*' /> 
        public Style GroupStyle {
            get { 
                return _groupStyle;
            }
        }
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.Templates"]/*' />
        public TemplateDefinition[] Templates { 
            get { 
                if (_templates == null) {
                    return emptyTemplateDefinitionArray; 
                }

                return (TemplateDefinition[])_templates.ToArray(typeof(TemplateDefinition));
            } 
        }
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.AddTemplateDefinition"]/*' /> 
        public void AddTemplateDefinition(TemplateDefinition templateDefinition) {
            if (_templates == null) { 
                _templates = new ArrayList();
            }

            _templates.Add(templateDefinition); 
        }
 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TemplateGroup" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Collections;
    using System.Web.UI.WebControls; 

    /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup"]/*' />
    public class TemplateGroup {
        private static TemplateDefinition[] emptyTemplateDefinitionArray = new TemplateDefinition[0]; 

        private string _groupName; 
        private Style _groupStyle; 
        private ArrayList _templates;
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.TemplateGroup"]/*' />
        public TemplateGroup(string groupName) : this(groupName, null) {
        }
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.TemplateGroup1"]/*' />
        public TemplateGroup(string groupName, Style groupStyle) { 
            _groupName = groupName; 
            _groupStyle = groupStyle;
        } 

        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.IsEmpty"]/*' />
        public bool IsEmpty {
            get { 
                return (_templates == null) || (_templates.Count == 0);
            } 
        } 

        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.GroupName"]/*' /> 
        public string GroupName {
            get {
                return _groupName;
            } 
        }
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.GroupStyle"]/*' /> 
        public Style GroupStyle {
            get { 
                return _groupStyle;
            }
        }
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.Templates"]/*' />
        public TemplateDefinition[] Templates { 
            get { 
                if (_templates == null) {
                    return emptyTemplateDefinitionArray; 
                }

                return (TemplateDefinition[])_templates.ToArray(typeof(TemplateDefinition));
            } 
        }
 
        /// <include file='doc\TemplateGroup.uex' path='docs/doc[@for="TemplateGroup.AddTemplateDefinition"]/*' /> 
        public void AddTemplateDefinition(TemplateDefinition templateDefinition) {
            if (_templates == null) { 
                _templates = new ArrayList();
            }

            _templates.Add(templateDefinition); 
        }
 
    } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
