using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using log4net;
using Newtonsoft.Json;
using System.Collections;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.Security.Cryptography;

namespace HL7ServiceBase
{
    internal static class Utils
    {
        internal static async Task ExponentialBackoff(this Func<Task> action, Func<Exception, bool> handleException, int maxAttempts = 6)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            for (int retryAttempt = 0; retryAttempt < maxAttempts; retryAttempt++)
            {
                ExceptionDispatchInfo capturedException = null;
                try
                {
                    await Task.Run(action).ConfigureAwait(false);
                    break;
                }
                catch (Exception ex)
                {
                    capturedException = ExceptionDispatchInfo.Capture(ex);
                }

                if (capturedException != null)
                {
                    if (retryAttempt < maxAttempts)
                    {
                        var handled = false;
                        if (handleException != null)
                            handled = handleException(capturedException.SourceException);
                        if (!handled)
                            await Task.Delay(TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));
                        else
                            break;
                    }
                    else
                        capturedException.Throw();
                }
            }
        }

        internal static bool IsSharingViolation(this IOException exception)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            int hr = GetHResult(exception, 0);
            return (hr == -2147024864); // 0x80070020 ERROR_SHARING_VIOLATION

        }

        static int GetHResult(IOException exception, int defaultValue)
        {
            if (exception == null)
                throw new ArgumentNullException("exception");

            var field = exception.GetType().GetField("_HResult", BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
                return Convert.ToInt32(field.GetValue(exception));

            return defaultValue;
        }
    }
}
