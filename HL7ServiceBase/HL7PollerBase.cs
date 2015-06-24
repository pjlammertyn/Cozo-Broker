using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using log4net;

namespace HL7ServiceBase
{
    public abstract class HL7PollerBase
    {
        #region Fields

        protected static readonly ILog log = LogManager.GetLogger(typeof(HL7PollerBase));
        string pollerDir;
        string errorDir;
        string searchPattern;
        SearchOption searchOption;
        Timer pollerTimer;
        bool started;
        bool retryOnSharingViolation;
        bool polling;

        #endregion

        #region Constructor

        public HL7PollerBase(string pollerDir, string errorDir = null)
        {
            this.pollerDir = pollerDir;
            this.errorDir = errorDir;

            searchPattern = ConfigurationManager.AppSettings["SearchPattern"] ?? "*.hl7";
            if (!Enum.TryParse<SearchOption>(ConfigurationManager.AppSettings["SearchOption"], out searchOption))
                searchOption = SearchOption.TopDirectoryOnly;

            if (!bool.TryParse(ConfigurationManager.AppSettings["RetryOnSharingViolation"], out retryOnSharingViolation))
                retryOnSharingViolation = true;

            int pollingInterval;
            if (!int.TryParse(ConfigurationManager.AppSettings["PollingInterval"], out pollingInterval))
                pollingInterval = 5000;

            pollerTimer = new Timer();
            pollerTimer.Interval = pollingInterval;
            pollerTimer.Elapsed += new ElapsedEventHandler(pollerTimer_Elapsed);
        }

        #endregion

        #region Events

        async void pollerTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //pollerTimer.Stop();
            if (!started || polling)
                return;

            try
            {
                polling = true;
                await Poll();
            }
            catch (Exception ex)
            {
                if (log.IsErrorEnabled)
                    log.Error(ex.Message, ex);
            }
            finally
            {
                polling = false;
                //if (started)
                //    pollerTimer.Start();
            }
        }

        #endregion

        #region Service Methods

        public virtual void Start()
        {
            started = true;
            pollerTimer.Start();
            pollerTimer_Elapsed(this, null); //poll on start
        }

        public virtual void Stop()
        {
            started = false;
            pollerTimer.Stop();
        }

        #endregion

        #region Process File Methods

        protected virtual async Task Poll()
        {
            foreach (var file in Directory.GetFiles(pollerDir, searchPattern, searchOption).OrderBy(file => file)) //get all files in dir ordered by filename ascending
            {
                if (!started)
                    break;
                await ReadFile(file);
            }
        }

        protected abstract Task ProcessFile(Stream stream);

        protected virtual bool HandleException(Exception ex)
        {
            return false;
        }

        async Task ReadFile(string file)
        {
            Func<Task> action = async () =>
            {
                if (!File.Exists(file))
                    return;

                if (log.IsInfoEnabled)
                    log.InfoFormat("Processing {0}", file);

                using (var fs = new FileStream(file, FileMode.Open, FileAccess.ReadWrite, FileShare.Delete))
                {
                    await ProcessFile(fs);

                    File.Delete(file);
                }
            };

            try
            {
                await action.ExponentialBackoff(ex =>
                {
                    if (ex is FileNotFoundException)
                        return true; //do not retry if file not found

                    if (ex is IOException && (ex as IOException).IsSharingViolation())
                        return !retryOnSharingViolation; //not logging sharing violation

                    if (log.IsErrorEnabled)
                        log.Error(string.Format("{0}: {1}", Path.GetFileName(file), ex.Message), ex);

                    return HandleException(ex);
                });
            }
            catch (Exception ex) //Exponential backoff failed!!!!
            {
                if (log.IsErrorEnabled)
                    log.Error(string.Format("Exponentional backoff failed: {0}: {1}", Path.GetFileName(file), ex.Message), ex);

                //TRY TO COPY THE FILE TO THE ERROR FOLDER!!!!!!!!
                try
                {
                    if (!string.IsNullOrEmpty(errorDir))
                        File.Copy(file, Path.Combine(errorDir, Path.GetFileName(file)), true);
                }
                catch (Exception) { }
            }
        }

        #endregion
    }
}
