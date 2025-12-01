using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace демо0202
{
    public partial class MainWindow : Window
    {
        Demo0202Entities db = new Demo0202Entities();

        public MainWindow()
        {
            InitializeComponent();
            // Загрузка данных при запуске приложения
            LoadOrdersData();
        }

        // Загрузка данных о заявках партнеров
        private void LoadOrdersData()
        {
            try
            {
                var ordersList = new List<OrderViewModel>();

                // Получение заказов
                var orders = db.Orders
                    .OrderByDescending(o => o.Дата)
                    .ToList();

                // Обработка каждого заказа
                foreach (var order in orders)
                {
                    // Поиск партнера по коду
                    var partner = db.Partners.FirstOrDefault(p => p.Код == order.Партнер);
                    if (partner != null)
                    {
                        // Получение типа партнера
                        var partnerType = db.PartnesType.FirstOrDefault(pt => pt.Код == partner.Тип_партнера);

                        // Расчет стоимости заказа
                        decimal calculatedCost = CalculateOrderCost(order.Код);

                        // Создание модели представления
                        var viewModel = new OrderViewModel
                        {
                            OrderId = order.Код,
                            PartnerId = partner.Код,
                            PartnerTypeName = partnerType?.Тип_партнера ?? "Тип не указан",
                            PartnerName = partner.Наименование_партнера ?? "Название не указано",
                            LegalAddress = partner.Юридический_адрес_партнера ?? "Адрес не указан",
                            PhoneNumber = partner.НомерТелефона ?? "+7 223 322 22 32",
                            Rating = partner.Рейтинг ?? 0,
                            OrderDate = order.Дата?.ToString("dd.MM.yyyy") ?? "Дата не указана",
                            Cost = (double)calculatedCost
                        };

                        ordersList.Add(viewModel);
                    }
                }

                // Установка источника данных для списка
                OrdersItemsControl.ItemsSource = ordersList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}");
            }
        }

        // Расчет стоимости заказа
        private decimal CalculateOrderCost(int orderId)
        {
            decimal totalCost = 0;

            // Получение всех продуктов в заказе
            var orderProducts = db.ProductsInOrders
                .Where(pio => pio.Заказ == orderId)
                .ToList();

            // Расчет стоимости по каждому продукту
            foreach (var orderProduct in orderProducts)
            {
                var product = db.Products
                    .FirstOrDefault(p => p.Код == orderProduct.Продукция);

                // Учет стоимости продукта, если цена указана
                if (product != null && product.Минимальная_стоимость_для_партнера.HasValue)
                {
                    decimal productPrice = (decimal)product.Минимальная_стоимость_для_партнера.Value;
                    int quantity = orderProduct.Количество ?? 0;

                    decimal productTotal = productPrice * quantity;
                    totalCost += productTotal;
                }
            }

            // Округление до сотых и проверка на отрицательное значение
            totalCost = Math.Max(0, Math.Round(totalCost, 2));

            return totalCost;
        }

        // Обработчик клика по элементу списка для редактирования
        private void PartnerItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // Проверка типа отправителя и контекста данных
            if (sender is Border border && border.DataContext is OrderViewModel orderViewModel)
            {
                // Поиск заказа по ID
                var order = db.Orders.FirstOrDefault(o => o.Код == orderViewModel.OrderId);
                if (order != null)
                {
                    // Открытие окна редактирования с передачей заказа
                    PartnerEditWindow partnerEditWindow = new PartnerEditWindow(order);

                    // Используем ShowDialog() чтобы дождаться закрытия окна
                    bool? result = partnerEditWindow.ShowDialog();

                    // Если данные были сохранены (пользователь нажал "Сохранить")
                    if (result == true)
                    {
                        // Обновляем данные в главном окне
                        LoadOrdersData();
                    }
                    // Не закрываем главное окно, просто показываем окно редактирования
                }
            }
        }

        // Добавление новой заявки
        private void AddPartnerButton_Click(object sender, RoutedEventArgs e)
        {
            // Открытие окна добавления заявки
            PartnerAddWindow partnerAddWindow = new PartnerAddWindow();

            // Используем ShowDialog() чтобы дождаться закрытия окна
            bool? result = partnerAddWindow.ShowDialog();

            // Если данные были сохранены (пользователь нажал "Сохранить")
            if (result == true)
            {
                // Обновляем данные в главном окне
                LoadOrdersData();
            }
            // Не закрываем главное окно, просто показываем окно добавления
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            ProductsWindow productsWindow = new ProductsWindow();
            productsWindow.Show();
            this.Close();
        }
    }

    // Модель представления для отображения заказа
    public class OrderViewModel : INotifyPropertyChanged
    {
        private double? _cost;

        public int OrderId { get; set; }
        public int PartnerId { get; set; }
        public string PartnerTypeName { get; set; }
        public string PartnerName { get; set; }
        public string LegalAddress { get; set; }
        public string PhoneNumber { get; set; }
        public int Rating { get; set; }
        public string OrderDate { get; set; }

        // Стоимость заказа
        public double? Cost
        {
            get => _cost;
            set
            {
                _cost = value;
                OnPropertyChanged(nameof(Cost));
                OnPropertyChanged(nameof(CostText));
            }
        }

        // Текст рейтинга для отображения
        public string RatingText => $"Рейтинг: {Rating}";

        // Текст стоимости для отображения
        public string CostText
        {
            get
            {
                if (Cost.HasValue)
                {
                    return $"{Cost.Value:N2} руб.";
                }
                return "Стоимость не рассчитана";
            }
        }

        // Событие изменения свойства
        public event PropertyChangedEventHandler PropertyChanged;

        // Метод вызова события изменения свойства
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}