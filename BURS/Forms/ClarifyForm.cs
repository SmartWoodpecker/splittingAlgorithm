using SplittingAlgorithm.Enums;
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
    public partial class ClarifyForm : Form
    {
        private MainForm _mainform;

        /// <summary>
        /// Конструктор ClarifyForm
        /// </summary>
        public ClarifyForm()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Конструктор ClarifyForm
        /// </summary>
        /// <param name="form">Главная форма</param>
        public ClarifyForm(MainForm mainform) : this()
        {
            _mainform = mainform;
        }

        /// <summary>
        /// Событие при нажатии на кнопку "Сохранить"
        /// </summary>
        /// <param name="sender">Объект</param>
        /// <param name="e">Данные события</param>
        private void button1_Click(object sender, EventArgs e)
        {
            if (IsValidate())
            {
                List<Clarify> clarifys = new List<Clarify>();

                for (int i = 0; dataGridView1.Rows.Count - 1 > i; i++)
                {
                    Clarify refineme = new Clarify();
                    object provaderName = dataGridView1.Rows[i].Cells["Column1"].Value;
                    object serviceName = dataGridView1.Rows[i].Cells["Column2"].Value;

                    refineme.ProviderName = provaderName != null ? provaderName.ToString() : "";
                    refineme.ServiceName = (ServiceEnum)Enum.Parse(typeof(ServiceEnum), serviceName.ToString());
                    refineme.Count = int.Parse(dataGridView1.Rows[i].Cells["Column3"].Value.ToString());
                    clarifys.Add(refineme);
                }

                _mainform.clarifys = clarifys;

                if (clarifys.Sum(clar => clar.Count) > _mainform._payment)
                {
                    MessageBox.Show("Сумма введенных уточнений привышает размер оплаты! Проверть введенные данные!");
                    _mainform.clarifys.Clear();
                }
                else
                {
                    Close(); //Закрытие формы
                }
            }
            else
            {
                MessageBox.Show("Введит уточнение");
            }
        }

        /// <summary>
        /// Проверка на введение уточнения
        /// </summary>
        /// <returns></returns>
        public bool IsValidate()
        {
            for (int i = 0; dataGridView1.Rows.Count - 1 > i; i++)
            {
                if (dataGridView1.Rows[i].Cells["Column3"].Value == null)

                    return false;
            }

            return true;
        }
    }
}
