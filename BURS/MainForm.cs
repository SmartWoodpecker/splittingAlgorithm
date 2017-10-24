﻿using SplittingAlgorithm.Enums;
using SplittingAlgorithm.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace SplittingAlgorithm
{
    public partial class MainForm : Form
    {
        private List<Provider> _provs;

        public int _payment = 0;
        public List<Clarify> clarifys = null; // список уточнений услуг

        public MainForm()
        {
            InitializeComponent();
            // Заполнение колонки с услугами исходными данными
            string[] names = Enum.GetNames(typeof(ServiceEnum));
            Column2.Items.AddRange(names);
        }

        /// <summary>
        /// считывание данных, рзапуск алгоритма расщипления и вывод данных
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void сalculate_Click(object sender, EventArgs e)
        {
            _provs = new List<Provider>();

            for (int i = 0; dataGridView1.Rows.Count - 1 > i; i++)
            {
                Provider prov = new Provider();
                prov.IsInactive = false;
                prov.Services = new List<Service>();
                Service serv = new Service();

                prov.Name = dataGridView1.Rows[i].Cells[0].Value.ToString();
                serv.ServiceName = (ServiceEnum)Enum.Parse(typeof(ServiceEnum), dataGridView1.Rows[i].Cells[1].Value.ToString());
                serv.SummPay = dataGridView1.Rows[i].Cells["Column7"].Value == null ? 0 : int.Parse(dataGridView1.Rows[i].Cells["Column7"].Value.ToString());
                if (serv.ServiceName != ServiceEnum.Peni)
                {

                    prov.IsInactive = dataGridView1.Rows[i].Cells["Column12"].Value == null ? false : bool.Parse(dataGridView1.Rows[i].Cells["Column12"].Value.ToString());
                    serv.CurrentChargesSize = dataGridView1.Rows[i].Cells["Column5"].Value == null ? 0 : int.Parse(dataGridView1.Rows[i].Cells["Column5"].Value.ToString());
                    serv.DeptSize = dataGridView1.Rows[i].Cells["Column3"].Value == null ? 0 : int.Parse(dataGridView1.Rows[i].Cells["Column3"].Value.ToString());
                    serv.ProportionCurrentCharges = dataGridView1.Rows[i].Cells["Column6"].Value == null ? 0 : double.Parse(dataGridView1.Rows[i].Cells["Column6"].Value.ToString());
                    serv.ProportionDept = dataGridView1.Rows[i].Cells["Column4"].Value == null ? 0 : double.Parse(dataGridView1.Rows[i].Cells["Column4"].Value.ToString());
                    serv.ProportionFine = dataGridView1.Rows[i].Cells["Column13"].Value == null ? 0 : double.Parse(dataGridView1.Rows[i].Cells["Column13"].Value.ToString());
                    serv.IsExcluded = false;
                    prov.Services.Add(serv);
                    _provs.Add(prov);
                }
                else
                {
                    _provs.LastOrDefault().Services.LastOrDefault().Fine = serv.SummPay;
                }
            }

            // используя LINQ объединяем поставщиков с одинаковым именем
            _provs = _provs
                .GroupBy(o => o.Name)
                .Select(g => new Provider
                {
                    Name = g.Key,
                    IsInactive = g.Select(p => p.IsInactive).FirstOrDefault(),
                    Services = g.SelectMany(p => p.Services).ToList()
                }).ToList();

            //Вызов алгоритма расщепления оплаты
            Algorithm1();
            //вывод данных
            int counter = 0;
            foreach (Provider provider in _provs)
            {
                for (int j = 0; provider.Services.Count > j; j++)
                {
                    Service service = provider.Services[j];
                    dataGridView1.Rows[counter].Cells[8].Value = service.SumSplitting;
                    counter++;
                    if (service.Fine != 0)
                    {
                        dataGridView1.Rows[counter].Cells[8].Value = service.Fine;
                        counter++;
                    }
                }

            }

        }

        /// <summary>
        /// Уточнение оплаты
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void clarify_Click(object sender, EventArgs e)
        {
            ClarifyForm refiForm = new ClarifyForm(this);
            refiForm.Show();
        }

        int sumCharges = 0;
        int sumDept = 0;

        //Алгоритм расщепления оплаты
        private void Algorithm1()
        {
            //  Пункт 1
            sumCharges = _provs.Sum(p => p.Services.Sum(o => o.CurrentChargesSize));
            sumDept = _provs.Sum(p => p.Services.Sum(o => o.DeptSize));

            //  Пункт 2
            //  Учесть суммы, указанные в уточнении оплаты
            if (clarifys != null && clarifys.Count != 0)
            {
                //  А
                //  Если уточнение оплаты указано в разрезе пары услуга-поставщик, 
                //  то оплату расщепить в соответствии с указанными суммой, услугой
                //  и поставщиком и исключить данные пары услуга-поставщик из дальнейшего расщепления.
                Algorithm1Paragraph2a();

                //  B
                var list = clarifys.Where(o => o.ProviderName == "").ToList();
                List<Provider> provList = new List<Provider>(); ;

                if (list.Count != 0) // работа только с улугами, где не определен поставщик
                {
                    for (int j = 0; list.Count > j; j++)
                    {
                        foreach (Provider prov in _provs)
                        {
                            for (int i = 0; prov.Services.Count > i; i++)
                            {
                                if (!prov.Services[i].IsExcluded)
                                {
                                    if (prov.Services[i].ServiceName.Equals(list[j].ServiceName))
                                    {
                                        provList.Add(prov);
                                    }
                                }
                            }
                        }

                    }

                    // b.1
                    algorithm2(provList);
                    // b.1 - конец

                    // b.2 - начало
                    if (_payment > 0)
                    {

                        var activeProvList = provList.Where(prov => prov.IsInactive).ToList();
                        algorithm3(activeProvList); //для действующих поставщиков
                        JoinLists(activeProvList, provList);
                        // b.2 - конец

                        // b.3 - начало
                        if (_payment > 0)
                        {
                            var inactiveProvList = provList.Where(qa => !qa.IsInactive).ToList();
                            algorithm3(inactiveProvList); //для недействующих поставщиков
                            JoinLists(inactiveProvList, provList);
                            // b.3 - конец

                            // b.4 - начало
                            if (_payment > 0)
                            {
                                algorithm5(provList); // расщепление переплаты
                                                      // b.4 - конец
                            }
                            else
                            {
                                return;
                            }
                        }
                        else
                        {
                            return;
                        }
                    }
                    else
                    {
                        return;
                    }
                    // b.5 - начало
                    JoinLists(provList, _provs);
                    for (int j = 0; list.Count > j; j++)
                    {
                        foreach (Provider prov in _provs)
                        {
                            for (int i = 0; prov.Services.Count > i; i++)
                            {
                                if (prov.Services[i].ServiceName == list[j].ServiceName)
                                {
                                    prov.Services[i].IsExcluded = true;
                                }
                            }
                        }
                    }
                    // b.5 - конец
                }
                // с- начало
                if (_payment == 0) return;
                // с- конец                
            }
            // Пункт 3
            //  Погасить текущие начисления за ЖКУ по алгоритму 2
            //  Если остаток оплаты равен нулю, то конец алгоритма

            var provsOfUtilitiesList = _provs
                .Select(prov => new Provider()
                {
                    IsInactive = prov.IsInactive,
                    Name = prov.Name,
                    Services = prov.Services
                                   .Where(o => o.ServiceName == ServiceEnum.Service1
                                    || o.ServiceName == ServiceEnum.Service2
                                    || o.ServiceName == ServiceEnum.Service3
                                    )
                                    .ToList()
                })
                                .ToList();

            algorithm2(provsOfUtilitiesList);
            JoinLists(provsOfUtilitiesList, _provs);
            if (_payment == 0)
            {
                return;
            }
            //

            // Пункт 4
            List<Provider> provsOfOverhaulList = new List<Provider>(); ;
            foreach (Provider prov in _provs)
            {
                for (int j = 0; prov.Services.Count > j; j++)
                {
                    Service service = prov.Services[j];
                    if (service.ServiceName == ServiceEnum.Service4)
                    {

                        provsOfOverhaulList.Add(prov);
                    }
                }
            }


            algorithm2(provsOfOverhaulList);
            JoinLists(provsOfOverhaulList, _provs);

            if (_payment == 0)
            {
                return;
            }
            //

            //Пункт 5 
            var provsOfActiveUtilitiesList = provsOfUtilitiesList.Where(p => !p.IsInactive).ToList();
            algorithm3(provsOfActiveUtilitiesList);
            JoinLists(provsOfActiveUtilitiesList, provsOfUtilitiesList);

            if (_payment == 0)
            {
                JoinLists(provsOfUtilitiesList, _provs);
                return;
            }
            //

            //Пункт 6
            var provsOfInactiveUtilitiesList = provsOfUtilitiesList.Where(p => p.IsInactive).ToList();
            algorithm3(provsOfInactiveUtilitiesList);
            JoinLists(provsOfInactiveUtilitiesList, provsOfUtilitiesList);


            if (_payment == 0)
            {
                JoinLists(provsOfOverhaulList, _provs);
                JoinLists(provsOfUtilitiesList, _provs);
                return;
            }
            //

            //Пункт 7
            var provsOfActiveOverhaulList = provsOfOverhaulList.Where(p => !p.IsInactive).ToList();
            algorithm3(provsOfActiveOverhaulList);
            JoinLists(provsOfActiveOverhaulList, provsOfOverhaulList);

            if (_payment == 0)
            {
                JoinLists(provsOfOverhaulList, _provs);
                JoinLists(provsOfUtilitiesList, _provs);
                return;
            }
            //

            //Пункт 8
            var provsOfInactiveOverhaulListl = provsOfOverhaulList.Where(p => p.IsInactive).ToList();
            algorithm3(provsOfInactiveOverhaulListl);
            JoinLists(provsOfInactiveOverhaulListl, provsOfOverhaulList);

            if (_payment == 0)
            {
                JoinLists(provsOfOverhaulList, _provs);
                JoinLists(provsOfUtilitiesList, _provs);
                return;
            }
            //

            //Пункт 9
            algorithm4(provsOfActiveUtilitiesList);
            JoinLists(provsOfActiveUtilitiesList, provsOfUtilitiesList);
            if (_payment == 0)
            {
                JoinLists(provsOfOverhaulList, _provs);
                JoinLists(provsOfUtilitiesList, _provs);
                return;
            }
            //

            //Пункт 10
            algorithm4(provsOfInactiveUtilitiesList);
            JoinLists(provsOfInactiveUtilitiesList, provsOfUtilitiesList);
            if (_payment == 0)
            {
                JoinLists(provsOfOverhaulList, _provs);
                JoinLists(provsOfUtilitiesList, _provs);
                return;
            }
            //

            //Пункт 11
            algorithm4(provsOfActiveOverhaulList);
            JoinLists(provsOfActiveOverhaulList, provsOfOverhaulList);
            if (_payment == 0)
            {
                JoinLists(provsOfOverhaulList, _provs);
                JoinLists(provsOfUtilitiesList, _provs);
                return;
            }
            //

            //Пункт 12
            algorithm4(provsOfInactiveOverhaulListl);
            JoinLists(provsOfInactiveOverhaulListl, provsOfOverhaulList);
            if (_payment == 0)
            {
                JoinLists(provsOfOverhaulList, _provs);
                JoinLists(provsOfUtilitiesList, _provs);
                return;
            }

            //

            // Пункт 13
            var provsOfFirstMounthList = _provs
                .Select(p => new Provider()
                {
                    IsInactive = p.IsInactive,
                    Name = p.Name,
                    Services = p.Services
                                    .Where(o => o.DeptSize > 0
                                    )
                                    .ToList()
                })
                                .ToList();

            algorithm5(provsOfFirstMounthList);

            //

            JoinLists(provsOfActiveUtilitiesList, provsOfUtilitiesList);
            JoinLists(provsOfInactiveUtilitiesList, provsOfUtilitiesList);
            JoinLists(provsOfActiveOverhaulList, provsOfOverhaulList);
            JoinLists(provsOfInactiveOverhaulListl, provsOfOverhaulList);
            JoinLists(provsOfUtilitiesList, _provs);
            JoinLists(provsOfOverhaulList, _provs);
        }
        /// <summary>
        /// объединение списков
        /// </summary>
        /// <param name="pr"></param>
        /// <param name="provs"></param>
        private void JoinLists(List<Provider> pr, List<Provider> provs)
        {
            for (int a = 0; a < pr.Count; a++)
            {
                for (int j = 0; pr[a].Services.Count > j; j++)
                {
                    Service serviceprovs = pr[a].Services[j];
                    for (int b = 0; b < provs.Count; b++)
                    {
                        for (int i = 0; provs[b].Services.Count > i; i++)
                        {
                            Service servicepr = provs[b].Services[i];
                            if (pr[a].Name == provs[b].Name && serviceprovs.ServiceName == servicepr.ServiceName)
                            {
                                provs[b] = pr[a];
                            }
                        }
                    }
                }
            }
        }


        /// <summary>
        /// алгоритм 2
        /// </summary>
        /// <param name="C"></param> Остаток оплаты
        /// <param name="provs"></param>
        private void algorithm2(List<Provider> provs)
        {
            int sumP = 0;
            int n = provs.Count();
            int D = 0;
            int C = _payment;

            foreach (Provider provider in provs)
            {
                for (int j = 0; provider.Services.Count > j; j++)
                {
                    Service service = provider.Services[j];
                    if (!service.IsExcluded)
                    {
                        service.SummPay = service.CurrentChargesSize + service.DeptSize;
                        if (service.SummPay <= 0)
                        {
                            service.IsExcluded = true;
                        }
                    }
                }

                D += provider.Services.Sum(p => p.SummPay);
            }

            if (D <= C)
            {
                foreach (Provider provider in provs)
                {

                    for (int j = 0; provider.Services.Count > j; j++)
                    {

                        Service service = provider.Services[j];
                        if (!service.IsExcluded)
                        {
                            service.SumSplitting = service.SummPay;
                        }
                    }
                }
            }
            else if (D > C)
            {
                foreach (Provider provider in provs)
                {

                    for (int j = 0; provider.Services.Count > j; j++)
                    {
                        Service service = provider.Services[j];
                        if (!service.IsExcluded)
                        {
                            service.SumSplitting = service.SumSplitting + (int)(C * service.ProportionCurrentCharges);
                        }
                    }
                }
            }

            foreach (Provider provider in provs)
            {
                foreach (Service service in provider.Services)
                {
                    if (!service.IsExcluded)
                    {
                        sumP += service.SumSplitting;
                    }
                }
            }
            _payment = C - sumP;
        }

        /// <summary>
        /// алгоритм 3
        /// </summary>
        /// <param name="C"></param>
        /// <param name="provs"></param>
        private void algorithm3(List<Provider> provs)
        {
            int sumP = 0;
            int C = _payment;
            int A = provs.Sum(o => o.Services.Sum(p => p.DeptSize));
      
            foreach (Provider provider in provs)
            {

                for (int j = 0; provider.Services.Count > j; j++)
                {
                    Service service = provider.Services[j];
                    if (service.DeptSize < 0) { service.IsExcluded = true; }
                }
            }

            if (A <= C)
            {
                foreach (Provider provider in provs)
                {

                    for (int j = 0; provider.Services.Count > j; j++)
                    {
                        if (!provider.Services[j].IsExcluded)
                        {
                            Service service = provider.Services[j];
                            service.SumSplitting = service.DeptSize;
                        }
                    }
                }
            }

            else if (A > C)
            {
                foreach (Provider provider in provs)
                {

                    for (int j = 0; provider.Services.Count > j; j++)
                    {
                        if (!provider.Services[j].IsExcluded)
                        {
                            Service service = provider.Services[j];
                            service.SumSplitting = (int)(C * service.ProportionDept);
                        }
                    }
                }
            }
            sumP = provs.Sum(o => o.Services.Sum(p => p.SumSplitting));

            _payment = C - sumP;
        }

        /// <summary>
        /// алгоритм 4 погашение пени
        /// </summary>
        /// <param name="C"></param>
        /// <param name="provs"></param>
        private void algorithm4(List<Provider> provs)
        {
            int sumFine = 0;
            int sumP = 0;
            int C = _payment;
            int A = provs.Sum(o => o.Services.Sum(p => p.Fine));

            foreach (Provider provider in provs)
            {

                for (int j = 0; provider.Services.Count > j; j++)
                {

                    Service service = provider.Services[j];
                    if (!service.IsExcluded)
                    {
                        if (service.Fine <= 0) { service.IsExcluded = true; }
                    }

                }
            }

            foreach (Provider provider in provs)
            {
                foreach (Service service in provider.Services)
                {
                    if (!service.IsExcluded)
                        sumFine += service.Fine;
                }
            }

            if (sumFine <= C)
            {
                foreach (Provider provider in provs)
                {

                    for (int j = 0; provider.Services.Count > j; j++)
                    {
                        Service service = provider.Services[j];
                        if (!service.IsExcluded)
                        {
                            service.SumSplitting = service.DeptSize;
                        }
                    }
                }
            }
            else if (sumFine > C)
            {
                foreach (Provider provider in provs)
                {

                    for (int j = 0; provider.Services.Count > j; j++)
                    {
                        Service service = provider.Services[j];
                        if (!service.IsExcluded)
                            service.SumSplitting = (int)(C * Math.Round((double)service.Fine / (double)A, 1));
                        else
                            continue;
                    }
                }
            }

            foreach (Provider provider in provs)
            {
                foreach (Service service in provider.Services)
                {
                    if (!service.IsExcluded)
                        sumP += service.SumSplitting;
                }
            }

            _payment = C - sumP;
        }

        /// <summary>
        /// алгоритм 5
        /// </summary>
        /// <param name="C"></param>
        /// <param name="provs"></param>
        private void algorithm5(List<Provider> provs)
        {
            int C = _payment;
            int sumP = 0;
            foreach (Provider provider in provs)
            {

                for (int j = 0; provider.Services.Count > j; j++)
                {
                    Service service = provider.Services[j];
                    if (!service.IsExcluded)
                    {
                        if (service.ProportionCurrentCharges <= 0) { service.IsExcluded = true; }
                    }
                }

            }
            foreach (Provider provider in provs)
            {

                for (int j = 0; provider.Services.Count > j; j++)
                {
                    Service service = provider.Services[j];
                    if (!service.IsExcluded)
                        service.SumSplitting = (int)(C * service.ProportionFine);
                    else
                        continue;
                }
            }

            foreach (Provider provider in provs)
            {
                foreach (Service service in provider.Services)
                {
                    if (!service.IsExcluded)
                        sumP += service.SumSplitting;
                }
            }

            _payment = C - sumP;
        }

        #region
        /// <summary>
        /// Подсчет долей при изменении значения ячейки
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void dataGridView1_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 2 || e.ColumnIndex == 4)
            {
                double summ = 0;

                //Подсчет общей суммы: Размера задолжностей/Текущиее начисления
                for (int i = 0; i < dataGridView1.RowCount - 1; i++)
                {
                    var cell = dataGridView1.Rows[i].Cells[e.ColumnIndex].Value;
                    double a = cell == null ? 0 : double.Parse(cell.ToString());
                    summ += a;
                }

                //Подсчет доли для каждой ячейки: Размера задолжностей/Текущиее начисления
                for (int i = 0; i < dataGridView1.RowCount - 1; i++)
                {
                    var cell = dataGridView1.Rows[i].Cells[e.ColumnIndex].Value;
                    double a = cell == null ? 0 : double.Parse(cell.ToString());
                    dataGridView1.Rows[i].Cells[e.ColumnIndex + 1].Value = Math.Round((a / summ), 1);
                }
            }
        }
        #endregion

        /// <summary>
        /// Алгоритм 1 - пукт 2a
        /// </summary>
        private void Algorithm1Paragraph2a()
        {
            var list = clarifys.Where(p => p.ProviderName != "").ToList();
            if (list.Count != 0)
            {
                for (int j = 0; list.Count > j; j++)
                {
                    foreach (Provider prov in _provs)
                    {
                        for (int i = 0; prov.Services.Count > i; i++)
                        {
                            if (prov.Name == list[j].ProviderName && prov.Services[i].ServiceName == list[j].ServiceName)
                            {
                                prov.Services[i].SumSplitting = list[j].Count;
                                prov.Services[i].IsExcluded = true;
                                _payment = _payment - list[j].Count;
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Заполненеи таблицы при выборе варианта в выпадающем списке
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void exampleNumber_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataGridView1.Rows.Clear();
            string strNumber = exampleNumber.SelectedItem.ToString();
            int number = int.Parse(strNumber);

            #region Объявляем строки
            DataGridViewRow row1 = new DataGridViewRow();
            DataGridViewRow row2 = new DataGridViewRow();
            DataGridViewRow row3 = new DataGridViewRow();
            DataGridViewRow row4 = new DataGridViewRow();
            DataGridViewRow row5 = new DataGridViewRow();
            DataGridViewRow row6 = new DataGridViewRow();
            DataGridViewRow row7 = new DataGridViewRow();
            DataGridViewRow row8 = new DataGridViewRow();
            DataGridViewRow row9 = new DataGridViewRow();
            DataGridViewRow row10 = new DataGridViewRow();
            #endregion

            #region выбор варианта
            switch (number)
            {
                case 1:
                    _payment = 2000;
                    row1 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row1.Cells[0].Value = "ПУ 1";
                    row1.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row1.Cells[2].Value = 600;
                    row1.Cells[4].Value = 1500;
                    dataGridView1.Rows.Add(row1);

                    row2 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row2.Cells[0].Value = "ПУ 2";
                    row2.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row2.Cells[2].Value = 100;
                    row2.Cells[4].Value = 900;
                    dataGridView1.Rows.Add(row2);

                    row3 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row3.Cells[0].Value = "ПУ 2";
                    row3.Cells[1].Value = ServiceEnum.Service3.ToString();
                    row3.Cells[2].Value = 300;
                    row3.Cells[4].Value = 600;
                    dataGridView1.Rows.Add(row3);
                    break;
                case 2:
                    _payment = 3000;
                    row1 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row1.Cells[0].Value = "ПУ 1";
                    row1.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row1.Cells[2].Value = 500;
                    row1.Cells[4].Value = 1500;
                    dataGridView1.Rows.Add(row1);

                    row2 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row2.Cells[0].Value = "ПУ 2";
                    row2.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row2.Cells[2].Value = 200;
                    row2.Cells[4].Value = 900;
                    dataGridView1.Rows.Add(row2);

                    row3 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row3.Cells[0].Value = "ПУ 2";
                    row3.Cells[1].Value = ServiceEnum.Service3.ToString();
                    row3.Cells[2].Value = 300;
                    row3.Cells[4].Value = 600;
                    dataGridView1.Rows.Add(row3);
                    break;
                case 3:
                    _payment = 4350;
                    row1 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row1.Cells[0].Value = "ПУ 1";
                    row1.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row1.Cells[2].Value = 100;
                    row1.Cells[10].Value = true;
                    dataGridView1.Rows.Add(row1);

                    row2 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row2.Cells[0].Value = "ПУ 2";
                    row2.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row2.Cells[2].Value = 600;
                    row2.Cells[4].Value = 1500;
                    dataGridView1.Rows.Add(row2);

                    row3 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row3.Cells[0].Value = "ПУ 2";
                    row3.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row3.Cells[6].Value = 70;
                    dataGridView1.Rows.Add(row3);

                    row4 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row4.Cells[0].Value = "ПУ 3";
                    row4.Cells[10].Value = true;
                    row4.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row4.Cells[2].Value = 150;
                    dataGridView1.Rows.Add(row4);

                    row5 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row5.Cells[0].Value = "ПУ 4";
                    row5.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row5.Cells[2].Value = 100;
                    row5.Cells[4].Value = 900;
                    dataGridView1.Rows.Add(row5);

                    row6 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row6.Cells[0].Value = "ПУ 4";
                    row6.Cells[1].Value = ServiceEnum.Service3.ToString();
                    row6.Cells[2].Value = 300;
                    row6.Cells[4].Value = 600;
                    dataGridView1.Rows.Add(row6);

                    row7 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row7.Cells[0].Value = "ПУ 4";
                    row7.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row7.Cells[6].Value = 130;
                    dataGridView1.Rows.Add(row7);
                    break;

                case 4:
                    _payment = 4450;
                    row1 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row1.Cells[0].Value = "ПУ 1";
                    row1.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row1.Cells[2].Value = 100;
                    row1.Cells[10].Value = true;
                    dataGridView1.Rows.Add(row1);

                    row2 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row2.Cells[0].Value = "ПУ 2";
                    row2.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row2.Cells[2].Value = 600;
                    row2.Cells[4].Value = 1500;
                    dataGridView1.Rows.Add(row2);

                    row3 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row3.Cells[0].Value = "ПУ 2";
                    row3.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row3.Cells[6].Value = 70;
                    dataGridView1.Rows.Add(row3);

                    row4 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row4.Cells[0].Value = "ПУ 3";
                    row4.Cells[10].Value = true;
                    row4.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row4.Cells[2].Value = 150;
                    dataGridView1.Rows.Add(row4);

                    row5 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row5.Cells[0].Value = "ПУ 4";
                    row5.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row5.Cells[2].Value = 100;
                    row5.Cells[4].Value = 900;
                    dataGridView1.Rows.Add(row5);

                    row6 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row6.Cells[0].Value = "ПУ 4";
                    row6.Cells[1].Value = ServiceEnum.Service3.ToString();
                    row6.Cells[2].Value = 300;
                    row6.Cells[4].Value = 600;
                    dataGridView1.Rows.Add(row6);

                    row7 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row7.Cells[0].Value = "ПУ 4";
                    row7.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row7.Cells[6].Value = 130;
                    dataGridView1.Rows.Add(row7);
                    break;
                case 5:
                    _payment = 5000;
                    DataGridViewRow row18 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row18.Cells[0].Value = "ПУ 1";
                    row18.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row18.Cells[2].Value = 100;
                    row18.Cells[10].Value = true;
                    dataGridView1.Rows.Add(row18);

                    DataGridViewRow row19 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row19.Cells[0].Value = "ПУ 2";
                    row19.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row19.Cells[2].Value = 600;
                    row19.Cells[4].Value = 1500;
                    dataGridView1.Rows.Add(row19);

                    DataGridViewRow row20 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row20.Cells[0].Value = "ПУ 2";
                    row20.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row20.Cells[6].Value = 70;
                    dataGridView1.Rows.Add(row20);

                    DataGridViewRow row21 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row21.Cells[0].Value = "ПУ 3";
                    row21.Cells[10].Value = true;
                    row21.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row21.Cells[2].Value = 150;
                    row21.Cells[4].Value = 150;
                    dataGridView1.Rows.Add(row21);

                    DataGridViewRow row22 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row22.Cells[0].Value = "ПУ 4";
                    row22.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row22.Cells[2].Value = 100;
                    row22.Cells[4].Value = 900;
                    dataGridView1.Rows.Add(row22);

                    DataGridViewRow row23 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row23.Cells[0].Value = "ПУ 4";
                    row23.Cells[1].Value = ServiceEnum.Service3.ToString();
                    row23.Cells[2].Value = 300;
                    row23.Cells[4].Value = 600;
                    dataGridView1.Rows.Add(row23);

                    DataGridViewRow row24 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row24.Cells[0].Value = "ПУ 4";
                    row24.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row24.Cells[7].Value = 130;
                    dataGridView1.Rows.Add(row24);
                    break;
                case 6:
                    _payment = 3700;
                    row1 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row1.Cells[0].Value = "ПУ 1";
                    row1.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row1.Cells[2].Value = 300;
                    row1.Cells[4].Value = 1500;
                    dataGridView1.Rows.Add(row1);

                    row2 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row2.Cells[0].Value = "ПУ 1";
                    row2.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row2.Cells[6].Value = 100;
                    dataGridView1.Rows.Add(row2);

                    row3 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row3.Cells[0].Value = "ПУ 2";
                    row3.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row3.Cells[2].Value = 500;
                    row3.Cells[4].Value = 900;
                    dataGridView1.Rows.Add(row3);

                    row4 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row4.Cells[0].Value = "ПУ 2";
                    row4.Cells[1].Value = ServiceEnum.Service3.ToString();
                    row4.Cells[2].Value = -200;
                    row4.Cells[4].Value = 600;
                    dataGridView1.Rows.Add(row4);

                    row5 = (DataGridViewRow)dataGridView1.Rows[0].Clone();
                    row5.Cells[0].Value = "ПУ 2";
                    row5.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row5.Cells[6].Value = 100;
                    dataGridView1.Rows.Add(row5);
                    break;
                case 7:
                    break;
                case 8:
                    break;
                case 9:
                    break;
                case 10:
                    row1.Cells[0].Value = "ПУ 1";
                    row1.Cells[1].Value = ServiceEnum.Service1.ToString();
                    row1.Cells[2].Value = 300;
                    row1.Cells[4].Value = 1500;
                    dataGridView1.Rows.Add(row1);

                    row2.Cells[0].Value = "ПУ 1";
                    row2.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row2.Cells[6].Value = 100;
                    dataGridView1.Rows.Add(row2);

                    row3.Cells[0].Value = "ПУ 2";
                    row3.Cells[1].Value = ServiceEnum.Service2.ToString();
                    row3.Cells[2].Value = 500;
                    row3.Cells[4].Value = 900;
                    dataGridView1.Rows.Add(row3);

                    row4.Cells[0].Value = "ПУ 2";
                    row4.Cells[1].Value = ServiceEnum.Service3.ToString();
                    row4.Cells[2].Value = -200;
                    row4.Cells[4].Value = 600;
                    dataGridView1.Rows.Add(row4);

                    row5.Cells[0].Value = "ПУ 2";
                    row5.Cells[1].Value = ServiceEnum.Peni.ToString();
                    row5.Cells[6].Value = 100;
                    dataGridView1.Rows.Add(row10);
                    break;
                case 11:
                    break;
            }
            #endregion
        }
    }
}
