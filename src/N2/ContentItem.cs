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
using System.Diagnostics;
using System.Reflection;
using System.Security.Principal;
using System.Web;
using System.Web.UI;
using N2.Definitions;
using N2.Integrity;
using N2UI = N2.Web.UI;
using System.Text;

namespace N2
{
    /// <summary>
    /// The base of N2 content items. All content pages and data items are 
    /// derived from this item. During initialization the CMS looks for classes
    /// deriving from <see cref="ContentItem"/> and makes them available for
    /// editing and storage in the database.
    /// </summary>
    /// <example>
    /// // Since the class is inheriting <see cref="ContentItem"/> it's 
    /// // recognized by the CMS and made available for editing.
    /// public class MyPage : N2.ContentItem
    /// {
    ///		public override string TemplateUrl
    ///		{
    ///			get { return "~/Path/To/My/Template.aspx"; }
    ///		}
    /// }
    /// </example>
	[Serializable, RestrictParents(typeof(ContentItem)), DebuggerDisplay("{Name} [{ID}]")]
	public abstract class ContentItem : IComparable, IComparable<ContentItem>, ICloneable, Web.IUrlParserDependency, IContainable, INode
    {
        #region Private Fields
        [Persistence.DoNotCopy]
		private int id;
        private string title;
        private string name;
        private string zoneName;
		[Persistence.DoNotCopy]
		private ContentItem parent = null;
        private DateTime created;
        private DateTime updated;
        private DateTime? published = DateTime.Now;
        private DateTime? expires = null;
        private int sortOrder;
		[Persistence.DoNotCopy]
		private string url = null;
        private bool visible = true;
		[Persistence.DoNotCopy]
		private ContentItem versionOf = null;
		private string savedBy;
		[Persistence.DoNotCopy]
		private IList<Security.AuthorizedRole> authorizedRoles = null;
		[Persistence.DoNotCopy]
		private IList<ContentItem> children = new List<ContentItem>();
		[Persistence.DoNotCopy]
		private IDictionary<string, Details.ContentDetail> details = new Dictionary<string, Details.ContentDetail>();
		[Persistence.DoNotCopy]
		private IDictionary<string, Details.DetailCollection> detailCollections = new Dictionary<string, Details.DetailCollection>();
		private Web.IUrlParser urlParser;
        #endregion

        #region Constructor
        /// <summary>Creates a new instance of the ContentItem.</summary>
		public ContentItem()
        {
            created = DateTime.Now;
            updated = DateTime.Now;
            published = DateTime.Now;
        }
        #endregion

		#region Statics

		static string defaultExtension = ".aspx";
		public static string DefaultExtension
		{
			get { return defaultExtension; }
			set { defaultExtension = value; }
		}

		#endregion

		#region Public Properties (persisted)
		/// <summary>Gets or sets item ID.</summary>
		public virtual int ID
		{
			get { return id; }
			set { id = value; }
		}

		/// <summary>Gets or sets this item's parent. This can be null for root items and previous versions but should be another page in other situations.</summary>
		public virtual ContentItem Parent
		{
			get { return parent; }
			set { parent = value; }
		}

		/// <summary>Gets or sets the item's title. This is used in edit mode and probably in a custom implementation.</summary>
		[Details.Displayable(typeof(Web.UI.WebControls.HN), "Text")]
		public virtual string Title
		{
			get { return title; }
			set { title = value; }
		}

		/// <summary>Gets or sets the item's name. This is used to compute the item's url and can be used to uniquely identify the item among other items on the same level.</summary>
		public virtual string Name
		{
			get 
			{ 
				return name 
					?? (ID > 0 ? ID.ToString() : string.Empty); 
			}
			set 
			{ 
				name = value; 
				url = null;  
			}
		}

		/// <summary>Gets or sets zone name which is associated with data items and their placement on a page.</summary>
		public virtual string ZoneName
		{
			get { return zoneName; }
			set { zoneName = value; }
		}

