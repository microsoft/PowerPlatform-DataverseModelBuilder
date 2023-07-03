using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    internal class StaticUtils
    {

        internal static string ApplicationName
        {
            get
            {
                try
                {
                    object[] attributes = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyTitleAttribute), false);
                    if (attributes.Length > 0) return ((AssemblyTitleAttribute)attributes[0]).Title;
                    else return "Unknown Title";
                }
                catch
                {
                    return "Unknown Title";
                }
            }
        }

        internal static string ApplicationDescription
        {
            get
            {
                try
                {
                    object[] attributes = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyDescriptionAttribute), false);
                    if (attributes.Length > 0) return ((AssemblyDescriptionAttribute)attributes[0]).Description;
                    else return "Unknown Description";
                }
                catch
                {
                    return "Unknown Description";
                }
            }
        }

        internal static string ApplicationVersion
        {
            get
            {
                try
                {
                    object[] attributes = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyFileVersionAttribute), true);
                    if ( attributes.Length > 0) return ((AssemblyFileVersionAttribute)attributes[0]).Version;
                    else return "Unknown Version";
                }
                catch
                {
                    return "Unknown Version";
                }
            }
        }

        internal static string ApplicationCopyright
        {
            get
            {
                try
                {
                    object[] attributes = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(AssemblyCopyrightAttribute), false);
                    if ( attributes.Length > 0 ) return ((AssemblyCopyrightAttribute)attributes[0]).Copyright;
                    else return "Unknown Copyright";
                }
                catch
                {
                    return "Unknown Copyright";
                }
            }
        }
    }
}
