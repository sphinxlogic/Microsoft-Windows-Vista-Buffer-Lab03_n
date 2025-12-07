//------------------------------------------------------------------------------ 
// <copyright file="ItemTypeToolStripMenuItem.cs" company="Microsoft">
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

 
 	/// <devdoc> 
	///      Associates Type with ToolStripMenuItem.
 	/// </devdoc> 
 	/// <internalonly/>
	internal class ItemTypeToolStripMenuItem : ToolStripMenuItem
 	{
		private static string systemWindowsFormsNamespace = typeof(System.Windows.Forms.ToolStripItem).Namespace; 
		private static ToolboxItem invalidToolboxItem = new ToolboxItem();
		private Type _itemType; 
 		private bool convertTo = false; 
		private ToolboxItem tbxItem = invalidToolboxItem;
 		private Image _image = null; 

 		public ItemTypeToolStripMenuItem(Type t)
		{
 			this._itemType = t; 
		}
 
		public Type ItemType 
		{
 			get 
			{
 				return _itemType;
 			}
		} 

 
 		public bool ConvertTo 
		{
			get 
			{
 				return convertTo;
			}
 			set 
 			{
				convertTo = value; 
 			} 
		}
 
		public override Image Image
		{
 			get
			{ 
 				if (_image == null)
 				{ 
					_image = ToolStripDesignerUtils.GetToolboxBitmap(ItemType); 
 				}
				return _image; 
			}
			set
 			{
			} 
 		}
 
 		public override string Text 
		{
 			get 
			{
				return ToolStripDesignerUtils.GetToolboxDescription(ItemType);
			}
 			set 
			{
 			} 
 		} 

		protected override void Dispose(bool disposing) 
 		{
			if (disposing)
			{
				tbxItem = null; 
 			}
			base.Dispose(disposing); 
 		} 

 	} 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------------------------ 
// <copyright file="ItemTypeToolStripMenuItem.cs" company="Microsoft">
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

 
 	/// <devdoc> 
	///      Associates Type with ToolStripMenuItem.
 	/// </devdoc> 
 	/// <internalonly/>
	internal class ItemTypeToolStripMenuItem : ToolStripMenuItem
 	{
		private static string systemWindowsFormsNamespace = typeof(System.Windows.Forms.ToolStripItem).Namespace; 
		private static ToolboxItem invalidToolboxItem = new ToolboxItem();
		private Type _itemType; 
 		private bool convertTo = false; 
		private ToolboxItem tbxItem = invalidToolboxItem;
 		private Image _image = null; 

 		public ItemTypeToolStripMenuItem(Type t)
		{
 			this._itemType = t; 
		}
 
		public Type ItemType 
		{
 			get 
			{
 				return _itemType;
 			}
		} 

 
 		public bool ConvertTo 
		{
			get 
			{
 				return convertTo;
			}
 			set 
 			{
				convertTo = value; 
 			} 
		}
 
		public override Image Image
		{
 			get
			{ 
 				if (_image == null)
 				{ 
					_image = ToolStripDesignerUtils.GetToolboxBitmap(ItemType); 
 				}
				return _image; 
			}
			set
 			{
			} 
 		}
 
 		public override string Text 
		{
 			get 
			{
				return ToolStripDesignerUtils.GetToolboxDescription(ItemType);
			}
 			set 
			{
 			} 
 		} 

		protected override void Dispose(bool disposing) 
 		{
			if (disposing)
			{
				tbxItem = null; 
 			}
			base.Dispose(disposing); 
 		} 

 	} 
}



// File provided for Reference Use Only by Microsoft Corporation (c) 2007.
// Copyright (c) Microsoft Corporation. All rights reserved.
