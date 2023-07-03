using System;
using System.Collections.Generic;
using System.Configuration;
using System.Diagnostics;
using System.Reflection;
using System.Text;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	internal static class ServiceFactory
	{
		internal static TIService CreateInstance<TIService>(TIService defaultServiceInstance, string parameterValue, ModelBuilderInvokeParameters parameters)
		{
			// look for a key with name same as the interface name minus starting I
			string configurationKey = typeof(TIService).Name.Substring(1);
			parameters.Logger.TraceVerbose("Creating instance of {0}", typeof(TIService).Name);

			string configuredTypeName = parameterValue;
			if (String.IsNullOrEmpty(configuredTypeName))
				configuredTypeName = ConfigurationManager.AppSettings[configurationKey]; //TODO: FIX FOR IOPTIONS HERE

			if (!string.IsNullOrEmpty(configuredTypeName))
			{
				parameters.Logger.TraceInformation("Looking for custom extension named {0} for ", configuredTypeName , typeof(TIService).Name);
				Type t = Type.GetType(configuredTypeName, false);
				if (t == null)
				{
					throw new NotSupportedException("Could not load provider of type '" + configuredTypeName + "'");
				}
				if (t.GetInterface(typeof(TIService).FullName) == null)
				{
					throw new NotSupportedException("Type '" + configuredTypeName + "'does not implement interface " + typeof(TIService).FullName);
				}
				if (t.IsAbstract)
				{
					throw new NotSupportedException("Cannot instantiate abstract type '" + configuredTypeName + "'.");
				}

				ConstructorInfo tc = t.GetConstructor(new Type[] { typeof(TIService), typeof(IDictionary<string, string>) });
				if (tc != null)
				{
					return (TIService)tc.Invoke(new object[] { defaultServiceInstance, parameters.ToDictionary() });
				}

				tc = t.GetConstructor(new Type[] { typeof(TIService) });
				if (tc != null)
				{
					return (TIService)tc.Invoke(new object[] { defaultServiceInstance });
				}

				tc = t.GetConstructor(new Type[] { typeof(IDictionary<string, string>) });
				if (tc != null)
				{
					return (TIService)tc.Invoke(new object[] { parameters.ToDictionary() });
				}

				return (TIService)Activator.CreateInstance(t);
			}

            parameters.Logger.TraceInformation("Creating default instance of {0}", typeof(TIService).Name);
            return defaultServiceInstance;
		}
	}
}
