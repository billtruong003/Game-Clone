using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace CasualGame.Core
{
    /// <summary>
    /// Google Play Billing through Unity IAP 5: one non-consumable ("remove ads"). Owned purchases are read back on
    /// every start, so reinstalling or a new phone restores the purchase without a button press.
    /// </summary>
    public class IapStore
    {
        private readonly string productId;
        private readonly Action onOwned;
        private readonly StoreController store;
        private Product product;
        private Action<bool> pendingBuy;

        public IapStore(string productId, Action onOwned)
        {
            this.productId = productId;
            this.onOwned = onOwned;
            store = UnityIAPServices.StoreController();
            store.OnProductsFetched += products =>
            {
                product = products.FirstOrDefault(p => p.definition.id == productId);
                store.FetchPurchases();
            };
            store.OnPurchasesFetched += orders =>
            {
                if (orders.ConfirmedOrders.Any(Contains)) onOwned();
                foreach (var pending in orders.PendingOrders.Where(Contains)) store.ConfirmPurchase(pending);
            };
            store.OnPurchasePending += order => store.ConfirmPurchase(order);
            store.OnPurchaseConfirmed += order =>
            {
                if (!Contains(order)) return;
                onOwned();
                Finish(true);
            };
            store.OnPurchaseFailed += _ => Finish(false);
            Connect();
        }

        private async void Connect()
        {
            try
            {
                await store.Connect();
                store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(productId, ProductType.NonConsumable) });
            }
            catch (Exception e)
            {
                Debug.LogWarning("IAP connect failed: " + e.Message);
            }
        }

        public void Buy(Action<bool> onDone)
        {
            if (product == null || !product.availableToPurchase)
            {
                FakeAdProvider.ShowOverlay(Loc.T("The store is not available right now.", "Cửa hàng chưa sẵn sàng."), 1.4f, () => onDone?.Invoke(false));
                return;
            }
            pendingBuy = onDone;
            store.PurchaseProduct(product);
        }

        public void Restore(Action<bool> onDone) =>
            store.RestoreTransactions((ok, _) => onDone?.Invoke(ok && Ads.RemoveAdsOwned));

        private bool Contains(Order order) =>
            order.CartOrdered.Items().Any(i => i.Product.definition.id == productId);

        private void Finish(bool ok)
        {
            var done = pendingBuy;
            pendingBuy = null;
            done?.Invoke(ok);
        }
    }
}
