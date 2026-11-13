using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace Mustafina_shoes
{
    public partial class ProductPage : Page
    {
        private List<ProductView> _allProducts = new List<ProductView>();
        private bool _isInitialized = false;
        private bool _isGuest;

        public ProductPage(User user)
        {
            InitializeComponent();

            // 1. Настройка интерфейса под роль
            _isGuest = user == null || user.IdRole == 0;

            if (!_isGuest)
            {
                FIOTB.Text = $"{user.LastName} {user.FirstName} {user.Patronymic}";

                if (user.IdRole == 1) RoleTB.Text = "Администратор";
                else if (user.IdRole == 2) RoleTB.Text = "Менеджер";
                else if (user.IdRole == 3) RoleTB.Text = "Клиент";
            }
            else
            {
                FIOTB.Text = "";
                RoleTB.Text = "Гость";
                ComboSort.Visibility = Visibility.Collapsed;
                ComboType.Visibility = Visibility.Collapsed;
                TBSearch.Visibility = Visibility.Collapsed;
            }

            // 2. Загрузка данных из базы
            LoadProducts();

            // 3. Инициализация фильтров
            ComboSort.SelectedIndex = 0;
            ComboType.SelectedIndex = 0;

            _isInitialized = true;
            ApplyFilter();
        }

        private void LoadProducts()
        {
            try
            {
                var context = Mustafina_ShoesEntities.GetContext();

                // Загружаем товары вместе со связанными таблицами (.Include)
                var products = context.Product
                    .Include("Category")
                    .Include("Subcategory")
                    .Include("Manufacture")
                    .Include("Stock")
                    .Include("Stock.Size")
                    .ToList();

                // Преобразуем в ProductView
                _allProducts = products.Select(p => new ProductView
                {
                    IdProduct = p.IdProduct,
                    NameProduct = p.NameProduct,
                    Compound = p.Compound,
                    PriceProduct = p.PriceProduct,
                    ImagePath = GetImagePath(p.Image),

                    CategoryName = p.Category != null ? p.Category.Category1 : "",
                    SubCategoryName = p.Subcategory != null ? p.Subcategory.Subcategory1 : "",
                    ManufactureName = p.Manufacture != null ? p.Manufacture.Manufacture1 : "",

                    // Считаем общий остаток на складе
                    TotalInStock = p.Stock != null ? p.Stock.Sum(s => s.OuantityInStock) : 0,
                    QuantityInStock = p.Stock != null ? p.Stock.Sum(s => s.OuantityInStock) : 0,

                    // Собираем список доступных размеров (где остаток > 0)
                    AvailableSizes = p.Stock != null && p.Stock.Any(s => s.OuantityInStock > 0 && s.Size != null)
                        ? string.Join(", ", p.Stock.Where(s => s.OuantityInStock > 0 && s.Size != null)
                                                   .Select(s => s.Size.Size1).OrderBy(sz => sz))
                        : "Нет в наличии"
                }).ToList();

                // Красим строки в красный, если товара меньше 3 штук
                foreach (var item in _allProducts)
                {
                    item.RowBackground = item.TotalInStock <= 3 && item.TotalInStock > 0
                        ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8080"))
                        : Brushes.White;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки каталога: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (!_isInitialized) return;

            if (_isGuest)
            {
                ProductListView.ItemsSource = _allProducts.ToList();
                return;
            }

            // Берем данные из памяти
            var filtered = _allProducts.AsEnumerable();

            // 1. Поиск (Защита от null с помощью ?? "")
            string search = (TBSearch.Text ?? "").Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(p =>
                    (p.NameProduct ?? "").ToLower().Contains(search) ||
                    (p.Compound ?? "").ToLower().Contains(search) ||
                    (p.ManufactureName ?? "").ToLower().Contains(search) ||
                    (p.CategoryName ?? "").ToLower().Contains(search) ||
                    (p.SubCategoryName ?? "").ToLower().Contains(search)
                );
            }

            // 2. Фильтр по категории (учитывая вашу базу)
            // Судя по вашим индексам: 1 - Женская, 2 - Мужская, 3 - Детская
            switch (ComboType.SelectedIndex)
            {
                case 1: filtered = filtered.Where(p => p.CategoryName == "Женская обувь" || p.CategoryName == "Женская"); break;
                case 2: filtered = filtered.Where(p => p.CategoryName == "Мужская обувь" || p.CategoryName == "Мужская"); break;
                case 3: filtered = filtered.Where(p => p.CategoryName == "Детская обувь" || p.CategoryName == "Детская"); break;
                    // Примечание: если названия категорий в базе другие, поменяйте их в кавычках выше!
            }

            // 3. Сортировка
            switch (ComboSort.SelectedIndex)
            {
                case 1: filtered = filtered.OrderBy(p => p.NameProduct); break;
                case 2: filtered = filtered.OrderBy(p => p.PriceProduct); break;
                case 3: filtered = filtered.OrderByDescending(p => p.PriceProduct); break;
            }

            // 4. Вывод на экран
            ProductListView.ItemsSource = filtered.ToList();
        }

        // --- ЛОГИКА РАБОТЫ С КАРТИНКАМИ ---
        private string GetImagePath(string imageName)
        {
            if (!string.IsNullOrWhiteSpace(imageName))
            {
                string fileName = System.IO.Path.GetFileName(imageName.Trim());
                return $"/Mustafina_shoes;component/res/Images/{fileName}";
            }

            return GetDefaultImagePath();
        }

        private string GetDefaultImagePath()
        {
            return "/Mustafina_shoes;component/res/lalala.png";
        }

        // --- ОБРАБОТЧИКИ СОБЫТИЙ ---
        private void TBSearch_TextChanged(object sender, TextChangedEventArgs e) { ApplyFilter(); }
        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e) { ApplyFilter(); }
        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e) { ApplyFilter(); }

        // Кнопка "Купить"
        private void BtnBuy_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.CurrentUser == null || Manager.CurrentUser.IdRole != 3)
            {
                MessageBox.Show("Оформление заказа доступно только клиентам.", "Недоступно", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var button = sender as Button;
            var selectedProduct = button?.DataContext as ProductView;
            if (selectedProduct == null)
            {
                MessageBox.Show("Не удалось определить выбранный товар.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            Manager.MainFrame.Navigate(new AddEditOrderPage(selectedProduct));
        }
    }
}