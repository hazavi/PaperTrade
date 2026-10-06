# Feature and improvement roadmap

This document collects practical ideas for expanding PaperTrade. Items are grouped by priority and include the main technical work each feature requires.

## Recommended next milestone: multi-asset markets

PaperTrade currently focuses on stocks. The next useful expansion is a shared instrument model that supports equities, foreign exchange, metals, commodities, indices, ETFs, and crypto without adding separate trading systems for each market.

### Foreign exchange

Add major and commonly traded currency pairs:

- `EUR/USD`
- `GBP/USD`
- `USD/JPY`
- `USD/CHF`
- `AUD/USD`
- `USD/CAD`
- `NZD/USD`

FX support needs:

- Base and quote currencies
- Bid, ask, spread, and mid-price fields
- Pip and fractional-pip formatting
- Units or lot-size order entry
- 24-hour weekday market sessions
- Currency-aware profit and loss calculations
- Conversion into the portfolio's account currency

### Metals and commodities

Start with:

- `XAU/USD` — Gold priced in U.S. dollars
- `XAG/USD` — Silver priced in U.S. dollars
- WTI crude oil
- Brent crude oil
- Natural gas

Gold should appear as `XAU/USD`, not as a normal company stock. The instrument should define its tick size, contract or unit size, trading session, price precision, and quote currency.

Later additions could include copper, platinum, corn, wheat, coffee, and sugar. Futures-style instruments also require contract expiration and rollover rules.

### Other asset classes

- Market indices such as the S&P 500, Nasdaq 100, and DAX
- ETFs
- Crypto pairs such as `BTC/USD` and `ETH/USD`
- Bonds or government yields after the core multi-asset model is stable

### Required domain changes

Create an `Instrument` model with fields such as:

```text
Id
Symbol
DisplayName
AssetClass
Exchange
BaseCurrency
QuoteCurrency
PricePrecision
QuantityPrecision
TickSize
MinimumOrderSize
MarketTimeZone
TradingSession
IsTradable
```

Orders and positions should reference an instrument ID instead of assuming every symbol is a U.S. equity. Market-data providers should map their provider-specific symbols to PaperTrade's canonical instrument symbols.

## Trading improvements

### Order types

- Market orders
- Limit orders
- Stop orders
- Stop-limit orders
- Bracket orders with take profit and stop loss
- Trailing stop loss
- Good-for-day and good-till-cancelled duration

Pending orders should have a separate lifecycle: `pending`, `partially_filled`, `filled`, `cancelled`, `rejected`, or `expired`.

### Execution simulation

- Execute buys at ask and sells at bid
- Simulate spread and configurable slippage
- Reject orders outside market hours when appropriate
- Support partial fills for larger orders
- Record commissions and fees
- Keep an immutable execution ledger

The UI should clearly label simulated execution rules so users know why the execution price differs from the chart price.

### Margin and leverage

Add only after spot trading and account-currency conversion are correct:

- Configurable leverage by asset class
- Used margin, available margin, and margin level
- Maintenance-margin warnings
- Automatic liquidation rules
- Overnight financing or swap charges

Leverage should be disabled by default and presented as an educational simulation.

## Risk-management tools

- Risk a fixed percentage of portfolio value per trade
- Position-size calculator based on entry, stop loss, and account risk
- Risk/reward ratio displayed before order confirmation
- Maximum daily loss setting
- Maximum position concentration setting
- Duplicate or highly correlated exposure warning
- Portfolio drawdown chart
- Daily, weekly, and monthly profit/loss summaries
- Trading journal linked to each order

For FX and commodities, the position-size calculator must understand pips, tick value, contract size, and account-currency conversion.

## Chart improvements

- Save drawings per user, instrument, and timeframe
- Edit, move, duplicate, and delete individual drawings
- Undo and redo drawing changes
- Snap drawings to OHLC values
- Magnet mode and object tree
- Named chart layouts
- Multiple synchronized charts
- Compare instruments on the same chart
- Logarithmic price scale
- Extended-hours display
- Additional timeframes, including 1 minute, 5 minutes, 15 minutes, 1 hour, 4 hours, and weekly

Suggested indicators:

- RSI
- MACD
- ATR
- Stochastic oscillator
- VWAP
- Average volume
- Ichimoku Cloud
- Pivot points
- Support and resistance levels

