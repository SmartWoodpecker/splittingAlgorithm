using SplittingAlgorithm.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplittingAlgorithm.Models
{
    /// <summary>
    /// Объект Уточнение
    /// </summary>
    public class Clarify
    {
        public string ProviderName { get; set; }
        public ServiceEnum ServiceName { get; set; }
        public int Count { get; set; }

    }
}