		/// <summary>Gets or sets when this item was initially created.</summary>
		public virtual DateTime Created
		{
			get { return created; }
			set { created = value; }
		}

		/// <summary>Gets or sets the date this item was updated.</summary>
		public virtual DateTime Updated
		{
			get { return updated; }
			set { updated = value; }
		}

		/// <summary>Gets or sets the publish date of this item.</summary>
		public virtual DateTime? Published
		{
			get { return published; }
			set { published = value; }
		}

		/// <summary>Gets or sets the expiration date of this item.</summary>
		public virtual DateTime? Expires
		{
			get { return expires; }
			set { expires = value != DateTime.MinValue ? value : null; }
		}

		/// <summary>Gets or sets the sort order of this item.</summary>
		public virtual int SortOrder
		{
			get { return sortOrder; }
			set { sortOrder = value; }
		}

		/// <summary>Gets or sets whether this item is visible. This is normally used to control it's visibility in the site map provider.</summary>
		public virtual bool Visible
		{
			get { return visible; }
			set { visible = value; }
		}

		/// <summary>Gets or sets the published version of this item. If this value is not null then this item is a previous version of the item specified by VersionOf.</summary>
		public virtual ContentItem VersionOf
		{
			get { return versionOf; }
			set { versionOf = value; }
		}

		/// <summary>Gets or sets the name of the identity who saved this item.</summary>
		public virtual string SavedBy
		{
			get { return savedBy; }
			set { savedBy = value; }
		}

		/// <summary>Gets or sets the details collection. These are usually accessed using the e.g. item["Detailname"]. This is a place to store content data.</summary>
		public virtual IDictionary<string, Details.ContentDetail> Details
		{
			get { return details; }
			set { details = value; }
		}

		/// <summary>Gets or sets the details collection collection. These are details grouped into a collection.</summary>
		public virtual IDictionary<string, Details.DetailCollection> DetailCollections
		{
			get { return detailCollections; }
			set { detailCollections = value; }
		}

		/// <summary>Gets or sets all a collection of child items of this item ignoring permissions. If you want the children the current user has permission to use <see cref="GetChildren()"/> instead.</summary>
		public virtual IList<ContentItem> Children
		{
			get { return children; }
			set { children = value; }
		}
		#endregion

		#region Public Properties (generated)
		/// <summary>Gets whether this item is a page. This is used for and site map purposes.</summary>
		public virtual bool IsPage
		{
			get { return true; }
		}

		/// <summary>Gets the public url to this item. This is computed by walking the parent path and prepending their names to the url.</summary>
		public virtual string Url
		{
			get 
			{ 
				return url ?? (url = 
					(urlParser != null && VersionOf == null) 
						? urlParser.BuildUrl(this) 
						: RewrittenUrl); 
			}
		}

		/// <summary>Gets the template that handle the presentation of this content item. For non page items (IsPage) this can be a user control (ascx).</summary>
        public virtual string TemplateUrl
        {
            get { return "~/default.aspx"; }
        }

		/// <summary>Gets the icon of this item. This can be used to distinguish item types in edit mode.</summary>
		public virtual string IconUrl
        {
			get { return Utility.ToAbsolute("~/edit/img/ico/" + (IsPage ? "page.gif" : "page_white.gif")); }
        }

		/// <summary>Gets the non-friendly url to this item (e.g. "/default.aspx?page=1"). This is used to uniquely identify this item. Non-page items have two query string properties; page and item (e.g. "/default.aspx?page=1&amp;item&#61;27").</summary>
        public virtual string RewrittenUrl
        {
            get
            {
                if (IsPage)
                {
                    return Utility.ToAbsolute(TemplateUrl) + "?page=" + ID;
                }

                for (ContentItem ancestorItem = Parent; ancestorItem != null; ancestorItem = ancestorItem.Parent)
                    if (ancestorItem.IsPage)
                        return Utility.ToAbsolute(ancestorItem.TemplateUrl) + string.Format("?page={0}&item={1}", ancestorItem.ID, ID);

				if (VersionOf != null)
					return VersionOf.TemplateUrl;

                throw new Web.TemplateNotFoundException(this);
            }
		}
		#endregion

