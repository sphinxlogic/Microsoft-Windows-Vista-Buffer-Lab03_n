namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner"]/*' />
    /// <devdoc>
    ///     An Adorner manages a collection of UI-related Glyphs.  Each Adorner
    ///     can be enabled/disabled.  Only Enabled Adorners will receive hit test 
    ///     and paint messages from the BehaviorService.  An Adorner can be viewed
    ///     as a proxy between UI-related elements (all Glyphs) and the BehaviorService. 
    /// </devdoc> 
    public sealed class Adorner {
 
        private BehaviorService     behaviorService;//ptr back to the BehaviorService
        private GlyphCollection     glyphs;//collection of Glyphs that this particular Adorner manages
        private bool                enabled;//enabled value - determines if Adorner gets paints & hits
 
        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Adorner"]/*' />
        /// <devdoc> 
        ///     Standard constructor.  Creates a new GlyphCollection and by default is enabled. 
        /// </devdoc>
        public Adorner() { 
            glyphs = new GlyphCollection();
            enabled = true;
        }
 
        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.BehaviorService"]/*' />
        /// <devdoc> 
        ///     When an Adorner is added to the BehaviorService's AdornerCollection, the collection 
        ///     will set this property so that the Adorner can call back to the BehaviorService.
        /// </devdoc> 
        public BehaviorService BehaviorService {
            get {
                return behaviorService;
            } 
            set {
                this.behaviorService = value; 
            } 
        }
 
        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Enabled"]/*' />
        /// <devdoc>
        ///     Determines if the BehaviorService will send HitTest and Paint messages to
        ///     the Adorner. 
        /// </devdoc>
        public bool Enabled { 
            get { 
                return enabled;
            } 
            set {
                if (value != enabled) {
                    enabled = value;
                    Invalidate(); 
                }
            } 
        } 

        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Glyphs"]/*' /> 
        /// <devdoc>
        ///     Returns the stronly-typed Glyph collection.
        /// </devdoc>
        public GlyphCollection Glyphs { 
            get {
                return glyphs; 
            } 
        }
 
        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Invalidate"]/*' />
        /// <devdoc>
        ///     Forces the BehaviorService to refresh its AdornerWindow.
        /// </devdoc> 
        public void Invalidate() {
            if (behaviorService != null) { 
                behaviorService.Invalidate(); 
            }
        } 

        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Invalidate2"]/*' />
        /// <devdoc>
        ///     Forces the BehaviorService to refresh its AdornerWindow within the given Rectangle. 
        /// </devdoc>
        public void Invalidate(Rectangle rectangle) { 
            if (behaviorService != null) { 
                behaviorService.Invalidate(rectangle);
            } 
        }

        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Invalidate3"]/*' />
        /// <devdoc> 
        ///     Forces the BehaviorService to refresh its AdornerWindow within the given Region.
        /// </devdoc> 
        public void Invalidate(Region region)  { 
            if (behaviorService != null) {
                behaviorService.Invalidate(region); 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
namespace System.Windows.Forms.Design.Behavior { 
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Design;
    using System.Diagnostics; 
    using System.Drawing; 
    using System.Windows.Forms.Design;
 
    /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner"]/*' />
    /// <devdoc>
    ///     An Adorner manages a collection of UI-related Glyphs.  Each Adorner
    ///     can be enabled/disabled.  Only Enabled Adorners will receive hit test 
    ///     and paint messages from the BehaviorService.  An Adorner can be viewed
    ///     as a proxy between UI-related elements (all Glyphs) and the BehaviorService. 
    /// </devdoc> 
    public sealed class Adorner {
 
        private BehaviorService     behaviorService;//ptr back to the BehaviorService
        private GlyphCollection     glyphs;//collection of Glyphs that this particular Adorner manages
        private bool                enabled;//enabled value - determines if Adorner gets paints & hits
 
        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Adorner"]/*' />
        /// <devdoc> 
        ///     Standard constructor.  Creates a new GlyphCollection and by default is enabled. 
        /// </devdoc>
        public Adorner() { 
            glyphs = new GlyphCollection();
            enabled = true;
        }
 
        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.BehaviorService"]/*' />
        /// <devdoc> 
        ///     When an Adorner is added to the BehaviorService's AdornerCollection, the collection 
        ///     will set this property so that the Adorner can call back to the BehaviorService.
        /// </devdoc> 
        public BehaviorService BehaviorService {
            get {
                return behaviorService;
            } 
            set {
                this.behaviorService = value; 
            } 
        }
 
        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Enabled"]/*' />
        /// <devdoc>
        ///     Determines if the BehaviorService will send HitTest and Paint messages to
        ///     the Adorner. 
        /// </devdoc>
        public bool Enabled { 
            get { 
                return enabled;
            } 
            set {
                if (value != enabled) {
                    enabled = value;
                    Invalidate(); 
                }
            } 
        } 

        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Glyphs"]/*' /> 
        /// <devdoc>
        ///     Returns the stronly-typed Glyph collection.
        /// </devdoc>
        public GlyphCollection Glyphs { 
            get {
                return glyphs; 
            } 
        }
 
        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Invalidate"]/*' />
        /// <devdoc>
        ///     Forces the BehaviorService to refresh its AdornerWindow.
        /// </devdoc> 
        public void Invalidate() {
            if (behaviorService != null) { 
                behaviorService.Invalidate(); 
            }
        } 

        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Invalidate2"]/*' />
        /// <devdoc>
        ///     Forces the BehaviorService to refresh its AdornerWindow within the given Rectangle. 
        /// </devdoc>
        public void Invalidate(Rectangle rectangle) { 
            if (behaviorService != null) { 
                behaviorService.Invalidate(rectangle);
            } 
        }

        /// <include file='doc\Adorner.uex' path='docs/doc[@for="Adorner.Invalidate3"]/*' />
        /// <devdoc> 
        ///     Forces the BehaviorService to refresh its AdornerWindow within the given Region.
        /// </devdoc> 
        public void Invalidate(Region region)  { 
            if (behaviorService != null) {
                behaviorService.Invalidate(region); 
            }
        }
    }
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
