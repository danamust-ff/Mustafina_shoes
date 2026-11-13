using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Media;

namespace Mustafina_shoes
{
    public class ProductView
    {
        public int IdProduct { get; set; }
        public string NameProduct { get; set; }
        public string Compound { get; set; }
        public decimal PriceProduct { get; set; }
        public string ImagePath { get; set; }

        // Данные о категориях (для фильтрации и поиска)
        public string CategoryName { get; set; }
        public string SubCategoryName { get; set; }
        public string ManufactureName { get; set; }

        // Данные о складе
        public int TotalInStock { get; set; }
        public int QuantityInStock { get; set; }
        public string AvailableSizes { get; set; }
        public Brush RowBackground { get; set; }

        public string StockAvailability => QuantityInStock >= 5
            ? "Количество: много"
            : "Количество: мало";

        // Видимость кнопки "Купить" (только для клиентов, IdRole == 3)
        public System.Windows.Visibility BuyVisibility =>
            Manager.CurrentUser != null && Manager.CurrentUser.IdRole == 3
                ? System.Windows.Visibility.Visible
                : System.Windows.Visibility.Collapsed;
    }
}
