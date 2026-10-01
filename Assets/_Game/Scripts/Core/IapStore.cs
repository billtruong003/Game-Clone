using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Purchasing;

namespace CasualGame.Core
{
    /// <summary>
    /// Google Play Billing through Unity IAP 5: the game's non-consumables (skins, bundles, Remove ads; see
    /// Store/IAP_PRODUCTS.md). Owned purchases are read back on every start, so reinstalling or a new phone restores
    /// them without a button press, and a refunded product is taken back.
    /// </summary>
    public class IapStore
    {
        private readonly List<string> productIds;
        private readonly Action<string, bool> setOwned;
        private readonly StoreController store;
        private readonly Dictionary<string, Product> products = new();
        private Action<bool> pendingBuy;
        private string pendingId;
        private bool connecting;

        /// <summary>True once Google Play answered with the products (prices are known).</summary>
        public bool Ready => products.Count > 0;

        public IapStore(IEnumerable<string> ids, Action<string, bool> setOwned)
        {
            productIds = ids.Distinct().ToList();
            this.setOwned = setOwned;
            store = UnityIAPServices.StoreController();
            store.OnProductsFetched += fetched =>
            {
                foreach (var p in fetched) products[p.definition.id] = p;
                store.FetchPurchases();
            };
            store.OnPurchasesFetched += orders =>
            {
                // the full list of what Play says is owned: anything else (refunded, revoked) is not
                var owned = new HashSet<string>();
                foreach (var o in orders.ConfirmedOrders) foreach (var id in Ids(o)) owned.Add(id);
                foreach (var id in productIds) setOwned(id, owned.Contains(id));
                foreach (var pending in orders.PendingOrders) store.ConfirmPurchase(pending);
            };
            store.OnPurchasePending += order => store.ConfirmPurchase(order);
            // IAP 5 reports both outcomes here: a FailedOrder (acknowledge / validation failed) must not unlock anything
            store.OnPurchaseConfirmed += order =>
            {
                var ok = order is ConfirmedOrder;
                if (ok) foreach (var id in Ids(order)) setOwned(id, true);
                if (pendingId != null && Ids(order).Contains(pendingId)) Finish(ok);
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
            if (!Ready) Connect();
        }

        // Offline or Play Store not signed in at launch: try again later, and whenever the player taps Buy.
        private async void Connect()
        {
            if (connecting) return;
            connecting = true;
            try
            {
                await store.Connect();
                store.FetchProducts(productIds.Select(id => new ProductDefinition(id, ProductType.NonConsumable)).ToList());
            }
            catch (Exception e)
            {
                Debug.LogWarning("IAP connect failed: " + e.Message);
                await System.Threading.Tasks.Task.Delay(30000);
                connecting = false;
                if (!Ready) Connect();
                return;
            }
            connecting = false;
        }

        /// <summary>Google Play's localized price ("29.000 ₫"), or null while unknown.</summary>
        public string Price(string id) =>
            products.TryGetValue(id, out var p) && p.availableToPurchase ? p.metadata.localizedPriceString : null;

        public void Buy(string id, Action<bool> onDone)
        {
            if (pendingBuy != null) return; // a purchase is already on screen: a second tap must not replace its callback
            if (!products.TryGetValue(id, out var product) || !product.availableToPurchase)
            {
                Connect();
                FakeAdProvider.ShowOverlay(Loc.T("The store is not available right now.", "Cửa hàng chưa sẵn sàng."), 1.4f, () => onDone?.Invoke(false));
                return;
            }
            pendingBuy = onDone;
            pendingId = id;
            store.PurchaseProduct(product);
        }

        /// <summary>Asks Play again for everything owned; the answer arrives through OnPurchasesFetched.</summary>
        public void Restore(Action<bool> onDone) =>
            store.RestoreTransactions((ok, _) =>
            {
                if (ok) store.FetchPurchases();
                onDone?.Invoke(ok);
            });

        private static IEnumerable<string> Ids(Order order) => order.CartOrdered.Items().Select(i => i.Product.definition.id);

        private void Finish(bool ok)
        {
            var done = pendingBuy;
            pendingBuy = null;
            pendingId = null;
            done?.Invoke(ok);
        }
    }
}
