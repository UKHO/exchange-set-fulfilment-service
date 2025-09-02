using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UKHO.ADDS.EFS.Domain.Builds
{
    public class BuildFileDetail
    {

        /// <summary>

        /// Gets or sets the file name.

        /// </summary>

        public required string FileName { get; set; }

        /// <summary>

        /// Gets or sets the file hash value.

        /// </summary>

        public required string Hash { get; set; }

    }
}
