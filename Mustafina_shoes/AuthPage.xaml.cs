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

namespace Mustafina_shoes
{
    /// <summary>
    /// Логика взаимодействия для AuthPage.xaml
    /// </summary>
    public partial class AuthPage : Page
    {
        private int failedAttempts = 0;
        public AuthPage()
        {
            InitializeComponent();
        }

        private void TBoxlog_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void TBoxpass_TextChanged(object sender, TextChangedEventArgs e)
        {

        }

        private void Guest_Click(object sender, RoutedEventArgs e)
        {
            User GuestUser = new User
            {
                IDUser = 0,
                Login = "guest",
                FirstName = "Гость",
                LastName = "",
                Patronymic = "",
                IDRole = 0
            };
            Manager.MainFrame.Navigate(new ProductPage(GuestUser));

        }

        private void LogIn_Click(object sender, RoutedEventArgs e)
        {
            string login = TBoxlog.Text;
            if (string.IsNullOrEmpty(login))
            {
                MessageBox.Show("Введите логин!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            User user = Mustafina_ShoesEntities.GetContext().User.FirstOrDefault(p=>p.Login==TBoxlog.Text);
            if (user != null)
            {
                TBoxlog.Text = "";
                Manager.MainFrame.Navigate(new ProductPage(user));
            }
            else
            {
                failedAttempts++;
                if (failedAttempts > 0)
                    MessageBox.Show("Введены неверные данные!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                TBoxlog.Text = "";
            }
        }
    }
}
