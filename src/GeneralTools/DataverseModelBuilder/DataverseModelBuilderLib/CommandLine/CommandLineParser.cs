using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Reflection;
using System.Globalization;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	internal interface ICommandLineArgumentSource
	{
		void OnUnknownArgument(string argumentName, string argumentValue);
		void OnInvalidArgument(string argument);
	}

	/// <remarks>
	/// Utility class to parse command line arguments.
	/// </remarks>
	internal sealed class CommandLineParser
	{
		#region Fields
		/// <summary>
		/// The object that contains the properties to set.
		/// </summary>
		private ICommandLineArgumentSource _argsSource;
		/// <summary>
		/// A mapping of argument switches to command line arguments.
		/// </summary>
		private Dictionary<string, CommandLineArgument> _argumentsMap;
		/// <summary>
		/// A list of all of the arguments that are supported
		/// </summary>
		private List<CommandLineArgument> _arguments;
		/// <summary>
		/// Logger Service. 
		/// </summary>
		private ModeBuilderLoggerService _modeBuilderLogger; 
        #endregion

        #region Constructors
        /// <summary>
        /// Creates a new command line parser for the given object.
        /// </summary>
        /// <param name="argsSource">The object containing the properties representing the command line args to set.</param>
        internal CommandLineParser(ICommandLineArgumentSource argsSource, ModeBuilderLoggerService modeBuilderLogger)
		{
            _modeBuilderLogger= modeBuilderLogger;
            _modeBuilderLogger.TraceMethodStart();
			_modeBuilderLogger.TraceVerbose("Creating CommandLineParser for {0}.", argsSource.GetType().Name);

			this._argsSource = argsSource;
			this._arguments = new List<CommandLineArgument>();
			this._argumentsMap = this.GetPropertyMap();

			_modeBuilderLogger.TraceMethodStop();
		}
		#endregion

		#region Properties
		private ICommandLineArgumentSource ArgumentsSource
		{
			get
			{
				return this._argsSource;
			}
		}

		private List<CommandLineArgument> Arguments
		{
			get
			{
				return this._arguments;
			}
		}

		private Dictionary<string, CommandLineArgument> ArgumentsMap
		{
			get
			{
				return this._argumentsMap;
			}
		}
		#endregion

		#region Methods
		internal void ParseArguments(string[] args)
		{
			_modeBuilderLogger.TraceMethodStart();

			// Loop through all of the arguments
			if (args != null)
			{
				foreach (string argument in args)
				{
					if (CommandLineParser.IsArgument(argument))
					{
						// If we've got an argument, set that argument's value from the map.
						string argValue = null;
						string argName = CommandLineParser.GetArgumentName(argument, out argValue);
						if (!String.IsNullOrEmpty(argName) && this.ArgumentsMap.ContainsKey(argName))
						{
							_modeBuilderLogger.TraceVerbose("Setting argument {0} to value {1}", argName, argValue);
							this.ArgumentsMap[argName].SetValue(this.ArgumentsSource, argValue);
						}
						else
						{
							this.ArgumentsSource.OnUnknownArgument(argName, argValue);
						}
					}
					else
					{
						this.ArgumentsSource.OnInvalidArgument(argument);
					}
				}
			}

			this.ParseConfigArguments();

            _modeBuilderLogger.TraceMethodStop();
		}

		private void ParseConfigArguments()
		{
			_modeBuilderLogger.TraceMethodStart();

			// Loop through all of the arguments in the app.config file.
			
			//TODO:FIX FOR IOPTIONS HERE

			foreach (string appSettingKey in System.Configuration.ConfigurationManager.AppSettings.AllKeys)
			{
				// Try to find the argument in the possible list of args.  If it has not yet been
				// provided, use it.  If it was given on the command line, that one wins and we'll
				// skip the one from the config file.  If it isn't an arg, skip it as it may be
				// useful for something else.
				string argName = appSettingKey.ToUpperInvariant();
				string argValue = System.Configuration.ConfigurationManager.AppSettings[appSettingKey];
				if (this.ArgumentsMap.ContainsKey(argName) && !this.ArgumentsMap[argName].IsSet)
				{
					_modeBuilderLogger.TraceVerbose("Setting argument {0} to config value {1}", argName, argValue);
					this.ArgumentsMap[argName].SetValue(this.ArgumentsSource, argValue.Trim());
				}
				else
				{
					_modeBuilderLogger.TraceVerbose("Skipping config value {0} as it is an unknown argument.", argName);
				}
			}

			_modeBuilderLogger.TraceMethodStop();
		}

		internal bool VerifyArguments()
		{
			_modeBuilderLogger.TraceMethodStart();

			// Add some complexity here..

			// if Outdir AND out are set.  fail. 
			if ((ArgumentsMap.ContainsKey("OUTDIRECTORY") && ArgumentsMap["OUTDIRECTORY"].IsSet || ArgumentsMap.ContainsKey("OUTDIR") && ArgumentsMap["OUTDIR"].IsSet) &&
                (ArgumentsMap.ContainsKey("OUT") && ArgumentsMap["OUT"].IsSet || ArgumentsMap.ContainsKey("O") && ArgumentsMap["O"].IsSet))
			{
                _modeBuilderLogger.WriteConsoleError("Arguments OUTDIRECTORY and OUT are set.  Only one or the other is allowed for this command to execute", false, Status.ProcessStage.ParseParamaters);
                return false;
            }

            // if WriteSettings and SettingsFile are set, Fail. 
            if ((ArgumentsMap.ContainsKey("SETTINGSTEMPLATEFILE") && ArgumentsMap["SETTINGSTEMPLATEFILE"].IsSet || ArgumentsMap.ContainsKey("STF") && ArgumentsMap["STF"].IsSet) &&
                (ArgumentsMap.ContainsKey("WRITESETTINGSTEMPLATEFILE") && ArgumentsMap["WRITESETTINGSTEMPLATEFILE"].IsSet || ArgumentsMap.ContainsKey("WSTF") && ArgumentsMap["WSTF"].IsSet))
            {
                _modeBuilderLogger.WriteConsoleError("Arguments settingsTemplateFile and writeSettingsTemplateFile are set.  Only one or the other is allowed for this command to execute", false, Status.ProcessStage.ParseParamaters);
                return false;
            }

            // If SplitFiles is set,  and Outdirectory is set, allow Outfile to be skipped. 
            bool outVarCanBeSkipped = false;
			if (ArgumentsMap.ContainsKey("SPLITFILES") && ArgumentsMap["SPLITFILES"].IsSet
				&& (ArgumentsMap.ContainsKey("OUTDIRECTORY") && ArgumentsMap["OUTDIRECTORY"].IsSet || ArgumentsMap.ContainsKey("OUTDIR") && ArgumentsMap["OUTDIR"].IsSet))
			{
				outVarCanBeSkipped = true;
			}

			// check both for either outdir or out.
			if (!(ArgumentsMap.ContainsKey("OUTDIRECTORY") && ArgumentsMap["OUTDIRECTORY"].IsSet || ArgumentsMap.ContainsKey("OUTDIR") && ArgumentsMap["OUTDIR"].IsSet) &&
				!(ArgumentsMap.ContainsKey("OUT") && ArgumentsMap["OUT"].IsSet || ArgumentsMap.ContainsKey("O") && ArgumentsMap["O"].IsSet))
			{
                // neither Out dir or out is set. 
                _modeBuilderLogger.WriteConsoleError("Required argument OUTDIRECTORY or OUT is not set.  One or the other is required for this command to execute", false, Status.ProcessStage.ParseParamaters);
                return false;
			}

			// Validate add bypassing logic for CmdLine and Profile base args.
			if (ArgumentsMap.ContainsKey("CONNECTIONNAME") && ArgumentsMap["CONNECTIONNAME"].IsSet
			&& (ArgumentsMap.ContainsKey("OUT") && ArgumentsMap["OUT"].IsSet || ArgumentsMap.ContainsKey("O") && ArgumentsMap["O"].IsSet))
				return true;

			// Fall out with true if Interative login is set.
			if (ArgumentsMap.ContainsKey("INTERACTIVELOGIN") && ArgumentsMap["INTERACTIVELOGIN"].IsSet
				&& (ArgumentsMap.ContainsKey("OUT") && ArgumentsMap["OUT"].IsSet || ArgumentsMap.ContainsKey("O") && ArgumentsMap["O"].IsSet))
				return true;

			// Fall out with true if Connection string is set.
			if (ArgumentsMap.ContainsKey("CONNECTIONSTRING") && ArgumentsMap["CONNECTIONSTRING"].IsSet
				&& (ArgumentsMap.ContainsKey("OUT") && ArgumentsMap["OUT"].IsSet || ArgumentsMap.ContainsKey("O") && ArgumentsMap["O"].IsSet))
				return true;

			bool isSuccess = true;
			foreach (CommandLineArgument argument in this.ArgumentsMap.Values)
			{
				if (argument.IsRequired && !argument.IsSet)
				{
                    if ((argument.Name.Equals("OUT", StringComparison.OrdinalIgnoreCase) || argument.Name.Equals("O", StringComparison.OrdinalIgnoreCase)) && !outVarCanBeSkipped)
                    {
						_modeBuilderLogger.WriteConsoleError($"Required argument {argument.Name} is not set.", false, Status.ProcessStage.ParseParamaters);
                        isSuccess = false;
                    }
                }
			}
			_modeBuilderLogger.TraceMethodStop();
			return isSuccess;
		}

		internal void WriteUsage()
		{
			_modeBuilderLogger.TraceMethodStart();


			_modeBuilderLogger.WriteConsole("", false, Status.ProcessStage.Help);
            _modeBuilderLogger.WriteConsole("Options:", false, Status.ProcessStage.Help);

			foreach (CommandLineArgument argument in this.Arguments)
			{
				if (!argument.IsHidden)
                    _modeBuilderLogger.WriteConsole(argument.ToString(), false, Status.ProcessStage.Help);
			}

            _modeBuilderLogger.WriteConsole("", false, Status.ProcessStage.Help);
            _modeBuilderLogger.WriteConsole("Example:", false, Status.ProcessStage.Help);
            _modeBuilderLogger.WriteConsole(this.GetSampleUsage(), false, Status.ProcessStage.Help);
            _modeBuilderLogger.WriteConsole("", false, Status.ProcessStage.Help);

            _modeBuilderLogger.TraceMethodStop();
		}

		private string GetSampleUsage()
		{
			StringBuilder builder = new StringBuilder();
			builder.Append(System.IO.Path.GetFileName(Assembly.GetExecutingAssembly().Location));
			foreach (CommandLineArgument argument in this.Arguments)
			{
				if (!argument.IsHidden && argument.IsRequired && !String.IsNullOrEmpty(argument.SampleUsageValue))
				{
					builder.Append(argument.ToSampleString());
				}
			}
			return CommandLineArgument.WrapLine(builder.ToString());
		}

		/// <summary>
		/// Populates the command line arguments map.
		/// </summary>
		private Dictionary<string, CommandLineArgument> GetPropertyMap()
		{
			_modeBuilderLogger.TraceMethodStart();

			Dictionary<string, CommandLineArgument> propertyMap = new Dictionary<string, CommandLineArgument>();

			// Get all instance properties for the represented object.
			PropertyInfo[] properties = this.ArgumentsSource.GetType().GetProperties(
				BindingFlags.Instance | BindingFlags.NonPublic |
				BindingFlags.Public | BindingFlags.SetProperty |
				BindingFlags.GetProperty);

			foreach (PropertyInfo property in properties)
			{
				_modeBuilderLogger.TraceVerbose("Checking property {0} for command line attribution", property.Name);

				// Check to see if it has the CommandLineArgumentAttribute.
				CommandLineArgumentAttribute attribute = GetCommandLineAttribute(property);
				if (attribute == null)
				{
					_modeBuilderLogger.TraceVerbose("Skipping property {0} since it does not have command line attribution", property.Name);
					continue;
				}

				// Create a new CommandLineArgument for the property.
				_modeBuilderLogger.TraceVerbose("Creating CommandLineArgument for Property {0}", property.Name);
				CommandLineArgument argument = new CommandLineArgument(property, attribute, _modeBuilderLogger);
				this.Arguments.Add(argument);

				CreateMapEntry(_modeBuilderLogger, propertyMap, property, argument, "shortcut", argument.Shortcut);
				CreateMapEntry(_modeBuilderLogger, propertyMap, property, argument, "name", argument.Name);
			}

			_modeBuilderLogger.TraceMethodStop();
			return propertyMap;
		}

		private static bool CreateMapEntry(ModeBuilderLoggerService modeBuilderLogger, Dictionary<string, CommandLineArgument> propertyMap, PropertyInfo property, CommandLineArgument argument, string type, string value)
		{
			if (!String.IsNullOrEmpty(value))
			{
                modeBuilderLogger.TraceVerbose("Property {0} has defined a {1} {2}", property.Name, type, value);
				propertyMap.Add(value.ToUpperInvariant(), argument);
				return true;
			}
			else
			{
				return false;
			}
		}

		private static CommandLineArgumentAttribute GetCommandLineAttribute(PropertyInfo property)
		{
			object[] attributes = property.GetCustomAttributes(typeof(CommandLineArgumentAttribute), false);
			if (attributes == null || attributes.Length == 0)
				return null;
			else
			{
				return (CommandLineArgumentAttribute)attributes[0];
			}
		}

		private static bool IsArgument(string argument)
		{
			return (argument[0] == CommandLineArgument.ArgumentStartChar);
		}

		private static string GetArgumentName(string argument, out string argumentValue)
		{
			argumentValue = null;
			string argumentName = null;
			if (argument[0] == CommandLineArgument.ArgumentStartChar)
			{
				int separatorPos = argument.IndexOf(CommandLineArgument.ArgumentSeparatorChar);


				if (separatorPos != -1)
				{
					argumentName = argument.Substring(1, separatorPos - 1);
					argumentValue = argument.Substring(separatorPos + 1).Trim();
				}
				else
				{
					argumentName = argument.Substring(1);
				}
			}
			// Case-Insensitive argument parsing.
			return argumentName.ToUpperInvariant();
		}
		#endregion
	}
}
