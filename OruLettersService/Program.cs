using System.Configuration;
using Topshelf;

namespace OruLettersService
{
    class Program
    {
        static void Main(string[] args)
        {
            HostFactory.Run(c =>
            {
                c.UseLog4Net("log4net.config");

                c.Service<HL7Poller>(s =>
                {
                    s.ConstructUsing(() =>
                    {
                        return new HL7Poller(ConfigurationManager.AppSettings["PollerDir"]);
                    });
                    s.WhenStarted(service => service.Start());
                    s.WhenStopped(service => service.Stop());
                });

                c.SetDescription("OruLettersService");
                c.SetDisplayName("OruLettersService");
                c.SetServiceName("OruLettersService");
            });
        }
    }
}
