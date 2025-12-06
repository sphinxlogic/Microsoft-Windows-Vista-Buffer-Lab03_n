 
namespace System.Windows.Forms.Design.Behavior {
    using System;
    using System.Collections;
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.Design; 
    using System.Diagnostics; 
    using System.Drawing;
    using System.Windows.Forms.Design; 

    /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine"]/*' />
    /// <devdoc>
    ///     The SnapLine class represents a UI-guideline that will be rendered 
    ///     during control movement (drag, keyboard, and resize) operations.
    ///     SnapLines will assist a user in aligning controls relative to one 
    ///     one another.  Each SnapLine will have a type: top, bottom, etc... 
    ///     Only SnapLines of like-types are allowed to align with each other.
    ///     The 'offset' will represent the distance from the origin (upper-left 
    ///     corner) of the control to where the SnapLine is located.  And finally
    ///     the 'filter' is a string used to define custome types of SnapLines.
    ///     This enables a SnapLine with a filter of "TypeX" to only snap to
    ///     other "TypeX" filtered lines. 
    /// </devdoc>
    public sealed class SnapLine { 
 
        private SnapLineType    type;//the type of SnapLine
        private SnapLinePriority priority;//priority of the line 
        private int             offset;//distance, in pixels, from the origin of the control to the snap location
        private string          filter;//used to specifiy custom SnapLine types

