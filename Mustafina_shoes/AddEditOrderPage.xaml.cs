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
                    Price = i.UnitPrice, // ВАЖНО: Поле в базе называется UnitPrice
                    Quantity = i.OrderItemQuantity
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

        private void BtnAddItem_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new Window
            {
                Title = "Выбор товара",
                Width = 900,
                Height = 600,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = Window.GetWindow(this),
                Background = System.Windows.Media.Brushes.White
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

            var searchPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10),
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#D2F6E7"))
            };

            var lblSearch = new TextBlock { Text = "Поиск:", FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(5, 0, 5, 0) };
            var txtSearch = new TextBox { Width = 300, Height = 28, FontSize = 14, Margin = new Thickness(0, 0, 20, 0), VerticalContentAlignment = VerticalAlignment.Center };
            var lblCat = new TextBlock { Text = "Категория:", FontSize = 14, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(0, 0, 5, 0) };
            var comboCat = new ComboBox { Width = 200, Height = 28, FontSize = 14 };

            searchPanel.Children.Add(lblSearch);
            searchPanel.Children.Add(txtSearch);
            searchPanel.Children.Add(lblCat);
            searchPanel.Children.Add(comboCat);
            Grid.SetRow(searchPanel, 0);
            grid.Children.Add(searchPanel);

            var lv = new ListView { Margin = new Thickness(10, 0, 10, 0), FontSize = 14 };
            Grid.SetRow(lv, 1);
            grid.Children.Add(lv);

            var btnOk = new Button
            {
                Content = "Добавить выбранное",
                Height = 40,
                Width = 220,
                Margin = new Thickness(10),
                FontSize = 14,
                Background = new System.Windows.Media.SolidColorBrush(
                    (System.Windows.Media.Color)System.Windows.Media.ColorConverter.ConvertFromString("#70B2AF")),
                Foreground = System.Windows.Media.Brushes.White,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                HorizontalAlignment = HorizontalAlignment.Right
            };
            Grid.SetRow(btnOk, 2);
            grid.Children.Add(btnOk);

            var context = Mustafina_ShoesEntities.GetContext();
            var allStocks = context.Stock
                .Include("Product")
                .Include("Product.Category")
                .Include("Size")
                .Where(s => s.OuantityInStock > 0)
                .ToList()
                .Select(s => new
                {
                    s.IdStock,
                    ProductName = s.Product.NameProduct,
                    CategoryName = s.Product.Category != null ? s.Product.Category.Category1 : "",
                    SizeName = s.Size.Size1.ToString(),
                    Price = s.Product.PriceProduct,
                    QuantityInStock = s.OuantityInStock,
                    Display = $"{s.Product.NameProduct} | Размер {s.Size.Size1} | {s.Product.PriceProduct:N2} руб. | Остаток: {s.OuantityInStock}"
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
                lv.DisplayMemberPath = "Display";
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

                int stockId = (int)selected.GetType().GetProperty("IdStock").GetValue(selected);

                var dbStock = context.Stock
                    .Include("Product")
                    .Include("Size")
                    .First(x => x.IdStock == stockId);

                string sizeStr = dbStock.Size.Size1.ToString();
                string productName = dbStock.Product.NameProduct;
                decimal price = dbStock.Product.PriceProduct;

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
                        Quantity = 1
                    });
                }

                RefreshItems();
                dlg.Close();
            };

            dlg.Content = grid;
            dlg.ShowDialog();
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
        public int Quantity { get; set; }
        public decimal Total => Price * Quantity;
        public bool CanEditQuantity { get; set; }

        public System.Windows.Visibility QtyButtonsVisibility =>
           CanEditQuantity
               ? System.Windows.Visibility.Visible
               : System.Windows.Visibility.Collapsed;
    }
}