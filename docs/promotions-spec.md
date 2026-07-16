# Spec — Service Promotions (codes promo Black Friday)

## Modèle — `Promotion`

| Champ | Type | Contrainte |
|---|---|---|
| `Code` | string | unique, normalisé en MAJUSCULES (insensible à la casse) |
| `Percent` | int | 1–90 |
| `ValidFrom` | DateTimeOffset (UTC) | — |
| `ValidUntil` | DateTimeOffset (UTC) | doit être > `ValidFrom` |
| `MaxUses` | int | ≥ 1 |
| `UsesCount` | int | compteur d'utilisations, ≥ 0, incrémenté atomiquement (voir Épic 2) |
| `MinAmount` | decimal | ≥ 0, montant mini du panier, **borne inclusive** (`amount >= MinAmount`) |

## Règles de refus

Évaluées dans cet ordre à `POST /validate` :

| Reason code | Condition |
|---|---|
| `unknown_code` | code inexistant |
| `not_started` | `now (UTC) < ValidFrom` |
| `expired` | `now (UTC) > ValidUntil` |
| `exhausted` | `UsesCount >= MaxUses` |
| `amount_too_low` | `amount < MinAmount` |

Rejetées dès la création (`POST /api/promotions`, `400`) :

- `Percent` hors bornes 1–90
- `ValidUntil <= ValidFrom`
- `MaxUses < 1`
- `MinAmount < 0`

**Horloge :** `now` vient d'un `IClock` injecté dans `PromotionRules` — jamais `DateTime.Now` en
dur, pour que les règles restent testables indépendamment de la date du jour.

**Arrondi :** `Math.Round(amount * (1 - percent / 100m), 2, MidpointRounding.AwayFromZero)`.

## Contrat d'API

### `POST /api/promotions`

Requête : `CreatePromotionRequest(string Code, int Percent, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil, int MaxUses, decimal MinAmount)`

- `201 Created` + `PromotionDto`
- `400 BadRequest` (message string) si une contrainte de création est violée
- `409 Conflict` si le code existe déjà

### `GET /api/promotions/{code}`

- `200 OK` + `PromotionDto` (comparaison insensible à la casse)
- `404 NotFound` si inconnu

### `POST /api/promotions/{code}/validate`

Requête : `ValidateRequest(decimal Amount)`

Réponse `200 OK` toujours (un refus métier n'est pas une erreur HTTP) :

```json
{ "valid": true, "discountedAmount": 64.00 }
```
```json
{ "valid": false, "reason": "amount_too_low" }
```

- `400 BadRequest` seulement si `Amount <= 0` (requête malformée, pas une règle métier)

`PromotionDto(string Code, int Percent, DateTimeOffset ValidFrom, DateTimeOffset ValidUntil, int MaxUses, int UsesCount, decimal MinAmount)`

## Exemples chiffrés

| Panier | Code | Règle déclenchée | Résultat |
|---|---|---|---|
| 80,00 € | `BLACKFRIDAY` (-20 %, minAmount 50 €) | valide | `valid: true`, 64,00 € |
| 45,00 € | `BLACKFRIDAY` (-20 %, minAmount 50 €) | `amount_too_low` | `valid: false` |
| 50,00 € (pile) | minAmount 50 € | inclusif → valide | remise appliquée |
| 79,99 € | code -15 % | arrondi | 67,99 € (79.99 × 0.85 = 67.9915 → 67.99) |
| n'importe quel montant | `EXPIRED2025` | `expired` | `valid: false` |
| n'importe quel montant | `FUTURECODE` | `not_started` | `valid: false` |
| n'importe quel montant | code déjà à `MaxUses` | `exhausted` | `valid: false` |
| n'importe quel montant | `XYZ123` (inexistant) | `unknown_code` | `valid: false` |
| création avec `Percent=95` | — | `400` | rejeté |

## Hors périmètre

- Cumul de plusieurs codes sur une même commande.
- Codes personnalisés/limités à un client précis (`MaxUses` est un compteur global, pas par
  utilisateur).
- Remise ciblée sur des produits/lignes spécifiques (le code s'applique au total du panier).
- Devises autres que EUR.
- Traduction humaine des `reason` — ce sont des codes machine ; leur mise en phrase
  (« Ce code a expiré ») est la responsabilité du frontend (Épic 3), pas de ce service.
