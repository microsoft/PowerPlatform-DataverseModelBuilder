using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Collections;
using System.Globalization;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	/// <remarks>
	/// Wrapper class for Command Line Argument PropertyInfos.
	/// </remarks>
	internal sealed class CommandLineArgument
	{
		#region Constants
		/// <summary>
		/// Character used to start a new command line paramter.
		/// </summary>
		internal const char ArgumentStartChar = '/';
		/// <summary>
		/// Character used to seperate command line parameter and value.
		/// </summary>
		internal const char ArgumentSeparatorChar = ':';
		/// <summary>
		/// Format to use when constructing the short form description for an argument.
		/// </summary>
		private const string ShortFormFormat = "Short form is '{0}{1}{2}'.";
		#endregion

		#region Fields
		private PropertyInfo _argumentProperty;
		private CommandLineArgumentAttribute _argumentAttribute;
		private bool _isSet;
		private ModeBuilderLoggerService _modeBuilderLogger;
        #endregion

        #region Constructors
        internal CommandLineArgument(PropertyInfo argumentProperty, CommandLineArgumentAttribute argumentAttribute, ModeBuilderLoggerService modeBuilderLogger)
		{
            _modeBuilderLogger = modeBuilderLogger;
            _modeBuilderLogger.TraceMethodStart();
			_modeBuilderLogger.TraceVerbose("Creating CommandLineArgument for ArgumentProperty {0} and ArgumentAttribute {1}",
				argumentProperty.Name, argumentAttribute.ToString());

			this._argumentAttribute = argumentAttribute;
			this._argumentProperty = argumentProperty;

			_modeBuilderLogger.TraceMethodStop();
		}
		#endregion

		#region Properties
		internal bool HasShortcut
		{
			get
			{
				return !(String.IsNullOrEmpty(this._argumentAttribute.Shortcut));
			}
		}

		internal string Shortcut
		{
			get
			{
				return this._argumentAttribute.Shortcut;
			}
		}

		internal string Name
		{
			get
			{
				return this._argumentAttribute.Name;
			}
		}

		internal string ParameterDescription
		{
			get
			{
				return this._argumentAttribute.ParameterDescription;
			}
		}

		internal bool IsSet
		{
			get
			{
				return this._isSet;
			}
			private set
			{
				this._isSet = value;
			}
		}

		internal bool IsCollection
		{
			get
			{
				return (this.ArgumentProperty.PropertyType.GetInterface("IList", true) != null) ||
					(this.ArgumentProperty.PropertyType.GetInterface(typeof(IList<>).FullName, true) != null);
			}
		}

		internal bool IsRequired
		{
			get
			{
				return ((this._argumentAttribute.Type & ArgumentType.Required) == ArgumentType.Required);
			}
		}

		internal bool SupportsMultiple
		{
			get
			{
				return ((this._argumentAttribute.Type & ArgumentType.Multiple) == ArgumentType.Multiple);
			}
		}

		internal bool IsFlag
		{
			get
			{
				return ((this._argumentAttribute.Type & ArgumentType.Binary) == ArgumentType.Binary);
			}
		}

		internal bool IsHidden
		{
			get
			{
				return ((this._argumentAttribute.Type & ArgumentType.Hidden) == ArgumentType.Hidden);
			}
		}

		internal string Description
		{
			get
			{
				return this._argumentAttribute.Description;
			}
		}

		internal string SampleUsageValue
		{
			get
			{
				return this._argumentAttribute.SampleUsageValue;
			}
		}

		private PropertyInfo ArgumentProperty
		{
			get
			{
				return this._argumentProperty;
			}
		}

		private static int? WrapLength
		{
			get
			{
				// If the output is not getting redirected then Console.WindowWidth will return
				// a valid value.  However, if the output is getting redirected, the property will
				// throw an IOException.  There is not good way to detect if the output is being
				// redirected so instead just catch the exception.  Callers should assume that if
				// this property returns null that the output is not going to the Console window.
				try
				{
					return Console.WindowWidth;
				}
				catch (System.IO.IOException)
				{
					return null;
				}
			}
		}
		#endregion

		#region Methods
		internal void SetValue(object argTarget, string argValue)
		{
			_modeBuilderLogger.TraceMethodStart();
			_modeBuilderLogger.TraceVerbose("Attempting to set the Argument {0} with the value {1}",
				argTarget.ToString(), ToNullableString(argValue));

			// It can only be set if it hasn't yet been set or if it supports multiple values.
			if (this.IsSet && !this.SupportsMultiple)
			{
				_modeBuilderLogger.TraceError("Attempt to set argument {0} multiple times", this.ArgumentProperty.Name);
				throw new InvalidOperationException(
					String.Format(CultureInfo.InvariantCulture,
					"Cannot set command line argument {0} multiple times",
					this.ArgumentProperty.Name));
			}

			if (this.IsCollection)
			{
				// Populate the collection parameter with the appropriate value.
				this.PopulateCollectionParameter(argTarget, argValue);
			}
			else if (this.IsFlag)
			{
				// Binary switch found so force it to true.
				_modeBuilderLogger.TraceVerbose("Setting flag property {0} to true", this.ArgumentProperty.Name);
				this.ArgumentProperty.SetValue(argTarget, true, null);
			}
			else
			{
				// Default property, so just set the value.
				_modeBuilderLogger.TraceVerbose("Setting property {0} to value {1}",
					this.ArgumentProperty.Name, ToNullableString(argValue));
				_modeBuilderLogger.TraceVerbose("Converting parameter value as ArgumentProperty {0} is defined as type {1}.",
					this.ArgumentProperty.Name, this.ArgumentProperty.PropertyType.Name);
				object castedParameter = Convert.ChangeType(argValue, this.ArgumentProperty.PropertyType,
					CultureInfo.InvariantCulture);
				this.ArgumentProperty.SetValue(argTarget, castedParameter, null);
			}

			this.IsSet = true;
			_modeBuilderLogger.TraceMethodStop();
		}

		private void PopulateCollectionParameter(object argTarget, string argValue)
		{
			_modeBuilderLogger.TraceMethodStart();

			// Collection, so add the value into the set.
			IList collection = this.ArgumentProperty.GetValue(argTarget, null) as IList;
			if (collection == null)
			{
				_modeBuilderLogger.TraceError("ArgumentProperty {0} did not return an IList as expected",
					this.ArgumentProperty.ToString());
				throw new InvalidOperationException(String.Format(
					CultureInfo.InvariantCulture,
					"ArgumentProperty {0} did not return an IList as expected.",
					this.ArgumentProperty.ToString()));
			}


			Type[] listType = this.ArgumentProperty.PropertyType.GetGenericArguments();
			if (listType == null || listType.Length == 0)
			{
				_modeBuilderLogger.TraceVerbose("Adding parameter value directly as ArgumentProperty {0} is not defined as a generic.",
					this.ArgumentProperty.Name);
				collection.Add(argValue);
			}
			else
			{
				_modeBuilderLogger.TraceVerbose("Casting parameter value as ArgumentProperty {0} is defined as a generic of type {1}.",
					this.ArgumentProperty.Name, listType[0].Name);
				object castedArgValue = Convert.ChangeType(argValue, listType[0], CultureInfo.InvariantCulture);
				_modeBuilderLogger.TraceVerbose("Argument value casted to {0} successfully.", listType[0].Name);
				collection.Add(castedArgValue);
			}

			_modeBuilderLogger.TraceMethodStop();
		}

		public override string ToString()
		{
			_modeBuilderLogger.TraceMethodStart();
			StringBuilder outputString = new StringBuilder();
			outputString.AppendLine(this.ToDescriptionString());

			StringBuilder description = new StringBuilder("  ");
			description.Append(this.Description);

			if (this.HasShortcut)
			{
				string seperatorValue = ArgumentSeparatorChar.ToString();
				if (this.IsFlag)
				{
					seperatorValue = String.Empty;
				}

				string shortForm = String.Format(CultureInfo.InvariantCulture, ShortFormFormat,
					ArgumentStartChar, this.Shortcut, seperatorValue);
				description.AppendFormat(CultureInfo.InvariantCulture, "  {0}", shortForm);
			}

			outputString.AppendLine(WrapLine(description.ToString()));

			_modeBuilderLogger.TraceMethodStop();
			return outputString.ToString();
		}

		internal string ToSampleString()
		{
			return this.ToSwitchString(this.SampleUsageValue);
		}

		private string ToDescriptionString()
		{
			if (this.IsFlag)
				return this.ToSwitchString(String.Empty);
			else
				return this.ToSwitchString(this.ParameterDescription);
		}

		private string ToSwitchString(string value)
		{
			string format = " {0}{1}{2}{3}";
			string seperatorValue = ArgumentSeparatorChar.ToString();
			if (this.IsFlag)
			{
				seperatorValue = String.Empty;
			}

			return String.Format(CultureInfo.InvariantCulture, format, ArgumentStartChar,
				this.Name, seperatorValue, value);
		}

		private static string ToNullableString(object value)
		{
			if (value == null)
				return "<NULL>";
			else
				return value.ToString();
		}

		internal static string WrapLine(string text)
		{
			int? wrapLength = WrapLength;
			if (wrapLength == null)
				return text;

			string[] strArray = text.Split(null);
			StringBuilder builder = new StringBuilder();
			int num = 0;
			foreach (string str in strArray)
			{
				int length = str.Length;
				if (((num + length) + 1) >= wrapLength)
				{
					num = length + 1;
					builder.Append("\n  " + str);
				}
				else
				{
					num += length + 1;
					builder.Append(str);
				}
				builder.Append(' ');
			}
			return builder.ToString();
		}
		#endregion
	}
}
