Good iPhone12 = new Good("IPhone 12");
Good iPhone11 = new Good("IPhone 11");

Warehouse warehouse = new Warehouse();

Shop shop = new Shop(warehouse);

warehouse.Delive(iPhone12, 10);
warehouse.Delive(iPhone11, 1);

//Вывод всех товаров на складе с их остатком
warehouse.ShowGoods();

Cart cart = shop.Cart();
cart.Add(iPhone12, 4);
cart.Add(iPhone11, 3); //при такой ситуации возникает ошибка так, как нет нужного количества товара на складе

//Вывод всех товаров в корзине
cart.ShowGoods();

Console.WriteLine(cart.Order().Paylink);

cart.Add(iPhone12, 9); //Ошибка, после заказа со склада убираются заказанные товары

public class Good
{
    public string Name { get; }

    public Good(string name)
    {
        Name = name;
    }
}

public abstract class GoodsDisplayer
{
    public abstract void ShowGoods();

    protected void Display(Dictionary<Good, int> goods, string place)
    {
        Console.WriteLine($"Товары {place}:");

        foreach (KeyValuePair<Good, int> good in goods)
            Console.WriteLine($"{good.Key.Name} в количестве {good.Value} шт.");
    }
}

public class Warehouse : GoodsDisplayer
{
    private Dictionary<Good, int> _goods = new Dictionary<Good, int>();

    public IReadOnlyDictionary<Good, int> Goods => _goods;

    public override void ShowGoods()
    {
        Display(_goods, "на складе");
    }

    public void Delive(Good good, int amount)
    {
        if (good == null)
            throw new ArgumentNullException(nameof(good));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (_goods.TryGetValue(good, out int availableAmount))
            _goods[good] = availableAmount + amount;
        else
            _goods.Add(good, amount);
    }

    public void PickUpOrderedGoods(IReadOnlyDictionary<Good, int> orderedGoods)
    {
        if (orderedGoods == null)
            throw new ArgumentNullException(nameof(orderedGoods));

        foreach (KeyValuePair<Good, int> orderedGood in orderedGoods)
        {
            if (_goods.TryGetValue(orderedGood.Key, out int availableAmount))
            {
                _goods[orderedGood.Key] = availableAmount - orderedGood.Value;

                if (_goods[orderedGood.Key] <= 0)
                    _goods.Remove(orderedGood.Key);
            }
        }
    }
}

public class Shop
{
    private readonly Warehouse _warehouse;

    private List<Cart> _carts = new List<Cart>();

    public Shop(Warehouse warehouse)
    {
        if (warehouse == null)
            throw new ArgumentNullException(nameof(warehouse));

        _warehouse = warehouse;
    }

    public Cart Cart()
    {
        Cart? freeCart = _carts.FirstOrDefault(cart => cart.IsBusy == false);

        if (freeCart == null)
        {
            freeCart = new Cart();
            _carts.Add(freeCart);
        }

        freeCart.Prepare(_warehouse.Goods);
        freeCart.GoodsOrdered += PickUpGoodFromWarehouse;

        return freeCart;
    }

    private void PickUpGoodFromWarehouse(Cart cart, IReadOnlyDictionary<Good, int> goods)
    {
        if (cart == null)
            throw new ArgumentNullException(nameof(cart));

        if (goods == null)
            throw new ArgumentNullException(nameof(goods));

        cart.GoodsOrdered -= PickUpGoodFromWarehouse;

        _warehouse.PickUpOrderedGoods(goods);
    }
}

public class Cart : GoodsDisplayer
{
    private IReadOnlyDictionary<Good, int> _availableGoods = new Dictionary<Good, int>();
    private Dictionary<Good, int> _selectedGoods = new Dictionary<Good, int>();

    public event Action<Cart, IReadOnlyDictionary<Good, int>>? GoodsOrdered;

    public bool IsBusy { get; private set; }

    public void Prepare(IReadOnlyDictionary<Good, int> goods)
    {
        if (goods == null)
            throw new ArgumentNullException(nameof(goods));

        _availableGoods = goods;
        IsBusy = true;
    }

    public override void ShowGoods()
    {
        Display(_selectedGoods, "в корзине");
    }

    public void Add(Good good, int amount)
    {
        if (good == null)
            throw new ArgumentNullException(nameof(good));
        if (amount <= 0)
            throw new ArgumentOutOfRangeException(nameof(amount));

        if (_availableGoods.TryGetValue(good, out int availableAmount))
        {
            if (availableAmount < amount)
                throw new InvalidOperationException(nameof(amount));
        }
        else
        {
            throw new KeyNotFoundException(nameof(good));
        }

        _selectedGoods.Add(good, amount);
    }

    public Order Order()
    {
        if (_selectedGoods.Count == 0)
            throw new InvalidOperationException(nameof(Order));

        GoodsOrdered?.Invoke(this, _selectedGoods);
        _selectedGoods.Clear();

        IsBusy = false;

        return new Order();
    }
}

public class Order
{
    public string Paylink { get; }

    public Order()
    {
        Paylink = CreateLink();
    }

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