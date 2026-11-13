using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace Mustafina_shoes
{
    public partial class OrdersPage : Page
    {
        private List<OrderView> _allOrders = new List<OrderView>();
        private bool _isInitialized = false;

        public OrdersPage()
        {
            InitializeComponent();
            _isInitialized = true; // Защита от вызова поиска до загрузки данных
            LoadOrders();
        }

        private void LoadOrders()
        {
            try
            {
                var context = Mustafina_ShoesEntities.GetContext();

                // 1. Проверка прав
                if (!Manager.IsAdmin && !Manager.IsManager)
                {
                    MessageBox.Show("У вас нет доступа к списку заказов.");
                    Manager.MainFrame.Navigate(new ProductPage(Manager.CurrentUser));
                    return;
                }

                // 2. Загружаем ВСЕ заказы из базы
                var orders = context.Order.ToList();

                // 3. Загружаем ВСЕ позиции заказов отдельно
                var allOrderItems = context.OrderItems.ToList();

                // 4. Загружаем всех пользователей (чтобы получить имена)
                var allUsers = context.User.ToList();

                // 5. Формируем список для отображения
                _allOrders = orders.Select(o =>
                {
                    // Ищем пользователя для этого заказа
                    var user = allUsers.FirstOrDefault(u => u.IdUser == o.IdUser);

                    // Ищем позиции для этого заказа
                    var currentItems = allOrderItems.Where(i => i.IdOrder == o.IdOrder).ToList();

                    return new OrderView
                    {
                        OrderId = o.IdOrder,
                        OrderNumber = o.OrderNumber,
                        OrderDate = o.DateOrder,

                        // Формируем ФИО безопасно
                        ClientName = user != null
                            ? $"{user.LastName} {user.FirstName} {user.Patronymic}".Trim()
                            : "Неизвестно",

                        ItemsCount = currentItems.Count,

                        // Считаем сумму безопасно (поле UnitPrice)
                        TotalSum = currentItems.Any()
                            ? currentItems.Sum(i => i.OrderItemQuantity * i.UnitPrice)
                            : 0m,

                        CanDelete = Manager.CanManageOrders
                    };
                }).OrderByDescending(o => o.OrderDate).ToList();

                // 6. Настройка видимости кнопки "Добавить"
                BtnAddOrder.Visibility = Manager.CanManageOrders
                    ? Visibility.Visible
                    : Visibility.Collapsed;

                // 7. Применяем фильтр (поиск) и выводим на экран
                ApplyFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки заказов: " + ex.Message,
                                "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ApplyFilter()
        {
            if (!_isInitialized) return;

            var filtered = _allOrders.AsEnumerable();

            string search = TBox_OrderSearch.Text.Trim().ToLower();
            if (!string.IsNullOrEmpty(search))
            {
                filtered = filtered.Where(o =>
                    (o.ClientName ?? "").ToLower().Contains(search) ||
                    o.OrderNumber.ToString().Contains(search));
            }

            ListViewOrders.ItemsSource = filtered.ToList();
        }

        private void TBox_OrderSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnAddOrder_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new AddEditOrderPage((int?)null));
        }

        private void BtnOpenOrder_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var order = btn?.DataContext as OrderView;
            if (order == null) return;

            // Переходим на страницу редактирования/просмотра
            Manager.MainFrame.Navigate(new AddEditOrderPage(order.OrderId));
        }

        private void BtnDeleteOrder_Click(object sender, RoutedEventArgs e)
        {
            // Удалять заказы может администратор или менеджер
            if (!Manager.CanManageOrders)
            {
                MessageBox.Show("Недостаточно прав для удаления заказа.");
                return;
            }

            var btn = sender as Button;
            var order = btn?.DataContext as OrderView;
            if (order == null) return;

            var result = MessageBox.Show($"Удалить заказ №{order.OrderNumber}?",
                                         "Подтверждение",
                                         MessageBoxButton.YesNo,
                                         MessageBoxImage.Question);
            if (result != MessageBoxResult.Yes) return;

            try
            {
                var context = Mustafina_ShoesEntities.GetContext();

                // 1. Находим сам заказ
                var dbOrder = context.Order.FirstOrDefault(o => o.IdOrder == order.OrderId);

                if (dbOrder != null)
                {
                    // 2. Удаляем все связанные позиции (OrderItems)
                    var items = context.OrderItems.Where(i => i.IdOrder == dbOrder.IdOrder).ToList();
                    foreach (var item in items)
                        context.OrderItems.Remove(item);

                    // 3. Удаляем сам заказ
                    context.Order.Remove(dbOrder);
                    context.SaveChanges();
                }

                // Перезагружаем список
                LoadOrders();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка удаления: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }

    // Класс для отображения (DataContext для ListView)
    public class OrderView
    {
        public int OrderId { get; set; }
        public int OrderNumber { get; set; }
        public DateTime OrderDate { get; set; }
        public string ClientName { get; set; }
        public int ItemsCount { get; set; }
        public decimal TotalSum { get; set; }
        public bool CanDelete { get; set; }

        // Свойство для управления видимостью кнопки удаления в XAML
        public System.Windows.Visibility DeleteVisibility =>
            CanDelete ? System.Windows.Visibility.Visible : System.Windows.Visibility.Collapsed;
    }
}