using System.Reflection;

namespace RealTimeChat.Application.Exceptions
{
    public class NotFoundException : Exception
    {
        public NotFoundException(string resourceName, object resourceId)
            : base($"{resourceName} with ID '{resourceId}' was not found.")
        {
        }
    }
}
