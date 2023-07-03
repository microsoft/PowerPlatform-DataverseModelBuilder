using System;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// SDK Message result
    /// </summary>
    [SerializableAttribute()]
	[XmlTypeAttribute()]
	public sealed partial class Result
	{
		#region Fields
		private string _nameField;
		private bool _isPrivateField;
		private byte _customizationLevel;
		private Guid _sdkMessageIdField;
		private Guid _sdkMessageRequestIdField;
		private Guid _sdkMessagePairIdField;
		private string _sdkMessagePairNamespace;
		private Guid _sdkMessageFilterIdField;
		private string _sdkMessageRequestNameField;
		private string _sdkMessageRequestFieldNameField;
		private bool _sdkMessageRequestFieldIsOptionalField;
		private string _sdkMessageRequestFieldParserField;
		private string _sdkMessageRequestFieldCLRParserField;
		private Guid _sdkMessageResponseIdField;
		private string _sdkMessageResponseFieldValueField;
		private string _sdkMessageResponseFieldFormatterField;
		private string _sdkMessageResponseFieldCLRFormatterField;
		private string _sdkMessageResponseFieldNameField;
		private int? _sdkMessageRequestFieldPositionField;
		private int? _sdkMessageResponseFieldPositionField;
		private int _sdkMessageFilterPrimaryOTCField;
		private int _sdkMessageFilterSecondaryOTCField;
		#endregion

		#region Constructors

        /// <summary>
        /// Default constructor
        /// </summary>
		public Result()
		{
		}
		#endregion

		#region Properties

        /// <summary>
        /// Message name
        /// </summary>
		[XmlElementAttribute(ElementName = "name", Form = XmlSchemaForm.Unqualified)]
		public string Name
		{
			get
			{
				return this._nameField;
			}
			set
			{
				this._nameField = value;
			}
		}

        /// <summary>
        /// Gets or sets whether the message is private
        /// </summary>
		[XmlElementAttribute(ElementName = "isprivate", Form = XmlSchemaForm.Unqualified)]
		public bool IsPrivate
		{
			get
			{
				return this._isPrivateField;
			}
			set
			{
				this._isPrivateField = value;
			}
		}

        /// <summary>
        /// Gets or sets the customization level
        /// </summary>
		[XmlElementAttribute(ElementName = "customizationlevel", Form = XmlSchemaForm.Unqualified)]
		public byte CustomizationLevel
		{
			get
			{
				return this._customizationLevel;
			}
			set
			{
				this._customizationLevel = value;
			}
		}

        /// <summary>
        /// Gets or sets the message id
        /// </summary>
		[XmlElementAttribute(ElementName = "sdkmessageid", Form = XmlSchemaForm.Unqualified)]
		public Guid SdkMessageId
		{
			get
			{
				return this._sdkMessageIdField;
			}
			set
			{
				this._sdkMessageIdField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message pair id
        /// </summary>
		[XmlElementAttribute("sdkmessagepair.sdkmessagepairid", Form = XmlSchemaForm.Unqualified)]
		public Guid SdkMessagePairId
		{
			get
			{
				return this._sdkMessagePairIdField;
			}
			set
			{
				this._sdkMessagePairIdField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message pair namespace
        /// </summary>
		[XmlElementAttribute("sdkmessagepair.namespace", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessagePairNamespace
		{
			get
			{
				return this._sdkMessagePairNamespace;
			}
			set
			{
				this._sdkMessagePairNamespace = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request id
        /// </summary>
		[XmlElementAttribute("sdkmessagerequest.sdkmessagerequestid", Form = XmlSchemaForm.Unqualified)]
		public Guid SdkMessageRequestId
		{
			get
			{
				return this._sdkMessageRequestIdField;
			}
			set
			{
				this._sdkMessageRequestIdField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request name
        /// </summary>
		[XmlElementAttribute("sdkmessagerequest.name", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessageRequestName
		{
			get
			{
				return this._sdkMessageRequestNameField;
			}
			set
			{
				this._sdkMessageRequestNameField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request field name
        /// </summary>
		[XmlElementAttribute("sdkmessagerequestfield.name", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessageRequestFieldName
		{
			get
			{
				return this._sdkMessageRequestFieldNameField;
			}
			set
			{
				this._sdkMessageRequestFieldNameField = value;
			}
		}

        /// <summary>
        /// Gets or sets whether the message request field is optional
        /// </summary>
		[XmlElementAttribute("sdkmessagerequestfield.optional", Form = XmlSchemaForm.Unqualified)]
		public bool SdkMessageRequestFieldIsOptional
		{
			get
			{
				return this._sdkMessageRequestFieldIsOptionalField;
			}
			set
			{
				this._sdkMessageRequestFieldIsOptionalField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request field parser
        /// </summary>
		[XmlElementAttribute("sdkmessagerequestfield.parser", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessageRequestFieldParser
		{
			get
			{
				return this._sdkMessageRequestFieldParserField;
			}
			set
			{
				this._sdkMessageRequestFieldParserField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request field CLR parser
        /// </summary>
        [XmlElementAttribute("sdkmessagerequestfield.clrparser", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessageRequestFieldClrParser
		{
			get
			{
				return this._sdkMessageRequestFieldCLRParserField;
			}
			set
			{
				this._sdkMessageRequestFieldCLRParserField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request response id
        /// </summary>
        [XmlElementAttribute("sdkmessageresponse.sdkmessageresponseid", Form = XmlSchemaForm.Unqualified)]
		public Guid SdkMessageResponseId
		{
			get
			{
				return this._sdkMessageResponseIdField;
			}
			set
			{
				this._sdkMessageResponseIdField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request response field value
        /// </summary>
		[XmlElementAttribute("sdkmessageresponsefield.value", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessageResponseFieldValue
		{
			get
			{
				return this._sdkMessageResponseFieldValueField;
			}
			set
			{
				this._sdkMessageResponseFieldValueField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request response field formatter
        /// </summary>
		[XmlElementAttribute("sdkmessageresponsefield.formatter", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessageResponseFieldFormatter
		{
			get
			{
				return this._sdkMessageResponseFieldFormatterField;
			}
			set
			{
				this._sdkMessageResponseFieldFormatterField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request response field CLR formatter
        /// </summary>
		[XmlElementAttribute("sdkmessageresponsefield.clrformatter", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessageResponseFieldClrFormatter
		{
			get
			{
				return this._sdkMessageResponseFieldCLRFormatterField;
			}
			set
			{
				this._sdkMessageResponseFieldCLRFormatterField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request response field name
        /// </summary>
		[XmlElementAttribute("sdkmessageresponsefield.name", Form = XmlSchemaForm.Unqualified)]
		public string SdkMessageResponseFieldName
		{
			get
			{
				return this._sdkMessageResponseFieldNameField;
			}
			set
			{
				this._sdkMessageResponseFieldNameField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message request field position
        /// </summary>
		[XmlElementAttribute("sdkmessagerequestfield.position", Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? SdkMessageRequestFieldPosition
		{
			get
			{
				return this._sdkMessageRequestFieldPositionField;
			}
			set
			{
				this._sdkMessageRequestFieldPositionField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message response field position
        /// </summary>
		[XmlElementAttribute("sdkmessageresponsefield.position", Form = XmlSchemaForm.Unqualified, IsNullable = true)]
		public int? SdkMessageResponseFieldPosition
		{
			get
			{
				return this._sdkMessageResponseFieldPositionField;
			}
			set
			{
				this._sdkMessageResponseFieldPositionField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message filter id
        /// </summary>
		[XmlElementAttribute("sdmessagefilter.sdkmessagefilterid", Form = XmlSchemaForm.Unqualified)]
		public Guid SdkMessageFilterId
		{
			get
			{
				return this._sdkMessageFilterIdField;
			}
			set
			{
				this._sdkMessageFilterIdField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message primary OTC filter
        /// </summary>
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OTC")]
		[XmlElementAttribute("sdmessagefilter.primaryobjecttypecode", Form = XmlSchemaForm.Unqualified)]
		public int SdkMessagePrimaryOTCFilter
		{
			get
			{
				return this._sdkMessageFilterPrimaryOTCField;
			}
			set
			{
				this._sdkMessageFilterPrimaryOTCField = value;
			}
		}

        /// <summary>
        /// Gets or sets the message secondary OTC filter
        /// </summary>
		[SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "OTC")]
		[XmlElementAttribute("sdmessagefilter.secondaryobjecttypecode", Form = XmlSchemaForm.Unqualified)]
		public int SdkMessageSecondaryOTCFilter
		{
			get
			{
				return this._sdkMessageFilterSecondaryOTCField;
			}
			set
			{
				this._sdkMessageFilterSecondaryOTCField = value;
			}
		}
		#endregion
	}
}
