using System.Runtime.Serialization;
using System.Text;

namespace TorrentLib
{
    [Serializable]
    public class TorrentException : Exception
    {
        public TorrentException(int errorCode) : base(ErrorHelper.GetErrorMessage(errorCode))
        {
            
        }

        public TorrentException(string? message) : base(message)
        {
        }

        public TorrentException(string? message, Exception? innerException) : base(message, innerException)
        {
        }

        protected TorrentException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}