//------------------------------------------------------------------------------ 
// <copyright file="ToolStripMenuItemCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ToolStripMenuItemCodeDomSerializer..ctor()")] 
namespace System.Windows.Forms.Design {


    using System; 
    using System.Diagnostics;
    using System.CodeDom; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 


    /// <summary>
    ///  The Reason for having a CustomSerializer for ToolStripMenuItem is the existance of Dummy ToolStripMenuItem for ContextMenuStrips. 
    ///  We add this Dummy ToolStripMenuItem on the "Non Site" ToolStrip to Host the DropDown which facilitates the entry of New MenuItems.
    ///  These items are then added to the ContextMenuStrip that we are designing. 
    ///  But we dont want the Dummy ToolStripMenuItem to Serialize and hence the need for this Custom Serializer. 
    /// </summary>
    internal class ToolStripMenuItemCodeDomSerializer : System.ComponentModel.Design.Serialization.CodeDomSerializer 
    {

        /// <summary>
        /// We implement this for the abstract method on CodeDomSerializer. 
        /// </summary>
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) 
        { 
                return GetBaseSerializer(manager).Deserialize(manager, codeObject);
        } 

        /// <summary>
        /// This is a small helper method that returns the serializer for base Class
        /// </summary> 
        private CodeDomSerializer GetBaseSerializer(IDesignerSerializationManager manager)
        { 
                return (CodeDomSerializer)manager.GetSerializer(typeof(Component), typeof(CodeDomSerializer)); 
        }
 
        /// <include file='doc\ToolStripMenuItemCodeDomSerializer.uex' path='docs/doc[@for="ToolStripMenuItemCodeDomSerializer.Serialize"]/*' />
        /// <devdoc>
        /// We implement this for the abstract method on CodeDomSerializer.  This method
        /// takes an object graph, and serializes the object into CodeDom elements. 
        /// </devdoc>
        public override object Serialize(IDesignerSerializationManager manager, object value) 
        { 
            ToolStripMenuItem item = value as ToolStripMenuItem;
            ToolStrip parent = item.GetCurrentParent() as ToolStrip; 
            //Dont Serialize if we are Dummy Item ...
            if ((item != null) && !(item.IsOnDropDown) && (parent != null) &&  (parent .Site == null))
            {
                //dont serialize anything... 
                return null;
            } 
            else { 

                CodeDomSerializer baseSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(ImageList).BaseType, typeof(CodeDomSerializer)); 
                return baseSerializer.Serialize(manager, value);
            }

        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ToolStripMenuItemCodeDomSerializer.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 

 [assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode", Scope="member", Target="System.Windows.Forms.Design.ToolStripMenuItemCodeDomSerializer..ctor()")] 
namespace System.Windows.Forms.Design {


    using System; 
    using System.Diagnostics;
    using System.CodeDom; 
    using System.ComponentModel; 
    using System.ComponentModel.Design;
    using System.ComponentModel.Design.Serialization; 


    /// <summary>
    ///  The Reason for having a CustomSerializer for ToolStripMenuItem is the existance of Dummy ToolStripMenuItem for ContextMenuStrips. 
    ///  We add this Dummy ToolStripMenuItem on the "Non Site" ToolStrip to Host the DropDown which facilitates the entry of New MenuItems.
    ///  These items are then added to the ContextMenuStrip that we are designing. 
    ///  But we dont want the Dummy ToolStripMenuItem to Serialize and hence the need for this Custom Serializer. 
    /// </summary>
    internal class ToolStripMenuItemCodeDomSerializer : System.ComponentModel.Design.Serialization.CodeDomSerializer 
    {

        /// <summary>
        /// We implement this for the abstract method on CodeDomSerializer. 
        /// </summary>
        public override object Deserialize(IDesignerSerializationManager manager, object codeObject) 
        { 
                return GetBaseSerializer(manager).Deserialize(manager, codeObject);
        } 

        /// <summary>
        /// This is a small helper method that returns the serializer for base Class
        /// </summary> 
        private CodeDomSerializer GetBaseSerializer(IDesignerSerializationManager manager)
        { 
                return (CodeDomSerializer)manager.GetSerializer(typeof(Component), typeof(CodeDomSerializer)); 
        }
 
        /// <include file='doc\ToolStripMenuItemCodeDomSerializer.uex' path='docs/doc[@for="ToolStripMenuItemCodeDomSerializer.Serialize"]/*' />
        /// <devdoc>
        /// We implement this for the abstract method on CodeDomSerializer.  This method
        /// takes an object graph, and serializes the object into CodeDom elements. 
        /// </devdoc>
        public override object Serialize(IDesignerSerializationManager manager, object value) 
        { 
            ToolStripMenuItem item = value as ToolStripMenuItem;
            ToolStrip parent = item.GetCurrentParent() as ToolStrip; 
            //Dont Serialize if we are Dummy Item ...
            if ((item != null) && !(item.IsOnDropDown) && (parent != null) &&  (parent .Site == null))
            {
                //dont serialize anything... 
                return null;
            } 
            else { 

                CodeDomSerializer baseSerializer = (CodeDomSerializer)manager.GetSerializer(typeof(ImageList).BaseType, typeof(CodeDomSerializer)); 
                return baseSerializer.Serialize(manager, value);
            }

        } 
    }
} 
 

// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
