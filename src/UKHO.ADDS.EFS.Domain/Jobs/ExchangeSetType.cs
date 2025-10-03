using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.ADDS.EFS.Domain.Jobs
{
    /// <summary>
    /// Enumeration of exchange set types for pipeline processing
    /// </summary>
    public enum ExchangeSetType
    {
        /// <summary>
        /// Request for complete exchange set
        /// </summary>
        Complete,

        /// <summary>
        /// Request for custom exchange set
        /// 
        Custom
    }
}
