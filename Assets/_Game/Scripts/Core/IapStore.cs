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
        private bool connecting;

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
            // IAP 5 reports both outcomes here: a FailedOrder (acknowledge / validation failed) must not unlock anything
            store.OnPurchaseConfirmed += order =>
            {
                if (!Contains(order)) return;
                if (order is not ConfirmedOrder) { Finish(false); return; }
                onOwned();
                Finish(true);
            };
            store.OnPurchaseFailed += _ => Finish(false);
            // paid later (cash, parental approval): nothing is owned yet, it unlocks through OnPurchasesFetched once paid
            store.OnPurchaseDeferred += _ =>
                FakeAdProvider.ShowOverlay(Loc.T("Payment pending. It unlocks as soon as it goes through.", "Đang chờ thanh toán. Xong là mở ngay."), 1.8f, () => Finish(false));
            store.OnProductsFetchFailed += failed =>
            {
                Debug.LogWarning("IAP products fetch failed: " + failed.FailureReason);
                RetryLater();
            };
            store.OnPurchasesFetchFailed += failed => Debug.LogWarning("IAP purchases fetch failed: " + failed.message);
            Connect();
        }

        private async void RetryLater()
        {
            await System.Threading.Tasks.Task.Delay(30000);
            if (product == null) Connect();
        }

        // Offline or Play Store not signed in at launch: try again later, and whenever the player taps Buy.
        private async void Connect()
        {
            if (connecting) return;
            connecting = true;
            try
            {
                await store.Connect();
                store.FetchProducts(new List<ProductDefinition> { new ProductDefinition(productId, ProductType.NonConsumable) });
            }
            catch (Exception e)
            {
                Debug.LogWarning("IAP connect failed: " + e.Message);
                await System.Threading.Tasks.Task.Delay(30000);
                connecting = false;
                if (product == null) Connect();
                return;
            }
            connecting = false;
        }

        public void Buy(Action<bool> onDone)
        {
            if (pendingBuy != null) return; // a purchase is already on screen: a second tap must not replace its callback
            if (product == null || !product.availableToPurchase)
            {
                Connect();
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
