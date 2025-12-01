using System.ComponentModel;

namespace демо0202
{
    // Модель представления для продуктов в заказе
    public class ProductInOrderViewModel : INotifyPropertyChanged
    {
        private int _код;
        private int _productId;
        private Products _product;
        private int _количество;
        private decimal _цена;
        private decimal _сумма;

        public int Код
        {
            get => _код;
            set { _код = value; OnPropertyChanged(nameof(Код)); }
        }

        public int ProductId
        {
            get => _productId;
            set
            {
                _productId = value;
                OnPropertyChanged(nameof(ProductId));
            }
        }

        public Products Product
        {
            get => _product;
            set
            {
                _product = value;
                OnPropertyChanged(nameof(Product));
                OnPropertyChanged(nameof(ProductName));

                // При изменении продукта обновляем цену
                if (value != null && value.Минимальная_стоимость_для_партнера.HasValue)
                {
                    Цена = (decimal)value.Минимальная_стоимость_для_партнера.Value;
                }
                else
                {
                    Цена = 0;
                }
            }
        }

        public string ProductName => Product?.Наименование_продукции ?? "Не выбрано";

        public int Количество
        {
            get => _количество;
            set
            {
                _количество = value;
                OnPropertyChanged(nameof(Количество));
                UpdateSum();
            }
        }

        public decimal Цена
        {
            get => _цена;
            set
            {
                _цена = value;
                OnPropertyChanged(nameof(Цена));
                UpdateSum();
            }
        }

        public decimal Сумма
        {
            get => _сумма;
            set
            {
                _сумма = value;
                OnPropertyChanged(nameof(Сумма));
            }
        }

        private void UpdateSum()
        {
            Сумма = Цена * Количество;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}