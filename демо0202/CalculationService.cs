using System;
using System.Linq;

namespace демо0202
{
    public class CalculationService
    {
        public int CalculateMaterialRequired(
    int productTypeId,
    int materialTypeId,
    int requiredQuantity,
    int stockQuantity,
    double parameter1,
    double parameter2)
        {
            try
            {
                using (var db = new Demo0202Entities())
                {
                    // Проверка существования типа продукции
                    var productType = db.ProductsType.FirstOrDefault(pt => pt.Код == productTypeId);
                    if (productType == null)
                        return -1;

                    // Проверка существования типа материала
                    var materialType = db.MaterialsType.FirstOrDefault(mt => mt.Код == materialTypeId);
                    if (materialType == null)
                        return -1;

                    // Проверка корректности входных параметров
                    if (requiredQuantity <= 0 || stockQuantity < 0 || parameter1 <= 0 || parameter2 <= 0)
                        return -1;

                    // Получение коэффициента типа продукции
                    double productCoefficient = productType.Коэффициент_типа_продукции ?? 1.0;

                    // Получение процента брака материала
                    double defectRate = materialType.Процент_брака_материала_ ?? 0.0;

                    // Расчет необходимого количества продукции с учетом наличия на складе
                    int productionNeeded = Math.Max(0, requiredQuantity - stockQuantity);
                    if (productionNeeded == 0)
                        return 0;

                    // Расчет материала на одну единицу продукции
                    double materialPerUnit = parameter1 * parameter2 * productCoefficient;

                    // Расчет общего материала без учета брака
                    double totalMaterial = materialPerUnit * productionNeeded;

                    // Учет процента брака материала
                    double defectFactor = 1.0 / (1.0 - defectRate);
                    double materialWithDefect = totalMaterial * defectFactor;

                    // Проверка на переполнение
                    if (materialWithDefect > int.MaxValue)
                    {
                        // Логируем для отладки
                        System.Diagnostics.Debug.WriteLine($"Переполнение: {materialWithDefect} > {int.MaxValue}");
                        return -2; // Специальный код для переполнения
                    }

                    // Округление до целого вверх
                    int requiredMaterial = (int)Math.Ceiling(materialWithDefect);

                    // Для отладки выводим значения
                    System.Diagnostics.Debug.WriteLine($"Расчет: productionNeeded={productionNeeded}, " +
                        $"materialPerUnit={materialPerUnit}, totalMaterial={totalMaterial}, " +
                        $"defectRate={defectRate}, result={requiredMaterial}");

                    return requiredMaterial;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Ошибка расчета: {ex.Message}");
                return -1;
            }
        }

        // Получить список продукции для конкретного партнера
        public System.Collections.Generic.List<Products> GetProductsForPartner(int partnerId)
        {
            try
            {
                using (var db = new Demo0202Entities())
                {
                    // Получаем тип партнера
                    var partner = db.Partners.FirstOrDefault(p => p.Код == partnerId);
                    if (partner == null)
                        return new System.Collections.Generic.List<Products>();

                    // Получаем все продукты с ценами для партнера
                    var products = db.Products.ToList();

                    // Здесь можно добавить логику фильтрации по типу партнера
                    // Например, разные цены для разных типов партнеров

                    return products;
                }
            }
            catch
            {
                return new System.Collections.Generic.List<Products>();
            }
        }
    }
}