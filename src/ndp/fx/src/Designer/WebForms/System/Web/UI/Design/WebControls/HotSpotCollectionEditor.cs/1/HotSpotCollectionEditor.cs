//------------------------------------------------------------------------------ 
// <copyright file="HotSpotCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.Design.WebControls {
 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Reflection;
    using System.Web.UI.WebControls;

    /// <include file='doc\HotSpotCollectionEditor.uex' path='docs/doc[@for="HotSpotCollectionEditor"]/*' /> 
    /// <devdoc>
    /// <para>CollectionEditor class for HotSpot</para> 
    /// </devdoc> 
    public class HotSpotCollectionEditor : CollectionEditor {
 
        /// <include file='doc\HotSpotCollectionEditor.uex' path='docs/doc[@for="HotSpotCollectionEditor.HotSpotCollectionEditor"]/*' />
        /// <devdoc>
        /// <para>Default Constructor.</para>
        /// </devdoc> 
        public HotSpotCollectionEditor(Type type) : base(type) {
        } 
 
        /// <include file='doc\HotSpotCollectionEditor.uex' path='docs/doc[@for="HotSpotCollectionEditor.CanSelectMultipleInstances"]/*' />
        /// <devdoc> 
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        protected override bool CanSelectMultipleInstances() {
            return false; 
        }
 
        /// <include file='doc\HotSpotCollectionEditor.uex' path='docs/doc[@for="HotSpotCollectionEditor.CreateNewItemTypes"]/*' /> 
        /// <devdoc>
        /// <para>Defines mutliple types which can be created by the collection editor.</para> 
        /// </devdoc>
        protected override Type[] CreateNewItemTypes() {
            return new Type[] {
                       typeof(CircleHotSpot), 
                       typeof(RectangleHotSpot),
                       typeof(PolygonHotSpot) }; 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.HotSpot.CollectionEditor";
            }
        } 

    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="HotSpotCollectionEditor.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 
namespace System.Web.UI.Design.WebControls {
 
    using System; 
    using System.ComponentModel;
    using System.ComponentModel.Design; 
    using System.Reflection;
    using System.Web.UI.WebControls;

    /// <include file='doc\HotSpotCollectionEditor.uex' path='docs/doc[@for="HotSpotCollectionEditor"]/*' /> 
    /// <devdoc>
    /// <para>CollectionEditor class for HotSpot</para> 
    /// </devdoc> 
    public class HotSpotCollectionEditor : CollectionEditor {
 
        /// <include file='doc\HotSpotCollectionEditor.uex' path='docs/doc[@for="HotSpotCollectionEditor.HotSpotCollectionEditor"]/*' />
        /// <devdoc>
        /// <para>Default Constructor.</para>
        /// </devdoc> 
        public HotSpotCollectionEditor(Type type) : base(type) {
        } 
 
        /// <include file='doc\HotSpotCollectionEditor.uex' path='docs/doc[@for="HotSpotCollectionEditor.CanSelectMultipleInstances"]/*' />
        /// <devdoc> 
        /// <para>[To be supplied.]</para>
        /// </devdoc>
        protected override bool CanSelectMultipleInstances() {
            return false; 
        }
 
        /// <include file='doc\HotSpotCollectionEditor.uex' path='docs/doc[@for="HotSpotCollectionEditor.CreateNewItemTypes"]/*' /> 
        /// <devdoc>
        /// <para>Defines mutliple types which can be created by the collection editor.</para> 
        /// </devdoc>
        protected override Type[] CreateNewItemTypes() {
            return new Type[] {
                       typeof(CircleHotSpot), 
                       typeof(RectangleHotSpot),
                       typeof(PolygonHotSpot) }; 
        } 

        protected override string HelpTopic { 
            get {
                return "net.Asp.HotSpot.CollectionEditor";
            }
        } 

    } 
} 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
