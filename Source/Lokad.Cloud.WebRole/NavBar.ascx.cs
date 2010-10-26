#region Copyright (c) Lokad 2009
// This code is released under the terms of the new BSD licence.
// URL: http://www.lokad.com/
#endregion

using System.Web.UI;
using System.Web.UI.HtmlControls;

namespace Lokad.Cloud.Web
{
	public partial class NavBar : UserControl
	{
		private string _selected;

		public string Selected
		{
			get { return _selected; }
			set
			{
				Enforce.That(string.IsNullOrEmpty(_selected), "Selected");
				_selected = value;
				((HtmlGenericControl)FindControl(_selected)).Attributes["class"] = "active";
			}
		}
	}
}