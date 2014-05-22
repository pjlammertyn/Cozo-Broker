using Castle.Core.Logging;
using COZO.KMEHR;
using Microsoft.Practices.ServiceLocation;

namespace COZO
{
    public static class KmehrUtils
    {
        static readonly ILogger log = ServiceLocator.Current.GetInstance<ILoggerFactory>().Create(typeof(KmehrUtils));

        public static string BuildMessage(folder[] folders)
        {
            //Create new message
            var kmehrMsg = Builder.message(Builder.header(ServiceLocator.Current.GetInstance<IGlobalData>().HOSPITALhcparty, ServiceLocator.Current.GetInstance<IGlobalData>().GZOhcparty, null, null, null), null);

            // add folders to message
            foreach (var folder in folders)
                Builder.add(kmehrMsg, folder);

            return Serializer.toXML(kmehrMsg);
        }
       
        public static string BuildMessage(personType patient, params transaction[] transactions)
        {
            //Create new message
            var kmehrMsg = Builder.message(Builder.header(ServiceLocator.Current.GetInstance<IGlobalData>().HOSPITALhcparty, ServiceLocator.Current.GetInstance<IGlobalData>().GZOhcparty, null, null, null), null);

            var folder = BuildFolder(patient, transactions);

            // add folder to message
            Builder.add(kmehrMsg, folder);

            return Serializer.toXML(kmehrMsg);
        }

        public static folder BuildFolder(personType patient, params transaction[] transactions)
        {
            //create tranasaction folder
            var folder = Builder.folder(patient, null, null);

            //add transactionlist
            foreach (var transaction in transactions)
            {
                if (transaction != null)
                    Builder.add(folder, transaction);
            }
            return folder;
        }
    }
}