		#region Security
		/// <summary>Gets an array of roles allowed to read this item. Null or empty list is interpreted as this item has no access restrictions (anyone may read).</summary>
		public virtual IList<Security.AuthorizedRole> AuthorizedRoles
		{
			get 
			{
				if (authorizedRoles == null)
					authorizedRoles = new List<Security.AuthorizedRole>();
				return authorizedRoles; 
			}
			set { authorizedRoles = value; }
		}
		#endregion

		#region Equals, HashCode and ToString Overrides
		/// <summary>Checks the item with another for equality.</summary>
		/// <returns>True if two items have the same ID.</returns>
		public override bool Equals( object obj )
		{
			if( this == obj ) return true;
			if( ( obj == null ) || ( obj.GetType() != GetType() ) ) return false;
			ContentItem item = obj as ContentItem;
			if (ID != 0 && item.ID != 0)
				return ID == item.ID;
			else
				return ReferenceEquals(this, item);
		}

		/// <summary>Gets a hash code based on the item's id.</summary>
		/// <returns>A hash code.</returns>
		public override int GetHashCode()
		{
			return id.GetHashCode(); 
		}

		/// <summary>Returns this item's name.</summary>
		/// <returns>The item's name.</returns>
        public override string ToString()
        {
            return Name ?? string.Empty;
        }
		#endregion

        #region this[]

		/// <summary>Gets or sets the detail or property with the supplied name. If a property with the supplied name exists this is always returned in favour of any detail that might have the same name.</summary>
		/// <param name="detailName">The name of the propery or detail.</param>
		/// <returns>The value of the property or detail. If now property exists null is returned.</returns>
		public virtual object this[string detailName]
        {
            get
            {
				if (detailName == null)
					throw new ArgumentNullException("detailName");

                switch (detailName)
                {
                    case "ID":
                        return ID;
                    case "Title":
                        return Title;
                    case "Name":
                        return Name;
                    case "Url":
                        return Url;
                    case "TemplateUrl":
                        return TemplateUrl;
                    default:
						return Utility.Evaluate(this, detailName)
							?? GetDetail(detailName)
							?? GetDetailCollection(detailName, false);
                }
            }
            set 
            {
                if (string.IsNullOrEmpty(detailName))
					throw new ArgumentNullException("Parameter 'detailName' cannot be null or empty.", "detailName");

                PropertyInfo info = GetType().GetProperty(detailName);
				if (info != null && info.CanWrite)
				{
					if (value != null && info.PropertyType != value.GetType())
						value = Utility.Convert(value, info.PropertyType);
					info.SetValue(this, value, null);
				}
				else if (value is Details.DetailCollection)
					throw new N2Exception("Cannot set a detail collection this way, add it to the DetailCollections collection instead.");
				else
				{
					SetDetail(detailName, value);
				}       
            }
        }
        #endregion

		#region GetDetail & SetDetail<T> Methods
		/// <summary>Gets a detail from the details bag.</summary>
		/// <param name="detailName">The name of the value to get.</param>
		/// <returns>The value stored in the details bag or null if no item was found.</returns>
		public virtual object GetDetail(string detailName)
		{
			return Details.ContainsKey(detailName)
				? Details[detailName].Value
				: null;
		}

		/// <summary>Set a value into the <see cref="Details"/> bag. If a value with the same name already exists it is overwritten. If the value equals the default value it will be removed from the details bag.</summary>
		/// <param name="detailName">The name of the item to set.</param>
		/// <param name="value">The value to set. If this parameter is null or equal to defaultValue the detail is removed.</param>
		/// <param name="defaultValue">The default value. If the value is equal to this value the detail will be removed.</param>
		protected virtual void SetDetail<T>(string detailName, T value, T defaultValue)
		{
			if (value == null || !value.Equals(defaultValue))
			{
				SetDetail<T>(detailName, value);
			}
			else if (Details.ContainsKey(detailName))
			{
				details.Remove(detailName);
			}
		}

