using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace демо0202
{
    public partial class PartnerAddWindow : Window, INotifyPropertyChanged
    {
        private Demo0202Entities db = new Demo0202Entities();
        private Partners _newPartner;
        private decimal _totalCost;

        private List<ProductInOrderViewModel> _orderProducts;
        public List<ProductInOrderViewModel> OrderProducts
        {
            get => _orderProducts;
            set
            {
                _orderProducts = value;
                OnPropertyChanged(nameof(OrderProducts));
            }
        }

        private List<Products> _allProducts;
        public List<Products> AllProducts
        {
            get => _allProducts;
            set
            {
                _allProducts = value;
                OnPropertyChanged(nameof(AllProducts));
            }
        }

        public decimal TotalCost
        {
            get => _totalCost;
            set
            {
                _totalCost = value;
                OnPropertyChanged(nameof(TotalCost));
                TotalCostTextBlock.Text = $"{_totalCost:N2} руб.";
            }
        }

        public PartnerAddWindow()
        {
            InitializeComponent();
            DataContext = this;
            LoadData();
            SetupDataGrid();
        }

        private void LoadData()
        {
            try
            {
                // Загрузка типов партнеров
                var partnerTypes = db.PartnesType.ToList();
                PartnerTypeComboBox.ItemsSource = partnerTypes;

                // Загрузка всех продуктов
                AllProducts = db.Products.ToList();

                OrderProducts = new List<ProductInOrderViewModel>();

                // Инициализация нового партнера
                _newPartner = new Partners
                {
                    Рейтинг = 10
                };

                // Подписка на изменения рейтинга
                RatingSlider.ValueChanged += RatingSlider_ValueChanged;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        private void SetupDataGrid()
        {
            // Установка источника данных для DataGrid
            ProductsDataGrid.ItemsSource = OrderProducts;

            // Добавление начальной строки
            AddNewProductRow();
        }

        private void PartnerTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PartnerTypeComboBox.SelectedItem is PartnesType selectedType)
            {
                if (_newPartner != null)
                {
                    _newPartner.Тип_партнера = selectedType.Код;
                }
            }
        }

        private void RatingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RatingValueTextBlock.Text = ((int)RatingSlider.Value).ToString();
            if (_newPartner != null)
            {
                _newPartner.Рейтинг = (int)RatingSlider.Value;
            }
        }

        private void AddProductRow_Click(object sender, RoutedEventArgs e)
        {
            AddNewProductRow();
        }

        private void AddNewProductRow()
        {
            OrderProducts.Add(new ProductInOrderViewModel
            {
                Код = 0,
                ProductId = 0,
                Product = null,
                Количество = 1,
                Цена = 0,
                Сумма = 0
            });

            ProductsDataGrid.Items.Refresh();
        }

        private void RemoveProductRow_Click(object sender, RoutedEventArgs e)
        {
            if (ProductsDataGrid.SelectedItem is ProductInOrderViewModel selectedProduct)
            {
                OrderProducts.Remove(selectedProduct);
                ProductsDataGrid.Items.Refresh();
                CalculateTotalCost();
            }
            else
            {
                MessageBox.Show("Выберите строку для удаления");
            }
        }

        private void ProductComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (sender is ComboBox comboBox && comboBox.DataContext is ProductInOrderViewModel item)
            {
                if (comboBox.SelectedItem is Products selectedProduct)
                {
                    item.ProductId = selectedProduct.Код;
                    item.Product = selectedProduct;
                    item.Цена = (decimal)(selectedProduct.Минимальная_стоимость_для_партнера ?? 0);
                    CalculateTotalCost();
                }
            }
        }

        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                if (!char.IsDigit(e.Text, 0))
                {
                    e.Handled = true;
                }
                else
                {
                    string newText = textBox.Text + e.Text;
                    if (!int.TryParse(newText, out int result) || result <= 0)
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        private void QuantityTextBox_LostFocus(object sender, RoutedEventArgs e)
        {
            if (sender is TextBox textBox && textBox.DataContext is ProductInOrderViewModel item)
            {
                if (int.TryParse(textBox.Text, out int quantity) && quantity > 0)
                {
                    item.Количество = quantity;
                    CalculateTotalCost();
                }
                else
                {
                    MessageBox.Show("Количество должно быть положительным числом");
                    item.Количество = 1;
                    textBox.Text = "1";
                }
            }
        }

        private void CalculateTotalCost()
        {
            TotalCost = OrderProducts.Sum(p => p.Сумма);
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Валидация данных партнера
                if (PartnerTypeComboBox.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип партнера");
                    return;
                }

                if (string.IsNullOrWhiteSpace(PartnerNameTextBox.Text))
                {
                    MessageBox.Show("Введите наименование партнера");
                    return;
                }

                if (string.IsNullOrWhiteSpace(DirectorLastNameTextBox.Text))
                {
                    MessageBox.Show("Введите фамилию директора");
                    return;
                }

                if (string.IsNullOrWhiteSpace(DirectorFirstNameTextBox.Text))
                {
                    MessageBox.Show("Введите имя директора");
                    return;
                }

                if (string.IsNullOrWhiteSpace(AddressTextBox.Text))
                {
                    MessageBox.Show("Введите юридический адрес");
                    return;
                }

                if (string.IsNullOrWhiteSpace(PhoneTextBox.Text))
                {
                    MessageBox.Show("Введите телефон");
                    return;
                }

                if (string.IsNullOrWhiteSpace(EmailTextBox.Text))
                {
                    MessageBox.Show("Введите email");
                    return;
                }

                if (OrderDatePicker.SelectedDate == null)
                {
                    MessageBox.Show("Укажите дату заявки");
                    return;
                }

                // Проверка наличия товаров в заказе
                if (OrderProducts.Count == 0 || OrderProducts.All(p => p.ProductId == 0))
                {
                    MessageBox.Show("Добавьте хотя бы один товар в заказ");
                    return;
                }

                // Сохранение партнера
                _newPartner.Наименование_партнера = PartnerNameTextBox.Text;
                _newPartner.Фамилия_директора = DirectorLastNameTextBox.Text;
                _newPartner.Имя_директора = DirectorFirstNameTextBox.Text;
                _newPartner.Отчество_директора = DirectorMiddleNameTextBox.Text;
                _newPartner.Юридический_адрес_партнера = AddressTextBox.Text;
                _newPartner.НомерТелефона = PhoneTextBox.Text;
                _newPartner.Email = EmailTextBox.Text;
                _newPartner.ИНН = InnTextBox.Text;

                db.Partners.Add(_newPartner);
                db.SaveChanges();

                // Создание нового заказа
                var order = new Orders
                {
                    Партнер = _newPartner.Код,
                    Дата = OrderDatePicker.SelectedDate.Value,
                };
                db.Orders.Add(order);
                db.SaveChanges();

                // Сохранение продуктов в заказе
                SaveOrderProducts(order.Код);

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения: {ex.Message}");
            }
        }

        private void SaveOrderProducts(int orderId)
        {
            try
            {
                foreach (var productViewModel in OrderProducts)
                {
                    if (productViewModel.ProductId > 0 && productViewModel.Количество > 0)
                    {
                        var productInOrder = new ProductsInOrders
                        {
                            Заказ = orderId,
                            Продукция = productViewModel.ProductId,
                            Количество = productViewModel.Количество
                        };
                        db.ProductsInOrders.Add(productInOrder);
                    }
                }

                db.SaveChanges();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения продукции: {ex.Message}");
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}