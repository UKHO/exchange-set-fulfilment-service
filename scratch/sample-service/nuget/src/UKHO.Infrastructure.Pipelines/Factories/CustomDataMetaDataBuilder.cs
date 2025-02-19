using System.Collections.Generic;

namespace UKHO.Infrastructure.Pipelines.Factories
{
    public class CustomDataMetaDataBuilder : IMetaDataBuilder
    {
        public void Apply<T>(INode<T> node, IDictionary<string, object> metaData)
        {
            if (metaData == null || node == null)
            {
                return;
            }

            if (metaData.TryGetValue(MetaDataKeys.CustomData, out object result))
            {
                node.CustomData = result;
            }
        }

        public class MetaDataKeys
        {
            public const string CustomData = "CoreMetaDataBuilder:CustomData";
        }
    }
}
