//------------------------------------------------------------------------------ 
// <copyright file="DesignerVerbToolStripMenuItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
 	using System.Design;
	using System.ComponentModel;
	using System.Diagnostics;
	using System; 
 	using System.Security;
	using System.Security.Permissions; 
 	using System.ComponentModel.Design; 
 	using System.Windows.Forms;
	using System.Drawing; 
 	using System.Drawing.Design;
	using System.Windows.Forms.Design.Behavior;
	using System.Runtime.InteropServices;
	using System.Drawing.Drawing2D; 

 
 	/// <include file='doc\DesignerVerbToolStripMenuItem.uex' path='docs/doc[@for="DesignerVerbToolStripMenuItem"]/*' /> 
	/// <devdoc>
 	///      Associates DesignerVerb with ToolStripMenuItem. 
 	/// </devdoc>
	/// <internalonly/>
 	internal class DesignerVerbToolStripMenuItem : ToolStripMenuItem
	{ 
		DesignerVerb verb;
 
        // Text is a virtual method on the base class, but since we don't override it we should be okay. 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
		public DesignerVerbToolStripMenuItem(DesignerVerb verb) 
 		{


			this.verb = verb; 
 			this.Text = verb.Text;
 
 			RefreshItem(); 

		} 

 		public void RefreshItem()
		{
			if (verb != null) 
			{
 				this.Visible = verb.Visible; 
				this.Enabled = verb.Enabled; 
 				this.Checked = verb.Checked;
 			} 
		}

 		protected override void OnClick(System.EventArgs e)
		{ 
			if (verb != null)
			{ 
 				verb.Invoke(); 
			}
 		} 
 	}
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="DesignerVerbToolStripMenuItem.cs" company="Microsoft">
//     Copyright (c) Microsoft Corporation.  All rights reserved.
// </copyright>
//----------------------------------------------------------------------------- 

/* 
 */ 
namespace System.Windows.Forms.Design
{ 
 	using System.Design;
	using System.ComponentModel;
	using System.Diagnostics;
	using System; 
 	using System.Security;
	using System.Security.Permissions; 
 	using System.ComponentModel.Design; 
 	using System.Windows.Forms;
	using System.Drawing; 
 	using System.Drawing.Design;
	using System.Windows.Forms.Design.Behavior;
	using System.Runtime.InteropServices;
	using System.Drawing.Drawing2D; 

 
 	/// <include file='doc\DesignerVerbToolStripMenuItem.uex' path='docs/doc[@for="DesignerVerbToolStripMenuItem"]/*' /> 
	/// <devdoc>
 	///      Associates DesignerVerb with ToolStripMenuItem. 
 	/// </devdoc>
	/// <internalonly/>
 	internal class DesignerVerbToolStripMenuItem : ToolStripMenuItem
	{ 
		DesignerVerb verb;
 
        // Text is a virtual method on the base class, but since we don't override it we should be okay. 
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2214:DoNotCallOverridableMethodsInConstructors")]
		public DesignerVerbToolStripMenuItem(DesignerVerb verb) 
 		{


			this.verb = verb; 
 			this.Text = verb.Text;
 
 			RefreshItem(); 

		} 

 		public void RefreshItem()
		{
			if (verb != null) 
			{
 				this.Visible = verb.Visible; 
				this.Enabled = verb.Enabled; 
 				this.Checked = verb.Checked;
 			} 
		}

 		protected override void OnClick(System.EventArgs e)
		{ 
			if (verb != null)
			{ 
 				verb.Invoke(); 
			}
 		} 
 	}
}


// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