		/// <summary>Set a value into the <see cref="Details"/> bag. If a value with the same name already exists it is overwritten.</summary>
		/// <param name="detailName">The name of the item to set.</param>
		/// <param name="value">The value to set. If this parameter is null the detail is removed.</param>
		protected virtual void SetDetail<T>(string detailName, T value)
		{
			Details.ContentDetail detail = Details.ContainsKey(detailName) ? Details[detailName] : null;

			if (detail != null && value != null && typeof(T).IsAssignableFrom(detail.ValueType))
			{
				// update an existing detail
				detail.Value = value;
			}
			else
			{
				if (detail != null)
					// delete detail or remove detail of wrong type
					Details.Remove(detailName);
				if (value != null)
					// add new detail
					Details.Add(detailName, N2.Details.ContentDetail.New(this, detailName, value));
			}
		} 
		#endregion

		#region GetDetailCollection
		/// <summary>Gets a named detail collection.</summary>
		/// <param name="collectionName">The name of the detail collection to get.</param>
		/// <param name="createWhenEmpty">Wether a new collection should be created if none exists. Setting this to false means null will be returned if no collection exists.</param>
		/// <returns>A new or existing detail collection or null if the createWhenEmpty parameter is false and no collection with the given name exists..</returns>
		public virtual Details.DetailCollection GetDetailCollection(string collectionName, bool createWhenEmpty)
		{
			if (DetailCollections.ContainsKey(collectionName))
				return DetailCollections[collectionName];
			else if (createWhenEmpty)
			{
				Details.DetailCollection collection = new Details.DetailCollection(this, collectionName);
				DetailCollections.Add(collectionName, collection);
				return collection;
			}
			else
				return null;
		}
		#endregion

		#region AddTo & GetChild & GetChildren

		private const int SortOrderTreshold = 9999;

		/// <summary>Adds an item to the children of this item updating it's parent refernce.</summary>
		/// <param name="newParent">The new parent of the item. If this parameter is null the item is detached from the hierarchical structure.</param>
		public virtual void AddTo(ContentItem newParent)
		{
			if (Parent != null && Parent != newParent && Parent.Children.Contains(this))
				Parent.Children.Remove(this);
			
			Parent = newParent;
			
			if (newParent != null && !newParent.Children.Contains(this))
			{
				IList<ContentItem> siblings = newParent.Children;
				if (siblings.Count > 0)
				{
					int lastOrder = siblings[siblings.Count - 1].SortOrder;

					for (int i = siblings.Count - 2; i >= 0; i--)
					{
						if (siblings[i].SortOrder < lastOrder - SortOrderTreshold)
						{
							siblings.Insert(i + 1, this);
							return;
						}
						lastOrder = siblings[i].SortOrder;
					}

					if (lastOrder > SortOrderTreshold)
					{
						siblings.Insert(0, this);
						return;
					}
				}

				siblings.Add(this);
			}
		}

		/// <summary>Tries to get a child item with a given name. This method igonres user permissions and any trailing '.aspx' that might be part of the name.</summary>
		/// <param name="childName">The name of the child item to get.</param>
		/// <returns>The child item if it is found otherwise null.</returns>
		/// <remarks>If the method is passed an empty or null string it will return itself.</remarks>
		public virtual ContentItem GetChild(string childName)
        {
			if (string.IsNullOrEmpty(childName))
				return null;

			int slashIndex = childName.IndexOf('/');
			if (slashIndex == 0) // starts with slash
			{
				if (childName.Length == 1)
					return this;
				else
					return GetChild(childName.Substring(1));
			}
			else if (slashIndex > 0) // contains a slash further down
			{
				ContentItem child = FindChild(childName.Substring(0, slashIndex));
				if (child != null)
					return child.GetChild(childName.Substring(slashIndex));
				else
					return null;
			}
			else // no slash, only a name
			{
				return FindChild(childName);
			}
        }

