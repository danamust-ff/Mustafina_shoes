using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Mustafina_shoes
{
    public partial class AddEditOrderPage : Page
    {
        private int? _orderId;
        private List<ItemView> _items = new List<ItemView>();
        private bool _canEditItems = false;

        public AddEditOrderPage(int? orderId)
        {
            InitializeComponent();
            _orderId = orderId;
            LoadClients();

            if (orderId.HasValue)
            {
                // Открытие существующего заказа
                LoadOrder(orderId.Value);

                if (Manager.IsAdmin)
                {
                    // Админ — редактирует
                    _canEditItems = true;
                    TbTitle.Text = "Редактирование заказа";
                }
                else
                {
                    // Менеджер / пользователь — только просмотр
                    _canEditItems = false;
                    BtnAddItem.Visibility = Visibility.Collapsed;
                    BtnRemoveItem.Visibility = Visibility.Collapsed;
                    TbTitle.Text = "Просмотр заказа";
                    ComboClient.IsEnabled = false;
                    DpDate.IsEnabled = false;

                    if (Manager.IsManager)
                    {
                        BtnSave.Visibility = Visibility.Collapsed;
                        BtnBackToOrders.Visibility = Visibility.Visible;
                    }
                }
            }
            else
            {
                // Новый заказ — все авторизованные могут редактировать
                _canEditItems = true;
                TbTitle.Text = "Новый заказ";
                DpDate.SelectedDate = DateTime.Now;
                if (ComboClient.Items.Count > 0) ComboClient.SelectedIndex = 0;
            }
        }

        private void BtnBackToOrders_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new OrdersPage());
        }

        public AddEditOrderPage(ProductView selectedProduct) : this((int?)null)
        {
            if (selectedProduct != null)
                ShowItemSelectionDialog(selectedProduct.IdProduct);
        }

        private void LoadClients()
        {
            var context = Mustafina_ShoesEntities.GetContext();

            // Менеджер и админ — видят всех клиентов
            if (Manager.IsAdmin || Manager.IsManager)
            {
                var users = context.User.OrderBy(u => u.LastName).ToList();
                ComboClient.ItemsSource = users;
            }
            else
            {
                // Обычный пользователь — только себя
                var me = context.User
                    .Where(u => u.IdUser == Manager.CurrentUser.IdUser)
                    .ToList();
                ComboClient.ItemsSource = me;
                ComboClient.SelectedIndex = 0;
                ComboClient.IsEnabled = false;
                DpDate.IsEnabled = false;
            }
        }

        private void LoadOrder(int orderId)
        {
            var context = Mustafina_ShoesEntities.GetContext();

            // 1. Загружаем сам заказ (БЕЗ .Include("OrderItems"))
            var order = context.Order.FirstOrDefault(o => o.IdOrder == orderId);

            if (order == null)
            {
                MessageBox.Show("Заказ не найден.");
                return;
            }

            DpDate.SelectedDate = order.DateOrder;

            var client = context.User.FirstOrDefault(u => u.IdUser == order.IdUser);
            if (client != null)
                ComboClient.SelectedItem = client;

            _items = new List<ItemView>();

            // 2. Загружаем позиции заказа отдельно
            var orderItems = context.OrderItems.Where(i => i.IdOrder == orderId).ToList();

            // 3. Проходимся циклом по позициям
            foreach (var i in orderItems)
            {
                var stock = context.Stock
                    .Include("Product")
                    .Include("Product.Category")
                    .Include("Product.Manufacture")
                    .Include("Product.Subcategory")
                    .Include("Product.Stock.Size")
                    .Include("Size")
                    .FirstOrDefault(s => s.IdStock == i.IdStock);

                _items.Add(new ItemView
                {
                    StockId = i.IdStock,
                    ProductName = stock != null && stock.Product != null
                        ? stock.Product.NameProduct
                        : ("Товар #" + i.IdStock),
                    SizeName = stock != null && stock.Size != null
                        ? stock.Size.Size1.ToString()
                        : "—",
                    Price = i.UnitPrice, // Историческая цена заказа не пересчитывается
                    OriginalPrice = stock != null && stock.Product != null
                        ? stock.Product.PriceProduct
                        : i.UnitPrice,
                    Quantity = i.OrderItemQuantity,
                    ImagePath = stock != null && stock.Product != null ? GetImagePath(stock.Product.Image) : GetDefaultImagePath(),
                    ManufactureName = stock != null && stock.Product != null && stock.Product.Manufacture != null
                        ? stock.Product.Manufacture.Manufacture1 : "",
                    CategoryName = stock != null && stock.Product != null && stock.Product.Category != null
                        ? stock.Product.Category.Category1 : "",
                    Compound = stock != null && stock.Product != null ? stock.Product.Compound : "",
                    Description = stock != null && stock.Product != null ? stock.Product.Description : "",
                    SizeRange = stock != null && stock.Product != null ? GetSizeRange(stock.Product) : "—",
                    AvailableSizes = stock != null && stock.Product != null ? GetAvailableSizes(stock.Product) : "—"
                });
            }

            RefreshItems();
        }

        private void RefreshItems()
        {
            // Прокидываем флаг «можно редактировать» в каждый ItemView
            foreach (var item in _items)
                item.CanEditQuantity = _canEditItems;

            ListViewItems.ItemsSource = null;
            ListViewItems.ItemsSource = _items;

            decimal total = _items.Sum(i => i.Price * i.Quantity);
            TbTotal.Text = $"Итого: {total:N2} руб.";
        }

        private void DpDate_SelectedDateChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_orderId.HasValue || !DpDate.SelectedDate.HasValue || _items.Count == 0)
                return;

            var context = Mustafina_ShoesEntities.GetContext();
            foreach (var item in _items)
            {
                var stock = context.Stock
                    .Include("Product")
                    .FirstOrDefault(s => s.IdStock == item.StockId);

                if (stock != null && stock.Product != null)
                {
                    item.OriginalPrice = stock.Product.PriceProduct;
                    item.Price = CalculatePrice(context, stock.Product, DpDate.SelectedDate.Value);
                }
            }

            RefreshItems();
        }

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            ShowItemSelectionDialog();
        }

        private void ShowItemSelectionDialog(int? productId = null)
        {
            var dlg = new Window
            {
                Title = "Выбор товара",
                Width = 980,
                Height = 650,
                MinWidth = 760,
                MinHeight = 500,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = Window.GetWindow(this),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#F7FBFA")),
                ResizeMode = ResizeMode.CanResize
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var title = new TextBlock
            {
                Text = productId.HasValue ? "Выберите размер выбранного товара" : "Добавьте товар в заказ",
                FontSize = 22,
                FontWeight = FontWeights.Bold,
                Foreground = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#2F4F4F")),
                Margin = new Thickness(18, 15, 18, 12)
            };
            Grid.SetRow(title, 0);
            grid.Children.Add(title);

            var searchPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(15, 0, 15, 12),
                VerticalAlignment = VerticalAlignment.Center,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D2F6E7"))
            };

            var lblSearch = new TextBlock { Text = "Поиск", FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(12, 0, 7, 0) };
            var txtSearch = new TextBox { Width = 300, Height = 30, FontSize = 14, Margin = new Thickness(0, 0, 25, 0), VerticalContentAlignment = VerticalAlignment.Center, Padding = new Thickness(7, 0, 7, 0) };
            var lblCat = new TextBlock { Text = "Категория", FontSize = 14, FontWeight = FontWeights.SemiBold, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 7, 0) };
            var comboCat = new ComboBox { Width = 230, Height = 30, FontSize = 14, Padding = new Thickness(5, 0, 5, 0) };

            searchPanel.Children.Add(lblSearch);
            searchPanel.Children.Add(txtSearch);
            searchPanel.Children.Add(lblCat);
            searchPanel.Children.Add(comboCat);
            Grid.SetRow(searchPanel, 1);
            grid.Children.Add(searchPanel);

            var lv = new ListView
            {
                Margin = new Thickness(15, 0, 15, 10),
                FontSize = 14,
                BorderBrush = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#C8DEDA")),
                BorderThickness = new Thickness(1),
                Background = System.Windows.Media.Brushes.White,
                SelectionMode = SelectionMode.Single
            };

            var itemStyle = new Style(typeof(ListViewItem));
            itemStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 7, 8, 7)));
            itemStyle.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
            var selectedTrigger = new Trigger { Property = ListViewItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(Control.BackgroundProperty,
                new System.Windows.Media.SolidColorBrush((System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#BDE9DD"))));
            selectedTrigger.Setters.Add(new Setter(Control.ForegroundProperty, System.Windows.Media.Brushes.Black));
            itemStyle.Triggers.Add(selectedTrigger);
            lv.ItemContainerStyle = itemStyle;

            var view = new GridView();
            view.Columns.Add(new GridViewColumn { Header = "Товар", Width = 365, DisplayMemberBinding = new System.Windows.Data.Binding("ProductName") });
            view.Columns.Add(new GridViewColumn { Header = "Категория", Width = 135, DisplayMemberBinding = new System.Windows.Data.Binding("CategoryName") });
            view.Columns.Add(new GridViewColumn { Header = "Размер", Width = 90, DisplayMemberBinding = new System.Windows.Data.Binding("SizeName") });
            view.Columns.Add(new GridViewColumn { Header = "Цена", Width = 145, DisplayMemberBinding = new System.Windows.Data.Binding("Price") { StringFormat = "{0:N2} руб." } });
            view.Columns.Add(new GridViewColumn { Header = "Остаток", Width = 100, DisplayMemberBinding = new System.Windows.Data.Binding("QuantityInStock") });
            lv.View = view;

            Grid.SetRow(lv, 2);
            grid.Children.Add(lv);

            var btnOk = new Button
            {
                Content = "Добавить выбранное",
                Height = 40,
                Width = 220,
                Margin = new Thickness(15, 0, 15, 15),
                FontSize = 14,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#70B2AF")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(btnOk, 3);
            grid.Children.Add(btnOk);

            var context = Mustafina_ShoesEntities.GetContext();
            var allStocks = context.Stock
                .Include("Product")
                .Include("Product.Category")
                .Include("Product.Manufacture")
                .Include("Product.Subcategory")
                .Include("Product.Stock.Size")
                .Include("Size")
                .Where(s => s.OuantityInStock > 0 && (!productId.HasValue || s.Product.IdProduct == productId.Value))
                .ToList()
                .Select(s => new StockSelection
                {
                    IdStock = s.IdStock,
                    ProductName = s.Product.NameProduct,
                    CategoryName = s.Product.Category != null ? s.Product.Category.Category1 : "",
                    SizeName = s.Size.Size1.ToString(),
                    OriginalPrice = s.Product.PriceProduct,
                    Price = CalculatePrice(context, s.Product, DpDate.SelectedDate ?? DateTime.Today),
                    QuantityInStock = s.OuantityInStock,
                    Display = $"{s.Product.NameProduct} | Размер {s.Size.Size1} | {CalculatePrice(context, s.Product, DpDate.SelectedDate ?? DateTime.Today):N2} руб. | Остаток: {s.OuantityInStock}"
                })
                .ToList();

            var categories = new List<string> { "Все категории" };
            categories.AddRange(allStocks.Select(s => s.CategoryName).Distinct().OrderBy(c => c));
            comboCat.ItemsSource = categories;
            comboCat.SelectedIndex = 0;

            Action refreshList = () =>
            {
                var filtered = allStocks.AsEnumerable();

                string search = txtSearch.Text.Trim().ToLower();
                if (!string.IsNullOrEmpty(search))
                {
                    filtered = filtered.Where(s =>
                        (s.ProductName ?? "").ToLower().Contains(search) ||
                        (s.SizeName ?? "").ToLower().Contains(search));
                }

                if (comboCat.SelectedIndex > 0)
                {
                    string cat = comboCat.SelectedItem.ToString();
                    filtered = filtered.Where(s => s.CategoryName == cat);
                }

                lv.ItemsSource = filtered.ToList();
            };

            txtSearch.TextChanged += (s, ev) => refreshList();
            comboCat.SelectionChanged += (s, ev) => refreshList();

            refreshList();

            btnOk.Click += (s, ev) =>
            {
                // Используем безопасный метод извлечения ID, без dynamic
                object selected = lv.SelectedItem;
                if (selected == null)
                {
                    MessageBox.Show("Выберите товар.");
                    return;
                }

                var selectedStock = selected as StockSelection;
                if (selectedStock == null)
                {
                    MessageBox.Show("Не удалось определить выбранную позицию.");
                    return;
                }

                int stockId = selectedStock.IdStock;

                var dbStock = context.Stock
                    .Include("Product")
                    .Include("Size")
                    .First(x => x.IdStock == stockId);

                string sizeStr = dbStock.Size.Size1.ToString();
                string productName = dbStock.Product.NameProduct;
                decimal price = selectedStock.Price;

                var existing = _items.FirstOrDefault(i => i.StockId == stockId);

                if (existing != null)
                {
                    if (existing.Quantity + 1 > dbStock.OuantityInStock)
                    {
                        MessageBox.Show("Недостаточно товара на складе.");
                        return;
                    }
                    existing.Quantity++;
                }
                else
                {
                    _items.Add(new ItemView
                    {
                        StockId = stockId,
                        ProductName = productName,
                        SizeName = sizeStr,
                        Price = price,
                        OriginalPrice = dbStock.Product.PriceProduct,
                        Quantity = 1,
                        ImagePath = GetImagePath(dbStock.Product.Image),
                        ManufactureName = dbStock.Product.Manufacture != null ? dbStock.Product.Manufacture.Manufacture1 : "",
                        CategoryName = dbStock.Product.Category != null ? dbStock.Product.Category.Category1 : "",
                        Compound = dbStock.Product.Compound,
                        Description = dbStock.Product.Description,
                        SizeRange = GetSizeRange(dbStock.Product),
                        AvailableSizes = GetAvailableSizes(dbStock.Product)
                    });
                }

                RefreshItems();
                dlg.Close();
            };

            dlg.Content = grid;
            dlg.ShowDialog();
        }

        private decimal CalculatePrice(Mustafina_ShoesEntities context, Product product, DateTime calculationDate)
        {
            DateTime currentMonthStart = new DateTime(calculationDate.Year, calculationDate.Month, 1);
            DateTime previousMonthStart = currentMonthStart.AddMonths(-1);

            var previousOrderIds = context.Order
                .Where(o => o.DateOrder >= previousMonthStart && o.DateOrder < currentMonthStart)
                .Select(o => o.IdOrder)
                .ToList();

            var productStockIds = context.Stock
                .Where(s => s.IdProduct == product.IdProduct)
                .Select(s => s.IdStock)
                .ToList();

            bool hasPreviousMonthOrder = context.OrderItems
                .Any(i => previousOrderIds.Contains(i.IdOrder) && productStockIds.Contains(i.IdStock));

            return hasPreviousMonthOrder
                ? product.PriceProduct
                : Math.Round(product.PriceProduct * 0.75m, 2);
        }

        private string GetSizeRange(Product product)
        {
            if (product == null || product.Stock == null)
                return "—";

            var sizes = product.Stock
                .Where(s => s.Size != null)
                .Select(s => s.Size.Size1.ToString())
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            return sizes.Any() ? string.Join(", ", sizes) : "—";
        }

        private string GetAvailableSizes(Product product)
        {
            if (product == null || product.Stock == null)
                return "Нет доступных размеров";

            var sizes = product.Stock
                .Where(s => s.OuantityInStock > 0 && s.Size != null)
                .Select(s => s.Size.Size1.ToString())
                .Distinct()
                .OrderBy(s => s)
                .ToList();

            return sizes.Any() ? string.Join(", ", sizes) : "Нет доступных размеров";
        }

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

        private void BtnRemoveItem_Click(object sender, RoutedEventArgs e)
        {
            var selected = ListViewItems.SelectedItem as ItemView;
            if (selected == null)
            {
                MessageBox.Show("Выберите позицию для удаления.");
                return;
            }

            _items.Remove(selected);
            RefreshItems();
        }

        private void BtnPlusQty_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.DataContext as ItemView;
            if (item == null) return;

            var context = Mustafina_ShoesEntities.GetContext();
            var stock = context.Stock.FirstOrDefault(s => s.IdStock == item.StockId);

            // ИСПРАВЛЕНО: QuantityInStock (с буквой 'a')
            if (stock != null && item.Quantity + 1 > stock.OuantityInStock)
            {
                MessageBox.Show("Недостаточно товара на складе.");
                return;
            }

            item.Quantity++;
            RefreshItems();
        }

        private void BtnMinusQty_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.DataContext as ItemView;
            if (item == null) return;

            if (item.Quantity > 1)
            {
                item.Quantity--;
                RefreshItems();
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (ComboClient.SelectedItem == null)
            {
                MessageBox.Show("Выберите клиента.");
                return;
            }
            if (DpDate.SelectedDate == null)
            {
                MessageBox.Show("Укажите дату.");
                return;
            }
            if (_items.Count == 0)
            {
                MessageBox.Show("Добавьте хотя бы одну позицию.");
                return;
            }

            try
            {
                var context = Mustafina_ShoesEntities.GetContext();
                var client = ComboClient.SelectedItem as User;

                Order order;
                if (_orderId.HasValue)
                {
                    order = context.Order.First(o => o.IdOrder == _orderId.Value);
                    var oldItems = context.OrderItems.Where(i => i.IdOrder == order.IdOrder).ToList();
                    foreach (var it in oldItems) context.OrderItems.Remove(it);
                }
                else
                {
                    order = new Order();
                    int maxNum = context.Order.Any() ? context.Order.Max(o => o.OrderNumber) : 0;
                    order.OrderNumber = maxNum + 1;
                    context.Order.Add(order);
                }

                order.DateOrder = DpDate.SelectedDate.Value;
                order.IdUser = client.IdUser;
                order.OrderQuantity = _items.Sum(i => i.Quantity);

                context.SaveChanges();

                foreach (var item in _items)
                {
                    context.OrderItems.Add(new OrderItems
                    {
                        IdOrder = order.IdOrder,
                        IdStock = item.StockId,
                        OrderItemQuantity = item.Quantity,
                        UnitPrice = item.Price
                    });
                }

                context.SaveChanges();

                MessageBox.Show("Заказ сохранён.", "Успех",
                                MessageBoxButton.OK, MessageBoxImage.Information);

                // ВНИМАНИЕ: Передаем Manager.CurrentUser в ProductPage, иначе будет ошибка!
                if (Manager.CanSeeOrders)
                    Manager.MainFrame.Navigate(new OrdersPage());
                else
                    Manager.MainFrame.Navigate(new ProductPage(Manager.CurrentUser));
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message);
            }
        }
    }

    public class ItemView
    {
        public int StockId { get; set; }
        public string ProductName { get; set; }
        public string SizeName { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public int Quantity { get; set; }
        public string ImagePath { get; set; }
        public string ManufactureName { get; set; }
        public string CategoryName { get; set; }
        public string Compound { get; set; }
        public string Description { get; set; }
        public string SizeRange { get; set; }
        public string AvailableSizes { get; set; }
        public decimal Total => Price * Quantity;
        public bool CanEditQuantity { get; set; }

        public string PriceDetails => OriginalPrice > Price
            ? $"Цена со скидкой: {Price:N2} руб. (обычная: {OriginalPrice:N2} руб.)"
            : $"Цена: {Price:N2} руб.";

        public System.Windows.Visibility QtyButtonsVisibility =>
           CanEditQuantity
               ? System.Windows.Visibility.Visible
               : System.Windows.Visibility.Collapsed;
    }

    public class StockSelection
    {
        public int IdStock { get; set; }
        public string ProductName { get; set; }
        public string CategoryName { get; set; }
        public string SizeName { get; set; }
        public decimal Price { get; set; }
        public decimal OriginalPrice { get; set; }
        public int QuantityInStock { get; set; }
        public string Display { get; set; }
    }
}