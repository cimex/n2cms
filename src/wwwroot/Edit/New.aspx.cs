#region License
/* Copyright (C) 2007 Cristian Libardo
 *
 * This is free software; you can redistribute it and/or modify it
 * under the terms of the GNU Lesser General Public License as
 * published by the Free Software Foundation; either version 2.1 of
 * the License, or (at your option) any later version.
 *
 * This software is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
 * Lesser General Public License for more details.
 *
 * You should have received a copy of the GNU Lesser General Public
 * License along with this software; if not, write to the Free
 * Software Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA
 * 02110-1301 USA, or see the FSF site: http://www.fsf.org.
 */
#endregion

using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using System.Reflection;
using N2.Definitions;
using N2.Integrity;

namespace N2.Edit
{
	[NavigationPlugin("New", "new", "../new.aspx?selected={selected}", "preview", "~/edit/img/ico/add.gif", 10, GlobalResourceClassName="Navigation")]
	[ToolbarPlugin("", "new", "new.aspx?selected={selected}", ToolbarArea.Preview, "preview", "~/Edit/Img/Ico/add.gif", 40, ToolTip = "new", GlobalResourceClassName = "Toolbar")]
	public partial class New : Web.EditPage
    {
		ItemDefinition ParentItemDefinition = null;
		protected string ZoneName = null;

		public ContentItem ActualItem
		{
			get
			{
				if(rblPosition.SelectedIndex == 1)
					return base.SelectedItem;
				else
					return base.SelectedItem.Parent;
			}
		}

        protected void Page_Init(object sender, EventArgs e)
        {
			hlCancel.NavigateUrl = SelectedItem.Url;
			if (SelectedItem.Parent == null)
			{
				rblPosition.Enabled = false;
			}
        }

		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);

			ParentItemDefinition = Engine.Definitions.GetDefinition(ActualItem.GetType());
			if (!IsPostBack)
			{
				LoadZones();
			}
			ZoneName = rblZone.SelectedValue;
        }

		protected void rblPosition_OnSelectedIndexChanged(object sender, EventArgs args)
		{
			ParentItemDefinition = Engine.Definitions.GetDefinition(ActualItem.GetType());
			LoadZones();
			ZoneName = rblZone.SelectedValue;
		}

		protected void rblZone_OnSelectedIndexChanged(object sender, EventArgs args)
		{
			ZoneName = rblZone.SelectedValue;
		}

		protected override void OnPreRender(EventArgs e)
		{
			base.OnPreRender(e);
			
			LoadAllowedTypes();
		}

		private void LoadAllowedTypes()
		{
			int allowedChildrenCount = ParentItemDefinition.AllowedChildren.Count;
			IList<ItemDefinition> allowedChildren = Engine.Definitions.GetAllowedChildren(ParentItemDefinition, ZoneName, this.User);

			if (allowedChildrenCount == 0)
			{
				Title = string.Format(GetLocalResourceString("NewPage.Title.NoneAllowed"), ParentItemDefinition.Title);
			}
			else if (allowedChildrenCount == 1 && allowedChildren.Count == 1)
			{
				Response.Redirect(GetEditUrl(allowedChildren[0]));
			}
			else
			{
				Title = string.Format(GetLocalResourceString("NewPage.Title.Select"), ActualItem.Title);

				rptTypes.DataSource = allowedChildren;
				rptTypes.DataBind();
			}
		}

		private void LoadZones()
		{
			string selectedZone = rblZone.SelectedValue;
			ListItem initialItem = rblZone.Items[0];

			rblZone.Items.Clear();
			rblZone.Items.Insert(0, initialItem);
			foreach (AvailableZoneAttribute zone in ParentItemDefinition.AvailableZones)
			{
				string title = GetZoneString(zone.ZoneName) ?? zone.Title;
				rblZone.Items.Add(new ListItem(title, zone.ZoneName));
			}

			string z = IsPostBack ? selectedZone : Request.QueryString["zoneName"];
			if(rblZone.Items.FindByValue(z) != null)
				rblZone.SelectedValue = z;
		}

		protected CreationPosition GetCreationPosition()
		{
			if (rblPosition.SelectedIndex == 0)
				return CreationPosition.Before;
			else if (rblPosition.SelectedIndex == 2)
				return CreationPosition.After;
			else
				return CreationPosition.Below;
				
		}

        protected string GetEditUrl(ItemDefinition definition)
        {
        	return Engine.EditManager.GetEditNewPageUrl(SelectedItem, definition, ZoneName, GetCreationPosition());
        }

		protected string GetDefinitionString(ItemDefinition definition, string key)
		{
			return Utility.GetGlobalResourceString("Definitions", definition.Discriminator + "." + key);
		}

		protected string GetZoneString(string key)
		{
			return Utility.GetGlobalResourceString("Zones", key);
		}
    }
    
}