		private ContentItem FindChild(string childName)
		{
			foreach (ContentItem child in Children)
			{
				if (string.Equals(childName, child.Name, StringComparison.InvariantCultureIgnoreCase))
					return child;
			}
			return null;
		}

		/// <summary>Gets child items the current user is allowed to access.</summary>
		/// <returns>A list of content items.</returns>
		/// <remarks>This method is used by N2 for site map providers, and for data source controls. Keep this in mind when overriding this method.</remarks>
		public virtual Collections.ItemList GetChildren()
		{
			return GetChildren(new Collections.AccessFilter());
		}

		/// <summary>Gets children the current user is allowed to access belonging to a certain zone, i.e. get only children with a certain zone name. </summary>
		/// <param name="childZoneName">The name of the zone.</param>
		/// <returns>A list of items that have the specified zone name.</returns>
		/// <remarks>This method is used by N2 when when non-page items are added to a zone on a page and in edit mode when displaying which items are placed in a certain zone. Keep this in mind when overriding this method.</remarks>
        public virtual Collections.ItemList GetChildren(string childZoneName)
        {
			return GetChildren(new Collections.ZoneFilter(childZoneName), 
				new Collections.AccessFilter());
        }

		/// <summary>Gets children applying filters.</summary>
		/// <param name="filters">The filters to apply on the children.</param>
		/// <returns>A list of filtered child items.</returns>
		public virtual Collections.ItemList GetChildren(params Collections.ItemFilter[] filters)
		{
			return GetChildren(filters as IEnumerable<Collections.ItemFilter>);
		}

		/// <summary>Gets children applying filters.</summary>
		/// <param name="filters">The filters to apply on the children.</param>
		/// <returns>A list of filtered child items.</returns>
		public virtual Collections.ItemList GetChildren(IEnumerable<Collections.ItemFilter> filters)
		{
			IEnumerable<ContentItem> items = VersionOf == null ? Children : VersionOf.Children;
			return new Collections.ItemList(items, filters);
		}

		#endregion

		#region IComparable & IComparable<ContentItem> Members

		int IComparable.CompareTo(object obj)
		{
			if (obj is ContentItem)
				return SortOrder - ((ContentItem)obj).SortOrder;
			else
				return 0;
		}
		int IComparable<ContentItem>.CompareTo(ContentItem other)
        {
            return SortOrder - other.SortOrder;
        }

        #endregion

        #region ICloneable Members

		object ICloneable.Clone()
		{
			return Clone(true);
		}
		
		/// <summary>Creats a copy of this item including details and authorized roles resetting ID.</summary>
		/// <param name="includeChildren">Wether this item's child items also should be cloned.</param>
		/// <returns>The cloned item with or without cloned child items.</returns>
		public virtual ContentItem Clone(bool includeChildren)
        {
			ContentItem cloned = (ContentItem)MemberwiseClone();
            cloned.id = 0;
			cloned.url = null;

			CloneDetails(cloned);
			CloneChildren(includeChildren, cloned);
			CloneAuthorizedRoles(cloned);

            return cloned;
        }

		#region Clone Helper Methods
		private void CloneAuthorizedRoles(ContentItem cloned)
		{
			if (AuthorizedRoles != null)
			{
				cloned.authorizedRoles = new List<Security.AuthorizedRole>();
				foreach (Security.AuthorizedRole role in AuthorizedRoles)
				{
					Security.AuthorizedRole clonedRole = role.Clone();
					clonedRole.EnclosingItem = cloned;
					cloned.authorizedRoles.Add(clonedRole);
				}
			}
		}

		private void CloneChildren(bool includeChildren, ContentItem cloned)
		{
			cloned.children = new List<ContentItem>();
			if (includeChildren)
			{
				foreach (ContentItem child in Children)
				{
					ContentItem clonedChild = child.Clone(true);
					clonedChild.AddTo(cloned);
				}
			}
		}

