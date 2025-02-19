namespace UKHO.ExchangeSets.Fulfilment.IIC.Parameters
{
    public class FileParameter
    {
        public FileParameter(Stream data, string fileName, string contentType)
        {
            Data = data;
            FileName = fileName;
            ContentType = contentType;
        }

        public Stream Data { get; private set; }

        public string FileName { get; private set; }

        public string ContentType { get; private set; }
    }
}
