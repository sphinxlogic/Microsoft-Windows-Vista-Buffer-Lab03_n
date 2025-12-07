//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewComponentPropertyGridSite.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System;
    using System.Design;
    using System.CodeDom;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization;
    using System.Diagnostics; 
    using System.Reflection;
    using System.Windows.Forms;

    internal class DataGridViewComponentPropertyGridSite : ISite { 

         private IServiceProvider sp; 
         private IComponent comp; 
         private bool       inGetService = false;
 
         public DataGridViewComponentPropertyGridSite(IServiceProvider sp, IComponent comp) {
             this.sp = sp;
             this.comp = comp;
         } 

         /** The component sited by this component site. */ 
         /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Component"]/*' /> 
         /// <devdoc>
         ///    <para>When implemented by a class, gets the component associated with the <see cref='System.ComponentModel.ISite'/>.</para> 
         /// </devdoc>
         public IComponent Component {get {return comp;}}

         /** The container in which the component is sited. */ 
         /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Container"]/*' />
         /// <devdoc> 
         /// <para>When implemented by a class, gets the container associated with the <see cref='System.ComponentModel.ISite'/>.</para> 
         /// </devdoc>
         public IContainer Container {get {return null;}} 

         /** Indicates whether the component is in design mode. */
         /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.DesignMode"]/*' />
         /// <devdoc> 
         ///    <para>When implemented by a class, determines whether the component is in design mode.</para>
         /// </devdoc> 
         public  bool DesignMode {get {return false;}} 

         /** 
          * The name of the component.
          */
         /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Name"]/*' />
           /// <devdoc> 
           ///    <para>When implemented by a class, gets or sets the name of
           ///       the component associated with the <see cref='System.ComponentModel.ISite'/>.</para> 
           /// </devdoc> 
           public String Name {
                   get {return null;} 
                   set {}
           }

           public object GetService(Type t) { 
               if (!inGetService && sp != null) {
                   try { 
                       inGetService = true; 
                       return sp.GetService(t);
                   } 
                   finally {
                       inGetService = false;
                   }
               } 
               return null;
         } 
     } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DataGridViewComponentPropertyGridSite.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design {
 
    using System;
    using System.Design;
    using System.CodeDom;
    using System.Collections; 
    using System.Collections.Specialized;
    using System.ComponentModel; 
    using System.ComponentModel.Design; 
    using System.ComponentModel.Design.Serialization;
    using System.Diagnostics; 
    using System.Reflection;
    using System.Windows.Forms;

    internal class DataGridViewComponentPropertyGridSite : ISite { 

         private IServiceProvider sp; 
         private IComponent comp; 
         private bool       inGetService = false;
 
         public DataGridViewComponentPropertyGridSite(IServiceProvider sp, IComponent comp) {
             this.sp = sp;
             this.comp = comp;
         } 

         /** The component sited by this component site. */ 
         /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Component"]/*' /> 
         /// <devdoc>
         ///    <para>When implemented by a class, gets the component associated with the <see cref='System.ComponentModel.ISite'/>.</para> 
         /// </devdoc>
         public IComponent Component {get {return comp;}}

         /** The container in which the component is sited. */ 
         /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Container"]/*' />
         /// <devdoc> 
         /// <para>When implemented by a class, gets the container associated with the <see cref='System.ComponentModel.ISite'/>.</para> 
         /// </devdoc>
         public IContainer Container {get {return null;}} 

         /** Indicates whether the component is in design mode. */
         /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.DesignMode"]/*' />
         /// <devdoc> 
         ///    <para>When implemented by a class, determines whether the component is in design mode.</para>
         /// </devdoc> 
         public  bool DesignMode {get {return false;}} 

         /** 
          * The name of the component.
          */
         /// <include file='doc\ISite.uex' path='docs/doc[@for="ISite.Name"]/*' />
           /// <devdoc> 
           ///    <para>When implemented by a class, gets or sets the name of
           ///       the component associated with the <see cref='System.ComponentModel.ISite'/>.</para> 
           /// </devdoc> 
           public String Name {
                   get {return null;} 
                   set {}
           }

           public object GetService(Type t) { 
               if (!inGetService && sp != null) {
                   try { 
                       inGetService = true; 
                       return sp.GetService(t);
                   } 
                   finally {
                       inGetService = false;
                   }
               } 
               return null;
         } 
     } 
}

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
