using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Utility;
using Microsoft.Xrm.Sdk;
using Microsoft.Xrm.Sdk.Metadata;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    internal sealed class TypeMappingService : ITypeMappingService
	{
		#region Fields
		private Dictionary<AttributeTypeCode, Type> _attributeTypeCodeMapping;
		private Dictionary<Type, Type> _attributeTypeMapping;
		private string _namespace;
		ModelBuilderInvokeParameters _parameters;
        #endregion

        #region Constructors
        internal TypeMappingService(ModelBuilderInvokeParameters parameters)
		{
            _parameters = parameters;
            _namespace = parameters.Namespace;

            _attributeTypeCodeMapping = new Dictionary<AttributeTypeCode, Type>
            {
                { AttributeTypeCode.Boolean, typeof(bool) },
                { AttributeTypeCode.ManagedProperty, typeof(BooleanManagedProperty) },
                { AttributeTypeCode.CalendarRules, typeof(object) },
                { AttributeTypeCode.Customer, typeof(EntityReference) },
                { AttributeTypeCode.DateTime, typeof(DateTime) },
                { AttributeTypeCode.Decimal, typeof(decimal) },
                { AttributeTypeCode.Double, typeof(double) },
                { AttributeTypeCode.Integer, typeof(int) },
                { AttributeTypeCode.EntityName, typeof(string) },
                { AttributeTypeCode.BigInt, typeof(Int64) },
                { AttributeTypeCode.Lookup, typeof(EntityReference) },
                { AttributeTypeCode.Memo, typeof(string) },
                { AttributeTypeCode.Money, typeof(Money) },
                { AttributeTypeCode.Owner, typeof(EntityReference) },
                // AttributeType.PartyList handled in logic directly
                // { AttributeTypeCode.Picklist, typeof(OptionSetValue) },
                // AttributeType.State handled in logic directly
                // { AttributeTypeCode.Status, typeof(OptionSetValue) },
                { AttributeTypeCode.String, typeof(string) },
                { AttributeTypeCode.Uniqueidentifier, typeof(Guid) }
            };

            // TODO: AttributeType.Virtual columns need to be added to _attributeTypeMapping
            _attributeTypeMapping = new Dictionary<Type, Type>
            {
                { typeof(ImageAttributeMetadata), typeof(byte[]) },
                { typeof(FileAttributeMetadata), typeof(Guid) }
            };
        }
		#endregion

		#region Properties
		private string Namespace
		{
			get { return _namespace; }
		}
		#endregion

		#region ITypeMappingService Members
		CodeTypeReference ITypeMappingService.GetTypeForEntity(EntityMetadata entityMetadata, IServiceProvider services)
		{
			var namingService = (INamingService)services.GetService(typeof(INamingService));

			var typeName = namingService.GetNameForEntity(entityMetadata, services);

			return TypeRef(typeName);
		}

		CodeTypeReference ITypeMappingService.GetTypeForAttributeType(EntityMetadata entityMetadata, AttributeMetadata attributeMetadata, IServiceProvider services)
		{
			Type targetType = typeof(object);

			if (Utilites.IsReadFromFormattedValues(entityMetadata, attributeMetadata, _parameters))
				return TypeRef(typeof(string)); // this will create a 'formattedvalue' property. Return this as a string.


            if (attributeMetadata.AttributeType != null)
			{
				AttributeTypeCode attributeTypeCode = attributeMetadata.AttributeType.Value;
				if (_attributeTypeCodeMapping.TryGetValue(attributeTypeCode, out var typeForAttributeTypeCode))
				{
					targetType = typeForAttributeTypeCode;
				}
				else if (_attributeTypeMapping.TryGetValue(typeof(AttributeMetadata), out var typeForAttributeType))
                {
					targetType = typeForAttributeType;
                }
                else if (attributeTypeCode == AttributeTypeCode.PartyList)
				{
					return this.BuildCodeTypeReferenceForPartyList(services);
				}
				else
				{
					OptionSetMetadataBase attributeOptionSet = GetAttributeOptionSet(attributeMetadata);
					if (attributeOptionSet != null)
					{
                        var result = this.BuildCodeTypeReferenceForOptionSet(attributeMetadata.LogicalName, entityMetadata, attributeOptionSet, services);
                        if ( result.BaseType.Equals("System.Object"))  // Handle fall though of Option Sets where no matching enum is present. 
                        {
                            if (attributeTypeCode.Equals(AttributeTypeCode.Picklist) || attributeTypeCode.Equals(AttributeTypeCode.Status))
                            {
                                targetType = typeof(OptionSetValue);
                                if (targetType.IsValueType)
                                {
                                    targetType = typeof(Nullable<>).MakeGenericType(targetType);
                                }
                            }
                        }
                        else
                        {
                            return result; 
                        }
					}
				}

				if (targetType.IsValueType)
				{
					targetType = typeof(Nullable<>).MakeGenericType(targetType);
				}
			}

			return TypeRef(targetType);
		}

		CodeTypeReference ITypeMappingService.GetTypeForRelationship(RelationshipMetadataBase relationshipMetadata, EntityMetadata otherEntityMetadata, IServiceProvider services)
		{
			var namingService = (INamingService)services.GetService(typeof(INamingService));

			var typeName = namingService.GetNameForEntity(otherEntityMetadata, services);

			return TypeRef(typeName);
		}


		CodeTypeReference ITypeMappingService.GetTypeForRequestField(SdkMessageRequestField requestField, IServiceProvider services)
		{
			return GetTypeForField(requestField.CLRFormatter, requestField.IsGeneric);
		}

		CodeTypeReference ITypeMappingService.GetTypeForResponseField(SdkMessageResponseField responseField, IServiceProvider services)
		{
			return GetTypeForField(responseField.CLRFormatter, false);
		}
		#endregion

		#region Helper Methods
		private CodeTypeReference BuildCodeTypeReferenceForOptionSet(string attributeName, EntityMetadata entityMetadata, OptionSetMetadataBase attributeOptionSet, IServiceProvider services)
		{
			var filterService = (ICodeWriterFilterService)services.GetService(typeof(ICodeWriterFilterService));
			var namingService = (INamingService)services.GetService(typeof(INamingService));
			var generationSevice = (ICodeGenerationService)services.GetService(typeof(ICodeGenerationService));

			if (filterService.GenerateOptionSet(attributeOptionSet, services))
			{
				var typeName = namingService.GetNameForOptionSet(entityMetadata, attributeOptionSet, services);

				var optionSetType = generationSevice.GetTypeForOptionSet(entityMetadata, attributeOptionSet, services);

				if (optionSetType == CodeGenerationType.Class)
				{
					return TypeRef(typeName);
				}

				if ((optionSetType == CodeGenerationType.Enum) || (optionSetType == CodeGenerationType.Struct))
				{
					return TypeRef(typeof(Nullable<>), TypeRef(typeName));
				}

				_parameters.Logger.TraceWarning("Cannot map type for attribute {0} with OptionSet type {1} which has CodeGenerationType {2}",
					attributeName, attributeOptionSet.Name, optionSetType);
			}

			return TypeRef(typeof(object));
		}

		private CodeTypeReference BuildCodeTypeReferenceForPartyList(IServiceProvider services)
		{
			// TODO: Refactor MetadataProvider to have lookups based on name/id
            var activityPartyMetadata = (EntityMetadata)null;
			var filterService = (ICodeWriterFilterService)services.GetService(typeof(ICodeWriterFilterService));
			var namingService = (INamingService)services.GetService(typeof(INamingService));

            var metadataProvider = (IMetadataProviderService)services.GetService(typeof(IMetadataProviderService));
            activityPartyMetadata = metadataProvider.LoadMetadata(services).Entities
             .FirstOrDefault(entity => string.Equals(entity.LogicalName, "activityparty", StringComparison.Ordinal)
                 && filterService.GenerateEntity(entity, services));

			return activityPartyMetadata != null
				? TypeRef(typeof(IEnumerable<>), TypeRef(namingService.GetNameForEntity(activityPartyMetadata, services)))
				: TypeRef(typeof(IEnumerable<>), TypeRef(typeof(Entity)));
		}

		internal static OptionSetMetadataBase GetAttributeOptionSet(AttributeMetadata attribute)
		{
			OptionSetMetadataBase optionSet = null;
			Type attributeType = attribute.GetType();

			if (attributeType == typeof(BooleanAttributeMetadata))
			{
				BooleanAttributeMetadata boolAttributeMetadata = (BooleanAttributeMetadata)attribute;
				optionSet = boolAttributeMetadata.OptionSet;
			}
			else if (attributeType == typeof(StateAttributeMetadata))
			{
				StateAttributeMetadata stateAttributeMetadata = (StateAttributeMetadata)attribute;
				optionSet = stateAttributeMetadata.OptionSet;
			}
			else if (attributeType == typeof(PicklistAttributeMetadata))
			{
				PicklistAttributeMetadata picklistAttributeMetadata = (PicklistAttributeMetadata)attribute;
				optionSet = picklistAttributeMetadata.OptionSet;
			}
			else if (attributeType == typeof(StatusAttributeMetadata))
			{
				StatusAttributeMetadata statusAttributeMetadata = (StatusAttributeMetadata)attribute;
				optionSet = statusAttributeMetadata.OptionSet;
			}else if ( attribute is MultiSelectPicklistAttributeMetadata attrib )
            {
				optionSet = attrib.OptionSet; 
            }

			return optionSet;
		}

		private CodeTypeReference GetTypeForField(string clrFormatter, bool isGeneric)
		{
			CodeTypeReference targetType = TypeRef(typeof(object));

			if (isGeneric)
			{
				CodeTypeParameter genericParam = new CodeTypeParameter("T");
				targetType = new CodeTypeReference(genericParam);
			}
			else if (!String.IsNullOrEmpty(clrFormatter))
			{
				Type type = Type.GetType(clrFormatter, false);
				if (type != null)
				{
					targetType = TypeRef(type);
				}
				else
				{
					// Was not able to find the type loaded in the current AppDomain.
					// Try parsing the CLRFormatter value to find the type name instead.
					// Note that this will not work with generics at this point.
					string[] typeNameParts = clrFormatter.Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
					if (typeNameParts != null && typeNameParts.Length > 0)
					{
						// The TypeName here is already fully qualified, so do not try to normalize it via
						// the TypeRef method.  That will prepend an additional namespace in the front of the
						// fully qualified type.
						targetType = new CodeTypeReference(typeNameParts[0]);
					}
				}
			}

			return targetType;
		}

		private CodeTypeReference TypeRef(string typeName)
		{
			return string.IsNullOrWhiteSpace(Namespace)
				? new CodeTypeReference(typeName)
				: new CodeTypeReference(string.Format(CultureInfo.InvariantCulture, "{0}.{1}", Namespace, typeName));
		}

		private static CodeTypeReference TypeRef(Type type)
		{
			return new CodeTypeReference(type);
		}

		private static CodeTypeReference TypeRef(Type type, CodeTypeReference typeParameter)
		{
			return new CodeTypeReference(type.FullName, typeParameter);
		}
		#endregion
	}
}
