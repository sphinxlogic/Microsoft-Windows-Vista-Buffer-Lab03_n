//------------------------------------------------------------------------------ 
// <copyright file="TemplatedEditableDesignerRegion.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Design;
    using System.ComponentModel; 

    public class TemplatedEditableDesignerRegion : EditableDesignerRegion {
        private TemplateDefinition _templateDefinition;
        private bool _isSingleInstance = false; 

        public TemplatedEditableDesignerRegion(TemplateDefinition templateDefinition) : 
            base(templateDefinition.Designer, templateDefinition.Name, templateDefinition.ServerControlsOnly) { 
            _templateDefinition = templateDefinition;
        } 

        public virtual bool IsSingleInstanceTemplate {
            get {
                return _isSingleInstance; 
            }
            set { 
                _isSingleInstance = value; 
            }
        } 

        public override bool SupportsDataBinding {
            get {
                return _templateDefinition.SupportsDataBinding; 
            }
            set { 
                throw new InvalidOperationException(SR.GetString(SR.TemplateEditableDesignerRegion_CannotSetSupportsDataBinding)); 
            }
        } 

        public TemplateDefinition TemplateDefinition {
            get {
                return _templateDefinition; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="TemplatedEditableDesignerRegion.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Web.UI.Design { 
    using System; 
    using System.Design;
    using System.ComponentModel; 

    public class TemplatedEditableDesignerRegion : EditableDesignerRegion {
        private TemplateDefinition _templateDefinition;
        private bool _isSingleInstance = false; 

        public TemplatedEditableDesignerRegion(TemplateDefinition templateDefinition) : 
            base(templateDefinition.Designer, templateDefinition.Name, templateDefinition.ServerControlsOnly) { 
            _templateDefinition = templateDefinition;
        } 

        public virtual bool IsSingleInstanceTemplate {
            get {
                return _isSingleInstance; 
            }
            set { 
                _isSingleInstance = value; 
            }
        } 

        public override bool SupportsDataBinding {
            get {
                return _templateDefinition.SupportsDataBinding; 
            }
            set { 
                throw new InvalidOperationException(SR.GetString(SR.TemplateEditableDesignerRegion_CannotSetSupportsDataBinding)); 
            }
        } 

        public TemplateDefinition TemplateDefinition {
            get {
                return _templateDefinition; 
            }
        } 
    } 
}
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
