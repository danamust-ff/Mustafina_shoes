using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
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
    /// Логика взаимодействия для ProductPage.xaml
    /// </summary>
    public partial class ProductPage : Page
    {
        public ProductPage(User user)
        {
            InitializeComponent();
            


            var currentProducts = Mustafina_ShoesEntities.GetContext().Products.ToList();
            ProductListView.ItemsSource = currentProducts;

            ComboSort.SelectedIndex = 0;
            ComboType.SelectedIndex = 0;
            UpdateShoes();
        }
        private void UpdateShoes()
        {
            var currentProducts = Mustafina_ShoesEntities.GetContext().Products.ToList();

            if (ComboType.SelectedIndex == 1)
            {
                currentProducts=currentProducts.Where(p=>p.IDCategory==2).ToList();
            }
            else if (ComboType.SelectedIndex == 2)
            {
                currentProducts=currentProducts.Where(p=>p.IDCategory==3).ToList();
            }
            else if (ComboType.SelectedIndex == 3)
            {
                currentProducts=currentProducts.Where(p=>p.IDCategory==1).ToList();
            }

            currentProducts=currentProducts.Where(p=>p.NameProduct.ToLower().Contains(TBSearch.Text.ToLower()) 
            || p.Compound.ToLower().Contains(TBSearch.Text.ToLower())).ToList();


           
            if (ComboSort.SelectedIndex==1)
            {
                currentProducts=currentProducts.OrderBy(p=>p.NameProduct).ToList();
            }
            else if (ComboSort.SelectedIndex==2)
            {
                currentProducts=currentProducts.OrderBy(p=>p.PriceProduct).ToList();
            }
            else if (ComboSort.SelectedIndex == 3)
            {
                currentProducts = currentProducts.OrderByDescending(p => p.PriceProduct).ToList();
            }
            ProductListView.ItemsSource = currentProducts.ToList();
        }

       

       

        private void ComboSort_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShoes();
        }

        private void ComboType_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateShoes();
        }

        private void TBSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateShoes();
        }
    }
}
