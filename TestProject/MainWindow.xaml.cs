using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.Linq;
using System.Globalization;
using System.Collections;
using Microsoft.Win32;
using System.Drawing.Printing;
using System.Drawing;
using Excel = Microsoft.Office.Interop.Excel;
using System.IO;

namespace TestProject
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : System.Windows.Window
    {
        IList<Storage> storageReceiptList { get; set; }
        IList<Storage> storageShipmentList { get; set; }
        public MainWindow()
        {
            InitializeComponent();           
            Overwriting();
            BalanceStorage(DateTime.Now);
        }

        //Запись таблиц из xml файла.
        public void Overwriting()
        {
            storageReceiptList = Converter(XDocument.Load("OOOSealReceipt.xml"));
            storageShipmentList = Converter(XDocument.Load("OOOSealShipment.xml"));
            StorageReceiptList.ItemsSource = storageReceiptList.ToList();
            StorageShipmentList.ItemsSource = storageShipmentList.ToList();
        }

        //Записть данных из xml файла.
        private List<Storage> Converter(XDocument xdoc) 
        {
            // Получаем корневой узел.
            XElement XELReturn = xdoc.Element("return");
            List<Storage> storageList = new List<Storage>();
            if (XELReturn != null)
            {
                //Получение атрибутов storage.
                foreach (XElement XELstorage in XELReturn.Elements("storage"))
                {
                    //Получение атрибутов product.
                    XElement nameStorage = XELstorage?.Element("storage_name");
                    foreach (XElement XELproduct in XELstorage.Elements("product"))
                    {
                        Storage storage = new Storage();
                        XElement nameProduct = XELproduct?.Element("product_name");
                        XElement quentityProduct = XELproduct?.Element("count");
                        XElement weighst = XELproduct?.Element("m");
                        XElement fragile = XELproduct?.Element("fragile");
                        XElement date = XELproduct?.Element("date");

                        IFormatProvider formatter = new NumberFormatInfo { NumberDecimalSeparator = "." };
                        storage.NameStorage = nameStorage?.Value;
                        storage.NameProduct = nameProduct?.Value;
                        storage.QuentityProduct = int.Parse(quentityProduct.Value);
                        storage.Weighst = double.Parse(weighst?.Value, formatter);
                        if (fragile.Value.ToLower() == "да")
                        {
                            storage.Fragile = true;
                        }
                        else
                            storage.Fragile = false;
                        storage.Date = DateTime.Parse(date?.Value);
                        storageList.Add(storage);
                    }
                }
            }
            return storageList;
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e) 
        {
            //Переходы по вкладкам, для обновления таблиц
            StorageControl.SelectedItem = ReceiptControl;
            StorageControl.SelectedItem = ShipmentControl;
            StorageControl.SelectedItem = BalancesControl;
            try
            {
                Excel.Application xlApp = new Excel.Application();
                xlApp.SheetsInNewWorkbook = 3;
                Excel.Workbook ObjWorkBook = xlApp.Workbooks.Add(Type.Missing);
                //Имя Excel файла.
                string fileName = "D:\\ExcelFile.xlsx"; 
                //Сохранить в эксель файл.
                ObjWorkBook.SaveAs(fileName);
                //Открываем Excel файл.
                Excel.Workbook xlWb = xlApp.Workbooks.Open(fileName);

                //Вызов метода для сохранения таблиц
                SaveFile(StorageReceiptList, 1, xlWb);
                SaveFile(StorageShipmentList, 2, xlWb);
                SaveFile(StorageBalancesList,3, xlWb);
                //Закрыть и сохранить книгу.
                xlWb.Close(true); 
                xlApp.Quit();
                MessageBox.Show("Файл успешно сохранён!");
            }
            catch(System.Runtime.InteropServices.COMException ex)
            {
                MessageBox.Show("Нет доступа к файлу\n" + ex.ToString());
            }
        }

        // Метод расчёт запасов на складах.
        private void BalanceStorage(DateTime? dateBalance) 
        {           
            IList<Storage> storagesReceipt = new List<Storage>();
            IList<Storage> storagesShipment = new List<Storage>();

            //Получение уникальных поставок.
            for (int i = 0; i < storageReceiptList.Count - 1; i++) 
            {
                if(storageReceiptList[i].Date >= dateBalance && storagesReceipt.Count == 0)
                {
                    storagesReceipt.Add(storageReceiptList[i]);
                }
                else if (storageReceiptList[i].Date >= dateBalance)
                {
                    int kol = 0;
                    for (int j = 0; j < storagesReceipt.Count; j++)
                    {
                        if (storageReceiptList[i].NameProduct == storagesReceipt[j].NameProduct)
                        {
                            kol++;
                        }
                    }
                    if (1 > kol)
                    {
                        storagesReceipt.Add(storageReceiptList[i]);
                    }
                }
            }

            //Получение уникальных отгруженных товаров.
            for (int i = 0; i < storageShipmentList.Count - 1; i++) 
            {
                if (storageShipmentList[i].Date >= dateBalance && storagesShipment.Count == 0)
                {
                    storagesShipment.Add(storageShipmentList[i]);
                }
                else if(storageShipmentList[i].Date >= dateBalance)
                {
                    int kol = 0;
                    for (int j = 0; j < storagesShipment.Count; j++)
                    {
                        if (storageShipmentList[i].NameProduct == storagesShipment[j].NameProduct)
                        {
                            kol++;
                        }
                    }
                    if (1 > kol)
                    {
                        storagesShipment.Add(storageShipmentList[i]);
                    }
                }
            }

            // Расчёт поступивших товаров.
            for (int j = 0; j < storagesReceipt.Count;j++) 
            {
                storagesReceipt[j].QuentityProduct = storageReceiptList.Where(x => x.NameProduct == storagesReceipt[j].NameProduct).Sum(x => x.QuentityProduct);
            }

            //Расчёт отгруженных товаров.
            for (int j = 0; j < storagesShipment.Count; j++) 
            {
                storagesShipment[j].QuentityProduct = storageShipmentList.Where(x => x.NameProduct == storagesShipment[j].NameProduct).Sum(x => x.QuentityProduct);
            }

            //Расчёт запасов на складах.
            for (int i = 0; i < storagesReceipt.Count; i++)
            {
                for(int j = 0; j < storagesShipment.Count;j++)
                {
                    if(storagesReceipt[i].NameProduct == storagesShipment[j].NameProduct)
                    {
                        storagesReceipt[i].QuentityProduct -= storagesShipment[j].QuentityProduct;
                    }
                }
                storagesReceipt[i].Weighst *= storagesReceipt[i].QuentityProduct;
            }
            StorageBalancesList.ItemsSource = storagesReceipt.ToList();
        }

        /// <summary>
        /// Метод для сохранения таблиц
        /// </summary>
        /// <param name="SaveXmlGrid">Передаваемая таблица</param>
        /// <param name="NumberList">Номер листа</param>
        /// <param name="xlWb">Объект хранения таблиц</param>
        private void SaveFile(DataGrid SaveXmlGrid, int NumberList, Excel.Workbook xlWb)
        {
            //Выбор листа в файле для использования.
            Excel.Worksheet xlSht = xlWb.Sheets[NumberList]; 
            Microsoft.Office.Interop.Excel.Range range;
            //Запись названия столбцов.
            for (int i = 0; i < SaveXmlGrid.Columns.Count; i++)
            {
                DataGridColumn column = SaveXmlGrid.Columns[i];
                range = (Microsoft.Office.Interop.Excel.Range)xlSht.Cells[1, i + 1];
                range.Value2 = column.Header.ToString();
            }

            ItemContainerGenerator generator = SaveXmlGrid.ItemContainerGenerator;
            string[] propertyName = new string[] { "NameStorage", "NameProduct", "QuentityProduct", "Weighst", "Fragile", "Date" };

            //Запись строк по столбцам.
            for (int i = 2; i < generator.Items.Count + 1; i++)
            {
                DataGridRow row = (DataGridRow)generator.ContainerFromIndex(i - 2);
                //Проверка на доступ к информации таблицам.
                if(row is null)
                {
                    MessageBox.Show("Таблицы не прогружены");
                    Overwriting();
                    break;
                }
                //определение столбцов для заполнения.
                Type type = row.Item.GetType();
                for (int j = 0; j < type.GetProperties().Length; j++)
                {
                    //Проверка типов данных для корректной записи.
                    string stringData;
                    if (type.GetProperty(propertyName[j]).GetValue(row.Item) is DateTime?)
                    {
                        DateTime date = (DateTime)type.GetProperty(propertyName[j]).GetValue(row.Item);
                        stringData = date.Date.ToShortDateString();
                    }
                    else if (type.GetProperty(propertyName[j]).GetValue(row.Item) is bool)
                    {
                        bool data = (bool)type.GetProperty(propertyName[j]).GetValue(row.Item);
                        stringData = data == true ? "да" : "нет";
                    }
                    else
                    {
                        stringData = type.GetProperty(propertyName[j]).GetValue(row.Item).ToString();
                    }
                    //Запись
                    range = (Microsoft.Office.Interop.Excel.Range)xlSht.Cells[i, j + 1];
                    range.Value2 = stringData;
                }
            }
        }
        private void DateBalancesDatePicker_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            //Передача даты для создания таблицы запасов товара на складах.
            BalanceStorage(DateBalancesDatePicker.SelectedDate);
            Overwriting();
        }
    }

}
