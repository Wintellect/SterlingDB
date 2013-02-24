using System;
using System.Collections.Generic;
using Wintellect.Sterling.Core.Exceptions;

namespace Wintellect.Sterling.Core.Database
{
    /// <summary>
    ///     Manages the loggers
    /// </summary>
    public class LogManager
    {
        private object _lock = new object();

        /// <summary>
        ///     The dictionary of loggers
        /// </summary>
        private readonly Dictionary<Guid,Action<SterlingLogLevel, string, Exception>> _loggers 
            = new Dictionary<Guid,Action<SterlingLogLevel, string, Exception>>();

        /// <summary>
        ///     Register a logger
        /// </summary>
        /// <param name="logger">The logger to register</param>
        /// <returns>A unique identifier</returns>
        public Guid RegisterLogger(Action<SterlingLogLevel, string, Exception> logger)
        {
            var identifier = Guid.NewGuid();

            lock(_lock)
            {
                _loggers.Add(identifier, logger);
            }

            return identifier;
        }

        /// <summary>
        ///     Removes a logger
        /// </summary>
        /// <param name="guid">The identifier for the logger</param>
        public void UnhookLogger(Guid guid)
        {
            lock(_lock)
            {
                _loggers.Remove(guid);
            }
        }

        /// <summary>
        ///     Log an entry
        /// </summary>
        /// <param name="level">The level</param>
        /// <param name="message">The message</param>
        /// <param name="exception">The exception</param>
        public void Log(SterlingLogLevel level, string message, Exception exception)
        {
            lock(_lock)
            {
                foreach (var key in _loggers.Keys)
                {
                    _loggers[key](level, message, exception);                    
                }
            }
        }
    }
}
