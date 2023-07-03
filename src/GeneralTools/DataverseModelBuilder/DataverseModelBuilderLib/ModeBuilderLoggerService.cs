using Microsoft.Extensions.Logging;
using Microsoft.PowerPlatform.Dataverse.ModelBuilderLib.Status;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Text;


namespace Microsoft.PowerPlatform.Dataverse.ModelBuilderLib
{
    /// <summary>
    /// Trace Logger for this project
    /// </summary>
    public class ModeBuilderLoggerService 
    {
        #region Private Fields
        /// <summary>
        /// Trace Tag.
        /// </summary>
        private TraceSource source;

        /// <summary>
        /// ILogger Sink for logging to logger output, If available TraceSource is not used. 
        /// </summary>
        private ILogger _logger; 

        /// <summary>
        /// String Builder Info.
        /// </summary>
        private StringBuilder _LastError = new StringBuilder();

        /// <summary>
        /// Last Exception.
        /// </summary>
        private Exception _LastException;
        #endregion

        #region Properties
        /// <summary>
        /// Last Error from CRM.
        /// </summary>
        public string LastError { get { return _LastError.ToString(); } }
        /// <summary>
        /// Last Exception from CRM .
        /// </summary>
        public Exception LastException { get { return _LastException; } }
        /// <summary>
        /// Returns the trace source level for the current logger.
        /// </summary>
        public SourceLevels CurrentTraceLevel
        {
            get
            {
                if (source != null)
                    return source.Switch.Level;
                else return SourceLevels.Off;
            }
        }
        #endregion

        #region Events 

        /// <summary>
        /// Raised when there is a request to write something to the Progress display. 
        /// </summary>
        public event EventHandler<ProgressStatus> OnWriteProgressItem;

        #endregion

        #region Public Methods
        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="traceSourceName">trace source name</param>
        public ModeBuilderLoggerService(string traceSourceName, ILogger logger = null)
        {
            _logger = logger;
            if (string.IsNullOrWhiteSpace(traceSourceName))
                source = new TraceSource("DataverseModelBuilderLib");
            else
                source = new TraceSource(traceSourceName);
        }
        /// <summary>
        /// Last error reset.
        /// </summary>
        public void ResetLastError()
        {
            _LastError.Remove(0, LastError.Length);
            _LastException = null;
        }

        /// <summary>
        /// Trace Log data out in the Verbose path. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messagedata"></param>
        public void TraceVerbose(string message, params object[] messagedata)
        {
            // Log to Information.
            if (_logger == null)
            {
                source.TraceEvent(TraceEventType.Verbose, (int)TraceEventType.Verbose, string.Format(CultureInfo.CurrentUICulture, message, messagedata));
            }
            else
            {
                _logger.LogTrace(string.Format(CultureInfo.CurrentUICulture, message, messagedata));
            }
        }

        /// <summary>
        /// Trace Log data out in the Information path. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messagedata"></param>
        public void TraceInformation(string message, params object[] messagedata)
        {
            // Log to Information.
            if (_logger == null)
            {
                source.TraceEvent(TraceEventType.Information, (int)TraceEventType.Information, string.Format(CultureInfo.CurrentUICulture, message, messagedata));
            }
            else
            {
                _logger.LogInformation(string.Format(CultureInfo.CurrentUICulture, message, messagedata));
            }
        }

        public void WriteConsole(string message, bool indented, ProcessStage stage)
        {
            // Log to Information.
            TraceInformation(message);
            OnWriteProgressItem?.Invoke(this, new ProgressStatus()
            {
                Indent = indented,
                Stage = stage,
                StatusMessage = message,
                StatusType = ProgressType.Information
            });
            //Console.WriteLine(message);
        }

        public void WriteConsoleError(string message, bool indented, ProcessStage stage)
        {
            // Log to Information.
            TraceError(message);
            OnWriteProgressItem?.Invoke(this, new ProgressStatus()
            {
                Indent = indented,
                Stage = stage,
                StatusMessage = message,
                StatusType = ProgressType.Error
            });
            //Console.WriteLine(message);
        }

        public void WriteConsoleWarning(string message, bool indented, ProcessStage stage)
        {
            // Log to Information.
            TraceWarning(message);
            OnWriteProgressItem?.Invoke(this, new ProgressStatus()
            {
                Indent = indented,
                Stage = stage,
                StatusMessage = message,
                StatusType = ProgressType.Warning
            });
            //Console.WriteLine(message);
        }

        /// <summary>
        /// Trace Log data out to the warning path. 
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messagedata"></param>
        public void TraceWarning(string message, params object[] messagedata)
        {
            // Log to Warning Channel.
            if (_logger == null)
            {
                source.TraceEvent(TraceEventType.Warning, (int)TraceEventType.Warning, string.Format(CultureInfo.CurrentUICulture, message, messagedata));
            }
            else
            {
                _logger.LogWarning(string.Format(CultureInfo.CurrentUICulture, message, messagedata));
            }
        }