Indicator settings should be editable and saved per user rather than hard-coded.

## Market-data improvements

- Create a provider-independent market-data interface for quotes, candles, search, and market status
- Store canonical instruments separately from provider symbols
- Add provider health and quota monitoring
- Fall back to a secondary provider without presenting fallback data as live
- Display data source, delay, and last-update time in the UI
- Detect stale quotes before accepting an order
- Backfill missing candle intervals
- Stream ticks when available and aggregate them into candles
- Use exchange calendars instead of fixed weekday assumptions

Before enabling a new asset class, verify that the selected provider license and subscription cover displaying and storing that data.

## Portfolio and analytics

- Portfolio equity curve
- Realized and unrealized profit/loss history
- Performance by instrument and asset class
- Win rate, average win, average loss, and profit factor
- Maximum drawdown
- Sharpe and Sortino ratios with clear educational explanations
- Trade calendar and daily performance heat map
- CSV export for orders, executions, and portfolio history
- Benchmark comparison

Analytics should be calculated from immutable executions and balance events rather than reconstructed from the current position alone.

## User experience

- Global instrument search with asset-class filters
- Recently viewed instruments
- Customizable dashboard widgets
- Light and dark themes while keeping the neobrutalist visual language
- Keyboard shortcuts for chart tools and order entry
- Command palette
- Responsive compact order ticket
- Clear empty, loading, offline, delayed-data, and rate-limit states
- Onboarding walkthrough using demo data
- Accessible focus states, labels, contrast, and reduced-motion support

## Notifications and engagement

- Percentage-change and volume alerts
- Moving-average crossover alerts
- Order-filled, stop-loss, and take-profit notifications
- In-app notification preferences
- Optional email notifications
- Alert history and reusable alert templates
- Private competitions with shared start date and balance
- Achievement system based on learning goals instead of trade frequency

## Platform improvements

- API versioning
- Idempotency keys for order submission
- Optimistic concurrency for portfolio updates
- Background jobs with retry and dead-letter handling
- Rate limits separated by endpoint and user
- Audit log for authentication and trading actions
- Structured provider error codes
- Database backup and restore documentation
- Feature flags for unfinished asset classes
- Admin health page for providers, queues, database, and Redis

## Security improvements

- Email verification
- Password reset flow
- Multi-factor authentication
- Session management and remote logout
- Login throttling and temporary lockout
- Content Security Policy and stricter production headers
- Secret rotation documentation
- Dependency and container vulnerability scanning

## Testing improvements

- Contract tests for every market-data provider
- Deterministic execution-engine tests using a fake clock
- Property-based tests for balances, fills, and currency conversion
- Tests for market sessions and daylight-saving transitions
- End-to-end tests for limit, stop, TP, and SL execution
- Visual regression tests for charts and responsive layouts
- Load tests for quote fan-out and SignalR subscriptions
- Failure tests for unavailable providers, Redis, and PostgreSQL

## Suggested implementation order

### Phase 1 — instrument foundation

1. Add the canonical `Instrument` model and asset-class enum.
2. Add provider-symbol mapping.
3. Make formatting depend on instrument precision and tick size.
4. Update orders, positions, watchlists, alerts, and search to reference instruments.

### Phase 2 — FX and gold

1. Add FX quote handling with bid, ask, and spread.
2. Add account-currency conversion.
3. Add the major FX pairs.
4. Add `XAU/USD` and `XAG/USD`.
5. Add units, lot sizes, pip values, and instrument-specific order validation.

### Phase 3 — better order simulation

1. Add pending orders and their lifecycle.
2. Add limit, stop, and bracket orders.
3. Execute against bid or ask with optional slippage.
4. Add fees and an immutable execution ledger.

### Phase 4 — risk and analytics

1. Add the risk-based position-size calculator.
2. Add drawdown and performance history.
3. Add the trading journal and exports.
4. Add portfolio-level risk limits.

### Phase 5 — advanced platform work

1. Add leverage and margin simulation.
2. Add more commodities, indices, and crypto.
3. Add saved chart layouts and synchronized multi-chart views.
4. Add competitions, advanced alerts, and optional email delivery.

## Scope rule

New features should preserve three boundaries:

- Real market data must always be identified by source and freshness.
- Demo or generated data must always be labeled as simulated.
- Every order remains a paper trade and never reaches a real broker.