		private void CloneDetails(ContentItem cloned)
		{
			cloned.details = new Dictionary<string, Details.ContentDetail>();
			foreach (Details.ContentDetail detail in Details.Values)
			{
				cloned[detail.Name] = detail.Value;
			}

			cloned.detailCollections = new Dictionary<string, Details.DetailCollection>();
			foreach (Details.DetailCollection collection in DetailCollections.Values)
			{
				Details.DetailCollection clonedCollection = collection.Clone();
				clonedCollection.EnclosingItem = cloned;
				cloned.DetailCollections[collection.Name] = clonedCollection;
			}
		} 
		#endregion


        #endregion

		#region IUrlRewriterDependency Members

		void Web.IUrlParserDependency.SetUrlParser(Web.IUrlParser parser)
		{
			urlParser = parser;
		}

		#endregion

		#region IContainable Members

		string IContainable.ContainerName
		{
			get { return ZoneName; }
			set { ZoneName = value; }
		}

		Control IContainable.AddTo(Control container)
		{
			if (!TemplateUrl.EndsWith(".ascx", StringComparison.InvariantCultureIgnoreCase))
				throw new N2Exception("Cannot add {0} defined by {1}'s TemplateUrl property to a page. Either refrain from adding this item to a page or override TemplateUrl and have it return the url to a user control.", TemplateUrl, GetType());
			Control templateItem = container.Page.LoadControl(TemplateUrl);
			if (templateItem is N2UI.IContentTemplate)
				(templateItem as N2UI.IContentTemplate).CurrentItem = this;
			container.Controls.Add(templateItem);
			return templateItem;
		}

		/// <summary>Gets wether a certain user is authorized to view this item.</summary>
		/// <param name="user">The user to check.</param>
		/// <returns>True if the item is open for all or the user has the required permissions.</returns>
		public virtual bool IsAuthorized(IPrincipal user)
		{
			if (AuthorizedRoles == null || AuthorizedRoles.Count == 0)
			{
				return true;
			}
			else
			{
				// Iterate allowed roles to find an allowed role
				foreach (Security.Authorization auth in AuthorizedRoles)
				{
					if(auth.IsAuthorized(user))
						return true;
				}
			}
			return false;

		}

		#endregion

		#region IComparable<IContainable> Members

		int IComparable<IContainable>.CompareTo(IContainable other)
		{
			return this.SortOrder - other.SortOrder;
		}

		#endregion

		#region INode Members

		/// <summary>The logical path to the node from the root node.</summary>
		public string Path
		{
			get
			{
				string path = "/";
				for (ContentItem item = this; item.Parent != null; item = item.Parent)
				{
					path = "/" + item.Name + path;
				}
				return path;
			}
		}

		string INode.PreviewUrl
		{
			get { return Url; }
		}

		string INode.ClassNames
		{
			get
			{
				StringBuilder className = new StringBuilder();

				if (!Published.HasValue || Published > DateTime.Now)
					className.Append("unpublished ");
				else if (Published > DateTime.Now.AddDays(-1))
					className.Append("day ");
				else if (Published > DateTime.Now.AddDays(-7))
					className.Append("week ");
				else if (Published > DateTime.Now.AddMonths(-1))
					className.Append("month ");

				if (Expires.HasValue && Expires <= DateTime.Now)
					className.Append("expired ");

				if (!Visible)
					className.Append("invisible ");

				if (AuthorizedRoles != null && AuthorizedRoles.Count > 0)
					className.Append("locked ");

				return className.ToString();
			}
		}

		#region ILink Members

		string Web.ILink.Contents
		{
			get { return Title; }
		}

		string Web.ILink.ToolTip
		{
			get { return string.Empty; }
		}

		string Web.ILink.Target
		{
			get { return string.Empty; }
		}

		#endregion
		#endregion
	}
}