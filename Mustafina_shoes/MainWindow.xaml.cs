using System;
using System.Windows;
using System.Windows.Controls;

namespace Mustafina_shoes
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // ВАЖНО: Сначала сохраняем фрейм в менеджер, потом навигируем
            Manager.MainFrame = MainFrame;
            MainFrame.Navigate(new AuthPage());

            // Подписываемся на событие перехода, чтобы обновлять шапку
            MainFrame.Navigated += (s, e) => UpdateHeader();
        }

        // Этот метод обновляет видимость кнопок в зависимости от роли
        public void UpdateHeader()
        {
            if (Manager.CurrentUser != null)
            {
                BtnCatalog.Visibility = Visibility.Visible;

                // Показываем кнопку "Заказы" только админу и менеджеру
                if (Manager.CanSeeOrders)
                    BtnOrders.Visibility = Visibility.Visible;
                else
                    BtnOrders.Visibility = Visibility.Collapsed;
            }
            else
            {
                BtnCatalog.Visibility = Visibility.Collapsed;
                BtnOrders.Visibility = Visibility.Collapsed;
            }
        }

        private void MainFrame_ContentRendered(object sender, EventArgs e)
        {
            // Логика для кнопки "Назад"
            if (MainFrame.CanGoBack)
                TBBback.Visibility = Visibility.Visible;
            else
                TBBback.Visibility = Visibility.Hidden;
        }

        private void TBBback_Click(object sender, RoutedEventArgs e)
        {
            if (Manager.MainFrame.CanGoBack)
                Manager.MainFrame.GoBack();
        }

        // Обработчик кнопки "Заказы"
        private void BtnOrders_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new OrdersPage());
        }

        private void BtnCatalog_Click(object sender, RoutedEventArgs e)
        {
            Manager.MainFrame.Navigate(new ProductPage(Manager.CurrentUser));
        }

        // Обработчик кнопки "Выйти"
        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            Manager.CurrentUser = null; // Сбрасываем пользователя
            MainFrame.Navigate(new AuthPage()); // Кидаем на страницу авторизации

            // Очищаем историю переходов
            while (MainFrame.CanGoBack)
                MainFrame.RemoveBackEntry();
        }
    }
}