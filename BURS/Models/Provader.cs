using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplittingAlgorithm.Models
{
    public class Provider
    {
        public string Name { get; set; }

        /// <summary>
        /// список услуг у поставщика
        /// </summary>
        public List<Service> Services { get; set; }


        /// <summary>
        /// Активность поставщика
        /// </summary>
        public bool IsInactive { get; set; }

    }
}