        /// <summary>
        /// Trace Error Data out to the error path.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="messagedata"></param>
        public void TraceError(string message, params object[] messagedata)
        {
            // Log to Error Channel.
            if (_logger == null)
            {
                source.TraceEvent(TraceEventType.Error, (int)TraceEventType.Error, string.Format(CultureInfo.CurrentUICulture, message, messagedata));
            }
            else
            {
                _logger.LogError(string.Format(CultureInfo.CurrentUICulture, message, messagedata));
            }
        }

        /// <summary>
        /// Trace Error Data from an exception out to the error path. 
        /// </summary>
        /// <param name="exception"></param>
        public void TraceError(Exception exception)
        {
            //string message = null;

            StringBuilder sbException = new StringBuilder();
            LogExceptionToFile(exception, sbException, 0);

            if (_logger == null)
            {
                if (sbException.Length > 0)
                    source.TraceEvent(TraceEventType.Error, (int)TraceEventType.Error, sbException.ToString());
            }
            else
            {
                if (sbException.Length > 0)
                    _logger.LogError(sbException.ToString());
            }

            _LastError.Append(sbException.ToString());
            _LastException = exception;
        }

        /// <summary>
        /// Log Method Start to debug path.
        /// </summary>
        public void TraceMethodStart()
        {
            StackTrace stackTrace = new StackTrace();
            if (_logger == null)
            {
                source.TraceEvent(TraceEventType.Start, (int)TraceEventType.Start, string.Format(CultureInfo.CurrentUICulture, "Entering {0}", stackTrace.GetFrame(Math.Min(1, stackTrace.FrameCount - 1)).GetMethod()));
            }
            else
            {
                _logger.LogDebug(string.Format(CultureInfo.CurrentUICulture, "Entering {0}", stackTrace.GetFrame(Math.Min(1, stackTrace.FrameCount - 1)).GetMethod()));
            }
        }

        /// <summary>
        /// Log Method Stop to debug path.
        /// </summary>
        public void TraceMethodStop()
        {
            StackTrace stackTrace = new StackTrace();
            if (_logger == null)
            {
                source.TraceEvent(TraceEventType.Stop, (int)TraceEventType.Stop, string.Format(CultureInfo.CurrentUICulture, "Exiting {0}", stackTrace.GetFrame(Math.Min(1, stackTrace.FrameCount - 1)).GetMethod()));
            }
            else
            {
                _logger.LogDebug(string.Format(CultureInfo.CurrentUICulture, "Exiting {0}", stackTrace.GetFrame(Math.Min(1, stackTrace.FrameCount - 1)).GetMethod()));
            }
        }

        /// <summary>
        /// Logs the error text to the stream.
        /// </summary>
        /// <param name="objException">Exception to be written.</param>
        /// <param name="sw">Stream writer to use to write the exception.</param>
        /// <param name="level">level of the exception, this deals with inner exceptions.</param>
        private static void LogExceptionToFile(Exception objException, StringBuilder sw, int level)
        {
            if (level != 0)
                sw.AppendLine(string.Format(CultureInfo.InvariantCulture, "Inner Exception Level {0}\t: ", level));

            sw.AppendLine("Source\t: " +
                (objException.Source != null ? objException.Source.ToString().Trim() : "Not Provided"));
            sw.AppendLine("Method\t: " +
                (objException.TargetSite != null ? objException.TargetSite.Name.ToString() : "Not Provided"));
            sw.AppendLine("Date\t: " +
                    DateTime.Now.ToLongTimeString());
            sw.AppendLine("Time\t: " +
                    DateTime.Now.ToShortDateString());
            sw.AppendLine("Error\t: " +
                (string.IsNullOrEmpty(objException.Message) ? "Not Provided" : objException.Message.ToString().Trim()));
            sw.AppendLine("Stack Trace\t: " +
                (string.IsNullOrEmpty(objException.StackTrace) ? "Not Provided" : objException.StackTrace.ToString().Trim()));
            sw.AppendLine("======================================================================================================================");

            level++;
            if (objException.InnerException != null)
                LogExceptionToFile(objException.InnerException, sw, level);

        }

        /// <summary>
        /// Adds Trace Listener to the Trace Source.
        /// </summary>
        /// <param name="listener"></param>
        /// <exception cref="ArgumentNullException"></exception>
        public void AddTraceListener(TraceListener listener)
        {
            _ = listener ?? throw new ArgumentNullException(nameof(listener));
            
            source?.Listeners.Add(listener);
        }

        /// <summary>
        /// Set trace level
        /// </summary>
        /// <param name="level"></param>
        public void SetTraceLevel(SourceLevels level)
        {
            if (source == null)
                return;

            if (source.Switch == null)
                source.Switch = new SourceSwitch(source.Switch.DisplayName, level.ToString());
            else
                source.Switch.Level = level;
        }

        #endregion
    }
}