        //These are used in the SnapLine filter to define custom margin/padding SnapLines. 
        //Margins will have special rules of equality, basically opposites will attract one another
        //(ex: margin right == margin left) and paddings will be attracted to like-margins. 
        internal const string Margin = "Margin"; 
        internal const string MarginRight = Margin + ".Right";
        internal const string MarginLeft = Margin + ".Left"; 
        internal const string MarginBottom = Margin + ".Bottom";
        internal const string MarginTop = Margin + ".Top";
        internal const string Padding = "Padding";
        internal const string PaddingRight = Padding + ".Right"; 
        internal const string PaddingLeft = Padding + ".Left";
        internal const string PaddingBottom = Padding + ".Bottom"; 
        internal const string PaddingTop = Padding + ".Top"; 

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLine"]/*' /> 
        /// <devdoc>
        ///     SnapLine constructor that takes the type and offset of SnapLine.
        /// </devdoc>
        public SnapLine(SnapLineType type, int offset) : this(type, offset, null, SnapLinePriority.Low) { 
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLine2"]/*' /> 
        /// <devdoc>
        ///     SnapLine constructor that takes the type, offset and filter of SnapLine. 
        /// </devdoc>
        public SnapLine(SnapLineType type, int offset, string filter) : this(type, offset, filter, SnapLinePriority.Low) {
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLine3"]/*' />
        /// <devdoc> 
        ///     SnapLine constructor that takes the type, offset, and priority of SnapLine. 
        /// </devdoc>
        public SnapLine(SnapLineType type, int offset, SnapLinePriority priority) : this(type, offset, null, priority) { 
        }

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLine4"]/*' />
        /// <devdoc> 
        ///     SnapLine constructor that takes the type, offset, filter, and priority of the SnapLine.
        /// </devdoc> 
        public SnapLine(SnapLineType type, int offset, string filter, SnapLinePriority priority) { 
            this.type = type;
            this.offset = offset; 
            this.filter = filter;
            this.priority = priority;
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.Filter"]/*' />
        /// <devdoc> 
        ///     This property returns a string representing an optional user-defined filter. 
        ///     Setting this filter will allow only those SnapLines with similar filters to align
        ///     to one another. 
        /// </devdoc>
        public string Filter {
            get {
                return filter; 
            }
        } 
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.IsHorizontal"]/*' />
        /// <devdoc> 
        ///     Returns true if the SnapLine is of a horizontal type.
        /// </devdoc>
        public bool IsHorizontal {
            get { 
                return (type == SnapLineType.Top || type == SnapLineType.Bottom ||
                      type == SnapLineType.Horizontal ||type == SnapLineType.Baseline); 
            } 
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.IsVertical"]/*' />
        /// <devdoc>
        ///     Returns true if the SnapLine is of a vertical type.
        /// </devdoc> 
        public bool IsVertical {
            get { 
                return (type == SnapLineType.Left || type == SnapLineType.Right ||type == SnapLineType.Vertical); 
            }
        } 

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.Offset"]/*' />
        /// <devdoc>
        ///     Read-only property that returns the distance from the origin to where this SnapLine is defined. 
        /// </devdoc>
        public int Offset { 
            get { 
                return offset;
            } 
        }

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.Priority"]/*' />
        /// <devdoc> 
        ///     Read-only property that returns the priority of the SnapLine.
        /// </devdoc> 
        public SnapLinePriority Priority { 
            get {
                return priority; 
            }
        }

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLineType"]/*' /> 
        /// <devdoc>
        ///     Read-only property that represents the 'type' of SnapLine. 
        /// </devdoc> 
        public SnapLineType SnapLineType {
            get  { 
                return type;
            }
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.AdjustOffset"]/*' />
        /// <devdoc> 
        ///     Adjusts the offset property of the SnapLine. 
        /// </devdoc>
        public void AdjustOffset(int adjustment) { 
            offset += adjustment;
        }

 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.ShouldSnap"]/*' />
        /// <devdoc> 
        ///     Returns true if SnapLine s1 should snap to SnapLine s2. 
        /// </devdoc>
        public static bool ShouldSnap(SnapLine line1, SnapLine line2) { 
            //types must first be equal
            if (line1.SnapLineType != line2.SnapLineType) {
                return false;
            } 

            //if the filters are both null - then return true 
            if ((line1.Filter == null && line2.Filter == null)) { 
                return true;
            } 

            //at least one filter is non-null so if the other is null
            //then we don't have a match
            if (line1.Filter == null || line2.Filter == null) { 
                return false;
            } 
 
            //check for our special-cased margin filter
            if (line1.Filter.Contains(Margin)) { 
                if (line1.Filter.Equals(MarginRight) && (line2.Filter.Equals(MarginLeft) ||line2.Filter.Equals(PaddingRight)) ||
                  line1.Filter.Equals(MarginLeft) && (line2.Filter.Equals(MarginRight) ||line2.Filter.Equals(PaddingLeft)) ||
                  line1.Filter.Equals(MarginTop) && (line2.Filter.Equals(MarginBottom) ||line2.Filter.Equals(PaddingTop)) ||
                  line1.Filter.Equals(MarginBottom) && (line2.Filter.Equals(MarginTop)) ||line2.Filter.Equals(PaddingBottom)) { 
                    return true;
                } 
                return false; 
            }
 
            //check for padding & margins
            if (line1.Filter.Contains(Padding)) {
                if (line1.Filter.Equals(PaddingLeft) && line2.Filter.Equals(MarginLeft) ||
                  line1.Filter.Equals(PaddingRight) && line2.Filter.Equals(MarginRight) || 
                  line1.Filter.Equals(PaddingTop) && line2.Filter.Equals(MarginTop) ||
                  line1.Filter.Equals(PaddingBottom) && line2.Filter.Equals(MarginBottom)) { 
                  return true; 
                }
                return false; 
            }

            //basic filter equality
            if (line1.Filter.Equals(line2.Filter)) { 
                return true;
            } 
 
            //not equal!
            return false; 
        }

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.ToString"]/*' />
        /// <devdoc> 
        ///     ToString implementation for SnapLines.
        /// </devdoc> 
        public override string ToString() { 
            return "SnapLine: {type = " + type + ", offset  = " + offset + ", priority = " + priority + ", filter = " + (filter == null ? "<null>" : filter) + "}";
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

    /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine"]/*' />
    /// <devdoc>
    ///     The SnapLine class represents a UI-guideline that will be rendered 
    ///     during control movement (drag, keyboard, and resize) operations.
    ///     SnapLines will assist a user in aligning controls relative to one 
    ///     one another.  Each SnapLine will have a type: top, bottom, etc... 
    ///     Only SnapLines of like-types are allowed to align with each other.
    ///     The 'offset' will represent the distance from the origin (upper-left 
    ///     corner) of the control to where the SnapLine is located.  And finally
    ///     the 'filter' is a string used to define custome types of SnapLines.
    ///     This enables a SnapLine with a filter of "TypeX" to only snap to
    ///     other "TypeX" filtered lines. 
    /// </devdoc>
    public sealed class SnapLine { 
 
        private SnapLineType    type;//the type of SnapLine
        private SnapLinePriority priority;//priority of the line 
        private int             offset;//distance, in pixels, from the origin of the control to the snap location
        private string          filter;//used to specifiy custom SnapLine types

        //These are used in the SnapLine filter to define custom margin/padding SnapLines. 
        //Margins will have special rules of equality, basically opposites will attract one another
        //(ex: margin right == margin left) and paddings will be attracted to like-margins. 
        internal const string Margin = "Margin"; 
        internal const string MarginRight = Margin + ".Right";
        internal const string MarginLeft = Margin + ".Left"; 
        internal const string MarginBottom = Margin + ".Bottom";
        internal const string MarginTop = Margin + ".Top";
        internal const string Padding = "Padding";
        internal const string PaddingRight = Padding + ".Right"; 
        internal const string PaddingLeft = Padding + ".Left";
        internal const string PaddingBottom = Padding + ".Bottom"; 
        internal const string PaddingTop = Padding + ".Top"; 

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLine"]/*' /> 
        /// <devdoc>
        ///     SnapLine constructor that takes the type and offset of SnapLine.
        /// </devdoc>
        public SnapLine(SnapLineType type, int offset) : this(type, offset, null, SnapLinePriority.Low) { 
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLine2"]/*' /> 
        /// <devdoc>
        ///     SnapLine constructor that takes the type, offset and filter of SnapLine. 
        /// </devdoc>
        public SnapLine(SnapLineType type, int offset, string filter) : this(type, offset, filter, SnapLinePriority.Low) {
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLine3"]/*' />
        /// <devdoc> 
        ///     SnapLine constructor that takes the type, offset, and priority of SnapLine. 
        /// </devdoc>
        public SnapLine(SnapLineType type, int offset, SnapLinePriority priority) : this(type, offset, null, priority) { 
        }

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLine4"]/*' />
        /// <devdoc> 
        ///     SnapLine constructor that takes the type, offset, filter, and priority of the SnapLine.
        /// </devdoc> 
        public SnapLine(SnapLineType type, int offset, string filter, SnapLinePriority priority) { 
            this.type = type;
            this.offset = offset; 
            this.filter = filter;
            this.priority = priority;
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.Filter"]/*' />
        /// <devdoc> 
        ///     This property returns a string representing an optional user-defined filter. 
        ///     Setting this filter will allow only those SnapLines with similar filters to align
        ///     to one another. 
        /// </devdoc>
        public string Filter {
            get {
                return filter; 
            }
        } 
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.IsHorizontal"]/*' />
        /// <devdoc> 
        ///     Returns true if the SnapLine is of a horizontal type.
        /// </devdoc>
        public bool IsHorizontal {
            get { 
                return (type == SnapLineType.Top || type == SnapLineType.Bottom ||
                      type == SnapLineType.Horizontal ||type == SnapLineType.Baseline); 
            } 
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.IsVertical"]/*' />
        /// <devdoc>
        ///     Returns true if the SnapLine is of a vertical type.
        /// </devdoc> 
        public bool IsVertical {
            get { 
                return (type == SnapLineType.Left || type == SnapLineType.Right ||type == SnapLineType.Vertical); 
            }
        } 

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.Offset"]/*' />
        /// <devdoc>
        ///     Read-only property that returns the distance from the origin to where this SnapLine is defined. 
        /// </devdoc>
        public int Offset { 
            get { 
                return offset;
            } 
        }

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.Priority"]/*' />
        /// <devdoc> 
        ///     Read-only property that returns the priority of the SnapLine.
        /// </devdoc> 
        public SnapLinePriority Priority { 
            get {
                return priority; 
            }
        }

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.SnapLineType"]/*' /> 
        /// <devdoc>
        ///     Read-only property that represents the 'type' of SnapLine. 
        /// </devdoc> 
        public SnapLineType SnapLineType {
            get  { 
                return type;
            }
        }
 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.AdjustOffset"]/*' />
        /// <devdoc> 
        ///     Adjusts the offset property of the SnapLine. 
        /// </devdoc>
        public void AdjustOffset(int adjustment) { 
            offset += adjustment;
        }

 
        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.ShouldSnap"]/*' />
        /// <devdoc> 
        ///     Returns true if SnapLine s1 should snap to SnapLine s2. 
        /// </devdoc>
        public static bool ShouldSnap(SnapLine line1, SnapLine line2) { 
            //types must first be equal
            if (line1.SnapLineType != line2.SnapLineType) {
                return false;
            } 

            //if the filters are both null - then return true 
            if ((line1.Filter == null && line2.Filter == null)) { 
                return true;
            } 

            //at least one filter is non-null so if the other is null
            //then we don't have a match
            if (line1.Filter == null || line2.Filter == null) { 
                return false;
            } 
 
            //check for our special-cased margin filter
            if (line1.Filter.Contains(Margin)) { 
                if (line1.Filter.Equals(MarginRight) && (line2.Filter.Equals(MarginLeft) ||line2.Filter.Equals(PaddingRight)) ||
                  line1.Filter.Equals(MarginLeft) && (line2.Filter.Equals(MarginRight) ||line2.Filter.Equals(PaddingLeft)) ||
                  line1.Filter.Equals(MarginTop) && (line2.Filter.Equals(MarginBottom) ||line2.Filter.Equals(PaddingTop)) ||
                  line1.Filter.Equals(MarginBottom) && (line2.Filter.Equals(MarginTop)) ||line2.Filter.Equals(PaddingBottom)) { 
                    return true;
                } 
                return false; 
            }
 
            //check for padding & margins
            if (line1.Filter.Contains(Padding)) {
                if (line1.Filter.Equals(PaddingLeft) && line2.Filter.Equals(MarginLeft) ||
                  line1.Filter.Equals(PaddingRight) && line2.Filter.Equals(MarginRight) || 
                  line1.Filter.Equals(PaddingTop) && line2.Filter.Equals(MarginTop) ||
                  line1.Filter.Equals(PaddingBottom) && line2.Filter.Equals(MarginBottom)) { 
                  return true; 
                }
                return false; 
            }

            //basic filter equality
            if (line1.Filter.Equals(line2.Filter)) { 
                return true;
            } 
 
            //not equal!
            return false; 
        }

        /// <include file='doc\SnapLine.uex' path='docs/doc[@for="SnapLine.ToString"]/*' />
        /// <devdoc> 
        ///     ToString implementation for SnapLines.
        /// </devdoc> 
        public override string ToString() { 
            return "SnapLine: {type = " + type + ", offset  = " + offset + ", priority = " + priority + ", filter = " + (filter == null ? "<null>" : filter) + "}";
        } 

    }
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
