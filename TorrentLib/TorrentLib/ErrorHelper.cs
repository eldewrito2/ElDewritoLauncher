using System.Text;

namespace TorrentLib
{
    public static class ErrorHelper
    {
        public static string GetErrorMessage(int errorCode)
        {
            int len = NativeApi.format_error_message(errorCode, null, 0);
            if (len >= 0)
            {
                byte[] buffer = new byte[len];
                len = NativeApi.format_error_message(errorCode, buffer, len);
                if (len > 0)
                    return Encoding.UTF8.GetString(buffer, 0, len);
            }

            return "Unknown error";
        }
    }
}
