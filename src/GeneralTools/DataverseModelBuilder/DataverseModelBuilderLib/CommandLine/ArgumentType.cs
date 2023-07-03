using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
	/// <remarks>
	/// Type of command line argument represented.
	/// </remarks>
	[Flags]
	internal enum ArgumentType
	{
		/// <summary>
		/// Argument is optional.
		/// </summary>
		Optional = 1,
		/// <summary>
		/// Argument is required.
		/// </summary>
		Required = 2,
		/// <summary>
		/// The argument may appear multiple times.
		/// </summary>
		Multiple = 4,
		/// <summary>
		/// Argument is a binary argument.  If it shows up
		/// it is equivalent to a true value.
		/// </summary>
		Binary = 8,
		/// <summary>
		/// Argument is hidden from the user.  It can be supplied
		/// on the command line, but will not show up in the
		/// standard usage message.
		/// </summary>
		Hidden = 16
	}
}
