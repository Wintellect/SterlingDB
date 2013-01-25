using System;
using System.Diagnostics;
using System.Text;

namespace Wintellect.Sterling.Core
{
    /// <summary>
    ///     Default logger (debug) for Sterling
    /// </summary>
    public class SterlingDefaultLogger
    {
        private Guid _guid = Guid.Empty;
        private readonly SterlingLogLevel _minimumLevel;

        /// <summary>
        ///     Create 
        /// </summary>
        /// <param name="minimumLevel">Minimum level to debug</param>
        public SterlingDefaultLogger(SterlingLogLevel minimumLevel)
        {
            _minimumLevel = minimumLevel;

            if (Debugger.IsAttached)
            {
                _guid = SterlingFactory.GetLogger().RegisterLogger(_Log);
            }
        }

        /// <summary>
        ///     Detach the logger
        /// </summary>
        public void Detach()
        {
            if (!_guid.Equals(Guid.Empty))
            {
                SterlingFactory.GetLogger().UnhookLogger(_guid);
            }
        }

        /// <summary>
        ///     
        /// </summary>
        /// <param name="logLevel"></param>
        /// <param name="message"></param>
        /// <param name="exception"></param>
        private void _Log(SterlingLogLevel logLevel, string message, Exception exception)
        {
            if (!Debugger.IsAttached || (int) logLevel < (int) _minimumLevel) return;

            var sb = new StringBuilder(string.Format("{0}::Sterling::{1}::{2}",
                                                     DateTime.Now, 
                                                     logLevel,
                                                     message));

            var local = exception; 

            while (local != null)
            {
                sb.Append(local);
                local = local.InnerException; 
            }

            Debug.WriteLine(sb.ToString());
        }
    }
}
