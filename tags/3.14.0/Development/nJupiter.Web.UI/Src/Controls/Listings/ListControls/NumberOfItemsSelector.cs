#region Copyright & License
/*
	Copyright (c) 2005-2010 nJupiter

	Permission is hereby granted, free of charge, to any person obtaining a copy
	of this software and associated documentation files (the "Software"), to deal
	in the Software without restriction, including without limitation the rights
	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the Software is
	furnished to do so, subject to the following conditions:

	The above copyright notice and this permission notice shall be included in
	all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
	THE SOFTWARE.
*/
#endregion

using System;
using System.Globalization;
using System.Web.UI.WebControls;

namespace nJupiter.Web.UI.Controls.Listings {

	public class NumberOfItemsSelector : WebDropDownList {
		#region Constants
#if DEBUG
		private const string	DebugPrefix		= "_";
#else
		private const string	DebugPrefix		= "";
#endif
		private const string	DefaultAllitemtext	= DebugPrefix + "All";

		private const int		AllitemValue		= int.MaxValue;
		#endregion

		#region Members
		private bool	includeAllItem;
		private string	allItemText		= DefaultAllitemtext;
		#endregion

		#region Properties
		public bool		IncludeAllItem	{ get { return this.includeAllItem; }	set { this.includeAllItem = value; } }
		public string	AllItemText		{ get { return this.allItemText; }		set { this.allItemText = value; } }
		#endregion

		#region Overridden Methods
		protected override void OnInit(EventArgs e) {
			this.Items.Add(10.ToString(NumberFormatInfo.CurrentInfo));
			this.Items.Add(25.ToString(NumberFormatInfo.CurrentInfo));
			this.Items.Add(50.ToString(NumberFormatInfo.CurrentInfo));
			AddAllItem();
			base.OnInit(e);
		}
		protected override void OnPreRender(EventArgs e) {
			base.OnPreRender(e);
			ListItem allItem = this.Items.FindByValue(AllitemValue.ToString(NumberFormatInfo.CurrentInfo));
			if(allItem != null) {
				if(this.IncludeAllItem) {
					allItem.Text = this.AllItemText;
				} else {
					this.Items.Remove(allItem);
				}
			} else if(this.IncludeAllItem) {
				AddAllItem();
			}
		}

		private void AddAllItem() {
			this.Items.Add(new ListItem(this.AllItemText, AllitemValue.ToString(NumberFormatInfo.CurrentInfo)));
		}
		#endregion
	}
}
