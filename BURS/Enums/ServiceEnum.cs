using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SplittingAlgorithm.Enums
{
    public enum ServiceEnum
    {
        [Display(Name = "Услуга 1")]
        Service1,

        [Display(Name = "Услуга 2")]
        Service2,

        [Display(Name = "Услуга 3")]
        Service3,

        [Display(Name = "Услуга 4")]
        Service4,

        [Display(Name = "Пени")]
        Peni

    }
}
