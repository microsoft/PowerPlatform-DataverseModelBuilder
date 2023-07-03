using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	/// <remarks>
	/// Represents a command line argument.
	/// </remarks>
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	internal sealed class CommandLineArgumentAttribute : Attribute
	{
		#region Fields
		private ArgumentType _argumentType;
		private string _fullname;
		private string _shortname;
		private string _description;
		private string _parameterDescription;
		private string _sampleUsageValue;
		#endregion

		#region Constructors
		/// <summary>
		/// Creates a new command line argument attribute.
		/// </summary>
		/// <param name="argType">Type of argument represented by the property.</param>
		/// <param name="name">Switch used by the command line argument</param>
		internal CommandLineArgumentAttribute(ArgumentType argType, string name)
			: base()
		{
			
			this._argumentType = argType;
			this._fullname = name;
			this._shortname = String.Empty;
			this._description = String.Empty;
			this._parameterDescription = String.Empty;
		}
		#endregion

		#region Properties
		/// <summary>
		/// Type of command line argument
		/// </summary>
		public ArgumentType Type
		{
			get
			{
				return this._argumentType;
			}
		}

		/// <summary>
		/// Switch used to represent the argument.
		/// </summary>
		public string Name
		{
			get
			{
				return this._fullname;
			}
			set
			{
				this._fullname = value;
			}
		}

		/// <summary>
		/// Shortcut switch used to represent the argument.
		/// </summary>
		public string Shortcut
		{
			get
			{
				return this._shortname;
			}
			set
			{
				this._shortname = value;
			}
		}

		/// <summary>
		/// Description of the command line argument.
		/// </summary>
		public string Description
		{
			get
			{
				return this._description;
			}
			set
			{
				this._description = value;
			}
		}

		/// <summary>
		/// Description of the parameter.
		/// </summary>
		public string ParameterDescription
		{
			get
			{
				return this._parameterDescription;
			}
			set
			{
				this._parameterDescription = value;
			}
		}

		public string SampleUsageValue
		{
			get
			{
				return this._sampleUsageValue;
			}
			set
			{
				this._sampleUsageValue = value;
			}
		}
		#endregion
	}
}
