using System.IO;
using System.Threading.Tasks;
using HL7ServiceBase;

namespace OruLettersService
{
    class HL7Poller : HL7PollerBase
    {
        #region Constructor

        public HL7Poller(string pollerDir, string errorDir = null)
            : base(pollerDir, errorDir)
        {
        }

        #endregion

        #region HL7PollerBase Implementation

        protected override async Task ProcessFile(Stream stream)
        {
            using (var hl7Parser = new HL7Parser(stream))
            {
                await hl7Parser.Parse();
            }
        }

        #endregion
    }
}
