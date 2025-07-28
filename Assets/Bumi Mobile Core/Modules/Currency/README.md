# 💰 Currency Module – BumiMobileCore

The `Currency` module handles virtual currency systems like coins, gems, etc., with full support for saving, loading,
and event callbacks.  
It is flexible, efficient, and easy to integrate into any Unity game.

---

## ✅ Features

- 📦 Manage multiple currency types (coins, gems, etc.)
- 🔄 Add, subtract, set, or query currency amounts
- 💾 Persistent data using the SaveController system
- 🧩 Easy access via `CurrencyController.Get(...)`
- 🛎️ Event-driven architecture (listen to currency changes)
- 🎯 Lightweight & editor-friendly

---

## ⚙️ How to Use

### 1. Initialize at startup:

```csharp
CurrencyController.Init(currencyDatabase);
```

### 2. Get current amount:

```csharp
int coins = CurrencyController.Get(CurrencyType.Coin);
```

### 3. Check if user has enough

```csharp
if (CurrencyController.HasAmount(CurrencyType.Gem, 10))
{
    // Player has at least 10 gems
}
```

### 4. Add, Set, or Subtract:

```csharp
CurrencyController.Add(CurrencyType.Coin, 50);
CurrencyController.Set(CurrencyType.Gem, 100);
CurrencyController.Substract(CurrencyType.Coin, 25);
```

### 5. Listen to currency changes:

```csharp
CurrencyController.SubscribeGlobalCallback(OnCurrencyChanged);

void OnCurrencyChanged(Currency currency, int difference)
{
    Debug.Log($"Currency {currency.CurrencyType} changed by {difference}. New total: {currency.Amount}");
}
```

#### To stop listening:

```csharp
CurrencyController.UnsubscribeGlobalCallback(OnCurrencyChanged);
```

## 🗂️ Required Assets
- CurrencyDatabase – A ScriptableObject with a list of currencies
- Currency – Each currency has a type, amount, and default value
- CurrencyType – Enum used to identify each currency
- Currency.Save – Used by SaveController for persistence

## 📝 Notes
- Initialization must be called once at game start
- All currency changes automatically trigger save state updates
- During editor time (non-play mode), the system will try to pull test data from project init settings
---
