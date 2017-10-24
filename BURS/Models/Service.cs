using SplittingAlgorithm.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplittingAlgorithm.Models
{
    public class Service
    {
        /// <summary>
        /// Название Услуги
        /// </summary>
        public ServiceEnum ServiceName { get; set; }
        /// <summary>
        /// Размер задолжности
        /// </summary>
        public int DeptSize { get; set; }
        /// <summary>
        /// Размер текущего начисления
        /// </summary>
        public int CurrentChargesSize { get; set; }
        /// <summary>
        /// Итого к оплате
        /// </summary>
        public int SummPay { get; set; }
        /// <summary>
        /// Расщепление платежа
        /// </summary>
        public int SplittingPayment { get; set; }

        /// <summary>
        /// Сумма расщипления
        /// </summary>
        public int SumSplitting { get; set; }

        /// <summary>
        /// Доля задолженности
        /// </summary>
        public double ProportionDept { get; set; }

        /// <summary>
        /// Доля Текущих начислений
        /// </summary>
        public double ProportionCurrentCharges { get; set; }

        /// <summary>
        /// Пеня
        /// </summary>
        public int Fine { get; set; }
        /// <summary>
        /// Доля пени
        /// </summary>
        public double ProportionFine { get; set; }

        /// <summary>
        /// Исключена услуга из расщипления или нет 
        /// </summary>
        public bool IsExcluded { get; set; }
    }
}
