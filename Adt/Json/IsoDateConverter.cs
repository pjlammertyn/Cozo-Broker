using Newtonsoft.Json.Converters;

namespace Adt.Json
{
    class IsoDateConverter : IsoDateTimeConverter
    {
        public IsoDateConverter()
        {
            base.DateTimeFormat = "yyyy-MM-dd";
        }
    }
}
