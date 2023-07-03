using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	internal sealed class ServiceProvider : IServiceProvider
	{
		#region Fields
		private Dictionary<Type, object> _services;
		#endregion

		#region Constructors
		internal ServiceProvider()
		{
			this._services = new Dictionary<Type, object>();
		}
		#endregion

		#region Properties
		internal ITypeMappingService TypeMappingService
		{
			get
			{
				return (ITypeMappingService)this._services[typeof(ITypeMappingService)];
			}
		}

		internal IMetadataProviderService MetadataProviderService
		{
			get
			{
				return (IMetadataProviderService)this._services[typeof(IMetadataProviderService)];
			}
		}

        internal IMetadataProviderQueryService MetadataProviderQueryServcie
        {
            get
            {
                return (IMetadataProviderQueryService)this._services[typeof(IMetadataProviderQueryService)];
            }
        }


		internal ICustomizeCodeDomService CodeCustomizationService
		{
			get
			{
				return (ICustomizeCodeDomService)this._services[typeof(ICustomizeCodeDomService)];
			}
		}

		internal ICodeGenerationService CodeGenerationService
		{
			get
			{
				return (ICodeGenerationService)this._services[typeof(ICodeGenerationService)];
			}
		}

		internal ICodeWriterFilterService CodeFilterService
		{
			get
			{
				return (ICodeWriterFilterService)this._services[typeof(ICodeWriterFilterService)];
			}
		}

		internal ICodeWriterMessageFilterService CodeMessageFilterService
		{
			get
			{
				return (ICodeWriterMessageFilterService)this._services[typeof(ICodeWriterMessageFilterService)];
			}
		}

		internal INamingService NamingService
		{
			get
			{
				return (INamingService)this._services[typeof(INamingService)];
			}
		}
		#endregion

		#region IServiceProvider Members
		object IServiceProvider.GetService(Type serviceType)
		{
			if (this._services.ContainsKey(serviceType))
				return this._services[serviceType];
			else
				return null;
		}
		#endregion

		#region Methods
		internal void InitializeServices(ModelBuilderInvokeParameters parameters)
		{
			CodeWriterFilterService filterService = new CodeWriterFilterService(parameters);
			this._services.Add(typeof(ICodeWriterFilterService), ServiceFactory.CreateInstance<ICodeWriterFilterService>(filterService, parameters.CodeWriterFilterService, parameters));
			this._services.Add(typeof(ICodeWriterMessageFilterService), ServiceFactory.CreateInstance<ICodeWriterMessageFilterService>(filterService, parameters.CodeWriterMessageFilterService, parameters));
			this._services.Add(typeof(IMetadataProviderService), ServiceFactory.CreateInstance<IMetadataProviderService>(new SdkMetadataProviderService(parameters), parameters.MetadataProviderService, parameters));
            this._services.Add(typeof(IMetadataProviderQueryService), ServiceFactory.CreateInstance<IMetadataProviderQueryService>(new MetadataProviderQueryService(parameters), parameters.MetadataQueryProviderService, parameters));
			this._services.Add(typeof(ICodeGenerationService), ServiceFactory.CreateInstance<ICodeGenerationService>(new CodeGenerationService(parameters), parameters.CodeGenerationService, parameters));
			this._services.Add(typeof(INamingService), ServiceFactory.CreateInstance<INamingService>(new NamingService(parameters), parameters.NamingService, parameters));
			this._services.Add(typeof(ICustomizeCodeDomService), ServiceFactory.CreateInstance<ICustomizeCodeDomService>(new CodeDomCustomizationService(), parameters.CodeCustomizationService, parameters));
			this._services.Add(typeof(ITypeMappingService), new TypeMappingService(parameters));
		}
		#endregion
	}
}
