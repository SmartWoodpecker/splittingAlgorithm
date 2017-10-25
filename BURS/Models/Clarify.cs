using SplittingAlgorithm.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplittingAlgorithm.Models
{
    /// <summary>
    /// Уточнения
    /// </summary>
    public class Clarify
    {
        /// <summary>
        /// Имя поставщика
        /// </summary>
        public string ProviderName { get; set; }
        /// <summary>
        /// Название услуги
        /// </summary>
        public ServiceEnum ServiceName { get; set; }
        /// <summary>
        /// Внесенная оплата
        /// </summary>
        public int Count { get; set; }

    }
}
