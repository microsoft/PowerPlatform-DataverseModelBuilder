using Microsoft.Crm.Sdk.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.BuilderSettings
{
    internal static class SettingsParser
    {
        internal static readonly string _defaultTemplateName = "builderSettings.json";
        /// <summary>
        /// 
        /// </summary>
        /// <param name="inboundParameters"></param>
        /// <returns></returns>
        internal static ModelBuilderInvokeParameters WriteSettingsFile(ModelBuilderInvokeParameters inboundParameters)
        {
            if (inboundParameters == null)
                throw new ArgumentNullException(nameof(inboundParameters));

            if (inboundParameters.WriteSettingsTemplate)
            {
                // populate template with relevant data and write out. 
                SettingsConfiguration templateSettingsFile = new SettingsConfiguration()
                {
                    CodeCustomizationService = inboundParameters.CodeCustomizationService ?? null,
                    CodeGenerationService = inboundParameters.CodeGenerationService ?? null,
                    CodeWriterFilterService = inboundParameters.CodeWriterFilterService ?? null,
                    CodeWriterMessageFilterService = inboundParameters.CodeWriterMessageFilterService ?? null,
                    EmitFieldsClasses = inboundParameters.EmitFieldClasses,
                    GenerateSdkMessages = inboundParameters.GenerateSdkMessages,
                    GenerateGlobalOptionSets = inboundParameters.GenerateGlobalOptionSets,
                    Language = inboundParameters.Language ?? null,
                    EntityTypesFolder = inboundParameters.EntityFolderName ?? "Entities",
                    MessagesTypesFolder = inboundParameters.MessagesFolderName ?? "Messages",
                    OptionSetsTypesFolder = inboundParameters.OptionSetFolderName ?? "OptionSets",
                    MetadataProviderService = inboundParameters.MetadataProviderService ?? null,
                    MetadataQueryProviderService = inboundParameters.MetadataQueryProviderService ?? null,
                    Namespace = inboundParameters.Namespace ?? null,
                    NamingService = inboundParameters.NamingService ?? null,
                    ServiceContextName = inboundParameters.ServiceContextName ?? null,
                    SuppressGeneratedCodeAttribute = inboundParameters.SuppressGenVersionAttribute,
                    SuppressINotifyPattern = inboundParameters.SuppressINotifyPattern,
                    EmitEntityETC = inboundParameters.EmitEntityETC,
                    EmitVirtualAttributes = inboundParameters.EmitVirtualAttributes,
                };

                if (!string.IsNullOrEmpty(inboundParameters.MessageNamesFilter))
                    templateSettingsFile.MessageNamesFilter = Utility.Utilites.GetItemListFromString(inboundParameters.ToDictionary(), ";", "messagenamesfilter");
                if (!string.IsNullOrEmpty(inboundParameters.EntityNamesFilter))
                    templateSettingsFile.EntityNamesFilter = Utility.Utilites.GetItemListFromString(inboundParameters.ToDictionary(), ";", "entitynamesfilter");

                // Write file: 
                // if Output directory is present
                if (!string.IsNullOrEmpty(inboundParameters.OutDirectory))
                {
                    if(!Directory.Exists(inboundParameters.OutDirectory))
                        Directory.CreateDirectory(inboundParameters.OutDirectory);

                    WriteSettingsFileTemplate(inboundParameters.OutDirectory, templateSettingsFile);
                    return inboundParameters;
                }

                // if Out file is set. 
                if (!string.IsNullOrEmpty(inboundParameters.OutputFile))
                {
                    if (!Directory.Exists(Path.GetDirectoryName(inboundParameters.OutputFile)))
                        Directory.CreateDirectory(Path.GetDirectoryName(inboundParameters.OutputFile));

                    WriteSettingsFileTemplate(Path.GetDirectoryName(inboundParameters.OutputFile), templateSettingsFile);
                    return inboundParameters;
                }
            }
            return inboundParameters; 
        }

        internal static string[] ReadSettingsFileIntoArgs(string[] incomingArgs)
        {
            List<string> argsList = new List<string>(incomingArgs);
            // Hammer Parse Args list for key value 
            var templateFileRow = argsList.Where(w => w.StartsWith($"/{GetPropertyCmdLineKeyForProperty("SettingsTemplateFile")}", StringComparison.OrdinalIgnoreCase) ||
                                                 w.StartsWith($"/{GetPropertyCmdLineKeyForProperty("SettingsTemplateFile",true)}", StringComparison.OrdinalIgnoreCase)).ToList();

            var createTemplateFileRow = argsList.Where(w => w.StartsWith($"/{GetPropertyCmdLineKeyForProperty("WriteSettingsTemplate")}", StringComparison.OrdinalIgnoreCase) ||
                                     w.StartsWith($"/{GetPropertyCmdLineKeyForProperty("WriteSettingsTemplate", true)}", StringComparison.OrdinalIgnoreCase)).ToList();
            
            if (templateFileRow.Any() && createTemplateFileRow.Any())
                return incomingArgs; // Do not process as both write and read are present,  process will fault on validate. 

            if (templateFileRow.Any())
            {
                string settingsFileName = templateFileRow.FirstOrDefault().Split(new List<char>() { ':' }.ToArray(), 2).Last();
                if (File.Exists(settingsFileName))
                {
                    // have settings file..> read that into parameters. 
                    SettingsConfiguration settingsConfiguration = SettingsConfiguration.FromJson(File.ReadAllText(settingsFileName));

                    ModelBuilderInvokeParameters inboundParameters;
                    // strings
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.CodeCustomizationService), nameof(inboundParameters.CodeCustomizationService));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.CodeGenerationService), nameof(inboundParameters.CodeGenerationService));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.CodeWriterFilterService), nameof(inboundParameters.CodeWriterFilterService));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.CodeWriterMessageFilterService), nameof(inboundParameters.CodeWriterMessageFilterService));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.Language), nameof(inboundParameters.Language));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.EntityTypesFolder), nameof(inboundParameters.EntityFolderName));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.MessagesTypesFolder), nameof(inboundParameters.MessagesFolderName));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.OptionSetsTypesFolder), nameof(inboundParameters.OptionSetFolderName));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.MetadataProviderService), nameof(inboundParameters.MetadataProviderService));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.MetadataQueryProviderService), nameof(inboundParameters.MetadataQueryProviderService));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.Namespace), nameof(inboundParameters.Namespace));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.NamingService), nameof(inboundParameters.NamingService));
                    AddOrSkipAddingProperty<string>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.ServiceContextName), nameof(inboundParameters.ServiceContextName));

                    // Arrays. 
                    if (settingsConfiguration.MessageNamesFilter != null && settingsConfiguration.MessageNamesFilter.Any())
                    {
                        AddOrSkipAddingProperty<List<string>>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.MessageNamesFilter), nameof(inboundParameters.MessageNamesFilter));
                        settingsConfiguration.GenerateSdkMessages = true; // Force add the GenerateMessages flag if messages are present. 
                    }

                    if (settingsConfiguration.EntityNamesFilter != null && settingsConfiguration.EntityNamesFilter.Any())
                    {
                        AddOrSkipAddingProperty<List<string>>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.EntityNamesFilter), nameof(inboundParameters.EntityNamesFilter));
                    }

                    // bools. 
                    AddOrSkipAddingProperty<bool>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.SuppressGeneratedCodeAttribute), nameof(inboundParameters.SuppressGenVersionAttribute));
                    AddOrSkipAddingProperty<bool>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.EmitFieldsClasses), nameof(inboundParameters.EmitFieldClasses));
                    AddOrSkipAddingProperty<bool>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.EmitEntityETC), nameof(inboundParameters.EmitEntityETC));
                    AddOrSkipAddingProperty<bool>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.EmitVirtualAttributes), nameof(inboundParameters.EmitVirtualAttributes));
                    // Backward compat.. 
                    if (settingsConfiguration.GenerateActions != null)
                        AddOrSkipAddingProperty<bool>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.GenerateActions), nameof(inboundParameters.GenerateSdkMessages));
                    else
                        AddOrSkipAddingProperty<bool>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.GenerateSdkMessages), nameof(inboundParameters.GenerateSdkMessages));
                    AddOrSkipAddingProperty<bool>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.GenerateGlobalOptionSets), nameof(inboundParameters.GenerateGlobalOptionSets));
                    AddOrSkipAddingProperty<bool>(ref argsList, settingsConfiguration, nameof(settingsConfiguration.SuppressINotifyPattern), nameof(inboundParameters.SuppressINotifyPattern));


                }
                else
                {
                    throw new FileNotFoundException($"Settings file {settingsFileName} cannot be found, Please check the file name and path and try again", settingsFileName);
                }
            }
            return argsList.ToArray();
        }

        private static void AddOrSkipAddingProperty<T>( ref List<string> argList , SettingsConfiguration incommingArray , string settingName , string targetpropertyname )
        {
            var settingProperty = incommingArray.GetType().GetProperty(settingName);
            if (settingProperty != null)
            {
                var prospectiveData = incommingArray.GetType().GetProperty(settingName).GetValue(incommingArray, null);
                if (prospectiveData != null)
                {
                    var data = (T)prospectiveData;
                    if (data != null)
                    {
                        string key = $"/{GetPropertyCmdLineKeyForProperty(targetpropertyname)}:";
                        if (data is string strData && !string.IsNullOrEmpty(strData))
                        {
                            AddOrReplaceKey(ref argList, key, strData);
                        }
                        else if (data is bool bValue)
                        {
                            AddOrReplaceKey(ref argList, key, bValue.ToString(), bValue);
                        }
                        else if (data is List<string> lstData && lstData.Any())
                        {
                            AddOrReplaceKey(ref argList, key, ParseToString(lstData));
                        }
                    }
                }
            }
        }

        private static void AddOrReplaceKey(ref List<string> argList, string key, string value, bool writeValue = true)
        {
            if (argList != null && argList.Any())
            {
                var foundstrings = argList.Where(w => w.StartsWith(key, StringComparison.OrdinalIgnoreCase)).ToList();
                if (foundstrings.Any())
                {
                    foreach (var item in foundstrings)
                    {
                        argList.Remove(item);
                    }
                }
                if (writeValue) // bools are switch's .. thus if its not set we want to make sure its removed and do not write it back. 
                    argList.Add($"{key}{value}");
            }
        }

        private static string GetPropertyCmdLineKeyForProperty(string memberName , bool returnShortCut = false )
        {
            var members = typeof(ModelBuilderInvokeParameters).GetMember(memberName, BindingFlags.Public | BindingFlags.Instance);

            //var members = t.GetMembers(BindingFlags.Public | BindingFlags.Instance)
            //.Where(x => x.MemberType == MemberTypes.Field || x.MemberType == MemberTypes.Property);

            foreach (MemberInfo member in members)
            {
                var attr = member.GetCustomAttribute<CommandLineArgumentAttribute>();

                if (attr != null)
                {
                    if (returnShortCut)
                        return attr.Shortcut;
                    else
                        return attr.Name;
                }
            }
            return null;
        }


        private static string ParseToString(List<string> namedarray)
        {
            if (namedarray != null && namedarray.Any())
            {
                StringBuilder sb = new StringBuilder();
#if NETFRAMEWORK
                foreach (var item in namedarray)
                {
                    sb.Append($"{item};");
                }
                sb.Remove(sb.Length - 1, 1); // remove trailing ; 
#else
                sb.AppendJoin(";", namedarray);
#endif
                return sb.ToString();
            }
            else
                return null; 
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="outputPath"></param>
        /// <returns></returns>
        internal static bool WriteSettingsFileTemplate(string outputPath, SettingsConfiguration settingsConfig = null)
        {
            // check to make src the outputPath is valid. 
            if (string.IsNullOrEmpty(outputPath))
                return false;


            string writeDirectory = string.Empty;
            if (File.GetAttributes(outputPath).HasFlag(FileAttributes.Directory))
                writeDirectory = outputPath;
            else
                writeDirectory = Path.GetDirectoryName(outputPath);

            if (!Directory.Exists(writeDirectory))
            {
                Directory.CreateDirectory(writeDirectory);
            }

            string templateFileFullName = Path.Combine(writeDirectory, _defaultTemplateName);

            if (File.Exists(templateFileFullName))
            {
                // try to delete the file. 
                File.Delete(templateFileFullName);
            }
            File.WriteAllText(templateFileFullName, settingsConfig != null ? settingsConfig.ToJson() : new SettingsConfiguration().ToJson(), Encoding.UTF8);

            return true;
        }
    }
}
