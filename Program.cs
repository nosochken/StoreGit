Product iPhone12 = new Product("IPhone 12");
Product iPhone11 = new Product("IPhone 11");

Warehouse warehouse = new Warehouse();

Shop shop = new Shop(warehouse);

warehouse.Delive(iPhone12, 10);
warehouse.Delive(iPhone11, 1);

//Вывод всех товаров на складе с их остатком
warehouse.ShowProducts();

Cart cart = shop.GetCart();
cart.Add(iPhone12, 4);
cart.Add(iPhone11, 3); //при такой ситуации возникает ошибка так, как нет нужного количества товара на складе

//Вывод всех товаров в корзине
cart.ShowProducts();

Console.WriteLine(cart.MakeOrder().Paylink);

cart.Add(iPhone12, 9); //Ошибка, после заказа со склада убираются заказанные товары

public class Product
{
    public Product(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException(nameof(name));

        Name = name;
    }

    public string Name { get; }
}

public abstract class ProductsDisplayer
{
    public ProductsDisplayer() { }

    public abstract void ShowProducts();

    protected void Display(Dictionary<Product, int> products, string place)
    {
        if (products == null)
            throw new ArgumentNullException(nameof(products));

        if (string.IsNullOrWhiteSpace(place))
            throw new ArgumentException(nameof(place));

        Console.WriteLine($"Товары {place}:");

        foreach (KeyValuePair<Product, int> product in products)
            Console.WriteLine($"{product.Key.Name} в количестве {product.Value} шт.");
    }
}

public interface IStorable
{
    public void MakeSureProductAvailable(Product product, int amount, IReadOnlyDictionary<Product, int> products);

    public void PickUpProducts(IReadOnlyDictionary<Product, int> orderedProducts);
}

public class Warehouse : ProductsDisplayer, IStorable
{
    private Dictionary<Product, int> _products = new Dictionary<Product, int>();

    public Warehouse() { }

    public override void ShowProducts()
    {
        Display(_products, "на складе");
    }

    public void Delive(Product product, int amount)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (_products.ContainsKey(product))
            _products[product] += amount;
        else
            _products.Add(product, amount);
    }

    public void PickUpProducts(IReadOnlyDictionary<Product, int> orderedProducts)
    {
        if (orderedProducts == null)
            throw new ArgumentNullException(nameof(orderedProducts));

        foreach (KeyValuePair<Product, int> orderedProduct in orderedProducts)
        {
            ValidateAvailability(orderedProduct.Key, orderedProduct.Value);

            _products[orderedProduct.Key] -= orderedProduct.Value;

            if (_products[orderedProduct.Key] == 0)
                _products.Remove(orderedProduct.Key);
        }
    }

    public void MakeSureProductAvailable(Product product, int amount, IReadOnlyDictionary<Product, int> products)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (products == null)
            throw new ArgumentNullException(nameof(products));

        int totalAmount = amount;

        if (products.ContainsKey(product))
            totalAmount += products[product];

        ValidateAvailability(product, totalAmount);
    }

    private void ValidateAvailability(Product product, int amount)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (_products.TryGetValue(product, out int availableAmount) == false)
            throw new InvalidOperationException(nameof(product));

        if (availableAmount < amount)
            throw new ArgumentOutOfRangeException(nameof(amount));
    }
}

public class Shop
{
    private readonly Warehouse _warehouse;

    public Shop(Warehouse warehouse)
    {
        if (warehouse == null)
            throw new ArgumentNullException(nameof(warehouse));

        _warehouse = warehouse;
    }

    public Cart GetCart()
    {
        return new Cart(_warehouse);
    }
}

public class Cart : ProductsDisplayer
{
    private readonly IStorable _warehouse;

    private Dictionary<Product, int> _selectedProducts = new Dictionary<Product, int>();

    public Cart(Warehouse warehouse)
    {
        if (warehouse == null)
            throw new ArgumentNullException(nameof(warehouse));

        _warehouse = warehouse;
    }

    private IReadOnlyDictionary<Product, int> _orderedProducts => _selectedProducts;

    public override void ShowProducts()
    {
        Display(_selectedProducts, "в корзине");
    }

    public void Add(Product product, int amount)
    {
        if (product == null)
            throw new ArgumentNullException(nameof(product));

        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        _warehouse.MakeSureProductAvailable(product, amount, _orderedProducts);
        _selectedProducts.Add(product, amount);
    }

    public Order MakeOrder()
    {
        if (_orderedProducts.Count == 0)
            throw new InvalidOperationException(nameof(MakeOrder));

        _warehouse.PickUpProducts(_orderedProducts);
        _selectedProducts.Clear();

        return new Order();
    }
}

public class Order
{
    public Order()
    {
        Paylink = CreateLink();
    }

    public string Paylink { get; }

    private string CreateLink()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";

        Random random = new Random();

        char[] result = Enumerable.Range(0, 10)
            .Select(symbol => chars[random.Next(chars.Length)])
            .ToArray();

        return new string(result);
    }
}