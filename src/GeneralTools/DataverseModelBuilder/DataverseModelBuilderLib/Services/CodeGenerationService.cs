using System;
using System.CodeDom;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.CodeDom.Compiler;
using System.Runtime.Serialization;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Client;
using Microsoft.Xrm.Sdk.Metadata;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.DefaultCustomziations;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Utility;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	[SuppressMessage("Microsoft.Maintainability", "CA1506:AvoidExcessiveClassCoupling", Justification = "The extra coupling is temporary.")]
	internal sealed class CodeGenerationService : ICodeGenerationService
	{
		#region Fields
		private static Type AttributeLogicalNameAttribute = typeof(AttributeLogicalNameAttribute);
		private static Type EntityLogicalNameAttribute = typeof(EntityLogicalNameAttribute);
		private static Type RelationshipSchemaNameAttribute = typeof(RelationshipSchemaNameAttribute);
		private static Type ObsoleteFieldAttribute = typeof(ObsoleteAttribute);

		private static Type ServiceContextBaseType = typeof(OrganizationServiceContext);
		private static Type EntityClassBaseType = typeof(Entity);

		private static Type RequestClassBaseType = typeof(OrganizationRequest);
		private static Type ResponseClassBaseType = typeof(OrganizationResponse);
		private static string RequestClassSuffix = "Request";
		private static string ResponseClassSuffix = "Response";
		private static string RequestNamePropertyName = "RequestName";
		private static string ParametersPropertyName = "Parameters";
		private static string ResultsPropertyName = "Results";
        private static ModelBuilderInvokeParameters _parameters = null;
		private static bool ContainsEnums = false;
		//private static bool ContainsMultiSelectEnums = false;
		#endregion

		internal CodeGenerationService(ModelBuilderInvokeParameters parameters)
        {
            _parameters = parameters;
        }
		#region ICodeWriter Members

		void ICodeGenerationService.Write(IOrganizationMetadata organizationMetadata, string language, string outputFile, string outputNamespace, IServiceProvider services)
		{
			_parameters.Logger.TraceMethodStart();
			ServiceProvider serviceProvider = services as ServiceProvider;
			if (_parameters.SplitFilesByObject)
			{
				Dictionary<string, CodeNamespace> codenamespaces = BuildCodeDom2(organizationMetadata, outputNamespace, serviceProvider, language, _parameters.LegacyMode);
				string ctxName = string.IsNullOrWhiteSpace(_parameters.ServiceContextName) ? "@##" : _parameters.ServiceContextName;
				foreach (var codeNSGroup in codenamespaces)
				{
					// will write the proxy attribute only in the servicecontext file. if no servicecontext file, will not write the proxy attribute.
					WriteFile(codeNSGroup.Key, language, codeNSGroup.Value, serviceProvider, codeNSGroup.Key.Contains(ctxName), true);
				}
				if (ctxName.Equals("@##"))
                {
                    _parameters.Logger.WriteConsoleWarning("ProxyTypesAssemblyAttribute not written, Please add [assembly: Microsoft.Xrm.Sdk.Client.ProxyTypesAssemblyAttribute()] to a file in your class if you require Linq or direct casting support.", true, Status.ProcessStage.FileGeneration);
				}
			}
			else
			{
				CodeNamespace codenamespace = BuildCodeDom(organizationMetadata, outputNamespace, serviceProvider, _parameters.LegacyMode);
				WriteFile(outputFile, language, codenamespace, serviceProvider);
			}
			_parameters.Logger.TraceMethodStop();
		}

		CodeGenerationType ICodeGenerationService.GetTypeForOptionSet(EntityMetadata entityMetadata, OptionSetMetadataBase optionSetMetadata, IServiceProvider services)
		{
			return CodeGenerationType.Enum;
		}

		CodeGenerationType ICodeGenerationService.GetTypeForOption(OptionSetMetadataBase optionSetMetadata, OptionMetadata optionMetadata, IServiceProvider services)
		{
			return CodeGenerationType.Field;
		}

		CodeGenerationType ICodeGenerationService.GetTypeForEntity(EntityMetadata entityMetadata, IServiceProvider services)
		{
			return CodeGenerationType.Class;
		}

		CodeGenerationType ICodeGenerationService.GetTypeForAttribute(EntityMetadata entityMetadata, AttributeMetadata attributeMetadata, IServiceProvider services)
		{
			return CodeGenerationType.Property;
		}

		CodeGenerationType ICodeGenerationService.GetTypeForMessagePair(SdkMessagePair messagePair, IServiceProvider services)
		{
			return CodeGenerationType.Class;
		}

		CodeGenerationType ICodeGenerationService.GetTypeForRequestField(SdkMessageRequest request, SdkMessageRequestField requestField, IServiceProvider services)
		{
			return CodeGenerationType.Property;
		}

		CodeGenerationType ICodeGenerationService.GetTypeForResponseField(SdkMessageResponse response, SdkMessageResponseField responseField, IServiceProvider services)
		{
			return CodeGenerationType.Property;
		}
		#endregion

		#region Helper Methods
		private static Dictionary<string, CodeNamespace> BuildCodeDom2(IOrganizationMetadata organizationMetadata, string outputNamespace, ServiceProvider serviceProvider, string language, bool useLegacyMode)
		{
			Dictionary<string, CodeNamespace> codeNamespaces = new Dictionary<string, CodeNamespace>();

			_parameters.Logger.TraceMethodStart();

			int iProcessCount = 0, iSkipCount = 0;
			Stopwatch sw = new Stopwatch();

			string defaultFolder = _parameters.OutDirectory;
			if (string.IsNullOrEmpty(defaultFolder))
			{
				defaultFolder = "Model";
			}


			// Need to allow for override of this via options.
			string defaultEntitySubFolder = _parameters.EntityFolderName;
			string defaultMessagesSubFolder = _parameters.MessagesFolderName;
			string defaultOptionSetSubFolder = _parameters.OptionSetFolderName;

			// Set the default file name extension.
			string fileExtension = "cs";
			using (CodeDomProvider provider = CodeDomProvider.CreateProvider(language))
			{
				fileExtension = provider.FileExtension;
			}

            _parameters.Logger.WriteConsole($"Processing {organizationMetadata.Entities.Count()} Entities", true, Status.ProcessStage.ClassGeneration );
			sw.Restart();
			foreach (var ent in organizationMetadata.Entities)
			{
				EntityMetadataCollection collect = new EntityMetadataCollection();
				collect.Add(ent);
				CodeNamespace codenamespace = Namespace(outputNamespace);
				codenamespace.Types.AddRange(BuildEntities(collect.ToArray(), serviceProvider,useLegacyMode, out int entProcessCount));

				if (codenamespace.Types.Count > 0) // Skip if no content.
				{
					codeNamespaces.Add(Path.Combine(defaultFolder, defaultEntitySubFolder, $"{ent.LogicalName}.{fileExtension}"), codenamespace);
					iProcessCount++;
				}
			}
			sw.Stop();
            _parameters.Logger.WriteConsole($"Wrote {iProcessCount} Entities - {sw.Elapsed}", true, Status.ProcessStage.ClassGeneration);
            _parameters.Logger.WriteConsole($"Processing {organizationMetadata.Messages.MessageCollection.Count()} Messages", true, Status.ProcessStage.ClassGeneration);
			sw.Restart();

			iProcessCount = 0;
			iSkipCount = 0;
            foreach (var sdkMessage in organizationMetadata.Messages.MessageCollection)
			{
				CodeNamespace codenamespaceMessage = Namespace(outputNamespace);
				var v1 = new Dictionary<Guid, SdkMessage>();
				v1.Add(sdkMessage.Key, sdkMessage.Value);
				SdkMessages workingMessage = new SdkMessages(v1);
				codenamespaceMessage.Types.AddRange(BuildMessages(workingMessage, serviceProvider, out int msgProcessCount));

				if (codenamespaceMessage.Types.Count > 0) // Skip if no content.
				{
					codeNamespaces.Add(Path.Combine(defaultFolder, defaultMessagesSubFolder, $"{sdkMessage.Value.Name}.{fileExtension}"), codenamespaceMessage);
					iProcessCount++;
				}
				else
				{
					iSkipCount++;
                }
			}
			sw.Stop();
            _parameters.Logger.WriteConsole($"Wrote {iProcessCount} Message(s). Skipped {iSkipCount} Message(s) - {sw.Elapsed}", true, Status.ProcessStage.ClassGeneration);
            _parameters.Logger.WriteConsole($"Processing {organizationMetadata.OptionSets.Count()} Global OptionSets", true, Status.ProcessStage.ClassGeneration);
            
			sw.Restart();
			iProcessCount = 0;
			// OptionSets.
			List<OptionSetMetadataBase> mbList = new List<OptionSetMetadataBase>();
			foreach (var optMetaBase in organizationMetadata.OptionSets)
            {
				mbList.Clear();
				mbList.Add(optMetaBase);
				CodeNamespace codenamespace = Namespace(outputNamespace);
				codenamespace.Types.AddRange(BuildOptionSets(mbList.ToArray(), serviceProvider, out int optProcessCount));
				if (codenamespace.Types.Count > 0)
				{
					codeNamespaces.Add(Path.Combine(defaultFolder, defaultOptionSetSubFolder, $"{optMetaBase.Name}.{fileExtension}"), codenamespace);
					iProcessCount++;
				}
			}
			sw.Stop();
            _parameters.Logger.WriteConsole($"Wrote {iProcessCount} Global OptionSets - {sw.Elapsed}", true, Status.ProcessStage.ClassGeneration);

			// Create ServiceProvider if needed
			if (!string.IsNullOrWhiteSpace(_parameters.ServiceContextName))
			{
				CodeNamespace codenamespaceServiceProvider = Namespace(outputNamespace);
				codenamespaceServiceProvider.Types.AddRange(BuildServiceContext(organizationMetadata.Entities, serviceProvider));
				string serviceProviderFileName = Path.Combine(defaultFolder, $"{_parameters.ServiceContextName}.{fileExtension}");
				codeNamespaces.Add(serviceProviderFileName, codenamespaceServiceProvider);
			}

            if (ContainsEnums)
            {
                CodeNamespace codenamespaceEnumOptionSetHandler = Namespace(outputNamespace);
                var generator = new DefaultCustomziations.EnumPropertyGenerator();
                generator.MultiSelectEnumCreated = true;
                codenamespaceEnumOptionSetHandler.Types.Add(generator.GetEntityOptionSetEnumDeclaration());
                string serviceProviderFileName = Path.Combine(defaultFolder, $"EntityOptionSetEnum.{fileExtension}");
                codeNamespaces.Add(serviceProviderFileName, codenamespaceEnumOptionSetHandler);

				// Need to deal with static class declaration for C# and VB
				//CodeNamespace optionSetAttributeClass = Namespace(outputNamespace);
				//optionSetAttributeClass.Types.Add(OptionSetMetadataAttributeGenerator.CreateOptionSetMetadataAttributeClass());
				//string optionSetAttribFileName = Path.Combine(defaultFolder, $"OptionSetMetadataAttribute.{fileExtension}");
				//codeNamespaces.Add(optionSetAttribFileName, optionSetAttributeClass);

    //            CodeNamespace optionSetExtention = Namespace(outputNamespace);
    //            optionSetExtention.Types.Add(OptionSetMetadataAttributeGenerator.CreateOptionSetExtensionClass());
    //            string optionSetExtentionFileName = Path.Combine(defaultFolder, $"OptionSetExtension.{fileExtension}");
    //            codeNamespaces.Add(optionSetExtentionFileName, optionSetExtention);
            }

            _parameters.Logger.TraceMethodStop();
			return codeNamespaces;
		}

		private static CodeNamespace BuildCodeDom(IOrganizationMetadata organizationMetadata, string outputNamespace, ServiceProvider serviceProvider, bool useLegacyMode)
		{
			_parameters.Logger.TraceMethodStart();

			CodeNamespace codenamespace = Namespace(outputNamespace);

			Stopwatch sw = new Stopwatch();
			sw.Start();

            _parameters.Logger.WriteConsole($"Processing {organizationMetadata.OptionSets.Count()} OptionSets", true, Status.ProcessStage.ClassGeneration);
            sw.Restart();
			codenamespace.Types.AddRange(BuildOptionSets(organizationMetadata.OptionSets, serviceProvider, out int optProcessedCount));
			sw.Stop();
            _parameters.Logger.WriteConsole($"Wrote {optProcessedCount} OptionSets - {sw.Elapsed}", true, Status.ProcessStage.ClassGeneration);


            _parameters.Logger.WriteConsole($"Processing {organizationMetadata.Entities.Count()} Entities", true, Status.ProcessStage.ClassGeneration);
			sw.Restart();
            codenamespace.Types.AddRange(BuildEntities(organizationMetadata.Entities, serviceProvider, useLegacyMode, out int entProcessedCount));
			sw.Stop();
            _parameters.Logger.WriteConsole($"Wrote {entProcessedCount} Entities - {sw.Elapsed}", true, Status.ProcessStage.ClassGeneration);

			codenamespace.Types.AddRange(BuildServiceContext(organizationMetadata.Entities, serviceProvider));

            _parameters.Logger.WriteConsole($"Processing {organizationMetadata.Messages.MessageCollection.Count()} Messages", true, Status.ProcessStage.ClassGeneration);
			sw.Restart();
			codenamespace.Types.AddRange(BuildMessages(organizationMetadata.Messages, serviceProvider, out int msgProcessedCount));
			sw.Stop();
            _parameters.Logger.WriteConsole($"Wrote {msgProcessedCount} Messages - {sw.Elapsed}", true, Status.ProcessStage.ClassGeneration);

			if (!useLegacyMode)
			{
				if (ContainsEnums)
				{
					var generator = new EnumPropertyGenerator();
					generator.MultiSelectEnumCreated = true;
					codenamespace.Types.Add(generator.GetEntityOptionSetEnumDeclaration());

					// Need to solve for Static attribute ( which means I need to deal with VB and Non VB ).
					//codenamespace.Types.Add(OptionSetMetadataAttributeGenerator.CreateOptionSetMetadataAttributeClass());
					//codenamespace.Types.Add(OptionSetMetadataAttributeGenerator.CreateOptionSetExtensionClass());
				}
			}

			_parameters.Logger.TraceMethodStop();
			return codenamespace;
		}

		private static void WriteFile(string outputFile, string language, CodeNamespace codenamespace, ServiceProvider serviceProvider , bool writeProxyAttrib = true , bool isFileSplit = false)
		{
			_parameters.Logger.TraceMethodStart();

			// force create path to file if required.
			FileInfo fi = new FileInfo(outputFile);
			if (!fi.Directory.Exists)
            {
				fi.Directory.Create();
            }

			// Use the CodeCompileUnit instead of the namespace directly so you get the
			// <autogenerated /> comments in the generated code.
			CodeCompileUnit compileUnit = new CodeCompileUnit();
			compileUnit.Namespaces.Add(codenamespace);
			if (writeProxyAttrib)
			{
				compileUnit.AssemblyCustomAttributes.Add(Attribute(typeof(Microsoft.Xrm.Sdk.Client.ProxyTypesAssemblyAttribute)));
			}

			serviceProvider.CodeCustomizationService.CustomizeCodeDom(compileUnit, serviceProvider);

			CodeGeneratorOptions options = new CodeGeneratorOptions();
			options.BlankLinesBetweenMembers = true;
			options.BracingStyle = "C";
			options.IndentString = "\t";
			options.VerbatimOrder = true;

			bool isCS = language.Equals("CS", StringComparison.OrdinalIgnoreCase);
			bool isVB = language.Equals("VB", StringComparison.OrdinalIgnoreCase);

			using (StreamWriter fileWriter = new StreamWriter(outputFile))
			{
				using (CodeDomProvider provider = CodeDomProvider.CreateProvider(language))
				{
					if (isCS) // Handle CS here.
					{
                        provider.GenerateCodeFromCompileUnit(new CodeSnippetCompileUnit("#pragma warning disable CS1591"), fileWriter, options);
					}

                    provider.GenerateCodeFromCompileUnit(compileUnit, fileWriter, options);

					if (isCS)
					{
						provider.GenerateCodeFromCompileUnit(new CodeSnippetCompileUnit("#pragma warning restore CS1591"), fileWriter, options);
					}
				}
			}

			_parameters.Logger.TraceMethodStop();
            _parameters.Logger.WriteConsole(String.Format(CultureInfo.InvariantCulture, "Code written to {0}.", System.IO.Path.GetFullPath(outputFile)), true, Status.ProcessStage.FileGeneration);
		}

		#region OptionSets/Options Generation Logic
		private static CodeTypeDeclarationCollection BuildOptionSets(OptionSetMetadataBase[] optionSetMetadata, ServiceProvider serviceProvider, out int processedCount)
		{
			_parameters.Logger.TraceMethodStart();

			CodeTypeDeclarationCollection types = new CodeTypeDeclarationCollection();
			int iProcessCount = 0;
			if (optionSetMetadata.Count() > 0)
			{
				_parameters.Logger.TraceVerbose($"Processing {optionSetMetadata.Count()} OptionSets");
				foreach (OptionSetMetadataBase optionSet in optionSetMetadata)
				{
					// Only worry about GlobalOptionSets here.  We'll hit the Entity specific ones during the entity processing.
					if (serviceProvider.CodeFilterService.GenerateOptionSet(optionSet, serviceProvider) &&
						!(optionSet.IsGlobal == null) && optionSet.IsGlobal.Value)
					{
						CodeTypeDeclaration typeDecl = BuildOptionSetEnumType(null, optionSet, serviceProvider);
						if (typeDecl != null)
						{
							iProcessCount++;
							types.Add(typeDecl);
						}
						else
						{
							_parameters.Logger.TraceVerbose("Skipping OptionSet {0} of type {1} from being generated.", optionSet.Name, optionSet.GetType());
						}
					}
					else
					{
						_parameters.Logger.TraceVerbose("Skipping OptionSet {0} from being generated.", optionSet.Name);
					}
				}
				_parameters.Logger.TraceVerbose($"Wrote {iProcessCount} OptionSets");
			}
			processedCount = iProcessCount;
			_parameters.Logger.TraceMethodStop();
			return types;
		}

		private static CodeTypeDeclaration BuildOptionSetEnumType(EntityMetadata entity, OptionSetMetadataBase optionSet, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			CodeTypeDeclaration optionSetEnum = Enum(serviceProvider.NamingService.GetNameForOptionSet(entity, optionSet, serviceProvider),
				Attribute(typeof(DataContractAttribute)));

			// Add Comments.
			TryAddCodeComments(optionSetEnum.Comments, optionSet.Description);

			OptionSetMetadata optionSetMetadata = optionSet as OptionSetMetadata;
			if (optionSetMetadata == null)
				return null;

            //Modify to return no Option Set if there are no Options to render
            if (optionSetMetadata.Options.Count() == 0)
            {
                _parameters.Logger.TraceWarning($"Skipping OptionSet {optionSet.Name}. OptionSet contains no Options and would generate an empty enum.");
                return null;
            }

			// do not write global optionsets into an entity
			if ( optionSetMetadata.IsGlobal.Value && entity != null )
            {
				// Add the optionset to the global optionsets list.
				IOrganizationMetadata orgMetdata = serviceProvider.MetadataProviderService.LoadMetadata(serviceProvider);
				if (orgMetdata is IOrganizationMetadata2 orgMetaDataLocal)
                {
					orgMetaDataLocal.AddOptionSetInfo(optionSetMetadata);
				}
				_parameters.Logger.TraceWarning($"Skipping OptionSet {optionSet.Name}. OptionSet is a global optionset, skipping entity level add.");
				return null;
			}

			List<string> sOptionSetNameList = new List<string>();
			foreach (OptionMetadata option in optionSetMetadata.Options)
			{
				if (serviceProvider.CodeFilterService.GenerateOption(option, serviceProvider))
				{
					optionSetEnum.Members.Add(BuildOption(optionSet, option, serviceProvider , sOptionSetNameList));
				}
				else
				{
					_parameters.Logger.TraceVerbose("Skipping {0}.Option {1} from being generated.", optionSet.Name, option.Value.Value);
				}
			}

			_parameters.Logger.TraceMethodStop();
			return optionSetEnum;
		}

		private static CodeTypeMember BuildOption(OptionSetMetadataBase optionSet, OptionMetadata option, ServiceProvider serviceProvider, List<string> optionsNameList)
		{
			_parameters.Logger.TraceMethodStart();
			string optionName = serviceProvider.NamingService.GetNameForOption(optionSet, option, serviceProvider);

			if (optionsNameList.Contains(optionName)) // Check for duplicates
            {
				// Duplicate OptionName add incrementing flag
				var matchingOptions = optionsNameList.Where(w => w.StartsWith(optionName)).OrderByDescending(q => q).ToList();
                foreach (var opt in matchingOptions)
                {
					if (opt.Length == optionName.Length) // direct match
					{
						optionName = $"{optionName}1";
						break;
					}
					else
					{
						if ((optionName.Length <= opt.Length) && (char.IsDigit(opt[optionName.Length]))) // in descending order this should be max
						{
							int iCurrentMaxValue = Convert.ToInt32(opt[optionName.Length].ToString());
							optionName = $"{optionName}{++iCurrentMaxValue}";
							break;
						}
					}

				}
            }
			optionsNameList.Add(optionName);

			CodeMemberField field = Field(optionName,
				//typeof(int), option.Value.Value, Attribute(typeof(EnumMemberAttribute)), OptionSetMetadataAttributeGenerator.CreateOptionSetAttribute(optionName, option));
				typeof(int), option.Value.Value, Attribute(typeof(EnumMemberAttribute))); // Disabling attribute for the moment till I resolve Static class problems.

			// Add comments if possible.
			TryAddCodeComments(field.Comments, option.Description);

			_parameters.Logger.TraceMethodStop();
			return field;
		}
		#endregion

		#region Entities/Attributes Generation Logic
		private static CodeTypeDeclarationCollection BuildEntities(EntityMetadata[] entityMetadata, ServiceProvider serviceProvider, bool useLegacyMode, out int processedCount)
		{
			_parameters.Logger.TraceMethodStart();

			CodeTypeDeclarationCollection types = new CodeTypeDeclarationCollection();
			int iProcessCount = 0;
			if (entityMetadata.Count() >0)
			{
				_parameters.Logger.TraceVerbose($"Processing {entityMetadata.Count()} Entities");
				foreach (EntityMetadata entity in entityMetadata.OrderBy(metadata => metadata.LogicalName))
				{
					if (serviceProvider.CodeFilterService.GenerateEntity(entity, serviceProvider))
					{
						iProcessCount++;
						types.AddRange(BuildEntity(entity, serviceProvider, useLegacyMode));
					}
					else
					{
						_parameters.Logger.TraceVerbose("Skipping Entity {0} from being generated.", entity.LogicalName);
					}
				}
				_parameters.Logger.TraceVerbose($"Wrote {iProcessCount} Entities");
			}
			processedCount = iProcessCount;
			_parameters.Logger.TraceMethodStop();

			return types;
		}

		private static CodeTypeDeclarationCollection BuildEntity(EntityMetadata entity, ServiceProvider serviceProvider, bool useLegacyMode)
		{
			_parameters.Logger.TraceMethodStart();

			CodeTypeDeclarationCollection types = new CodeTypeDeclarationCollection();

			CodeTypeDeclaration entityClass = Class(serviceProvider.NamingService.GetNameForEntity(entity, serviceProvider), TypeRef(EntityClassBaseType),
				Attribute(typeof(DataContractAttribute)),
				Attribute(EntityLogicalNameAttribute, AttributeArg(entity.LogicalName)));

			InitializeEntityClass(entityClass, entity);

			CodeTypeMember attributeMember = null;

			foreach (var attribute in entity.Attributes.OrderBy(metadata => metadata.LogicalName))
			{
				if (serviceProvider.CodeFilterService.GenerateAttribute(attribute, serviceProvider))
				{
					attributeMember = BuildAttribute(entity, attribute, serviceProvider, useLegacyMode);
					entityClass.Members.Add(attributeMember);

					if (entity.PrimaryIdAttribute == attribute.LogicalName
						&& attribute.IsPrimaryId.GetValueOrDefault())
					{
						entityClass.Members.Add(BuildIdProperty(entity, attribute, serviceProvider));
					}
				}
				else
				{
					_parameters.Logger.TraceVerbose("Skipping {0}.Attribute {1} from being generated.", entity.LogicalName, attribute.LogicalName);
				}

				// handle setting up OptionSet In Class.
				if (attribute is EnumAttributeMetadata pickListMetadata)
				{
					ContainsEnums = true;
					CodeTypeDeclaration optionSetType = BuildAttributeOptionSet(entity, attribute, attributeMember, serviceProvider, useLegacyMode);
					if (optionSetType != null)
					{
						types.Add(optionSetType);
					}
				}
			}

			entityClass.Members.AddRange(BuildOneToManyRelationships(entity, serviceProvider));
			entityClass.Members.AddRange(BuildManyToManyRelationships(entity, serviceProvider));
			entityClass.Members.AddRange(BuildManyToOneRelationships(entity, serviceProvider));

            // Add Field List generation.
            if (_parameters.EmitFieldClasses)
            {
				var attributes = new HashSet<string>();
				var @class = new CodeTypeDeclaration
				{
					Name = AttributeConstsClassName,
					IsClass = true,
					TypeAttributes = TypeAttributes.Public,
					IsPartial = true,
					
				};
				@class.Comments.AddRange(CommentSummary($"Available fields, a the time of codegen, for the {entity.LogicalName} entity"));

				foreach (var member in from CodeTypeMember member in entityClass.Members
									   let prop = member as CodeMemberProperty
									   where prop != null
									   select prop)
				{
					CreateAttributeConstForProperty(@class, member, attributes);
				}

				if (attributes.Any())
				{
					entityClass.Members.Insert(0, GenerateTypeWithoutEmptyLines(@class));
				}

			}

			types.Add(entityClass);

			_parameters.Logger.TraceMethodStop();
			return types;
		}

		private static void InitializeEntityClass(CodeTypeDeclaration entityClass, EntityMetadata entity)
		{
			if (!_parameters.SuppressINotifyPattern)
			{
				entityClass.BaseTypes.Add(TypeRef(typeof(INotifyPropertyChanging)));
				entityClass.BaseTypes.Add(TypeRef(typeof(INotifyPropertyChanged)));
			}

			entityClass.Members.Add(EntityConstructor());
			entityClass.Members.Add(EntityLogicalNameConstant(entity));
			entityClass.Members.Add(EntityLogicalCollectionNameConstant(entity));
			entityClass.Members.Add(EntitySetNameConstant(entity));
			if (_parameters.EmitEntityETC && !_parameters.LegacyMode)
			{
                entityClass.Members.Add(EntityTypeCodeConstant(entity));
            }
			if (!_parameters.SuppressINotifyPattern)
			{
				entityClass.Members.Add(Event("PropertyChanged", typeof(PropertyChangedEventHandler), typeof(INotifyPropertyChanged)));
				entityClass.Members.Add(Event("PropertyChanging", typeof(PropertyChangingEventHandler), typeof(INotifyPropertyChanging)));
				entityClass.Members.Add(RaiseEvent("OnPropertyChanged", "PropertyChanged", typeof(PropertyChangedEventArgs)));
				entityClass.Members.Add(RaiseEvent("OnPropertyChanging", "PropertyChanging", typeof(PropertyChangingEventArgs)));
			}

			TryAddCodeComments(entityClass.Comments, entity.Description);
		}

		private static CodeTypeMember EntityLogicalNameConstant(EntityMetadata entity)
		{
			CodeMemberField constField = Field("EntityLogicalName", typeof(string), entity.LogicalName);
			constField.Attributes = MemberAttributes.Const | MemberAttributes.Public;
			return constField;
		}

		private static CodeTypeMember EntityLogicalCollectionNameConstant(EntityMetadata entity)
		{
			CodeMemberField constField = Field("EntityLogicalCollectionName", typeof(string), entity.LogicalCollectionName);
			constField.Attributes = MemberAttributes.Const | MemberAttributes.Public;
			return constField;
		}

		private static CodeTypeMember EntitySetNameConstant(EntityMetadata entity)
		{
			CodeMemberField constField = Field("EntitySetName", typeof(string), entity.EntitySetName);
			constField.Attributes = MemberAttributes.Const | MemberAttributes.Public;
			return constField;
		}

		private static CodeTypeMember EntityTypeCodeConstant(EntityMetadata entity)
		{
			CodeMemberField constField = Field("EntityTypeCode", typeof(int), entity.ObjectTypeCode.GetValueOrDefault());
			constField.Attributes = MemberAttributes.Const | MemberAttributes.Public;
			return constField;
		}

		private static CodeTypeMember EntityConstructor()
		{
			CodeConstructor ctor = Constructor();
			ctor.BaseConstructorArgs.Add(VarRef("EntityLogicalName"));
			ctor.Comments.AddRange(CommentSummary("Default Constructor."));
			return ctor;
		}

		private static CodeTypeMember BuildAttribute(EntityMetadata entity, AttributeMetadata attribute, ServiceProvider serviceProvider, bool useLegacyMode)
		{
			_parameters.Logger.TraceMethodStart();

			CodeTypeReference targetType = serviceProvider.TypeMappingService.GetTypeForAttributeType(entity, attribute, serviceProvider);

			CodeMemberProperty property = PropertyGet(targetType, serviceProvider.NamingService.GetNameForAttribute(entity, attribute, serviceProvider));
			property.HasSet = attribute.IsValidForCreate.GetValueOrDefault() || attribute.IsValidForUpdate.GetValueOrDefault();
			property.HasGet = attribute.IsValidForRead.GetValueOrDefault() || property.HasSet;
			if (property.HasGet)
			{
				property.GetStatements.AddRange(BuildAttributeGet(attribute, targetType));
			}
			if (property.HasSet)
			{
				property.SetStatements.AddRange(BuildAttributeSet(entity, attribute, property.Name));
			}

			property.CustomAttributes.Add(Attribute(AttributeLogicalNameAttribute, AttributeArg(attribute.LogicalName)));
			if (attribute.DeprecatedVersion != null)
			{
				property.CustomAttributes.Add(Attribute(ObsoleteFieldAttribute));
			}

			TryAddCodeComments(property.Comments, attribute.Description);

			if (!useLegacyMode)
			{
				// Override behavior for OptionSets when "default" behavior is on.
				if (TypeMappingService.GetAttributeOptionSet(attribute) != null && !(attribute is BooleanAttributeMetadata))
				{
					// this is an optionset type we care about.
					var generator = new Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.DefaultCustomziations.EnumPropertyGenerator(false, true, false, serviceProvider,_parameters);
					var dataOut = generator.GetOptionSetEnumType(property, entity.LogicalName);
					//string sdata = Utility.Utilites.DebugGenerateCodeFromMember(dataOut);
					property = dataOut;

				}
			}

			_parameters.Logger.TraceMethodStop();
			return property;
		}

		private static CodeStatementCollection BuildAttributeGet(AttributeMetadata attribute, CodeTypeReference targetType)
		{
			var statements = new CodeStatementCollection();

			if (attribute.AttributeType.GetValueOrDefault() == AttributeTypeCode.PartyList && targetType.TypeArguments.Count > 0)
			{
				statements.AddRange(BuildEntityCollectionAttributeGet(attribute.LogicalName, targetType));
			}
			else
			{
				// return this.GetAttributeValue<targetType>("attributeLogicalName");

				if (!Utilites.IsReadFromFormatedValues(attribute, _parameters))
				{
					statements.Add(Return(ThisMethodInvoke("GetAttributeValue", targetType, StringLiteral(attribute.LogicalName))));
                }
                else
                {
                    statements.Add(
						If(ContainsProperty("FormattedValues", attribute.AttributeOf),
						Return(PropertyIndexer("FormattedValues", attribute.AttributeOf)),
						Return(new CodeDefaultValueExpression(new CodeTypeReference(typeof(string))))
						));
                }
            }
            return statements;
        }

        private static CodeStatementCollection BuildAttributeSet(EntityMetadata entity, AttributeMetadata attribute, string propertyName)
        {
			var statements = new CodeStatementCollection();

			if (!_parameters.SuppressINotifyPattern)
			{
				// this.OnPropertyChanging(<propertyName>);
				statements.Add(ThisMethodInvoke("OnPropertyChanging", StringLiteral(propertyName)));
			}

			if (attribute.AttributeType.GetValueOrDefault() == AttributeTypeCode.PartyList)
			{
				statements.Add(BuildEntityCollectionAttributeSet(attribute.LogicalName));
			}
			else
			{
				// this.SetAttributeValue("attributeLogicalName", value);
				statements.Add(ThisMethodInvoke("SetAttributeValue", StringLiteral(attribute.LogicalName), VarRef("value")));
			}

			if (entity.PrimaryIdAttribute == attribute.LogicalName
				&& attribute.IsPrimaryId.GetValueOrDefault())
			{
				// if (value.HasValue)
				//		base.Id = value.Value;
				// else
				//		base.Id = System.Guid.Empty;

				statements.Add(If(PropRef(VarRef("value"), "HasValue"),
					AssignValue(BaseProp("Id"), PropRef(VarRef("value"), "Value")),
					AssignValue(BaseProp("Id"), GuidEmpty())));
			}

			if (!_parameters.SuppressINotifyPattern)
			{
				// this.OnPropertyChanged(<propertyName>);
				statements.Add(ThisMethodInvoke("OnPropertyChanged", StringLiteral(propertyName)));
			}
			return statements;
		}

		private static CodeStatementCollection BuildEntityCollectionAttributeGet(string attributeLogicalName, CodeTypeReference propertyType)
		{
			var statements = new CodeStatementCollection();

			// EntityCollection collection = this.GetAttributeValue<EntityCollection>("attributeLogicalName")
			// if (collection != null && collection.Entities != null)
			//     return System.Linq.Enumerable.Cast<T>(collection.Entities);
			// else
			//     return null;

			statements.Add(Var(typeof(EntityCollection), "collection", ThisMethodInvoke("GetAttributeValue", TypeRef(typeof(EntityCollection)), StringLiteral(attributeLogicalName))));
			statements.Add(If(And(NotNull(VarRef("collection")), NotNull(PropRef(VarRef("collection"), "Entities"))),
				Return(StaticMethodInvoke(typeof(Enumerable), "Cast", propertyType.TypeArguments[0], PropRef(VarRef("collection"), "Entities"))),
				Return(Null())));

			return statements;
		}

		private static CodeStatement BuildEntityCollectionAttributeSet(string attributeLogicalName)
		{
			// if (value == null)
			//     this.SetAttributeValue("attributeLogicalName", value);
			// else
			//     this.SetAttributeValue("attributeLogicalName", new Microsoft.Xrm.Sdk.EntityCollection(new System.Collections.Generic.List<Microsoft.Xrm.Sdk.Entity>(value)))

			return If(ValueNull(),
				ThisMethodInvoke("SetAttributeValue", StringLiteral(attributeLogicalName), VarRef("value")),
				ThisMethodInvoke("SetAttributeValue", StringLiteral(attributeLogicalName), New(TypeRef(typeof(EntityCollection)), New(TypeRef(typeof(List<Entity>)), VarRef("value")))));
		}

		private static CodeTypeMember BuildIdProperty(EntityMetadata entity, AttributeMetadata attribute, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			CodeMemberProperty property = PropertyGet(TypeRef(typeof(Guid)), "Id");
			property.CustomAttributes.Add(Attribute(AttributeLogicalNameAttribute, AttributeArg(attribute.LogicalName)));
			property.Attributes = MemberAttributes.Public | MemberAttributes.Override;
			property.HasSet = attribute.IsValidForCreate.GetValueOrDefault() || attribute.IsValidForUpdate.GetValueOrDefault();
			property.HasGet = attribute.IsValidForRead.GetValueOrDefault() || property.HasSet;

			// get { return base.Id; }
			property.GetStatements.Add(Return(BaseProp("Id")));

			if (property.HasSet)
			{
				// set { this.<primaryIdAttribute> = value; }
				property.SetStatements.Add(AssignValue(ThisProp(serviceProvider.NamingService.GetNameForAttribute(entity, attribute, serviceProvider)), VarRef("value")));
			}
			else
			{
				// set { base.Id = value; }
				property.SetStatements.Add(AssignValue(BaseProp("Id"), VarRef("value")));
			}

			_parameters.Logger.TraceMethodStop();
			return property;
		}

		/// <summary>
		/// Builds OptionSet Enum and returns that class.
		/// </summary>
		/// <param name="entity"></param>
		/// <param name="attribute"></param>
		/// <param name="attributeMember"></param>
		/// <param name="serviceProvider"></param>
		/// <param name="useLegacyMode"></param>
		/// <returns></returns>
		private static CodeTypeDeclaration BuildAttributeOptionSet(EntityMetadata entity, AttributeMetadata attribute, CodeTypeMember attributeMember, ServiceProvider serviceProvider, bool useLegacyMode)
		{
			_parameters.Logger.TraceMethodStart();

			OptionSetMetadataBase optionSet = TypeMappingService.GetAttributeOptionSet(attribute);
			if (optionSet == null || !serviceProvider.CodeFilterService.GenerateOptionSet(optionSet, serviceProvider))
			{
				if (optionSet != null)
					_parameters.Logger.TraceMethodStop();
				return null;
			}
			else
			{
				CodeTypeDeclaration typeDecl = BuildOptionSetEnumType(entity, optionSet, serviceProvider);
				if (typeDecl == null)
				{
					_parameters.Logger.TraceMethodStop();
					return null;
				}

				_parameters.Logger.TraceMethodStop();

				UpdateAttributeMemberStatements(entity, attribute, attributeMember , serviceProvider, useLegacyMode);

				return typeDecl;
			}
		}

		private static void UpdateAttributeMemberStatements(EntityMetadata entity, AttributeMetadata attribute, CodeTypeMember attributeMember , ServiceProvider serviceProvider, bool useLegacyMode)
		{
			if (attributeMember == null)
			{
				return;
			}

			if (useLegacyMode)
			{
				CodeMemberProperty attributeProperty = attributeMember as CodeMemberProperty;

				if (attributeProperty.HasGet)
				{
					attributeProperty.GetStatements.Clear();
					attributeProperty.GetStatements.AddRange(BuildOptionSetAttributeGet(attribute, attributeProperty.Type));
				}

				if (attributeProperty.HasSet)
				{
					attributeProperty.SetStatements.Clear();
					attributeProperty.SetStatements.AddRange(BuildOptionSetAttributeSet(attribute, attributeProperty.Name));
				}
			}
        }

        private static CodeStatementCollection BuildOptionSetAttributeGet(AttributeMetadata attribute, CodeTypeReference attributeType)
		{
			// var optionSet = this.GetAttributeValue<Microsoft.Xrm.Sdk.OptionSetValue>("attributeLogicalName");
			// if (optionSet != null)
			//     return (T)System.Enum.ToObject(typeof(T), optionSet.Value);
			// else
			//     return null;

			var optionSetType = attributeType;

			if (optionSetType.TypeArguments.Count > 0)
			{
				optionSetType = optionSetType.TypeArguments[0];
			}

			return new CodeStatementCollection(new CodeStatement[]
			{
				Var(typeof(OptionSetValue), "optionSet", ThisMethodInvoke("GetAttributeValue", TypeRef(typeof(OptionSetValue)), StringLiteral(attribute.LogicalName))),
				If(NotNull(VarRef("optionSet")),
					Return(Cast(optionSetType, ConvertEnum(optionSetType, "optionSet"))),
					Return(Null()))
			});
		}

		private static CodeStatementCollection BuildOptionSetAttributeSet(AttributeMetadata attribute, string propertyName)
		{
			CodeStatementCollection statements = new CodeStatementCollection();

			if (!_parameters.SuppressINotifyPattern)
			{
				statements.Add(ThisMethodInvoke("OnPropertyChanging", StringLiteral(propertyName)));
			}

			//if (value == null)
			//    this.SetAttributeValue("attributeLogicalName", null);
			//else
			//    this.SetAttributeValue("attributeLogicalName", new Microsoft.Xrm.Sdk.OptionSetValue((int)value));

			statements.Add(If(ValueNull(),
				ThisMethodInvoke("SetAttributeValue",
					StringLiteral(attribute.LogicalName),
					Null()),
				ThisMethodInvoke("SetAttributeValue",
					StringLiteral(attribute.LogicalName),
					//New(TypeRef(typeof(OptionSetValue)), Cast(TypeRef(typeof(int)), VarRef("value.Value")))
                    New(TypeRef(typeof(OptionSetValue)), Cast(TypeRef(typeof(int)), VarRef("value")))
					)));

			if (!_parameters.SuppressINotifyPattern)
			{
				statements.Add(ThisMethodInvoke("OnPropertyChanged", StringLiteral(propertyName)));
			}
			return statements;
		}

		private static CodeTypeMember BuildCalendarRuleAttribute(EntityMetadata entity, EntityMetadata otherEntity, OneToManyRelationshipMetadata oneToMany, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			var targetType = serviceProvider.TypeMappingService.GetTypeForRelationship(oneToMany, otherEntity, serviceProvider);

			var property = PropertyGet(IEnumerable(targetType), "CalendarRules");

			property.GetStatements.AddRange(BuildEntityCollectionAttributeGet("calendarrules", property.Type));

			if (!_parameters.SuppressINotifyPattern)
			{
				property.SetStatements.Add(ThisMethodInvoke("OnPropertyChanging", StringLiteral(property.Name)));
			}
			property.SetStatements.Add(BuildEntityCollectionAttributeSet("calendarrules"));
			if (!_parameters.SuppressINotifyPattern)
			{
				property.SetStatements.Add(ThisMethodInvoke("OnPropertyChanged", StringLiteral(property.Name)));
			}

			property.CustomAttributes.Add(Attribute(AttributeLogicalNameAttribute, AttributeArg("calendarrules")));

			property.Comments.AddRange(CommentSummary("1:N " + oneToMany.SchemaName));

			_parameters.Logger.TraceMethodStop();
			return property;
		}

		private static CodeTypeMemberCollection BuildOneToManyRelationships(EntityMetadata entity, ServiceProvider serviceProvider)
		{
			var relationships = new CodeTypeMemberCollection();
			if (entity.OneToManyRelationships == null)
				return relationships;

			foreach (var oneToMany in entity.OneToManyRelationships.OrderBy(metadata => metadata.SchemaName))
			{
				var otherEntityMetadata = GetEntityMetadata(oneToMany.ReferencingEntity, serviceProvider);

                if (otherEntityMetadata == null)
                {
                    _parameters.Logger.TraceVerbose("Skipping {0}.OneToMany {1} from being generated. Correlating entity not returned.", entity.LogicalName, oneToMany.SchemaName);
                    continue;
                }

				// special case for calendar rules, as they're attributes and not relationships.
				if (string.Equals(oneToMany.SchemaName, "calendar_calendar_rules", StringComparison.Ordinal) ||
					string.Equals(oneToMany.SchemaName, "service_calendar_rules", StringComparison.Ordinal))
				{
					relationships.Add(BuildCalendarRuleAttribute(entity, otherEntityMetadata, oneToMany, serviceProvider));
				}
				else if (serviceProvider.CodeFilterService.GenerateEntity(otherEntityMetadata, serviceProvider)
					&& serviceProvider.CodeFilterService.GenerateRelationship(oneToMany, otherEntityMetadata, serviceProvider))
				{
					relationships.Add(BuildOneToMany(entity, otherEntityMetadata, oneToMany, serviceProvider));
				}
				else
				{
					_parameters.Logger.TraceVerbose("Skipping {0}.OneToMany {1} from being generated.", entity.LogicalName, oneToMany.SchemaName);
				}
			}

			return relationships;
		}

		private static CodeTypeMember BuildOneToMany(EntityMetadata entity, EntityMetadata otherEntity, OneToManyRelationshipMetadata oneToMany, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			var targetType = serviceProvider.TypeMappingService.GetTypeForRelationship(oneToMany, otherEntity, serviceProvider);

			var entityRole = oneToMany.ReferencingEntity == entity.LogicalName
				? EntityRole.Referenced
				: (EntityRole?)null;

			var property = PropertyGet(IEnumerable(targetType), serviceProvider.NamingService.GetNameForRelationship(entity, oneToMany, entityRole, serviceProvider));

			property.GetStatements.Add(BuildRelationshipGet("GetRelatedEntities", oneToMany, targetType, entityRole));

			property.SetStatements.AddRange(BuildRelationshipSet("SetRelatedEntities", oneToMany, targetType, property.Name, entityRole));

			property.CustomAttributes.Add(BuildRelationshipSchemaNameAttribute(oneToMany.SchemaName, entityRole));

			property.Comments.AddRange(CommentSummary("1:N " + oneToMany.SchemaName));

			_parameters.Logger.TraceMethodStop();

			return property;
		}

		private static CodeTypeMemberCollection BuildManyToManyRelationships(EntityMetadata entity, ServiceProvider serviceProvider)
		{
			var relationships = new CodeTypeMemberCollection();
			if (entity.ManyToManyRelationships == null)
				return relationships;

			foreach (var manyToMany in entity.ManyToManyRelationships.OrderBy(metadata => metadata.SchemaName))
			{
				var otherEntityLogicalName = entity.LogicalName != manyToMany.Entity1LogicalName
					? manyToMany.Entity1LogicalName
					: manyToMany.Entity2LogicalName;

				var otherEntityMetadata = GetEntityMetadata(otherEntityLogicalName, serviceProvider);
                if (otherEntityMetadata == null)
                {
                    _parameters.Logger.TraceVerbose("Skipping {0}.ManyToMany {1} from being generated. Correlating entity not returned.", entity.LogicalName, manyToMany.SchemaName);
                    continue;
                }

				if (serviceProvider.CodeFilterService.GenerateEntity(otherEntityMetadata, serviceProvider)
					&& serviceProvider.CodeFilterService.GenerateRelationship(manyToMany, otherEntityMetadata, serviceProvider))
				{
					if (otherEntityMetadata.LogicalName != entity.LogicalName)
					{
						var propertyName = serviceProvider.NamingService.GetNameForRelationship(entity, manyToMany, null, serviceProvider);
						var manyToManyMember = BuildManyToMany(entity, otherEntityMetadata, manyToMany, propertyName, null, serviceProvider);
						relationships.Add(manyToManyMember);
					}
					else
					{
						var referencingPropertyName = serviceProvider.NamingService.GetNameForRelationship(entity, manyToMany, EntityRole.Referencing, serviceProvider);
						var referencingManyToManyMember = BuildManyToMany(entity, otherEntityMetadata, manyToMany, referencingPropertyName, EntityRole.Referencing, serviceProvider);
						relationships.Add(referencingManyToManyMember);

						var referencedPropertyName = serviceProvider.NamingService.GetNameForRelationship(entity, manyToMany, EntityRole.Referenced, serviceProvider);
						var referencedManyToManyMember = BuildManyToMany(entity, otherEntityMetadata, manyToMany, referencedPropertyName, EntityRole.Referenced, serviceProvider);
						relationships.Add(referencedManyToManyMember);
					}
				}
				else
				{
					_parameters.Logger.TraceVerbose("Skipping {0}.ManyToMany {1} from being generated.", entity.LogicalName, manyToMany.SchemaName);
				}
			}
			return relationships;
		}

		private static CodeTypeMember BuildManyToMany(EntityMetadata entity, EntityMetadata otherEntity, ManyToManyRelationshipMetadata manyToMany, string propertyName, EntityRole? entityRole, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			var targetType = serviceProvider.TypeMappingService.GetTypeForRelationship(manyToMany, otherEntity, serviceProvider);

			var property = PropertyGet(IEnumerable(targetType), propertyName);

			property.GetStatements.Add(BuildRelationshipGet("GetRelatedEntities", manyToMany, targetType, entityRole));

			property.SetStatements.AddRange(BuildRelationshipSet("SetRelatedEntities", manyToMany, targetType, propertyName, entityRole));

			property.CustomAttributes.Add(BuildRelationshipSchemaNameAttribute(manyToMany.SchemaName, entityRole));

			property.Comments.AddRange(CommentSummary("N:N " + manyToMany.SchemaName));

			_parameters.Logger.TraceMethodStop();

			return property;
		}

		private static CodeTypeMemberCollection BuildManyToOneRelationships(EntityMetadata entity, ServiceProvider serviceProvider)
		{
			CodeTypeMemberCollection relationships = new CodeTypeMemberCollection();
			if (entity.ManyToOneRelationships == null)
				return relationships;


			foreach (var manyToOne in entity.ManyToOneRelationships.OrderBy(metadata => metadata.SchemaName))
			{
				var otherEntityMetadata = GetEntityMetadata(manyToOne.ReferencedEntity, serviceProvider);
                if (otherEntityMetadata == null)
                {
                    _parameters.Logger.TraceVerbose("Skipping {0}.ManyToOne {1} from being generated. Correlating entity not returned.", entity.LogicalName, manyToOne.SchemaName);
                    continue;
                }

				if (serviceProvider.CodeFilterService.GenerateEntity(otherEntityMetadata, serviceProvider)
					&& serviceProvider.CodeFilterService.GenerateRelationship(manyToOne, otherEntityMetadata, serviceProvider))
				{
					var manyToOneMember = BuildManyToOne(entity, otherEntityMetadata, manyToOne, serviceProvider);
					if (manyToOneMember != null)
					{
						relationships.Add(manyToOneMember);
					}
				}
				else
				{
					_parameters.Logger.TraceVerbose("Skipping {0}.ManyToOne {1} from being generated.", entity.LogicalName, manyToOne.SchemaName);
				}
			}
			return relationships;
		}

		private static CodeTypeMember BuildManyToOne(EntityMetadata entity, EntityMetadata otherEntity, OneToManyRelationshipMetadata manyToOne, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			var targetType = serviceProvider.TypeMappingService.GetTypeForRelationship(manyToOne, otherEntity, serviceProvider);

			var entityRole = otherEntity.LogicalName == entity.LogicalName
				? EntityRole.Referencing
				: (EntityRole?)null;

			var property = PropertyGet(targetType, serviceProvider.NamingService.GetNameForRelationship(entity, manyToOne, entityRole, serviceProvider));

			property.GetStatements.Add(BuildRelationshipGet("GetRelatedEntity", manyToOne, targetType, entityRole));

			var referencingAttribute = entity.Attributes.SingleOrDefault(attribute => attribute.LogicalName == manyToOne.ReferencingAttribute);
			if (referencingAttribute == null)
			{
				_parameters.Logger.TraceMethodStop();
				return null;
			}

			if (referencingAttribute.IsValidForCreate.GetValueOrDefault() ||
				referencingAttribute.IsValidForUpdate.GetValueOrDefault())
			{
				property.SetStatements.AddRange(BuildRelationshipSet("SetRelatedEntity", manyToOne, targetType, property.Name, entityRole));
			}

			property.CustomAttributes.Add(Attribute(AttributeLogicalNameAttribute, AttributeArg(manyToOne.ReferencingAttribute)));
			property.CustomAttributes.Add(BuildRelationshipSchemaNameAttribute(manyToOne.SchemaName, entityRole));

			property.Comments.AddRange(CommentSummary("N:1 " + manyToOne.SchemaName));

			_parameters.Logger.TraceMethodStop();

			return property;
		}

		private static CodeStatement BuildRelationshipGet(string methodName, RelationshipMetadataBase relationship, CodeTypeReference targetType, EntityRole? entityRole)
		{
			// return this.[methodName]<targetType>("schemaName", entityRole);

			var entityRoleParameter = entityRole.HasValue
				? FieldRef(typeof(EntityRole), entityRole.ToString())
				: (CodeExpression)Null();

			return Return(ThisMethodInvoke(methodName, targetType,
				StringLiteral(relationship.SchemaName),
				entityRoleParameter));
		}

		private static CodeStatementCollection BuildRelationshipSet(string methodName, RelationshipMetadataBase relationship, CodeTypeReference targetType, string propertyName, EntityRole? entityRole)
		{
			CodeStatementCollection statements = new CodeStatementCollection();

			var entityRoleParameter = entityRole.HasValue
				? FieldRef(typeof(EntityRole), entityRole.ToString())
				: (CodeExpression)Null();

			if (!_parameters.SuppressINotifyPattern)
			{
				statements.Add(ThisMethodInvoke("OnPropertyChanging", StringLiteral(propertyName)));
			}


			// this.[methodName]<targetType>("schemaName", entityRole, value);
			statements.Add(ThisMethodInvoke(methodName, targetType,
				StringLiteral(relationship.SchemaName),
				entityRoleParameter,
				VarRef("value")));

			if (!_parameters.SuppressINotifyPattern)
			{
				statements.Add(ThisMethodInvoke("OnPropertyChanged", StringLiteral(propertyName)));
			}

			return statements;
		}

		private static CodeAttributeDeclaration BuildRelationshipSchemaNameAttribute(string relationshipSchemaName, EntityRole? entityRole)
		{
			if (entityRole.HasValue)
			{
				return Attribute(RelationshipSchemaNameAttribute, AttributeArg(relationshipSchemaName), AttributeArg(FieldRef(typeof(EntityRole), entityRole.ToString())));
			}

			return Attribute(RelationshipSchemaNameAttribute, AttributeArg(relationshipSchemaName));
		}

		private static EntityMetadata GetEntityMetadata(string entityLogicalName, ServiceProvider serviceProvider)
		{
			return serviceProvider.MetadataProviderService.LoadMetadata(serviceProvider).Entities.SingleOrDefault(e => e.LogicalName == entityLogicalName);
		}
		#endregion

		#region ServiceContext Generation Logic
		private static CodeTypeDeclarationCollection BuildServiceContext(EntityMetadata[] entityMetadata, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			var types = new CodeTypeDeclarationCollection();

			if (serviceProvider.CodeFilterService.GenerateServiceContext(serviceProvider))
			{
				var serviceContext = Class(serviceProvider.NamingService.GetNameForServiceContext(serviceProvider), ServiceContextBaseType);

				serviceContext.Members.Add(ServiceContextConstructor());

				serviceContext.Comments.AddRange(CommentSummary("Represents a source of entities bound to a Dataverse service. It tracks and manages changes made to the retrieved entities."));

				foreach (EntityMetadata entity in entityMetadata.OrderBy(metadata => metadata.LogicalName))
				{
					if (serviceProvider.CodeFilterService.GenerateEntity(entity, serviceProvider) &&
						!string.Equals(entity.LogicalName, "calendarrule", StringComparison.Ordinal))
					{
						serviceContext.Members.Add(BuildEntitySet(entity, serviceProvider));
					}
					else
					{
						_parameters.Logger.TraceVerbose("Skipping {0} entity set and AddTo method from being generated.", entity.LogicalName);
					}
				}

				types.Add(serviceContext);
			}
			else
			{
				_parameters.Logger.TraceVerbose("Skipping data context from being generated.");
			}

			_parameters.Logger.TraceMethodStop();

			return types;
		}

		private static CodeTypeMember ServiceContextConstructor()
		{
			var ctor = Constructor(Param(TypeRef(typeof(IOrganizationService)), "service"));
			ctor.BaseConstructorArgs.Add(VarRef("service"));
			ctor.Comments.AddRange(CommentSummary("Constructor."));
			return ctor;
		}

		private static CodeTypeMember BuildEntitySet(EntityMetadata entity, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			var targetType = serviceProvider.TypeMappingService.GetTypeForEntity(entity, serviceProvider);

			CodeMemberProperty property = PropertyGet(IQueryable(targetType),
				serviceProvider.NamingService.GetNameForEntitySet(entity, serviceProvider),
				Return(ThisMethodInvoke("CreateQuery", targetType)));

			property.Comments.AddRange(CommentSummary(string.Format(CultureInfo.InvariantCulture, @"Gets a binding to the set of all <see cref=""{0}""/> entities.", targetType.BaseType)));

			_parameters.Logger.TraceMethodStop();
			return property;
		}

		#endregion

		#region Request/Response Generation Logic
		private static CodeTypeDeclarationCollection BuildMessages(SdkMessages sdkMessages, ServiceProvider serviceProvider, out int processedCount)
		{
			_parameters.Logger.TraceMethodStart();

			CodeTypeDeclarationCollection types = new CodeTypeDeclarationCollection();
			int iProcessCount = 0;
			if (_parameters.GenerateSdkMessages) 
			{
				if (sdkMessages != null && sdkMessages.MessageCollection != null && sdkMessages.MessageCollection.Count > 0)
				{

					if (sdkMessages.MessageCollection.Count > 0)
					{
						foreach (SdkMessage message in sdkMessages.MessageCollection.Values)
						{
							if (serviceProvider.CodeMessageFilterService.GenerateSdkMessage(message, serviceProvider))
							{
								iProcessCount++;
								types.AddRange(BuildMessage(message, serviceProvider));
							}
							else
							{
								_parameters.Logger.TraceVerbose("Skipping SDK Message {0} from being generated.", message.Name);
							}
						}
						_parameters.Logger.TraceVerbose($"Wrote {iProcessCount} Messages");
					}
					else
					{
						_parameters.Logger.TraceVerbose("Skipping All SDK Messages from being generated as no SDK Messages were found.");
					}
				}
				else
				{
					_parameters.Logger.TraceVerbose("Skipping All SDK Messages from being generated as Messages and Custom Actions were not requested.");
				}
			}

			processedCount = iProcessCount;
			_parameters.Logger.TraceMethodStop();
			return types;
		}

		private static CodeTypeDeclarationCollection BuildMessage(SdkMessage message, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			CodeTypeDeclarationCollection types = new CodeTypeDeclarationCollection();

			foreach (SdkMessagePair pair in message.SdkMessagePairs.Values)
			{
				if (serviceProvider.CodeMessageFilterService.GenerateSdkMessagePair(pair, serviceProvider))
				{
					var reqMsg = BuildMessageRequest(pair, pair.Request, serviceProvider);
					var responseMsg = BuildMessageResponse(pair, pair.Response, serviceProvider);
					if (responseMsg != null && reqMsg != null)
                    {
						types.Add(reqMsg);
						types.Add(responseMsg);
					}
					else
                    {
						_parameters.Logger.TraceWarning("Skipping {0}.Message Pair from being generated. - Supporting Types Missing", message.Name,
								pair.Request.Name);
					}
				}
				else
				{
					_parameters.Logger.TraceVerbose("Skipping {0}.Message Pair from being generated.", message.Name,
						pair.Request.Name);
				}
			}

			_parameters.Logger.TraceMethodStop();

			return types;
		}

		private static CodeTypeDeclaration BuildMessageRequest(SdkMessagePair messagePair, SdkMessageRequest sdkMessageRequest, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			string requestName = String.Format(CultureInfo.InvariantCulture, "{0}{1}",
				serviceProvider.NamingService.GetNameForMessagePair(messagePair, serviceProvider),
				RequestClassSuffix);
			CodeTypeDeclaration requestClass = Class(
				requestName, RequestClassBaseType, Attribute(typeof(DataContractAttribute), AttributeArg("Namespace", messagePair.MessageNamespace)),
				Attribute(typeof(RequestProxyAttribute), AttributeArg(null, messagePair.Request.Name)));

			try
			{
				bool isGenericType = false;
				CodeStatementCollection ctorStatements = new CodeStatementCollection();
				if (sdkMessageRequest.RequestFields != null & sdkMessageRequest.RequestFields.Count > 0)
				{
					foreach (SdkMessageRequestField field in sdkMessageRequest.RequestFields.Values)
					{
						CodeMemberProperty requestField = BuildRequestField(sdkMessageRequest, field, serviceProvider);
						if (requestField.Type.Options == CodeTypeReferenceOptions.GenericTypeParameter)
						{
							_parameters.Logger.TraceVerbose("Request Field {0} is generic.  Adding generic parameter to the {1} class.",
								requestField.Name, requestClass.Name);
							isGenericType = true;
							ConvertRequestToGeneric(messagePair, requestClass, requestField);
						}

						requestClass.Members.Add(requestField);
						if (!field.IsOptional)
						{
							ctorStatements.Add(AssignProp(requestField.Name, new CodeDefaultValueExpression(requestField.Type)));
						}

					}
				}

				if (!isGenericType)
				{
					CodeConstructor ctor = Constructor();
					ctor.Statements.Add(AssignProp(RequestNamePropertyName, new CodePrimitiveExpression(messagePair.Request.Name)));
					ctor.Statements.AddRange(ctorStatements);
					requestClass.Members.Add(ctor);
				}
			}
			catch (MissingMemberException exMemMissing)
			{
				_parameters.Logger.TraceError(exMemMissing);
                _parameters.Logger.TraceMethodStop();
				return null;
			}

			_parameters.Logger.TraceMethodStop();

			return requestClass;
		}

		private static void ConvertRequestToGeneric(SdkMessagePair messagePair, CodeTypeDeclaration requestClass, CodeMemberProperty requestField)
		{
			CodeTypeParameter parameter = new CodeTypeParameter(requestField.Type.BaseType);
			parameter.HasConstructorConstraint = true;
			parameter.Constraints.Add(new CodeTypeReference(EntityClassBaseType));
			requestClass.TypeParameters.Add(parameter);

			requestClass.Members.Add(Constructor(New(requestField.Type)));
			CodeConstructor ctor = Constructor(Param(requestField.Type, "target"),
				AssignProp(requestField.Name, VarRef("target")));
			ctor.Statements.Add(AssignProp(RequestNamePropertyName, new CodePrimitiveExpression(messagePair.Request.Name)));
			requestClass.Members.Add(ctor);
		}


		private static CodeTypeDeclaration BuildMessageResponse(SdkMessagePair messagePair, SdkMessageResponse sdkMessageResponse, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();
			string responseName = String.Format(CultureInfo.InvariantCulture, "{0}{1}",
				serviceProvider.NamingService.GetNameForMessagePair(messagePair, serviceProvider),
				ResponseClassSuffix);
			CodeTypeDeclaration responseClass = Class(responseName, ResponseClassBaseType,
				Attribute(typeof(DataContractAttribute), AttributeArg("Namespace", messagePair.MessageNamespace)),
				Attribute(typeof(ResponseProxyAttribute), AttributeArg(null, messagePair.Request.Name)));
			responseClass.Members.Add(Constructor());
			try
			{

				if (sdkMessageResponse != null && sdkMessageResponse.ResponseFields != null & sdkMessageResponse.ResponseFields.Count > 0)
				{
					foreach (SdkMessageResponseField field in sdkMessageResponse.ResponseFields.Values)
					{
						responseClass.Members.Add(BuildResponseField(sdkMessageResponse, field, serviceProvider));
					}
				}
				else
				{
					_parameters.Logger.TraceVerbose("SDK Response Class {0} has not fields", responseClass.Name);
				}
			}
			catch ( MissingMemberException exMemMissing )
			{
				_parameters.Logger.TraceError(exMemMissing);
                _parameters.Logger.TraceMethodStop();
				return null;
			}

			_parameters.Logger.TraceMethodStop();

			return responseClass;
		}

		private static CodeMemberProperty BuildRequestField(SdkMessageRequest request, SdkMessageRequestField field, ServiceProvider serviceProvider)
		{
			_parameters.Logger.TraceMethodStart();

			CodeTypeReference targetType = serviceProvider.TypeMappingService.GetTypeForRequestField(field, serviceProvider);
			if (!targetType.BaseType.Equals("T", StringComparison.OrdinalIgnoreCase) && !ValidateTypeAvailable(request.Name, targetType.BaseType))
			{
				throw new MissingMemberException($"Type {targetType.BaseType} is not available");
			}

			CodeMemberProperty property = PropertyGet(targetType, serviceProvider.NamingService.GetNameForRequestField(request, field, serviceProvider));
			property.HasSet = true;
			property.HasGet = true;
			property.GetStatements.Add(BuildRequestFieldGetStatement(field, targetType));
			property.SetStatements.Add(BuildRequestFieldSetStatement(field));

			_parameters.Logger.TraceMethodStop();

			return property;
		}

		private static CodeStatement BuildRequestFieldGetStatement(SdkMessageRequestField field, CodeTypeReference targetType)
		{
			return If(ContainsParameter(field.Name),
				Return(Cast(targetType, PropertyIndexer(ParametersPropertyName, field.Name))),
				Return(new CodeDefaultValueExpression(targetType)));
		}

		private static CodeAssignStatement BuildRequestFieldSetStatement(SdkMessageRequestField field)
		{
			return AssignValue(PropertyIndexer(ParametersPropertyName, field.Name));
		}


		private static CodeMemberProperty BuildResponseField(SdkMessageResponse response, SdkMessageResponseField field, ServiceProvider serviceProvider)
        {
            _parameters.Logger.TraceMethodStart();

            CodeTypeReference targetType = serviceProvider.TypeMappingService.GetTypeForResponseField(field, serviceProvider);

            if (!ValidateTypeAvailable(response.Id.ToString(), targetType.BaseType))
            {
				throw new MissingMemberException($"Type {targetType.BaseType} is not available");
            }

            CodeMemberProperty property = PropertyGet(targetType, serviceProvider.NamingService.GetNameForResponseField(response, field, serviceProvider));
            property.HasSet = false;
            property.HasGet = true;
            property.GetStatements.Add(BuildResponseFieldGetStatement(field, targetType));

            _parameters.Logger.TraceMethodStop();

            return property;
        }

        private static bool ValidateTypeAvailable(string MessageName, string targetType)
        {
            var v = Type.GetType(targetType);
            if (v is null)
            {
                var s = Assembly.GetAssembly(typeof(Microsoft.Crm.Sdk.Messages.AccessRights)).GetTypes().Where(w => w.FullName.Equals(targetType));
                if (!s.Any())
                {
                    s = Assembly.GetAssembly(typeof(Microsoft.Xrm.Sdk.Messages.AssociateRequest)).GetTypes().Where(w => w.FullName.Equals(targetType));
                    if (!s.Any())
                    {
						return false;
                    }
                }
            }
			return true;
        }

        private static CodeStatement BuildResponseFieldGetStatement(SdkMessageResponseField field, CodeTypeReference targetType)
		{
			//return (T)this.Parameters[(attributelogicalname}];
			return If(ContainsResult(field.Name),
				Return(Cast(targetType, PropertyIndexer(ResultsPropertyName, field.Name))),
				Return(new CodeDefaultValueExpression(targetType)));
		}

		#endregion
		#endregion

		#region CodeDom Helpers
		private static CodeNamespace Namespace(string name)
		{
			return new CodeNamespace(name);
		}

		private static CodeTypeDeclaration Class(string name, Type baseType, params CodeAttributeDeclaration[] attrs)
		{
			return Class(name, TypeRef(baseType), attrs);
		}

		private static CodeTypeDeclaration Class(string name, CodeTypeReference baseType, params CodeAttributeDeclaration[] attrs)
		{
			CodeTypeDeclaration ctd = new CodeTypeDeclaration(name);
			ctd.IsClass = true;
			ctd.TypeAttributes = TypeAttributes.Class | TypeAttributes.Public;
			ctd.BaseTypes.Add(baseType);
			if (attrs != null)
				ctd.CustomAttributes.AddRange(attrs);
			ctd.IsPartial = true;

			if (!_parameters.SuppressGenVersionAttribute)
			{
				ctd.CustomAttributes.Add(Attribute(typeof(GeneratedCodeAttribute),
					AttributeArg(StaticUtils.ApplicationName), AttributeArg(StaticUtils.ApplicationVersion)));
			}

			return ctd;
		}

		private static CodeTypeDeclaration Enum(string name, params CodeAttributeDeclaration[] attrs)
		{
			CodeTypeDeclaration ctd = new CodeTypeDeclaration(name);
			ctd.IsEnum = true;
			ctd.TypeAttributes = TypeAttributes.Public;
			if (attrs != null)
				ctd.CustomAttributes.AddRange(attrs);

			if (!_parameters.SuppressGenVersionAttribute)
			{
				ctd.CustomAttributes.Add(Attribute(typeof(GeneratedCodeAttribute),
					AttributeArg(StaticUtils.ApplicationName), AttributeArg(StaticUtils.ApplicationVersion)));
			}

			return ctd;
		}

		private static CodeTypeReference TypeRef(Type type)
		{
			return new CodeTypeReference(type);
		}

		private static CodeAttributeDeclaration Attribute(Type type)
		{
			return new CodeAttributeDeclaration(TypeRef(type));
		}

		private static CodeAttributeDeclaration Attribute(Type type, params CodeAttributeArgument[] args)
		{
			return new CodeAttributeDeclaration(TypeRef(type), args);
		}

		private static CodeAttributeArgument AttributeArg(object value)
		{
			CodeExpression codeExpression = value as CodeExpression;
			if (codeExpression != null)
				return AttributeArg(null, codeExpression);
			else
				return AttributeArg(null, value);
		}

		private static CodeAttributeArgument AttributeArg(string name, object value)
		{
			return AttributeArg(name, new CodePrimitiveExpression(value));
		}

		private static CodeAttributeArgument AttributeArg(string name, CodeExpression value)
		{
			return String.IsNullOrEmpty(name)
				? new CodeAttributeArgument(value)
				: new CodeAttributeArgument(name, value);
		}

		private static CodeMemberProperty PropertyGet(CodeTypeReference type, string name, params CodeStatement[] stmts)
		{
			CodeMemberProperty prop = new CodeMemberProperty();
			prop.Type = type;
			prop.Name = name;
			prop.Attributes = MemberAttributes.Public | MemberAttributes.Final;
			prop.HasGet = true;
			prop.HasSet = false;
			prop.GetStatements.AddRange(stmts);
			return prop;
		}

		private static CodeMemberEvent Event(string name, Type type, Type implementationType)
		{
			CodeMemberEvent eventDef = new CodeMemberEvent();
			eventDef.Name = name;
			eventDef.Type = TypeRef(type);
			eventDef.Attributes = MemberAttributes.Public | MemberAttributes.Final;

			if (implementationType != null)
			{
				eventDef.ImplementationTypes.Add(TypeRef(implementationType));
			}

			return eventDef;
		}

		private static CodeMemberMethod RaiseEvent(string methodName, string eventName, Type eventArgsType)
		{
			var method = new CodeMemberMethod { Name = methodName };

			method.Parameters.Add(Param(TypeRef(typeof(string)), "propertyName"));

			var eventRef = new CodeEventReferenceExpression(This(), eventName);

			method.Statements.Add(If(
				NotNull(eventRef),
				new CodeDelegateInvokeExpression(eventRef, This(), New(TypeRef(eventArgsType), VarRef("propertyName")))));

			return method;
		}
        private static CodeMethodInvokeExpression ContainsProperty(string propertyName, string parameterName)
        {
            return new CodeMethodInvokeExpression(ThisProp(propertyName), "Contains", StringLiteral(parameterName));
        }

        private static CodeMethodInvokeExpression ContainsParameter(string parameterName)
		{
			return new CodeMethodInvokeExpression(ThisProp(ParametersPropertyName), "Contains", StringLiteral(parameterName));
		}

		private static CodeMethodInvokeExpression ContainsResult(string resultName)
		{
			return new CodeMethodInvokeExpression(ThisProp(ResultsPropertyName), "Contains", StringLiteral(resultName));
		}

		private static CodeConditionStatement If(CodeExpression condition, CodeExpression trueCode)
		{
			return If(condition, new CodeExpressionStatement(trueCode), null);
		}

		private static CodeConditionStatement If(CodeExpression condition, CodeExpression trueCode, CodeExpression falseCode)
		{
			return If(condition, new CodeExpressionStatement(trueCode), new CodeExpressionStatement(falseCode));
		}

		private static CodeConditionStatement If(CodeExpression condition, CodeStatement trueStatement)
		{
			return If(condition, trueStatement, null);
		}

		private static CodeConditionStatement If(CodeExpression condition, CodeStatement trueStatement, CodeStatement falseStatement)
		{
			CodeConditionStatement ifStatement = new CodeConditionStatement(condition, trueStatement);
			if (falseStatement != null)
				ifStatement.FalseStatements.Add(falseStatement);
			return ifStatement;
		}

		private static CodeFieldReferenceExpression FieldRef(Type targetType, string fieldName)
		{
			return new CodeFieldReferenceExpression(new CodeTypeReferenceExpression(targetType), fieldName);
		}

		private static CodeMemberField Field(string name, Type type, object value, params CodeAttributeDeclaration[] attrs)
		{
			CodeMemberField field = new CodeMemberField(type, name);
			field.InitExpression = new CodePrimitiveExpression(value);
			if (attrs != null)
				field.CustomAttributes.AddRange(attrs);
			return field;
		}

		private static CodeParameterDeclarationExpression Param(CodeTypeReference type, string name)
		{
			return new CodeParameterDeclarationExpression(type, name);
		}

		private static CodeTypeParameter TypeParam(string name, params Type[] constraints)
		{
			var typeParam = new CodeTypeParameter(name);
			if (constraints != null)
				typeParam.Constraints.AddRange(constraints.Select(TypeRef).ToArray());
			return typeParam;
		}

		private static CodeVariableReferenceExpression VarRef(string name)
		{
			return new CodeVariableReferenceExpression(name);
		}

		private static CodeVariableDeclarationStatement Var(Type type, string name, CodeExpression init)
		{
			return new CodeVariableDeclarationStatement(type, name, init);
		}

		private static CodeConstructor Constructor(params CodeExpression[] thisArgs)
		{
			CodeConstructor ctor = new CodeConstructor();
			ctor.Attributes = MemberAttributes.Public;
			if (thisArgs != null)
				ctor.ChainedConstructorArgs.AddRange(thisArgs);
			return ctor;
		}

		private static CodeConstructor Constructor(CodeParameterDeclarationExpression arg, params CodeStatement[] statements)
		{
			CodeConstructor ctor = new CodeConstructor();
			ctor.Attributes = MemberAttributes.Public;
			if (arg != null)
				ctor.Parameters.Add(arg);
			if (statements != null)
				ctor.Statements.AddRange(statements);
			return ctor;
		}

		private static CodeObjectCreateExpression New(CodeTypeReference createType, params CodeExpression[] args)
		{
			CodeObjectCreateExpression create = new CodeObjectCreateExpression(createType, args);
			return create;
		}

		private static CodeAssignStatement AssignProp(string propName, CodeExpression value)
		{
			CodeAssignStatement assign = new CodeAssignStatement();
			assign.Left = ThisProp(propName);
			assign.Right = value;
			return assign;
		}

		private static CodeAssignStatement AssignValue(CodeExpression target)
		{
			return AssignValue(target, new CodeVariableReferenceExpression("value"));
		}

		private static CodeAssignStatement AssignValue(CodeExpression target, CodeExpression value)
		{
			return new CodeAssignStatement(target, value);
		}

		private static CodePropertyReferenceExpression BaseProp(string propertyName)
		{
			return new CodePropertyReferenceExpression(new CodeBaseReferenceExpression(), propertyName);
		}

		private static CodeIndexerExpression PropertyIndexer(string propertyName, string index)
		{
			return new CodeIndexerExpression(ThisProp(propertyName), new CodePrimitiveExpression(index));
		}

		private static CodePropertyReferenceExpression PropRef(CodeExpression expression, string propertyName)
		{
			return new CodePropertyReferenceExpression(expression, propertyName);
		}

		private static CodePropertyReferenceExpression ThisProp(string propertyName)
		{
			return new CodePropertyReferenceExpression(This(), propertyName);
		}

		private static CodeThisReferenceExpression This()
		{
			return new CodeThisReferenceExpression();
		}

		private static CodeMethodInvokeExpression ThisMethodInvoke(string methodName, params CodeExpression[] parameters)
		{
			return new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(This(), methodName), parameters);
		}

		private static CodeMethodInvokeExpression ThisMethodInvoke(string methodName, CodeTypeReference typeParameter, params CodeExpression[] parameters)
		{
			return new CodeMethodInvokeExpression(new CodeMethodReferenceExpression(This(), methodName, typeParameter), parameters);
		}

		private static CodeMethodInvokeExpression StaticMethodInvoke(Type targetObject, string methodName, params CodeExpression[] parameters)
		{
			return new CodeMethodInvokeExpression(
				new CodeMethodReferenceExpression(
					new CodeTypeReferenceExpression(targetObject),
					methodName),
				parameters);
		}

		private static CodeMethodInvokeExpression StaticMethodInvoke(Type targetObject, string methodName, CodeTypeReference typeParameter, params CodeExpression[] parameters)
		{
			return new CodeMethodInvokeExpression(
				new CodeMethodReferenceExpression(
					new CodeTypeReferenceExpression(targetObject),
					methodName,
					typeParameter),
				parameters);
		}

		private static CodeCastExpression Cast(CodeTypeReference targetType, CodeExpression expression)
		{
			return new CodeCastExpression(targetType, expression);
		}

		private static void TryAddCodeComments (CodeCommentStatementCollection commentCollection , Label comment)
        {
			var cmpts = CommentSummary(comment);
			if (cmpts != null)
				commentCollection.AddRange(cmpts);
		}

		private static CodeCommentStatementCollection CommentSummary(Label comment)
		{
			string descriptionText = comment.UserLocalizedLabel != null
					? comment.UserLocalizedLabel.Label
					: comment.LocalizedLabels.Any()
						? comment.LocalizedLabels.First().Label
						: string.Empty;

			if (string.IsNullOrEmpty(descriptionText))
				return null;


			return CommentSummary(descriptionText);
		}

		private static CodeCommentStatementCollection CommentSummary(string comment)
		{
			return new CodeCommentStatementCollection
			{
				new CodeCommentStatement("<summary>", true),
				new CodeCommentStatement(comment, true),
				new CodeCommentStatement("</summary>", true)
			};
		}

		private static CodePrimitiveExpression StringLiteral(string value)
		{
			return new CodePrimitiveExpression(value);
		}

		private static CodeMethodReturnStatement Return()
		{
			return new CodeMethodReturnStatement();
		}

		private static CodeMethodReturnStatement Return(CodeExpression expression)
		{
			return new CodeMethodReturnStatement(expression);
		}

		private static CodeMethodInvokeExpression ConvertEnum(CodeTypeReference type, string variableName)
		{
			return new CodeMethodInvokeExpression(
				new CodeTypeReferenceExpression(TypeRef(typeof(Enum))), "ToObject",
				new CodeTypeOfExpression(type),
				new CodePropertyReferenceExpression(VarRef(variableName), "Value"));
		}

		private static CodeExpression ValueNull()
		{
			return new CodeBinaryOperatorExpression(VarRef("value"), CodeBinaryOperatorType.IdentityEquality, Null());
		}


		private static CodePrimitiveExpression Null()
		{
			return new CodePrimitiveExpression(null);
		}

		private static CodeBinaryOperatorExpression NotNull(CodeExpression expression)
		{
			return new CodeBinaryOperatorExpression(expression, CodeBinaryOperatorType.IdentityInequality, Null());
		}

		private static CodeExpression GuidEmpty()
		{
			return PropRef(new CodeTypeReferenceExpression(typeof(Guid)), "Empty");
		}

		private static CodeExpression False()
		{
			return new CodePrimitiveExpression(false);
		}

		private static CodeTypeReference IEnumerable(CodeTypeReference typeParameter)
		{
			return new CodeTypeReference(typeof(IEnumerable<>).FullName, typeParameter);
		}

		private static CodeTypeReference IQueryable(CodeTypeReference typeParameter)
		{
			return new CodeTypeReference(typeof(IQueryable<>).FullName, typeParameter);
		}

		private static CodeThrowExceptionStatement ThrowArgumentNull(string paramName)
		{
			return new CodeThrowExceptionStatement(New(TypeRef(typeof(ArgumentNullException)), StringLiteral(paramName)));
		}

		private static CodeBinaryOperatorExpression Or(CodeExpression left, CodeExpression right)
		{
			return new CodeBinaryOperatorExpression(left, CodeBinaryOperatorType.BooleanOr, right);
		}

		private static CodeBinaryOperatorExpression Equal(CodeExpression left, CodeExpression right)
		{
			return new CodeBinaryOperatorExpression(left, CodeBinaryOperatorType.IdentityEquality, right);
		}

		private static CodeBinaryOperatorExpression And(CodeExpression left, CodeExpression right)
		{
			return new CodeBinaryOperatorExpression(left, CodeBinaryOperatorType.BooleanAnd, right);
		}


		private const string RemovalString = "9C8F3879-309D-4DB2-B138-3F2E3A462A1C";
		private static string AttributeConstsClassName => "Fields";//ConfigHelper.GetAppSettingOrDefault("AttributeConstsClassName", "Fields");
		private const string XrmAttributeLogicalName = "Microsoft.Xrm.Sdk.AttributeLogicalNameAttribute";
		private const string XrmRelationshipSchemaName = "Microsoft.Xrm.Sdk.RelationshipSchemaNameAttribute";


		private static void CreateAttributeConstForProperty(CodeTypeDeclaration type, CodeMemberProperty prop, HashSet<string> attributes)
		{
			AddAttributeConstIfNotExists(type, prop.Name, GetAttributeConstantLogicalName(prop), attributes);
		}

		private static int AddAttributeConstIfNotExists(CodeTypeDeclaration type, string name, string attributeLogicalName, HashSet<string> attributes)
		{
			if (attributeLogicalName == null)
			{
				return -1;
			}

            // Handle Removal of characters as specified by the attribute logical name (used for N:N relationships
            if (attributeLogicalName.Contains(RemovalString))
            {
                var parts = attributeLogicalName.Split(new[] { RemovalString }, StringSplitOptions.None);
                attributeLogicalName = parts[1];
                name = name.Substring(parts[0].Length);
            }

            if (attributes.Contains(name))
				return -1;

			attributes.Add(name);
			return type.Members.Add(new CodeMemberField
			{
				Attributes = MemberAttributes.Public | MemberAttributes.Const,
				Name = name,
				Type = new CodeTypeReference(typeof(string)),
				InitExpression = new CodePrimitiveExpression(attributeLogicalName)
			});
		}


		/// <summary>
		/// Generate the Constant name for an attribute property.
		/// </summary>
		/// <param name="prop"></param>
		/// <returns></returns>
		private static string GetAttributeConstantLogicalName(CodeMemberProperty prop)
		{
			var info = (from CodeAttributeDeclaration att in prop.CustomAttributes
						where IsConstGeneratingAttribute(prop, att)
						select new
						{
							FieldName = ((CodePrimitiveExpression)att.Arguments[0].Value).Value.ToString(),
							Order = att.AttributeType.BaseType == XrmRelationshipSchemaName ? 0 : 1,
							Att = att
						})
				.OrderBy(a => a.Order)
				.FirstOrDefault();

			return info == null
				? prop.Name
				: GenerateAttributeLogicalName(info.FieldName, prop, info.Att);
		}

        //protected override string GetAttributeLogicalName(CodeMemberProperty prop)
        //{
        //	return prop.Name;
        //}

		/// <summary>
		/// Determine if this should be generated for the constants lists.
		/// </summary>
		/// <param name="prop"></param>
		/// <param name="att"></param>
		/// <returns></returns>
        private static bool IsConstGeneratingAttribute(CodeMemberProperty prop, CodeAttributeDeclaration att)
        {
			bool found = IsManyToMany(prop, att);
			if (!found)
            {
				found = att.AttributeType.BaseType == XrmAttributeLogicalName
				   || HasAttributeAndRelationship(prop, att);
			}

			return found;
		}

        private static string GenerateAttributeLogicalName(string fieldName, CodeMemberProperty prop, CodeAttributeDeclaration att)
		{
			return GetManyToManyName(fieldName, prop, att, "Referencing")
				   ?? GetManyToManyName(fieldName, prop, att, "Referenced")
				   ?? fieldName;
		}


		private static bool IsManyToMany(CodeTypeMember prop, CodeAttributeDeclaration att)
		{
			return att.AttributeType.BaseType == XrmRelationshipSchemaName
				   && prop.Comments?.Count > 1
				   && prop.Comments[1]?.Comment?.Text?.Trim().StartsWith("N:N ") == true;
		}

		private static string GetManyToManyName(string name, CodeTypeMember prop, CodeAttributeDeclaration att, string @ref)
		{
			return prop.Name.StartsWith(@ref)
				   && IsManyToMany(prop, att)
				? @ref + RemovalString + name
				: null;
		}

		private static bool HasAttributeAndRelationship(CodeMemberProperty prop, CodeAttributeDeclaration att)
		{
			return att?.AttributeType.BaseType == XrmRelationshipSchemaName
				&& prop.CustomAttributes.Cast<CodeAttributeDeclaration>().Any(a => a.AttributeType.BaseType == XrmAttributeLogicalName);
		}

		/// <summary>
		/// Removes the blank lines spaces by generating the code as a string without BlankLinesBetweenMembers
		/// </summary>
		/// <param name="type">The type.</param>
		/// <returns></returns>
		private static CodeSnippetTypeMember GenerateTypeWithoutEmptyLines(CodeTypeDeclaration type)
		{
			var provider = CodeDomProvider.CreateProvider("CSharp");  // Modify for VB / JSON
			using (var sourceWriter = new StringWriter())
			using (var tabbedWriter = new IndentedTextWriter(sourceWriter, "\t"))
			{
				tabbedWriter.Indent = 2;
				provider.GenerateCodeFromType(type, tabbedWriter, new CodeGeneratorOptions
				{
					BracingStyle = "C",
					IndentString = "\t",
					BlankLinesBetweenMembers = false
				});
				var stringSource = sourceWriter.ToString().Replace("public class", "public static class");
				var lastNewLine = stringSource.LastIndexOf(Environment.NewLine, StringComparison.Ordinal);
				if (lastNewLine >= 0)
				{
					stringSource = stringSource.Remove(lastNewLine);
				}
				return new CodeSnippetTypeMember("\t\t" + stringSource);
			}
		}

		#endregion
	}
}
