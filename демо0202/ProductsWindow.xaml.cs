using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace демо0202
{
    public partial class ProductsWindow : Window
    {
        private CalculationService calculator = new CalculationService();
        private Products selectedProduct = null;

        public ProductsWindow()
        {
            InitializeComponent();
            LoadProducts();
            LoadMaterialTypes();
        }

        // Загрузка продукции из базы
        private void LoadProducts()
        {
            try
            {
                using (var db = new Demo0202Entities())
                {
                    // Загружаем продукцию с типами
                    var products = db.Products
                        .Include("ProductsType")
                        .ToList();

                    ProductsGrid.ItemsSource = products;

                    // Выбираем первую строку
                    if (products.Count > 0)
                    {
                        ProductsGrid.SelectedIndex = 0;
                        UpdateProductInfo();
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки продукции: {ex.Message}");
            }
        }

        // Загрузка типов материалов
        private void LoadMaterialTypes()
        {
            try
            {
                using (var db = new Demo0202Entities())
                {
                    var materialTypes = db.MaterialsType.ToList();
                    MaterialTypeCombo.ItemsSource = materialTypes;

                    if (materialTypes.Count > 0)
                    {
                        MaterialTypeCombo.SelectedIndex = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки типов материалов: {ex.Message}");
            }
        }

        // Обновление информации о выбранном продукте
        private void ProductsGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateProductInfo();
        }

        private void UpdateProductInfo()
        {
            selectedProduct = ProductsGrid.SelectedItem as Products;

            if (selectedProduct != null)
            {
                SelectedProductInfo.Text = $"{selectedProduct.Наименование_продукции}\n" +
                                          $"Артикул: {selectedProduct.Артикул}\n" +
                                          $"Цена: {selectedProduct.Минимальная_стоимость_для_партнера:N2} руб.";

                if (selectedProduct.ProductsType != null)
                {
                    CoefficientInfo.Text = $"Коэффициент типа: {selectedProduct.ProductsType.Коэффициент_типа_продукции:F2}\n" +
                                           $"Тип: {selectedProduct.ProductsType.Тип_продукции}";
                }
                else
                {
                    CoefficientInfo.Text = "Коэффициент типа: не определен";
                }
            }
            else
            {
                SelectedProductInfo.Text = "Не выбрано";
                CoefficientInfo.Text = "Коэффициент: не определен";
            }
        }

        // Валидация числового ввода
        private void NumberValidationTextBox(object sender, TextCompositionEventArgs e)
        {
            if (sender is TextBox textBox)
            {
                // Проверяем, является ли ввод числом или точкой/запятой для вещественных чисел
                bool isNumber = char.IsDigit(e.Text, 0);
                bool isDot = e.Text == "." || e.Text == ",";

                // Для параметров допускаем вещественные числа
                if (textBox.Name == "Param1Text" || textBox.Name == "Param2Text")
                {
                    if (!isNumber && !isDot)
                    {
                        e.Handled = true;
                        return;
                    }

                    // Проверяем, чтобы точка/запятая была только одна
                    if (isDot && (textBox.Text.Contains(".") || textBox.Text.Contains(",")))
                    {
                        e.Handled = true;
                        return;
                    }
                }
                else // Для количества - только целые числа
                {
                    if (!isNumber)
                    {
                        e.Handled = true;
                    }
                }
            }
        }

        // Кнопка расчета
        private void CalculateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Проверяем выбран ли продукт
                if (selectedProduct == null || selectedProduct.Тип_продукции == null)
                {
                    MessageBox.Show("Выберите продукт из таблицы");
                    return;
                }

                // Проверяем выбран ли тип материала
                if (MaterialTypeCombo.SelectedItem == null)
                {
                    MessageBox.Show("Выберите тип материала");
                    return;
                }

                // Получаем ID выбранного типа материала
                var selectedMaterial = (MaterialsType)MaterialTypeCombo.SelectedItem;
                int materialTypeId = selectedMaterial.Код;
                int productTypeId = selectedProduct.Тип_продукции.Value;

                // Парсим введенные значения
                if (!int.TryParse(RequiredQuantityText.Text, out int requiredQuantity) || requiredQuantity <= 0)
                {
                    MessageBox.Show("Введите корректное требуемое количество продукции");
                    RequiredQuantityText.Focus();
                    return;
                }

                if (!int.TryParse(StockQuantityText.Text, out int stockQuantity) || stockQuantity < 0)
                {
                    MessageBox.Show("Введите корректное количество продукции на складе");
                    StockQuantityText.Focus();
                    return;
                }

                // Заменяем запятую на точку для парсинга вещественных чисел
                string param1Text = Param1Text.Text.Replace(',', '.');
                string param2Text = Param2Text.Text.Replace(',', '.');

                if (!double.TryParse(param1Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double param1) || param1 <= 0)
                {
                    MessageBox.Show("Введите корректный параметр продукции 1 (положительное число)");
                    Param1Text.Focus();
                    return;
                }

                if (!double.TryParse(param2Text, System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture, out double param2) || param2 <= 0)
                {
                    MessageBox.Show("Введите корректный параметр продукции 2 (положительное число)");
                    Param2Text.Focus();
                    return;
                }

                int materialRequired = calculator.CalculateMaterialRequired(
             productTypeId: productTypeId,
             materialTypeId: materialTypeId,
             requiredQuantity: requiredQuantity,
             stockQuantity: stockQuantity,
             parameter1: param1,
             parameter2: param2
         );

                // Выводим результат
                if (materialRequired == -1)
                {
                    ResultText.Text = "Ошибка расчета! Проверьте введенные данные.";
                    ResultText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (materialRequired == -2)
                {
                    ResultText.Text = "Ошибка: результат расчета слишком большой! Уменьшите количество продукции.";
                    ResultText.Foreground = System.Windows.Media.Brushes.Red;
                }
                else if (materialRequired == 0)
                {
                    ResultText.Text = "Вся требуемая продукция уже есть на складе. Дополнительный материал не требуется.";
                    ResultText.Foreground = System.Windows.Media.Brushes.Blue;
                }
                else
                {
                    string productName = selectedProduct.Наименование_продукции;
                    string materialName = selectedMaterial.Тип_материала;
                    double defectRate = selectedMaterial.Процент_брака_материала_ ?? 0.0;
                    double defectPercentage = defectRate * 100;

                    ResultText.Text = $"Для производства {requiredQuantity} ед. продукции '{productName}' " +
                                     $"(уже на складе: {stockQuantity} ед.) потребуется:\n" +
                                     $"{materialRequired} ед. материала '{materialName}'\n" +
                                     $"(учтен брак материала: {defectPercentage:F3}%)";
                    ResultText.Foreground = System.Windows.Media.Brushes.Green;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при расчете: {ex.Message}");
            }
        }

        // Кнопка возврата
        private void BackButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow mainWindow = new MainWindow();
            mainWindow.Show();
            this.Close();
        }

        // Кнопка очистки
        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            RequiredQuantityText.Text = "1";
            StockQuantityText.Text = "0";
            Param1Text.Text = "1.0";
            Param2Text.Text = "1.0";
            ResultText.Text = "Здесь будет результат расчета";
            ResultText.Foreground = System.Windows.Media.Brushes.Black;

            if (MaterialTypeCombo.Items.Count > 0)
                MaterialTypeCombo.SelectedIndex = 0;
        }

        // Обработчик загрузки окна - добавляем валидацию
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            // Добавляем валидацию ввода для всех текстовых полей
            RequiredQuantityText.PreviewTextInput += NumberValidationTextBox;
            StockQuantityText.PreviewTextInput += NumberValidationTextBox;
            Param1Text.PreviewTextInput += NumberValidationTextBox;
            Param2Text.PreviewTextInput += NumberValidationTextBox;
        }
    }
}