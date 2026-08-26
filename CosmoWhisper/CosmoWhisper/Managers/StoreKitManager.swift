import Foundation
import StoreKit

@MainActor
class StoreKitManager: ObservableObject {
    static let shared = StoreKitManager()
    
    // Product Identifiers for App Store Connect
    let productIDs: Set<String> = [
        "com.cosmowhisper.personal.monthly",
        "com.cosmowhisper.pro.monthly",
        "com.cosmowhisper.medical.monthly"
    ]
    
    @Published var products: [Product] = []
    @Published var purchasedProductIDs = Set<String>()
    @Published var isLoading = false
    @Published var statusMessage: String?
    
    private var transactionListener: Task<Void, Error>?
    
    private init() {
        // Listen for background transaction updates from Apple
        transactionListener = listenForTransactions()
        
        Task {
            await fetchProducts()
            await updatePurchasedProducts()
        }
    }
    
    deinit {
        transactionListener?.cancel()
    }
    
    /// Fetches subscription products from StoreKit
    func fetchProducts() async {
        do {
            let storeProducts = try await Product.products(for: productIDs)
            self.products = storeProducts.sorted(by: { $0.price < $1.price })
            LogManager.shared.log("StoreKitManager: Fetched \(products.count) products from App Store.")
        } catch {
            LogManager.shared.log("StoreKitManager: Failed to fetch products: \(error.localizedDescription)")
        }
    }
    
    /// Purchases a StoreKit subscription
    func purchase(_ product: Product) async -> Bool {
        self.isLoading = true
        self.statusMessage = nil
        
        do {
            let result = try await product.purchase()
            self.isLoading = false
            
            switch result {
            case .success(let verification):
                switch verification {
                case .verified(let transaction):
                    await transaction.finish()
                    await updatePurchasedProducts()
                    self.statusMessage = "Thank you! Subscription activated."
                    LogManager.shared.log("StoreKitManager: Successfully purchased \(product.id)")
                    return true
                case .unverified(_, let error):
                    self.statusMessage = "Purchase unverified: \(error.localizedDescription)"
                    return false
                }
            case .userCancelled:
                self.statusMessage = nil
                return false
            case .pending:
                self.statusMessage = "Purchase pending approval..."
                return false
            @unknown default:
                return false
            }
        } catch {
            self.isLoading = false
            self.statusMessage = "Purchase failed: \(error.localizedDescription)"
            LogManager.shared.log("StoreKitManager: Purchase error: \(error.localizedDescription)")
            return false
        }
    }
    
    /// Restores prior Apple In-App Purchases
    func restorePurchases() async {
        self.isLoading = true
        self.statusMessage = nil
        
        do {
            try await AppStore.sync()
            await updatePurchasedProducts()
            self.isLoading = false
            if !purchasedProductIDs.isEmpty {
                self.statusMessage = "Purchases restored successfully!"
            } else {
                self.statusMessage = "No active Apple subscriptions found."
            }
        } catch {
            self.isLoading = false
            self.statusMessage = "Restore failed: \(error.localizedDescription)"
        }
    }
    
    /// Checks current Apple entitlements and unlocks the appropriate tier
    func updatePurchasedProducts() async {
        var activeIDs = Set<String>()
        
        for await result in Transaction.currentEntitlements {
            if case .verified(let transaction) = result {
                if transaction.revocationDate == nil {
                    activeIDs.insert(transaction.productID)
                }
            }
        }
        
        self.purchasedProductIDs = activeIDs
        
        // If the user has an active Apple subscription, apply tier to LicenseManager
        if let highestTier = highestActiveTier(from: activeIDs) {
            LicenseManager.shared.tier = highestTier
            LicenseManager.shared.monthlyLimitMinutes = 999999.0
            UserDefaults.standard.set(highestTier, forKey: "subscriptionTier")
            LicenseManager.shared.recalculateLocalMonthlyUsage()
            LogManager.shared.log("StoreKitManager: Unlocked active Apple subscription tier: \(highestTier)")
        }
    }
    
    private func highestActiveTier(from productIDs: Set<String>) -> String? {
        if productIDs.contains("com.cosmowhisper.medical.monthly") { return "medical" }
        if productIDs.contains("com.cosmowhisper.pro.monthly") { return "pro" }
        if productIDs.contains("com.cosmowhisper.personal.monthly") { return "personal" }
        return nil
    }
    
    private func listenForTransactions() -> Task<Void, Error> {
        return Task.detached {
            for await result in Transaction.updates {
                if case .verified(let transaction) = result {
                    await transaction.finish()
                    await self.updatePurchasedProducts()
                }
            }
        }
    }
}
