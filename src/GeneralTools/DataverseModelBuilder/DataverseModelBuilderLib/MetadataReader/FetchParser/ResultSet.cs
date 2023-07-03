using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Serialization;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Set of SDK message results
    /// </summary>
	[SerializableAttribute()]
	[XmlTypeAttribute(Namespace = "")]
	[XmlRootAttribute(ElementName = "resultset", Namespace = "")]
	public sealed partial class ResultSet
	{
		#region Fields
		private Result[] _results;
		private string _pagingCookie;
		private int _moreRecords;
		#endregion

		#region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
		public ResultSet()
		{
		}
		#endregion

		#region Properties

        /// <summary>
        /// Gets or sets an array of results
        /// </summary>
		[SuppressMessage("Microsoft.Performance", "CA1819:PropertiesShouldNotReturnArrays")]
		[XmlElementAttribute("result")]
		public Result[] Results
		{
			get
			{
				return this._results;
			}
			set
			{
				this._results = value;
			}
		}

        /// <summary>
        /// Gets or sets the paging cookie
        /// </summary>
		[XmlAttribute("paging-cookie")]
		public string PagingCookie
		{
			get
			{
				return _pagingCookie;
			}
			set
			{
				_pagingCookie = value;
			}
		}

        /// <summary>
        /// Gets or sets a flag for more records
        /// </summary>
		[XmlAttribute("morerecords")]
		public int MoreRecords
		{
			get
			{
				return _moreRecords;
			}
			set
			{
				_moreRecords = value;
			}
		}
		#endregion
	}
}
