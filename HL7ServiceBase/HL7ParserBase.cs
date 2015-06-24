using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using log4net;
using RestClient;

namespace HL7ServiceBase
{
    public abstract class HL7ParserBase : IDisposable
    {
        #region Fields

        static readonly ILog log = LogManager.GetLogger(typeof(HL7ParserBase));

        protected char fieldSep = '|';
        protected char componentSep = '^';
        protected char subComponentSep = '&';
        protected char repetitionSep = '~';
        protected char escapeChar = '\\';

        TextReader reader;
        static Encoding hl7FileEncoding;

        protected GenericRestCrudHttpClient httpClient;
        bool disposed = false;

        #endregion

        #region Constructor

        static HL7ParserBase()
        {
            var encodingName = ConfigurationManager.AppSettings["Hl7FileEncoding"];
            hl7FileEncoding = Encoding.GetEncoding(encodingName ?? "iso-8859-1"); //latin1
        }

        public HL7ParserBase(Stream stream, GenericRestCrudHttpClient httpClient)
            : this(httpClient)
        {
            reader = new StreamReader(stream, hl7FileEncoding);
        }

        public HL7ParserBase(GenericRestCrudHttpClient httpClient)
        {
            this.httpClient = httpClient;
        }

        #endregion

        #region IDisposable Members

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        void Dispose(bool disposing)
        {
            if (!disposed && disposing)
            {
                if (reader != null)
                {
                    var d = reader;
                    reader = null;
                    d.Dispose();
                }
                disposed = true;
            }
        }

        #endregion IDisposable Members

        #region Methods

        public async Task Parse()
        {
            string line = null;
            while ((line = await reader.ReadLineAsync()) != null)
                await ProcessSegment(line);

            //store the documents
            await StoreDocuments();
        }

        protected abstract Task StoreDocuments();

        #endregion

        #region HL7 Segment Methods

        protected abstract Task ProcessSegment(string line);

        #endregion

        #region HL7 Helper Methods

        protected string GetValueFromField(string field, int repetitionIndex = 0, int componentIndex = 0, int subComponentIndex = 0)
        {
            if (string.IsNullOrEmpty(field))
                return null;

            var repetitions = field.Split(repetitionSep);
            var repetition = repetitions.ElementAtOrDefault(repetitionIndex);
            if (string.IsNullOrEmpty(repetition))
                return null;

            var components = repetition.Split(componentSep);
            var component = components.ElementAtOrDefault(componentIndex);
            if (string.IsNullOrEmpty(component))
                return null;

            var subComponents = component.Split(subComponentSep);
            var subComponent = subComponents.ElementAtOrDefault(subComponentIndex);
            if (string.IsNullOrEmpty(subComponent))
                return null;

            return ParseEscapeSequences(subComponent);
        }

        string ParseEscapeSequences(string value)
        {
            var sb = new StringBuilder();

            var parts = value.Split(escapeChar);

            for (int i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (i % 2 == 0)
                    sb.Append(part);
                else if (part == "H") //start highlighting
                { }
                else if (part == "N") //normal text (end highlighting)
                { }
                else if (part == "F") //field separator
                    sb.Append(fieldSep);
                else if (part == "S") //component separator
                    sb.Append(componentSep);
                else if (part == "T") //subcomponent separator
                    sb.Append(subComponentSep);
                else if (part == "R") //repetition separator
                    sb.Append(subComponentSep);
                else if (part == "E") //escape character
                    sb.Append(escapeChar);
                else if (part.StartsWith("X")) //hexadecimal data
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part.StartsWith("Z")) //locally defined escape sequence
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part.StartsWith("C")) //Single-byte character sets:
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part.StartsWith("M")) //Multi-byte character sets:
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part.StartsWith(".sp")) //End current output line and skip <number> vertical spaces. <number> is a positive integer or absent. If <number> is absent, skip one space. The horizontal character position remains unchanged. Note that for purposes of compatibility with previous versions of HL7, "^\.sp\" is equivalent to "\.br\."
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part == ".br") //Begin new output line. Set the horizontal position to the current left margin and increment the vertical position by 1.
                {
                    sb.AppendLine();
                }
                else if (part == ".fi") //Begin word wrap or fill mode. This is the default state. It can be changed to a no-wrap mode using the .nf command.
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part == ".nf") //Begin no-wrap mode.
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part.StartsWith(".in")) //Indent <number> of spaces, where <number> is a positive or negative integer. This command cannot appear after the first printable character of a line.
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part.StartsWith(".ti")) //Temporarily indent <number> of spaces where number is a positive or negative integer. This command cannot appear after the first printable character of a line.
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part.StartsWith(".sk")) //Skip <number> spaces to the right.
                {
                    System.Diagnostics.Debugger.Break();
                }
                else if (part == ".ce") //End current output line and center the next line.
                {
                    System.Diagnostics.Debugger.Break();
                }
            }

            return sb.ToString();
        }

        #endregion
    }
}