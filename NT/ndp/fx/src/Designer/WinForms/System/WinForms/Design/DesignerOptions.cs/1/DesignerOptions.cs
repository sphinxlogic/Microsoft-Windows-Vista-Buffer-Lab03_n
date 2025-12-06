//------------------------------------------------------------------------------ 
// <copyright file="DesignerOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
    using Microsoft.Win32; 
    using System;
    using System.Design; 
    using System.Drawing;
    using System.ComponentModel.Design;
    using System.Collections;
    using System.ComponentModel; 
    using System.Diagnostics;
    using System.Globalization; 
 
    /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions"]/*' />
    /// <devdoc> 
    ///     Provides access to get and set option values for a designer.
    /// </devdoc>
    public class DesignerOptions {
        private const int minGridSize = 2; 
        private const int maxGridSize = 200;
        private bool   showGrid = true; 
        private bool   snapToGrid = true; 
        private Size   gridSize = new Size(8,8);
 
        private bool useSnapLines = false;
        private bool useSmartTags = false;
        private bool objectBoundSmartTagAutoShow = true;
        private bool enableComponentCache = false; 
        private bool enableInSituEditing = true;
 
        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.DesignerOptions"]/*' /> 
        /// <devdoc>
        ///     Creates a new DesignerOptions object. 
        /// </devdoc>
        public DesignerOptions() {
        }
 
        /// <devdoc>
        ///     Public GridSize property. 
        /// </devdoc> 
        [SRCategory(SR.DesignerOptions_LayoutSettings)]
        [SRDescription(SR.DesignerOptions_GridSizeDesc)] 
        public virtual Size GridSize {
            get {
                return gridSize;
            } 
            set {
                //do some validation checking here 
                if (value.Width  < minGridSize) value.Width = minGridSize; 
                if (value.Height < minGridSize) value.Height = minGridSize;
                if (value.Width  > maxGridSize) value.Width = maxGridSize; 
                if (value.Height > maxGridSize) value.Height = maxGridSize;

                gridSize = value;
            } 
        }
 
        /// <devdoc> 
        ///     Public ShowGrid property.
        /// </devdoc> 
        [SRCategory(SR.DesignerOptions_LayoutSettings)]
        [SRDescription(SR.DesignerOptions_ShowGridDesc)]
        public virtual bool ShowGrid {
 
            get {
                return showGrid; 
            } 
            set {
                showGrid = value; 
            }
        }

        /// <devdoc> 
        ///     Public SnapToGrid property.
        /// </devdoc> 
        [SRCategory(SR.DesignerOptions_LayoutSettings)] 
        [SRDescription(SR.DesignerOptions_SnapToGridDesc)]
        public virtual bool SnapToGrid { 
            get {
                return snapToGrid;
            }
            set { 
                snapToGrid = value;
            } 
        } 

        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.UseSnapLines"]/*' /> 
        /// <devdoc>
        ///     This property enables or disables snaplines in the designer.
        /// </devdoc>
        [SRCategory(SR.DesignerOptions_LayoutSettings)] 
        [SRDescription(SR.DesignerOptions_UseSnapLines)]
        public virtual bool UseSnapLines { 
            get { 
                return useSnapLines;
            } 
            set {
                useSnapLines = value;
            }
        } 

        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.UseSmartTags"]/*' /> 
        /// <devdoc> 
        ///     This property enables or disables smart tags in the designer.
        /// </devdoc> 
        [SRCategory(SR.DesignerOptions_LayoutSettings)]
        [SRDescription(SR.DesignerOptions_UseSmartTags)]
        public virtual bool UseSmartTags {
            get { 
                return useSmartTags;
            } 
            set { 
                useSmartTags = value;
            } 
        }

        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.UseSmartTags"]/*' />
        /// <devdoc> 
        ///     This property enables or disables smart tags in the designer.
        /// </devdoc> 
        [SRDisplayName(SR.DesignerOptions_ObjectBoundSmartTagAutoShowDisplayName)] 
        [SRCategory(SR.DesignerOptions_ObjectBoundSmartTagSettings)]
        [SRDescription(SR.DesignerOptions_ObjectBoundSmartTagAutoShow)] 
        public virtual bool ObjectBoundSmartTagAutoShow {
            get {
                return objectBoundSmartTagAutoShow;
            } 
            set {
                objectBoundSmartTagAutoShow = value; 
            } 
        }
 
        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.UseSmartTags"]/*' />
        /// <devdoc>
        ///     This property enables or disables the component cache
        /// </devdoc> 
        [SRDisplayName(SR.DesignerOptions_CodeGenDisplay)]
        [SRCategory(SR.DesignerOptions_CodeGenSettings)] 
        [SRDescription(SR.DesignerOptions_OptimizedCodeGen)] 
        public virtual bool UseOptimizedCodeGeneration {
            get { 
                return enableComponentCache;
            }
            set {
                enableComponentCache = value; 
            }
        } 
 
        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.EnableInsituEditing"]/*' />
        /// <devdoc> 
        ///     This property enables or disables the InSitu Editing for ToolStrips
        /// </devdoc>
        [SRDisplayName(SR.DesignerOptions_EnableInSituEditingDisplay)]
        [SRCategory(SR.DesignerOptions_EnableInSituEditingCat)] 
        [SRDescription(SR.DesignerOptions_EnableInSituEditingDesc)]
        [Browsable(false)] 
        public virtual bool EnableInSituEditing { 
            get {
                return enableInSituEditing; 
            }
            set {
                enableInSituEditing = value;
            } 
        }
 
 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerOptions.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

namespace System.Windows.Forms.Design { 
    using Microsoft.Win32; 
    using System;
    using System.Design; 
    using System.Drawing;
    using System.ComponentModel.Design;
    using System.Collections;
    using System.ComponentModel; 
    using System.Diagnostics;
    using System.Globalization; 
 
    /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions"]/*' />
    /// <devdoc> 
    ///     Provides access to get and set option values for a designer.
    /// </devdoc>
    public class DesignerOptions {
        private const int minGridSize = 2; 
        private const int maxGridSize = 200;
        private bool   showGrid = true; 
        private bool   snapToGrid = true; 
        private Size   gridSize = new Size(8,8);
 
        private bool useSnapLines = false;
        private bool useSmartTags = false;
        private bool objectBoundSmartTagAutoShow = true;
        private bool enableComponentCache = false; 
        private bool enableInSituEditing = true;
 
        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.DesignerOptions"]/*' /> 
        /// <devdoc>
        ///     Creates a new DesignerOptions object. 
        /// </devdoc>
        public DesignerOptions() {
        }
 
        /// <devdoc>
        ///     Public GridSize property. 
        /// </devdoc> 
        [SRCategory(SR.DesignerOptions_LayoutSettings)]
        [SRDescription(SR.DesignerOptions_GridSizeDesc)] 
        public virtual Size GridSize {
            get {
                return gridSize;
            } 
            set {
                //do some validation checking here 
                if (value.Width  < minGridSize) value.Width = minGridSize; 
                if (value.Height < minGridSize) value.Height = minGridSize;
                if (value.Width  > maxGridSize) value.Width = maxGridSize; 
                if (value.Height > maxGridSize) value.Height = maxGridSize;

                gridSize = value;
            } 
        }
 
        /// <devdoc> 
        ///     Public ShowGrid property.
        /// </devdoc> 
        [SRCategory(SR.DesignerOptions_LayoutSettings)]
        [SRDescription(SR.DesignerOptions_ShowGridDesc)]
        public virtual bool ShowGrid {
 
            get {
                return showGrid; 
            } 
            set {
                showGrid = value; 
            }
        }

        /// <devdoc> 
        ///     Public SnapToGrid property.
        /// </devdoc> 
        [SRCategory(SR.DesignerOptions_LayoutSettings)] 
        [SRDescription(SR.DesignerOptions_SnapToGridDesc)]
        public virtual bool SnapToGrid { 
            get {
                return snapToGrid;
            }
            set { 
                snapToGrid = value;
            } 
        } 

        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.UseSnapLines"]/*' /> 
        /// <devdoc>
        ///     This property enables or disables snaplines in the designer.
        /// </devdoc>
        [SRCategory(SR.DesignerOptions_LayoutSettings)] 
        [SRDescription(SR.DesignerOptions_UseSnapLines)]
        public virtual bool UseSnapLines { 
            get { 
                return useSnapLines;
            } 
            set {
                useSnapLines = value;
            }
        } 

        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.UseSmartTags"]/*' /> 
        /// <devdoc> 
        ///     This property enables or disables smart tags in the designer.
        /// </devdoc> 
        [SRCategory(SR.DesignerOptions_LayoutSettings)]
        [SRDescription(SR.DesignerOptions_UseSmartTags)]
        public virtual bool UseSmartTags {
            get { 
                return useSmartTags;
            } 
            set { 
                useSmartTags = value;
            } 
        }

        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.UseSmartTags"]/*' />
        /// <devdoc> 
        ///     This property enables or disables smart tags in the designer.
        /// </devdoc> 
        [SRDisplayName(SR.DesignerOptions_ObjectBoundSmartTagAutoShowDisplayName)] 
        [SRCategory(SR.DesignerOptions_ObjectBoundSmartTagSettings)]
        [SRDescription(SR.DesignerOptions_ObjectBoundSmartTagAutoShow)] 
        public virtual bool ObjectBoundSmartTagAutoShow {
            get {
                return objectBoundSmartTagAutoShow;
            } 
            set {
                objectBoundSmartTagAutoShow = value; 
            } 
        }
 
        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.UseSmartTags"]/*' />
        /// <devdoc>
        ///     This property enables or disables the component cache
        /// </devdoc> 
        [SRDisplayName(SR.DesignerOptions_CodeGenDisplay)]
        [SRCategory(SR.DesignerOptions_CodeGenSettings)] 
        [SRDescription(SR.DesignerOptions_OptimizedCodeGen)] 
        public virtual bool UseOptimizedCodeGeneration {
            get { 
                return enableComponentCache;
            }
            set {
                enableComponentCache = value; 
            }
        } 
 
        /// <include file='doc\DesignerOptions.uex' path='docs/doc[@for="DesignerOptions.EnableInsituEditing"]/*' />
        /// <devdoc> 
        ///     This property enables or disables the InSitu Editing for ToolStrips
        /// </devdoc>
        [SRDisplayName(SR.DesignerOptions_EnableInSituEditingDisplay)]
        [SRCategory(SR.DesignerOptions_EnableInSituEditingCat)] 
        [SRDescription(SR.DesignerOptions_EnableInSituEditingDesc)]
        [Browsable(false)] 
        public virtual bool EnableInSituEditing { 
            get {
                return enableInSituEditing; 
            }
            set {
                enableInSituEditing = value;
            } 
        }
 
 
    }
} 



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
