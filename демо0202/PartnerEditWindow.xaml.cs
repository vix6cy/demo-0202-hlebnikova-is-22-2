using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace демо0202
{
    public partial class PartnerEditWindow : Window, INotifyPropertyChanged
    {
        private Demo0202Entities db = new Demo0202Entities();
        private Orders _currentOrder;
        private Partners _selectedPartner;
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
                if (TotalCostTextBlock != null)
                    TotalCostTextBlock.Text = $"{_totalCost:N2} руб.";
            }
        }

        public PartnerEditWindow(Orders order)
        {
            InitializeComponent();
            DataContext = this;
            LoadData();
            SetupDataGrid();
            LoadOrderData(order);
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
        }

        private void LoadOrderData(Orders order)
        {
            if (order != null)
            {
                _currentOrder = order;

                // Установка партнера
                var partner = db.Partners.FirstOrDefault(p => p.Код == order.Партнер);
                if (partner != null)
                {
                    _selectedPartner = partner;

                    // Заполнение полей партнера
                    // Тип партнера
                    if (partner.Тип_партнера.HasValue)
                    {
                        var partnerType = db.PartnesType.FirstOrDefault(pt => pt.Код == partner.Тип_партнера.Value);
                        if (partnerType != null)
                        {
                            PartnerTypeComboBox.SelectedItem = partnerType;
                        }
                    }

                    // Наименование
                    PartnerNameTextBox.Text = partner.Наименование_партнера ?? "";

                    // ФИО директора (раздельные поля)
                    DirectorLastNameTextBox.Text = partner.Фамилия_директора ?? "";
                    DirectorFirstNameTextBox.Text = partner.Имя_директора ?? "";
                    DirectorMiddleNameTextBox.Text = partner.Отчество_директора ?? "";

                    // Адрес
                    AddressTextBox.Text = partner.Юридический_адрес_партнера ?? "";

                    // Рейтинг
                    if (partner.Рейтинг.HasValue)
                    {
                        RatingSlider.Value = partner.Рейтинг.Value;
                        RatingValueTextBlock.Text = partner.Рейтинг.Value.ToString();
                    }

                    // Телефон
                    PhoneTextBox.Text = partner.НомерТелефона ?? "";

                    // Email
                    EmailTextBox.Text = partner.Email ?? "";

                    // ИНН
                    InnTextBox.Text = partner.ИНН ?? "";
                }

                // Установка даты
                if (order.Дата.HasValue)
                {
                    OrderDatePicker.SelectedDate = order.Дата.Value;
                }
                else
                {
                    OrderDatePicker.SelectedDate = DateTime.Now;
                }

                // Загрузка продуктов в заказе
                LoadOrderProducts(order.Код);
            }
        }

        private void LoadOrderProducts(int orderId)
        {
            try
            {
                OrderProducts.Clear();

                var productsInOrder = db.ProductsInOrders
                    .Where(p => p.Заказ == orderId)
                    .ToList();

                foreach (var productInOrder in productsInOrder)
                {
                    var product = db.Products.FirstOrDefault(p => p.Код == productInOrder.Продукция);
                    var price = product?.Минимальная_стоимость_для_партнера ?? 0;
                    var quantity = productInOrder.Количество ?? 0;

                    OrderProducts.Add(new ProductInOrderViewModel
                    {
                        Код = productInOrder.Код,
                        ProductId = (int)productInOrder.Продукция,
                        Product = product,
                        Количество = quantity,
                        Цена = (decimal)price,
                        Сумма = (decimal)(price * quantity)
                    });
                }

                CalculateTotalCost();
                ProductsDataGrid.Items.Refresh();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продукции: {ex.Message}");
            }
        }

        private void RatingSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RatingValueTextBlock.Text = ((int)RatingSlider.Value).ToString();
            if (_selectedPartner != null)
            {
                _selectedPartner.Рейтинг = (int)RatingSlider.Value;
            }
        }

        private void PartnerTypeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PartnerTypeComboBox.SelectedItem is PartnesType selectedType && _selectedPartner != null)
            {
                _selectedPartner.Тип_партнера = selectedType.Код;
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
                if (_currentOrder == null || _selectedPartner == null)
                {
                    MessageBox.Show("Ошибка: заказ не найден");
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
                    MessageBox.Show("В заказе должен быть хотя бы один товар");
                    return;
                }

                // Валидация данных партнера
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

                // Обновление данных партнера
                _selectedPartner.Наименование_партнера = PartnerNameTextBox.Text;
                _selectedPartner.Фамилия_директора = DirectorLastNameTextBox.Text;
                _selectedPartner.Имя_директора = DirectorFirstNameTextBox.Text;
                _selectedPartner.Отчество_директора = DirectorMiddleNameTextBox.Text;
                _selectedPartner.Юридический_адрес_партнера = AddressTextBox.Text;
                _selectedPartner.НомерТелефона = PhoneTextBox.Text;
                _selectedPartner.Email = EmailTextBox.Text;
                _selectedPartner.ИНН = InnTextBox.Text;

                // Обновление даты заказа
                _currentOrder.Дата = OrderDatePicker.SelectedDate.Value;

                // Сохранение изменений партнера и заказа
                db.SaveChanges();

                // Сохранение продуктов в заказе
                SaveOrderProducts(_currentOrder.Код);

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
                // Удаление старых записей
                var existingProducts = db.ProductsInOrders.Where(p => p.Заказ == orderId).ToList();
                db.ProductsInOrders.RemoveRange(existingProducts);

                // Добавление новых записей
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