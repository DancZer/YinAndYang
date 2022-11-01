using UnityEngine;
public class GameResourceStorage : MonoBehaviour
{
    public GameResources Type;

    public int Amount = 0;
    public int MaxCapacity=100;

    public bool DestroyOnEmpty;

    [ReadOnly] public float Saturation;

    public GameObject[] StockPileObjects;
    private float _stockPileObjectAmount;

    // Start is called before the first frame update
    void Start()
    {
        _stockPileObjectAmount = MaxCapacity / StockPileObjects.Length;
    }

    // Update is called once per frame
    void Update()
    {
        UpdateSaturation();
    }

    public GameResource AddResource(GameResource resource)
    {
        if (resource.Type != Type) return resource;

        if (Amount + resource.Amount > MaxCapacity)
        {
            var remaining = (Amount + resource.Amount) - MaxCapacity;
            Amount = MaxCapacity;

            UpdateSaturation();

            return new GameResource() { Type = Type, Amount = remaining };
        }
        else
        {
            Amount += resource.Amount;

            UpdateSaturation();

            return null;
        }
    }

    public GameResource RemoveResource(GameResource resource)
    {
        if (resource.Type != Type) return resource;

        if (Amount - resource.Amount < 0)
        {
            var remaining = Amount;
            Amount = 0;

            UpdateSaturation();

            return new GameResource() { Type = Type, Amount = remaining };
        }
        else
        {
            Amount -= resource.Amount;

            UpdateSaturation();

            if(Amount == 0 && DestroyOnEmpty)
            {
                Destroy(gameObject);
            }

            return new GameResource() { Type = Type, Amount = resource.Amount }; ;
        }
    }

    private void UpdateSaturation()
    {
        Saturation = (float)Amount / MaxCapacity;

        var lastFullStockPileIdx = Mathf.FloorToInt(Mathf.Min(Mathf.Max(Amount / _stockPileObjectAmount, 0), StockPileObjects.Length-1));

        if(Amount <= 0)
        {
            var obj = StockPileObjects[0];
            obj.SetActive(false);
        }
        else
        {
            for (int i = 0; i < lastFullStockPileIdx; i++)
            {
                var obj = StockPileObjects[i];
                obj.transform.localScale = Vector3.one;
                obj.SetActive(true);
            }

            if (lastFullStockPileIdx < StockPileObjects.Length)
            {
                var scale = (Amount - _stockPileObjectAmount * lastFullStockPileIdx) / _stockPileObjectAmount;

                var obj = StockPileObjects[lastFullStockPileIdx];
                obj.transform.localScale = new Vector3(1, scale, 1);
                obj.SetActive(true);
            }
        }

        if (lastFullStockPileIdx + 1 < StockPileObjects.Length)
        {
            for (int i = lastFullStockPileIdx + 1; i < StockPileObjects.Length; i++)
            {
                var obj = StockPileObjects[i];
                obj.SetActive(false);
            }
        }
    }
}
