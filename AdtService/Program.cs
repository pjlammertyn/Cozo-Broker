using System.Configuration;
using Topshelf;

namespace AdtService
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
                        return new HL7Poller(ConfigurationManager.AppSettings["PollerDir"], ConfigurationManager.AppSettings["ErrorDir"]);
                    });
                    s.WhenStarted(service => service.Start());
                    s.WhenStopped(service => service.Stop());
                });

                c.SetDescription("ADTService");
                c.SetDisplayName("ADTService");
                c.SetServiceName("ADTService");
            });
        }
    }
}
