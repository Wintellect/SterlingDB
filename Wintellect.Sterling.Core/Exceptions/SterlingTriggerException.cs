using System;

namespace Wintellect.Sterling.Core.Exceptions
{
    public class SterlingTriggerException : SterlingException 
    {
        public SterlingTriggerException(string message, Type triggerType) :
            base(string.Format(Exceptions.SterlingTriggerException_SterlingTriggerException_Sterling_trigger_exception, triggerType.FullName,
                               message))
        {
            
        }
    }
}